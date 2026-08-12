# EMVR Lab Common Architecture

## Boundary

The package contains only behavior that is reusable across multiple teaching labs.

Inside the package:

- desktop and XR pointer behavior;
- planar dragging and drag observer contracts;
- hover annotations and visible status feedback;
- replacement spawning and trigger recycling;
- scene loading and movement-based scene triggers;
- validated lab/step lifecycle and canonical event ordering;
- stable lab object identity plus reversible JSON snapshots;
- telemetry emission and recent-event buffering;
- assist grounding payload construction;
- read-only active-scene validation.

Outside the package:

- electrostatics, magnetostatics, wave, or other domain formulas;
- lab-specific presets and report questions;
- lab step definitions and rubric content;
- scene builders and generated scenes;
- per-lab JSON contract files;
- network-specific assist gateway clients.

## Namespace map

```text
EMVR.Common.Assist
EMVR.Common.Contracts
EMVR.Common.Interaction
EMVR.Common.Objects
EMVR.Common.SceneFlow
EMVR.Common.Telemetry
EMVR.Common.UI
EMVR.Common.Validation
EMVR.Common.Editor.Validation
```

## Object-state persistence

- `ILabObjectStateProvider` exposes stable identity plus JSON export.
- `IRestorableLabObjectStateProvider` adds validated import and an authored reset point.
- `EmVrObjectStateSnapshotStore` stores deterministic, ID-keyed multi-object snapshots
  and rejects duplicate IDs, missing objects, type mismatches, or unsupported schemas.

`EmVrParameterizedLabObject` is a neutral reference implementation for a transform
plus one bounded numeric experiment parameter. Lab-specific subclasses may replace it
while keeping the same reversible contract.

## Lab contract lifecycle

`EmVrLabContractRunner` validates required session, user, lab, step, and object IDs
before a lab starts. It advances ordered steps and emits the canonical lifecycle events
while updating `EmVrAssistGroundingProvider` to the active step. Domain-specific code
retains ownership of physics and rubric rules, then calls the runner after those rules
permit selection, dragging, or step completion.

## XR hardware gate

`EmVrXrHardwareValidationProbe` separates real-device acceptance from the editor
simulator. It requires tracked physical HMD/controller devices and active standard XRI
controller rays before a grab can be evaluated. The hardware scene disables the
desktop manual-select fallback, then verifies physical movement, drag telemetry, and
assist grounding after release.

## Persistent scene lifecycle

`EmVrPersistentServiceRoot` owns one application-level service hierarchy across
single-scene loads. When the bootstrap scene is revisited, the newly authored duplicate
destroys itself while the original XR setup, EventSystem, loader, status, and camera
remain alive. `EmVrSceneLifecycleValidationProbe` exercises Start, Lab, Lab reload, and
Back repeatedly, checking singular services and EventSystem routing after every load.

## Migration map

```text
ElectrostaticMouseVrPointer   -> EmVrMousePointer
ElectrostaticXrRayTuner       -> EmVrXrRayRuntimeTuner
ElectrostaticHoverAnnotation  -> EmVrHoverAnnotation
ElectrostaticsLab namespace   -> EMVR.Common.*
```

## Extension pattern

1. Add `EmVrLabObject` to every contract-visible scene object and assign the exact
   `object_id` used by the lab JSON contract.
2. Subclass `EmVrLabObject` when a domain object must export charge, current,
   orientation, phase, or another physical state.
3. Implement `IEmVrDragObserver` in a lab-specific component for effects such as
   charging, snapping, or step progression.
4. Implement `IPlacementCondition` in a lab-specific component when completion
   depends on screen-plane, world-volume, or domain-specific placement logic.
5. Send stable event names through `ILabTelemetryEmitter` rather than coupling
   interaction components to a backend.
6. Use `EmVrAssistGroundingProvider` to assemble object and recent-action context;
   keep the actual network client in the consuming project.

## Input compatibility

`EmVrMousePointer` accepts an optional `IEmVrPointerInput` provider. Without one it
uses the package's shared desktop adapter, which supports both the new Input System
and legacy input compile paths.
