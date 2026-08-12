# TryNewLab Build Summary

This summary records the construction rules and lessons from the `tryrebild` new-lab scaffold.

## Build Goal

The goal is not to rebuild Lab 1. Treat the Summer26 project as an existing Unity VR lab codebase, then build a new lab on top of its shared infrastructure.

The correct standard is:

- Reuse the Summer26 room/background and scene style.
- Reuse common XR, interaction, UI, assist, and visualization code where possible.
- Keep new experiment content in a separate layer.
- Avoid hand-editing `.unity` YAML.
- Use Editor menu builders so Unity scene generation is visible and repeatable.
- Let the user compile and test in Unity Editor instead of running hidden batchmode work.

## Reused From Summer26

The `tryrebild` project was copied from the original Summer26 Unity project as an isolated workspace.

Core reused project infrastructure:

- `Assets`
- `Packages`
- `ProjectSettings`
- `LabMCP`
- `LabInterfaceContracts`
- `AI_AGENT_CONTEXT_BUNDLE`
- `common`
- `docs`
- `README_FOR_AGENT.md`

Shared scene/environment elements:

- Summer26 room/background, especially objects such as `Room_Big_Part_01`
- Existing lighting and render style
- Existing camera and scene-entry conventions
- Existing lab room scale and object placement conventions
- `AI_Assist_System`

Shared code and systems:

- XR movement and hand scripts: `Continuous_Movement.cs`, `HandPresence.cs`, `GrabDetect*.cs`
- Step/instruction flow: `Step.cs`, `Start_Game.cs`
- Scene loading scripts: `Scene1.cs`, `Scene2.cs`, `Scene3.cs`, `LoadScene.cs`
- Assist framework: `Assets/Scripts/EMVR/Assist`
- Electrostatics domain code: `ElectricField.cs`, `Sphere.cs`, `Wire.cs`, `Plane.cs`
- XRI Starter Assets and XR Device Simulator samples

Generated Unity folders such as `Library`, `Temp`, `Logs`, `UserSettings`, `.csproj`, and `.sln` are not design inputs. Unity may regenerate them after the project is opened.

## New Content Added

New-lab entry document:

```text
TRYNEWLAB_START_HERE.md
```

New-lab documentation:

```text
docs/tryrebild/README.md
Assets/Labs/NewLab/README.md
summary/README.md
```

New Editor menu builder:

```text
Assets/Editor/TryNewLab/TryNewLabSceneBuilder.cs
```

Runtime new-lab adapter scripts:

```text
Assets/Scripts/EMVR/NewLab/NewLabReuseManifest.cs
Assets/Scripts/EMVR/NewLab/NewLabChargePreset.cs
Assets/Scripts/EMVR/NewLab/NewLabStepGuide.cs
```

New contract files:

```text
LabInterfaceContracts/newlab_electrostatics_prototype_lab_spec.example.json
LabInterfaceContracts/newlab_electrostatics_prototype_objects.example.json
LabInterfaceContracts/newlab_electrostatics_prototype_steps.example.json
LabInterfaceContracts/newlab_electrostatics_prototype_rubric.example.json
LabInterfaceContracts/newlab_electrostatics_prototype_telemetry_map.example.json
```

## Current Unity Menu Commands

Create or replace the new-lab prototype scene:

```text
Tools > TryNewLab > Create Electrostatics Prototype From Summer26 Base
```

Patch an already-open scene with the corrected controller-ray input setup:

```text
Tools > TryNewLab > Fix Current Scene Controller Ray Input
```

Generated target scene:

```text
Assets/Scenes/TryNewLab_ElectrostaticsPrototype.unity
```

## Generated Scene Structure

The generated scene uses:

```text
Assets/Scenes/Static_Electric_Field.unity
```

only as an environment donor. It is not treated as the finished Lab 1 scene.

Expected generated objects include:

- `TryNewLab_XR_Interaction_Setup`
- `TryNewLab_XR_Device_Simulator`
- `NewLab_Generated`
- `NewLab_PublicCodeReuse`
- `NewLab_ExperimentLayer`
- `NewLab_ObservationZone`
- `NewLab_PointCharge_Positive`
- `NewLab_PointCharge_Negative`
- `NewLab_LineCharge`
- `NewLab_PlaneCharge`
- `NewLab_Guide`

The old donor experiment objects such as `Sphere`, `Wire`, `Plane`, `Chargebox`, and old Lab 1 UI buttons are hidden in the generated scene so the new-lab layer is easier to inspect.

## Important Lessons

### 1. Do Not Treat This As Lab 1 Rebuild

The first attempt looked like a Lab 1 rebuild and copied too much old Lab 1 behavior. The better pattern is:

1. Use a Summer26 lab scene as an environment donor.
2. Preserve shared infrastructure.
3. Add a separate new-lab experiment layer.
4. Document what is reused and what is newly generated.

### 2. Room And Background Are Reusable Infrastructure

The room/background is not decoration. It is part of the shared Summer26 lab experience and should be reused unless the user explicitly asks for a new environment.

