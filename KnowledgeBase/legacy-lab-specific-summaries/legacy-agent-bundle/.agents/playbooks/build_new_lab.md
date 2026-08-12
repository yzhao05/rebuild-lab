# Playbook: Build A New Unity VR Lab

Use this when starting a new lab from a handbook or lab specification.

## 1. Read First

- Read `Agents.md`.
- Read `summary/` files for earlier labs.
- Read `.agents/unity-vr-lab-agent.md`.
- Read `.agents/knowledge/common_lessons.md`.
- Inspect existing lab script structure under `Assets/Scripts/`.
- Inspect existing builders under `Assets/Editor/`.

## 2. Extract Lab Requirements

Create a brief design note before implementation:

- Lab goal.
- Student tasks or report questions.
- Physics quantities to compute.
- Objects in the scene.
- Which objects are draggable, clickable, display-only, calculation regions, text, or result feedback.
- Presets and what each preset should demonstrate.
- Desktop testing controls.
- VR/XR controls.

## 3. Plan The File Structure

Default structure:

```text
Assets/
  Editor/
    <LabName>/
      <LabName>SceneBuilder.cs
  Scripts/
    <LabName>/
      <LabName>Controller.cs
      <LabName>Visualizer.cs
      <LabName>DesktopDraggable.cs
      <LabName>Button.cs
      <LabName>PresetPad.cs
      <LabName>ScenePresentation.cs
      README.md
      <LabName>_Experiment_Guide_README.md
```

Reuse `Assets/Scripts/Common/` where possible.

## 4. Implement Minimum Runnable Scene

- Add an Editor menu item under `Tools > <Lab Title> > Build <Lab Number> Scene`.
- Refuse to run the builder in Play Mode.
- Create the main camera or XR rig intentionally.
- Ensure there is only one active `AudioListener`.
- Create scene objects, labels, lights, colliders, and materials.
- Save the scene under `Assets/Scenes/`.

## 5. Add Interaction

- Add desktop mouse path first.
- Add clear hit areas for draggable objects.
- Disable or filter transparent colliders that block interaction.
- Add VR/XR support or fallback rays if required.
- Add status feedback for recognized objects and state changes.

## 6. Add Physics Visualization

- Implement the simplest correct model first.
- Display the core phenomenon in a way the user can see immediately.
- Make presets visually distinct.
- Keep calculation and display code separated enough to debug.

## 7. Document

- Write or update implementation README.
- Write or update experiment guide README.
- Include scene path, menu path, controls, presets, expected observations, screenshots to collect, and common issues.

## 8. Verify With User Feedback

Ask the user to:

- Open Unity Editor.
- Exit Play Mode.
- Run the builder menu.
- Open the generated scene.
- Enter Play Mode.
- Provide Game view and Console screenshots.

Then iterate on concrete feedback.
