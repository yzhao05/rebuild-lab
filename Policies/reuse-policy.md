# Reuse policy

## Workspace modes

- A blind rebuild, recreation, or comparison must use the fixed workspace
  `EMVR_Blind_BuilderPack/RebuildWorkspaces/EMVR_Blind_Rebuild_<lab_id>` created by
  `Tools/create_blind_run_pack.ps1 -LabId <lab_id>`. The rebuilding Agent must
  execute this command and report the exact generated path; the User only
  switches workspace and must never be asked to run the command or copy the
  Pack. It must not be created in the Builder Pack's parent or under a different
  name.
- A valid fixed child whose `.emvr-workspace.json` matches the requested Lab ID
  is resumed in place. The Agent must not invoke the master copy tool again from
  that child.
- When this Builder Pack is deliberately embedded in a host project to develop
  a genuinely new Lab, the internal-location rule does not apply. Use
  `labflow new --mode integrated-development`; reuse and Gate rules remain in
  force.

## Two-level integrity policy

Release-level frozen hashes in `Policies/pack-inventory.json` and
`ReusableFoundation/Gate5MinimalSlice/manifest.json` are maintained only by the
workflow maintainer. A rebuilding Agent must never edit, regenerate, or bypass
them. Gate 0 begins only after that release-level Builder Boundary passes.

After recording the observed Boundary PASS, the rebuilding Agent must run:

```powershell
python Tools/labflow/labflow.py capture-gate0-baseline --lab-id <lab_id>
```

This creates `BuildEvidence/<lab_id>/gate0-protected-baseline.json` exactly once
and records its own SHA-256 in `workflow_state.json`. It snapshots the protected
workflow, Common, reusable-foundation, approved-asset, knowledge, package, and
project-setting roots for this run. The command is allowed only in Gate 0,
refuses overwrite, and cannot be used to bless a failed release Boundary.
All later gates compare current protected files against this Gate 0 snapshot and
block on every unregistered change, addition or removal; verified maintainer
receipts below provide only exact replacement hashes. The current Lab's
`KnowledgeBase/rebuild-lessons/<lab_id>.md` is the sole planned exclusion
because blocker recovery can append it; all older lessons remain protected.

### Maintainer compatibility receipts before scaffold generation

A user-authorized workflow maintainer may register a reviewed tooling repair
at Gate 4, before any scaffold/implementation output or Unity evidence exists.
Profile `gate4_pre_scaffold_tooling_v1` permits only the explicit workflow tool
and instruction paths in `labflow.PROTECTED_MAINTENANCE_PATHS`; it cannot
replace Common, assets, packages or Lab inputs, or add/remove protected files.
The entire actual drift must match a reviewed old/new SHA-256 and size plan,
with passing release Boundary and source-reference checks.

The maintainer creates an immutable numbered receipt at
`BuildEvidence/<lab_id>/protected-maintenance/0001.json` (then 0002, etc.)
and appends its repository-relative path and SHA-256 to the optional state
`protected_maintenance` list. Receipts bind the Lab, original baseline hash,
source manifest, predecessor receipt, reviewed state, user authorization,
reason, validation and exact replacements. Original baseline bytes/registration
and Gate 0-3 approvals remain unchanged; all Unity evidence stays pending.
State schema 2.0.0 stays backward compatible: an absent list means no maintenance
authorization. The receipt schema is independently versioned as 1.0.0.

The Builder cannot issue, edit, remove, rehash or register these receipts or
alter that state list. Issuance is deliberately outside the Pack and is not a
Builder command or input. After registration, rerun Boundary,
`labflow source-references` and `labflow status`. Resume the same Gate 4 only
after integrity passes; generate the scaffold and obtain Unity compile evidence
normally. Every later gate verifies actual protected bytes against the original
snapshot with these exact replacements. A receipt is never a wildcard,
baseline recapture, gate approval or exclusion. Repairs outside this profile
require a separate maintenance compatibility decision or a new run.

## Portable read-only source references

`Policies/reuse-source-references.json` is the maintainer-owned catalog for the
original reusable code, Common package, validated Gate 5 source, approved
assets, room, and XR Prefabs. `labflow new` copies only these references—not
the source contents—into `LabSpecs/<lab_id>/source-references.json` and records
the manifest and catalog SHA-256 values in `workflow_state.json`.

Both manifests use normalized repository-relative POSIX paths resolved from
the current Builder Pack root. They must never store a Windows drive letter,
UNC path, or other machine-specific absolute path. After a GitHub clone, run:

```powershell
python Tools/labflow/labflow.py source-references --lab-id <lab_id>
```

The command validates the binding and prints absolute paths only as transient
diagnostic output. Do not paste that output into source, contracts, scenes,
specifications, or evidence. The original sources remain read-only and
byte-identical; reference-only items are called in place, and copied items may
move only through the catalog's declared verified importer/materializer.
Gate 0 freezes the catalog and per-Lab binding, and every later gate rejects a
changed manifest, changed catalog, missing source, or access/path-policy drift.

