# Common Lessons From Lab 1-3

This file is the reusable issue library for future Unity VR teaching labs.

For every concrete issue/fix pair from the Lab 1, Lab 2, and Lab 3 summaries, also read `.agents/knowledge/lab_issue_catalog.md`. This file keeps the high-level rules; the catalog keeps the detailed coverage.

## Workflow Lessons

- Do not default to Unity batchmode in this project. It created poor observability and repeated delays in prior labs.
- Scene generation should be exposed as a Unity Editor menu item.
- The user should manually run scene generation in the Unity Editor, enter Play Mode, and provide Game/Console screenshots.
- Start with a minimum runnable scene, then add visual polish and VR interaction.
- Store only final effective fixes in summaries. Avoid preserving long chains of failed attempts unless they prevent future repetition.

## Unity Scene Lessons

- Keep exactly one active `AudioListener`.
- Separate preview camera and XR camera responsibilities.
- Ensure the actual Play Mode camera matches the intended Game view, not just the Scene view.
- Do not call Editor-only scene creation APIs during Play Mode.
- Guard scene builders so they refuse to run in Play Mode with a clear message.

## Material And Renderer Lessons

- Avoid `renderer.material` in edit mode.
- Use `renderer.sharedMaterial` for shared edit-time material access.
- Use `MaterialPropertyBlock` for per-renderer color/state changes.
- Lazy-create `MaterialPropertyBlock` before calling `GetPropertyBlock`.
- Skip `TextMesh` renderers when applying object color logic to child renderers.

## Text And Layout Lessons

- Use short text blocks with strong contrast.
- Separate workflow, report questions, models/formulas, and status feedback.
- Keep labels close to the object they explain.
- Text must face the runtime camera, not only look correct in Scene view.
- Avoid fixing mirrored text by flipping both camera and text; that can cancel out and hide the real issue.
- Use proven TextMesh settings from earlier labs as the starting point before experimenting.

## Interaction Lessons

- Support desktop mouse testing when practical, even for VR labs.
- Define mouse-to-VR mapping early: hover, select, drag, rotate, and release.
- Use `Physics.RaycastAll` or filtering when transparent objects may sit in front of interactive objects.
- Make hit areas larger than purely visual geometry when the visual object is thin or small.
- `LineRenderer` visuals need colliders that match user expectations.
- Important changes should be explicit clicks or buttons when hidden 3D depth makes drag-trigger behavior unreliable.

## Orthographic Camera Lessons

- In orthographic views, users judge interaction by screen projection.
- Do not require invisible `z` depth alignment when the task is visually 2D.
- For drop zones, observation stages, and visualization planes, prefer screen-plane `x/y` checks when appropriate.
- Snap dragged objects to the visualization plane if the physics display depends on that plane.

## Visualization Lessons

- Correct calculations are not enough; the physical difference must be visible.
- Show the variables that distinguish the phenomenon.
- Use status bars to show what the system currently recognizes.
- Presets should show large, readable differences immediately.
- A visualization region should not be just decorative; it should communicate state and cause/effect.

## Documentation Lessons

- Each lab needs both implementation README and experiment guide README.
- Experiment guides should tell students/testers how to run the lab, what to drag/click, what each preset means, and what to screenshot.
- README files must be updated whenever interaction semantics change.
- Call out non-interactive display-only objects explicitly when they could be mistaken for draggable objects.
