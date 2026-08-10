# Lab 1 Rebuild Design

## Grounding

The Summer26 knowledge base says to avoid starting from scratch. This rebuild uses:

- existing Lab 1 scene concept: `Assets/Scenes/Static_Electric_Field.unity`
- existing Summer26 domain scripts as the implementation authority: `ElectricField`, `Sphere`, `Wire`, `Plane`, `ChargingBox`
- shared lessons: no default batchmode, Editor menu generation, one AudioListener, visible status feedback, generous colliders, and no hidden-depth interaction for visually 2D tasks
- MCP contract: `lab1_electrostatics` validates successfully with no issues

The builder should not invent an alternate Lab 1 physics model when Summer26 already has one. It creates a clearer scene around the existing runtime components.

## Rebuilt Scene Target

- Scene path: `Assets/Scenes/Lab1_Electrostatics_Rebuild.unity`
- Menu path: `Tools > Electrostatics Lab > Rebuild Lab 1 Scene`
- Builder path after applying overlay: `Assets/Editor/Lab1_Electrostatics_Rebuild/Lab1ElectrostaticsRebuildSceneBuilder.cs`
- Runtime script path after applying overlay: `Assets/Scripts/Lab1_Electrostatics_Rebuild/`

## Object Roles

- Draggable objects: point charges using `Sphere.cs`, line charges using `Wire.cs`, plane charges using `Plane.cs`.
- Clickable objects: preset pads 1-5, red/blue chargers as click fallbacks.
- State-changing objects: red positive charger and blue negative charger using `ChargingBox.cs`.
- Calculation region: observation zone using `ElectricField.cs`.
- Display-only objects: Summer26 electric field lines, formula labels, workflow labels.
- Feedback object: status panel.

## Interaction Model

- Desktop:
  - click and hold a source to drag it on the screen plane,
  - click red / blue charger to change the selected source,
  - drag a source into red / blue charger to change charge by proximity,
  - press `1-5` to apply presets,
  - press `C` to clear charges.
- VR target:
  - controller ray selection should map to the same selection and drag contract,
  - charger and preset activation should be explicit select/click actions,
  - the rebuild keeps desktop testing stable first.

## Visualization Model

- The rebuild uses `ElectricField.cs`, the existing Summer26 Lab 1 visualization controller.
- Source objects are real Summer26 `Sphere`, `Wire`, and `Plane` components.
- The observation zone keeps a trigger collider for `ElectricField.OnTriggerEnter/Exit`.
- The observation zone is placed on Unity's Ignore Raycast layer so it can detect physics overlap without stealing mouse selection from charge objects.
- `Field_Line_Length`, `Wire_density`, `Plane_density`, and `stepsize` are reduced in the generated scene to keep the rebuild responsive while preserving the Summer26 field-line logic.

## Preset Intent

- `1`: two opposite point charges for attraction / dipole field.
- `2`: two same-sign point charges for repulsion / saddle region.
- `3`: line charge plus point charge for mixed geometry.
- `4`: opposite plane charges for capacitor-like field.
- `5`: mixed point, line, and plane scene for report challenge.

Presets `1`, `2`, and `5` are highlighted because prior Lab 1 testing emphasized them.

## Known Risks Already Addressed

- Builder refuses to run in Play Mode.
- Scene generation is visible through an Editor menu.
- The generated scene has exactly one active AudioListener.
- Transparent observation region uses a trigger collider on Ignore Raycast, avoiding the old ray-blocking problem while still feeding `ElectricField`.
- Text is short and separated by purpose.
- Status feedback reports selected source, charge changes, active sources, and presets.
- Dragging uses screen-plane movement so hidden `z` depth does not control the main interaction.
