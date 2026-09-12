# LabSpecs

Each workflow run owns one directory:

```text
LabSpecs/<lab_id>/
  brief.yaml
  source-references.json
  workflow_state.json
  design.md
  prior-lessons-review.md
  implementation.md (after Gate 4 scaffold)
  evidence/
```

Create a run from the Builder Pack root with
`python Tools/labflow/labflow.py new ...`. The brief is the variable input for
one rebuild or new lab. Project rules and curated reusable knowledge stay in
`AGENTS.md`, `Policies/`, and `KnowledgeBase/`.

Every Brief keeps `interaction_modes.desktop_to_xr_mapping.policy` equal to
`equivalent_by_default`. The Agent derives ordinary XR behavior from each
desktop action through Common/XRI and records only explicitly confirmed
exceptions; normal mouse-to-XR bindings are not Gate 1 User input.

State schema 2.0 records the immutable per-Lab reusable-source manifest and
catalog SHA-256 values, plus the create-once Gate 0 protected baseline path and its
SHA-256. The baseline itself lives under `BuildEvidence/<lab_id>/`; neither the
state metadata nor that file may be manually changed to accept a later
protected-foundation difference.

`source-references.json` contains only repository-relative POSIX paths resolved
from the current Builder Pack root. It passes references to the original
read-only code/assets without changing their contents. Do not edit it or save
the runtime resolver's machine-specific absolute paths. A GitHub clone resolves
the same references from its new local root with `labflow source-references`.

Only one run may be visible in a Builder Pack. To build another Lab, start from
a pristine foundation copy rather than retaining the previous run's LabSpec,
contracts, Unity output, or evidence.

Do not scaffold until contracts validate and `design.md` has been approved. The
scaffold creates a new scene and must not copy a Gold/donor scene.
