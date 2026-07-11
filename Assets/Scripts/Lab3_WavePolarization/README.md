# Lab 3 Wave Polarization README

This folder contains Lab 3 specific logic for the wave polarization VR lab. Shared VR/mouse helpers live in `Assets/Scripts/Common`.

## Scene Entry

Generate or refresh the scene from Unity:

- Menu: `Tools > Wave Polarization Lab > Build Lab 3 Scene`
- Output scene: `Assets/Scenes/Lab3_WavePolarization.unity`
- Build log: `tmp/wave-polarization-lab-build-progress.log`

Do not run the builder during Play Mode. Exit Play Mode first, wait for script compilation, then run the menu item.

## User Operation

### Desktop Mouse Test

- Move mouse: shows a yellow pointer ray from the camera.
- Hold left mouse button on a polarizer or quarter-wave plate: grab and drag it.
- The cyan input wave and yellow output wave are display-only traces.
- Hold `R` while dragging: rotate the selected element continuously.
- Press `Q` or `E`: continuously rotate the currently selected element.
- Drag elements into the transparent observation stage. Elements are applied from left to right.
- Press or click `1-5`: load guided challenge scenes for Q1-Q5.
- Press or click `C`: clear preset elements.
- Press or click `W`: toggle wave trace.
- Press or click `O`: toggle output polarization orbit.

### VR Operation

- Use Grip/Select to grab linear polarizers and quarter-wave plates.
- Place elements in the observation stage from source side to output side.
- Use controller rays or direct touch to select preset pads and control buttons.
- Hover/select an object to show its annotation.

## Physics Model

The incoming wave is a 45-degree linearly polarized plane wave represented by a Jones vector:

```text
E_in = [1/sqrt(2), 1/sqrt(2)]
```

Elements are sorted by their world `x` position inside the observation stage and applied as a cascade.

- Linear polarizer: `sigma = q q`
- Quarter-wave plate: `sigma = i f f + s s`

The visualizer classifies the resulting state as linear, RHCP, LHCP, RHEP, LHEP, or near extinction.

## Lab 3 Specific Scripts

### `PolarizationElementType`

Enum for optical component type:

- `LinearPolarizer`
- `QuarterWavePlate`

### `PolarizationElement`

Represents one polarization component.

Important fields:

- `elementType`
- `axisAngleDegrees`
- `label`
- `angleLabel`
- `axisVisual`

Important methods:

- `SetAngle(float angleDegrees)`
- `AddAngle(float deltaDegrees)`
- `ApplyTo(ref JonesVector wave)`

### `PolarizationDesktopDraggable`

Desktop mouse dragging and continuous rotation. Implements `IEmVrMouseDraggable`.

Important controls:

- Drag normally to move the element.
- Hold `R` while dragging to rotate continuously.
- Press `Q` or `E` after selecting an element to rotate continuously.

### `PolarizationWaveVisualizer`

Computes the cascaded output Jones vector and renders:

- Incident wave trace.
- Output wave trace.
- Output polarization orbit.
- Result label with classification, intensity, and phase difference.

Important methods:

- `ForceRefresh()`
- `SetWaveTraceVisible(bool visible)`
- `SetOrbitVisible(bool visible)`
- `CalculateOutput(IList<PolarizationElement> elements)`
- `Classify(JonesVector wave)`

### `PolarizationLabController`

Owns Lab 3 presets and global actions.

Important methods:

- `ApplyPreset(int presetId)`
- `ClearPresetElements()`
- `ToggleWaveTrace()`
- `ToggleOrbit()`

### `PolarizationLabButton`

Clickable/XR-selectable button for clear, visibility toggles, and presets.

### `PolarizationScenePresentation`

Runtime presentation and compatibility helper.

Responsibilities:

- Forces a stable front orthographic desktop camera.
- Hides XR Device Simulator UI.
- Keeps exactly one active Audio Listener.
- Keeps TextMesh labels readable.

### `PolarizationElementFactory`

Creates runtime element GameObjects for both the scene builder and Play Mode presets.

## Notes

- The first implementation focuses on electric-field polarization. Magnetic-field polarization is left as a future improvement, matching the handbook's further-development section.
- Blue squares are linear polarizers. Orange squares are quarter-wave plates. Drag these films into the observation stage.
- Rotation is continuous, not quantized to preset angles.
- The transparent observation stage does not participate in mouse raycast hits, so it will not block draggable elements.
