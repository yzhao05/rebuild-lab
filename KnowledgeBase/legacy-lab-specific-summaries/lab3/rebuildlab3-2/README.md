# Lab 3 Rebuild/New-Lab Prototype Experience: rebuildlab3-2

## Scope

This record summarizes the second Lab 3 wave-polarization prototype attempt. The user requirement was not to rebuild old Lab 3 directly, but to build a new lab prototype based on Summer26 public code, public room/background, and public scene conventions.

The active implementation was under:

```text
E:/暑研/tryrebild/EMVRLABS-Summer26
```

Main generated scene target:

```text
Assets/Scenes/TryNewLab3_WavePolarizationPrototype.unity
```

Main generator:

```text
Assets/Editor/TryNewLab/TryNewLab3WavePolarizationSceneBuilder.cs
```

## Reused Requirements

- Reuse the Summer26 Lab 3 donor scene `Assets/Scenes/Wave_Polarization.unity`.
- Preserve the shared room/background, lighting, render settings, and XR scene conventions.
- Reuse existing Lab 3 concepts and code where practical:
  - `WaveField.cs`
  - `ChargedObjects.Polarizer`
  - `LinearPolarizer.cs`
  - `QuarterPlate.cs`
- Add new experiment content under a separate generated root:

```text
NewLab3_WavePolarization_Generated
```

- Keep Unity validation in the visible Editor; do not proactively use batchmode.

## Handbook Requirements Used

The Lab 3 handbook requires students to observe and reason about:

- a 45-degree linearly polarized incident wave;
- linear polarizer projection and maximum/minimum transmitted amplitude;
- quarter-wave plate fast/slow axes and 90-degree relative phase shift;
- circular and elliptical polarization from combinations of polarizers;
- screenshots and reasoning for report questions Q1-Q5.

## What Was Added

- `NewWavePolarizationVisualizer`: phasor-based electric-field vector display and output orbit.
- `NewWavePolarizationPlate`: adapter for linear polarizers and quarter-wave plates.
- `NewWavePolarizationPresetPad`: preset controls for incoming, LP max/min, circular-like, and elliptical states.
- `NewWavePolarizationReuseManifest`: documents reused Summer26 scene/code conventions inside the generated scene.
- `NewWavePolarizationStepGuide`: short step guide text.
- `newlab3_wave_polarization_prototype_*.example.json` contracts.
- MCP registry entry for `newlab3_wave_polarization_prototype`.

## Problem-Solution Index

### Q1. Is seeing one wave normal?

Problem:
- The user entered Play Mode and saw one wave-like line of arrows.
- This was partly expected conceptually, because Lab 3 is about one incident plane wave passing through optical elements.
- But the visible wave was mostly the old donor scene's `WaveField` clones (`Arrow (1)(Clone)`), not the newly generated prototype visualization.

Root cause:
- The generator copied `Wave_Polarization.unity` and kept the old Lab 3 experiment active.
- The old donor `WaveField` instantiated arrow clones during Play Mode.
- The new layer existed, but it did not dominate the runtime view.

Fix:
- Separate shared infrastructure from donor experiment content.
- Preserve the room/background and XR setup.
- Disable old donor experiment objects such as `Arrow (1)`, old polarizers, old `QuarterPlate`, old stage cube, and donor `WaveField`.
- Keep the generated prototype under `NewLab3_WavePolarization_Generated`.

Lesson:
- "Reuse donor scene" does not mean keep all donor experiment objects active. Reuse the room/background and public components, then isolate old lab-specific runtime generators unless intentionally needed.

### Q2. Why did everything disappear after hiding the donor experiment layer?

Problem:
- After hiding the old donor wave/objects, the user saw only the shared room and controllers.
- The new wave, polarizers, and controls were not visible.

Root cause:
- Original Lab 3's experiment region is not centered at world origin.
- The old `WaveField.Start()` explicitly begins at:

```csharp
StartPosition = new Vector3(-28, 1, 0);
```

- The new generated Lab3 objects were initially placed near `x = 0`.
- The copied room/XR camera made the user look at the old Lab 3 experiment region near `x = -28`, so the new layer was outside the expected view.

Fix:
- Add a Lab 3 generated-layer offset:

