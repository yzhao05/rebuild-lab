# Lab 3 Wave Polarization Experiment Guide

## Open The Lab

1. In Unity, run `Tools > Wave Polarization Lab > Build Lab 3 Scene`.
2. Open `Assets/Scenes/Lab3_WavePolarization.unity`.
3. Enter Play Mode.

## Basic Controls

- Drag a linear polarizer or quarter-wave plate into the transparent observation stage.
- The cyan and yellow sine waves are visual displays only; they are not draggable objects.
- Elements are applied from left to right, matching the wave propagation direction.
- Hold `R` while dragging to rotate an element continuously.
- Press `Q` or `E` to rotate the currently selected element.
- Use `1-5` or the bottom preset pads to load challenge scenes.
- Use `C` to clear, `W` to toggle the wave trace, and `O` to toggle the output orbit.

## What To Observe

The right-side result label shows:

- Output polarization type.
- Relative output intensity.
- Phase difference between `y` and `x` field components.

The orange output orbit is the clearest view for distinguishing linear, circular, and elliptical polarization.
The transparent center box is the active observation stage. Only films inside that box are included in the Jones-vector cascade.

## Presets And Report Questions

- `1`: Q1, one linear polarizer. Rotate it continuously and find maximum/minimum output.
- `2`: Q2, one quarter-wave plate first. Identify output polarization and label fast/slow axes.
- `3`: Q3, quarter-wave plate followed by a `y`-axis linear polarizer. Use output intensity for the Poynting vector ratio.
- `4`: Q4, quarter-wave plate configurations for RHCP and LHCP exploration.
- `5`: Q5, linear polarizer plus quarter-wave plate for RHEP cases. Change angles to create different eccentricities.

## Screenshot Suggestions

For each report question, capture:

- The full observation stage.
- The element order from left to right.
- The axis angle labels.
- The output orbit and result label.

## Common Issues

- If an element does not affect the wave, make sure it is inside the transparent observation stage.
- If dragging feels like it only moves the element, hold `R` while dragging to rotate instead.
- If the output looks unchanged, check the element order. Cascaded polarizers generally do not commute.
- If text looks wrong after entering Play Mode, wait one second; the presentation helper refreshes TextMesh clarity at runtime.

## Bonus Q6

The current version intentionally focuses on electric-field polarization. Useful future improvements include:

- Add magnetic-field polarization visualization.
- Add half-wave plates.
- Allow arbitrary incident polarization.
- Add reflected/transmitted wave polarization experiments.
