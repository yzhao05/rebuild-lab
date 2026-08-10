# LabSpecs

Each workflow run owns one directory:

```text
LabSpecs/<lab_id>/
  brief.yaml
  workflow_state.json
  design.md
  evidence/
```

Create a run with `python tools/labflow/labflow.py new ...`. The brief is the variable input for one rebuild or new lab. Project rules and historical knowledge stay in `AGENTS.md`, `README_FOR_AGENT.md`, and the agent context bundle.

Do not scaffold until contracts validate and `design.md` has been approved.
