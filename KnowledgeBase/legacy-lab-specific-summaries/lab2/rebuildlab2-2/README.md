# RebuildLab2-2: Lab 2 New-Lab Prototype Experience

This summary records the second Lab 2 attempt in `tryrebild`: it was not meant to rebuild the original Lab 2 directly. The standard was to build a new Lab prototype based on Summer26 public code, public room/background, public scene conventions, LabMCP, and the lessons from the second Lab 1/new-lab rebuild.

## Build Requirement

- Build a new Lab 2-style magnetostatics prototype, not a direct rebuild of the original scene.
- Reuse the Summer26 room/background and scene conventions.
- Reuse common XR, assist, interaction, and visualization code where possible.
- Use `Assets/Scenes/Static_Magnetic_Field.unity` as the environment donor.
- Use Lab 2 handbook requirements: straight wires, solenoids, red/blue current directions, six current magnitudes per direction, field-line observation, and two-coil comparison.
- Keep generated content under a separate root: `NewLab2_Magnetostatics_Generated`.

## Reused From Summer26

- Room/background scene from `Static_Magnetic_Field.unity`, including `_Level`, `Room_Big_Part_01`, doors, floor, walls, props, lighting, and scene scale.
- Existing Lab 2 interactive scene objects such as `Chargebox`, `Visualization_Cube`, and buttons.
- Magnetostatics domain code:
  - `MagneticField.cs`
  - `ChargedObjects.Wire`
  - `ChargedObjects.Solenoid`
  - `Magnetic.compute`
- Assist and object metadata framework under `Assets/Scripts/EMVR/Assist`.
- XRI Starter Assets and XR Device Simulator prefabs.
- LabMCP and LabInterfaceContracts conventions.

## New Content Added

- Editor builder:
  - `Assets/Editor/TryNewLab/TryNewLab2MagnetostaticsSceneBuilder.cs`
- Runtime adapter scripts:
  - `Assets/Scripts/EMVR/NewLabMagnetostatics/NewMagnetostaticsCurrentPreset.cs`
  - `Assets/Scripts/EMVR/NewLabMagnetostatics/NewMagnetostaticsCurrentPad.cs`
  - `Assets/Scripts/EMVR/NewLabMagnetostatics/NewMagnetostaticsFieldSeeder.cs`
  - `Assets/Scripts/EMVR/NewLabMagnetostatics/NewMagnetostaticsReuseManifest.cs`
  - `Assets/Scripts/EMVR/NewLabMagnetostatics/NewMagnetostaticsStatus.cs`
  - `Assets/Scripts/EMVR/NewLabMagnetostatics/NewMagnetostaticsStepGuide.cs`
- New contracts:
  - `LabInterfaceContracts/newlab2_magnetostatics_prototype_*.example.json`
- MCP lab ID:
  - `newlab2_magnetostatics_prototype`
- Unity menus:
  - `Tools > TryNewLab > Lab 2 > Create Magnetostatics Prototype From Summer26 Base`
  - `Tools > TryNewLab > Lab 2 > Fix Current Scene Controller Ray Input`
  - `Tools > TryNewLab > Lab 2 > Optimize Current Scene Performance`

## Problem-Solution Index

### Q1. Should Lab 2 be rebuilt directly?

Problem:

- The user clarified that this should not be another rebuild of the original Lab 2.
- The goal was to prepare a pattern for building future new labs from Summer26 public infrastructure.

Solution:

- Use the original Lab 2 scene as a donor, not as the final authored scene.
- Add new prototype objects under `NewLab2_Magnetostatics_Generated`.
- Keep reusable Summer26 room/background, UI conventions, assist framework, and domain code.

### Q2. Why did Unity report two EventSystems?

Problem:

- Adding XRI prefabs introduced a second `EventSystem`.
- Unity warned: `There are 2 event systems in the scene`.

Solution:

- Add `EnsureOneEventSystem()` to the Lab 2 builder.
- Prefer the EventSystem that uses `InputSystemUIInputModule`.
- Disable duplicate EventSystem objects when generating or patching the scene.

### Q3. Why did moving the view feel very laggy even with low hardware usage?

Problem:

- The scene had duplicate XR rigs: original `XR Interaction Setup` plus generated `TryNewLab_XR_Interaction_Setup`.
- Unity main thread was also recalculating magnetic field lines heavily.
- Low total CPU/GPU usage did not mean the Unity main thread was free.

Solution:

- Add `EnsureSingleXriSetup()` to avoid duplicate active XR rigs.
- Add `Optimize Current Scene Performance` menu.
- Add `redrawInterval` to `MagneticField.cs` so field lines do not recalculate every frame.
- Lower debug defaults for generated prototype field-line density and length.

