import importlib.util
import json
import tempfile
import unittest
from pathlib import Path


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

            state = {
                "schema_version": "1.7.0",
                "lab_id": lab_id,
                "gate_approvals": {
                    str(gate): {"approved": True} for gate in range(5)
                },
            }
            state_path = root / "LabSpecs" / lab_id / "workflow_state.json"
            state_path.parent.mkdir(parents=True)
            state_path.write_text(json.dumps(state), encoding="utf-8")

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


if __name__ == "__main__":
    unittest.main()
