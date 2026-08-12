# Unity VR Lab Acceptance Checklist

Use before calling a lab implementation complete.

## Project Safety

- [ ] Did not edit `Library`, `Temp`, `Logs`, `Obj`, `Build`, or `UserSettings`.
- [ ] Did not hand-edit `.unity` YAML unless explicitly requested.
- [ ] Scene builder is under `Assets/Editor/<LabName>/`.
- [ ] Generated scene path is under `Assets/Scenes/`.
- [ ] `.meta` files were preserved when moving Unity assets.

## Scene Generation

- [ ] Builder has a Unity Editor menu item.
- [ ] Builder refuses to run in Play Mode.
- [ ] Builder creates or updates the expected scene.
- [ ] There is exactly one active `AudioListener`.
- [ ] Camera used in Play Mode matches the intended Game view.

## Interaction

- [ ] Interactive objects have colliders sized for reliable selection.
- [ ] Display-only objects are clearly labeled.
- [ ] Transparent visualization regions do not block interaction.
- [ ] Desktop mouse path is tested or documented as out of scope.
- [ ] VR/XR path is tested or documented as out of scope.
- [ ] Shortcuts are handled in one central owner.
- [ ] Key interactions produce visible status feedback.

## Visualization

- [ ] Physics model computes the required quantities.
- [ ] Visualization reveals the core phenomenon clearly.
- [ ] Presets produce visible differences.
- [ ] Orthographic interactions do not depend on hidden depth unless intentional.
- [ ] Labels explain object roles without overcrowding the scene.

## Documentation

- [ ] Implementation README exists.
- [ ] Experiment guide README exists.
- [ ] Scene path and builder menu path are documented.
- [ ] Controls and presets are documented.
- [ ] Non-interactive display objects are called out.
- [ ] Guide matches the current scene behavior.

## Verification

- [ ] User can generate the scene from Unity Editor.
- [ ] User can enter Play Mode.
- [ ] Console has no blocking compile errors.
- [ ] Game view screenshot confirms layout is readable.
- [ ] User can complete the core experiment flow.
