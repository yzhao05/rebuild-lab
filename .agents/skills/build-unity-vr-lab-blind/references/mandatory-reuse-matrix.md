# Code inventory and mandatory reuse matrix

## Inventory scope

Run `scripts/list_code_inventory.ps1` to list every active code file. The
default inventory excludes Unity-generated caches, the duplicate Common mirror,
the disabled MCP archive, and Gold Reference.

Treat the script output as authoritative for the current Pack; do not copy
hard-coded counts into an audit. It groups canonical Common, any independently
approved Category B candidates, approved third-party dependency code, the
single current run's output, and workflow implementation.

`ReusableFoundation/Gate5MinimalSlice/` is a separately protected,
manifest-verified source layer. Its six C#/asmdef files are copied byte-for-byte
at Gate 5 and are not editable reusable examples.

The root Common mirror is an integrity mirror, not a second implementation.
Third-party sample scripts are dependencies of selected assets, not a blanket
reuse requirement. A pristine new run must have no current-Lab implementation
group. Current-run code, restricted legacy material, and archived MCP content
are never reusable answers for another run.

## Mandatory in every accepted Lab

Record each row in the design reuse audit and show its scene wiring/evidence.
Do not replace these responsibilities with new Lab-specific equivalents.

| Responsibility | Required Common code | Requirement |
| --- | --- | --- |
| Stable contract identity and reversible object state | `EmVrLabObject`, `ILabObjectStateProvider`, `IRestorableLabObjectStateProvider` | Add/subclass on every contract-visible mutable object; use exact contract IDs. |
| Capture/Restore/Reset | `EmVrObjectStateSnapshotStore` | Register every mutable contract-visible object. Lab-specific code may additionally manage runtime-created membership, but must not replace the Common state envelope. |
| Lab and step lifecycle | `EmVrLabContractRunner` | Configure the confirmed lab ID, ordered steps, known objects, and shared services. Domain code decides physics success and then advances the runner. |
| Telemetry | `EmVrTelemetryEmitter`, `ILabTelemetryEmitter` | Emit the canonical lifecycle and interaction events through the Common emitter. |
| Assist grounding | `EmVrAssistGroundingProvider`, `IAssistGroundingProvider` | Expose current step, selected object, object state, recent actions, and diagnostic context. Network clients remain Lab/application-specific. |
| Visible operational status | `EmVrStatusDisplay` | Show at least ready, selected/action state, updating, completed, reset, and error. A Console-only state is insufficient. |
| Desktop pointer baseline | `EmVrMousePointer` and its `EmVrDesktopInput`/`IEmVrPointerInput` support | Required because every accepted Lab has a desktop path. |
| XR ray baseline | `EmVrXrRayRuntimeTuner` and `EmVrXrRayUtility` | Required with the approved XRI rig/controller-ray assets because every accepted Lab has an XR Simulator path. |
| Application scene lifecycle | `EmVrPersistentServiceRoot`, `EmVrSceneLoader` | Required for Start/Lab/Back and reload/reset lifecycle. A domain-only prototype may leave this pending but cannot reach final acceptance. |
| Read-only scene validation | `EmVrSceneValidator` | Run after every build and require zero errors; record warnings explicitly. |
| Room/environment baseline | `ApprovedAssets/XRInteractionToolkitSamples/Starter Assets/DemoSceneAssets/Prefabs/Teleportation Environment.prefab` | Instantiate this approved Prefab in every new Lab scene with its `.meta` and Starter Assets dependency closure. Do not replace it with generated primitive walls. |
| Gate 5 wiring foundation | `ReusableFoundation/Gate5MinimalSlice` via `materialize_gate5_foundation.py` | Copy only through the materializer; keep source and generated copy immutable. Use its EventSystem/button/spawn-visibility/build-guard helpers and run its three EditMode tests before domain code. |

The Common contract interfaces and internal support types are reused
transitively. Do not add meaningless components merely to mention every source
file.

## Mandatory when the capability exists

| Capability | Required Common code |
| --- | --- |
| Planar mouse dragging | `EmVrPlanarMouseDraggable`, `IEmVrMouseDraggable` |
| Shared desktop drag feedback/telemetry | `EmVrDragFeedbackBridge`, `IEmVrDragObserver` |
| XR grab equivalent to desktop drag | `EmVrXrGrabFeedbackBridge` |
| Hover/object annotation | `EmVrHoverAnnotation` |
| Generic replacement spawning | `EmVrReplacementSpawner` |
| Trigger-based recycling/deletion | `EmVrTriggerRecycler`, `IPlacementCondition` |
| Movement-triggered scene change | `EmVrMovementSceneTrigger` |
| Neutral bounded numeric reference object | `EmVrParameterizedLabObject` |

If a capability is present, the design must either use the listed Common code
or report a foundation defect/blocker. It may not silently create a parallel
Lab-specific implementation.

For a loader/spawn interaction, use the Common `EmVrReplacementSpawner` and
the Gate 5 foundation wiring together. Never use an inactive replacement
template: Unity clones the inactive state and the spawned object will remain
invisible. Keep the neutral template active but outside the camera/interaction
area, and require the foundation visibility check after button invocation.

## Gate 5 implementation split

The Builder Agent must preserve this ownership boundary:

1. Direct-call Common; never copy Common `.cs` files into the Lab.
2. Import the approved room/XR assets only with GUID closure and preserve their
   source.
3. Materialize the Gate 5 foundation byte-for-byte and never edit either copy.
4. Put only the target's domain object, deterministic solver/result, adapter,
   state membership, telemetry/assist mapping, and domain tests in editable
   current-Lab paths.

Foundation pass evidence is not domain pass evidence. Both must pass in order.

## Validation-only Common code

The five `Editor/Samples/*SceneBuilder.cs` files and eight
`Runtime/Validation/*` files validate Common itself. Run the relevant supplied
validation scene when the underlying capability changes; do not place its probe
in a production Lab scene.

## Never mandatory reuse

- Category B assist-system and legacy runtime files: inspect only when the
  design identifies a capability not already owned by Common; adapt into the
  current Lab without changing the archive.
- Approved TextMesh Pro and XRI sample scripts: copy only as the dependency
  closure of an explicitly selected approved prefab.
- Domain formulas, solvers, renderers, presets, rubric logic, source models,
  scene builders, and tests from another Lab: always implement from the current
  Brief/contracts.
- `OptionalIntegrations/LabMCP`: disabled and unavailable during rebuilds.

## Mandatory room/environment baseline

Treat the approved XRI `Teleportation Environment.prefab` as the mandatory EMVR
room baseline for every Lab rebuild. It is an asset rather than C# source, but
it belongs to the same mandatory-reuse audit as the Common code baseline.

`new_scene` means create a new Lab scene and instantiate the approved room
Prefab inside it. It does not authorize copying `DemoScene.unity`, generating
Cube walls/floors as a substitute, or searching Gold. Preserve the Prefab and
its dependency GUIDs by importing the required Starter Assets dependency
closure with `.meta` files. Lab-specific apparatus, UI, lighting adjustments,
and navigation limits may be added around the instantiated room without
replacing its room geometry.

If the Prefab or any required dependency is missing or cannot be instantiated,
report a protected foundation asset blocker and stop before design approval.
