# Lab 1 Rebuild Checklist

## Before Applying

- [ ] Review `docs/lab1_pdf_requirements.md`.
- [ ] Review `docs/lab1_rebuild_design.md`.
- [ ] Confirm the copied contracts in `contracts/` still match `LabInterfaceContracts/`.
- [ ] Confirm `mcp/lab1_mcp_validation.json` says `ok: true`.

## Unity Generation

- [ ] Copy `unity_overlay/Assets/Editor/Lab1_Electrostatics_Rebuild` into `Assets/Editor/`.
- [ ] Copy `unity_overlay/Assets/Scripts/Lab1_Electrostatics_Rebuild` into `Assets/Scripts/`.
- [ ] Open Unity and wait for compilation.
- [ ] Exit Play Mode before generating.
- [ ] Run `Tools > Electrostatics Lab > Rebuild Lab 1 Scene`.
- [ ] Confirm the generated scene path is `Assets/Scenes/Lab1_Electrostatics_Rebuild.unity`.

## Scene Safety

- [ ] There is exactly one active `AudioListener`.
- [ ] Game view camera faces the bench directly.
- [ ] Observation zone is on Ignore Raycast and does not block dragging.
- [ ] Text is readable and not mirrored in Play Mode.
- [ ] Status panel is visible.
- [ ] Charge objects have Summer26 `Sphere`, `Wire`, or `Plane` components.
- [ ] Observation zone has Summer26 `ElectricField`.
- [ ] Red and blue charger objects have Summer26 `ChargingBox`.

## Interaction

- [ ] Pressing `1-5` applies visibly different presets.
- [ ] Presets `1`, `2`, and `5` are visually highlighted.
- [ ] Dragging point, line, and plane source objects works with the mouse.
- [ ] Red charger increases positive charge.
- [ ] Blue charger increases negative charge.
- [ ] Pressing `C` clears charges.
- [ ] Status panel updates after selection, charging, and preset changes.

## Visualization

- [ ] Summer26 field lines appear when charged sources are inside the observation zone.
- [ ] Opposite point charges and same-sign point charges look different.
- [ ] Plane-charge preset shows a mostly uniform field region.
- [ ] Mixed preset shows visible superposition.

## Feedback For Next Iteration

After running in Unity, collect:

- Game view screenshot.
- Console screenshot.
- Whether Play Mode was active.
- Which preset and object were tested.
- What looked wrong or unclear.