## Gate 5 default room lighting

The complete authority is immutable `Policies/emvr-room-lighting.json`, which
captures the current reviewed RenderSettings, original RenderSettings provenance, complete LightingSettings values and
rendering defaults. Original bytes and GUIDs are preserved in
the room/archive assets. For the published LightingSettings text only, maintenance
on 2026-09-12 standardized CRLF to LF so Unity serialization preserves the approved
byte format. No field values, GUIDs or metadata changed. The original and published
hashes/sizes are recorded in the profile's settings_serialization. Reuse
`ApprovedAssets/EMVRRoom/Lighting/RoomLighting.lighting` and its `.meta`.
Import this asset through the approved importer and assign its verified copy
to `Lightmapping.lightingSettings`; never edit the asset or substitute new
settings with a few matching fields. Source and imported copies are read-only.

Original GI defaults: realtime GI enabled, baked GI disabled, realtime environment
lighting enabled, On Demand generation, bounce/indirect/albedo multipliers 1,
realtime resolution 1, AO enabled with indirect exponent 6, final gather enabled
with 2048 rays. All remaining sampling/filtering values come from the original
asset, not inferred defaults. Preserve the original Gamma color space, Built-in
pipeline and Ultra pixel-light count 4 already configured in the protected project.

The original 2018.3.14f1 precomputed data and metadata are sealed at
`Policies/EMVRRoomLightingReference/LightingData.asset` for read-only provenance.
They contain scene-object associations and are **not transferable generated data
for a new scene**. Do not import, patch or assign this archive. Unity must
generate the current scene's own lighting data with the original settings;
write only the current Lab's declared `UnityProject/Assets/Scenes/<lab_id>/`
lighting output. Rebuilding changes renderer identities and invalidates old
data. Keep the settings asset and every room Part unchanged while generating.

Gate 5 checks the entire frozen environment, immutable imported lighting source,
and current-Lab LightingData reference, rejecting the historical data GUID.
Missing generation remains pending. Data-file presence is not proof of correct
GI: also inspect Unity's lighting completion/errors and the current Game view.
Only a specific user request may initiate default changes through maintenance;
Builders may not alter the profile, source hashes, imports or Gate 0 baseline.

Gate 5 lighting handoff is mandatory. The Editor builder automatically loads
the approved imported LightingSettings, verifies its identity and derives the
current-scene execution settings described below before saving; users must not create an asset or drag in a substitute.
Stop with an actionable import error when the required asset is missing.
Before touching scene objects, verify the imported lighting asset and metadata
against their reviewed byte hashes. Validate GUID, loaded type and GI defaults.
If loaded validation fails, the user-invoked Editor command synchronously reimports
only that verified asset once (`ForceUpdate | ForceSynchronousImport`), then
rechecks bytes, GUID, loaded type and GI defaults. Keep a valid loaded object;
unnecessary reimports can retrigger GI precompute. This refreshes Unity's loaded
import without rewriting source/default values. Distinguish missing files, byte
drift, GUID/type failure and loaded-property mismatch; include expected and actual
values for a mismatch. Do not label all of them as a missing immutable import.
Use the Inspector's serialized GI properties and public API values for validation.
The frozen source may load with workflow=0 / autoGenerate=true even though its
stored mode is 1. This observed Unity behavior is handled by the execution adapter
below; never patch the source/imported asset or remove GI/default checks.
Run this preflight before opening/rebuilding the scene so a dependency failure
does not destroy and then reopen the room. Disable Build and generation menus
while playing, compiling, importing or generating. A direct invocation in those
states gives a waiting/exit-Play dialog and returns, rather than a red Console error.
Current-scene lighting compatibility (maintainer-approved 2026-09-12): preserve
the source/imported LightingSettings byte-for-byte. The canonical current-Lab
Editor routine derives `Assets/Scenes/<lab_id>/RoomLighting_Execution.lighting`
using Unity's native Object.Instantiate/CreateAsset APIs. Set autoGenerate=false
only on this execution object; retain every original photometric/GI parameter.
Its name and Unity serialization version may differ; no authored light value may
differ from the frozen profile. Existing execution assets are reused and validated;
the routine may restore only their loaded auto-generation mode. Reject other drift.
Create the adjacent `.lighting.provenance.json` binding the original repository-
relative source path, source SHA-256/GUID and execution GUID. Validate fields and
provenance before acceptance. These are current-Lab generated outputs, not approved
imports; do not copy/import them from another run or hand-edit them. The scene must
reference its own execution asset. This explicitly supersedes earlier instructions
to reference the imported original directly. Keep Auto Generate off on execution
settings and retain all source/import hashes. Reuse this adapter for both Build
and generation-only recovery; do not write another lighting implementation.
The user-verified execution adapter, cache retirement and exact-path save guard
below are mandatory Gate 5A defaults. Reuse them; an agent must not replace or
rewrite this available implementation without an explicit user change request.
When valid execution settings already exist, validate them against the frozen
source file bytes without loading the original LightingSettings object again.
After binding execution settings, clear the original imported object's unsaved
native dirty state and unload it only when no loaded scene references it. This
discards a derived editor cache, not source file content. A deferred code-reload
cleanup may do this only for the current Lab already using its execution asset;
it must not open/save scenes, assign settings or start generation.
Install a current-Lab AssetModificationProcessor.OnWillSaveAssets filter for the
exact imported RoomLighting.lighting path and its metadata. Preserve every other
save path. Never call SaveAssetIfDirty on that original; that API bypasses the save
filter. Keep execution/data saves targeted. Verify original source/import byte
hashes after each native scene save and before reporting lighting-save success.
If Unity rewrites an original despite these protections, stop; do not automatically
restore files, accept changed hashes or issue maintenance receipts from the Builder.

