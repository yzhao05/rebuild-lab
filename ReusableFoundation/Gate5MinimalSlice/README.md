# Gate 5 reusable minimal slice

This directory is protected workflow foundation. It contains only cross-Lab
wiring and validation code; it contains no old Lab formula, answer, object ID,
class name, coordinate, or scene wiring.

Builder Agents must not edit these files. At Gate 5 they must run:

```powershell
python .agents/skills/build-unity-vr-lab-blind/scripts/materialize_gate5_foundation.py --lab-id <lab_id>
```

The command verifies every source hash, copies the files byte-for-byte into
`UnityProject/Assets/Generated/<lab_id>/Gate5Foundation/`, marks copied source
files read-only, and writes
`BuildEvidence/<lab_id>/gate5-foundation-import.json`.

The copied foundation is also immutable. Lab-specific code must call or compose
around it. If a source or copied file appears defective, stop the current gate
and report the defect to the user; do not patch either copy during the rebuild.

The slice supplies:

- a runtime harness that calls Common `EmVrReplacementSpawner`, validates one
  active EventSystem, and checks that a spawned object is active, renderable,
  collidable, and inside the validation camera;
- an Editor scene utility for a single XR UI EventSystem, a persistently wired
  button, a neutral active offstage spawn template, and fail-closed build logs;
- three generic EditMode regression tests for inactive-template, visible-spawn,
  and duplicate-EventSystem failures.

It does not build the target domain. The current Lab must add its own smallest
domain object, deterministic calculation, visible result, interaction adapter,
state/reset, telemetry, assist grounding, and domain tests beside this layer.
