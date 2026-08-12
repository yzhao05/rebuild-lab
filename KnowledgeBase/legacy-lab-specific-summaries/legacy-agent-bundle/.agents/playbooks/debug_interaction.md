# Playbook: Debug Interaction

Use this when clicking, dragging, hovering, presets, shortcuts, or VR rays do not behave as expected.

## Inputs To Request

- What object the user tried to interact with.
- Whether they used mouse, keyboard, XR Device Simulator, or real VR controller.
- Whether the scene was in Play Mode.
- Game view screenshot.
- Console screenshot.

## First Checks

- Confirm there are no compile errors.
- Confirm the intended object has an enabled collider.
- Confirm transparent visualization volumes are not intercepting the ray.
- Confirm the object has the correct interaction script.
- Confirm shortcut keys are handled in only one central controller.
- Confirm object role is clear: draggable, clickable, display-only, or calculation region.

## Ray And Collider Rules

- Use larger colliders for small or thin objects.
- Add colliders to `LineRenderer` objects that must be selected.
- Prefer `RaycastAll` plus filtering when display volumes sit in front of interactables.
- Disable colliders on transparent visualization regions unless needed.
- Keep draggable hit areas aligned with what the user visually expects.

## Drag Rules

- For orthographic screen-space interaction, use screen delta or screen-plane projection.
- Avoid hidden 3D depth requirements unless the user can see and control depth.
- Snap dragged objects to the visualization plane when the calculation depends on that plane.
- Add visible selected/active feedback.

## State Feedback

Add or check status text such as:

```text
active objects: ...
selected: ...
current: ...
charge: ...
output: ...
```

The user should be able to tell whether the system recognized the interaction.

## Done When

- The intended object is easy to select.
- The object moves predictably.
- The system visibly confirms the selected or active object.
- Presets and shortcut keys have one clear owner.
- Desktop and VR paths either both work or unsupported paths are documented.