A successful user-invoked Build must assign and validate execution settings,
save the assembled scene and verify original byte integrity. It must then show:
`Window > Rendering > Lighting > Generate Lighting, wait, then save.`
Do not invoke Bake automatically at the end of Build: the normal Gate 5A sequence
uses the user's explicit Lighting-window generation after Build. Keep execution
`LightingSettings.autoGenerate` false and all source/import values unchanged.
Retain the current-Lab Generate Lighting And Save menu as optional generation-only
recovery, reusing the same adapter and save protection. That explicit utility may
use native one-shot `Lightmapping.Bake()` and save only after success; it does not
replace the required normal user-facing sequence. Do not generate on script import,
editor startup or Play. Require Edit Mode, no running bake, exactly the target scene
loaded, current foundation revisions and verified settings. Save before generation.
On success recheck the scene/settings, require current LightingData in this Lab's
declared output directory, save and verify its nonzero GUID reference in the scene.
Never repair GI by assigning an old file. Cancellation, failure, missing output or
failed save remain pending; starting generation is not completion. Refuse a rebuild
while generation runs. Interaction READY and build success are not lighting
completion or Gate 5 acceptance.

Do not use file modification times to judge GI freshness: saving the scene normally
makes it newer than LightingData, and Git checkout changes timestamps. A null or
unresolved saved reference is a real blocker. A valid reference is structural proof
only; require native generation completion/errors and visual evidence for acceptance.

Every Gate 5A user-facing reply must include this ordered lighting procedure:

1. Exit Play Mode.
2. Run the current Lab's Build menu once. For `ece329_electric_field_0`:
   `Tools > EMVR > Labs > Build Ece329 Electric Field 0`.
3. `Window > Rendering > Lighting > Generate Lighting, wait, then save.`

Use the actual generated Build menu name for another Lab. This supersedes the
older lighting-only one-line handoff and the instruction to omit Build when a
settings reference exists. Keep the normal reply concise; keep asset/reference
checks internal and add diagnostics only for an actual failure or user request.
Generate after that Build and save after completion. Do not ask for another Build
after generation unless content changes; a later Build requires regeneration.

The default room light intensity is **5.0 per light** (Unity `Light.intensity`,
not a measured illuminance in lux). Reuse all four original Part 03 point lights
and apply the existing `m_Intensity: 5` instance overrides from read-only
`Policies/emvr-room-assembly.json`. The standalone Prefab value is 1.5;
importing it without its frozen overrides does not reproduce the room.

The original EMVRLABS environment remains recorded as `original_render_settings`.
On 2026-09-12 the user requested a modest brighter default: multiply the original
Flat ambient sky RGB by 1.5, preserving alpha and all other settings. This lifts
the constant ambient contribution without increasing the four light hot spots.
Use `render_settings` for the current default; the original record is provenance.
Set these defaults explicitly in the current-Lab
scene builder, before visual validation; never edit a room asset to set them:

| Setting | Gate 5 default |
| --- | --- |
| Part 03 point lights | 4 lights, intensity **5.0** each; preserve recipe colors/ranges |
| Ambient mode / intensity | `Flat` (serialized 3) / **1.0** |
| Ambient sky RGB | `(0.13116321, 0.15650724, 0.212264115)` (original RGB x1.5) |
| Ambient equator RGB | `(0.1400303, 0.21646784, 0.27205884)` |
| Ambient ground RGB | `(0.048010383, 0.112683475, 0.17647058)` |
| Ambient color alpha | `1` for all three colors |
| Skybox / Sun / Fog | None / None / disabled |
| Reflection mode / intensity / custom cubemap | `Custom` / `1.0` / None |
| Extra Directional Light | None in the Gate 5 minimal room |

In Flat mode the sky color supplies constant ambient light; equator/ground
colors are preserved for source consistency. The full profile and source asset
also preserve GI settings. These values do not guarantee identical screen brightness across render
pipelines, quality settings or displays. Keep all room Parts, dependency copies
and the assembly recipe byte-identical. Gate 5 checks the saved environment and
each expected light override; Unity visual/compile evidence remains required.

