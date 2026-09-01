#!/usr/bin/env python3
"""Contract-first workflow CLI for Summer26 Unity VR labs.

This tool writes project metadata and C# source only. It never starts Unity,
edits scene YAML, or marks editor-only checks as passed.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


CONTRACT_KINDS = ("lab_spec", "objects", "steps", "rubric", "telemetry_map")
LAB_ID_PATTERN = re.compile(r"^[a-z][a-z0-9_]{2,63}$")
RESULT_VALUES = ("pending", "passed", "failed")
CURRENT_STATE_SCHEMA = "1.9.0"
PROTECTED_BASELINE_SCHEMA = "1.0.0"
WORKFLOW_MODES = ("blind-rebuild", "integrated-development")
REBUILD_WORKSPACE_PREFIX = "EMVR_Blind_Rebuild_"
MASTER_PACK_DIRECTORY_NAME = "EMVR_Blind_BuilderPack"
WORKSPACE_MARKER_SCHEMA = "1.1.0"
PROTECTED_BASELINE_ROOTS = (
    ".gitattributes",
    ".gitignore",
    "AGENTS.md",
    "README.md",
    "README_USER.md",
    ".agents",
    "Policies",
    "Tools",
    "LocalPackages",
    "UnityProject/LocalPackages",
    "OptionalIntegrations",
    "ReusableFoundation",
    "ApprovedAssets",
    "KnowledgeBase",
    "LabSpecs/templates",
    "UnityProject/Packages",
    "UnityProject/ProjectSettings",
    "UnityProject/Assets/ApprovedReuse",
    "UnityProject/Assets/Plugins",
    "UnityProject/Assets/XR",
    "UnityProject/Assets/XRI",
    "UnityProject/Assets/.gitkeep",
    "UnityProject/Assets/ApprovedReuse.meta",
    "UnityProject/Assets/Editor.meta",
    "UnityProject/Assets/Generated.meta",
    "UnityProject/Assets/Plugins.meta",
    "UnityProject/Assets/Scenes.meta",
    "UnityProject/Assets/Scripts.meta",
    "UnityProject/Assets/Tests.meta",
    "UnityProject/Assets/XR.meta",
    "UnityProject/Assets/XRI.meta",
)
GATE_NAMES = {
    0: "Boundary verified",
    1: "Brief confirmed",
    2: "Contracts ready",
    3: "Design approved",
    4: "Scaffold ready",
    5: "Reusable foundation and domain slice validated",
    6: "Unity scene validated",
    7: "Interaction and lifecycle passed",
    8: "Acceptance package ready",
    9: "User accepted",
    10: "Retrospective captured",
}

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")
if hasattr(sys.stderr, "reconfigure"):
    sys.stderr.reconfigure(encoding="utf-8")


def default_evidence() -> dict[str, Any]:
    return {
        "boundary_verifier": "pending",
        "scaffold_compile": "pending",
        "gate5_foundation": "pending",
        "domain_slice": "pending",
        "domain_compile": "pending",
        "compile": "pending",
        "scene_built": "pending",
        "playtest": "pending",
        "notes": [],
    }


def ensure_evidence(state: dict[str, Any]) -> dict[str, Any]:
    evidence = state.setdefault("evidence", {})
    for key, value in default_evidence().items():
        if key not in evidence:
            evidence[key] = [] if key == "notes" else value
    return evidence


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="seconds")


def write_text(path: Path, content: str, force: bool = False) -> None:
    if path.exists() and not force:
        raise RuntimeError(f"Refusing to overwrite existing file: {path}")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8", newline="\n")


def write_json(path: Path, payload: dict[str, Any], force: bool = False) -> None:
    write_text(path, json.dumps(payload, ensure_ascii=False, indent=2) + "\n", force)


def read_json(path: Path) -> dict[str, Any]:
    try:
        return json.loads(path.read_text(encoding="utf-8-sig"))
    except json.JSONDecodeError as error:
        raise RuntimeError(f"Invalid JSON in {path}: {error}") from error


def native_path(path: Path) -> str:
    """Return an absolute OS path that bypasses legacy MAX_PATH on Windows."""
    absolute = os.path.abspath(os.fspath(path))
    if os.name != "nt" or absolute.startswith("\\\\?\\"):
        return absolute
    if absolute.startswith("\\\\"):
        return "\\\\?\\UNC\\" + absolute[2:]
    return "\\\\?\\" + absolute


def normal_path(path: str) -> Path:
    if os.name == "nt" and path.startswith("\\\\?\\UNC\\"):
        return Path("\\\\" + path[8:])
    if os.name == "nt" and path.startswith("\\\\?\\"):
        return Path(path[4:])
    return Path(path)


def fs_exists(path: Path) -> bool:
    return os.path.exists(native_path(path))


def fs_is_file(path: Path) -> bool:
    return os.path.isfile(native_path(path))


def iter_files(path: Path):
    if not fs_exists(path):
        return
    for directory, _subdirectories, files in os.walk(native_path(path)):
        display_directory = normal_path(directory)
        for file_name in files:
            yield display_directory / file_name


def validate_workspace_mode(root: Path, lab_id: str, workflow_mode: str) -> None:
    if workflow_mode not in WORKFLOW_MODES:
        raise RuntimeError(
            "workflow mode must be blind-rebuild or integrated-development"
        )
    if workflow_mode == "integrated-development":
        return

    marker_path = root / ".emvr-workspace.json"
    expected_name = f"{REBUILD_WORKSPACE_PREFIX}{lab_id}"
    if root.name != expected_name:
        raise RuntimeError(
            "Blind rebuilds must run in the fixed internal workspace "
            f"RebuildWorkspaces/{expected_name}; current directory is {root.name}. "
            "The rebuilding Agent must create it from the master Builder Pack with "
            f"`powershell -ExecutionPolicy Bypass -File Tools/create_blind_run_pack.ps1 -LabId {lab_id}`."
        )
    if (
        root.parent.name != "RebuildWorkspaces"
        or root.parent.parent.name != MASTER_PACK_DIRECTORY_NAME
        or not marker_path.is_file()
    ):
        raise RuntimeError(
            "Blind rebuild workspace marker/parent is missing. The rebuilding "
            "Agent must use the fixed create_blind_run_pack.ps1 command from "
            "the master Builder Pack; do not delegate it to the User."
        )
    marker = read_json(marker_path)
    if (
        marker.get("schema_version") != WORKSPACE_MARKER_SCHEMA
        or marker.get("workflow_mode") != "blind-rebuild"
        or marker.get("lab_id") != lab_id
        or marker.get("required_master_directory_name") != MASTER_PACK_DIRECTORY_NAME
        or marker.get("required_directory_name") != expected_name
    ):
        raise RuntimeError("Blind rebuild workspace marker does not match the requested Lab ID.")


def file_sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with open(native_path(path), "rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def directory_digest(path: Path) -> str:
    digest = hashlib.sha256()
    if not path.exists():
        return digest.hexdigest().upper()
    lines = []
    files = [item for item in path.rglob("*") if item.is_file()]
    for file_path in sorted(files, key=lambda item: item.relative_to(path).as_posix()):
        relative = file_path.relative_to(path).as_posix()
        lines.append(f"{relative}\t{file_sha256(file_path)}")
    digest.update("\n".join(lines).encode("utf-8"))
    return digest.hexdigest().upper()


def protected_baseline_relative_path(lab_id: str) -> Path:
    return Path("BuildEvidence") / lab_id / "gate0-protected-baseline.json"


def protected_baseline_exclusions(lab_id: str) -> tuple[str, ...]:
    # Gate 10 creates this one safe lesson after Gate 0. It must not invalidate
    # the run baseline, while all pre-existing and cross-Lab lessons stay frozen.
    return (f"KnowledgeBase/rebuild-lessons/{lab_id}.md",)


def is_protected_baseline_excluded(relative: str, exclusions: set[str]) -> bool:
    parts = Path(relative).parts
    return (
        relative in exclusions
        or "__pycache__" in parts
        or relative.endswith((".pyc", ".pyo"))
    )


def collect_protected_files(
    root: Path,
    scope_roots: tuple[str, ...] | list[str],
    exclusions: tuple[str, ...] | list[str],
    known_files: dict[str, dict[str, Any]] | None = None,
) -> dict[str, Any]:
    excluded = set(exclusions)
    files: dict[str, dict[str, Any]] = {}
    present_roots: list[str] = []
    missing_roots: list[str] = []
    for scope in scope_roots:
        target = root / scope
        if not fs_exists(target):
            missing_roots.append(scope)
            continue
        present_roots.append(scope)
        candidates = [target] if fs_is_file(target) else list(iter_files(target))
        for candidate in candidates:
            relative = candidate.relative_to(root).as_posix()
            if is_protected_baseline_excluded(relative, excluded):
                continue
            stat = os.stat(native_path(candidate))
            known = (known_files or {}).get(relative, {})
            if (
                known.get("size") == stat.st_size
                and known.get("mtime_ns") == stat.st_mtime_ns
                and isinstance(known.get("sha256"), str)
            ):
                digest = known["sha256"]
            else:
                digest = file_sha256(candidate)
            files[relative] = {
                "size": stat.st_size,
                "mtime_ns": stat.st_mtime_ns,
                "sha256": digest,
            }
    return {
        "present_roots": sorted(present_roots),
        "missing_roots": sorted(missing_roots),
        "files": {key: files[key] for key in sorted(files)},
    }


def protected_baseline_errors(root: Path, state: dict[str, Any]) -> list[str]:
    lab_id = str(state.get("lab_id", ""))
    metadata = state.get("protected_baseline")
    expected_relative = protected_baseline_relative_path(lab_id).as_posix()
    if not isinstance(metadata, dict):
        return [
            "Gate 0 protected baseline has not been captured. After the release-level "
            f"Builder Boundary passes, run `python Tools/labflow/labflow.py capture-gate0-baseline --lab-id {lab_id}`."
        ]
    if metadata.get("path") != expected_relative:
        return ["Workflow state points to an unexpected Gate 0 protected-baseline path."]
    baseline_path = root / expected_relative
    if not fs_is_file(baseline_path):
        return [f"Gate 0 protected baseline is missing: {expected_relative}"]
    if file_sha256(baseline_path) != metadata.get("sha256"):
        return ["Gate 0 protected-baseline file was changed after capture."]
    try:
        baseline = read_json(baseline_path)
    except RuntimeError as error:
        return [str(error)]
    if baseline.get("schema_version") != PROTECTED_BASELINE_SCHEMA:
        return ["Gate 0 protected-baseline schema is unsupported; do not regenerate it mid-run."]
    if baseline.get("lab_id") != lab_id:
        return ["Gate 0 protected baseline belongs to a different Lab ID."]
    if baseline.get("scope_roots") != list(PROTECTED_BASELINE_ROOTS):
        return ["Gate 0 protected-baseline scope differs from the current workflow definition."]
    expected_exclusions = list(protected_baseline_exclusions(lab_id))
    if baseline.get("excluded_paths") != expected_exclusions:
        return ["Gate 0 protected-baseline exclusions differ from the current workflow definition."]

    baseline_files = baseline.get("files", {})
    if not isinstance(baseline_files, dict):
        return ["Gate 0 protected baseline has an invalid files table."]
    if baseline.get("file_count") != len(baseline_files):
        return ["Gate 0 protected baseline file_count does not match its files table."]
    current = collect_protected_files(
        root, PROTECTED_BASELINE_ROOTS, expected_exclusions, baseline_files
    )
    errors: list[str] = []
    if current["present_roots"] != baseline.get("present_roots"):
        errors.append("Protected root presence changed after Gate 0 capture.")
    if current["missing_roots"] != baseline.get("missing_roots"):
        errors.append("Protected missing-root set changed after Gate 0 capture.")
    current_files = current["files"]
    for relative in sorted(set(baseline_files) - set(current_files)):
        errors.append(f"Protected file was removed after Gate 0: {relative}")
    for relative in sorted(set(current_files) - set(baseline_files)):
        errors.append(f"Protected file was added after Gate 0: {relative}")
    for relative in sorted(set(baseline_files) & set(current_files)):
        baseline_entry = baseline_files[relative]
        current_entry = current_files[relative]
        if (
            baseline_entry.get("size") != current_entry.get("size")
            or baseline_entry.get("sha256") != current_entry.get("sha256")
        ):
            errors.append(f"Protected file changed after Gate 0: {relative}")
    return errors


def capture_gate0_protected_baseline(
    root: Path, lab_id: str, state: dict[str, Any]
) -> Path:
    if current_gate(state) != 0:
        raise RuntimeError("The protected baseline may be captured only during Gate 0.")
    if state.get("protected_baseline") is not None:
        raise RuntimeError("Gate 0 protected baseline is already registered and cannot be replaced.")
    evidence = ensure_evidence(state)
    if evidence.get("boundary_verifier") != "passed":
        raise RuntimeError("Record a passed release-level Builder Boundary check before baseline capture.")
    release_errors = foundation_errors(root)
    if release_errors:
        raise RuntimeError(
            "Release-level Builder Boundary/foundation checks failed; Gate 0 cannot freeze a bad pack:\n- "
            + "\n- ".join(release_errors)
        )
    relative = protected_baseline_relative_path(lab_id)
    path = root / relative
    if fs_exists(path):
        raise RuntimeError(f"Refusing to overwrite existing Gate 0 baseline: {relative.as_posix()}")
    exclusions = protected_baseline_exclusions(lab_id)
    snapshot = collect_protected_files(root, PROTECTED_BASELINE_ROOTS, exclusions)
    payload = {
        "schema_version": PROTECTED_BASELINE_SCHEMA,
        "lab_id": lab_id,
        "captured_at": utc_now(),
        "state_schema": CURRENT_STATE_SCHEMA,
        "scope_roots": list(PROTECTED_BASELINE_ROOTS),
        "excluded_paths": list(exclusions),
        "present_roots": snapshot["present_roots"],
        "missing_roots": snapshot["missing_roots"],
        "file_count": len(snapshot["files"]),
        "files": snapshot["files"],
    }
    path.parent.mkdir(parents=True, exist_ok=True)
    content = json.dumps(payload, ensure_ascii=False, indent=2) + "\n"
    try:
        with open(native_path(path), "x", encoding="utf-8", newline="\n") as stream:
            stream.write(content)
        state["protected_baseline"] = {
            "path": relative.as_posix(),
            "sha256": file_sha256(path),
            "captured_at": payload["captured_at"],
            "file_count": payload["file_count"],
        }
        save_state(root, lab_id, state)
    except Exception:
        if fs_is_file(path):
            os.remove(native_path(path))
        state["protected_baseline"] = None
        raise
    return path


def mandatory_approved_assets(root: Path) -> list[str]:
    policy_path = root / "Policies" / "reuse-allowlist.json"
    if policy_path.exists():
        policy = read_json(policy_path)
        configured = policy.get("mandatory_assets", [])
        if isinstance(configured, list) and configured:
            return [str(item) for item in configured]
    return [
        "ApprovedAssets/EMVRRoom/Prefabs/Room_Big_Part_01.prefab"
    ]


def approved_room_asset_errors(root: Path) -> list[str]:
    room = root / "ApprovedAssets" / "EMVRRoom" / "Prefabs" / "Room_Big_Part_01.prefab"
    if not room.exists():
        return []
    text = room.read_text(encoding="utf-8-sig")
    errors: list[str] = []
    for geometry in ("Floor", "Wall", "Roof"):
        if re.search(rf"^\s*m_Name:\s*.*{geometry}", text, flags=re.MULTILINE) is None:
            errors.append(f"Mandatory EMVR room has no {geometry} geometry: {room.relative_to(root)}")
    renderer_count = len(re.findall(r"^MeshRenderer:$", text, flags=re.MULTILINE))
    if renderer_count < 6:
        errors.append(
            "Mandatory EMVR room is not a complete room shell; "
            f"expected at least 6 MeshRenderers but found {renderer_count}."
        )
    return errors


def foundation_errors(root: Path) -> list[str]:
    errors: list[str] = []
    for maintenance_leak in (
        root / "WORKFLOW_MAINTAINER_GUIDE.md",
        root / "WorkflowMaintenance",
        root / "EMVR_Workflow_Maintenance",
    ):
        if maintenance_leak.exists():
            errors.append(
                "Maintainer-only material leaked into the Builder Pack: "
                f"{maintenance_leak.relative_to(root)}"
            )
    workspace_marker = root / ".emvr-workspace.json"
    if workspace_marker.exists():
        try:
            marker = read_json(workspace_marker)
            marker_lab_id = str(marker.get("lab_id", ""))
            validate_workspace_mode(root, marker_lab_id, str(marker.get("workflow_mode", "")))
        except RuntimeError as error:
            errors.append(f"Invalid rebuild workspace marker/location: {error}")
    for relative in (
        ".gitattributes",
        ".gitignore",
        "Policies/reuse-allowlist.json",
        "Policies/gold-exclusions.json",
        "Policies/pack-inventory.json",
        "AGENTS.md",
        "README.md",
        ".agents/skills/build-unity-vr-lab-blind/SKILL.md",
        ".agents/skills/build-unity-vr-lab-blind/agents/openai.yaml",
        ".agents/skills/build-unity-vr-lab-blind/references/workflow-gates.md",
        ".agents/skills/build-unity-vr-lab-blind/references/foundation-preflight.md",
        ".agents/skills/build-unity-vr-lab-blind/references/mandatory-reuse-matrix.md",
        ".agents/skills/build-unity-vr-lab-blind/references/unity-beginner-handoff.md",
        "Tools/labflow/labflow.py",
        "Tools/create_blind_run_pack.ps1",
        "KnowledgeBase/rebuild-lessons/README.md",
        "KnowledgeBase/rebuild-lessons/build-experience/README.md",
        "ReusableFoundation/Gate5MinimalSlice/manifest.json",
        "ReusableFoundation/Gate5MinimalSlice/Source/Runtime/EmVrGate5FoundationHarness.cs",
        ".agents/skills/build-unity-vr-lab-blind/scripts/import_approved_asset_closure.py",
        ".agents/skills/build-unity-vr-lab-blind/scripts/materialize_gate5_foundation.py",
        ".agents/skills/build-unity-vr-lab-blind/scripts/verify_builder_pack.ps1",
    ):
        if not (root / relative).exists():
            errors.append(f"Required Builder foundation item is missing: {relative}")

    attributes_path = root / ".gitattributes"
    if attributes_path.exists():
        attribute_rules = {
            line.split("#", 1)[0].strip()
            for line in attributes_path.read_text(encoding="utf-8-sig").splitlines()
        }
        if "* text=auto eol=lf" not in attribute_rules:
            errors.append(
                ".gitattributes must enforce '* text=auto eol=lf' so frozen hashes survive Git checkout"
            )

    if (root / "KnowledgeBase" / "EMVR-Agent-Knowledge").exists():
        errors.append("Unsafe legacy Agent knowledge bundle was not moved to the restricted archive")
    if (root / "KnowledgeBase" / "EMVR-Agent-Knowledge" / "summary").exists():
        errors.append("Old Lab-specific summaries were not moved to the restricted archive")
    if list((root / "LabInterfaceContracts").glob("*.example.json")):
        errors.append("Legacy example contracts remain in the active contract directory")
    restricted_archive = root / "KnowledgeBase" / "legacy-lab-specific-summaries"
    if restricted_archive.exists() and not (restricted_archive / "README.md").exists():
        errors.append("Restricted legacy archive exists without its boundary README")

    errors.extend(gate5_foundation_source_errors(root))

    manifest_path = root / "UnityProject" / "Packages" / "manifest.json"
    lock_path = manifest_path.parent / "packages-lock.json"
    if not manifest_path.exists() or not lock_path.exists():
        errors.append("Unity manifest.json or packages-lock.json is missing.")
        return errors

    manifest = read_json(manifest_path)
    lock = read_json(lock_path)
    dependencies = manifest.get("dependencies", {})
    locked_dependencies = lock.get("dependencies", {})
    for package_name, version in dependencies.items():
        if not isinstance(version, str) or not version.lower().startswith("file:"):
            continue
        resolved = (manifest_path.parent / version[5:]).resolve()
        if not (resolved / "package.json").exists():
            errors.append(f"{package_name} does not resolve to package.json: {version} -> {resolved}")
        locked = locked_dependencies.get(package_name, {})
        if locked.get("version") != version:
            errors.append(f"{package_name} differs between manifest.json and packages-lock.json")

    common_value = dependencies.get("com.emvr.lab-common")
    expected_common = (root / "UnityProject" / "LocalPackages" / "com.emvr.lab-common").resolve()
    if common_value != "file:../LocalPackages/com.emvr.lab-common":
        errors.append("com.emvr.lab-common must use file:../LocalPackages/com.emvr.lab-common")
    elif (manifest_path.parent / common_value[5:]).resolve() != expected_common:
        errors.append("com.emvr.lab-common does not resolve to UnityProject/LocalPackages")

    if "com.ivanmurzak.unity.mcp" in dependencies:
        errors.append("Unity MCP is disabled but remains in manifest.json")
    settings = root / "UnityProject" / "ProjectSettings" / "ProjectSettings.asset"
    if settings.exists() and "UNITY_MCP_READY" in settings.read_text(encoding="utf-8-sig"):
        errors.append("UNITY_MCP_READY is present while Unity MCP is disabled")
    if (root / "UnityProject" / "Assets" / "Plugins" / "NuGet").exists():
        errors.append("MCP NuGet DLLs remain in UnityProject/Assets while MCP is disabled")

    canonical_common = root / "UnityProject" / "LocalPackages" / "com.emvr.lab-common"
    canonical_package_json = (
        root / "UnityProject" / "LocalPackages" / "com.emvr.lab-common" / "package.json"
    )
    inventory_path = root / "Policies" / "pack-inventory.json"
    if canonical_package_json.exists() and inventory_path.exists():
        inventory = read_json(inventory_path)
        common_inventory = inventory.get("sections", {}).get(
            "UnityProject/LocalPackages/com.emvr.lab-common", {}
        )
        expected_hash = common_inventory.get("package_json_sha256")
        if expected_hash and file_sha256(canonical_package_json) != expected_hash:
            errors.append("Canonical Common package.json differs from the frozen inventory")
        expected_digest = common_inventory.get("directory_digest_sha256")
        if expected_digest and directory_digest(canonical_common) != expected_digest:
            errors.append("Canonical Common directory differs from the frozen inventory")
        legacy_common = root / "LocalPackages" / "com.emvr.lab-common"
        if legacy_common.exists() and directory_digest(legacy_common) != directory_digest(canonical_common):
            errors.append("Duplicate Common packages have drifted")
        approved_inventory = inventory.get("sections", {}).get("ApprovedAssets", {})
        expected_approved_digest = approved_inventory.get("directory_digest_sha256")
        if expected_approved_digest and directory_digest(root / "ApprovedAssets") != expected_approved_digest:
            errors.append("ApprovedAssets differs from the frozen inventory")

    for required_asset in mandatory_approved_assets(root):
        required_path = root / required_asset
        if not required_path.exists() or not Path(str(required_path) + ".meta").exists():
            errors.append(f"Mandatory approved asset or its .meta is missing: {required_asset}")
    errors.extend(approved_room_asset_errors(root))
    active_states = list((root / "LabSpecs").glob("*/workflow_state.json"))
    if len(active_states) > 1:
        errors.append(
            "More than one active Lab workflow exists in this Builder Pack; use one isolated Pack per run"
        )
    return errors


def require_foundation(root: Path) -> None:
    errors = foundation_errors(root)
    if errors:
        details = "\n".join(f"- {error}" for error in errors)
        raise RuntimeError(
            "Builder foundation preflight failed. Stop and report this to the user; "
            f"do not repair it during the Lab rebuild.\n{details}"
        )


def require_lab_id(lab_id: str) -> str:
    if not LAB_ID_PATTERN.fullmatch(lab_id):
        raise RuntimeError(
            "lab_id must start with a lowercase letter and contain only "
            "lowercase letters, digits, and underscores (3-64 characters)."
        )
    return lab_id


def gate5_foundation_source_errors(root: Path) -> list[str]:
    errors: list[str] = []
    foundation = root / "ReusableFoundation" / "Gate5MinimalSlice"
    source_root = foundation / "Source"
    manifest_path = foundation / "manifest.json"
    if not manifest_path.exists():
        return ["Gate 5 reusable foundation manifest is missing."]
    try:
        manifest = read_json(manifest_path)
    except RuntimeError as error:
        return [str(error)]
    entries = manifest.get("files", [])
    if not entries:
        return ["Gate 5 reusable foundation manifest has no files."]
    listed_paths: set[str] = set()
    for entry in entries:
        relative = Path(str(entry.get("path", "")))
        normalized = relative.as_posix()
        if normalized in listed_paths:
            errors.append(f"Gate 5 foundation manifest contains a duplicate path: {normalized}")
            continue
        listed_paths.add(normalized)
        expected_hash = str(entry.get("sha256", ""))
        source = (source_root / relative).resolve()
        if source_root.resolve() not in source.parents:
            errors.append(f"Gate 5 foundation manifest path escapes Source: {relative.as_posix()}")
            continue
        if not source.is_file():
            errors.append(f"Gate 5 foundation source is missing: {relative.as_posix()}")
            continue
        if file_sha256(source) != expected_hash:
            errors.append(f"Gate 5 foundation source hash mismatch: {relative.as_posix()}")
    actual_non_meta = {
        path.relative_to(source_root).as_posix()
        for path in source_root.rglob("*")
        if path.is_file() and path.suffix.lower() != ".meta"
    } if source_root.exists() else set()
    for unlisted in sorted(actual_non_meta - listed_paths):
        errors.append(f"Gate 5 foundation source file is not listed in the manifest: {unlisted}")
    return errors


def gate5_materialization_errors(root: Path, lab_id: str) -> list[str]:
    errors = gate5_foundation_source_errors(root)
    if errors:
        return errors
    manifest_path = root / "ReusableFoundation" / "Gate5MinimalSlice" / "manifest.json"
    manifest = read_json(manifest_path)
    source_root = manifest_path.parent / "Source"
    destination_root = (
        root / "UnityProject" / "Assets" / "Generated" / lab_id / "Gate5Foundation"
    )
    evidence_path = root / "BuildEvidence" / lab_id / "gate5-foundation-import.json"
    if not evidence_path.exists():
        errors.append(
            "Gate 5 foundation import evidence is missing; run materialize_gate5_foundation.py."
        )
        return errors
    evidence = read_json(evidence_path)
    if evidence.get("lab_id") != lab_id:
        errors.append("Gate 5 foundation import evidence has the wrong lab_id.")
    if evidence.get("source_manifest") != manifest_path.relative_to(root).as_posix():
        errors.append("Gate 5 foundation evidence identifies the wrong source manifest.")
    if evidence.get("source_manifest_sha256") != file_sha256(manifest_path):
        errors.append("Gate 5 foundation evidence has a stale source-manifest hash.")
    if evidence.get("copy_mode") != "byte_for_byte_read_only":
        errors.append("Gate 5 foundation evidence has the wrong copy mode.")
    recorded = {item.get("destination"): item for item in evidence.get("files", [])}
    for entry in manifest.get("files", []):
        relative = Path(entry["path"])
        source = source_root / relative
        destination = destination_root / relative
        destination_relative = destination.relative_to(root).as_posix()
        if not destination.exists():
            errors.append(f"Gate 5 foundation copy is missing: {destination_relative}")
            continue
        if file_sha256(destination) != file_sha256(source):
            errors.append(f"Gate 5 foundation copy was modified: {destination_relative}")
        item = recorded.get(destination_relative)
        if item is None or item.get("sha256") != entry["sha256"]:
            errors.append(f"Gate 5 foundation provenance is missing/mismatched: {destination_relative}")
    expected_non_meta = {Path(entry["path"]).as_posix() for entry in manifest.get("files", [])}
    actual_non_meta = {
        path.relative_to(destination_root).as_posix()
        for path in destination_root.rglob("*")
        if path.is_file() and path.suffix.lower() != ".meta"
    } if destination_root.exists() else set()
    for extra in sorted(actual_non_meta - expected_non_meta):
        errors.append(f"Gate 5 foundation copy contains an unlisted file: {extra}")
    return errors


def approved_asset_import_errors(root: Path, lab_id: str) -> list[str]:
    errors: list[str] = []
    manifest_root = root / "BuildEvidence" / lab_id / "approved-asset-imports"
    destination_root = (
        root / "UnityProject" / "Assets" / "Generated" / lab_id / "ApprovedDependencies"
    ).resolve()
    approved_root = (root / "ApprovedAssets").resolve()
    required_assets = set(mandatory_approved_assets(root))
    manifests = sorted(manifest_root.glob("*.json")) if manifest_root.exists() else []
    if not manifests:
        return ["Mandatory approved-asset import evidence is missing."]

    found_required_assets: set[str] = set()
    expected_destinations: set[Path] = set()
    for manifest_path in manifests:
        try:
            manifest = read_json(manifest_path)
        except RuntimeError as error:
            errors.append(str(error))
            continue
        if manifest.get("lab_id") != lab_id:
            errors.append(f"Approved-asset import has the wrong lab_id: {manifest_path.name}")
        if manifest.get("dry_run") is not False:
            errors.append(f"Approved-asset import is not a real copy: {manifest_path.name}")
        approved_source = str(manifest.get("approved_source", ""))
        if approved_source in required_assets:
            found_required_assets.add(approved_source)
        assets = manifest.get("assets", [])
        if manifest.get("asset_count") != len(assets):
            errors.append(f"Approved-asset import count is inconsistent: {manifest_path.name}")
        for record in assets:
            source = (root / record.get("source", "")).resolve()
            destination = (root / record.get("destination", "")).resolve()
            try:
                source.relative_to(approved_root)
                destination.relative_to(destination_root)
            except ValueError:
                errors.append(f"Approved-asset provenance escapes allowed roots: {manifest_path.name}")
                continue
            source_meta = Path(str(source) + ".meta")
            destination_meta = Path(str(destination) + ".meta")
            for path, expected, label in (
                (source, record.get("sha256"), "source"),
                (destination, record.get("sha256"), "copy"),
                (source_meta, record.get("meta_sha256"), "source meta"),
                (destination_meta, record.get("meta_sha256"), "copy meta"),
            ):
                if not fs_is_file(path):
                    errors.append(f"Approved-asset {label} is missing: {path}")
                elif not expected or file_sha256(path) != expected:
                    errors.append(f"Approved-asset {label} hash mismatch: {path}")
            expected_destinations.add(destination)

    for missing in sorted(required_assets - found_required_assets):
        errors.append(f"Mandatory approved asset was not imported: {missing}")
    if fs_exists(destination_root):
        for path in iter_files(destination_root):
            if path.suffix.lower() != ".meta" and Path(os.path.abspath(path)) not in expected_destinations:
                errors.append(
                    "ApprovedDependencies contains an untracked or modified extra file: "
                    + path.relative_to(destination_root).as_posix()
                )
    return errors


def require_current_state_schema(state: dict[str, Any]) -> None:
    actual = state.get("schema_version", "missing")
    if actual != CURRENT_STATE_SCHEMA:
        raise RuntimeError(
            f"Workflow state schema {actual} is not current ({CURRENT_STATE_SCHEMA}). "
            "Run `labflow migrate-state --lab-id <lab_id>` and re-approve the gates; "
            "old approvals cannot satisfy new workflow checks."
        )


def gate5_lab_output_digest(root: Path, lab_id: str) -> str:
    included: list[Path] = []
    lab_spec = root / "LabSpecs" / lab_id
    if lab_spec.exists():
        included.extend(
            path for path in lab_spec.rglob("*")
            if path.is_file() and path.name != "workflow_state.json"
        )
    contracts = root / "LabInterfaceContracts"
    if contracts.exists():
        included.extend(contracts.glob(f"{lab_id}_*.json"))
    unity_assets = root / "UnityProject" / "Assets"
    excluded_roots = (
        unity_assets / "Generated" / lab_id / "Gate5Foundation",
    )
    if fs_exists(unity_assets):
        for path in iter_files(unity_assets):
            if path.suffix.lower() == ".meta":
                continue
            resolved = Path(os.path.abspath(path))
            if any(Path(os.path.abspath(excluded)) in resolved.parents for excluded in excluded_roots):
                continue
            included.append(path)
    lines = [
        f"{path.relative_to(root).as_posix()}\t{file_sha256(path)}"
        for path in sorted(set(included))
    ]
    digest = hashlib.sha256()
    digest.update("\n".join(lines).encode("utf-8"))
    return digest.hexdigest().upper()


def gate5_evidence_freshness_errors(root: Path, state: dict[str, Any]) -> list[str]:
    evidence = ensure_evidence(state)
    actual = gate5_lab_output_digest(root, state["lab_id"])
    errors: list[str] = []
    if evidence.get("gate5_foundation") == "passed":
        recorded = evidence.get("gate5_foundation_lab_output_digest")
        if not recorded or recorded != actual:
            errors.append(
                "Gate 5 reusable-foundation evidence is stale after current-Lab output changed; "
                "rerun the 3 foundation tests and neutral scene check, then record it again."
            )
    if (
        evidence.get("domain_slice") == "passed"
        or evidence.get("domain_compile") == "passed"
    ):
        recorded = evidence.get("gate5_domain_lab_output_digest")
        if not recorded or recorded != actual:
            errors.append(
                "Gate 5 domain evidence is stale after current-Lab output changed; "
                "rerun the domain assertion and clean-Console compile check, then record both again."
            )
    return errors


def require_gate5_evidence_order(
    state: dict[str, Any],
    gate5_foundation: str | None,
    domain_slice: str | None,
    domain_compile: str | None,
) -> None:
    domain_requested = domain_slice is not None or domain_compile is not None
    if gate5_foundation is not None and domain_requested:
        raise RuntimeError(
            "Record Gate 5 foundation evidence separately before domain evidence."
        )
    if domain_requested and ensure_evidence(state).get("gate5_foundation") != "passed":
        raise RuntimeError(
            "Gate 5 foundation has not passed; domain evidence cannot be recorded yet."
        )


def state_path(root: Path, lab_id: str) -> Path:
    return root / "LabSpecs" / lab_id / "workflow_state.json"


def load_state(root: Path, lab_id: str) -> dict[str, Any]:
    path = state_path(root, lab_id)
    if not path.exists():
        raise RuntimeError(f"No workflow exists for {lab_id}. Run `new` first.")
    state = read_json(path)
    if state.get("lab_id") != lab_id:
        raise RuntimeError(f"Workflow state lab_id does not match its directory: {path}")
    return state


def load_current_state(root: Path, lab_id: str) -> dict[str, Any]:
    state = load_state(root, lab_id)
    require_current_state_schema(state)
    validate_workspace_mode(
        root, lab_id, str(state.get("workflow_mode", "blind-rebuild"))
    )
    return state


def save_state(root: Path, lab_id: str, state: dict[str, Any]) -> None:
    state["schema_version"] = CURRENT_STATE_SCHEMA
    state["updated_at"] = utc_now()
    write_json(state_path(root, lab_id), state, force=True)


def gate_is_approved(state: dict[str, Any], gate: int) -> bool:
    approval = state.get("gate_approvals", {}).get(str(gate), {})
    return bool(approval.get("approved", False))


def require_gate_approved(state: dict[str, Any], gate: int) -> None:
    if not gate_is_approved(state, gate):
        raise RuntimeError(
            f"Gate {gate} ({GATE_NAMES[gate]}) has not been approved by the user. "
            "Complete it, obtain a clear approval or progression response, and record "
            "the user's original wording before continuing."
        )


def record_gate_approval(state: dict[str, Any], gate: int, note: str) -> None:
    state.setdefault("gate_approvals", {})[str(gate)] = {
        "approved": True,
        "note": note,
        "approved_at": utc_now(),
    }


def current_gate(state: dict[str, Any]) -> int:
    for gate in range(11):
        if not gate_is_approved(state, gate):
            return gate
    return 10


def contract_candidates(root: Path, lab_id: str, kind: str) -> tuple[Path, Path]:
    base = root / "LabInterfaceContracts"
    return base / f"{lab_id}_{kind}.json", base / f"{lab_id}_{kind}.example.json"


def find_contract(root: Path, lab_id: str, kind: str) -> Path:
    canonical, _example = contract_candidates(root, lab_id, kind)
    if canonical.exists():
        return canonical
    raise RuntimeError(f"Missing canonical {kind} contract: expected {canonical}")


def validate_text_argument(value: str, label: str) -> str:
    if not value.strip():
        raise RuntimeError(f"{label} must not be empty")
    if any(character in value for character in ("\r", "\n", "\0")):
        raise RuntimeError(f"{label} must be one line without control characters")
    return value.strip()


def validate_scene_path(scene: str) -> str:
    scene = scene.replace("\\", "/")
    if (
        not scene.startswith("Assets/Scenes/")
        or not scene.endswith(".unity")
        or ".." in Path(scene).parts
        or any(character in scene for character in ('"', "'", "\r", "\n", "\0"))
    ):
        raise RuntimeError(
            "scene must be a safe Unity asset path under Assets/Scenes/ ending in .unity"
        )
    return scene


def new_run_contamination_errors(root: Path) -> list[str]:
    errors: list[str] = []
    state_files = sorted((root / "LabSpecs").glob("*/workflow_state.json"))
    if state_files:
        errors.append(
            "An active workflow already exists: "
            + ", ".join(path.parent.name for path in state_files)
        )
    canonical_contracts = sorted(
        path for path in (root / "LabInterfaceContracts").glob("*.json")
        if not path.name.endswith(".example.json")
    )
    if canonical_contracts:
        errors.append("Canonical contracts from an earlier run remain in LabInterfaceContracts")
    lab_spec_outputs = sorted(
        path for path in (root / "LabSpecs").iterdir()
        if path.is_dir() and path.name != "templates"
    ) if (root / "LabSpecs").exists() else []
    if lab_spec_outputs:
        errors.append("LabSpecs contains an earlier run directory")
    unity_outputs = []
    for relative in (
        "UnityProject/Assets/Scripts/EMVR/Labs",
        "UnityProject/Assets/Editor",
        "UnityProject/Assets/Tests",
        "UnityProject/Assets/Scenes",
        "UnityProject/Assets/Generated",
    ):
        path = root / relative
        if path.exists() and any(child.is_file() for child in path.rglob("*")):
            unity_outputs.append(relative)
    if unity_outputs:
        errors.append("Unity output from an earlier run remains: " + ", ".join(unity_outputs))
    evidence_root = root / "BuildEvidence"
    if evidence_root.exists():
        prior_evidence = [
            path for path in evidence_root.iterdir()
            if path.name != "EvalTemplates"
        ]
        if prior_evidence:
            errors.append("BuildEvidence contains output from an earlier run")
    return errors


def default_contracts(lab_id: str, title: str, domain: str, scene: str) -> dict[str, dict[str, Any]]:
    object_id = f"{lab_id}_experiment_root"
    return {
        "lab_spec": {
            "schema_version": "0.1.0",
            "lab_id": lab_id,
            "title": title,
            "domain": domain,
            "unity_scene": scene,
            "entry_step_id": "S1",
            "learning_goals": ["Replace with an observable learning goal before implementation."],
            "assist_contract": {
                "required_grounding_fields": [
                    "session_id", "user_id", "lab_id", "step_id", "mode",
                    "student_input_text", "selected_object_json", "objects_json",
                    "recent_actions_json", "diagnostic_hints_json",
                    "trigger_reason_json", "step_rubric_json", "session_memory_json"
                ],
                "gateway_route": "/v1/assist",
                "response_root": "unity_output"
            },
            "telemetry_profile": "research_mvp",
            "notes": "Generated by labflow. Replace placeholders before design approval."
        },
        "objects": {
            "schema_version": "0.1.0",
            "lab_id": lab_id,
            "objects": [{
                "object_id": object_id,
                "display_name": "Experiment root",
                "object_type": "ExperimentRoot",
                "unity_name_hint": "Generated",
                "domain_properties": {},
                "telemetry_roles": ["observable"]
            }]
        },
        "steps": {
            "schema_version": "0.1.0",
            "lab_id": lab_id,
            "steps": [{
                "step_id": "S1",
                "title": "Complete the core experiment task",
                "student_goal": "Replace with the first observable student task.",
                "expected_action": "observe_or_interact",
                "expected_object_ids": [object_id],
                "success_criteria": ["Replace with an observable completion condition."],
                "assist_triggers": ["idle_on_step", "repeated_invalid_action"]
            }]
        },
        "rubric": {
            "schema_version": "0.1.0",
            "lab_id": lab_id,
            "rubrics": [{
                "step_id": "S1",
                "criteria": [{
                    "criterion_id": "S1_C1",
                    "concept": "replace_with_core_concept",
                    "mastery_indicator": "Replace with observable evidence of mastery.",
                    "common_misconceptions": [],
                    "diagnostic_hint_keys": ["core_task_incomplete"]
                }]
            }]
        },
        "telemetry_map": {
            "schema_version": "0.1.0",
            "lab_id": lab_id,
            "required_context_fields": ["session_id", "user_id", "lab_id", "step_id", "timestamp_ms"],
            "event_types": [
                {"event_type": "session_start", "required_fields": ["session_id", "user_id", "lab_id"]},
                {"event_type": "step_start", "required_fields": ["session_id", "step_id"]},
                {"event_type": "object_selected", "required_fields": ["session_id", "step_id", "object_id", "object_type"]},
                {"event_type": "assist_requested", "required_fields": ["session_id", "step_id", "mode", "trigger_reason_json"]},
                {"event_type": "assist_response_shown", "required_fields": ["session_id", "step_id", "trace_id", "intervention_type"]},
                {"event_type": "outcome_recorded", "required_fields": ["session_id", "step_id", "trace_id", "resolved", "status"]}
            ],
            "export_notes": "Keep event names stable across rebuilds."
        }
    }


def brief_text(
    lab_id: str, title: str, domain: str, scene: str, workflow_mode: str = "blind-rebuild"
) -> str:
    title_yaml = json.dumps(title, ensure_ascii=False)
    domain_yaml = json.dumps(domain, ensure_ascii=False)
    scene_yaml = json.dumps(scene, ensure_ascii=False)
    return f'''schema_version: "1.0.0"
lab_id: "{lab_id}"
title: {title_yaml}
domain: {domain_yaml}
workflow_mode: "{workflow_mode}"
status: "draft"

source_material:
  handbook: ""
  additional_notes: []

learning_goals:
  - "Replace with an observable learning goal."

student_tasks:
  - step_id: "S1"
    goal: "Replace with the first student task."
    expected_action: "observe_or_interact"
    success_criteria:
      - "Replace with an observable completion condition."

physics:
  formulas: []
  units: []
  parameter_ranges: []
  assumptions: []
  expected_results: []

objects:
  - object_id: "{lab_id}_experiment_root"
    display_name: "Experiment root"
    object_type: "ExperimentRoot"
    role: "calculation-region"
    desktop_interaction: "none"
    xr_interaction: "equivalent_to_desktop_via_common_xri_mapping"
    visible_feedback: "status display shows ready"

presets: []

interaction_modes:
  desktop_mouse: true
  xr_device_simulator: true
  real_vr: true
  desktop_to_xr_mapping:
    policy: "equivalent_by_default"
    semantics: "same target, state transition, visible feedback, and success result"
    implementation: "Common desktop pointer/drag plus approved XRI Select/Trigger/Grab adapters"
    user_input_required: false
    exceptions: []

environment:
  room_shell_strategy: "mandatory_approved_prefab"
  room_shell_source: "ApprovedAssets/EMVRRoom/Prefabs/Room_Big_Part_01.prefab"
  copy_demo_scene: false
  allow_generated_primitive_room: false
  room_placement_and_adaptation: "replace_with_confirmed_placement_and_non_destructive_adaptation"
  visual_style_reference: "replace_with_confirmed_visual_style"
  lighting_requirement: "replace_with_confirmed_lighting"
  camera_and_ui_safe_area: "replace_with_confirmed_game_view_layout"
  world_space_ui_front_face: "Use the actual Play Mode camera; for Unity Canvas/TextMesh require positive dot(-Transform.forward, normalized direction from surface to camera)."

initial_and_action_states:
  authored_initial_state: "replace_with_exact_visible_initial_state"
  hidden_templates_or_loaders: "replace_with_none_or_exact_hidden_items"
  first_required_action: "replace_with_first_user_action"
  expected_visible_after_action: "replace_with_exact_post_action_state"

reuse_requirements:
  mandatory_common_baseline: true
  capability_triggered_common_reuse: true
  reuse_audit_required_in_design: true

scene:
  output_scene: {scene_yaml}
  creation_mode: "new_scene"
  approved_asset_sources: []
  language: "English"
  required_visible_regions: ["instruction", "experiment", "status", "result"]

acceptance:
  core_flow:
    - "Scene is generated from an Editor menu."
    - "There are no blocking compile errors."
    - "Desktop input completes the core task."
    - "Important actions produce visible feedback."
  required_evidence: ["Game View initial state", "Game View completed state", "Console after Clear"]

workflow_limits:
  run_batchmode: false
  hand_edit_scene_yaml: false
'''


def design_text(lab_id: str, scene: str) -> str:
    return f'''# {lab_id} design review

Approval status: NOT APPROVED

## Learning flow

Describe the shortest student path from instruction to observable result.

## Mandatory Common reuse audit

List every universal and capability-triggered row from the mandatory reuse
matrix, its exact scene wiring, and planned evidence.
Canonical source: `UnityProject/LocalPackages/com.emvr.lab-common/`.

## API signature audit

For every Common, Unity, and XRI API that Lab-specific code will call, record
the exact source path or package version, type/member signature, assembly, and
the planned call site. Do not infer an API from its name or from memory.

## Prior rebuild lessons applied

List every lesson file discovered under `KnowledgeBase/rebuild-lessons/`,
including the recommended `build-experience/` files and excluding files named
`README.md`. Mark it applied or not relevant, explain why, and name the
preventive check added to this design for each applicable verified lesson.
Do not inspect `KnowledgeBase/legacy-lab-specific-summaries/`; it contains
restricted old-Lab implementation details and answers.

## Environment decision

- Output scene: `{scene}`
- Mandatory room Prefab: `ApprovedAssets/EMVRRoom/Prefabs/Room_Big_Part_01.prefab`
- Generated import path: `Assets/Generated/{lab_id}/ApprovedDependencies/EMVRRoom/Prefabs/Room_Big_Part_01.prefab`
- Import manifest path under `BuildEvidence/{lab_id}/approved-asset-imports/`:
- Prefab placement and non-destructive adaptation:
- Room floor/wall/roof validation before scene save:
- Confirm that `DemoScene.unity` is not copied and primitive walls are not substituted:
- Visual continuity, lighting, camera, and UI safe areas:
- Explain why `new_scene` does not imply a generated primitive room:

## Scene plan

- Approved reusable assets/components to import:
- XR rig/camera/lighting elements to instantiate or generate:
- Generated root and lab-specific objects to add:

## Initial and post-action visibility

- Authored initial visible state:
- Hidden templates/loaders and why:
- First required user action:
- Exact visible state after that action:

## Interaction plan

- Desktop mouse path:
- XR Device Simulator path:
- Desktop/XR default equivalence: same target, state transition, visible feedback, and success result through Common/XRI adapters
- Confirmed mapping exceptions: none unless Gate 1 records one
- Real VR path:
- Visible feedback after each important action:

## Domain and presets

- Formula and units:
- Valid parameter ranges:
- Presets and expected results:

## Acceptance evidence

- Initial Game View:
- Completed Game View:
- Console after Clear:
- Known risks:

## Builder failure policy

- Native-backed Unity objects initialize in `Awake`, `OnEnable`, or lazily.
- Capture exceptions/errors and abort before saving a partial scene.
- Emit success only after validation and successful save.

## Gate 5 reusable foundation plan

- Protected source: `ReusableFoundation/Gate5MinimalSlice/`
- Immutable generated copy: `Assets/Generated/{lab_id}/Gate5Foundation/`
- Foundation test assembly: `EMVR.ReusableFoundation.Gate5.Tests` (3 EditMode tests)
- Common components called by the Lab beside this copy:
- Neutral button/spawn/visibility scene check and expected evidence:
- Domain code remains separate; protected source/copy edits: forbidden

## Game View layout

- Required readable regions and minimum usable control size:
- Camera framing and unobstructed experiment area:
- Runtime camera and world-space UI front-face validation:
- Initial/action/completed screenshots to review:
'''


def prior_lessons_review_relative_path(lab_id: str) -> Path:
    return Path("LabSpecs") / lab_id / "prior-lessons-review.md"


def prior_lessons_review_text(lab_id: str) -> str:
    return f'''# {lab_id} prior rebuild-lessons review

Review status: TODO

## Reviewed lesson files

TODO: List every Markdown file under `KnowledgeBase/rebuild-lessons/`, excluding
files named `README.md`. Include the recommended `build-experience/` lessons.
Do not read `KnowledgeBase/legacy-lab-specific-summaries/`. If no safe lesson
exists, write `none available`.

## Applicable safeguards

TODO: Record the verified prevention checks that apply to this Lab and the Gate
where each will be enforced. Use `none` only when justified.

## Not applicable or deferred lessons

TODO: Name the reviewed lessons that do not apply and explain why. Do not turn
another Lab's formulas, answers, object IDs, or rubric rules into requirements.

## Restricted archive declaration

Restricted archive accessed: no
'''


def prior_lessons_review_errors(root: Path, lab_id: str) -> list[str]:
    report = root / prior_lessons_review_relative_path(lab_id)
    if not report.exists():
        return [f"Prior rebuild-lessons review is missing: {report}"]
    content = report.read_text(encoding="utf-8-sig")
    lowered = content.lower()
    errors: list[str] = []
    if len(content.strip()) < 250:
        errors.append("Prior rebuild-lessons review is too short.")
    if "TODO" in content or "Review status: COMPLETE" not in content:
        errors.append("Prior rebuild-lessons review is incomplete.")
    for heading in (
        "## Reviewed lesson files",
        "## Applicable safeguards",
        "## Not applicable or deferred lessons",
        "## Restricted archive declaration",
    ):
        if heading not in content:
            errors.append(f"Prior rebuild-lessons review is missing `{heading}`.")
    if "Restricted archive accessed: no" not in content:
        errors.append("Prior rebuild-lessons review must declare that the restricted archive was not accessed.")

    lessons_root = root / "KnowledgeBase" / "rebuild-lessons"
    lesson_files = [] if not lessons_root.exists() else sorted(
        path for path in lessons_root.rglob("*.md") if path.name.lower() != "readme.md"
    )
    if lesson_files:
        for lesson in lesson_files:
            relative = lesson.relative_to(root).as_posix()
            if relative not in content:
                errors.append(f"Prior rebuild lesson was not acknowledged: {relative}")
    elif "none available" not in lowered:
        errors.append("No prior lessons exist, but the review does not state `none available`.")
    return errors


def retrospective_relative_paths(lab_id: str) -> tuple[Path, Path]:
    return (
        Path("BuildEvidence") / "retrospectives" / f"{lab_id}.md",
        Path("KnowledgeBase") / "rebuild-lessons" / f"{lab_id}.md",
    )


def evidence_relative_paths(lab_id: str) -> tuple[Path, Path]:
    base = Path("BuildEvidence") / lab_id
    return base / "reuse-report.md", base / "validation-report.md"


def reuse_report_text(lab_id: str) -> str:
    return f'''# Reuse report

Lab ID: `{lab_id}`

Status: initialized; update provenance during each Gate.

## Mandatory Common and room reuse

Record each mandatory responsibility, exact source, destination/wiring, changes,
and observed validation evidence.

## Approved asset imports

Record exact approved source paths, current-Lab destination paths, dependency
closure, hashes or copy evidence, and Unity import validation.

## Conditional reuse decisions

Record each Category B item inspected, the generic responsibility extracted,
decoupling performed, and why it does not contain target answers or old wiring.

## Gold boundary declaration

- [ ] No Gold scene, implementation source, GUID wiring, restricted legacy
  summary, or legacy example contract was read.
- [ ] Missing requirements came only from authorized inputs and user confirmation.
'''


def validation_report_text(lab_id: str) -> str:
    return f'''# Validation report

Lab ID: `{lab_id}`

| Gate/check | Result | Observed evidence |
|---|---|---|
| Gate 0 boundary and prior lessons | pending | |
| Gate 2 contracts | pending | |
| Gate 4 scaffold compile | pending | |
| Gate 5 domain slice and compile | pending | |
| Gate 6 scene build/Common validation/Game View | pending | |
| Gate 7 desktop/XR/lifecycle | pending | |
| Gate 8 evidence package | pending | |
| Gate 9 user acceptance | pending | |

Acceptance remains a user/evaluator decision. Do not mark a Unity-owned result
passed without observed Console, Game View, interaction, or hardware evidence.
'''


def retrospective_text(lab_id: str, title: str) -> str:
    return f'''# {lab_id} rebuild error retrospective

Lab title: {title}
Review status: TODO

## Build outcome

TODO: Summarize the accepted Lab and the final compile/scene/playtest evidence.

## Error timeline

TODO: List errors and warnings in observed order, starting with each earliest
root error. If no errors occurred, state that explicitly and list the checks
that kept the build clean.

## Verified root causes

TODO: For each verified issue, record the error signature, owning layer,
evidence, root cause, and why later messages were or were not cascades.

## Fixes and verification

TODO: Describe the allowed fix location and the observed evidence that proved
the fix. Do not paste large logs or old Lab implementation code.

## False leads and rejected workarounds

TODO: Record misleading symptoms and unsafe workarounds that were avoided.

## Prevention checks by gate

TODO: Map each verified issue to the earliest Gate/check that should prevent it
in a future rebuild.

## Foundation issues reported to the user

TODO: List protected-foundation issues without writing repair code. Use `none`
when no foundation issue occurred.

## Deferred or unresolved items

TODO: Record real-hardware deferrals or remaining risks. Use `none` when empty.
'''


def reusable_lesson_text(lab_id: str, title: str) -> str:
    return f'''---
lab_id: {lab_id}
lab_title: "{title.replace('"', "'")}"
knowledge_kind: rebuild_lessons
verified: true
unity_version: TODO
common_version: TODO
components: [TODO]
error_signatures: [TODO]
---

# Reusable lessons from {lab_id}

## Verified reusable failure patterns

TODO: Normalize only verified patterns that can recur in another Lab. If the
build was clean, state `none` and preserve the safeguards that worked.

## Ownership and allowed fix location

TODO: Distinguish current-Lab output from protected workflow foundation.

## Prevention checks by gate

TODO: Give concrete checks for Gate 0 through Gate 7 as applicable.

## API and lifecycle constraints

TODO: Record verified signatures, package/version constraints, Unity lifecycle
rules, or asmdef boundaries that a future Agent must check.

## Search cues for future rebuilds

TODO: List concise component names, error fragments, and conditions that make
this lesson relevant.

## Exclusions

Do not include target formulas, expected answers, rubric logic, Gold Reference
details, secrets, personal paths, or unverified guesses.
'''


def retrospective_readiness_errors(root: Path, state: dict[str, Any]) -> list[str]:
    lab_id = state["lab_id"]
    default_run, default_lesson = retrospective_relative_paths(lab_id)
    recorded = state.get("retrospective_paths", {})
    errors: list[str] = []
    if recorded.get("run") not in (None, default_run.as_posix()):
        errors.append("Gate 10 run retrospective path differs from the designated location.")
    if recorded.get("lesson") not in (None, default_lesson.as_posix()):
        errors.append("Gate 10 reusable-lesson path differs from the designated location.")
    run_path = root / default_run
    lesson_path = root / default_lesson

    requirements = (
        (
            run_path,
            600,
            (
                "## Build outcome",
                "## Error timeline",
                "## Verified root causes",
                "## Fixes and verification",
                "## Prevention checks by gate",
                "## Deferred or unresolved items",
            ),
        ),
        (
            lesson_path,
            400,
            (
                "knowledge_kind: rebuild_lessons",
                "verified: true",
                "## Verified reusable failure patterns",
                "## Ownership and allowed fix location",
                "## Prevention checks by gate",
                "## API and lifecycle constraints",
                "## Search cues for future rebuilds",
            ),
        ),
    )
    for path, minimum_length, required_markers in requirements:
        if not path.exists():
            errors.append(f"Gate 10 retrospective file is missing: {path}")
            continue
        content = path.read_text(encoding="utf-8-sig")
        if len(content.strip()) < minimum_length:
            errors.append(f"Gate 10 retrospective file is too short: {path}")
        if "TODO" in content or "Replace with" in content:
            errors.append(f"Gate 10 retrospective still contains placeholders: {path}")
        for marker in required_markers:
            if marker not in content:
                errors.append(f"Gate 10 retrospective is missing `{marker}`: {path}")
    return errors


def command_new(args: argparse.Namespace) -> int:
    root = args.project_root.resolve()
    require_foundation(root)
    lab_id = require_lab_id(args.lab_id)
    workflow_mode = args.mode
    validate_workspace_mode(root, lab_id, workflow_mode)
    title = validate_text_argument(args.title, "title")
    domain = validate_text_argument(args.domain, "domain")
    scene = validate_scene_path(args.scene)
    contamination = new_run_contamination_errors(root)
    if contamination:
        raise RuntimeError(
            "A new workflow run requires a pristine Builder Pack. Resume the single active run "
            "or start from a clean foundation copy; do not expose one run to another.\n- "
            + "\n- ".join(contamination)
        )
    lab_dir = root / "LabSpecs" / lab_id
    files = [
        lab_dir / "brief.yaml",
        lab_dir / "design.md",
        root / prior_lessons_review_relative_path(lab_id),
        state_path(root, lab_id),
    ]
    files.extend(contract_candidates(root, lab_id, kind)[0] for kind in CONTRACT_KINDS)
    files.extend(root / relative for relative in evidence_relative_paths(lab_id))
    existing = [path for path in files if path.exists()]
    if existing:
        raise RuntimeError("Workflow already contains run files; refusing to overwrite:\n" + "\n".join(map(str, existing)))

    write_text(
        lab_dir / "brief.yaml",
        brief_text(lab_id, title, domain, scene, workflow_mode),
    )
    write_text(lab_dir / "design.md", design_text(lab_id, scene))
    write_text(
        root / prior_lessons_review_relative_path(lab_id),
        prior_lessons_review_text(lab_id),
    )
    for kind, payload in default_contracts(lab_id, title, domain, scene).items():
        write_json(contract_candidates(root, lab_id, kind)[0], payload)
    reuse_relative, validation_relative = evidence_relative_paths(lab_id)
    write_text(root / reuse_relative, reuse_report_text(lab_id))
    write_text(root / validation_relative, validation_report_text(lab_id))

    now = utc_now()
    state = {
        "schema_version": CURRENT_STATE_SCHEMA,
        "lab_id": lab_id,
        "title": title,
        "domain": domain,
        "workflow_mode": workflow_mode,
        "stage": "gate_0_in_progress",
        "scene": scene,
        "scene_creation_mode": "new_scene",
        "brief_confirmation": {"confirmed": False, "note": "", "confirmed_at": None},
        "design_approval": {"approved": False, "note": "", "approved_at": None},
        "gate_approvals": {},
        "evidence": default_evidence(),
        "protected_baseline": None,
        "created_at": now,
        "updated_at": now,
    }
    write_json(state_path(root, lab_id), state)
    print(f"Created workflow for {lab_id}")
    print(
        "Complete Gate 0 by reviewing every safe prior lesson under "
        "KnowledgeBase/rebuild-lessons, especially build-experience, without reading "
        "KnowledgeBase/legacy-lab-specific-summaries; then finish "
        f"LabSpecs/{lab_id}/prior-lessons-review.md before requesting approval."
    )
    return 0


def duplicate_values(values: list[Any]) -> set[Any]:
    seen: set[Any] = set()
    duplicates: set[Any] = set()
    for value in values:
        if value in seen:
            duplicates.add(value)
        seen.add(value)
    return duplicates


def placeholder_locations(value: Any, prefix: str = "$") -> list[str]:
    """Return JSON-style paths containing generated placeholder text."""
    locations: list[str] = []
    if isinstance(value, dict):
        for key, child in value.items():
            locations.extend(placeholder_locations(child, f"{prefix}.{key}"))
    elif isinstance(value, list):
        for index, child in enumerate(value):
            locations.extend(placeholder_locations(child, f"{prefix}[{index}]"))
    elif isinstance(value, str):
        lowered = value.lower()
        if "replace with" in lowered or "replace_with" in lowered:
            locations.append(prefix)
    return locations


def validate_contracts(root: Path, lab_id: str) -> tuple[list[str], list[str], dict[str, dict[str, Any]]]:
    errors: list[str] = []
    warnings: list[str] = []
    bundle: dict[str, dict[str, Any]] = {}
    for kind in CONTRACT_KINDS:
        try:
            path = find_contract(root, lab_id, kind)
            bundle[kind] = read_json(path)
        except RuntimeError as error:
            errors.append(str(error))

    if len(bundle) != len(CONTRACT_KINDS):
        return errors, warnings, bundle

    for kind, contract in bundle.items():
        if contract.get("lab_id") != lab_id:
            errors.append(f"{kind}: lab_id must be {lab_id!r}")
        for location in placeholder_locations(contract):
            errors.append(f"{kind}: unresolved generated placeholder at {location}")

    brief = root / "LabSpecs" / lab_id / "brief.yaml"
    if brief.exists():
        brief_text_content = brief.read_text(encoding="utf-8-sig").lower()
        if "replace with" in brief_text_content or "replace_with" in brief_text_content:
            errors.append(f"brief: unresolved generated placeholder in {brief}")

    objects = bundle["objects"].get("objects")
    steps = bundle["steps"].get("steps")
    rubrics = bundle["rubric"].get("rubrics")
    events = bundle["telemetry_map"].get("event_types")
    for label, value in (("objects", objects), ("steps", steps), ("rubrics", rubrics), ("event_types", events)):
        if not isinstance(value, list) or not value:
            errors.append(f"{label}: must be a non-empty list")

    if errors:
        return errors, warnings, bundle

    object_ids = [item.get("object_id") for item in objects]
    step_ids = [item.get("step_id") for item in steps]
    if any(not value for value in object_ids):
        errors.append("objects: every object requires object_id")
    if any(not value for value in step_ids):
        errors.append("steps: every step requires step_id")
    for value in sorted(str(x) for x in duplicate_values(object_ids)):
        errors.append(f"objects: duplicate object_id {value}")
    for value in sorted(str(x) for x in duplicate_values(step_ids)):
        errors.append(f"steps: duplicate step_id {value}")

    object_id_set = set(object_ids)
    step_id_set = set(step_ids)
    entry_step = bundle["lab_spec"].get("entry_step_id")
    if entry_step not in step_id_set:
        errors.append(f"lab_spec: entry_step_id {entry_step!r} does not exist")

    rubric_steps: set[Any] = set()
    for rubric in rubrics:
        step_id = rubric.get("step_id")
        rubric_steps.add(step_id)
        if step_id not in step_id_set:
            errors.append(f"rubric: references missing step {step_id!r}")
    for step in steps:
        step_id = step.get("step_id")
        for object_id in step.get("expected_object_ids", []):
            if object_id not in object_id_set:
                errors.append(f"steps: {step_id} references missing object {object_id!r}")
        if step_id not in rubric_steps:
            warnings.append(f"rubric: step {step_id} has no rubric entry")

    event_names = [event.get("event_type") for event in events]
    for required in ("session_start", "step_start", "assist_requested", "outcome_recorded"):
        if required not in event_names:
            warnings.append(f"telemetry_map: recommended event {required!r} is missing")
    return errors, warnings, bundle


def brief_readiness_errors(root: Path, lab_id: str) -> list[str]:
    errors: list[str] = []
    brief = root / "LabSpecs" / lab_id / "brief.yaml"
    if not brief.exists():
        return [f"Brief does not exist: {brief}"]
    content = brief.read_text(encoding="utf-8-sig")
    lowered = content.lower()
    if "replace with" in lowered or "replace_with" in lowered:
        errors.append("Brief still contains generated placeholders.")
    if len(content.strip()) < 200:
        errors.append("Brief is too short.")
    for required_section in (
        "interaction_modes:",
        "environment:",
        "initial_and_action_states:",
        "reuse_requirements:",
    ):
        if required_section not in lowered:
            errors.append(f"Brief is missing required section: {required_section}")
    if "desktop_to_xr_mapping:" not in lowered:
        errors.append("Brief is missing the default desktop-to-XR mapping policy.")
    if not re.search(
        r"policy:\s*[\"']?equivalent_by_default[\"']?", content, flags=re.IGNORECASE
    ):
        errors.append("Brief must preserve `equivalent_by_default` desktop-to-XR mapping.")
    if "exceptions:" not in lowered:
        errors.append("Brief must record desktop-to-XR mapping exceptions, normally an empty list.")
    return errors


def design_readiness_errors(root: Path, lab_id: str) -> list[str]:
    errors: list[str] = []
    design = root / "LabSpecs" / lab_id / "design.md"
    if not design.exists():
        return [f"Design review does not exist: {design}"]
    content = design.read_text(encoding="utf-8")
    if len(content.strip()) < 100:
        errors.append("Design review is too short.")
    if "Approval status: READY FOR APPROVAL" not in content:
        errors.append("design.md must contain `Approval status: READY FOR APPROVAL` before submission.")
    if "TODO" in content or "replace_with" in content.lower() or "replace with" in content.lower():
        errors.append("design.md still contains unresolved placeholders.")
    for heading in (
        "## Mandatory Common reuse audit",
        "## API signature audit",
        "## Prior rebuild lessons applied",
        "## Environment decision",
        "## Initial and post-action visibility",
        "## Builder failure policy",
        "## Game View layout",
        "## Gate 5 reusable foundation plan",
    ):
        if heading not in content:
            errors.append(f"design.md is missing required section: {heading}")
    required_room = "ApprovedAssets/EMVRRoom/Prefabs/Room_Big_Part_01.prefab"
    if required_room not in content:
        errors.append("design.md does not include the mandatory room Prefab path")
    if "UnityProject/LocalPackages/com.emvr.lab-common" not in content:
        errors.append("design.md does not identify the canonical Common package path")
    if "ReusableFoundation/Gate5MinimalSlice" not in content:
        errors.append("design.md does not identify the protected Gate 5 reusable foundation")
    if f"Assets/Generated/{lab_id}/Gate5Foundation/" not in content:
        errors.append("design.md does not identify the immutable Gate 5 foundation copy path")
    expected_import_root = f"Assets/Generated/{lab_id}/ApprovedDependencies/"
    if expected_import_root not in content:
        errors.append(f"design.md does not identify the current-Lab approved import root: {expected_import_root}")
    required_values = (
        "- Prefab placement and non-destructive adaptation:",
        "- Authored initial visible state:",
        "- First required user action:",
        "- Exact visible state after that action:",
        "- Desktop mouse path:",
        "- XR Device Simulator path:",
        "- Desktop/XR default equivalence:",
        "- Confirmed mapping exceptions:",
        "- Camera framing and unobstructed experiment area:",
        "- Room floor/wall/roof validation before scene save:",
        "- Runtime camera and world-space UI front-face validation:",
    )
    for prefix in required_values:
        matching = next((line for line in content.splitlines() if line.startswith(prefix)), None)
        if matching is None or not matching[len(prefix):].strip():
            errors.append(f"design.md requires a non-empty value for `{prefix}`")
    return errors


def gate_readiness_errors(root: Path, state: dict[str, Any], gate: int) -> list[str]:
    lab_id = state["lab_id"]
    baseline_errors = protected_baseline_errors(root, state)
    if gate == 0:
        errors = foundation_errors(root)
        errors.extend(prior_lessons_review_errors(root, lab_id))
        if ensure_evidence(state).get("boundary_verifier") != "passed":
            errors.append("Builder Boundary verifier has not been recorded as passed.")
        errors.extend(baseline_errors)
        return errors
    if baseline_errors:
        return baseline_errors
    if gate == 1:
        return brief_readiness_errors(root, lab_id)
    if gate == 2:
        errors, _warnings, _bundle = validate_contracts(root, lab_id)
        if not state.get("last_contract_validation", {}).get("ok", False):
            errors.append("Contracts have not been recorded as validated.")
        return errors
    if gate == 3:
        errors, _warnings, _bundle = validate_contracts(root, lab_id)
        errors.extend(design_readiness_errors(root, lab_id))
        return errors
    if gate == 4:
        generated = state.get("generated_files", [])
        missing = [relative for relative in generated if not (root / relative).exists()]
        if not generated:
            missing.append("Scaffold has not been generated.")
        evidence = ensure_evidence(state)
        if evidence.get("scaffold_compile") != "passed":
            missing.append("Unity scaffold compile smoke test has not passed with a clean Console.")
        return missing
    evidence = ensure_evidence(state)
    if gate == 5:
        missing = gate5_materialization_errors(root, lab_id)
        missing.extend(approved_asset_import_errors(root, lab_id))
        missing.extend(gate5_evidence_freshness_errors(root, state))
        if evidence.get("gate5_foundation") != "passed":
            missing.append(
                "Gate 5 reusable-foundation tests and neutral scene check have not passed."
            )
        if evidence.get("domain_slice") != "passed":
            missing.append("Domain-slice evidence has not passed.")
        if evidence.get("domain_compile") != "passed":
            missing.append("Unity domain-slice compile evidence has not passed with a clean Console.")
        return missing
    if gate == 6:
        missing = []
        if evidence.get("compile") != "passed":
            missing.append("Unity compile evidence has not passed.")
        if evidence.get("scene_built") != "passed":
            missing.append("Unity scene-build/validation evidence has not passed.")
        return missing
    if gate == 7:
        return [] if evidence.get("playtest") == "passed" else ["Interaction/lifecycle playtest has not passed."]
    if gate == 8:
        missing = []
        for prior in range(8):
            if not gate_is_approved(state, prior):
                missing.append(f"Gate {prior} is not approved.")
        for relative in evidence_relative_paths(lab_id):
            path = root / relative
            if not path.exists():
                missing.append(f"Acceptance evidence is missing: {relative.as_posix()}")
                continue
            content = path.read_text(encoding="utf-8-sig")
            if lab_id not in content:
                missing.append(f"Acceptance evidence does not identify {lab_id}: {relative.as_posix()}")
            if "pending" in content.lower() or "TODO" in content:
                missing.append(f"Acceptance evidence is incomplete: {relative.as_posix()}")
        return missing
    if gate == 9:
        return []
    if gate == 10:
        return retrospective_readiness_errors(root, state)
    return [f"Unsupported gate: {gate}"]


def command_validate(args: argparse.Namespace) -> int:
    root = args.project_root.resolve()
    lab_id = require_lab_id(args.lab_id)
    state = load_current_state(root, lab_id)
    require_gate_approved(state, 1)
    if current_gate(state) != 2:
        raise RuntimeError(f"Contract validation belongs to Gate 2, but current gate is {current_gate(state)}.")
    errors, warnings, _bundle = validate_contracts(root, lab_id)
    for warning in warnings:
        print(f"WARNING: {warning}")
    for error in errors:
        print(f"ERROR: {error}")
    if errors:
        print(f"Contract validation FAILED for {lab_id}: {len(errors)} error(s), {len(warnings)} warning(s).")
        return 1
    path = state_path(root, lab_id)
    if path.exists():
        if not state.get("brief_confirmation", {}).get("confirmed", False):
            print("ERROR: Brief has not been confirmed or semantically approved by the user.")
            print(f"Run: python Tools/labflow/labflow.py approve-gate --lab-id {lab_id} --gate 1 --note \"<user confirmation>\"")
            return 1
        state["stage"] = "contracts_ready"
        state["last_contract_validation"] = {"ok": True, "warnings": warnings, "validated_at": utc_now()}
        save_state(root, lab_id, state)
    print(f"Contract validation PASSED for {lab_id}: 0 errors, {len(warnings)} warning(s).")
    return 0


def pascal_name(lab_id: str) -> str:
    return "".join(part[:1].upper() + part[1:] for part in lab_id.split("_") if part)


def runtime_source(namespace_name: str, class_name: str, lab_id: str) -> str:
    return f'''using UnityEngine;

namespace EMVR.Labs.{namespace_name}
{{
    /// <summary>Compile-safe entry point. Add domain behaviour behind this boundary.</summary>
    [DisallowMultipleComponent]
    public sealed class {class_name}Controller : MonoBehaviour
    {{
        public const string LabId = "{lab_id}";

        // Gate 4 intentionally contains no Common or domain API calls. Add only
        // signatures verified in the approved design during Gate 5.
    }}
}}
'''


def editor_source(namespace_name: str, class_name: str, lab_id: str, scene: str) -> str:
    menu_label = " ".join(part.capitalize() for part in lab_id.split("_"))
    return f'''using System;
using System.Collections.Generic;
using EMVR.Labs.{namespace_name};
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EMVR.Labs.{namespace_name}.Editor
{{
    public static class {class_name}SceneBuilder
    {{
        const string OutputScenePath = "{scene}";
        const string GeneratedRootName = "{class_name}_Generated";

        [MenuItem("Tools/EMVR/Labs/Build {menu_label}")]
        public static void BuildScene()
        {{
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {{
                EditorUtility.DisplayDialog("EMVR Lab Builder", "Exit Play Mode before building the lab scene.", "OK");
                return;
            }}

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {{
                return;
            }}

            bool outputExisted = AssetDatabase.LoadAssetAtPath<SceneAsset>(OutputScenePath) != null;
            Scene scene = default(Scene);
            List<string> capturedErrors = new List<string>();
            Application.LogCallback capture = delegate(string condition, string stackTrace, LogType type)
            {{
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                {{
                    capturedErrors.Add(condition);
                }}
            }};

            Application.logMessageReceived += capture;
            try
            {{
                if (!outputExisted)
                {{
                    EnsureAssetFolder(OutputScenePath);
                    scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                }}
                else
                {{
                    scene = EditorSceneManager.OpenScene(OutputScenePath, OpenSceneMode.Single);
                }}

                GameObject root = GameObject.Find(GeneratedRootName);
                if (outputExisted && root == null)
                {{
                    throw new InvalidOperationException(
                        "Refusing to adopt an existing scene without the current-run marker root: "
                        + GeneratedRootName);
                }}
                if (root == null)
                {{
                    root = new GameObject(GeneratedRootName);
                }}

                if (root.GetComponent<{class_name}Controller>() == null)
                {{
                    root.AddComponent<{class_name}Controller>();
                }}

                if (capturedErrors.Count > 0)
                {{
                    throw new InvalidOperationException(
                        "Build emitted " + capturedErrors.Count + " error(s): "
                        + string.Join(" | ", capturedErrors.ToArray()));
                }}

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, OutputScenePath))
                {{
                    throw new InvalidOperationException("Unity could not save " + OutputScenePath);
                }}

                AssetDatabase.SaveAssets();
                Selection.activeGameObject = root;
            }}
            catch (Exception exception)
            {{
                if (outputExisted && AssetDatabase.LoadAssetAtPath<SceneAsset>(OutputScenePath) != null)
                {{
                    EditorSceneManager.OpenScene(OutputScenePath, OpenSceneMode.Single);
                }}
                else
                {{
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                }}

                Debug.LogError("[{lab_id}] Build failed; no partial scene was accepted: " + exception);
                return;
            }}
            finally
            {{
                Application.logMessageReceived -= capture;
            }}

            Debug.Log("[{lab_id}] Blind scaffold scene built: " + OutputScenePath);
        }}

        static void EnsureAssetFolder(string assetPath)
        {{
            string folder = System.IO.Path.GetDirectoryName(assetPath).Replace('\\\\', '/');
            string current = "Assets";
            string[] parts = folder.Split('/');
            for (int i = 1; i < parts.Length; i++)
            {{
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {{
                    AssetDatabase.CreateFolder(current, parts[i]);
                }}
                current = next;
            }}
        }}
    }}
}}
'''


def command_scaffold(args: argparse.Namespace) -> int:
    root = args.project_root.resolve()
    require_foundation(root)
    lab_id = require_lab_id(args.lab_id)
    state = load_current_state(root, lab_id)
    require_gate_approved(state, 3)
    if current_gate(state) != 4:
        raise RuntimeError(f"Scaffolding belongs to Gate 4, but current gate is {current_gate(state)}.")
    errors, _warnings, _bundle = validate_contracts(root, lab_id)
    if errors:
        raise RuntimeError("Contracts are invalid. Run validate before scaffold.")
    if not state.get("design_approval", {}).get("approved"):
        raise RuntimeError("Design is not approved. Review design.md and run approve first.")

    class_name = pascal_name(lab_id)
    runtime = root / "UnityProject" / "Assets" / "Scripts" / "EMVR" / "Labs" / class_name / f"{class_name}Controller.cs"
    editor = root / "UnityProject" / "Assets" / "Editor" / class_name / f"{class_name}SceneBuilder.cs"
    guide = root / "LabSpecs" / lab_id / "implementation.md"
    output_scene = root / "UnityProject" / state["scene"]
    if output_scene.exists():
        raise RuntimeError(
            f"Output scene already exists before the current scaffold owns it: {output_scene}. "
            "Do not adopt or overwrite a donor/previous-run scene."
        )
    write_text(runtime, runtime_source(class_name, class_name, lab_id))
    write_text(editor, editor_source(class_name, class_name, lab_id, state["scene"]))
    write_text(guide, f'''# {lab_id} implementation handoff

Generated files:

- `{runtime.relative_to(root).as_posix()}`
- `{editor.relative_to(root).as_posix()}`

Unity beginner handoff requirements:

1. Assume the user has never used Unity. Before every Unity-owned check, send a
   numbered click-by-click procedure; never send only `run Test Runner`, `Clear
   Console`, `run the Builder`, or `enter Play Mode`.
2. For compilation, first confirm the triangular Play button centered in the
   top toolbar is gray (Edit Mode), wait for the lower-right progress spinner to
   disappear, open `Window > General > Console`, click **Clear** at the Console
   toolbar's upper-left, run `Assets > Refresh`, and wait again. Success requires
   zero red compiler errors; on failure request a screenshot with the first red
   entry selected and its full details visible.
3. After Gate 5 test code exists, inspect its `.asmdef` and `[Test]` methods.
   Before asking the user to test, state the exact assembly, exact test names,
   and expected count. Direct the user to `Window > General > Test Runner`, note
   that the tab normally docks in the lower panel, click **EditMode**, search the
   exact assembly, expand it, select it, and click **Run Selected**. Success is
   every requested test green with the exact passed count and zero failures.
   If it fails, request Test Runner and Console screenshots. Do not enter Play
   Mode or run the scene Builder during EditMode tests.
4. During Gate 5, run the exact top-menu command
   `Tools > EMVR > Labs > Build {' '.join(part.capitalize() for part in lab_id.split('_'))}`
   only after it has been reduced to the neutral/minimal Gate 5 validation unit.
   Use it to obtain the button/spawn/visibility evidence. After Gate 5 approval,
   Gate 6 expands the same Builder to the complete integrated scene and runs it
   again, followed by `Tools > EMVR > Common > Validate Active Scene`.
5. Open Game view by clicking the **Game** tab beside **Scene** in the central
   editor area; fallback path: `Window > General > Game`. Enter Play Mode with
   the top-center triangular button and state that it should become blue. Test
   the desktop core flow before XR, then exit Play Mode so the button is gray.
6. Tell the user exactly which initial/completed Game View screenshots and
   Console counts/log lines to return. Follow the Skill reference
   `references/unity-beginner-handoff.md` for every Unity instruction.

    Record Gate 4 with `labflow record --scaffold-compile passed --note
    "<observed evidence>"`. At Gate 5, first run
    the mandatory-room importer, then
    `python .agents/skills/build-unity-vr-lab-blind/scripts/materialize_gate5_foundation.py
    --lab-id {lab_id}`. Do not edit either the protected source or the generated
    immutable copy. Run the exact three `EMVR.ReusableFoundation.Gate5.Tests`
    EditMode tests and a neutral button/spawn/visibility scene check, then record
    `--gate5-foundation passed`. Only after that foundation checkpoint passes,
    implement and record the smallest domain slice and its clean compile. Then
    rerun and re-record both evidence sets without another file change so their
    stored content digests describe the same final Gate 5 output.

The scaffold creates a new scene and adds only a generated root/controller. It
does not call Common or domain APIs before the Gate 3 signature audit is applied.
Import approved assets with `scripts/import_approved_asset_closure.py` into
`UnityProject/Assets/Generated/{lab_id}/ApprovedDependencies/`, record their
provenance, then implement every mandatory Common reuse row plus approved domain
objects and interactions. Keep fail-closed error capture around all generated
content: any build Error/Exception/Assert must abort before save and must not
print success. Never adopt an existing scene that lacks the generated marker
root. Do not declare the lab complete yet.
''')
    state["stage"] = "scaffold_ready"
    state["generated_files"] = [str(runtime.relative_to(root)), str(editor.relative_to(root)), str(guide.relative_to(root))]
    ensure_evidence(state)["scaffold_compile"] = "pending"
    save_state(root, lab_id, state)
    print(f"Scaffold generated for {lab_id}.")
    print(f"Unity menu: Tools > EMVR > Labs > Build {' '.join(part.capitalize() for part in lab_id.split('_'))}")
    return 0


def derived_stage(state: dict[str, Any]) -> str:
    if gate_is_approved(state, 10):
        return "complete"
    if state.get("stage") == "retrospective_draft":
        return "retrospective_draft"
    if gate_is_approved(state, 9):
        return "accepted"
    evidence = ensure_evidence(state)
    if evidence.get("compile") == "passed" and evidence.get("scene_built") == "passed" and evidence.get("playtest") == "passed":
        return "playtest_passed"
    if evidence.get("compile") == "passed" and evidence.get("scene_built") == "passed":
        return "scene_built"
    if evidence.get("compile") == "passed":
        return "unity_compiled"
    if (evidence.get("gate5_foundation") == "passed" and
            evidence.get("domain_slice") == "passed" and
            evidence.get("domain_compile") == "passed"):
        return "domain_slice_validated"
    if evidence.get("gate5_foundation") == "passed":
        return "gate5_foundation_validated"
    if evidence.get("domain_compile") == "passed":
        return "domain_compiled"
    if evidence.get("scaffold_compile") == "passed":
        return "scaffold_compiled"
    return state.get("stage", "unknown")


def next_action(root: Path, state: dict[str, Any]) -> str:
    if gate_is_approved(state, 10):
        return "Workflow and Gate 10 retrospective are complete."
    gate = current_gate(state)
    readiness = gate_readiness_errors(root, state, gate)
    if not readiness:
        if gate == 9:
            return (
                "Gate 9 user-acceptance review is complete. Request user approval and explicitly "
                "remind them that Gate 10 will summarize verified errors and save reusable lessons "
                "before the workflow is complete."
            )
        return (
            f"Gate {gate} ({GATE_NAMES[gate]}) is complete. Request approval; a clear "
            "progression response such as `继续`, `可以`, or `下一步` is sufficient. "
            "Make no next-gate changes while waiting."
        )
    if gate == 0:
        if ensure_evidence(state).get("boundary_verifier") != "passed":
            return "Run the release-level Builder Boundary and record the observed PASS."
        if not isinstance(state.get("protected_baseline"), dict):
            return (
                "Run `python Tools/labflow/labflow.py capture-gate0-baseline "
                f"--lab-id {state['lab_id']}` once."
            )
        return "Complete the safe prior-lessons review, or report any protected-baseline blocker shown above."
    return {
        1: "Complete the Brief interview and Brief file in one work package, then submit it once for approval.",
        2: "Complete and validate all five contracts in one work package, then submit Gate 2 once for approval.",
        3: "Complete the full design/reuse/environment review in one work package, then submit it once for approval.",
        4: "Generate only the scaffold, run a clean Unity compile smoke test, and record scaffold-compile evidence before approval.",
        5: "Materialize and validate the immutable Gate 5 foundation first; only then implement the smallest domain closure and record foundation, domain-slice, and clean-compile evidence.",
        6: "Complete Unity compile, scene build, Common validation, Console, and Game View checks as one Gate 6 package.",
        7: "Complete desktop/XR/state/lifecycle testing as one Gate 7 package.",
        8: "Complete the evidence/provenance package and submit it for approval.",
        9: "Present the completed Lab for explicit user acceptance; after acceptance, announce the required Gate 10 retrospective.",
        10: "Run `labflow retrospective`, replace every placeholder with verified evidence, and submit both retrospective files for approval.",
    }[gate]


def command_status(args: argparse.Namespace) -> int:
    root = args.project_root.resolve()
    lab_id = require_lab_id(args.lab_id)
    state = load_state(root, lab_id)
    workflow_mode = str(state.get("workflow_mode", "blind-rebuild"))
    try:
        validate_workspace_mode(root, lab_id, workflow_mode)
    except RuntimeError as error:
        payload = {
            "lab_id": lab_id,
            "workflow_mode": workflow_mode,
            "state_schema": state.get("schema_version", "missing"),
            "current_schema": CURRENT_STATE_SCHEMA,
            "current_gate": None,
            "gate_ready_for_approval": False,
            "gate_blockers": [str(error)],
            "next_action": "Create/open the prescribed workspace, then start or resume there.",
        }
        print(json.dumps(payload, ensure_ascii=False, indent=2))
        return 0
    if state.get("schema_version") != CURRENT_STATE_SCHEMA:
        payload = {
            "lab_id": lab_id,
            "state_schema": state.get("schema_version", "missing"),
            "current_schema": CURRENT_STATE_SCHEMA,
            "migration_required": True,
            "current_gate": None,
            "gate_ready_for_approval": False,
            "gate_blockers": [
                "Legacy workflow approvals cannot satisfy the current gate definitions."
            ],
            "next_action": (
                f"Run `python Tools/labflow/labflow.py migrate-state --lab-id {lab_id}`; "
                "then resume from the first invalidated gate."
            ),
        }
        print(json.dumps(payload, ensure_ascii=False, indent=2))
        return 0
    errors, warnings, _bundle = validate_contracts(root, lab_id)
    gate = current_gate(state)
    accepted = gate_is_approved(state, 9)
    workflow_complete = gate_is_approved(state, 10)
    readiness = [] if workflow_complete else gate_readiness_errors(root, state, gate)
    payload = {
        "lab_id": lab_id,
        "workflow_mode": workflow_mode,
        "state_schema": state.get("schema_version", "missing"),
        "current_schema": CURRENT_STATE_SCHEMA,
        "legacy_state_requires_gate_reapproval": state.get("schema_version") != CURRENT_STATE_SCHEMA,
        "stage": derived_stage(state),
        "accepted": accepted,
        "workflow_complete": workflow_complete,
        "current_gate": None if workflow_complete else gate,
        "current_gate_name": None if workflow_complete else GATE_NAMES[gate],
        "gate_ready_for_approval": False if workflow_complete else not readiness,
        "gate_blockers": readiness,
        "gate_approvals": state.get("gate_approvals", {}),
        "contracts": {"ok": not errors, "errors": len(errors), "warnings": len(warnings)},
        "brief_confirmed": state.get("brief_confirmation", {}).get("confirmed", False),
        "design_approved": state.get("design_approval", {}).get("approved", False),
        "protected_baseline": state.get("protected_baseline"),
        "evidence": state.get("evidence", {}),
        "next_action": next_action(root, state),
    }
    print(json.dumps(payload, ensure_ascii=False, indent=2))
    return 0


def command_record(args: argparse.Namespace) -> int:
    root = args.project_root.resolve()
    lab_id = require_lab_id(args.lab_id)
    state = load_current_state(root, lab_id)
    require_gate5_evidence_order(
        state,
        args.gate5_foundation,
        args.domain_slice,
        args.domain_compile,
    )
    groups: list[int] = []
    if args.boundary_verifier is not None:
        groups.append(0)
    if args.scaffold_compile is not None:
        groups.append(4)
    if args.gate5_foundation is not None or args.domain_slice is not None or args.domain_compile is not None:
        groups.append(5)
    if args.compile is not None or args.scene_built is not None:
        groups.append(6)
    if args.playtest is not None:
        groups.append(7)
    groups = sorted(set(groups))
    if len(groups) > 1:
        raise RuntimeError("One record command may contain evidence for only one gate.")
    if groups and not (args.note or "").strip():
        raise RuntimeError("Observed evidence requires a non-empty --note identifying the Unity check/result.")
    if groups:
        gate = groups[0]
        if gate > 0:
            require_gate_approved(state, gate - 1)
        if current_gate(state) != gate:
            raise RuntimeError(f"Evidence belongs to Gate {gate}, but current gate is {current_gate(state)}.")

    evidence = ensure_evidence(state)
    for key in ("boundary_verifier", "scaffold_compile", "gate5_foundation", "domain_slice", "domain_compile", "compile", "scene_built", "playtest"):
        value = getattr(args, key)
        if value is not None:
            evidence[key] = value
    if args.gate5_foundation == "passed":
        materialization_errors = gate5_materialization_errors(root, lab_id)
        materialization_errors.extend(approved_asset_import_errors(root, lab_id))
        if materialization_errors:
            raise RuntimeError(
                "Cannot record Gate 5 foundation as passed:\n- "
                + "\n- ".join(materialization_errors)
            )
        evidence["gate5_foundation_lab_output_digest"] = gate5_lab_output_digest(
            root, lab_id
        )
    elif args.gate5_foundation is not None:
        evidence.pop("gate5_foundation_lab_output_digest", None)
    if args.domain_slice is not None or args.domain_compile is not None:
        if (
            evidence.get("domain_slice") == "passed"
            and evidence.get("domain_compile") == "passed"
        ):
            evidence["gate5_domain_lab_output_digest"] = gate5_lab_output_digest(
                root, lab_id
            )
        else:
            evidence.pop("gate5_domain_lab_output_digest", None)
    if args.note:
        evidence.setdefault("notes", []).append({
            "recorded_at": utc_now(),
            "gate": groups[0] if groups else current_gate(state),
            "text": args.note.strip(),
        })
    state["stage"] = derived_stage(state)
    save_state(root, lab_id, state)
    print(f"Evidence recorded for {lab_id}; stage is now {state['stage']}.")
    print(f"Next: {next_action(root, state)}")
    return 0


def command_capture_gate0_baseline(args: argparse.Namespace) -> int:
    root = args.project_root.resolve()
    lab_id = require_lab_id(args.lab_id)
    state = load_current_state(root, lab_id)
    path = capture_gate0_protected_baseline(root, lab_id, state)
    print(f"Captured immutable Gate 0 run baseline: {path}")
    print("Gate 1 through Gate 10 will compare protected files against this snapshot.")
    return 0


def command_retrospective(args: argparse.Namespace) -> int:
    root = args.project_root.resolve()
    lab_id = require_lab_id(args.lab_id)
    state = load_current_state(root, lab_id)
    require_gate_approved(state, 9)
    if current_gate(state) != 10:
        raise RuntimeError(
            f"Retrospective generation belongs to Gate 10, but current gate is {current_gate(state)}."
        )

    run_relative, lesson_relative = retrospective_relative_paths(lab_id)
    run_path = root / run_relative
    lesson_path = root / lesson_relative
    existing = [path for path in (run_path, lesson_path) if path.exists()]
    if existing:
        raise RuntimeError(
            "Refusing to overwrite retrospective history; use a distinct lab_id or review the existing files:\n"
            + "\n".join(str(path) for path in existing)
        )

    write_text(run_path, retrospective_text(lab_id, state.get("title", lab_id)))
    write_text(lesson_path, reusable_lesson_text(lab_id, state.get("title", lab_id)))
    state["retrospective_paths"] = {
        "run": run_relative.as_posix(),
        "lesson": lesson_relative.as_posix(),
    }
    state["stage"] = "retrospective_draft"
    save_state(root, lab_id, state)
    print(f"Gate 10 run retrospective created: {run_path}")
    print(f"Gate 10 reusable lesson created: {lesson_path}")
    print("Replace every TODO with verified evidence, then request Gate 10 approval.")
    return 0


def command_approve_gate(args: argparse.Namespace) -> int:
    root = args.project_root.resolve()
    lab_id = require_lab_id(args.lab_id)
    gate = args.gate
    note = validate_text_argument(args.note, "approval note")
    state = load_current_state(root, lab_id)
    if gate not in GATE_NAMES:
        raise RuntimeError("gate must be an integer from 0 through 10")
    if gate_is_approved(state, gate):
        raise RuntimeError(f"Gate {gate} is already approved.")
    if current_gate(state) != gate:
        raise RuntimeError(f"Current gate is {current_gate(state)}; cannot approve Gate {gate}.")
    if gate > 0:
        require_gate_approved(state, gate - 1)
    readiness = gate_readiness_errors(root, state, gate)
    if readiness:
        raise RuntimeError(
            f"Gate {gate} ({GATE_NAMES[gate]}) is not ready for approval:\n- "
            + "\n- ".join(readiness)
        )

    if gate == 1:
        state["brief_confirmation"] = {
            "confirmed": True,
            "note": note,
            "confirmed_at": utc_now(),
        }
    elif gate == 3:
        state["design_approval"] = {
            "approved": True,
            "note": note,
            "approved_at": utc_now(),
        }
        state["stage"] = "design_approved"
    elif gate == 9:
        state["stage"] = "accepted"
    elif gate == 10:
        state["stage"] = "complete"

    record_gate_approval(state, gate, note)
    save_state(root, lab_id, state)
    if gate == 9:
        print("Gate 9 user acceptance recorded. The required Gate 10 error-retrospective and knowledge-capture step is next.")
        print("Do not end the workflow yet; run `labflow retrospective --lab-id <lab_id>` in the next response.")
    elif gate < 10:
        print(f"Gate {gate} approval recorded. Next response may execute Gate {gate + 1} only.")
    else:
        print("Gate 10 approval recorded. Workflow and retrospective are complete.")
    return 0


def command_migrate_state(args: argparse.Namespace) -> int:
    root = args.project_root.resolve()
    lab_id = require_lab_id(args.lab_id)
    state = load_state(root, lab_id)
    old_schema = state.get("schema_version", "missing")
    if old_schema == CURRENT_STATE_SCHEMA:
        raise RuntimeError("Workflow state is already current; no migration is needed.")

    workflow_mode = str(state.get("workflow_mode", "blind-rebuild"))
    validate_workspace_mode(root, lab_id, workflow_mode)
    state["workflow_mode"] = workflow_mode

    evidence = ensure_evidence(state)
    state.setdefault("migration_history", []).append(
        {
            "from_schema": old_schema,
            "to_schema": CURRENT_STATE_SCHEMA,
            "migrated_at": utc_now(),
            "invalidated_gates": list(range(11)),
            "reason": (
                "Gate 0 now captures a one-time protected run baseline in addition to "
                "release-level frozen hashes; all gates require fresh review."
            ),
        }
    )
    state["gate_approvals"] = {}
    state["brief_confirmation"] = {
        "confirmed": False,
        "note": "",
        "confirmed_at": None,
    }
    state["design_approval"] = {
        "approved": False,
        "note": "",
        "approved_at": None,
    }
    state["protected_baseline"] = None
    for key in default_evidence():
        evidence[key] = "pending"
    evidence.pop("gate5_foundation_lab_output_digest", None)
    evidence.pop("gate5_domain_lab_output_digest", None)
    evidence.pop("notes", None)
    state.pop("last_contract_validation", None)
    state["stage"] = "boundary_review"
    save_state(root, lab_id, state)
    print(
        f"Migrated {lab_id} from schema {old_schema} to {CURRENT_STATE_SCHEMA}. "
        "All previous approvals/evidence were invalidated; resume at Gate 0."
    )
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Contract-first workflow for Summer26 Unity VR labs.")
    parser.add_argument("--project-root", type=Path, default=Path(__file__).resolve().parents[2], help="Builder Pack root (defaults to this repository).")
    subparsers = parser.add_subparsers(dest="command", required=True)

    new = subparsers.add_parser("new", help="Create a brief, design review, state, and five contracts.")
    new.add_argument("--lab-id", required=True)
    new.add_argument("--title", required=True)
    new.add_argument("--domain", required=True)
    new.add_argument("--scene", required=True)
    new.add_argument(
        "--mode",
        choices=WORKFLOW_MODES,
        default="blind-rebuild",
        help=(
            "blind-rebuild requires the fixed internal RebuildWorkspaces directory; "
            "integrated-development is for a Builder Pack embedded in a host project"
        ),
    )
    new.set_defaults(func=command_new)

    validate = subparsers.add_parser("validate", help="Validate all five contracts and cross-references.")
    validate.add_argument("--lab-id", required=True)
    validate.set_defaults(func=command_validate)

    scaffold = subparsers.add_parser("scaffold", help="Generate runtime and clean-scene builder C# skeletons.")
    scaffold.add_argument("--lab-id", required=True)
    scaffold.set_defaults(func=command_scaffold)

    status = subparsers.add_parser("status", help="Show workflow gates, evidence, and next action.")
    status.add_argument("--lab-id", required=True)
    status.set_defaults(func=command_status)

    migrate_state = subparsers.add_parser(
        "migrate-state",
        help="Migrate a legacy run and invalidate approvals affected by newer gate rules.",
    )
    migrate_state.add_argument("--lab-id", required=True)
    migrate_state.set_defaults(func=command_migrate_state)

    record = subparsers.add_parser("record", help="Record evidence from user-owned Unity checks.")
    record.add_argument("--lab-id", required=True)
    record.add_argument("--boundary-verifier", dest="boundary_verifier", choices=RESULT_VALUES)
    record.add_argument("--scaffold-compile", dest="scaffold_compile", choices=RESULT_VALUES)
    record.add_argument("--gate5-foundation", dest="gate5_foundation", choices=RESULT_VALUES)
    record.add_argument("--domain-slice", dest="domain_slice", choices=RESULT_VALUES)
    record.add_argument("--domain-compile", dest="domain_compile", choices=RESULT_VALUES)
    record.add_argument("--compile", choices=RESULT_VALUES)
    record.add_argument("--scene-built", dest="scene_built", choices=RESULT_VALUES)
    record.add_argument("--playtest", choices=RESULT_VALUES)
    record.add_argument("--note")
    record.set_defaults(func=command_record)

    capture_baseline = subparsers.add_parser(
        "capture-gate0-baseline",
        help="Freeze the per-run protected-file snapshot once during Gate 0 after Builder Boundary passes.",
    )
    capture_baseline.add_argument("--lab-id", required=True)
    capture_baseline.set_defaults(func=command_capture_gate0_baseline)

    retrospective = subparsers.add_parser(
        "retrospective",
        help="Create the Gate 10 run retrospective and reusable-lesson files after user acceptance.",
    )
    retrospective.add_argument("--lab-id", required=True)
    retrospective.set_defaults(func=command_retrospective)

    approve_gate = subparsers.add_parser(
        "approve-gate",
        help="Record direct or semantic user approval for exactly the current completed gate.",
    )
    approve_gate.add_argument("--lab-id", required=True)
    approve_gate.add_argument("--gate", required=True, type=int, choices=range(11))
    approve_gate.add_argument("--note", required=True)
    approve_gate.set_defaults(func=command_approve_gate)
    return parser


def main() -> int:
    parser = build_parser()
    args = parser.parse_args()
    try:
        return args.func(args)
    except RuntimeError as error:
        print(f"ERROR: {error}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
