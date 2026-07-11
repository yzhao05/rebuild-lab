# Lab 1 Electrostatics README

This folder contains Lab 1 specific logic for the electrostatics VR lab. Shared VR/mouse interaction helpers live in `Assets/Scripts/Common`.

## Scene Entry

Generate or refresh the scene from Unity:

- Menu: `Tools > Electrostatics Lab > Build Lab 1 Scene`
- Output scene: `Assets/Scenes/Lab1_Electrostatics.unity`
- Build log: `tmp/electrostatics-lab-build-progress.log`

Do not run the builder during Play Mode. Exit Play Mode first, wait for script compilation, then run the menu item.

## User Operation

### Desktop Mouse Test

Use this when testing without a headset.

- Move mouse: shows a yellow pointer ray from the camera.
- Hold left mouse button on a charge object: grab/drag the charge.
- Drag a charge near the red charger: add positive charge.
- Drag a charge near the blue charger: add negative charge.
- Drag a charged object into the transparent field cube: field lines and vectors update.
- Click preset pads `1` to `5`: load preset challenge scenes.
- Click `C`: clear preset charges.
- Click `F`: toggle field lines.
- Click `V`: toggle vector map.

### VR Operation

- Use Grip/Select to grab charge objects.
- Hold a charge inside or near the red charger to add positive charge.
- Hold a charge inside or near the blue charger to add negative charge.
- Place charges in the transparent field visualization cube to observe the electric field.
- Use controller ray or direct touch to select pads `1` to `5`, `C`, `F`, and `V`.
- Hover/select an object to show its annotation.

## Preset Pads

- `1`: unequal opposite line charges.
- `2`: point charge plus line charge.
- `3`: line charge plus surface charge.
- `4`: opposite surface charges, near-uniform ROI.
- `5`: rows of point charges approximating plates.

Pads `1`, `2`, and `5` are visually highlighted in red because they are common testing targets.

## Lab 1 Specific Scripts

### `ChargeDistribution`

Enum for source geometry:

- `Point`
- `Line`
- `Surface`

Used by charge objects and the field visualizer to choose the field approximation.

### `ElectrostaticCharge`

Represents one charge source.

Important fields:

- `distribution`: point, line, or surface.
- `chargeUnits`: signed integer charge level.
- `sourceStrength`: scalar multiplier for field strength.
- `displayName`: label text.
- `chargeLabel`: optional `TextMesh` label.

Important methods/properties:

- `SignedStrength`: signed field source value.
- `LineAxis`: current `transform.up`, used for line-charge field geometry.
- `SurfaceNormal`: current `transform.right`, used for surface-charge field geometry.
- `AddCharge(int amount, int maxAbsCharge)`: increments charge and clamps magnitude.
- `SetCharge(int amount)`: sets charge directly.
- `UpdateVisuals()`: updates color and label.

### `ElectrostaticCharger`

Red/blue charging zone.

Important fields:

- `polarity`: `1` for red positive charger, `-1` for blue negative charger.
- `maxAbsCharge`: maximum absolute charge level.
- `chargeInterval`: time between automatic charge increments.
- `effectiveRadius`: distance fallback for mouse/VR tests.

Important method:

- `ChargeImmediately(ElectrostaticCharge charge)`: adds one charge step immediately.

### `ElectrostaticDesktopDraggable`

Desktop mouse dragging for charge objects. Implements `IEmVrMouseDraggable` from `Common`.

Important methods:

- `BeginMouseDrag(Camera cameraToUse, Vector3 screenPosition)`
- `ContinueMouseDrag(Vector3 screenPosition)`
- `EndMouseDrag()`

During dragging it also checks nearby `ElectrostaticCharger` objects, so mouse testing can charge objects reliably.

### `ElectrostaticFieldVisualizer`

Computes and renders electric field lines and vector maps inside the field cube.

Important fields:

