# Summer26 Lab MCP Server

This folder contains a lab-specific MCP server for the Summer26 EM VR labs.

The server reads contract files from:

`../LabInterfaceContracts`

It exposes tools for lab-aware agents to inspect lab interfaces, retrieve step/object/rubric context, validate contract consistency, and build grounded AI-assist payload templates.

## Labs

Labs are discovered when the server starts. A workflow-generated canonical
`<lab_id>_lab_spec.json` is preferred; legacy
`<lab_id>_lab_spec.example.json` remains supported as a fallback. Restart the
MCP server after adding a lab so its tool schemas include the new `lab_id`.

Create and validate a new contract set from the project root with:

```powershell
python tools/labflow/labflow.py new --lab-id <lab_id> --title "<title>" --domain <domain> --scene Assets/Scenes/<Lab>.unity --donor-scene Assets/Scenes/<Donor>.unity
python tools/labflow/labflow.py validate --lab-id <lab_id>
```

## Run

```powershell
python lab_mcp_server.py
```

The server speaks MCP over stdio. It has no third-party Python dependencies.

## Tools

- `list_labs`
- `get_unity_scene_map`
- `get_lab_spec`
- `get_lab_steps`
- `get_lab_objects`
- `get_lab_rubric`
- `get_lab_telemetry_map`
- `get_lab_contract_bundle`
- `get_step_context`
- `get_object_context`
- `build_assist_grounding_template`
- `validate_lab_contracts`

## Example MCP Client Config

Use the absolute path in `mcp_config.example.json`, adjusting the Python executable if needed.
