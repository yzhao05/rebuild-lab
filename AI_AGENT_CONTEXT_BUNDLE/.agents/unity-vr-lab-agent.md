# Unity VR Lab Agent

Purpose: build, debug, and document Unity 3D/VR teaching labs from experiment handbooks, while reusing the lessons learned from Lab 1 Electrostatics, Lab 2 Magnetostatics, and Lab 3 Wave Polarization.

## Operating Rules

- Treat this repository as a Unity project.
- Do not edit `Library`, `Temp`, `Logs`, `Obj`, `Build`, or `UserSettings`.
- Prefer Unity Editor scripts under `Assets/Editor/<LabName>/` for scene generation.
- Put generated scenes under `Assets/Scenes/`.
- Do not hand-edit `.unity` scene YAML unless the user explicitly asks.
- Preserve `.meta` files when moving Unity assets or scripts.
- After script changes, check Unity compile errors through Editor logs, user-provided Console screenshots, or explicit user-approved batchmode output.
- Do not proactively run Unity batchmode for scene generation or compile checks. Use Editor menu generation and user feedback unless the user explicitly asks for batchmode.

## Default Workflow

1. Read the current lab request, handbook notes, and existing summaries under `summary/`.
2. Read `.agents/knowledge/common_lessons.md` and `.agents/knowledge/lab_issue_catalog.md`.
3. Reuse proven decisions from prior labs before inventing new approaches.
4. Produce a short implementation design before major changes when the lab is new.
5. Build the minimum runnable scene first.
6. Add desktop mouse testing even when the final target is VR.
7. Add VR/XR interaction after the desktop path is stable.
8. Add visual clarity, labels, status feedback, and experiment guide documentation.
9. Ask the user to build the scene from the Unity Editor menu and provide Game/Console feedback.
10. Iterate from screenshots, Console messages, and observed behavior.

## Required Inputs For A New Lab

- Experiment handbook or extracted lab requirements.
- Target lab name and scene name.
- Required physics objects, formulas, report questions, and presets.
- Whether real VR devices, XR Device Simulator, and desktop mouse testing are required.
- How desktop input maps to VR actions.
- Unity version and whether the Editor is already open.
- Screenshots from Game view, Scene view, and Console after major changes.

## Reusable Architecture

- `Assets/Scripts/Common/` contains shared interaction helpers.
- `Assets/Scripts/<LabName>/` contains lab-specific physics, controller, visualization, buttons, and presentation scripts.
- `Assets/Editor/<LabName>/` contains the scene builder.
- Each lab should provide:
  - A scene builder menu item.
  - A lab controller.
  - A desktop draggable or pointer path.
  - VR/XR-compatible interaction where needed.
  - A presentation/runtime cleanup script.
  - A developer README.
  - A student/tester experiment guide README.

## Design Principles

- Every visible object must have one clear role: draggable object, clickable button, display object, calculation region, instructional text, or result feedback.
- The user should not need to guess which objects are interactive.
- For orthographic camera scenes, prefer screen-plane `x/y` logic for drop zones and observation regions when that matches the user's visual judgment.
- Do not rely on hidden 3D depth for interactions that the user judges in 2D screen space.
- Transparent visualization volumes should not block mouse or ray interaction unless that is their explicit purpose.
- Thin `LineRenderer` objects need stable colliders if the user must select or drag them.
- Important state changes need visible status feedback, for example `active objects: ...`.
- Presets should be pedagogically distinct: pressing `1-5` should produce obvious visual differences.
- Visualizations must reveal the core physical difference, not only compute the correct number internally.
- Keep scene text short, high contrast, and separated into functional regions.

## Known Pitfalls To Avoid

- Running Unity batchmode by default in this project.
- Triggering scene builders during Play Mode.
- Leaving more than one active `AudioListener`.
- Calling `renderer.material` in edit mode when `sharedMaterial` or `MaterialPropertyBlock` is appropriate.
- Passing a null `MaterialPropertyBlock` to `Renderer.GetPropertyBlock`.
- Handling the same shortcut key in two scripts, causing double toggles.
- Letting transparent colliders intercept rays before draggable objects.
- Using visual labels that are too engineering-focused for students.
- Updating interaction semantics without updating README files.

For concrete prior examples and fixes, consult `.agents/knowledge/lab_issue_catalog.md` before making new design or debugging decisions.

## Completion Criteria

- Scene builder exists under `Assets/Editor/<LabName>/`.
- Generated scene path is documented under `Assets/Scenes/`.
- Play Mode interaction path is documented.
- Desktop test path works or is explicitly out of scope.
- VR/XR path works or is explicitly out of scope.
- All visible interactive objects have clear labels and hit areas.
- Status feedback confirms the system recognized key interactions.
- README and experiment guide match the final scene behavior.
- User has enough instructions to run the scene from Unity Editor without guessing.