```csharp
private static readonly Vector3 GeneratedLabOffset = new Vector3(-28f, 0f, 0f);
```

- Route all generated object positions through:

```csharp
private static Vector3 Lab3WorldPosition(Vector3 localPosition)
{
    return GeneratedLabOffset + localPosition;
}
```

- Make the repair menu rebuild the generated layer at the corrected offset:

```text
Tools > TryNewLab > Lab 3 > Repair Current Wave Polarization Prototype
```

Lesson:
- When reusing a donor scene, inspect the donor experiment area's actual world coordinates before adding a generated layer. A common room can have lab-specific coordinate offsets.

### Q3. Why did the first fix create a new regression?

Problem:
- The first reaction was to hide old donor Lab 3 objects.
- This removed visual clutter, but also exposed that the new generated layer was misplaced.

Root cause:
- The fix addressed visibility conflict but not scene-coordinate reuse.
- The generator did not yet encode Lab 3's existing experiment-space origin.

Fix:
- Apply changes in two dimensions:
  - disable donor experiment objects that should not run in the new prototype;
  - place the generated layer where the donor experiment actually lived.

Lesson:
- In Unity scene-reuse work, classify both object roles and coordinate spaces. Hiding old objects without checking where the new objects are placed can turn "wrong object visible" into "nothing visible."

### Q4. Why must quarter-wave plates be handled carefully?

Problem:
- The new prototype reused `ChargedObjects.Polarizer` for both linear polarizers and quarter-wave plates.
- The old `Polarizer.Update()` forces `linear == false` objects to rotation `(0,0,0)` every frame.
- This conflicts with the handbook requirement to reason about quarter-wave plate fast/slow axes.

Root cause:
- The old script encoded the old Lab 3 behavior, where QWP orientation was effectively locked.
- The new prototype needed QWP axis labels and orientation-controlled phase behavior.

Fix:
- Keep `ChargedObjects.Polarizer` as a semantic reuse marker.
- For quarter-wave plates, disable the legacy `Polarizer` component's runtime behavior so the new adapter controls angle.
- Keep legacy `Polarizer` enabled for linear polarizers where its rotation constraint is useful.

Lesson:
- Reusing public code can mean reusing semantics, naming, and component contracts, not blindly enabling all old runtime behavior.

### Q5. What should future agents verify before declaring a new lab prototype usable?

Checklist:
- In Play Mode, confirm the visible content is the generated prototype, not donor runtime clones.
- Confirm the shared room/background remains visible.
- Confirm the generated layer is in front of the active XR camera or desktop preview camera.
- Confirm all generated objects are under the expected root, such as `NewLab3_WavePolarization_Generated`.
- Confirm exactly one EventSystem and one active AudioListener.
- Confirm the repair menu can patch an already-open scene.
- Confirm old donor lab runtime scripts are disabled only when they are experiment-specific, not shared infrastructure.

## Final Implementation Pattern

Preferred builder flow:

1. Copy donor scene:

```text
Assets/Scenes/Wave_Polarization.unity
```

2. Preserve shared environment:

```text
_Level
Door1
Canvas/Button where relevant
XR Interaction Setup / Teleport Area
```

3. Hide donor experiment layer:

```text
Arrow (1)
LinearPolarizer (1)
Polarizer
QuarterPlate
Cube
WaveField
```

4. Add generated layer:

```text
NewLab3_WavePolarization_Generated
```

5. Place generated layer in Lab 3's donor experiment coordinate region:

```text
x = -28 offset
```

6. Add repair menu to rebuild and re-show the generated layer.

## Important Warning For Future Labs

Do not assume the donor lab experiment is centered around `(0,0,0)`. Lab 3's old wave starts at `(-28, 1, 0)`. A future generated lab that copies a donor scene should inspect representative source/object coordinates before choosing object positions.

## Files To Show Future Agents

When asking another agent to continue Lab 3 or build a later wave lab, show:

```text
summary/lab3/rebuildlab3-2/README.md
.agents/knowledge/lab_issue_catalog.md
Assets/Editor/TryNewLab/TryNewLab3WavePolarizationSceneBuilder.cs
Assets/Scripts/EMVR/NewLabWavePolarization/
LabInterfaceContracts/newlab3_wave_polarization_prototype_*.example.json
```
