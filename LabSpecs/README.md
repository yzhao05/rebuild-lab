# LabSpecs

Each workflow run owns one directory:

```text
LabSpecs/<lab_id>/
  brief.yaml
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

Only one run may be visible in a Builder Pack. To build another Lab, start from
a pristine foundation copy rather than retaining the previous run's LabSpec,
contracts, Unity output, or evidence.

Do not scaffold until contracts validate and `design.md` has been approved. The
scaffold creates a new scene and must not copy a Gold/donor scene.
