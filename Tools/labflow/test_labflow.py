from __future__ import annotations

import argparse
import copy
import importlib.util
import json
import os
import shutil
import tempfile
import unittest
from pathlib import Path, PurePosixPath, PureWindowsPath
from unittest import mock


MODULE_PATH = Path(__file__).with_name("labflow.py")
SPEC = importlib.util.spec_from_file_location("labflow_under_test", MODULE_PATH)
assert SPEC and SPEC.loader
labflow = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(labflow)


def make_source_reference_fixture(root: Path, lab_id: str = "lab_example") -> dict:
    sources = []
    for source_id in labflow.REQUIRED_SOURCE_REFERENCE_IDS:
        relative = f"Sources/{source_id}"
        (root / relative).mkdir(parents=True, exist_ok=True)
        sources.append(
            {
                "source_id": source_id,
                "source_kind": "directory",
                "path": relative,
                "access": "read_only",
                "transfer_mode": "reference_only",
            }
        )
    catalog = {
        "schema_version": labflow.SOURCE_REFERENCE_SCHEMA,
        "reference_base": "builder_pack_root",
        "path_format": "repository_relative_posix",
        "absolute_paths_persisted": False,
        "source_access": "read_only",
        "integrity_enforcement": ["per_lab_reference_manifest_sha256"],
        "sources": sources,
    }
    labflow.write_json(root / labflow.SOURCE_REFERENCE_CATALOG, catalog)
    metadata = labflow.ensure_source_reference_manifest(root, lab_id)
    state = {
        "schema_version": labflow.CURRENT_STATE_SCHEMA,
        "lab_id": lab_id,
        "source_references": metadata,
    }
    return state


def make_scaffold_fixture(root: Path) -> dict:
    """Create an isolated Gate 4 test run with real reference/baseline locks."""
    state = make_source_reference_fixture(root)
    state.update(
        workflow_mode="integrated-development",
        scene="Assets/Scenes/LabExample.unity",
        stage="design_approved",
        gate_approvals={},
        evidence=labflow.default_evidence(),
        protected_baseline=None,
        design_approval={"approved": True},
    )
    labflow.write_text(root / "Tools/labflow/labflow.py", "# fixture tool\n")
    state["evidence"]["boundary_verifier"] = "passed"
    with mock.patch.object(labflow, "foundation_errors", return_value=[]):
        labflow.capture_gate0_protected_baseline(root, "lab_example", state)
    state["gate_approvals"] = {
        str(gate): {"approved": True, "note": "unit-test fixture"}
        for gate in range(4)
    }
    labflow.save_state(root, "lab_example", state)
    return state


def add_maintenance_fixture(root: Path, state: dict) -> Path:
    """Model explicit maintainer issuance, never automatic drift acceptance."""
    baseline = labflow.read_json(root / state["protected_baseline"]["path"])
    effective, errors = labflow.maintained_protected_files(root, state, baseline["files"])
    assert not errors
    previous = state.get("protected_maintenance", [])
    sequence = len(previous) + 1
    relative = "Tools/labflow/labflow.py"
    old = effective[relative]
    (root / relative).write_text(f"# maintenance {sequence}\n", encoding="utf-8")
    path = root / labflow.protected_maintenance_relative_path(state["lab_id"], sequence)
    receipt = {
        "schema_version": labflow.PROTECTED_MAINTENANCE_SCHEMA,
        "profile": labflow.PROTECTED_MAINTENANCE_PROFILE,
        "lab_id": state["lab_id"],
        "sequence": sequence,
        "baseline_sha256": state["protected_baseline"]["sha256"],
        "previous_receipt_sha256": previous[-1]["sha256"] if previous else state["protected_baseline"]["sha256"],
        "source_references_sha256": state["source_references"]["sha256"],
        "reviewed_state_sha256": labflow.file_sha256(labflow.state_path(root, state["lab_id"])),
        "applied_at_gate": 4,
        "authorization_note": "Unit-test maintainer authorization fixture",
        "reason": "Test a reviewed tooling replacement",
        "validation_summary": "Isolated test fixture",
        "created_at": labflow.utc_now(),
        "changes": [{"path": relative, "before_sha256": old["sha256"], "before_size": old["size"], "after_sha256": labflow.file_sha256(root / relative), "after_size": (root / relative).stat().st_size}],
    }
    labflow.write_json(path, receipt)
    state["protected_maintenance"] = previous + [{"path": path.relative_to(root).as_posix(), "sha256": labflow.file_sha256(path)}]
    labflow.save_state(root, state["lab_id"], state)
    return path