## Gate 5 XR defaults and ownership

These are required defaults in every Gate 5 minimal slice, not optional examples.
Only an explicit user request may initiate changing a default. Record the user's
exact request, affected setting, reason and validation in the current design and
reuse report. A general approval/continue message does not authorize changes.
Protected source changes require a reviewed maintainer release/compatibility
decision; Builders must not modify sources, imports, manifests or validators.
Do not introduce an agent-written exception file that bypasses these checks.

| Responsibility | Mandatory owner and default |
| --- | --- |
| Rig, camera, left/right tracking, models, interaction/locomotion managers, input action manager | Exactly one complete approved `XR Interaction Setup.prefab` by GUID closure. Preserve every nested source link/component/action reference and child pose. |
| Simulated HMD/controllers, pose input, device mode, bindings and simulator UI | Exactly one approved `XR Device Simulator.prefab` with its entire closure. Use installed XRI `XRDeviceSimulator`; never rewrite a pose updater or inject a second simulated device. |
| Initial keyboard movement target | Set the imported simulator's public `axis2DTargets` to `XRDeviceSimulator.Axis2DTargets.Position` once in the current-Lab startup composition. XRI 2.6.5 initializes Primary2DAxis; this explicit Gate 5 default enables immediate keyboard hand translation. Preserve subsequent user mode changes. |
| Controller ray casting and line origin | Keep approved `XRRayInteractor` / `XRInteractorLineVisual` and authored tracking/stabilized and model origin references. Do not move models, reparent origins, align rays to the camera, or reconstruct lines. |
| Runtime ray tuning | One Common `EmVrXrRayRuntimeTuner`: radius 0.06, minimum distance 20, controller rays only true, mouse-left debug selection false. Its existing Start/Update applies these; do not persist editor ray overrides into the rig. |
| Desktop interaction | One Common `EmVrMousePointer` on external Systems group, `showPointerRay=false`. Keep desktop behavior; the camera-origin yellow debug line is not a controller laser. |
| XR grab / feedback | XRI `XRGrabInteractable` and Common `EmVrXrGrabFeedbackBridge` on the neutral/domain object, sharing Common feedback/state with desktop. |
| XR UI / button / spawning | `EmVrGate5FoundationSceneUtility`, the rig's input actions, one XR EventSystem and persistent buttons, plus Common spawner/harness. Keep protected six-file foundation copy byte-identical. |

Default composition configures existing code. Both complete Prefabs must already
be instantiated through approved imports; `systems` is outside their hierarchies:

```csharp
var pointer = systems.gameObject.AddComponent<EmVrMousePointer>();
pointer.showPointerRay = false;
// Configure current-Lab interaction targets/layers through Common APIs.
var tuner = systems.gameObject.AddComponent<EmVrXrRayRuntimeTuner>();
tuner.sphereCastRadius = 0.06f;
tuner.minimumRayDistance = 20f;
tuner.controllerRaysOnly = true;
tuner.enableMouseLeftDebugSelect = false;
// Existing Common Start/Update applies settings at runtime.
```

Bind the imported root's existing `XRDeviceSimulator` to the current controller
in the scene builder. In that controller's `Start`, after verifying the reference,
apply exactly this existing API once (never in Update or LateUpdate):

```csharp
deviceSimulator.axis2DTargets = XRDeviceSimulator.Axis2DTargets.Position;
```

Use `UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation`. This is composition
of the installed implementation, not permission to copy or modify its internals.

Preserve simulator keyboard Y speed **0.2** and Q/E plus Down/Up arrow bindings.
In controller mode, hold **Left Shift** for the left device or **Space** for the
right; **T/Y** select each persistently. Up/Down translate relative to the
configured local movement space. Game view must have focus. **1** selects
Primary 2D Axis, **2** Secondary 2D Axis, and **3** toggles Device Position
targeting. Keyboard position movement runs only while Position is active.
If changed, use 3 once and confirm Position is selected, rather than repeatedly
toggling it. **Tab** cycles devices; **Escape** stops manipulation. These keys
belong to the simulator; do not advertise numbered Lab steps as shortcuts or
assign conflicting Lab keys.

Review code inventory/provenance: if Common, XRI, approved assets or Gate 5
foundation implement a responsibility, call it or use the verified
importer/materializer. Never copy, rename, fork or recreate that implementation
in Lab code. Lab code owns domain behavior and composition. Automated checks
reject missing/duplicate/unpacked or modified XR instances, changed Common
defaults and known duplicate/input-injection patterns. Static pattern checks
cannot prove arbitrary renamed code is semantically equivalent; review all
current-Lab code and declared output paths as well.

Before foundation acceptance, obtain Unity observations of both hands moving
up/down, each controller ray following its controller, no extra camera debug
beam, and equivalent desktop/XR button/grab results. Import hashes or a compile
PASS cannot replace these observations. Record actual observations in the
Gate 5 foundation evidence, not assumed results.

