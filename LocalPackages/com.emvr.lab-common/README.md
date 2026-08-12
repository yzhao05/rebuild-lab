# EMVR Lab Common

`com.emvr.lab-common` is the reusable Unity runtime package for EMVR teaching labs.
It contains interaction, scene-flow, object identity/state export, telemetry, status
feedback, assist grounding, and read-only scene validation.

## Supported environment

- Unity `2022.3`
- XR Interaction Toolkit `2.6.5`
- XR Plug-in Management `4.7.0`
- OpenXR `1.14.3`

## Install locally

In Unity Package Manager, choose **Add package from disk** and select this package's
`package.json`.

For the Summer26 project, the equivalent `Packages/manifest.json` entry is:

```json
"com.emvr.lab-common": "file:../../../emvr-lab-common"
```

## Package boundary

This package owns behavior shared by multiple labs. Lab-specific physics formulas,
presets, steps, scene builders, and JSON contracts remain in the consuming Unity
project.

The original extraction at `E:/暑研/common` remains unchanged. Electrostatics-specific
class names have been replaced with neutral EMVR names in this package.

## Editor validation

After installing the package, use:

```text
Tools > EMVR > Common > Validate Active Scene
```

The validator reports duplicate `AudioListener` or `EventSystem` objects, missing or
duplicate lab object IDs, and draggable objects without colliders. It does not modify
the scene.

## Minimal validation scene

After Unity finishes compiling the package, run:

```text
Tools > EMVR > Common > Build Minimal Validation Scene
```

The builder creates `Assets/Scenes/EMVR_Common_MinimalValidation.unity` plus three
generated materials under `Assets/EMVRCommonValidation/Materials`. It also uses the
XRI Starter Assets already imported in the host project to add the XR rig, visible left
and right controller models, ray interactors, and an `XRGrabInteractable` cube.

Enter Play Mode in the default F1 desktop view and drag the orange cube with the left
mouse button. This view is explicitly monoscopic so the editor mouse and camera ray use
the same coordinates. Press F2 for the headset view (F1 returns to desktop); an XR grab
uses the same feedback and telemetry route. A successful desktop drag or XR grab
produces:

In F2 simulator view, use `Y` to toggle right-controller manipulation (`Space` while
held is the momentary alternative), aim with the mouse, and use the left mouse button
as Trigger/Select. The simulator's native Y Translate action uses `Q`/`E`; the host
sample also binds `Down Arrow`/`Up Arrow` as aliases, so these keys move the selected
controller and its standard XRI line together. The status panel reports the active ray
count, current hit target, and selection state.

```text
[EMVR Minimal Validation] PASS
```

Rebuilding asks before replacing the generated scene and never edits scene YAML by
hand.

## Object-state validation scene

The second smoke-test scene verifies the reusable state contract separately from XR
interaction. After Unity finishes compiling, run:

```text
Tools > EMVR > Common > Build Object State Validation Scene
```

The builder creates `Assets/Scenes/EMVR_Common_ObjectStateValidation.unity`. Enter
Play Mode and the probe automatically runs this sequence:

```text
CAPTURE -> MUTATE -> RESTORE -> MUTATE -> RESET
```

It verifies two distinct object types, unique stable object IDs, position/rotation/
scale, a bounded numeric experiment parameter, deterministic JSON serialization, and
restoration to both a captured snapshot and the scene-authored initial state. Success
is shown in the world-space status panel and logged as:

```text
[EMVR Object State Validation] PASS
```

Reusable lab code can use `EmVrObjectStateSnapshotStore` with `EmVrLabObject` or a
subclass. A domain-specific subclass should override `ExportStateJson` and
`TryImportStateJson` together so its physical parameters participate in capture,
restore, reset, and agent-visible state.

## Lab-contract validation scene

The third smoke-test verifies the workflow contract that joins stable IDs, step
progression, telemetry, and assist context. Run:

```text
Tools > EMVR > Common > Build Lab Contract Validation Scene
```

The builder creates `Assets/Scenes/EMVR_Common_LabContractValidation.unity`. In Play
Mode the probe first confirms that a missing `labId` is explicitly rejected, then runs
a two-step lab automatically. Its expected event sequence is:

```text
lab_started
step_started
object_selected
object_drag_started
object_drag_ended
step_completed
step_started
step_completed
lab_completed
```

Every event payload must contain the current session, lab, and step contract fields.
The probe also verifies that assist grounding contains the registered objects, selected
object, recent actions, and the current step after navigation. Success is logged as:

```text
[EMVR Lab Contract Validation] PASS
```

Generated labs can configure `EmVrLabContractRunner` with their own lab ID, ordered
step definitions, known objects, telemetry emitter, status display, and assist
grounding provider. Lab-specific physics decides when a step is complete; the runner
owns the shared lifecycle and event ordering.

## XR hardware validation scene

The fourth smoke-test is intentionally manual because it verifies real OpenXR device
tracking rather than editor simulation. Run:

```text
Tools > EMVR > Common > Build XR Hardware Validation Scene
```

The builder creates `Assets/Scenes/EMVR_Common_XrHardwareValidation.unity` using the
same XRI Starter Assets controller and ray setup used by the rebuilt labs. Connect and
wake a real headset and both controllers, enter Play Mode, then ray-grab the orange
cube with Trigger/Select, move it at least 5 cm, and release it.

The hardware gate requires a tracked physical HMD, tracked left and right controllers,
and two active controller rays. Device names containing simulator, mock, or synthetic
markers are displayed as `SIMULATED` and cannot produce PASS. A successful physical
grab must also emit drag-start/drag-end telemetry and update assist grounding. Success
is logged as:

```text
[EMVR XR Hardware Validation] PASS
```

Without connected hardware the scene remains in `WAITING`; that is not a package
failure. The ray tuner disables its editor mouse-select bridge in this scene so a
desktop click cannot accidentally satisfy the hardware test.

## Scene-lifecycle validation scenes

This automatic test covers the lifecycle case that was skipped when the numbered
object-state test was built first. Run:

```text
Tools > EMVR > Common > Build Scene Lifecycle Validation Scenes
```

The builder creates and enables two scenes while preserving the existing Build
Settings list:

```text
Assets/Scenes/EMVR_Common_SceneLifecycle_Start.unity
Assets/Scenes/EMVR_Common_SceneLifecycle_Lab.unity
```

The Start scene is left open. Enter Play Mode and the persistent probe automatically
runs `Start -> Lab -> Reset -> Back` three times. Reset is a real reload after the Lab
object has been mutated. Returning to Start creates the scene-authored bootstrap again,
so `EmVrPersistentServiceRoot` must remove that duplicate while retaining the original
application services.

Every stage requires exactly one persistent root, XR Origin, active EventSystem, and
active AudioListener. The probe also routes EventSystem selection to a persistent target
after each transition, proving that the input route remains operational. Success is
logged as:

```text
[EMVR Scene Lifecycle Validation] PASS
```

This test uses a monoscopic persistent desktop camera and disables rendering/audio on
the retained XR camera, so it does not require a headset or controllers.
