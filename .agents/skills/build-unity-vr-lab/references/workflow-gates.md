# Workflow gates

## 1. Brief ready

The lab has a stable ID, title, domain, donor scene, output scene, learning
goals, tasks, formulas/units, object inventory, interaction modes, and evidence
requirements. Placeholders may exist only while this stage is active.

## 2. Contracts ready

All five canonical contracts parse and share the same `lab_id`. Object and step
IDs are unique. Entry step, object references, and rubric references resolve.
Baseline assist and telemetry interfaces are explicit.

## 3. Design approved

The user has reviewed the scene and interaction plan and explicitly approved
it. Approval is never inferred from silence or from contract validation.

## 4. Scaffold ready

The generated runtime controller and Editor builder exist. The builder copies
the donor scene if needed and adds only a generated root/controller. This stage
does not prove Unity compilation or correct lab behavior.

## 5. Unity compiled and scene built

The user reports a clean relevant Console after Unity imports/compiles, runs the
builder menu, and runs the common active-scene validator. Record failures as
failures; do not work around them by editing scene YAML.

## 6. Playtest passed

The user completes the core flow in Play Mode, first with desktop input and then
with XR simulation/hardware as available. Visible feedback, expected domain
results, reset/repeat behavior, and evidence captures match the approved design.

## 7. Accepted

Acceptance is a separate manual decision based on the eval checklist and saved
evidence. Do not automatically promote `playtest_passed` to `accepted`.
