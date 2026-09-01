#!/usr/bin/env python3
"""Verify and copy the protected Gate 5 foundation into current-Lab output."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import shutil
import stat
from datetime import datetime, timezone
from pathlib import Path


LAB_ID_PATTERN = re.compile(r"^[a-z][a-z0-9_]{2,63}$")
CURRENT_STATE_SCHEMA = "1.9.0"


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def load_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def workflow_gate(root: Path, lab_id: str) -> int:
    state_path = root / "LabSpecs" / lab_id / "workflow_state.json"
    if not state_path.is_file():
        raise SystemExit(f"No active workflow state exists for {lab_id}.")
    state = load_json(state_path)
    if state.get("lab_id") != lab_id:
        raise SystemExit("Workflow state lab_id does not match --lab-id.")
    if state.get("schema_version") != CURRENT_STATE_SCHEMA:
        raise SystemExit("Workflow state schema is stale; run labflow migrate-state first.")
    approvals = state.get("gate_approvals", {})
    for gate in range(11):
        approval = approvals.get(str(gate), {})
        if not isinstance(approval, dict) or not approval.get("approved", False):
            return gate
    return 11


def evidence_matches(recorded: dict, expected: dict) -> bool:
    keys = ("schema_version", "lab_id", "source_manifest", "source_manifest_sha256", "copy_mode", "files")
    return all(recorded.get(key) == expected.get(key) for key in keys)


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Materialize the immutable Gate 5 reusable foundation."
    )
    parser.add_argument("--lab-id", required=True)
    parser.add_argument(
        "--builder-root", type=Path, default=Path(__file__).resolve().parents[4]
    )
    parser.add_argument("--verify-only", action="store_true")
    args = parser.parse_args()

    if not LAB_ID_PATTERN.fullmatch(args.lab_id):
        raise SystemExit("Invalid lab_id; use lowercase letters, digits, and underscores.")

    root = args.builder_root.resolve()
    active_gate = workflow_gate(root, args.lab_id)
    if (not args.verify_only and active_gate != 5) or (args.verify_only and active_gate < 5):
        raise SystemExit(
            f"Gate 5 foundation materialization is only allowed at Gate 5 "
            f"(verification is allowed afterward); current gate is {active_gate}."
        )
    foundation = root / "ReusableFoundation" / "Gate5MinimalSlice"
    source_root = foundation / "Source"
    manifest_path = foundation / "manifest.json"
    manifest = load_json(manifest_path)
    entries = manifest.get("files", [])
    if not entries:
        raise SystemExit("Gate 5 foundation manifest has no files.")

    destination_root = (
        root
        / "UnityProject"
        / "Assets"
        / "Generated"
        / args.lab_id
        / "Gate5Foundation"
    )
    evidence_path = (
        root / "BuildEvidence" / args.lab_id / "gate5-foundation-import.json"
    )
    copied = []
    copy_plan: list[tuple[Path, Path]] = []
    listed_sources: set[Path] = set()

    for entry in entries:
        relative = Path(entry["path"])
        source = (source_root / relative).resolve()
        if source_root.resolve() not in source.parents:
            raise SystemExit(f"Manifest path escapes Source: {relative.as_posix()}")
        if not source.is_file():
            raise SystemExit(f"Missing Gate 5 foundation source: {relative.as_posix()}")
        actual_source_hash = sha256(source)
        listed_sources.add(source)
        if actual_source_hash != entry["sha256"]:
            raise SystemExit(
                f"Protected source hash mismatch: {relative.as_posix()}\n"
                f"expected {entry['sha256']}\nactual   {actual_source_hash}"
            )

        destination = destination_root / relative
        if destination.exists():
            if not destination.is_file() or sha256(destination) != actual_source_hash:
                raise SystemExit(
                    "Refusing to overwrite a modified Gate 5 foundation copy: "
                    f"{destination.relative_to(root).as_posix()}"
                )
        elif args.verify_only:
            raise SystemExit(
                f"Gate 5 foundation copy is missing: {destination.relative_to(root).as_posix()}"
            )
        else:
            copy_plan.append((source, destination))

        copied.append(
            {
                "source": source.relative_to(root).as_posix(),
                "destination": destination.relative_to(root).as_posix(),
                "sha256": actual_source_hash,
                "immutable_copy": True,
            }
        )

    unlisted_sources = sorted(
        path.relative_to(source_root).as_posix()
        for path in source_root.rglob("*")
        if path.is_file() and path.suffix.lower() != ".meta" and path.resolve() not in listed_sources
    )
    if unlisted_sources:
        raise SystemExit(
            "Gate 5 foundation Source contains files absent from manifest: "
            + ", ".join(unlisted_sources)
        )

    expected_destinations = {
        (root / record["destination"]).resolve() for record in copied
    }
    extra_destinations = sorted(
        path.relative_to(destination_root).as_posix()
        for path in destination_root.rglob("*")
        if path.is_file()
        and path.suffix.lower() != ".meta"
        and path.resolve() not in expected_destinations
    ) if destination_root.exists() else []
    if extra_destinations:
        raise SystemExit(
            "Gate 5 foundation copy contains unlisted files: "
            + ", ".join(extra_destinations)
        )

    payload = {
        "schema_version": "1.0.0",
        "lab_id": args.lab_id,
        "source_manifest": manifest_path.relative_to(root).as_posix(),
        "source_manifest_sha256": sha256(manifest_path),
        "copy_mode": "byte_for_byte_read_only",
        "generated_at": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "files": copied,
    }

    # Validate any existing evidence before copying. This keeps a manifest
    # conflict from producing a partial foundation materialization.
    if evidence_path.exists():
        recorded = load_json(evidence_path)
        if not evidence_matches(recorded, payload):
            action = "verify" if args.verify_only else "overwrite"
            raise SystemExit(
                f"Refusing to {action} mismatched Gate 5 foundation evidence."
            )

    if args.verify_only:
        if not evidence_path.exists():
            raise SystemExit(
                f"Gate 5 foundation evidence is missing: {evidence_path.relative_to(root).as_posix()}"
            )
        print(f"[EMVR Gate 5 Foundation] VERIFIED: {len(copied)} files")
        return 0

    for source, destination in copy_plan:
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, destination)
        try:
            destination.chmod(stat.S_IREAD)
        except OSError:
            pass

    evidence_path.parent.mkdir(parents=True, exist_ok=True)
    if not evidence_path.exists():
        evidence_path.write_text(
            json.dumps(payload, ensure_ascii=False, indent=2) + "\n",
            encoding="utf-8",
            newline="\n",
        )

    print(f"[EMVR Gate 5 Foundation] MATERIALIZED: {len(copied)} files")
    print(f"Destination: {destination_root.relative_to(root).as_posix()}")
    print(f"Evidence: {evidence_path.relative_to(root).as_posix()}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