## Category A: directly reusable

These resources were validated as cross-Lab infrastructure and may be imported
or called directly. They are user-owned, read-only inputs during a rebuild:

- `UnityProject/LocalPackages/com.emvr.lab-common/` (canonical Common package)
- `ApprovedAssets/`
- `ReusableFoundation/Gate5MinimalSlice/` through its controlled materializer
- Unity/XR package dependencies declared by the clean project

This category includes pointer/drag/grab behavior, XR rig and controller-ray
support, object identity/state, Capture/Restore/Reset, lifecycle, telemetry,
assist grounding, status UI, validation builders, controller models, materials,
audio, and general visual assets.

Every core desktop interaction has a functionally equivalent XR path by
default. Common desktop pointer/drag and the approved XRI
Select/Trigger/Grab adapter must invoke the same Lab action handler and reach
the same target, state transition, visible feedback, and success result. The
physical gestures need not be identical. Gate 1 records this default without
asking the User to design bindings. Ask only when supplied requirements demand
different semantics or the Common/XRI baseline cannot express the action; each
exception requires explicit User confirmation.

The Gate 5 reusable foundation has stricter copy semantics than ordinary
Category A assets. Run
`.agents/skills/build-unity-vr-lab-blind/scripts/materialize_gate5_foundation.py`
to verify its manifest and copy it byte-for-byte into
`UnityProject/Assets/Generated/<lab_id>/Gate5Foundation/`. Both the protected
source and the generated copy are immutable to the Builder Agent. The current
Lab must call or compose around the copied assemblies and Common APIs; it may
not fork, patch, rename, or hand-copy these helpers. Only domain code beside
this layer is editable.

Mandatory reuse is defined by the active Skill reference
`references/mandatory-reuse-matrix.md`. Universal Common responsibilities must
appear in every accepted Lab; capability-triggered Common implementations must
be used whenever that capability appears. Category A permission alone is not a
reason to omit the mandatory baseline.

The room/environment shell is mandatory direct reuse. Every Lab must instantiate
`ApprovedAssets/EMVRRoom/Prefabs/Room_Big_Part_01.prefab` inside its newly
created scene, preserving its `.meta` and dependency closure. This is the
validated modular EMVR room containing floor, wall, and roof geometry. The XRI
`Teleportation Environment.prefab` contains only a sample ground and must not be
used as the room shell.
The full EMVRLABS room is frozen in `Policies/emvr-room-assembly.json`: two
Part 01, two Part 02, one Part 03, forming approximately 15 x 18 m of regular
floor extent. Part 01 alone or two joined Part 01 copies do not match this room.
Import all three Prefabs with dependency closure and preserve their source and
imported bytes/links. Apply only the recipe's placements and explicit Part 03
floor/light overrides to instances. No donor Lab scene or domain code is used.
Validate actual floor support, roof/joins and camera-visible room/UI.

Room reuse is all-or-nothing. Every room Part, dependency and .meta, the
generated imports, and the frozen assembly recipe are read-only to workflow
agents. Do not edit, unpack, regenerate, simplify, substitute or delete them,
or alter hashes to accept a changed copy. Always instantiate Part 01 twice,
Part 02 twice and Part 03 once. Only the already-frozen instance settings and
whole-room placement may be applied. Do not choose a subset for a minimal Lab.
Before Gate 5 foundation evidence can pass, verify the saved scene contains
those exact linked-instance counts and no added/removed, disabled or rescaled
module content. Copy/import success alone is insufficient. Existing maintenance
receipts are historical compatibility records, not Builder write permission.

Room reuse is all-or-nothing. Every room Part, dependency and .meta, the
generated imports, and the frozen assembly recipe are read-only to workflow
agents. Do not edit, unpack, regenerate, simplify, substitute or delete them,
or alter hashes to accept a changed copy. Always instantiate Part 01 twice,
Part 02 twice and Part 03 once. Only the already-frozen instance settings and
whole-room placement may be applied. Do not choose a subset for a minimal Lab.
Before Gate 5 foundation evidence can pass, verify the saved scene contains
those exact linked-instance counts and no added/removed, disabled or rescaled
module content. Copy/import success alone is insufficient. Existing maintenance
receipts are historical compatibility records, not Builder write permission.

For maintenance of a pending Gate 5 room assembly, the separate
`gate5_room_assembly_guidance_v1` profile is restricted to `AGENTS.md`, this
policy, the mandatory reuse matrix, `Tools/labflow/labflow.py`, and its tests.
Only the maintainer may issue/register the explicit before/after hash receipt;
the original Gate 0 snapshot and earlier receipts remain unchanged. Issuance
requires Gate 0-4 approvals, passed scaffold compilation, and pending Gate 5
and later checks. All assets, Common/reusable copies and source bindings must
still validate. It neither recaptures a baseline nor approves a gate.
This Prefab supplies one half of the room. A complete shell needs two linked
instances joined at the open longitudinal edge of the regular `Floor_01`
geometry, with a 180-degree relative yaw and positive unit scale. Derive the
join from actual geometry, excluding the doorway floor extension; do not
delete children, unpack the Prefab, change source/imported bytes, or fill the
missing half with primitives. Check floor support across the seam and beneath
player/apparatus footprints, matching roof/end-wall joins, and both sides in
Game View. Presence of three geometry roles alone is insufficient.

