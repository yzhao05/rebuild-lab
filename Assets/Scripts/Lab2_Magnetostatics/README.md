# Lab 2 Magnetostatics README

This folder contains Lab 2 specific logic for the magnetostatics VR lab. Shared VR/mouse interaction helpers live in `Assets/Scripts/Common`.

## Scene Entry

Generate or refresh the scene from Unity:

- Menu: `Tools > Magnetostatics Lab > Build Lab 2 Scene`
- Output scene: `Assets/Scenes/Lab2_Magnetostatics.unity`
- Build log: `tmp/magnetostatics-lab-build-progress.log`

Do not run the builder during Play Mode. Exit Play Mode first, wait for script compilation, then run the menu item.

## User Operation

### Desktop Mouse Test

- Move mouse: shows a yellow pointer ray from the camera.
- Hold left mouse button on a wire or solenoid: grab/drag the source.
- Drag a source through the red/blue current loader bars: set current direction and one of six magnitude levels.
- Hold `R` while dragging: rotate the source.
- Place energized sources in the transparent visualization cube to observe magnetic field lines.
- Click preset pads `1` to `5`: load guided challenge scenes.
- Click `C`: clear preset sources.
- Click `F`: toggle magnetic field lines.
- Click `V`: toggle direction samples.
- Click `X`: flip current direction for sources inside the stage.
- Click `+` or `-`: adjust current magnitude for sources inside the stage.

### VR Operation

- Use Grip/Select to grab straight wires or solenoids.
- Touch or ray-select current loader bars to set the current level.
- Place energized sources into the visualization cube.
- Use controller ray or direct touch to select preset pads and control buttons.
- Hover/select an object to show its annotation.

## Preset Pads

- `1`: two straight wires with opposite current directions.
- `2`: straight wire plus solenoid comparison.
- `3`: three straight wires with mixed directions and magnitudes.
- `4`: two solenoids for same/opposite direction experiments.
- `5`: Helmholtz-like solenoid pair with a red region of interest.

Pads `1`, `4`, and `5` are visually highlighted because they are common testing targets.

## Lab 2 Specific Scripts

### `MagneticSourceType`

Enum for current source geometry:

- `StraightWire`
- `Solenoid`

### `MagneticCurrentSource`

Represents one current-carrying source.

Important fields:

- `sourceType`: straight wire or solenoid.
- `currentLevel`: signed current level from `-6` to `+6`.
- `sourceStrength`: scalar multiplier for field strength.
- `displayName`: label text.
- `currentLabel`: optional `TextMesh` label.

Important methods/properties:

- `SignedStrength`: signed field source value.
- `SourceAxis`: current `transform.up`, used for wire and solenoid direction.
- `SetCurrent(int level)`
- `AddCurrent(int amount)`
- `FlipCurrent()`
- `UpdateVisuals()`

### `MagneticCurrentEnergizer`

Current loader bar.

Important fields:

- `assignedLevel`: signed current level applied to touched sources.
- `effectiveRadius`: distance fallback for mouse/VR tests.

Important method:

- `TryApply(MagneticCurrentSource source)`: assigns the current level.

### `MagneticDesktopDraggable`

Desktop mouse dragging for current sources. Implements `IEmVrMouseDraggable` from `Common`.

Important methods:

- `BeginMouseDrag(Camera cameraToUse, Vector3 screenPosition)`
- `ContinueMouseDrag(Vector3 screenPosition)`
- `EndMouseDrag()`

During dragging it also checks nearby `MagneticCurrentEnergizer` objects, so mouse testing can set current reliably.

### `MagneticFieldVisualizer`

Computes and renders approximated magnetic field lines and direction samples inside the visualization cube.

Important fields:

- `showFieldLines`
- `showVectorMap`
- `maxFieldLineSteps`
- `integrationStep`
- `fieldLineWidth`
- `vectorSpacing`
- `wireFieldScale`
- `solenoidFieldScale`
- `softening`

Important methods:

- `ForceRefresh()`
- `SetFieldLinesVisible(bool visible)`
- `SetVectorMapVisible(bool visible)`
- `Contains(Vector3 point)`
- `FieldAt(Vector3 point, IList<MagneticCurrentSource> sources)`

### `MagneticLabController`

Owns Lab 2 presets and global actions.

Important fields:

- `fieldVisualizer`
- `presetSourceRoot`
- `statusLabel`
- `presetMaxCurrent`

Important methods:

- `ApplyPreset(int presetId)`
- `ClearPresetSources()`
- `ToggleFieldLines()`
- `ToggleVectors()`
- `FlipAllSourcesInStage()`
- `AddCurrentToSourcesInStage(int amount)`

### `MagneticPresetPad`

Clickable/XR-selectable preset pad.

Important fields:

- `controller`
- `presetId`

Important method:

- `Activate()`: calls `controller.ApplyPreset(presetId)`.

### `MagneticLabButton`

Generic Lab 2 control button for clear, visibility, flip, and current adjustments.

Actions:

- `ClearSources`
- `ToggleFieldLines`
- `ToggleVectors`
- `FlipCurrents`
- `IncreaseCurrent`
- `DecreaseCurrent`
- `ApplyPreset`

### `MagneticScenePresentation`

Runtime presentation and compatibility helper.

Responsibilities:

- Forces a stable front orthographic desktop camera.
- Hides or shrinks XR Device Simulator UI.
- Keeps exactly one active Audio Listener.

## Expected Test Flow

1. Enter Play Mode.
2. Confirm yellow mouse pointer ray appears in Game view.
3. Click preset `5`; observe a Helmholtz-like field concentration in the red ROI.
4. Click presets `1`, `3`, and `4`; confirm source combinations update.
5. Click `X` to flip current directions and observe changed field lines.
6. Click `C` to clear preset sources.
7. Drag a free straight wire through a red or blue current loader bar.
8. Hold `R` while dragging to rotate it.
9. Place it in the transparent cube and toggle `F` / `V`.

## Notes

- Red represents `+I`; blue represents `-I`.
- Current magnitude has six discrete levels per direction.
- The magnetic model is intentionally approximate for responsive VR visualization.
- The field line view is the primary required visualization; direction samples are included as an optional aid.
