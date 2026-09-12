import importlib.util
import json
import shutil
import tempfile
import unittest
from pathlib import Path
from unittest import mock


SCRIPT = (
    Path(__file__).resolve().parents[2]
    / ".agents"
    / "skills"
    / "build-unity-vr-lab-blind"
    / "scripts"
    / "import_approved_asset_closure.py"
)
SPEC = importlib.util.spec_from_file_location("approved_asset_importer", SCRIPT)
assert SPEC and SPEC.loader
IMPORTER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(IMPORTER)


class ApprovedAssetImporterTests(unittest.TestCase):
    @staticmethod
    def write_gate_five_state(root: Path, lab_id: str) -> None:
        state = {
            "schema_version": "2.0.0",
            "lab_id": lab_id,
            "gate_approvals": {
                str(gate): {"approved": True} for gate in range(5)
            },
        }
        state_path = root / "LabSpecs" / lab_id / "workflow_state.json"
        state_path.parent.mkdir(parents=True)
        state_path.write_text(json.dumps(state), encoding="utf-8")

    def test_dependency_conflict_leaves_no_partial_import(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            lab_id = "audit_lab"
            approved = root / "ApprovedAssets"
            source = approved / "Room.prefab"
            dependency = approved / "RoomMaterial.mat"
            dependency_guid = "0123456789abcdef0123456789abcdef"

            approved.mkdir(parents=True)
            source.write_text(
                f"m_Material: {{fileID: 2100000, guid: {dependency_guid}, type: 2}}\n",
                encoding="utf-8",
            )
            Path(str(source) + ".meta").write_text(
                "guid: fedcba9876543210fedcba9876543210\n", encoding="utf-8"
            )
            dependency.write_text("approved material\n", encoding="utf-8")
            Path(str(dependency) + ".meta").write_text(
                f"guid: {dependency_guid}\n", encoding="utf-8"
            )

            self.write_gate_five_state(root, lab_id)

            destination_root = (
                root
                / "UnityProject"
                / "Assets"
                / "Generated"
                / lab_id
                / "ApprovedDependencies"
            )
            conflicting_dependency = destination_root / "RoomMaterial.mat"
            conflicting_dependency.parent.mkdir(parents=True)
            conflicting_dependency.write_text("different material\n", encoding="utf-8")

            with self.assertRaisesRegex(RuntimeError, "Refusing to overwrite"):
                IMPORTER.import_closure(root, lab_id, "Room.prefab", False)

            self.assertFalse((destination_root / "Room.prefab").exists())
            self.assertFalse((destination_root / "Room.prefab.meta").exists())
            self.assertFalse((root / "BuildEvidence").exists())

    def test_copy_failure_rolls_back_asset_and_meta(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            lab_id = "rollback_lab"
            approved = root / "ApprovedAssets"
            source = approved / "Room.prefab"
            approved.mkdir(parents=True)
            source.write_text("room\n", encoding="utf-8")
            Path(str(source) + ".meta").write_text(
                "guid: fedcba9876543210fedcba9876543210\n", encoding="utf-8"
            )
            self.write_gate_five_state(root, lab_id)

            real_copy = IMPORTER.copy_file
            call_count = 0

            def fail_second_copy(src: Path, dst: Path) -> None:
                nonlocal call_count
                call_count += 1
                if call_count == 2:
                    IMPORTER.ensure_directory(dst.parent)
                    with open(IMPORTER.native_path(dst), "wb") as stream:
                        stream.write(b"partial")
                    raise OSError("injected copy failure")
                real_copy(src, dst)

            with mock.patch.object(IMPORTER, "copy_file", side_effect=fail_second_copy):
                with self.assertRaisesRegex(RuntimeError, "rolled back"):
                    IMPORTER.import_closure(root, lab_id, "Room.prefab", False)

            destination = (
                root
                / "UnityProject"
                / "Assets"
                / "Generated"
                / lab_id
                / "ApprovedDependencies"
                / "Room.prefab"
            )
            self.assertFalse(IMPORTER.fs_exists(destination))
            self.assertFalse(IMPORTER.fs_exists(Path(str(destination) + ".meta")))
            self.assertFalse((root / "BuildEvidence").exists())

    def test_matching_uncommitted_files_are_adopted_on_retry(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            lab_id = "retry_lab"
            approved = root / "ApprovedAssets"
            source = approved / "Room.prefab"
            approved.mkdir(parents=True)
            source.write_text("room\n", encoding="utf-8")
            source_meta = Path(str(source) + ".meta")
            source_meta.write_text(
                "guid: fedcba9876543210fedcba9876543210\n", encoding="utf-8"
            )
            self.write_gate_five_state(root, lab_id)

            destination = (
                root
                / "UnityProject"
                / "Assets"
                / "Generated"
                / lab_id
                / "ApprovedDependencies"
                / "Room.prefab"
            )
            IMPORTER.copy_file(source, destination)
            IMPORTER.copy_file(source_meta, Path(str(destination) + ".meta"))

            payload = IMPORTER.import_closure(root, lab_id, "Room.prefab", False)

            self.assertEqual(1, payload["asset_count"])
            self.assertTrue(IMPORTER.fs_exists(destination))
            self.assertTrue((root / payload["manifest"]).is_file())

    def test_failed_overlapping_import_preserves_committed_shared_dependency(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            lab_id = "overlap_lab"
            approved = root / "ApprovedAssets"
            first = approved / "First.prefab"
            second = approved / "Second.prefab"
            shared = approved / "Shared.mat"
            shared_guid = "0123456789abcdef0123456789abcdef"

            approved.mkdir(parents=True)
            shared.write_text("shared material\n", encoding="utf-8")
            Path(str(shared) + ".meta").write_text(
                f"guid: {shared_guid}\n", encoding="utf-8"
            )
            for index, source in enumerate((first, second), start=1):
                source.write_text(
                    f"m_Material: {{fileID: 2100000, guid: {shared_guid}, type: 2}}\n",
                    encoding="utf-8",
                )
                Path(str(source) + ".meta").write_text(
                    f"guid: {index:032x}\n", encoding="utf-8"
                )
            self.write_gate_five_state(root, lab_id)

            first_payload = IMPORTER.import_closure(root, lab_id, "First.prefab", False)
            shared_record = next(
                record
                for record in first_payload["assets"]
                if record["source"].endswith("Shared.mat")
            )
            shared_destination = root / shared_record["destination"]
            shared_meta_destination = Path(str(shared_destination) + ".meta")

            def fail_copy(src: Path, dst: Path) -> None:
                IMPORTER.ensure_directory(dst.parent)
                with open(IMPORTER.native_path(dst), "wb") as stream:
                    stream.write(b"partial")
                raise OSError("injected overlapping import failure")

            with mock.patch.object(IMPORTER, "copy_file", side_effect=fail_copy):
                with self.assertRaisesRegex(RuntimeError, "rolled back"):
                    IMPORTER.import_closure(root, lab_id, "Second.prefab", False)

            self.assertTrue(IMPORTER.fs_exists(shared_destination))
            self.assertTrue(IMPORTER.fs_exists(shared_meta_destination))
            self.assertEqual(IMPORTER.sha256(shared), IMPORTER.sha256(shared_destination))
            self.assertEqual(
                IMPORTER.sha256(Path(str(shared) + ".meta")),
                IMPORTER.sha256(shared_meta_destination),
            )

    def test_script_import_includes_assembly_definition_and_upgrades_manifest(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            lab_id = "assembly_lab"
            approved = root / "ApprovedAssets"
            source = approved / "Starter" / "Rig.prefab"
            script = approved / "Starter" / "Scripts" / "RigController.cs"
            script_guid = "0123456789abcdef0123456789abcdef"

            script.parent.mkdir(parents=True)
            source.write_text(
                f"m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}\n",
                encoding="utf-8",
            )
            Path(str(source) + ".meta").write_text(
                "guid: fedcba9876543210fedcba9876543210\n", encoding="utf-8"
            )
            script.write_text("public sealed class RigController {}\n", encoding="utf-8")
            Path(str(script) + ".meta").write_text(
                f"guid: {script_guid}\n", encoding="utf-8"
            )
            self.write_gate_five_state(root, lab_id)

            original = IMPORTER.import_closure(root, lab_id, "Starter/Rig.prefab", False)
            self.assertEqual(2, original["asset_count"])

            asmdef = approved / "Starter" / "Starter.Runtime.asmdef"
            asmdef.write_text('{"name":"Starter.Runtime"}\n', encoding="utf-8")
            Path(str(asmdef) + ".meta").write_text(
                "guid: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\n", encoding="utf-8"
            )

            upgraded = IMPORTER.import_closure(root, lab_id, "Starter/Rig.prefab", False)

            self.assertEqual(3, upgraded["asset_count"])
            sources = {record["source"] for record in upgraded["assets"]}
            self.assertIn("ApprovedAssets/Starter/Starter.Runtime.asmdef", sources)
            destination = (
                root
                / "UnityProject"
                / "Assets"
                / "Generated"
                / lab_id
                / "ApprovedDependencies"
                / "Starter"
                / "Starter.Runtime.asmdef"
            )
            self.assertTrue(IMPORTER.fs_exists(destination))
            committed = json.loads((root / upgraded["manifest"]).read_text(encoding="utf-8"))
            self.assertEqual(3, committed["asset_count"])

    def test_asmref_boundary_imports_its_referenced_asmdef(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            lab_id = "asmref_lab"
            approved = root / "ApprovedAssets"
            source = approved / "Feature" / "Feature.prefab"
            script = approved / "Feature" / "Scripts" / "FeatureController.cs"
            asmref = approved / "Feature" / "Feature.asmref"
            asmdef = approved / "Definitions" / "Shared.Runtime.asmdef"
            script_guid = "0123456789abcdef0123456789abcdef"
            asmdef_guid = "abcdef0123456789abcdef0123456789"

            script.parent.mkdir(parents=True)
            asmdef.parent.mkdir(parents=True)
            source.write_text(
                f"m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}\n",
                encoding="utf-8",
            )
            Path(str(source) + ".meta").write_text(
                "guid: fedcba9876543210fedcba9876543210\n", encoding="utf-8"
            )
            script.write_text("public sealed class FeatureController {}\n", encoding="utf-8")
            Path(str(script) + ".meta").write_text(
                f"guid: {script_guid}\n", encoding="utf-8"
            )
            asmref.write_text(
                f'{{"reference":"GUID:{asmdef_guid}"}}\n', encoding="utf-8"
            )
            Path(str(asmref) + ".meta").write_text(
                "guid: bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb\n", encoding="utf-8"
            )
            asmdef.write_text('{"name":"Shared.Runtime"}\n', encoding="utf-8")
            Path(str(asmdef) + ".meta").write_text(
                f"guid: {asmdef_guid}\n", encoding="utf-8"
            )
            self.write_gate_five_state(root, lab_id)

            payload = IMPORTER.import_closure(root, lab_id, "Feature/Feature.prefab", False)

            sources = {record["source"] for record in payload["assets"]}
            self.assertIn("ApprovedAssets/Feature/Feature.asmref", sources)
            self.assertIn("ApprovedAssets/Definitions/Shared.Runtime.asmdef", sources)

    def test_windows_max_path_is_bypassed_for_generated_destination(self) -> None:
        directory = tempfile.mkdtemp()
        root = Path(directory)
        try:
            lab_id = "long_path_lab"
            approved = root / "ApprovedAssets"
            parts: list[str] = []
            while len(str((approved / Path(*parts) / "Room.prefab.meta").resolve())) < 220:
                parts.append(f"dependency_folder_{len(parts):02d}")
            relative = Path(*parts) / "Room.prefab"
            source = approved / relative
            self.assertLess(len(str(Path(str(source) + ".meta").resolve())), 260)
            source.parent.mkdir(parents=True)
            source.write_text("room\n", encoding="utf-8")
            Path(str(source) + ".meta").write_text(
                "guid: fedcba9876543210fedcba9876543210\n", encoding="utf-8"
            )
            self.write_gate_five_state(root, lab_id)

            destination = (
                root
                / "UnityProject"
                / "Assets"
                / "Generated"
                / lab_id
                / "ApprovedDependencies"
                / relative
            )
            self.assertGreater(len(str(destination.resolve())), 260)

            payload = IMPORTER.import_closure(root, lab_id, relative.as_posix(), False)

            self.assertTrue(IMPORTER.fs_exists(destination))
            self.assertEqual(IMPORTER.sha256(source), IMPORTER.sha256(destination))
            self.assertTrue((root / payload["manifest"]).is_file())
        finally:
            shutil.rmtree(IMPORTER.native_path(root), ignore_errors=True)


if __name__ == "__main__":
    unittest.main()
