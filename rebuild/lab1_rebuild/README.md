# Lab 1 Rebuild Package

This folder is a staged rebuild package for `lab1_electrostatics`.

The rebuild rule is: Summer26 code is the source of truth. The scene builder must reuse the existing Summer26 Lab 1 runtime scripts and only add small glue scripts where the original project does not already provide a desktop rebuild workflow.

It was created from:

- `README_FOR_AGENT.md`
- `AI_AGENT_CONTEXT_BUNDLE/`
- `common/`
- `LabInterfaceContracts/`
- `LabMCP/`
- `E:/暑研/EM_VR_Labs/Docs/Handbook/Lab 1 Electrostatics.pdf`

The package does not directly modify the current Unity scene. It keeps a Unity overlay under `unity_overlay/Assets/...` so the rebuilt Lab 1 can be reviewed before being copied into the live `Assets` tree.

## Contents

- `docs/lab1_pdf_requirements.md`: distilled requirements from the Lab 1 handbook.
- `docs/lab1_rebuild_design.md`: scene, interaction, physics, and verification design.
- `docs/rebuild_checklist.md`: acceptance checklist based on the Summer26 agent knowledge base.
- `contracts/`: copied Lab 1 interface contracts from `LabInterfaceContracts`.
- `mcp/`: MCP validation and assist grounding notes.
- `unity_overlay/Assets/Editor/Lab1_Electrostatics_Rebuild/`: Editor scene builder that assembles Summer26 Lab 1 objects.
- `unity_overlay/Assets/Scripts/Lab1_Electrostatics_Rebuild/`: glue scripts for desktop drag, status, and presets.

## Summer26 Reuse

The active rebuild reuses these existing project scripts:

- `Assets/Scripts/ElectricField.cs`
- `Assets/Scripts/Sphere.cs`
- `Assets/Scripts/Wire.cs`
- `Assets/Scripts/Plane.cs`
- `Assets/Scripts/ChargingBox.cs`
- `Assets/Scripts/EMVR/Assist/...`

The builder creates scene objects with those components, then adds rebuild-only helper scripts for desktop selection, preset buttons, and status text. Earlier standalone rebuild physics scaffolding was removed from the active `Assets/` path so the generated scene follows the Summer26 Lab 1 implementation.

## Unity Status

The overlay has also been applied into the real Unity project:

- `Assets/Editor/Lab1_Electrostatics_Rebuild/`
- `Assets/Scripts/Lab1_Electrostatics_Rebuild/`

To regenerate:

1. Open the project in Unity and wait for compilation.
2. Exit Play Mode if it is active.
3. Run `Tools > Electrostatics Lab > Rebuild Lab 1 Scene`.
4. Open `Assets/Scenes/Lab1_Electrostatics_Rebuild.unity`.
5. Enter Play Mode and test with the checklist in `docs/rebuild_checklist.md`.

Do not run Unity batchmode by default for this project. Use the Unity Editor menu and provide Game view / Console feedback after generation.
