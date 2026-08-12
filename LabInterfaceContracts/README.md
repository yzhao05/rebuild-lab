# Lab Interface Contracts

This folder is the contract source of truth for the single current workflow
run. `Tools/labflow/labflow.py new` creates exactly five canonical `.json`
files:

- `lab_spec`
- `objects`
- `steps`
- `rubric`
- `telemetry_map`

Run `python Tools/labflow/labflow.py validate --lab-id <lab_id>` before design
approval or C# scaffolding. The validator checks parsing, shared `lab_id`,
unique IDs, step/object references, rubric/step references, and baseline
telemetry events. It never falls back to old example contracts. Legacy examples
are retained only in the restricted archive and are not blind-rebuild inputs.