class LabflowTests(unittest.TestCase):
    def full_lighting_fixture(self, root):
        files = []
        for relative in sorted(labflow.LIGHTING_ADDITIONS - {"Policies/emvr-room-lighting.json"}):
            labflow.write_text(root / relative, "immutable lighting fixture\n")
            files.append({"path": relative, "sha256": labflow.file_sha256(root / relative), "size": (root / relative).stat().st_size})
        profile = {"access": "read_only", "reference_base": "builder_pack_root", "path_format": "repository_relative_posix",
                   "source_files": files, "render_settings": {"m_AmbientIntensity": 1, "m_AmbientSkyColor": {"r": 0.1, "g": 0.2, "b": 0.3, "a": 1}},
                   "settings_guid": "a" * 32, "settings_file_id": 42, "historical_lighting_data": {"guid": "b" * 32}}
        labflow.write_json(root / "Policies/emvr-room-lighting.json", profile)
        labflow.write_json(root / "Policies/pack-inventory.json", {"sections": {"EMVRRoomLighting": {
            "path": "Policies/emvr-room-lighting.json", "sha256": labflow.file_sha256(root / "Policies/emvr-room-lighting.json")}}})
        return profile

    def test_full_lighting_sources_archive_and_profile_are_immutable(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            profile = self.full_lighting_fixture(root)
            self.assertEqual(labflow.room_lighting_profile_errors(root), [])
            for entry in profile["source_files"]:
                path = root / entry["path"]
                original = path.read_bytes()
                path.write_bytes(original + b"drift")
                self.assertTrue(labflow.room_lighting_profile_errors(root))
                path.write_bytes(original)
            path = root / "Policies/emvr-room-lighting.json"
            path.write_text(path.read_text(encoding="utf-8") + " ", encoding="utf-8")
            self.assertTrue(labflow.room_lighting_profile_errors(root))

    def test_lighting_release_rejects_crlf_even_with_matching_frozen_hashes(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            profile = self.full_lighting_fixture(root)
            relative = "ApprovedAssets/EMVRRoom/Lighting/RoomLighting.lighting"
            path = root / relative
            self.assertEqual(labflow.room_lighting_profile_errors(root), [])
            path.write_bytes(path.read_bytes().replace(b"\n", b"\r\n"))
            entry = next(e for e in profile["source_files"] if e["path"] == relative)
            entry.update(sha256=labflow.file_sha256(path), size=path.stat().st_size)
            profile_path = root / "Policies/emvr-room-lighting.json"
            labflow.write_json(profile_path, profile, force=True)
            inventory_path = root / "Policies/pack-inventory.json"
            inventory = labflow.read_json(inventory_path)
            inventory["sections"]["EMVRRoomLighting"]["sha256"] = labflow.file_sha256(profile_path)
            labflow.write_json(inventory_path, inventory, force=True)
            self.assertTrue(any("LF line endings" in e for e in labflow.room_lighting_profile_errors(root)))

    def test_full_lighting_requires_original_settings_and_current_scene_data(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self.full_lighting_fixture(root)
            state = {"lab_id": "lab_example"}
            scene = ("--- !u!104 &2\nRenderSettings:\n  m_AmbientIntensity: 1\n"
                     "  m_AmbientSkyColor: {r: 0.1, g: 0.2, b: 0.3, a: 1}\n"
                     "--- !u!157 &3\nLightmapSettings:\n"
                     f"  m_LightingSettings: {{fileID: 42, guid: {'a' * 32}, type: 2}}\n"
                     f"  m_LightingDataAsset: {{fileID: 112000000, guid: {'c' * 32}, type: 2}}\n")
            self.assertTrue(labflow.full_room_lighting_scene_errors(root, state, scene))
            data = root / "UnityProject/Assets/Scenes/lab_example/LightingData.asset"
            labflow.write_text(data, "fixture representing current-scene output")
            labflow.write_text(Path(str(data) + ".meta"), "guid: " + "c" * 32)
            self.assertEqual(labflow.full_room_lighting_scene_errors(root, state, scene), [])
            # Saving a successfully generated scene makes it newer than its data.
            saved = root / "UnityProject/Assets/Scenes/lab_example.unity"
            labflow.write_text(saved, scene)
            os.utime(data, (1000000000, 1000000000))
            os.utime(saved, (1100000000, 1100000000))
            self.assertLess(data.stat().st_mtime, saved.stat().st_mtime)
            self.assertEqual(labflow.full_room_lighting_scene_errors(root, state,
                saved.read_text(encoding="utf-8")), [])
            # Data existence (even newer) cannot compensate for a null scene reference.
            os.utime(data, (1200000000, 1200000000))
            null_scene = scene.replace("fileID: 112000000, guid: " + "c" * 32 + ", type: 2", "fileID: 0")
            self.assertEqual(labflow.full_room_lighting_scene_errors(root, state, null_scene),
                ["Window > Rendering > Lighting > Generate Lighting, wait, then save."])
            for old, new in (("a" * 32, "d" * 32), ("c" * 32, "b" * 32), ("c" * 32, "e" * 32),
                             ("fileID: 112000000", "fileID: 0"), ("m_AmbientIntensity: 1", "m_AmbientIntensity: 2"),
                             ("r: 0.1", "r: nan")):
                self.assertTrue(labflow.full_room_lighting_scene_errors(root, state, scene.replace(old, new)))
            self.assertTrue(labflow.full_room_lighting_scene_errors(root, {"lab_id": "other_lab"}, scene))

    def test_required_full_lighting_profile_cannot_be_omitted(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            labflow.write_json(root / "Policies/reuse-allowlist.json", {"mandatory_assets": ["ApprovedAssets/EMVRRoom/Lighting/RoomLighting.lighting"]})
            self.assertTrue(labflow.room_lighting_profile_errors(root))

    def test_execution_lighting_requires_source_binding_and_unchanged_light_values(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            profile = self.full_lighting_fixture(root)
            profile['settings_asset'] = 'ApprovedAssets/EMVRRoom/Lighting/RoomLighting.lighting'
            profile['execution_settings'] = {'mode': 'native_on_demand'}
            profile['lighting_settings'] = {'serializedVersion': 6, 'm_Name': 'original',
                'm_GIWorkflowMode': 1, 'm_EnableRealtimeLightmaps': 1, 'm_EnableBakedLightmaps': 0,
                'm_CompAOExponent': 6, 'm_LightmapParameters': {'fileID': 15204, 'guid': '0' * 32, 'type': 0}}
            path = root / 'UnityProject/Assets/Scenes/lab_example/RoomLighting_Execution.lighting'
            reference = [None, '1234', 'd' * 32]
            self.assertTrue(labflow.execution_lighting_errors(root, 'lab_example', profile, reference))
            native = ('--- !u!850595691 &1234\nLightingSettings:\n  serializedVersion: 7\n'
                      '  m_Name: RoomLighting_Execution\n  m_GIWorkflowMode: 1\n'
                      '  m_EnableRealtimeLightmaps: 1\n  m_EnableBakedLightmaps: 0\n'
                      '  m_CompAOExponent: 6\n  m_LightmapParameters: {fileID: 15204, guid: ' + '0' * 32 + ',\n    type: 0}\n')
            labflow.write_text(path, native)
            labflow.write_text(Path(str(path) + '.meta'), 'guid: ' + 'd' * 32 + '\n')
            source = next(e for e in profile['source_files'] if e['path'] == profile['settings_asset'])
            provenance = dict(source_path=profile['settings_asset'], source_sha256=source['sha256'],
                              source_guid='a' * 32, execution_guid='d' * 32)
            evidence = Path(str(path) + '.provenance.json')
            labflow.write_json(evidence, provenance)
            self.assertEqual(labflow.execution_lighting_errors(root, 'lab_example', profile, reference), [])
            path.write_bytes(native.replace('\n', '\r\n').encode())
            self.assertEqual(labflow.execution_lighting_errors(root, 'lab_example', profile, reference), [])
            for old, new in [('m_GIWorkflowMode: 1', 'm_GIWorkflowMode: 0'),
                             ('m_EnableRealtimeLightmaps: 1', 'm_EnableRealtimeLightmaps: 0'),
                             ('m_CompAOExponent: 6', 'm_CompAOExponent: 7'),
                             ('m_CompAOExponent: 6', 'm_CompAOExponent: nan'),
                             ('m_CompAOExponent: 6', 'm_CompAOExponent: 6\n  m_CompAOExponent: 6'),
                             ('fileID: 15204', 'fileID: 15205')]:
                path.write_text(native.replace(old, new), encoding='utf-8')
                self.assertTrue(labflow.execution_lighting_errors(root, 'lab_example', profile, reference))
            path.write_text(native, encoding='utf-8')
            for wrong in [None, [None, '0', 'd' * 32], [None, '1234', 'a' * 32]]:
                self.assertTrue(labflow.execution_lighting_errors(root, 'lab_example', profile, wrong))
            for key in provenance:
                bad = dict(provenance, **{key: 'wrong'})
                labflow.write_json(evidence, bad, force=True)
                self.assertTrue(labflow.execution_lighting_errors(root, 'lab_example', profile, reference))

    def xr_defaults_fixture(self):
        blocks = []
        for index, (name, guid, transform_id, _) in enumerate(labflow.XR_DEFAULT_SOURCES):
            blocks.append(f"--- !u!1001 &{index + 100}\nPrefabInstance:\n  m_Modification:\n"
                          f"    - target: {{fileID: {transform_id}, guid: {guid},\n        type: 3}}\n"
                          "      propertyPath: m_LocalPosition.z\n      value: -2.5\n"
                          "      objectReference: {fileID: 0}\n"
                          "    m_RemovedComponents: []\n    m_RemovedGameObjects: []\n"
                          "    m_AddedComponents: []\n    m_AddedGameObjects: []\n"
                          f"  m_SourcePrefab: {{fileID: 100100000, guid: {guid}, type: 3}}\n")
        blocks.append("--- !u!114 &200\nMonoBehaviour:\n  m_Enabled: 1\n"
                      "  m_Script: {fileID: 11500000, guid: e2bbcbd14c6b470187c4ca91c6d1759e, type: 3}\n"
                      "  sphereCastRadius: 0.06\n  minimumRayDistance: 20\n"
                      "  controllerRaysOnly: 1\n  enableMouseLeftDebugSelect: 0\n")
        blocks.append("--- !u!114 &201\nMonoBehaviour:\n  m_Enabled: 1\n"
                      "  m_Script: {fileID: 11500000, guid: 89ed2d21dd9de8f40841aeb37dd363ff, type: 3}\n"
                      "  showPointerRay: 0\n")
        return blocks

    def test_xr_requires_complete_instances_and_default_common_components(self):
        blocks = self.xr_defaults_fixture()
        scene = "".join(blocks)
        self.assertEqual(labflow.xr_scene_default_errors(scene), [])
        self.assertEqual(labflow.xr_scene_default_errors(scene.replace("\n", "\r\n")), [])
        for i in range(4):
            self.assertTrue(labflow.xr_scene_default_errors("".join(blocks[:i] + blocks[i+1:])))
            self.assertTrue(labflow.xr_scene_default_errors(scene + blocks[i]))
        for old, new in (("showPointerRay: 0", "showPointerRay: 1"),
                         ("enableMouseLeftDebugSelect: 0", "enableMouseLeftDebugSelect: 1"),
                         ("minimumRayDistance: 20", "minimumRayDistance: nan"),
                         ("m_Enabled: 1", "m_Enabled: 0")):
            self.assertTrue(labflow.xr_scene_default_errors(scene.replace(old, new)))

    def test_xr_rejects_child_origin_binding_scale_and_content_changes(self):
        scene = "".join(self.xr_defaults_fixture())
        for field in ("m_RayOriginTransform", "m_LineOriginTransform", "m_KeyboardYTranslateAction", "m_LocalScale.x", "m_IsActive"):
            altered = scene.replace("m_LocalPosition.z", field).replace("value: -2.5", "value: 0")
            self.assertTrue(labflow.xr_scene_default_errors(altered), field)
        for field in ("m_RemovedComponents", "m_RemovedGameObjects", "m_AddedComponents", "m_AddedGameObjects"):
            self.assertTrue(labflow.xr_scene_default_errors(scene.replace(field + ": []", field + ":\n    - {fileID: 42}")))
        altered = scene.replace("fileID: 4134505312059479865", "fileID: 42", 1)
        self.assertTrue(labflow.xr_scene_default_errors(altered))
        extra = "--- !u!114 &400\nMonoBehaviour:\n  m_Script: {fileID: 11500000, guid: 6803edce0201f574f923fd9d10e5b30a, type: 3}\n"
        self.assertTrue(labflow.xr_scene_default_errors(scene + extra))
        self.assertEqual(labflow.xr_scene_default_errors(scene + extra.replace("&400", "&400 stripped")), [])

    def test_xr_duplicate_guard_allows_composition_but_rejects_reimplementation_patterns(self):
        for code in ("class XRDeviceSimulator {}", "rig.AddComponent<XRRayInteractor>();",
                     "ray.rayOriginTransform = camera.transform;", "visual.lineOriginOffset = 1;",
                     "InputSystem.QueueStateEvent(device, state);", "rig.AddComponent<Unity.XR.CoreUtils.XROrigin>();"):
            self.assertTrue(labflow.xr_duplicate_implementation_errors(code), code)
        for code in ("var tuner = systems.AddComponent<EmVrXrRayRuntimeTuner>();",
                     "tuner.sphereCastRadius = 0.06f;", "var origin = ray.rayOriginTransform;",
                     '// ray.rayOriginTransform = camera;\nvar x = "InputSystem.AddDevice(foo)";'):
            self.assertEqual(labflow.xr_duplicate_implementation_errors(code), [])

    def room_lighting_fixture(self):
        return ("--- !u!104 &2\nRenderSettings:\n"
                "  m_AmbientMode: 3\n  m_AmbientIntensity: 1\n"
                "  m_AmbientSkyColor: {r: 0.13116321, g: 0.15650724, b: 0.212264115, a: 1}\n"
                "  m_AmbientEquatorColor: {r: 0.1400303, g: 0.21646784, b: 0.27205884, a: 1}\n"
                "  m_AmbientGroundColor: {r: 0.048010383, g: 0.112683475, b: 0.17647058, a: 1}\n"
                "  m_DefaultReflectionMode: 1\n  m_ReflectionIntensity: 1\n  m_Fog: 0\n"
                "  m_SkyboxMaterial: {fileID: 0}\n  m_CustomReflection: {fileID: 0}\n  m_Sun: {fileID: 0}\n")

    def test_room_light_overrides_cannot_fall_back_to_prefab_or_change_intensity(self):
        modules, blocks = self.full_room_fixture()
        modules[2]["overrides"] = [{"file_id": n, "property": "m_Intensity", "value": 5}
                                   for n in (10820366, 10822286, 10830590, 10897880)]
        overrides = [f"    - target: {{fileID: {s['file_id']}, guid: {modules[2]['guid']}, type: 3}}\n"
                     "      propertyPath: m_Intensity\n      value: 5\n      objectReference: {fileID: 0}\n"
                     for s in modules[2]["overrides"]]
        def scene(values):
            parts = blocks.copy()
            parts[2] = parts[2].replace("    m_RemovedComponents", "".join(values) + "    m_RemovedComponents")
            return "".join(parts)
        self.assertEqual(labflow.room_prefab_instance_errors(scene(overrides), modules), [])
        for index in range(4):
            for value in ("1.5", "0", "nan", "invalid"):
                altered = overrides.copy()
                altered[index] = altered[index].replace("value: 5", "value: " + value)
                self.assertTrue(labflow.room_prefab_instance_errors(scene(altered), modules))
            self.assertTrue(labflow.room_prefab_instance_errors(scene(overrides[:index] + overrides[index + 1:]), modules))
        self.assertTrue(labflow.room_prefab_instance_errors(scene(overrides + overrides[:1]), modules))

    def test_room_environment_accepts_brighter_defaults_and_rejects_old_scene_or_drift(self):
        scene = self.room_lighting_fixture()
        self.assertEqual(labflow.room_environment_lighting_errors(scene), [])
        self.assertEqual(labflow.room_environment_lighting_errors(scene.replace("\n", "\r\n")), [])
        for old, new in (("m_AmbientMode: 3", "m_AmbientMode: 0"),
                         ("m_AmbientIntensity: 1", "m_AmbientIntensity: 2"),
                         ("m_ReflectionIntensity: 1", "m_ReflectionIntensity: nan"),
                         ("m_DefaultReflectionMode: 1", "m_DefaultReflectionMode: 0"),
                         ("m_Fog: 0", "m_Fog: 1"),
                         ("r: 0.13116321", "r: 0.5"), ("a: 1", "a: 0"),
                         ("r: 0.13116321, g: 0.15650724, b: 0.212264115",
                          "r: 0.08744214, g: 0.10433816, b: 0.14150941"),
                         ("m_SkyboxMaterial: {fileID: 0}", "m_SkyboxMaterial: {fileID: 42}"),
                         ("m_CustomReflection: {fileID: 0}", ""), ("m_Sun: {fileID: 0}", "")):
            with self.subTest(field=old):
                self.assertTrue(labflow.room_environment_lighting_errors(scene.replace(old, new)))
        self.assertTrue(labflow.room_environment_lighting_errors(""))
        self.assertTrue(labflow.room_environment_lighting_errors(scene + scene))
        self.assertTrue(labflow.room_environment_lighting_errors(scene + "--- !u!108 &42\nLight:\n  m_Type: 1\n"))

    def full_room_fixture(self):
        modules = [{"guid": str(n) * 32, "prefab": f"ApprovedAssets/EMVRRoom/Prefabs/Room_Big_Part_{n:02}.prefab"}
                   for n in (1, 2, 3, 2, 1)]
        blocks = [f"--- !u!1001 &{i + 100}\nPrefabInstance:\n  m_Modification:\n"
                  "    m_RemovedComponents: []\n    m_RemovedGameObjects: []\n"
                  "    m_AddedComponents: []\n    m_AddedGameObjects: []\n"
                  f"  m_SourcePrefab: {{fileID: 100100000, guid: {m['guid']}, type: 3}}\n"
                  for i, m in enumerate(modules)]
        return modules, blocks

    def test_full_room_requires_exact_linked_instance_counts(self):
        modules, blocks = self.full_room_fixture()
        self.assertEqual(labflow.room_prefab_instance_errors("".join(blocks), modules), [])
        for broken in (blocks[:1], blocks[:2], blocks[:-1], blocks + blocks[:1],
                       [b.replace("!u!1001", "!u!1") for b in blocks]):
            self.assertTrue(labflow.room_prefab_instance_errors("".join(broken), modules))

    def test_readonly_room_rejects_removed_added_disabled_or_scaled_content(self):
        modules, blocks = self.full_room_fixture()
        scene = "".join(blocks)
        for field in ("m_RemovedComponents", "m_RemovedGameObjects", "m_AddedComponents", "m_AddedGameObjects"):
            with self.subTest(field=field):
                broken = scene.replace(field + ": []", field + ":\n    - {fileID: 42}", 1)
                self.assertTrue(labflow.room_prefab_instance_errors(broken, modules))
        for field, value in (("m_IsActive", "0"), ("m_Enabled", "0"), ("m_LocalScale.x", "0"),
                             ("m_LocalScale.y", "2"), ("m_LocalScale.z", "-1")):
            with self.subTest(field=field, value=value):
                modification = f"    - propertyPath: {field}\n      value: {value}\n"
                broken = scene.replace("    m_RemovedComponents", modification + "    m_RemovedComponents", 1)
                self.assertTrue(labflow.room_prefab_instance_errors(broken, modules))

    def test_room_scene_check_reads_saved_scene_and_refuses_missing_scene(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            state = {"lab_id": "lab_example", "scene": "Assets/Scenes/lab_example.unity"}
            modules, blocks = self.full_room_fixture()
            labflow.write_json(root / "Policies/emvr-room-assembly.json", {"modules": modules})
            self.assertTrue(labflow.room_scene_reuse_errors(root, state))
            path = root / "UnityProject" / state["scene"]
            labflow.write_text(path, self.room_lighting_fixture() + "".join(blocks))
            before = path.read_bytes()
            self.assertEqual(labflow.room_scene_reuse_errors(root, state), [])
            self.assertEqual(path.read_bytes(), before)

    def test_record_foundation_cannot_pass_with_incomplete_room(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            state = make_scaffold_fixture(root)
            state["stage"] = "scaffold_compiled"
            state["gate_approvals"]["4"] = {"approved": True}
            state["evidence"]["scaffold_compile"] = "passed"
            labflow.save_state(root, "lab_example", state)
            modules, blocks = self.full_room_fixture()
            labflow.write_json(root / "Policies/emvr-room-assembly.json", {"modules": modules})
            labflow.write_text(root / "UnityProject" / state["scene"], "".join(blocks[:1]))
            state_path = labflow.state_path(root, "lab_example")
            before = state_path.read_bytes()
            args = argparse.Namespace(project_root=root, lab_id="lab_example", note="fixture observation",
                                      boundary_verifier=None, scaffold_compile=None, gate5_foundation="passed",
                                      domain_slice=None, domain_compile=None, compile=None, scene_built=None, playtest=None)
            with mock.patch.object(labflow, "gate5_materialization_errors", return_value=[]), mock.patch.object(labflow, "approved_asset_import_errors", return_value=[]):
                with self.assertRaisesRegex(RuntimeError, "Complete room requires"):
                    labflow.command_record(args)
            self.assertEqual(state_path.read_bytes(), before)

    def source_expansion_fixture(self, root):
        (root / "ApprovedAssets").mkdir()
        state = make_scaffold_fixture(root)
        receipt_path = add_maintenance_fixture(root, state)
        receipt = labflow.read_json(receipt_path)
        receipt.update(profile=labflow.EMVRLABS_ROOM_PROFILE, applied_at_gate=5)
        relative = "ApprovedAssets/EMVRRoom/Prefabs/Room_Big_Part_02.prefab"
        asset = root / relative
        labflow.write_text(asset, "reviewed module bytes\n")
        receipt["changes"].append({"path": relative, "before_sha256": None, "before_size": None,
                                   "after_sha256": labflow.file_sha256(asset), "after_size": asset.stat().st_size})
        labflow.write_json(receipt_path, receipt, force=True)
        state["protected_maintenance"][-1]["sha256"] = labflow.file_sha256(receipt_path)
        return state, receipt_path, asset

    def test_room_source_additions_preserve_baseline_and_remain_protected(self):
        for defect in (None, "changed", "removed", "extra"):
            with self.subTest(defect=defect), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                state, receipt, asset = self.source_expansion_fixture(root)
                original_sha = state["protected_baseline"]["sha256"]
                self.assertEqual(labflow.protected_baseline_errors(root, state), [])
                if defect == "changed":
                    asset.write_text("drift", encoding="utf-8")
                elif defect == "removed":
                    asset.unlink()
                elif defect == "extra":
                    (asset.parent / "unreviewed.prefab").write_text("extra", encoding="utf-8")
                if defect:
                    self.assertTrue(labflow.protected_baseline_errors(root, state))
                self.assertEqual(labflow.file_sha256(root / state["protected_baseline"]["path"]), original_sha)

    def test_room_source_additions_reject_unapproved_paths_or_fake_prior_bytes(self):
        for defect in ("path", "old_profile", "fake_prior", "missing_absence"):
            with self.subTest(defect=defect), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                state, path, asset = self.source_expansion_fixture(root)
                receipt = labflow.read_json(path)
                change = receipt["changes"][-1]
                if defect == "path":
                    change["path"] = "ApprovedAssets/unreviewed.prefab"
                elif defect == "old_profile":
                    receipt["profile"] = labflow.ROOM_MAINTENANCE_PROFILE
                elif defect == "fake_prior":
                    change["before_sha256"] = "A" * 64
                else:
                    del change["before_size"]
                labflow.write_json(path, receipt, force=True)
                state["protected_maintenance"][-1]["sha256"] = labflow.file_sha256(path)
                self.assertTrue(labflow.protected_baseline_errors(root, state))

    def test_room_maintenance_chains_after_gate4_without_rewriting_baseline(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            state = make_scaffold_fixture(root)
            baseline_path = root / state["protected_baseline"]["path"]
            original = baseline_path.read_bytes()
            first = add_maintenance_fixture(root, state)
            first_bytes = first.read_bytes()
            second = add_maintenance_fixture(root, state)
            receipt = labflow.read_json(second)
            receipt.update(profile=labflow.ROOM_MAINTENANCE_PROFILE, applied_at_gate=5)
            labflow.write_json(second, receipt, force=True)
            state["protected_maintenance"][-1]["sha256"] = labflow.file_sha256(second)
            self.assertEqual(labflow.protected_baseline_errors(root, state), [])
            self.assertEqual(baseline_path.read_bytes(), original)
            self.assertEqual(first.read_bytes(), first_bytes)

    def test_room_maintenance_rejects_wrong_gate_unknown_profile_and_asset_changes(self) -> None:
        for defect in ("gate", "profile", "unhashable_profile", "asset"):
            with self.subTest(defect=defect), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                state = make_scaffold_fixture(root)
                path = add_maintenance_fixture(root, state)
                receipt = labflow.read_json(path)
                receipt.update(profile=labflow.ROOM_MAINTENANCE_PROFILE, applied_at_gate=5)
                if defect == "gate":
                    receipt["applied_at_gate"] = 4
                elif defect == "profile":
                    receipt["profile"] = "accept_any_drift"
                elif defect == "unhashable_profile":
                    receipt["profile"] = []
                else:
                    receipt["changes"][0]["path"] = "ApprovedAssets/EMVRRoom/Prefabs/Room_Big_Part_01.prefab"
                labflow.write_json(path, receipt, force=True)
                state["protected_maintenance"][0]["sha256"] = labflow.file_sha256(path)
                self.assertTrue(labflow.protected_baseline_errors(root, state))

    def test_source_references_are_portable_read_only_and_content_locked(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / "first_clone"
            root.mkdir()
            state = make_source_reference_fixture(root)
            self.assertEqual([], labflow.source_reference_errors(root, state))
            persisted = (root / state["source_references"]["path"]).read_text(
                encoding="utf-8"
            )
            self.assertNotIn(str(root), persisted)
            self.assertNotRegex(persisted, r"[A-Za-z]:[\\/]")

            resolved_first = labflow.resolved_source_references(root, state)
            moved = Path(directory) / "github_clone"
            root.rename(moved)
            resolved_moved = labflow.resolved_source_references(moved, state)
            self.assertNotEqual(
                resolved_first["resolved_builder_pack_root"],
                resolved_moved["resolved_builder_pack_root"],
            )
            self.assertEqual(
                [item["repository_relative_path"] for item in resolved_first["sources"]],
                [item["repository_relative_path"] for item in resolved_moved["sources"]],
            )

            manifest_path = moved / state["source_references"]["path"]
            manifest = labflow.read_json(manifest_path)
            manifest["sources"][0]["access"] = "write"
            labflow.write_json(manifest_path, manifest, force=True)
            self.assertTrue(labflow.source_reference_errors(moved, state))

    def test_source_catalog_rejects_absolute_and_parent_paths(self) -> None:
        root = Path("E:/portable-test-root")
        for invalid in (
            "E:/other/source",
            "../source",
            "/source",
            r"Sources\source",
        ):
            self.assertIsNotNone(
                labflow.portable_repository_path_error(invalid, "source.path")
            )

    def test_generated_brief_locks_room_ui_and_default_desktop_xr_mapping(self) -> None:
        brief = labflow.brief_text(
            "lab_example",
            "Example Lab",
            "example",
            "Assets/Scenes/Example.unity",
        )
        self.assertIn(
            "ApprovedAssets/EMVRRoom/Prefabs/Room_Big_Part_01.prefab",
            brief,
        )
        self.assertIn("dot(-Transform.forward", brief)
        self.assertIn("desktop_to_xr_mapping:", brief)
        self.assertIn('policy: "equivalent_by_default"', brief)
        self.assertIn(
            'xr_interaction: "equivalent_to_desktop_via_common_xri_mapping"',
            brief,
        )
        design = labflow.design_text(
            "lab_example", "Assets/Scenes/Example.unity"
        )
        self.assertIn("- Desktop/XR default equivalence:", design)
        self.assertIn("- Confirmed mapping exceptions:", design)

    def test_brief_readiness_requires_default_desktop_xr_mapping(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            brief = root / "LabSpecs" / "lab_example" / "brief.yaml"
            brief.parent.mkdir(parents=True)
            content = labflow.brief_text(
                "lab_example",
                "Example Lab",
                "example",
                "Assets/Scenes/Example.unity",
            )
            content = content.replace("Replace with", "Confirmed")
            content = content.replace("replace_with", "confirmed")
            content = content.replace('manual_number: null', 'manual_number: 4').replace('manual_title: ""', 'manual_title: "Example"')
            content = content.replace('language: ""', 'language: "English"').replace('report_submission_requirements: ""', 'report_submission_requirements: "Written report"')
            content = content.replace('scoring_policy: ""', 'scoring_policy: "ungraded"').replace('metadata_confirmed: false', 'metadata_confirmed: true')
            brief.write_text(content, encoding="utf-8")
            self.assertEqual([], labflow.brief_readiness_errors(root, "lab_example"))

            brief.write_text(
                content.replace("equivalent_by_default", "user_defined"),
                encoding="utf-8",
            )
            self.assertIn(
                "Brief must preserve `equivalent_by_default` desktop-to-XR mapping.",
                labflow.brief_readiness_errors(root, "lab_example"),
            )

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
                        "schema_version": "1.2.0",
                        "workflow_mode": "blind-rebuild",
                        "lab_id": "lab_example",
                        "required_master_directory_name": "EMVR_Blind_BuilderPack",
                        "required_directory_name": "EMVR_Blind_Rebuild_lab_example",
                        "reference_base": "builder_pack_root",
                        "path_format": "repository_relative_posix",
                        "absolute_paths_persisted": False,
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

    def test_foundation_requires_byte_preserving_checkout_rule(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / ".gitattributes").write_text("* text=auto\n", encoding="utf-8")

            errors = labflow.foundation_errors(root)

            self.assertTrue(
                any("frozen hashes survive Git checkout" in item for item in errors)
            )

    def test_git_byte_policy_rejects_conversion_overrides(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / ".gitattributes"
            self.assertTrue(labflow.git_byte_preservation_errors(root))
            path.write_text("# frozen bytes\n* -text\n*.png binary\n", encoding="utf-8")
            self.assertEqual(labflow.git_byte_preservation_errors(root), [])
            for rule in ("*.meta text eol=lf", "*.bat text eol=crlf", "* text=auto",
                         "*.asset filter=lfs", "*.md ident", "*.md export-subst"):
                with self.subTest(rule=rule):
                    path.write_text("* -text\n" + rule + "\n", encoding="utf-8")
                    self.assertTrue(labflow.git_byte_preservation_errors(root))

    def test_git_distribution_receipt_is_limited_to_tooling(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            state = make_scaffold_fixture(root)
            baseline = (root / state["protected_baseline"]["path"]).read_bytes()
            path = add_maintenance_fixture(root, state)
            receipt = labflow.read_json(path)
            receipt.update(profile=labflow.GIT_DISTRIBUTION_PROFILE, applied_at_gate=5)
            labflow.write_json(path, receipt, force=True)
            state["protected_maintenance"][-1]["sha256"] = labflow.file_sha256(path)
            self.assertEqual(labflow.protected_baseline_errors(root, state), [])
            self.assertEqual((root / state["protected_baseline"]["path"]).read_bytes(), baseline)
            receipt["changes"][0]["path"] = "ApprovedAssets/EMVRRoom/Lighting/RoomLighting.lighting"
            labflow.write_json(path, receipt, force=True)
            state["protected_maintenance"][-1]["sha256"] = labflow.file_sha256(path)
            self.assertTrue(labflow.protected_baseline_errors(root, state))

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
        self.assertIn('schema_version = "1.2.0"', script)
        self.assertIn('path_format = "repository_relative_posix"', script)
        self.assertIn('absolute_paths_persisted = $false', script)

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
            with mock.patch.object(labflow, "foundation_errors", side_effect=lambda _root: []), mock.patch.object(
                labflow, "protected_baseline_errors", side_effect=lambda *_args: []
            ):
                errors = labflow.gate_readiness_errors(root, state, 0)
                self.assertTrue(any("Boundary verifier" in item for item in errors))
                state["evidence"]["boundary_verifier"] = "passed"
                self.assertEqual(labflow.gate_readiness_errors(root, state, 0), [])

    def test_gate_zero_baseline_is_one_time_and_detects_protected_changes(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            protected = root / "Policies" / "reuse-policy.md"
            protected.parent.mkdir(parents=True)
            protected.write_text("version one\n", encoding="utf-8")
            (root / "KnowledgeBase" / "rebuild-lessons").mkdir(parents=True)
            state = make_source_reference_fixture(root)
            state.update(
                {
                    "gate_approvals": {},
                    "evidence": labflow.default_evidence(),
                    "protected_baseline": None,
                }
            )
            state["evidence"]["boundary_verifier"] = "passed"
            with mock.patch.object(labflow, "foundation_errors", return_value=[]), mock.patch.object(
                labflow, "save_state"
            ):
                baseline = labflow.capture_gate0_protected_baseline(
                    root, "lab_example", state
                )
            self.assertTrue(baseline.is_file())
            self.assertEqual(labflow.protected_baseline_errors(root, state), [])

            with mock.patch.object(labflow, "foundation_errors", return_value=[]), mock.patch.object(
                labflow, "save_state"
            ):
                with self.assertRaisesRegex(RuntimeError, "already registered"):
                    labflow.capture_gate0_protected_baseline(root, "lab_example", state)

            protected.write_text("version two\n", encoding="utf-8")
            errors = labflow.protected_baseline_errors(root, state)
            self.assertIn(
                "Protected file changed after Gate 0: Policies/reuse-policy.md",
                errors,
            )

    def test_gate_zero_readiness_requires_captured_baseline(self) -> None:
        state = {
            "lab_id": "lab_example",
            "evidence": {"boundary_verifier": "passed"},
            "protected_baseline": None,
        }
        with mock.patch.object(labflow, "foundation_errors", return_value=[]), mock.patch.object(
            labflow, "prior_lessons_review_errors", return_value=[]
        ):
            errors = labflow.gate_readiness_errors(Path.cwd(), state, 0)
        self.assertTrue(any("has not been captured" in item for item in errors))

    def test_gate_zero_next_action_advances_from_boundary_to_baseline(self) -> None:
        state = {
            "lab_id": "lab_example",
            "gate_approvals": {},
            "evidence": labflow.default_evidence(),
            "protected_baseline": None,
        }
        with mock.patch.object(labflow, "gate_readiness_errors", return_value=["pending"]):
            self.assertIn("Builder Boundary", labflow.next_action(Path.cwd(), state))
            state["evidence"]["boundary_verifier"] = "passed"
            self.assertIn("capture-gate0-baseline", labflow.next_action(Path.cwd(), state))

    def test_gate_zero_baseline_allows_only_current_gate10_lesson(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            lessons = root / "KnowledgeBase" / "rebuild-lessons"
            lessons.mkdir(parents=True)
            (lessons / "safe.md").write_text("safe\n", encoding="utf-8")
            state = make_source_reference_fixture(root)
            state.update(
                {
                    "gate_approvals": {},
                    "evidence": labflow.default_evidence(),
                    "protected_baseline": None,
                }
            )
            state["evidence"]["boundary_verifier"] = "passed"
            with mock.patch.object(labflow, "foundation_errors", return_value=[]), mock.patch.object(
                labflow, "save_state"
            ):
                baseline = labflow.capture_gate0_protected_baseline(
                    root, "lab_example", state
                )
            (lessons / "lab_example.md").write_text("Gate 10 lesson\n", encoding="utf-8")
            self.assertEqual(labflow.protected_baseline_errors(root, state), [])
            baseline.write_text("{}\n", encoding="utf-8")
            self.assertIn(
                "Gate 0 protected-baseline file was changed after capture.",
                labflow.protected_baseline_errors(root, state),
            )

    def test_gate_zero_baseline_cannot_be_captured_after_gate_zero(self) -> None:
        state = {
            "lab_id": "lab_example",
            "gate_approvals": {"0": {"approved": True}},
            "evidence": {"boundary_verifier": "passed"},
            "protected_baseline": None,
        }
        with self.assertRaisesRegex(RuntimeError, "only during Gate 0"):
            labflow.capture_gate0_protected_baseline(Path.cwd(), "lab_example", state)

    def test_gate_eight_uses_only_current_lab_evidence(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            state = {
                "lab_id": "lab_example",
                "gate_approvals": {
                    str(gate): {"approved": True, "workflow_model": labflow.WORKFLOW_MODEL} for gate in labflow.GATE_SEQUENCE[:-1]
                },
                "evidence": labflow.default_evidence(),
            }
            (root / "BuildEvidence").mkdir()
            (root / "BuildEvidence" / "reuse-report.md").write_text(
                "lab_example complete", encoding="utf-8"
            )
            with mock.patch.object(labflow, "protected_baseline_errors", side_effect=lambda *_args: []):
                errors = labflow.gate_readiness_errors(root, state, 8)
            self.assertEqual(len([e for e in errors if "Acceptance evidence is missing" in e]), 2)
            self.assertTrue(any("Lab Manual" in e for e in errors))

            reuse, validation = labflow.evidence_relative_paths("lab_example")
            for relative in (reuse, validation):
                path = root / relative
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text("lab_example\ncomplete\npassed\n", encoding="utf-8")
            with mock.patch.object(labflow, "protected_baseline_errors", side_effect=lambda *_args: []):
                remaining = labflow.gate_readiness_errors(root, state, 8)
                self.assertFalse(any("Acceptance evidence" in e for e in remaining))
                self.assertTrue(any("Lab Manual" in e for e in remaining))
                self.assertTrue(any("Developer" in e for e in remaining))

    def test_scaffold_runtime_has_no_unreviewed_common_calls(self) -> None:
        source = labflow.runtime_source("LabExample", "LabExample", "lab_example")
        self.assertNotIn("EMVR.Common", source)
        self.assertNotIn("EmVrTelemetryEmitter", source)
        self.assertNotIn("EmVrStatusDisplay", source)

    def test_scaffold_persists_portable_files_and_survives_relocation(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / "first clone"
            state = make_scaffold_fixture(root)
            baseline = root / state["protected_baseline"]["path"]
            baseline_bytes = baseline.read_bytes()
            args = argparse.Namespace(project_root=root, lab_id="lab_example")
            with mock.patch.object(labflow, "require_foundation"), mock.patch.object(
                labflow, "validate_contracts", return_value=([], [], {})
            ):
                self.assertEqual(labflow.command_scaffold(args), 0)

            persisted = labflow.load_state(root, "lab_example")
            expected = [
                "UnityProject/Assets/Scripts/EMVR/Labs/LabExample/LabExampleController.cs",
                "UnityProject/Assets/Editor/LabExample/LabExampleSceneBuilder.cs",
                "LabSpecs/lab_example/implementation.md",
            ]
            self.assertEqual(persisted["generated_files"], expected)
            for relative in expected:
                self.assertIsNone(labflow.portable_repository_path_error(relative, "generated_files"))
                self.assertTrue((root / relative).is_file())
            handoff = (root / expected[2]).read_text(encoding="utf-8")
            for relative in expected[:2]:
                self.assertIn(relative, handoff)
            self.assertEqual(persisted["stage"], "scaffold_ready")
            self.assertEqual(labflow.current_gate(persisted), 4)
            self.assertEqual(persisted["evidence"]["scaffold_compile"], "pending")
            self.assertEqual(persisted["gate_approvals"], state["gate_approvals"])
            self.assertFalse((root / "UnityProject" / state["scene"]).exists())
            self.assertFalse((root / "UnityProject/Assets/Generated").exists())
            self.assertEqual(baseline.read_bytes(), baseline_bytes)

            moved = Path(directory) / "relocated clone"
            root.rename(moved)
            reloaded = labflow.load_state(moved, "lab_example")
            self.assertEqual(reloaded, persisted)
            self.assertTrue(all((moved / relative).is_file() for relative in reloaded["generated_files"]))
            self.assertEqual(labflow.protected_baseline_errors(moved, reloaded), [])
            self.assertEqual(labflow.source_reference_errors(moved, reloaded), [])

    def test_scaffold_serialization_uses_posix_for_both_path_flavors(self) -> None:
        # Exercise the whole command and real save_state/write_json serialization
        # in memory, so a POSIX CI host still detects Windows separator regressions.
        for root in (PureWindowsPath("C:/portable pack"), PurePosixPath("/portable pack")):
            with self.subTest(flavor=type(root).__name__):
                state = {
                    "scene": "Assets/Scenes/LabExample.unity",
                    "gate_approvals": {str(gate): {"approved": True} for gate in range(4)},
                    "design_approval": {"approved": True},
                }
                project_root = mock.Mock()
                project_root.resolve.return_value = root
                args = argparse.Namespace(project_root=project_root, lab_id="lab_example")
                with (
                    mock.patch.object(labflow, "require_foundation"),
                    mock.patch.object(labflow, "load_current_state", return_value=state),
                    mock.patch.object(labflow, "protected_baseline_errors", return_value=[]),
                    mock.patch.object(labflow, "source_reference_errors", return_value=[]),
                    mock.patch.object(labflow, "validate_contracts", return_value=([], [], {})),
                    mock.patch.object(type(root), "exists", return_value=False, create=True),
                    mock.patch.object(labflow, "write_text") as write,
                ):
                    self.assertEqual(labflow.command_scaffold(args), 0)
                saved = next(
                    call.args[1] for call in write.call_args_list
                    if call.args[0].name == "workflow_state.json"
                )
                entries = json.loads(saved)["generated_files"]
                self.assertEqual(len(entries), 3)
                for relative in entries:
                    self.assertIsNone(labflow.portable_repository_path_error(relative, "generated_files"))

    def test_scaffold_rejects_integrity_errors_before_any_write(self) -> None:
        for defect in ("missing_baseline", "changed_tool", "changed_references"):
            with self.subTest(defect=defect), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                state = make_scaffold_fixture(root)
                if defect == "missing_baseline":
                    state["protected_baseline"] = None
                    labflow.save_state(root, "lab_example", state)
                    expected = "Gate 0 protected baseline has not been captured"
                elif defect == "changed_tool":
                    (root / "Tools/labflow/labflow.py").write_text("# changed\n", encoding="utf-8")
                    expected = "Protected file changed after Gate 0"
                else:
                    manifest = root / state["source_references"]["path"]
                    manifest.write_text("{}\n", encoding="utf-8")
                    expected = "[Ss]ource"
                before = {p.relative_to(root).as_posix(): p.read_bytes() for p in root.rglob("*") if p.is_file()}
                args = argparse.Namespace(project_root=root, lab_id="lab_example")
                with mock.patch.object(labflow, "require_foundation"), mock.patch.object(
                    labflow, "validate_contracts", return_value=([], [], {})
                ), mock.patch.object(labflow, "write_text") as write:
                    with self.assertRaisesRegex(RuntimeError, expected):
                        labflow.command_scaffold(args)
                    write.assert_not_called()
                after = {p.relative_to(root).as_posix(): p.read_bytes() for p in root.rglob("*") if p.is_file()}
                self.assertEqual(after, before)

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

    def test_maintenance_preserves_original_baseline_and_unblocks_scaffold(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / "original"
            state = make_scaffold_fixture(root)
            original_state = copy.deepcopy(state)
            baseline_path = root / state["protected_baseline"]["path"]
            original_baseline = baseline_path.read_bytes()
            add_maintenance_fixture(root, state)
            self.assertEqual(labflow.protected_baseline_errors(root, state), [])
            self.assertEqual(state["protected_baseline"], original_state["protected_baseline"])
            self.assertEqual(state["gate_approvals"], original_state["gate_approvals"])
            self.assertEqual(state["evidence"], original_state["evidence"])
            self.assertEqual(baseline_path.read_bytes(), original_baseline)
            with mock.patch.object(labflow, "require_foundation"), mock.patch.object(
                labflow, "validate_contracts", return_value=([], [], {})
            ):
                labflow.command_scaffold(argparse.Namespace(project_root=root, lab_id="lab_example"))
            saved = labflow.load_state(root, "lab_example")
            self.assertEqual(labflow.current_gate(saved), 4)
            self.assertEqual(saved["evidence"]["scaffold_compile"], "pending")
            self.assertEqual(saved["protected_maintenance"], state["protected_maintenance"])
            self.assertEqual(labflow.protected_baseline_errors(root, saved), [])
            moved = Path(directory) / "relocated"
            root.rename(moved)
            self.assertEqual(labflow.protected_baseline_errors(moved, saved), [])

    def test_maintenance_receipt_chain_requires_all_predecessors(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            state = make_scaffold_fixture(root)
            first = add_maintenance_fixture(root, state)
            add_maintenance_fixture(root, state)
            self.assertEqual(labflow.protected_baseline_errors(root, state), [])
            first.unlink()
            self.assertTrue(labflow.protected_baseline_errors(root, state))

    def test_maintenance_rejects_changed_or_unregistered_receipts(self) -> None:
        for defect in ("bytes", "registration", "path"):
            with self.subTest(defect=defect), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                state = make_scaffold_fixture(root)
                receipt_path = add_maintenance_fixture(root, state)
                if defect == "bytes":
                    receipt_path.write_text("{}\n", encoding="utf-8")
                elif defect == "registration":
                    state.pop("protected_maintenance")
                else:
                    state["protected_maintenance"][0]["path"] = "../receipt.json"
                self.assertTrue(labflow.protected_baseline_errors(root, state))

    def test_maintenance_rejects_invalid_binding_and_change_metadata(self) -> None:
        for defect in ("lab_id", "baseline_sha256", "previous_receipt_sha256", "source_references_sha256", "profile", "sequence", "authorization_note", "before_sha256", "after_sha256", "unapproved_path", "duplicate", "empty"):
            with self.subTest(defect=defect), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                state = make_scaffold_fixture(root)
                receipt_path = add_maintenance_fixture(root, state)
                receipt = labflow.read_json(receipt_path)
                if defect in ("before_sha256", "after_sha256"):
                    receipt["changes"][0][defect] = "0" * 64
                elif defect == "unapproved_path":
                    receipt["changes"][0]["path"] = "UnityProject/Packages/manifest.json"
                elif defect == "duplicate":
                    receipt["changes"].append(receipt["changes"][0])
                elif defect == "empty":
                    receipt["changes"] = []
                else:
                    receipt[defect] = ""
                labflow.write_json(receipt_path, receipt, force=True)
                state["protected_maintenance"][0]["sha256"] = labflow.file_sha256(receipt_path)
                self.assertTrue(labflow.protected_baseline_errors(root, state))

    def test_maintenance_still_detects_all_later_protected_drift(self) -> None:
        for defect in ("same_size_mtime", "added", "removed", "baseline"):
            with self.subTest(defect=defect), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                state = make_scaffold_fixture(root)
                add_maintenance_fixture(root, state)
                tool = root / "Tools/labflow/labflow.py"
                if defect == "same_size_mtime":
                    stat = tool.stat()
                    tool.write_bytes(tool.read_bytes().replace(b"maintenance", b"MAINTENANCE"))
                    os.utime(tool, ns=(stat.st_atime_ns, stat.st_mtime_ns))
                elif defect == "added":
                    (root / "Tools/unreviewed.py").write_text("# new\n", encoding="utf-8")
                elif defect == "removed":
                    tool.unlink()
                else:
                    (root / state["protected_baseline"]["path"]).write_text("{}\n", encoding="utf-8")
                self.assertTrue(labflow.protected_baseline_errors(root, state))
                self.assertIn("Stop", labflow.next_action(root, state))
                with mock.patch.object(labflow, "require_foundation"), mock.patch.object(labflow, "write_text") as write:
                    with self.assertRaisesRegex(RuntimeError, "protected run integrity"):
                        labflow.command_scaffold(argparse.Namespace(project_root=root, lab_id="lab_example"))
                    write.assert_not_called()

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
            ), mock.patch.object(
                labflow, "room_scene_reuse_errors", side_effect=lambda *_args: []
            ), mock.patch.object(
                labflow, "xr_reuse_errors", side_effect=lambda *_args: []
            ), mock.patch.object(
                labflow, "protected_baseline_errors", side_effect=lambda *_args: []
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

    def test_room_semantics_reject_sample_ground(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            room = root / "ApprovedAssets/EMVRRoom/Prefabs/Room_Big_Part_01.prefab"
            room.parent.mkdir(parents=True)
            room.write_text(
                "GameObject:\n  m_Name: Non-Teleportable Ground\nMeshRenderer:\n",
                encoding="utf-8",
            )
            errors = labflow.approved_room_asset_errors(root)
            self.assertTrue(any("Wall geometry" in item for item in errors))
            self.assertTrue(any("Roof geometry" in item for item in errors))
            self.assertTrue(any("at least 6 MeshRenderers" in item for item in errors))

    def test_room_semantics_accept_complete_shell(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            room = root / "ApprovedAssets/EMVRRoom/Prefabs/Room_Big_Part_01.prefab"
            room.parent.mkdir(parents=True)
            room.write_text(
                "GameObject:\n  m_Name: Floor_01\n"
                "GameObject:\n  m_Name: Wall_01\n"
                "GameObject:\n  m_Name: Roof_01\n"
                + "MeshRenderer:\n" * 6,
                encoding="utf-8",
            )
            self.assertEqual(labflow.approved_room_asset_errors(root), [])

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
                "ApprovedAssets/EMVRRoom/Prefabs/Room_Big_Part_01.prefab"
            )
            source = root / source_relative
            source.parent.mkdir(parents=True)
            source.write_text("room", encoding="utf-8")
            source_meta = Path(str(source) + ".meta")
            source_meta.write_text("guid: 0123456789abcdef0123456789abcdef", encoding="utf-8")
            destination_relative = Path(
                "UnityProject/Assets/Generated/lab_example/ApprovedDependencies/"
                "EMVRRoom/Prefabs/Room_Big_Part_01.prefab"
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
                    "ApprovedAssets/EMVRRoom/Prefabs/Room_Big_Part_01.prefab"
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
                        "schema_version": "1.2.0",
                        "workflow_mode": "blind-rebuild",
                        "lab_id": "lab_example",
                        "required_master_directory_name": "EMVR_Blind_BuilderPack",
                        "required_directory_name": "EMVR_Blind_Rebuild_lab_example",
                        "reference_base": "builder_pack_root",
                        "path_format": "repository_relative_posix",
                        "absolute_paths_persisted": False,
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
            source_metadata = {
                "path": "LabSpecs/lab_example/source-references.json",
                "sha256": "A" * 64,
                "catalog_path": labflow.SOURCE_REFERENCE_CATALOG,
                "catalog_sha256": "B" * 64,
                "reference_base": "builder_pack_root",
                "path_format": "repository_relative_posix",
                "source_access": "read_only",
            }
            with mock.patch.object(
                labflow,
                "ensure_source_reference_manifest",
                return_value=source_metadata,
            ):
                self.assertEqual(labflow.command_migrate_state(args), 0)
            migrated = labflow.load_state(root, "lab_example")
            self.assertEqual(migrated["schema_version"], labflow.CURRENT_STATE_SCHEMA)
            self.assertEqual(migrated["gate_approvals"], {})
            self.assertFalse(migrated["brief_confirmation"]["confirmed"])
            self.assertFalse(migrated["design_approval"]["approved"])
            self.assertIsNone(migrated["protected_baseline"])
            self.assertEqual(migrated["source_references"], source_metadata)
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


class CompactWorkflowTests(unittest.TestCase):
    def fixture(self, root, gate=4):
        state = {"schema_version": labflow.CURRENT_STATE_SCHEMA,
                 "lab_id": "lab_example", "workflow_mode": "integrated-development",
                 "gate_approvals": {}, "evidence": labflow.default_evidence()}
        for previous in labflow.GATE_SEQUENCE:
            if previous == gate:
                break
            labflow.record_gate_approval(state, previous, "继续")
        labflow.save_state(root, "lab_example", state)
        return state

    def test_sequence_skips_removed_gates_and_completes_at_eight(self):
        self.assertEqual(labflow.GATE_SEQUENCE, (0, 1, 2, 3, 4, 5, 6, 8))
        state = {"gate_approvals": {}}
        for gate in labflow.GATE_SEQUENCE:
            self.assertEqual(labflow.current_gate(state), gate)
            labflow.record_gate_approval(state, gate, "继续")
        self.assertTrue(labflow.workflow_is_complete(state))
        self.assertEqual(labflow.derived_stage(state), "complete")

    def test_old_acceptance_does_not_silently_complete_new_flow(self):
        state = {"gate_approvals": {str(g): {"approved": True} for g in range(11)}}
        self.assertEqual(labflow.current_gate(state), 6)
        self.assertFalse(labflow.workflow_is_complete(state))
        labflow.record_gate_approval(state, 6, "继续")
        self.assertEqual(labflow.current_gate(state), 8)
        self.assertTrue(state["superseded_gate_approvals"])

    def test_gate_six_accepts_semantic_approval_without_playtest_gate(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            state = self.fixture(root, 6)
            state["evidence"].update(compile="passed", scene_built="passed")
            labflow.save_state(root, "lab_example", state)
            with mock.patch.object(labflow, "protected_baseline_errors", return_value=[]), mock.patch.object(labflow, "source_reference_errors", return_value=[]):
                self.assertEqual(labflow.gate_readiness_errors(root, state, 6), [])
                args = argparse.Namespace(project_root=root, lab_id="lab_example", gate=6, note="继续")
                self.assertEqual(labflow.command_approve_gate(args), 0)
            saved = labflow.load_state(root, "lab_example")
            self.assertEqual(saved["user_acceptance"]["note"], "继续")
            self.assertEqual(labflow.current_gate(saved), 8)

    def test_retired_gates_cannot_be_approved(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self.fixture(root)
            for gate in (7, 9, 10):
                with self.assertRaisesRegex(RuntimeError, "supported gates"):
                    labflow.command_approve_gate(argparse.Namespace(project_root=root, lab_id="lab_example", gate=gate, note="继续"))

    def test_failed_evidence_automatically_enters_recovery(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self.fixture(root)
            args = labflow.build_parser().parse_args(["--project-root", str(root), "record", "--lab-id", "lab_example", "--scaffold-compile", "failed", "--note", "Observed compiler error CS0103"])
            with mock.patch.object(labflow, "next_action", return_value="recover"):
                labflow.command_record(args)
            saved = labflow.load_state(root, "lab_example")
            self.assertEqual(labflow.current_gate(saved), 4)
            self.assertEqual(saved["blocker_recovery"]["active"]["gate"], 4)
            self.assertEqual(saved["blocker_recovery"]["active"]["required_rechecks"], ["scaffold_compile"])
            self.assertFalse(labflow.gate_is_approved(saved, 4))

    def test_blocker_can_start_at_every_gate_and_retries_are_idempotent(self):
        with tempfile.TemporaryDirectory() as directory:
            for gate in labflow.GATE_SEQUENCE:
                root = Path(directory) / str(gate)
                state = self.fixture(root, gate)
                first = labflow.begin_blocker(root, state, "logic", "Observed wrong state transition")
                second = labflow.begin_blocker(root, state, "logic", "Observed again")
                self.assertEqual(first["id"], second["id"])
                self.assertEqual(labflow.current_gate(state), gate)
                self.assertEqual(len(list((root / "BuildEvidence/lab_example/blocker-retrospectives").glob("*.json"))), 1)

    def resolve_args(self, root):
        return labflow.build_parser().parse_args(["--project-root", str(root), "retrospective", "--lab-id", "lab_example", "--resolve", "--root-cause", "Incorrect local state transition", "--fix", "Corrected current Lab transition", "--verification", "Observed regression check passed", "--lesson", "Validate transitions before reporting success", "--prevention", "Check state reset at the originating gate"])

    def test_resolution_returns_to_gate_and_appends_only_owned_lessons(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            state = self.fixture(root)
            labflow.begin_blocker(root, state, "logic", "Observed reset mismatch")
            labflow.save_state(root, "lab_example", state)
            with mock.patch.object(labflow, "protected_baseline_errors", return_value=[]), mock.patch.object(labflow, "source_reference_errors", return_value=[]):
                labflow.command_retrospective(self.resolve_args(root))
                saved = labflow.load_state(root, "lab_example")
                self.assertIsNone(saved["blocker_recovery"]["active"])
                self.assertEqual(labflow.current_gate(saved), 4)
                lesson = root / "KnowledgeBase/rebuild-lessons/lab_example.md"
                first_bytes = lesson.read_bytes()
                first_report = root / saved["blocker_recovery"]["history"][0]["path"]
                report_bytes = first_report.read_bytes()
                labflow.begin_blocker(root, saved, "logic", "Observed another mismatch")
                labflow.save_state(root, "lab_example", saved)
                labflow.command_retrospective(self.resolve_args(root))
                self.assertTrue(lesson.read_bytes().startswith(first_bytes))
                self.assertEqual(first_report.read_bytes(), report_bytes)

    def test_resolution_refuses_unverified_failure_or_unowned_lesson(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            state = self.fixture(root)
            labflow.begin_blocker(root, state, "code", "Compile failed", ["scaffold_compile"])
            labflow.save_state(root, "lab_example", state)
            with mock.patch.object(labflow, "protected_baseline_errors", return_value=[]), mock.patch.object(labflow, "source_reference_errors", return_value=[]):
                with self.assertRaisesRegex(RuntimeError, "recheck"):
                    labflow.command_retrospective(self.resolve_args(root))
                state["evidence"]["scaffold_compile"] = "passed"
                labflow.save_state(root, "lab_example", state)
                lesson = root / "KnowledgeBase/rebuild-lessons/lab_example.md"
                lesson.parent.mkdir(parents=True)
                lesson.write_text("existing protected history", encoding="utf-8")
                with self.assertRaisesRegex(RuntimeError, "unowned"):
                    labflow.command_retrospective(self.resolve_args(root))
                self.assertEqual(lesson.read_text(encoding="utf-8"), "existing protected history")

    def test_active_recovery_prevents_approval(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            state = self.fixture(root, 6)
            state["evidence"].update(compile="passed", scene_built="passed")
            labflow.begin_blocker(root, state, "logic", "Observed wrong output")
            labflow.save_state(root, "lab_example", state)
            with mock.patch.object(labflow, "protected_baseline_errors", return_value=[]), mock.patch.object(labflow, "source_reference_errors", return_value=[]):
                self.assertTrue(any("recovery" in e for e in labflow.gate_readiness_errors(root, state, 6)))

    def test_protected_failure_can_be_reported_but_not_resolved_by_bypass(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self.fixture(root)
            args = labflow.build_parser().parse_args(["--project-root", str(root), "retrospective", "--lab-id", "lab_example", "--kind", "protected-foundation", "--note", "Protected hash mismatch"])
            labflow.command_retrospective(args)
            with mock.patch.object(labflow, "protected_baseline_errors", return_value=["Protected hash mismatch"]):
                with self.assertRaisesRegex(RuntimeError, "integrity"):
                    labflow.command_retrospective(self.resolve_args(root))

    def test_resolution_rolls_back_on_state_save_failure(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            state = self.fixture(root)
            labflow.begin_blocker(root, state, "logic", "Observed reset mismatch")
            labflow.save_state(root, "lab_example", state)
            before = {p.relative_to(root).as_posix(): p.read_bytes() for p in root.rglob("*") if p.is_file()}
            with mock.patch.object(labflow, "protected_baseline_errors", return_value=[]), mock.patch.object(labflow, "source_reference_errors", return_value=[]), mock.patch.object(labflow, "save_state", side_effect=OSError("write failure")):
                with self.assertRaises(OSError):
                    labflow.command_retrospective(self.resolve_args(root))
            after = {p.relative_to(root).as_posix(): p.read_bytes() for p in root.rglob("*") if p.is_file()}
            self.assertEqual(before, after)

    def test_completed_reports_and_lessons_reject_tampering(self):
        for target in ("report", "lesson"):
            with self.subTest(target=target), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                state = self.fixture(root)
                labflow.begin_blocker(root, state, "logic", "Observed mismatch")
                labflow.save_state(root, "lab_example", state)
                with mock.patch.object(labflow, "protected_baseline_errors", return_value=[]), mock.patch.object(labflow, "source_reference_errors", return_value=[]):
                    labflow.command_retrospective(self.resolve_args(root))
                saved = labflow.load_state(root, "lab_example")
                recovery = saved["blocker_recovery"]
                path = root / (recovery["history"][0]["path"] if target == "report" else recovery["lesson"]["path"])
                path.write_text("tampered", encoding="utf-8")
                with self.assertRaisesRegex(RuntimeError, "modified"):
                    labflow.begin_blocker(root, saved, "logic", "New mismatch")

    def test_contract_failure_opens_recovery_and_requires_successful_recheck(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            state = self.fixture(root, 2)
            state["brief_confirmation"] = {"confirmed": True}
            labflow.save_state(root, "lab_example", state)
            args = argparse.Namespace(project_root=root, lab_id="lab_example")
            with mock.patch.object(labflow, "validate_contracts", return_value=(["Invalid reference"], [], {})):
                self.assertEqual(labflow.command_validate(args), 1)
            saved = labflow.load_state(root, "lab_example")
            self.assertEqual(saved["blocker_recovery"]["active"]["required_rechecks"], ["contracts"])
            with mock.patch.object(labflow, "protected_baseline_errors", return_value=[]), mock.patch.object(labflow, "source_reference_errors", return_value=[]), mock.patch.object(labflow, "validate_contracts", return_value=(["Invalid reference"], [], {})):
                with self.assertRaisesRegex(RuntimeError, "recheck"):
                    labflow.command_retrospective(self.resolve_args(root))

    def test_status_is_read_only_and_pending_is_not_failure(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self.fixture(root)
            original = labflow.state_path(root, "lab_example").read_bytes()
            with mock.patch.object(labflow, "validate_contracts", return_value=([], [], {})), mock.patch.object(labflow, "gate_readiness_errors", return_value=["Compile pending"]), mock.patch.object(labflow, "next_action", return_value="Wait for evidence"):
                labflow.command_status(argparse.Namespace(project_root=root, lab_id="lab_example"))
            self.assertEqual(labflow.state_path(root, "lab_example").read_bytes(), original)
            self.assertFalse((root / "BuildEvidence/lab_example/blocker-retrospectives").exists())


if __name__ == "__main__":
    unittest.main()
