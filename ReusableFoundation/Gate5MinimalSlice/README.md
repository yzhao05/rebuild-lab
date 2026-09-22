# Gate 5 reusable minimal slice

This directory is protected workflow foundation. It contains only cross-Lab
wiring and validation code; it contains no old Lab formula, answer, object ID,
class name, coordinate, or scene wiring.

Builder Agents must not edit these files. At Gate 5 they must run:

```powershell
python .agents/skills/build-unity-vr-lab-blind/scripts/materialize_gate5_foundation.py --lab-id <lab_id>
```

The command verifies every source hash, copies the files byte-for-byte into
`UnityProject/Assets/Generated/<lab_id>/Gate5Foundation/`, marks copied source
files read-only, and writes
`BuildEvidence/<lab_id>/gate5-foundation-import.json`.

The copied foundation is also immutable. Lab-specific code must call or compose
around it. If a source or copied file appears defective, stop the current gate
and report the defect to the user; do not patch either copy during the rebuild.

The slice supplies:

- a runtime harness that calls Common `EmVrReplacementSpawner`, validates one
  active EventSystem, and checks that a spawned object is active, renderable,
  collidable, and inside the validation camera;
- an Editor scene utility for a single XR UI EventSystem, a persistently wired
  button whose listener is enabled in both EditMode and runtime, a neutral
  active offstage spawn template, and fail-closed build logs;
- three generic EditMode regression tests for inactive-template, visible-spawn,
  and duplicate-EventSystem failures. The visible-spawn test invokes the
  persistent button in EditMode to prevent a `RuntimeOnly` listener regression.

It does not build the target domain. The current Lab must add its own smallest
domain object, deterministic calculation, visible result, interaction adapter,
state/reset, telemetry, assist grounding, and domain tests beside this layer.

## Portable Gate 5 lighting implementation

The protected implementation is
`ReusableFoundation/Gate5MinimalSlice/Source/Editor/EmVrRoomLighting.cs`, assembly
`EMVR.ReusableFoundation.Gate5.Editor`. The existing foundation materializer imports
it byte-for-byte into each Lab's `Gate5Foundation/Editor/`; clean workspaces retain
this source even though another Lab's Editor directory is excluded. Use
`EmVrRoomLighting.ForLab(labId, scenePath)` with repository-relative POSIX paths.
Call `RequireRoomLightingSettings()` before any scene mutation, then
`ApplyRoomLightingDefaults(room, settings)` and `VerifyOriginalLightingAfterSave()`
after saving. Use `RetireOriginalLightingCache()` around manual lighting generation.
Execution settings, source cache retirement and the cross-Lab exact-path save guard
are implemented by this protected component. Never copy their bodies into a Lab
SceneBuilder or write a competing adapter. Lab code owns its menu, scene and user
handoff only. All existing frozen source hashes, room modules, light values and
brighter ambient defaults remain unchanged; no bake occurs on import/startup/Play.

The materializer includes a separate seven-test EditMode assembly
`EMVR.ReusableFoundation.Lighting.Tests`. Run it when validating the lighting
integration, in addition to the original three `EMVR.ReusableFoundation.Gate5.Tests`.
Pure helper tests do not prove native import/save/GI or visual acceptance; retain
the actual Unity checks and ordered Exit Play -> Build once -> Generate Lighting,
wait, then save handoff. Existing imports with an older manifest must stop for
maintainer-reviewed migration; the Builder cannot replace their evidence or hashes.