- `showFieldLines`
- `showVectorMap`
- `maxFieldLineSteps`
- `integrationStep`
- `fieldLineWidth`
- `vectorWidth`
- `vectorSpacing`
- `vectorLength`
- `pointFieldScale`
- `lineFieldScale`
- `surfaceFieldScale`
- `softening`

Important methods:

- `ForceRefresh()`: rebuilds visual output on next update.
- `SetFieldLinesVisible(bool visible)`
- `SetVectorMapVisible(bool visible)`
- `Contains(Vector3 point)`: checks if a point is inside the stage bounds.
- `FieldAt(Vector3 point, IList<ElectrostaticCharge> charges)`: returns approximated total electric field.

### `ElectrostaticLabController`

Owns Lab 1 presets and global actions.

Important fields:

- `fieldVisualizer`
- `presetChargeRoot`
- `statusLabel`
- `presetMaxCharge`

Important methods:

- `ApplyPreset(int presetId)`: loads preset `1` to `5`.
- `ClearPresetCharges()`
- `ToggleFieldLines()`
- `ToggleVectors()`

Keyboard shortcuts are kept for desktop debugging:

- `1` to `5`: apply preset.
- `C`: clear preset charges.
- `F`: toggle field lines, handled by `ElectrostaticFieldVisualizer`.
- `V`: toggle vector map, handled by `ElectrostaticFieldVisualizer`.

### `ElectrostaticPresetPad`

Clickable/XR-selectable preset pad.

Important fields:

- `controller`
- `presetId`

Important method:

- `Activate()`: calls `controller.ApplyPreset(presetId)`.

It listens to mouse click, XR activate, and XR select events.

### `ElectrostaticLabButton`

Generic Lab 1 control button for `C`, `F`, and `V`.

Actions:

- `ApplyPreset`
- `ClearCharges`
- `ToggleFieldLines`
- `ToggleVectors`

Important fields:

- `controller`
- `action`
- `presetId`

Important method:

- `Activate()`: dispatches the configured action.

### `ElectrostaticScenePresentation`

Runtime presentation and compatibility helper.

Responsibilities:

- Forces a stable front orthographic desktop camera.
- Hides or shrinks XR Device Simulator UI.
- Improves legacy `TextMesh` readability.
- Adds missing VR control buttons in old scenes.
- Adds desktop mouse pointer ray if missing.
- Adds XR fallback ray renderer if missing.
- Highlights preset pads `1`, `2`, and `5`.

## Shared Common Scripts Used By Lab 1

Located in `Assets/Scripts/Common`.

### `IEmVrMouseDraggable`

Common interface for desktop-as-controller dragging. Lab 2 current sources and Lab 3 polarizers should implement this same interface later.

### `ElectrostaticMouseVrPointer`

Shows a yellow mouse pointer ray and routes mouse drag events to `IEmVrMouseDraggable`.

### `ElectrostaticHoverAnnotation`

Shows/hides annotations on mouse hover, XR hover, and XR select.

### `ElectrostaticXrRayFallback`

Draws fallback cyan rays for XR controller interactors and disables invalid line visual components that would otherwise spam errors.

## Expected Test Flow

1. Enter Play Mode.
2. Confirm yellow mouse pointer ray appears in Game view.
3. Click preset `4`; observe near-parallel field lines between opposite plates.
4. Click presets `1`, `2`, and `5`; confirm highlighted red pads trigger scenes.
5. Click `C` to clear preset charges.
6. Drag a free charge near the red charger until it turns red and label shows positive `q`.
7. Drag it near the blue charger until it turns blue and label shows negative `q`.
8. Drag the charged object into the transparent field cube.
9. Toggle `F` and `V` to verify field-line and vector-map visibility.

## Notes

- Existing class names still use `Electrostatic*` to preserve Unity MonoBehaviour references.
- Folder structure now separates shared interaction code from Lab 1 physics code.
- Lab 2 and Lab 3 should reuse `Common` and keep their physics/model code in their own folders.
