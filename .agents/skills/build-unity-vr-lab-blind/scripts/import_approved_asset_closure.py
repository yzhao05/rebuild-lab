#!/usr/bin/env python3
"""Copy one approved Unity asset and its in-pack GUID dependency closure.

The destination is fixed to the current Lab's generated asset directory so a
rebuild never edits ApprovedAssets or the shared ApprovedReuse foundation.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import shutil
import sys
from pathlib import Path


LAB_ID_PATTERN = re.compile(r"^[a-z][a-z0-9_]{2,63}$")
CURRENT_STATE_SCHEMA = "1.7.0"
GUID_PATTERN = re.compile(r"\bguid:\s*([0-9a-fA-F]{32})\b")
TEXT_EXTENSIONS = {
    ".anim", ".asmdef", ".asset", ".compute", ".controller", ".cs",
    ".inputactions", ".json", ".mat", ".meta", ".overridecontroller",
    ".physicmaterial", ".prefab", ".shader", ".txt", ".unity",
}


def load_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8-sig"))


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
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def read_guids(path: Path) -> set[str]:
    if path.suffix.lower() not in TEXT_EXTENSIONS or path.stat().st_size > 32 * 1024 * 1024:
        return set()
    try:
        text = path.read_text(encoding="utf-8-sig", errors="ignore")
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
        for src, dst in copy_plan:
            if dst.exists():
                if not dst.is_file() or sha256(src) != sha256(dst):
                    raise RuntimeError(f"Refusing to overwrite a different imported asset: {dst}")
        if manifest.exists() and manifest.read_text(encoding="utf-8-sig") != serialized:
            raise RuntimeError(f"Refusing to overwrite a different import manifest: {manifest}")

        for src, dst in copy_plan:
            if not dst.exists():
                dst.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(src, dst)
        manifest.parent.mkdir(parents=True, exist_ok=True)
        if not manifest.exists():
            manifest.write_text(serialized, encoding="utf-8", newline="\n")
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
