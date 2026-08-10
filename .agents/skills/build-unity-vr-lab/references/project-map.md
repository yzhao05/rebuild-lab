# Project map

- `AGENTS.md`: non-negotiable repository workflow and safety rules.
- `README_FOR_AGENT.md`: historical knowledge routes and Unity collaboration rules.
- `LabSpecs/<lab_id>/brief.yaml`: user intent, domain requirements, scene plan,
  interaction modes, and acceptance needs.
- `LabSpecs/<lab_id>/design.md`: design approval artifact.
- `LabSpecs/<lab_id>/workflow_state.json`: machine-readable gate and evidence state.
- `LabInterfaceContracts/<lab_id>_*.json`: five canonical agent/MCP contracts.
- `tools/labflow/labflow.py`: workflow state machine and scaffold generator.
- `LabMCP/lab_mcp_server.py`: contract reader for lab-aware agent tools.
- `Assets/Scripts/EMVR/Labs/<LabName>/`: lab-specific runtime/domain code.
- `Assets/Editor/<LabName>/`: donor-scene-based Editor builder.
- `Packages/manifest.json`: must reference the local `com.emvr.lab-common` package.
- `evals/`: rebuild evidence records and acceptance results.

Prefer canonical `.json` contracts. Legacy `.example.json` files are references,
not templates to copy without review.
