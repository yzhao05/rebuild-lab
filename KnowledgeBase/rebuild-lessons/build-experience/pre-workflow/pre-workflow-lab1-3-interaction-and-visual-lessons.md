---
knowledge_kind: rebuild_lessons
source_run_type: pre_workflow_legacy
source_scope: lab1_lab2_lab3
verified: true
migrated_on: 2026-08-11
source_material:
  - KnowledgeBase/legacy-lab-specific-summaries/legacy-agent-bundle/.agents/knowledge/common_lessons.md
  - KnowledgeBase/legacy-lab-specific-summaries/legacy-agent-bundle/.agents/playbooks/debug_interaction.md
  - KnowledgeBase/legacy-lab-specific-summaries/legacy-agent-bundle/.agents/playbooks/debug_visual_layout.md
  - KnowledgeBase/legacy-lab-specific-summaries/lab1/Lab1_TryNewLab_Rebuild_Merged_Experience.md
  - KnowledgeBase/legacy-lab-specific-summaries/lab2/Lab2_Magnetostatics_Issue_Summary.md
  - KnowledgeBase/legacy-lab-specific-summaries/lab3/Lab3_WavePolarization_Issue_Summary.md
---

# Pre-workflow Lab 1-3 interaction and visual lessons

This file normalizes recurring lessons from rebuilds performed before the
current Gate workflow. It intentionally excludes old Lab answers, exact scene
wiring, fixed coordinates, class names, and implementation code.

## Verified reusable failure patterns

### Build and feedback loop

- Long or hidden Unity batch-mode runs produced poor observability. Prefer an
  Editor menu builder, user-owned compilation/Play Mode, and Game/Console
  evidence.
- A large first implementation allowed unrelated errors to accumulate. Start
  with the smallest runnable scene and interaction slice, verify it, then add
  visual polish and additional behavior.
- A script running or printing a log does not prove the required step/event
  chain advanced. Verify the expected event, object identity, and visible state.

### Pointer, collider, and drag behavior

- Transparent display volumes can intercept mouse/XR rays and make an object
  selectable only near the screen edge. Disable display-only colliders or use
  filtered multi-hit raycasts that prefer intended interactables.
- Thin lines, coils, films, and small controls need hit areas larger than their
  visible geometry. Match colliders to user expectations, not renderer bounds
  alone.
- In orthographic or visually two-dimensional tasks, full 3D containment can
  reject an object that visibly appears inside a target. Use the authored
  screen plane when appropriate and snap dragged objects to the calculation
  plane.
- Ray/plane drag can jump when the selected object's original depth differs
  from the visualization plane. Screen-delta or stable plane projection avoids
  this discontinuity.
- A shortcut handled by multiple controllers may toggle twice and appear to do
  nothing. Give each key and state transition one owner.

### Scene and lifecycle

- Duplicate runtime managers, observation zones, or event chains create mixed
  visuals and duplicate telemetry. Search for and adopt the intended instance
  before creating a fallback.
- Keep one active `AudioListener`; separate preview and XR camera roles; verify
  the camera that actually renders Game View.
- Editor scene APIs and builder menus must not run in Play Mode. Guard them and
  display a clear instruction to exit Play Mode.
- Reset must restore object transforms, parameters, derived visualizations,
  selection, status, and step state together.

### Visual meaning and UI

- Correct calculations were often visually indistinguishable. Show the
  geometry or vectors that separate the phenomena, and choose presets with an
  immediately visible contrast.
- Status text should report what the system currently recognizes: selected
  object, active objects, parameter value, output state, and completed action.
- Distinguish draggable controls from display-only geometry through labels,
  placement, color, cursor/ray feedback, and documentation.
- Validate text orientation and density in runtime Game View. Short,
  object-adjacent labels and separated instruction/status/result regions are
  more reliable than dense panels.
- A world-space Canvas or TextMesh is directional. Determine the camera that
  actually renders Play Mode after XR initialization, compute the normalized
  direction from the surface to that camera. For Unity Canvas and TextMesh, the
  readable front normal is `-Transform.forward`, so require a positive dot
  product with that front normal. Testing `Transform.forward` instead reverses
  the rule and creates the mirrored result it was meant to prevent. A
  non-positive front-normal result means the camera is viewing the back even
  when Scene View looks correct.
- Give orientation one owner. Face the UI toward the runtime camera exactly
  once; do not rotate both the camera and UI to compensate, because the two
  changes can cancel and conceal the cause. Make the scene builder fail before
  save when the authored initial camera sees a UI back face, then verify the
  result in Play Mode Game View.
- Update the experiment guide whenever interaction semantics change.

## Ownership and allowed fix location

- Current-Lab colliders, drag projection, key ownership, labels, camera layout,
  event wiring, and documentation: fix in declared current-Lab output.
- Shared pointer/XR/state behavior that already exists in Common: reuse the
  Common implementation; report a verified Common defect instead of replacing
  it locally.
- Approved environment assets and protected shared files: report defects to the
  user; do not edit during a rebuild.

## Prevention checks by gate

- Gate 0: read prior lessons and confirm Unity validation is user-owned.
- Gate 1: specify interactive versus display-only roles, initial state, first
  desktop action, visible result, the default functionally equivalent XR path,
  any explicitly confirmed semantic exceptions, and reset behavior.
- Gate 3: assign one input owner per shortcut; document ray filtering,
  colliders, drag plane, runtime camera, UI regions, the world-space UI facing
  check, and event evidence.
- Gate 4: compile only the scaffold and reject duplicate managers/listeners.
- Gate 5: prove one object can be selected, moved, recognized, reset, logged,
  and visibly confirmed; also prove the real room and non-mirrored world-space
  UI are visible from the runtime camera before adding the full Lab.
- Gate 6: inspect cleared Console and actual Game View, not Scene View alone.
- Gate 7: separately test desktop, XR simulator, lifecycle, and deferred real
  hardware.

## API and lifecycle constraints

- Read installed XRI and Common event/ray signatures before wiring callbacks.
- Use the runtime rendering camera when orienting text or converting pointer
  movement.
- Apply per-renderer state with a safely initialized property block; avoid
  unintended material instantiation in edit mode.
- Event success requires the expected action ID and identified Lab object, not
  only a collision or debug log.

## Search cues for future rebuilds

`transparent collider`, `ray blocked`, `second drag fails`, `hidden z depth`,
`orthographic drag jump`, `thin object hard to select`, `double toggle`,
`duplicate manager`, `step does not advance`, `mirrored text`, `UI overlap`,
`preset looks unchanged`, `reset incomplete`.

## Exclusions

Do not derive target formulas, answers, rubric rules, old object IDs, exact
coordinates, scene names, or implementation code from this file.