For future labs, reuse:

- room/background geometry
- doors, floor, wall, and props
- lighting style
- XR origin / camera conventions
- scene navigation conventions

### 3. Contracts Should Exist Before Serious Runtime Wiring

For a new lab, create the five contract files first:

- lab spec
- objects
- steps
- rubric
- telemetry map

Runtime does not fully load these yet, but they keep object IDs, step IDs, assist grounding, and telemetry aligned.

### 4. XR Controller-Ray Issue Was Inherited From Source

The original Summer26 lab scenes include XR-related code and XRI sample assets, but the lab scenes did not clearly use a complete Action-Based XRI rig.

Observed issue:

- controller laser can move left/right
- vertical movement affects the view/HMD instead of the controller ray

Cause found in source inspection:

- `XRDeviceSimulator` requires `ActionBasedController` and Input System `TrackedPoseDriver` style pose input.
- Existing project scripts such as `Continuous_Movement.cs` and `HandPresence.cs` use older `InputDevices.GetDeviceAtXRNode` patterns.
- The XRI Starter Assets input actions are present, so this was not a missing file copy. It was an inherited scene setup gap.

Fix added:

- Generated scenes now add `XR Interaction Setup.prefab`.
- Generated scenes now add `XR Device Simulator.prefab`.
- Existing scenes can be patched with `Tools > TryNewLab > Fix Current Scene Controller Ray Input`.

### 5. Avoid Invisible Unity Batch Work

The agent should not silently run Unity batchmode for this project. Unity compilation, menu execution, and Play Mode checks should be done by the user in the visible Editor.

Ask the user for:

- Console screenshot
- Game view screenshot
- Scene view screenshot
- whether Play Mode is active

## Recommended Future New-Lab Flow

1. Read `README_FOR_AGENT.md`.
2. Read `common/README.md` and `common/common_code_inventory.json`.
3. Read the existing lab summaries and contracts.
4. Decide which existing scene is the environment donor.
5. Create five new contract files for the new lab.
6. Add a new `Assets/Scripts/EMVR/NewLabOrLabName` adapter layer.
7. Add an Editor menu builder under `Assets/Editor`.
8. Preserve room/background, lighting, XRI setup, and assist framework.
9. Generate a separate experiment root such as `NewLab_Generated`.
10. Let the user compile, run the menu, enter Play Mode, and report screenshots/errors.

## Current Verification Done Outside Unity

Static checks completed:

- New JSON contract files parse successfully.
- Main new Editor script has balanced braces.
- XRI setup prefab path exists.
- XR Device Simulator prefab path exists.
- Old `TryRebuild` menu script and old rebuild scene files were removed from the current scaffold.

Unity Editor checks still need user confirmation:

- compilation has no errors
- generated scene appears in Game view
- controller ray can move vertically after XRI setup patch
- new-lab objects show field lines when placed in the observation zone

## Problem-Solution Index

This section was added after reviewing whether the second Lab 1/new-lab rebuild experience had an explicit question-and-solution list.

### Q1. Should this be treated as a Lab 1 rebuild?

Problem:

- The early attempt was interpreted as rebuilding Lab 1.
- The user clarified that the work should prepare for future new labs, not merely reproduce Lab 1.

Solution:

- Treat Summer26 as a reusable VR lab framework.
- Use an existing lab scene only as an environment donor.
- Keep new experiment content under a separate generated root such as `NewLab_Generated`.
- Preserve the shared room/background, XR setup, assist framework, and common code conventions.

### Q2. Should room/background objects be reused?

Problem:

- The first interpretation treated the room/background as optional decoration.
- The user clarified that background and room are part of the common reusable infrastructure.

Solution:

- Reuse the Summer26 room/background, lighting, doors, floor, walls, props, scene scale, and navigation conventions by default.
- Do not hide or delete shared room objects unless the user explicitly asks for a new environment.

### Q3. How should existing Summer26 code be reused?

Problem:

- Rebuilding too much code would make future labs inconsistent and harder to maintain.

Solution:

- Reuse existing domain scripts, shared interaction scripts, assist framework, XRI samples, and contract conventions.
- Add adapter scripts only where old domain code needs a stable startup state or clearer new-lab controls.
- Document what was reused and what was newly generated.

### Q4. Why did the controller ray only move left/right?

Problem:

- The controller laser could move left/right, but vertical motion affected the HMD/view instead of the controller ray.

Solution:

- Source inspection showed this was an inherited scene setup gap, not a missing copied file.
- Add Action-Based XRI `XR Interaction Setup.prefab`.
- Add `XR Device Simulator.prefab`.
- Provide an Editor menu to patch already-open generated scenes.

### Q5. How should future agents verify these generated scenes?

Problem:

- Unity scene problems often only appear in Play Mode, not from static code inspection.

Solution:

- Do not silently run Unity batchmode by default.
- Let the user compile, generate, and test in the visible Unity Editor.
- Ask for Game view, Scene view, Console screenshots, and whether Play Mode is active.
