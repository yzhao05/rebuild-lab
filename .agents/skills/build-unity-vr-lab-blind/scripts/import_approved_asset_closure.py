#!/usr/bin/env python3
"""Copy one approved Unity asset and its in-pack GUID dependency closure.

The destination is fixed to the current Lab's generated asset directory so a
rebuild never edits ApprovedAssets or the shared ApprovedReuse foundation.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import shutil
import sys
import uuid
from pathlib import Path


LAB_ID_PATTERN = re.compile(r"^[a-z][a-z0-9_]{2,63}$")
CURRENT_STATE_SCHEMA = "2.0.0"
GUID_PATTERN = re.compile(r"\bguid:\s*([0-9a-fA-F]{32})\b", re.IGNORECASE)
TEXT_EXTENSIONS = {
    ".anim", ".asmdef", ".asmref", ".asset", ".compute", ".controller", ".cs",
    ".inputactions", ".json", ".mat", ".meta", ".overridecontroller",
    ".physicmaterial", ".prefab", ".shader", ".txt", ".unity",
}


def native_path(path: Path) -> str:
    """Return an absolute OS path that is safe past MAX_PATH on Windows."""
    absolute = os.path.abspath(os.fspath(path))
    if os.name != "nt" or absolute.startswith("\\\\?\\"):
        return absolute
    if absolute.startswith("\\\\"):
        return "\\\\?\\UNC\\" + absolute[2:]
    return "\\\\?\\" + absolute


def fs_exists(path: Path) -> bool:
    return os.path.exists(native_path(path))


def fs_is_file(path: Path) -> bool:
    return os.path.isfile(native_path(path))


def ensure_directory(path: Path) -> None:
    os.makedirs(native_path(path), exist_ok=True)


def read_text(path: Path, encoding: str = "utf-8-sig") -> str:
    with open(native_path(path), "r", encoding=encoding) as stream:
        return stream.read()


def write_text(path: Path, value: str) -> None:
    ensure_directory(path.parent)
    with open(native_path(path), "w", encoding="utf-8", newline="\n") as stream:
        stream.write(value)


def copy_file(source: Path, destination: Path) -> None:
    ensure_directory(destination.parent)
    shutil.copy2(native_path(source), native_path(destination))


def remove_file(path: Path) -> None:
    os.unlink(native_path(path))


def load_json(path: Path) -> dict:
    return json.loads(read_text(path))


def workflow_gate(project_root: Path, lab_id: str) -> int:
    state_path = project_root / "LabSpecs" / lab_id / "workflow_state.json"
    if not state_path.is_file():
        raise RuntimeError(f"No active workflow state exists for {lab_id}")
    state = load_json(state_path)
    if state.get("lab_id") != lab_id:
        raise RuntimeError("Workflow state lab_id does not match --lab-id")
    if state.get("schema_version") != CURRENT_STATE_SCHEMA:
        raise RuntimeError("Workflow state schema is stale; run labflow migrate-state first")
    approvals = state.get("gate_approvals", {})
    for gate in range(11):
        approval = approvals.get(str(gate), {})
        if not isinstance(approval, dict) or not approval.get("approved", False):
            return gate
    return 11


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with open(native_path(path), "rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def remove_empty_import_directories(files: list[Path], destination_root: Path) -> None:
    """Remove empty directories created by a failed import, but never go higher."""
    candidates: set[Path] = set()
    for file_path in files:
        parent = file_path.parent
        while True:
            try:
                parent.relative_to(destination_root)
            except ValueError:
                break
            candidates.add(parent)
            if parent == destination_root:
                break
            parent = parent.parent
    for directory in sorted(candidates, key=lambda item: len(item.parts), reverse=True):
        try:
            os.rmdir(native_path(directory))
        except FileNotFoundError:
            continue
        except OSError:
            # The directory contains a pre-existing or conflicting file.
            continue


def rollback_uncommitted_import(files: list[Path], destination_root: Path) -> list[str]:
    failures: list[str] = []
    for path in reversed(files):
        try:
            if fs_exists(path):
                remove_file(path)
        except OSError as error:
            failures.append(f"{path}: {error}")
    remove_empty_import_directories(files, destination_root)
    return failures


def read_guids(path: Path) -> set[str]:
    if (
        path.suffix.lower() not in TEXT_EXTENSIONS
        or os.path.getsize(native_path(path)) > 32 * 1024 * 1024
    ):
        return set()
    try:
        with open(native_path(path), "r", encoding="utf-8-sig", errors="ignore") as stream:
            text = stream.read()
    except OSError:
        return set()
    return {match.lower() for match in GUID_PATTERN.findall(text)}


def guid_index(approved_root: Path) -> dict[str, Path]:
    index: dict[str, Path] = {}
    for meta in approved_root.rglob("*.meta"):
        matches = GUID_PATTERN.findall(meta.read_text(encoding="utf-8-sig", errors="ignore"))
        if not matches:
            continue
        asset = Path(str(meta)[:-5])
        if not asset.is_file():
            continue
        guid = matches[0].lower()
        if guid in index and index[guid] != asset:
            raise RuntimeError(f"Duplicate approved GUID {guid}: {index[guid]} and {asset}")
        index[guid] = asset
    return index


def nearest_unity_assembly_boundary(asset: Path, approved_root: Path) -> Path | None:
    """Return the asmdef/asmref that controls an imported C# source file."""
    if asset.suffix.lower() != ".cs":
        return None
    directory = asset.parent
    while True:
        boundaries: list[Path] = []
        try:
            with os.scandir(native_path(directory)) as entries:
                for entry in entries:
                    if entry.is_file() and Path(entry.name).suffix.lower() in {".asmdef", ".asmref"}:
                        boundaries.append(directory / entry.name)
        except OSError as error:
            raise RuntimeError(
                f"Cannot inspect Unity assembly boundary for approved script {asset}: {error}"
            ) from error
        boundaries.sort(key=lambda item: item.name.lower())
        if len(boundaries) > 1:
            names = ", ".join(item.name for item in boundaries)
            raise RuntimeError(
                f"Ambiguous Unity assembly boundary for approved script {asset}: {names}"
            )
        if boundaries:
            return boundaries[0]
        if directory == approved_root:
            return None
        try:
            directory.relative_to(approved_root)
        except ValueError as error:
            raise RuntimeError(f"Approved script escaped ApprovedAssets: {asset}") from error
        directory = directory.parent