For maintenance of a pending Gate 5 room assembly, the separate
`gate5_room_assembly_guidance_v1` profile is restricted to `AGENTS.md`, this
policy, the mandatory reuse matrix, `Tools/labflow/labflow.py`, and its tests.
Only the maintainer may issue/register the explicit before/after hash receipt;
the original Gate 0 snapshot and earlier receipts remain unchanged. Issuance
requires Gate 0-4 approvals, passed scaffold compilation, and pending Gate 5
and later checks. All assets, Common/reusable copies and source bindings must
still validate. It neither recaptures a baseline nor approves a gate.
Record this decision in the Brief/design, but execute the approved-asset copy
only during Gate 5 after Gate 4 approval. Gate 5 readiness verifies the import
manifest and every source/copy hash.
`new_scene` forbids donor Lab scene copying, including `DemoScene.unity`; it does
not permit primitive-generated walls as a substitute. A missing or broken room
Prefab is a foundation blocker. The scene builder must fail before saving if
the instantiated room lacks visible floor, wall, or roof geometry.

World-space UI/status orientation is a mandatory build-time validation. Resolve
the camera that actually renders Play Mode and require
`dot(-surface.forward, normalize(camera.position - surface.position)) > 0` for
Unity Canvas/TextMesh surfaces. The builder must fail before save otherwise;
never rotate both the camera and UI as a compensating fix.

The verified XR baseline is also mandatory direct reuse. Import and instantiate
`Starter Assets/Prefabs/XR Interaction Setup.prefab` as the top-level XR setup
and `XR Device Simulator/XR Device Simulator.prefab` as its desktop simulator.
Do not instantiate the bare `XR Origin (XR Rig).prefab` as a substitute for
the complete setup and do not reconstruct the controller rig in Lab-specific
code. The approved simulator controls must retain the validated Up/Down-arrow
`Keyboard Y Translate` bindings. Configure rays through Common
`EmVrXrRayRuntimeTuner`/`EmVrXrRayUtility`; never fork the old Lab-specific
ray implementation.

## Disabled optional integration: Unity MCP

Unity MCP is not part of the active rebuild workflow. Its package, OpenUPM
registry, `UNITY_MCP_READY` define, MCP tool calls, and NuGet DLLs under
`UnityProject/Assets` must remain disabled. Preserved MCP 0.79.1 dependency
materials live outside the Unity project under `OptionalIntegrations/UnityMCP/`
for a future, separate user-authorized maintenance decision. Its generated
tool Skills are also archived there instead of remaining under active
`.agents/skills` discovery.

Legacy Lab MCP/tooling material is likewise disabled under
`OptionalIntegrations/LabMCP/`. Neither optional integration may be consulted,
copied into the Unity project, or used to bypass a gate during a blind rebuild.

## Category B: conditionally reusable

Resources under `KnowledgeBase/ReusableCodeArchive/` may be inspected and
adapted only after checking that the selected part is independent of the target
Lab's physics, steps, answers, object IDs, scene wiring, and class names.
The active archive currently contains no approved code candidates; its README
is a staging policy, not reusable implementation. A future item becomes
eligible only after user-led curation and validation outside a Lab run.

Before using a Category B item:

1. Identify the smallest reusable responsibility.
2. Rename and decouple Lab-specific symbols.
3. Put the adaptation only in the current Lab's declared output. If it should
   become shared behavior, report that recommendation to the user; do not write
   it into Common during the rebuild.
4. Add tests or a minimal validation scene.
5. Record source, changes, and justification in
   `BuildEvidence/<lab_id>/reuse-report.md`.

Do not modify the archived Category B source.

## Category C: knowledge-only

Issue catalogs, summaries, playbooks, documentation, configuration examples,
and contract examples may guide architecture and debugging. Do not copy code or
scene wiring from prose descriptions. Examples are not confirmed requirements;
the current Brief and contracts are authoritative.

Normalized pre-workflow construction lessons live under
`KnowledgeBase/rebuild-lessons/build-experience/pre-workflow/`; verified blocker lessons live at
`KnowledgeBase/rebuild-lessons/<lab_id>.md`. At Gate 0, the Builder Agent must
read every prior lesson file and record applicability in the current Lab's
`prior-lessons-review.md`. Lessons provide safeguards and debugging cues, not
target requirements or answers. They may preserve verified constraints such as
required initial values, API signatures, Unity lifecycle placement, or build
ordering only when those constraints are cross-Lab and stripped of old target
answers and wiring.

