# Lab 1 Electrostatics Rebuild Runtime

This rebuild is grounded in the existing Summer26 Lab 1 code. It does not replace the canonical physics/object scripts.

## Reused Summer26 Code

- `Assets/Scripts/ElectricField.cs`: electric field-line generation and Summer26 field sampling logic.
- `Assets/Scripts/Sphere.cs`: point-charge runtime state.
- `Assets/Scripts/Wire.cs`: line-charge runtime state.
- `Assets/Scripts/Plane.cs`: plane-charge runtime state.
- `Assets/Scripts/ChargingBox.cs`: red/blue charger behavior.
- `Assets/Scripts/EMVR/Assist/...`: retained indirectly through the existing Lab 1 objects, especially `ElectrostaticsAssistBinder` and interaction logging hooks.

## Rebuild-Only Glue

- `Lab1Summer26SourceRuntime.cs`: desktop dragging, selection, labels, and material feedback around existing `Sphere/Wire/Plane` components.
- `Lab1Summer26ChargerFallback.cs`: click fallback for charging the selected Summer26 charge object.
- `Lab1Summer26PresetController.cs`: report presets using existing Summer26 charge components.
- `Lab1Summer26PresetPad.cs`: mouse-click preset buttons.
- `Lab1RebuildStatus.cs`: visible status text.

The scene builder is under `Assets/Editor/Lab1_Electrostatics_Rebuild/` and creates `Assets/Scenes/Lab1_Electrostatics_Rebuild.unity`.