def compatible_manifest_extension(existing: dict, replacement: dict) -> bool:
    """Allow an old committed closure to grow without changing prior records."""
    for key in ("schema_version", "lab_id", "approved_source", "destination_root", "dry_run"):
        if existing.get(key) != replacement.get(key):
            return False
    old_assets = existing.get("assets")
    new_assets = replacement.get("assets")
    if not isinstance(old_assets, list) or not isinstance(new_assets, list):
        return False
    old_by_source = {
        item.get("source"): item for item in old_assets if isinstance(item, dict)
    }
    new_by_source = {
        item.get("source"): item for item in new_assets if isinstance(item, dict)
    }
    if len(old_by_source) != len(old_assets) or len(new_by_source) != len(new_assets):
        return False
    if len(new_by_source) <= len(old_by_source):
        return False
    return all(new_by_source.get(source) == record for source, record in old_by_source.items())


def destinations_owned_by_other_manifests(
    project_root: Path,
    lab_id: str,
    current_manifest: Path,
) -> set[Path]:
    manifest_root = project_root / "BuildEvidence" / lab_id / "approved-asset-imports"
    if not manifest_root.is_dir():
        return set()

    owned: set[Path] = set()
    for manifest_path in manifest_root.glob("*.json"):
        if manifest_path.resolve() == current_manifest.resolve():
            continue
        try:
            payload = load_json(manifest_path)
        except (OSError, ValueError) as error:
            raise RuntimeError(
                f"Cannot audit existing import manifest ownership: {manifest_path}"
            ) from error
        if payload.get("lab_id") != lab_id:
            continue
        for record in payload.get("assets", []):
            destination = record.get("destination")
            if not isinstance(destination, str) or not destination:
                raise RuntimeError(
                    f"Existing import manifest has an invalid destination: {manifest_path}"
                )
            asset = (project_root / destination).resolve()
            owned.add(asset)
            owned.add(Path(str(asset) + ".meta"))
    return owned