Raw old summaries are physically separated under
`KnowledgeBase/legacy-lab-specific-summaries/`. They may contain old Lab
implementation, exact coordinates, class names, object IDs, scene wiring, and
answers. The archived legacy Agent bundle and legacy example contracts live in
the same restricted tree, so the Builder Agent must not read or search them during a blind
rebuild. An explicit user authorization must name an exact file and a non-blind
comparison purpose; even then, it is not reusable code and must not replace the
current Brief or normalized build experience.

## Gold Reference: forbidden to the Builder Agent

The original Lab 1/2/3 scenes, implementation scripts, rebuild overlays, scene
GUID wiring, and complete source copies remain outside this pack. Do not search
for them. If a required fact is absent, stop and request it from the user.

Models and generic assets are approved even when their names mention a domain
apparatus, such as a solenoid or polarizer. Approval of a model does not approve
the old behavior code that once controlled it.

## Workflow-maintenance material: forbidden to the Builder Agent

Workflow development, dependency maps, internal change-impact notes, and
maintainer instructions are not rebuild inputs. They live outside the Builder
Pack, currently under the sibling `EMVR_Workflow_Maintenance/` directory. A
Builder Agent must not read, search, cite, or copy that directory or any file
named `WORKFLOW_MAINTAINER_GUIDE.md`. This restriction prevents maintenance
context from broadening the Builder's permissions or leaking old-run knowledge.

Maintenance must occur in a separate Agent context. A clean run Pack must not
contain maintenance material; if such a file appears inside the Pack, Gate 0 is
blocked until the user removes it from the run boundary.

## Beginner-safe Unity handoffs

Treat every user as a first-time Unity user for Unity-owned validation. A valid
handoff names the starting Edit/Play state, exact top-menu path, expected
window/tab location, exact target and button, observable success condition,
failure screenshot/text, and actions to avoid. Do not require the user to ask
where Test Runner, Console, Game view, a Builder menu, or Play Mode is located.

For EditMode testing, always identify `Window > General > Test Runner`, the
**EditMode** tab, the current Lab's exact test assembly and test names/count,
**Run Selected**, and the expected all-green/zero-failure result. Read those
identifiers from the current `.asmdef` and test source rather than guessing.

## Authorized conversation-document exception

A requirements/reference document explicitly uploaded or attached by the user
in the current conversation is an allowed input at its exact resolved path,
including an app temporary path outside the Builder Pack. This includes PDFs,
images, Word documents, spreadsheets, text requirements, and similar non-code
materials.

This exception does not authorize directory enumeration or sibling files. A
source repository, code archive, Unity project, scene, prefab, or implementation
script requires separate explicit approval and remains subject to the Gold
boundary.

## Foundation defect ownership

### Git distribution byte integrity

GitHub requires neither CRLF nor LF for repository files. This Pack uses
`* -text` in `.gitattributes` so its already-reviewed LF, CRLF, mixed text and
binary bytes stay identical in the Git index, clone and source archives.
Do not add text/eol/encoding/filter/ident/export-subst overrides or mass-convert
source files. Unity RoomLighting retains its explicitly published LF format.
All source/copy/directory SHA-256 checks remain strict. No hashes are refreshed
by the Builder after download, and a resumed run preserves its original baseline.

Publish a clean master containing no current-Lab output, baseline/receipts,
Unity caches, restricted old-Lab archive or maintainer material. Include hidden
`.agents`, `.gitattributes` and `.gitignore`. A maintainer who publishes into an
existing Git index must stage new files and re-index tracked files with the
new attributes; changing `.gitattributes` alone does not repair old blobs.
Validate a fresh clone and a source ZIP against the reviewed full-file hashes
before publishing. Keep repository/root name `EMVR_Blind_BuilderPack`.

The `gate5_git_byte_preservation_v1` compatibility profile registers only its
eight named existing tooling/instruction files in a pending Gate 5 run. No
asset, source-binding or baseline update is authorized by this profile. Only
the maintainer may register its precise hash plan, retaining all prior receipts,
approvals and evidence. It does not grant Gate 5 completion.

The Builder Agent owns only the current Lab's generated specifications,
non-example contracts, declared Lab-specific Unity output, and evidence. The
workflow infrastructure, repository `.gitattributes`/`.gitignore`, Skills, policies, tools, Common package, reusable
code, models/assets, knowledge base, dependencies, project/package settings,
templates, and pre-existing shared Unity files are protected foundation owned
by the user.

`ReusableFoundation/` is protected foundation. The controlled Gate 5 copy is
generated output only in location/provenance terms; it remains immutable. A
hash mismatch in either source or copy is a foundation blocker, not permission
to regenerate, patch, or replace it manually.