### Q4. Why did Play Mode show huge field lines or text in the camera?

Problem:

- `MagneticField.Start()` creates field-line GameObjects only at runtime.
- The first generated experiment layer was placed too close to the XR camera origin.

Solution:

- Move generated Lab 2 prototype content forward with `GeneratedLabOffset`.
- Add `LayoutGeneratedLab2Layer()` to fix already-generated scenes.
- Parent runtime field-line GameObjects under the observation zone so they are easier to inspect.

### Q5. Why did the background room disappear?

Problem:

- `_Level` was mistakenly added to the hidden donor-object list.
- `_Level` contains the shared room/background, so hiding it left only the camera clear color.

Solution:

- Remove `_Level` from `HideDonorLab2ExperimentLayer()`.
- Add `EnsureSharedEnvironmentVisible()` to restore `_Level`, `Room_Big_Part_01`, `Door1`, `trash1`, and `Teleport Area`.
- Rule for future labs: never treat room/background roots as disposable experiment objects.

### Q6. Why did the interactive UI disappear?

Problem:

- The early hide list also disabled `Chargebox`, `Chargebox (1)`, `Visualization_Cube`, `Button`, and `Button (Legacy)`.
- These are not just old clutter; they are part of the Lab 2 reusable interaction layer.

Solution:

- Keep Lab 2 interactive objects visible.
- Add `EnsureLab2InteractiveObjectsVisible()` to restore UI and interaction objects.
- Only hide objects after confirming they are truly duplicate/generated clutter.

### Q7. Why were magnetic field lines still invisible after the room and UI came back?

Problem:

- `Wire.Start()` and `Solenoid.Start()` reset `electric_mag` to `0`.
- Visible current-source objects therefore had no effective current in Play Mode.
- Some `MagneticField` components relied on trigger registration, which was fragile after scene edits.
- `MagneticField.Start()` tried to initialize compute shader buffers even when `GPU=false`; if `shader` was missing, startup could fail before line creation.

Solution:

- Add `NewMagnetostaticsCurrentPreset` to visible `Wire` and `Solenoid` objects.
- Keep nonzero startup current during Play Mode.
- Add `NewMagnetostaticsFieldSeeder` to `MagneticField` objects so active wires and solenoids are registered every second.
- Make `MagneticField.Start()` skip compute-shader initialization when `shader == null` and force CPU mode.
- Add null checks in `OnDestroy()` for compute buffers.
- Make field lines white and thicker for debugging visibility.

### Q8. Why did fixing one problem repeatedly break another?

Problem:

- Several fixes were too broad:
  - hiding `_Level` removed the room;
  - hiding `Chargebox` and `Visualization_Cube` removed UI;
  - hiding donor `Wire/Solenoid` removed sources needed by donor visualization.

Solution:

- Use narrow, named recovery functions:
  - `EnsureSharedEnvironmentVisible()`
  - `EnsureLab2InteractiveObjectsVisible()`
  - `EnsureGeneratedPrototypeVisible()`
  - `EnsureMagneticSourceBootstrap()`
  - `EnsureMagneticFieldSeeders()`
- Prefer restoring required shared objects over broad hiding.
- Future builders must classify objects into shared environment, reusable UI, old experiment source, generated prototype source, and debug-only objects before changing active state.

## Current Verification Status

Static checks completed:

- JSON contracts parse.
- LabMCP recognizes `newlab2_magnetostatics_prototype`.
- Builder and modified C# scripts have balanced braces.
- XRI prefab paths and `Magnetic.compute` exist.

Unity Editor verification still required:

- Run `Tools > TryNewLab > Lab 2 > Optimize Current Scene Performance`.
- Enter Play Mode.
- Confirm room/background remains visible.
- Confirm `Chargebox`, `Visualization_Cube`, `Button`, `Wire`, and `Solenoid_*` are active.
- Confirm `MagneticField` has `NewMagnetostaticsFieldSeeder`.
- Confirm field lines are visible and current-source counts are nonzero in Play Mode.

## Future Agent Rules From This Attempt

- Do not hide donor objects by broad name matching until their role is known.
- Room/background roots must be preserved.
- Existing UI/control objects may be reusable infrastructure, not old clutter.
- When source scripts reset physical state in `Start()`, add an adapter or bootstrap to preserve nonzero runtime state.
- Field visualization bugs often come from missing source registration, not from rendering alone.
- Every fix menu should restore known-required objects before optimizing or hiding anything.