def safe_relative_source(approved_root: Path, source_value: str) -> Path:
    source = (approved_root / Path(source_value.replace("\\", "/"))).resolve()
    try:
        source.relative_to(approved_root)
    except ValueError as error:
        raise RuntimeError("--source must remain inside ApprovedAssets") from error
    if not source.is_file():
        raise RuntimeError(f"Approved source file does not exist: {source}")
    if source.suffix.lower() == ".unity":
        raise RuntimeError(
            "Approved scene files cannot be imported; build a new Lab scene and import only approved prefabs/dependencies"
        )
    meta = Path(str(source) + ".meta")
    if not meta.is_file():
        raise RuntimeError(f"Approved source has no .meta file: {meta}")
    return source


def asset_record(source: Path, approved_root: Path, destination_root: Path) -> dict[str, str]:
    relative = source.relative_to(approved_root)
    destination = destination_root / relative
    destination.resolve().relative_to(destination_root.resolve())
    source_meta = Path(str(source) + ".meta")
    return {
        "source": source.relative_to(approved_root.parent).as_posix(),
        "destination": destination.relative_to(approved_root.parent).as_posix(),
        "sha256": sha256(source),
        "meta_sha256": sha256(source_meta),
    }


def import_closure(project_root: Path, lab_id: str, source_value: str, dry_run: bool) -> dict:
    if not LAB_ID_PATTERN.fullmatch(lab_id):
        raise RuntimeError("lab_id must use lowercase letters, digits, and underscores")
    active_gate = workflow_gate(project_root, lab_id)
    if not dry_run and active_gate != 5:
        raise RuntimeError(
            f"Approved assets may be copied only during Gate 5; current gate is {active_gate}"
        )
    approved_root = (project_root / "ApprovedAssets").resolve()
    unity_assets = (project_root / "UnityProject" / "Assets").resolve()
    destination_root = (
        unity_assets / "Generated" / lab_id / "ApprovedDependencies"
    ).resolve()
    destination_root.relative_to(unity_assets)
    source = safe_relative_source(approved_root, source_value)
    index = guid_index(approved_root)

    pending = [source]
    visited: set[Path] = set()
    records: list[dict[str, str]] = []
    unresolved: set[str] = set()
    while pending:
        asset = pending.pop()
        if asset in visited:
            continue
        visited.add(asset)
        records.append(asset_record(asset, approved_root, destination_root))
        references = read_guids(asset) | read_guids(Path(str(asset) + ".meta"))
        for guid in sorted(references):
            dependency = index.get(guid)
            if dependency is None:
                unresolved.add(guid)
            elif dependency not in visited:
                pending.append(dependency)
        boundary = nearest_unity_assembly_boundary(asset, approved_root)
        if boundary is not None and boundary not in visited:
            pending.append(boundary)

    source_relative = source.relative_to(approved_root).as_posix()
    payload = {
        "schema_version": "1.0.0",
        "lab_id": lab_id,
        "approved_source": f"ApprovedAssets/{source_relative}",
        "destination_root": destination_root.relative_to(project_root).as_posix(),
        "asset_count": len(records),
        "assets": sorted(records, key=lambda item: item["source"]),
        "external_or_unresolved_guid_count": len(unresolved),
        "external_or_unresolved_guids": sorted(unresolved),
        "dry_run": dry_run,
    }
    if not dry_run:
        key = hashlib.sha256(source_relative.encode("utf-8")).hexdigest()[:10]
        manifest = (
            project_root / "BuildEvidence" / lab_id / "approved-asset-imports"
            / f"{source.stem}-{key}.json"
        )
        serialized = json.dumps(payload, ensure_ascii=False, indent=2) + "\n"

        # Preflight the complete closure before writing anything. A conflict in
        # one dependency must not leave an earlier subset copied into the Lab.
        copy_plan: list[tuple[Path, Path]] = []
        for record in records:
            src = project_root / record["source"]
            dst = project_root / record["destination"]
            copy_plan.extend(
                ((src, dst), (Path(str(src) + ".meta"), Path(str(dst) + ".meta")))
            )
        manifest_exists = fs_exists(manifest)
        manifest_upgrade = False
        if manifest_exists and read_text(manifest) != serialized:
            try:
                existing_manifest = load_json(manifest)
            except (OSError, ValueError) as error:
                raise RuntimeError(f"Cannot read existing import manifest: {manifest}") from error
            if existing_manifest != payload:
                if not compatible_manifest_extension(existing_manifest, payload):
                    raise RuntimeError(
                        f"Refusing to overwrite a different import manifest: {manifest}"
                    )
                manifest_upgrade = True

        existing_matching: list[Path] = []
        missing: list[tuple[Path, Path]] = []
        for src, dst in copy_plan:
            if fs_exists(dst):
                if not fs_is_file(dst) or sha256(src) != sha256(dst):
                    raise RuntimeError(f"Refusing to overwrite a different imported asset: {dst}")
                existing_matching.append(dst)
            else:
                missing.append((src, dst))
        if manifest_exists and missing and not manifest_upgrade:
            raise RuntimeError(
                f"Import manifest exists but {len(missing)} imported files are missing: {manifest}"
            )

        created: list[Path] = []
        manifest_temp = manifest.with_name(f".{manifest.name}.tmp-{uuid.uuid4().hex}")
        try:
            for src, dst in missing:
                created.append(dst)
                copy_file(src, dst)
                if sha256(src) != sha256(dst):
                    raise RuntimeError(f"Imported asset hash verification failed: {dst}")
            if not manifest_exists or manifest_upgrade:
                write_text(manifest_temp, serialized)
                os.replace(native_path(manifest_temp), native_path(manifest))
        except (OSError, RuntimeError) as error:
            if fs_exists(manifest_temp):
                try:
                    remove_file(manifest_temp)
                except OSError:
                    pass
            # With no committed manifest, exact-hash pre-existing files may be
            # uncommitted leftovers, but they may also be shared dependencies
            # owned by another committed closure. Never roll back a destination
            # recorded by another manifest.
            protected = destinations_owned_by_other_manifests(
                project_root,
                lab_id,
                manifest,
            )
            candidates = created if manifest_exists else created + existing_matching
            rollback = [path for path in candidates if path.resolve() not in protected]
            cleanup_failures = rollback_uncommitted_import(rollback, destination_root)
            detail = f"; rollback failures: {' | '.join(cleanup_failures)}" if cleanup_failures else ""
            raise RuntimeError(
                f"Approved asset import failed; uncommitted output was rolled back: {error}{detail}"
            ) from error
        payload["manifest"] = manifest.relative_to(project_root).as_posix()
    return payload


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--project-root",
        type=Path,
        default=Path(__file__).resolve().parents[4],
        help="Builder Pack root",
    )
    parser.add_argument("--lab-id", required=True)
    parser.add_argument(
        "--source",
        required=True,
        help="File path relative to ApprovedAssets",
    )
    parser.add_argument("--dry-run", action="store_true")
    return parser


def main() -> int:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    args = build_parser().parse_args()
    try:
        payload = import_closure(
            args.project_root.resolve(), args.lab_id, args.source, args.dry_run
        )
    except RuntimeError as error:
        print(f"ERROR: {error}", file=sys.stderr)
        return 2
    print(json.dumps(payload, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
