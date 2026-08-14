from __future__ import annotations

import importlib.util
import json
import os
import shutil
import tempfile
import unittest
from pathlib import Path
from unittest import mock


MODULE_PATH = Path(__file__).with_name("labflow.py")
SPEC = importlib.util.spec_from_file_location("labflow_under_test", MODULE_PATH)
assert SPEC and SPEC.loader
labflow = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(labflow)


class LabflowTests(unittest.TestCase):
    def test_blind_rebuild_requires_fixed_internal_workspace(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / "wrong_name"
            root.mkdir()
            with self.assertRaisesRegex(RuntimeError, "fixed internal workspace"):
                labflow.validate_workspace_mode(root, "lab_example", "blind-rebuild")

            valid = (
                Path(directory)
                / "EMVR_Blind_BuilderPack"
                / "RebuildWorkspaces"
                / "EMVR_Blind_Rebuild_lab_example"
            )
            valid.mkdir(parents=True)
            (valid / ".emvr-workspace.json").write_text(
                json.dumps(
                    {
                        "schema_version": "1.1.0",
                        "workflow_mode": "blind-rebuild",
                        "lab_id": "lab_example",
                        "required_master_directory_name": "EMVR_Blind_BuilderPack",
                        "required_directory_name": "EMVR_Blind_Rebuild_lab_example",
                    }
                ),
                encoding="utf-8",
            )
            labflow.validate_workspace_mode(valid, "lab_example", "blind-rebuild")

    def test_integrated_development_has_no_internal_workspace_constraint(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            labflow.validate_workspace_mode(
                Path(directory), "lab_example", "integrated-development"
            )

    def test_blind_rebuild_rejects_stale_workspace_marker_schema(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = (
                Path(directory)
                / "EMVR_Blind_BuilderPack"
                / "RebuildWorkspaces"
                / "EMVR_Blind_Rebuild_lab_example"
            )
            root.mkdir(parents=True)
            (root / ".emvr-workspace.json").write_text(
                json.dumps(
                    {
                        "schema_version": "1.0.0",
                        "workflow_mode": "blind-rebuild",
                        "lab_id": "lab_example",
                        "required_master_directory_name": "EMVR_Blind_BuilderPack",
                        "required_directory_name": "EMVR_Blind_Rebuild_lab_example",
                    }
                ),
                encoding="utf-8",
            )
            with self.assertRaisesRegex(RuntimeError, "marker does not match"):
                labflow.validate_workspace_mode(root, "lab_example", "blind-rebuild")

    def test_foundation_rejects_maintainer_material_inside_pack(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            guide = root / "WORKFLOW_MAINTAINER_GUIDE.md"
            guide.write_text("maintenance only", encoding="utf-8")
            errors = labflow.foundation_errors(root)
            self.assertTrue(any("Maintainer-only material" in item for item in errors))

    def test_foundation_requires_portable_lf_checkout_rule(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / ".gitattributes").write_text("* text=auto\n", encoding="utf-8")

            errors = labflow.foundation_errors(root)

            self.assertTrue(
                any("frozen hashes survive Git checkout" in item for item in errors)
            )

    def test_foundation_requires_repository_ignore_rules(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            errors = labflow.foundation_errors(root)
            self.assertIn(
                "Required Builder foundation item is missing: .gitignore",
                errors,
            )

    def test_run_pack_creator_uses_extended_length_copy(self) -> None:
        script = (Path(__file__).resolve().parents[1] / "create_blind_run_pack.ps1").read_text(
            encoding="utf-8-sig"
        )
        self.assertIn("Convert-ToExtendedLengthPath", script)
        self.assertIn("[System.IO.Directory]::CreateDirectory", script)
        self.assertIn("[System.IO.File]::Copy", script)
        self.assertNotIn("Copy-Item", script)

    def test_scene_path_is_confined_to_assets_scenes(self) -> None:
        self.assertEqual(
            labflow.validate_scene_path("Assets/Scenes/MyLab.unity"),
            "Assets/Scenes/MyLab.unity",
        )
        for invalid in (
            "../Gold.unity",
            "Assets/Other/MyLab.unity",
            "Assets/Scenes/../../Gold.unity",
            'Assets/Scenes/Bad"Name.unity',
            "Assets/Scenes/NoExtension",
        ):
            with self.assertRaises(RuntimeError, msg=invalid):
                labflow.validate_scene_path(invalid)

    def test_contract_lookup_never_falls_back_to_example(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            contracts = root / "LabInterfaceContracts"
            contracts.mkdir()
            (contracts / "lab_example_lab_spec.example.json").write_text(
                "{}", encoding="utf-8"
            )
            with self.assertRaises(RuntimeError):
                labflow.find_contract(root, "lab_example", "lab_spec")

    def test_gate_progression_is_strictly_sequential(self) -> None:
        state = {"gate_approvals": {}}
        self.assertEqual(labflow.current_gate(state), 0)
        labflow.record_gate_approval(state, 0, "continue")
        self.assertEqual(labflow.current_gate(state), 1)
        state["gate_approvals"]["2"] = {"approved": True}
        self.assertEqual(labflow.current_gate(state), 1)

    def test_gate_zero_requires_recorded_boundary_pass(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            lesson = root / "KnowledgeBase" / "rebuild-lessons" / "safe.md"
            lesson.parent.mkdir(parents=True)
            lesson.write_text("# safe\n", encoding="utf-8")
            report = root / "LabSpecs" / "lab_example" / "prior-lessons-review.md"
            report.parent.mkdir(parents=True)
            report.write_text(
                "# review\n\nReview status: COMPLETE\n\n"
                "## Reviewed lesson files\n\n"
                "KnowledgeBase/rebuild-lessons/safe.md\n\n"
                "## Applicable safeguards\n\nApplied with evidence at Gate 0.\n\n"
                "## Not applicable or deferred lessons\n\nNone.\n\n"
                "## Restricted archive declaration\n\n"
                "Restricted archive accessed: no\n",
                encoding="utf-8",
            )
            state = {"lab_id": "lab_example", "evidence": labflow.default_evidence()}
            with mock.patch.object(labflow, "foundation_errors", side_effect=lambda _root: []):
                errors = labflow.gate_readiness_errors(root, state, 0)
                self.assertTrue(any("Boundary verifier" in item for item in errors))
                state["evidence"]["boundary_verifier"] = "passed"
                self.assertEqual(labflow.gate_readiness_errors(root, state, 0), [])

    def test_gate_eight_uses_only_current_lab_evidence(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            state = {
                "lab_id": "lab_example",
                "gate_approvals": {
                    str(gate): {"approved": True} for gate in range(8)
                },
                "evidence": labflow.default_evidence(),
            }
            (root / "BuildEvidence").mkdir()
            (root / "BuildEvidence" / "reuse-report.md").write_text(
                "lab_example complete", encoding="utf-8"
            )
            errors = labflow.gate_readiness_errors(root, state, 8)
            self.assertEqual(len(errors), 2)

            reuse, validation = labflow.evidence_relative_paths("lab_example")
            for relative in (reuse, validation):
                path = root / relative
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text("lab_example\ncomplete\npassed\n", encoding="utf-8")
            self.assertEqual(labflow.gate_readiness_errors(root, state, 8), [])

    def test_scaffold_runtime_has_no_unreviewed_common_calls(self) -> None:
        source = labflow.runtime_source("LabExample", "LabExample", "lab_example")
        self.assertNotIn("EMVR.Common", source)
        self.assertNotIn("EmVrTelemetryEmitter", source)
        self.assertNotIn("EmVrStatusDisplay", source)

    def test_scaffold_handoff_contains_beginner_unity_locations(self) -> None:
        source = MODULE_PATH.read_text(encoding="utf-8")
        for required in (
            "Window > General > Test Runner",
            "normally docks in the lower panel",
            "Run Selected",
            "exact assembly, exact test names",
            "Window > General > Console",
            "Window > General > Game",
            "references/unity-beginner-handoff.md",
        ):
            self.assertIn(required, source)

    def test_gate_five_requires_foundation_before_domain(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            state = {
                "lab_id": "lab_example",
                "evidence": labflow.default_evidence(),
            }
            with mock.patch.object(
                labflow, "gate5_materialization_errors", side_effect=lambda *_args: []
            ), mock.patch.object(
                labflow, "approved_asset_import_errors", side_effect=lambda *_args: []
            ):
                errors = labflow.gate_readiness_errors(root, state, 5)
                self.assertTrue(any("reusable-foundation" in item for item in errors))
                state["evidence"]["domain_slice"] = "passed"
                state["evidence"]["domain_compile"] = "passed"
                self.assertTrue(labflow.gate_readiness_errors(root, state, 5))
                state["evidence"]["gate5_foundation"] = "passed"
                current_digest = labflow.gate5_lab_output_digest(root, "lab_example")
                state["evidence"]["gate5_foundation_lab_output_digest"] = current_digest
                state["evidence"]["gate5_domain_lab_output_digest"] = current_digest
                self.assertEqual(labflow.gate_readiness_errors(root, state, 5), [])

    def test_materialized_foundation_copy_must_match_source(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = (
                root
                / "ReusableFoundation"
                / "Gate5MinimalSlice"
                / "Source"
                / "Runtime"
                / "Safe.cs"
            )
            source.parent.mkdir(parents=True)
            source.write_text("safe", encoding="utf-8")
            manifest = source.parents[2] / "manifest.json"
            labflow.write_json(
                manifest,
                {
                    "files": [
                        {
                            "path": "Runtime/Safe.cs",
                            "sha256": labflow.file_sha256(source),
                        }
                    ]
                },
            )
            destination = (
                root
                / "UnityProject"
                / "Assets"
                / "Generated"
                / "lab_example"
                / "Gate5Foundation"
                / "Runtime"
                / "Safe.cs"
            )
            destination.parent.mkdir(parents=True)
            destination.write_text("modified", encoding="utf-8")
            evidence = root / "BuildEvidence" / "lab_example" / "gate5-foundation-import.json"
            labflow.write_json(
                evidence,
                {
                    "lab_id": "lab_example",
                    "files": [
                        {
                            "destination": destination.relative_to(root).as_posix(),
                            "sha256": labflow.file_sha256(source),
                        }
                    ],
                },
            )
            errors = labflow.gate5_materialization_errors(root, "lab_example")
            self.assertTrue(any("was modified" in item for item in errors))

    def test_gate_five_rejects_missing_mandatory_room_import(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            errors = labflow.approved_asset_import_errors(Path(directory), "lab_example")
            self.assertTrue(any("approved-asset import evidence" in item.lower() for item in errors))

    def test_policy_drives_all_mandatory_approved_assets(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            policy = root / "Policies/reuse-allowlist.json"
            policy.parent.mkdir(parents=True)
            expected = ["ApprovedAssets/Room.prefab", "ApprovedAssets/XR Setup.prefab"]
            policy.write_text(json.dumps({"mandatory_assets": expected}), encoding="utf-8")
            self.assertEqual(labflow.mandatory_approved_assets(root), expected)

    def test_gate_five_detects_modified_approved_asset_copy(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source_relative = Path(
                "ApprovedAssets/XRInteractionToolkitSamples/Starter Assets/"
                "DemoSceneAssets/Prefabs/Teleportation Environment.prefab"
            )
            source = root / source_relative
            source.parent.mkdir(parents=True)
            source.write_text("room", encoding="utf-8")
            source_meta = Path(str(source) + ".meta")
            source_meta.write_text("guid: 0123456789abcdef0123456789abcdef", encoding="utf-8")
            destination_relative = Path(
                "UnityProject/Assets/Generated/lab_example/ApprovedDependencies/"
                "XRInteractionToolkitSamples/Starter Assets/DemoSceneAssets/Prefabs/"
                "Teleportation Environment.prefab"
            )
            destination = root / destination_relative
            destination.parent.mkdir(parents=True)
            destination.write_text("room", encoding="utf-8")
            destination_meta = Path(str(destination) + ".meta")
            destination_meta.write_text(source_meta.read_text(encoding="utf-8"), encoding="utf-8")
            manifest = {
                "lab_id": "lab_example",
                "approved_source": source_relative.as_posix(),
                "asset_count": 1,
                "assets": [{
                    "source": source_relative.as_posix(),
                    "destination": destination_relative.as_posix(),
                    "sha256": labflow.file_sha256(source),
                    "meta_sha256": labflow.file_sha256(source_meta),
                }],
                "dry_run": False,
            }
            manifest_path = root / "BuildEvidence/lab_example/approved-asset-imports/room.json"
            manifest_path.parent.mkdir(parents=True)
            labflow.write_json(manifest_path, manifest)
            self.assertEqual(labflow.approved_asset_import_errors(root, "lab_example"), [])
            destination.write_text("modified", encoding="utf-8")
            errors = labflow.approved_asset_import_errors(root, "lab_example")
            self.assertTrue(any("copy hash mismatch" in item.lower() for item in errors))

    def test_gate_five_validates_import_beyond_windows_max_path(self) -> None:
        directory = tempfile.mkdtemp()
        root = Path(directory)
        try:
            lab_id = "lab_example"
            approved = root / "ApprovedAssets"
            parts: list[str] = []
            while len(str((approved / Path(*parts) / "Room.prefab.meta").resolve())) < 220:
                parts.append(f"dependency_folder_{len(parts):02d}")
            relative = Path(*parts) / "Room.prefab"
            source = approved / relative
            self.assertLess(len(str(Path(str(source) + ".meta").resolve())), 260)
            source.parent.mkdir(parents=True)
            source.write_text("room", encoding="utf-8")
            source_meta = Path(str(source) + ".meta")
            source_meta.write_text(
                "guid: 0123456789abcdef0123456789abcdef", encoding="utf-8"
            )

            destination_relative = (
                Path("UnityProject/Assets/Generated")
                / lab_id
                / "ApprovedDependencies"
                / relative
            )
            destination = root / destination_relative
            destination_meta = Path(str(destination) + ".meta")
            self.assertGreater(len(str(destination_meta.resolve())), 260)
            os.makedirs(labflow.native_path(destination.parent), exist_ok=True)
            shutil.copy2(labflow.native_path(source), labflow.native_path(destination))
            shutil.copy2(labflow.native_path(source_meta), labflow.native_path(destination_meta))

            source_relative = Path("ApprovedAssets") / relative
            manifest = {
                "lab_id": lab_id,
                "approved_source": (
                    "ApprovedAssets/XRInteractionToolkitSamples/Starter Assets/"
                    "DemoSceneAssets/Prefabs/Teleportation Environment.prefab"
                ),
                "asset_count": 1,
                "assets": [{
                    "source": source_relative.as_posix(),
                    "destination": destination_relative.as_posix(),
                    "sha256": labflow.file_sha256(source),
                    "meta_sha256": labflow.file_sha256(source_meta),
                }],
                "dry_run": False,
            }
            manifest_path = root / "BuildEvidence/lab_example/approved-asset-imports/room.json"
            manifest_path.parent.mkdir(parents=True)
            labflow.write_json(manifest_path, manifest)

            self.assertEqual(labflow.approved_asset_import_errors(root, lab_id), [])
            digest_before = labflow.gate5_lab_output_digest(root, lab_id)
            with open(labflow.native_path(destination), "a", encoding="utf-8") as stream:
                stream.write(" changed")
            digest_after = labflow.gate5_lab_output_digest(root, lab_id)
            self.assertNotEqual(digest_before, digest_after)
        finally:
            shutil.rmtree(labflow.native_path(root), ignore_errors=True)

    def test_foundation_manifest_rejects_unlisted_source_files(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source_root = (
                root / "ReusableFoundation" / "Gate5MinimalSlice" / "Source"
            )
            listed = source_root / "Runtime" / "Safe.cs"
            listed.parent.mkdir(parents=True)
            listed.write_text("safe", encoding="utf-8")
            (listed.parent / "Unlisted.cs").write_text("unlisted", encoding="utf-8")
            labflow.write_json(
                source_root.parent / "manifest.json",
                {
                    "files": [
                        {
                            "path": "Runtime/Safe.cs",
                            "sha256": labflow.file_sha256(listed),
                        }
                    ]
                },
            )
            errors = labflow.gate5_foundation_source_errors(root)
            self.assertTrue(any("not listed" in item for item in errors))

    def test_legacy_state_cannot_continue_without_migration(self) -> None:
        state = {
            "schema_version": "1.5.0",
            "lab_id": "lab_example",
            "gate_approvals": {str(gate): {"approved": True} for gate in range(6)},
            "evidence": {
                "domain_slice": "passed",
                "domain_compile": "passed",
            },
        }
        with self.assertRaises(RuntimeError):
            labflow.require_current_state_schema(state)

    def test_migrate_state_invalidates_every_gate_and_evidence(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = (
                Path(directory)
                / "EMVR_Blind_BuilderPack"
                / "RebuildWorkspaces"
                / "EMVR_Blind_Rebuild_lab_example"
            )
            root.mkdir(parents=True)
            (root / ".emvr-workspace.json").write_text(
                json.dumps(
                    {
                        "schema_version": "1.1.0",
                        "workflow_mode": "blind-rebuild",
                        "lab_id": "lab_example",
                        "required_master_directory_name": "EMVR_Blind_BuilderPack",
                        "required_directory_name": "EMVR_Blind_Rebuild_lab_example",
                    }
                ),
                encoding="utf-8",
            )
            state = {
                "schema_version": "1.5.0",
                "lab_id": "lab_example",
                "stage": "accepted",
                "gate_approvals": {
                    str(gate): {"approved": True} for gate in range(10)
                },
                "brief_confirmation": {"confirmed": True},
                "design_approval": {"approved": True},
                "evidence": {
                    key: (value if key == "notes" else "passed")
                    for key, value in labflow.default_evidence().items()
                },
            }
            labflow.write_json(labflow.state_path(root, "lab_example"), state)
            args = type("Args", (), {"project_root": root, "lab_id": "lab_example"})()
            self.assertEqual(labflow.command_migrate_state(args), 0)
            migrated = labflow.load_state(root, "lab_example")
            self.assertEqual(migrated["schema_version"], labflow.CURRENT_STATE_SCHEMA)
            self.assertEqual(migrated["gate_approvals"], {})
            self.assertFalse(migrated["brief_confirmation"]["confirmed"])
            self.assertFalse(migrated["design_approval"]["approved"])
            self.assertEqual(labflow.current_gate(migrated), 0)
            for key in labflow.default_evidence():
                if key != "notes":
                    self.assertEqual(migrated["evidence"][key], "pending")

    def test_domain_evidence_requires_foundation_pass_first(self) -> None:
        state = {
            "schema_version": labflow.CURRENT_STATE_SCHEMA,
            "evidence": labflow.default_evidence(),
        }
        with self.assertRaises(RuntimeError):
            labflow.require_gate5_evidence_order(
                state,
                gate5_foundation=None,
                domain_slice="passed",
                domain_compile="passed",
            )
        labflow.require_gate5_evidence_order(
            state,
            gate5_foundation="passed",
            domain_slice=None,
            domain_compile=None,
        )

    def test_gate_five_foundation_evidence_is_invalidated_by_lab_code_change(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            lab_file = root / "UnityProject" / "Assets" / "Scripts" / "Lab.cs"
            lab_file.parent.mkdir(parents=True)
            lab_file.write_text("version one", encoding="utf-8")
            state_path = root / "LabSpecs/lab_example/workflow_state.json"
            state_path.parent.mkdir(parents=True)
            labflow.write_json(state_path, {"lab_id": "lab_example", "generated_files": [lab_file.relative_to(root).as_posix()]})
            state = {
                "lab_id": "lab_example",
                "evidence": labflow.default_evidence(),
            }
            state["evidence"]["gate5_foundation"] = "passed"
            state["evidence"]["gate5_foundation_lab_output_digest"] = (
                labflow.gate5_lab_output_digest(root, "lab_example")
            )
            lab_file.write_text("version two", encoding="utf-8")
            errors = labflow.gate5_evidence_freshness_errors(root, state)
            self.assertTrue(any("stale" in item.lower() for item in errors))

    def test_gate_five_domain_evidence_is_invalidated_by_lab_code_change(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            lab_file = root / "UnityProject" / "Assets" / "Scripts" / "Lab.cs"
            lab_file.parent.mkdir(parents=True)
            lab_file.write_text("version one", encoding="utf-8")
            state_path = root / "LabSpecs/lab_example/workflow_state.json"
            state_path.parent.mkdir(parents=True)
            labflow.write_json(state_path, {"lab_id": "lab_example", "generated_files": [lab_file.relative_to(root).as_posix()]})
            state = {
                "lab_id": "lab_example",
                "evidence": labflow.default_evidence(),
            }
            state["evidence"]["domain_slice"] = "passed"
            state["evidence"]["domain_compile"] = "passed"
            state["evidence"]["gate5_domain_lab_output_digest"] = (
                labflow.gate5_lab_output_digest(root, "lab_example")
            )
            lab_file.write_text("version two", encoding="utf-8")
            errors = labflow.gate5_evidence_freshness_errors(root, state)
            self.assertTrue(any("domain evidence is stale" in item.lower() for item in errors))

    def test_gate_five_evidence_detects_new_unregistered_unity_file(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            existing = root / "UnityProject/Assets/Scripts/Lab.cs"
            existing.parent.mkdir(parents=True)
            existing.write_text("existing", encoding="utf-8")
            state = {
                "lab_id": "lab_example",
                "evidence": labflow.default_evidence(),
            }
            state["evidence"]["gate5_foundation"] = "passed"
            state["evidence"]["gate5_foundation_lab_output_digest"] = (
                labflow.gate5_lab_output_digest(root, "lab_example")
            )
            new_test = root / "UnityProject/Assets/Tests/DomainTests.cs"
            new_test.parent.mkdir(parents=True)
            new_test.write_text("new test", encoding="utf-8")
            errors = labflow.gate5_evidence_freshness_errors(root, state)
            self.assertTrue(any("stale" in item.lower() for item in errors))


if __name__ == "__main__":
    unittest.main()
