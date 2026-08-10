# Playbook: Write Lab Documentation

Use this when adding or updating README files for a lab.

## Required Files

- `Assets/Scripts/<LabName>/README.md`
- `Assets/Scripts/<LabName>/<LabName>_Experiment_Guide_README.md`

## Implementation README Should Include

- Scene path.
- Builder menu path.
- Main script responsibilities.
- Public controls and keyboard shortcuts.
- Desktop interaction path.
- VR/XR interaction path.
- Known Unity warnings and whether they matter.
- Developer notes for future labs.

## Experiment Guide Should Include

- How to open and run the scene.
- What each visible region means.
- Which objects are draggable.
- Which objects are display-only.
- How to use presets.
- How to perform each report question or experiment task.
- What screenshots or observations to collect.
- Common confusion points.

## Writing Rules

- Write for the tester or student, not only for the programmer.
- Keep steps short and concrete.
- Update docs whenever labels, controls, or object roles change.
- Explicitly say when a visually prominent object is not interactive.

## Done When

- A new tester can run the lab without asking which scene to open.
- A tester can identify all interactive objects.
- Presets are mapped to lab questions or observations.
- The guide matches the current scene behavior.