The controlled exception is automatic blocker recovery at any active gate.
`labflow retrospective` creates numbered incidents in
`BuildEvidence/<current_lab_id>/blocker-retrospectives/`. After verified resolution,
the command seals the report and appends only to the current-run
`KnowledgeBase/rebuild-lessons/<current_lab_id>.md`. Existing lesson/report hashes
must match before extension; unowned files and earlier entries cannot be replaced.
No user acceptance or separate Gate 10 approval is required to start recovery.
See the Skill's `references/workflow-gates.md` for the full automatic subflow.

When a protected item is incorrect or incomplete, the Agent must stop the
current gate, open a protected-foundation retrospective, and report its path,
evidence, impact, and the user action needed.
It must not edit the item, weaken validation, introduce a local workaround,
copy in a patched replacement, or provide ready-to-paste code, patches, or
repair commands. Work resumes only after the user performs the repair and asks
the Agent to verify it.

## Per-run isolation and approved asset import

Use exactly one active Lab run per Builder Pack. A new run must start from a
pristine foundation copy with no previous LabSpec, canonical contract,
Lab-specific Unity output, or run evidence. Resume an existing run in place;
never let a new target read an earlier run's output.

`ApprovedAssets/` is outside `UnityProject/Assets` and cannot be instantiated by
Unity directly. Copy only the selected asset plus its approved GUID dependency
closure with the Skill importer into
`UnityProject/Assets/Generated/<lab_id>/ApprovedDependencies/`. Preserve the
import manifest under `BuildEvidence/<lab_id>/approved-asset-imports/`; do not
copy into shared `UnityProject/Assets/ApprovedReuse/` during a run.


`gate5_emvrlabs_room_sources_v1` additionally permits the six reviewed Part 02,
Part 03 and Light_01 asset/meta files and `Policies/emvr-room-assembly.json`,
plus its named instruction/tool/inventory updates. Addition receipts bind
explicit null before hash/size and exact after hash/size. Added files become
protected expectations; they are never excluded from later drift checks.
Only the maintainer can issue/register this profile at pending Gate 5 after
Gates 0-4 approval and scaffold compilation. All source/import checks still
apply. Preserve the Gate 0 snapshot, prior receipts/approvals and source
bindings; issue no Unity evidence and accept no unrelated file changes.


## Compact workflow maintenance compatibility

The maintainer-only `gate5_compact_workflow_v1` profile covers its explicitly
listed workflow code, policy, Skill and documentation files during Gate 5. It
preserves source assets, current Lab output, original Gate 0 baseline, existing
receipt chain, all evidence (including stale evidence) and Gates 0-4 approvals.
Gate 5 foundation evidence may already exist; this profile grants no passing
evidence or gate approval. All old/new hashes and the reviewed state are bound.
Existing schema-2.0 runs require no baseline recapture or state reset. Old Gate
6/8 approvals remain history until explicitly approved under the compact model.
The Builder cannot issue or modify maintenance receipts.


## Original Gate 8: sequential 8A and 8B handbook delivery

Keep original Gate 8 numbering in the sequence 0,1,2,3,4,5,6,8. Read
`.agents/skills/build-unity-vr-lab-blind/references/developer-handbooks.md`.
Accumulate current-project developer material at Gates 0-6 and each resolved
blocker with `handbooks.py collect`; never invent observations at the end.
8A generates independent student PDF and single-Lab developer DOCX/PDF from the
same Gate 6 accepted version. Preserve `Lab <number> <theme>` naming, adding
` Developer Handbook` only for the developer book. Verify content consistency
and every PDF page. Clearly label executed versus document-checked procedures.
8B starts only after 8A is verified. Increment a saved copy of the existing
project Development Handbook: only the current Lab's tagged section, references
to existing general material and verified unique information. Preserve all other
chapters, images and package parts. Reruns update that section without duplicates.
Retain the before-version, increment change list and old issues left untouched;
render/review the whole master PDF. Never regenerate the entire master or turn
its old implementation details into new Lab requirements or reusable sources.
All three manuals bind the same acceptance; changed final 8A evidence invalidates
8B. Keep current-run outputs in `BuildEvidence/<lab_id>/handbooks/`; host publishing
is a separate release instruction. Retain reuse/validation reports internally.
Submit both steps together for one Gate 8 user confirmation. Neither 8A nor file
existence is approval. Missing captures/results remain pending. Workflow maintenance
does not execute current Lab gates or request its course metadata.

The maintainer-only `gate5_student_manual_workflow_v1` profile registers only its
explicit workflow/rule/template/tool/test changes and seven named additions.
It preserves the original Gate 0 baseline, prior receipts, existing Lab content,
approvals and evidence. Added files use null before-hash/size and become protected
expectations. Registration is maintenance compatibility, never handbook generation
or Lab acceptance. Builder Agents cannot issue, amend or register these receipts.


The maintainer-only `gate5_two_step_handbooks_v1` profile covers its exact
rule/tool/template/test paths and named additions for the 8A/8B extension. It
preserves the original baseline, prior receipts, current Lab code/evidence and
approvals. Receipt registration grants no Lab acceptance, handbook generation,
master publication or protected source access beyond the existing boundaries.

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
