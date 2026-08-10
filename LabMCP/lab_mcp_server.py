#!/usr/bin/env python3
"""Summer26 lab-specific MCP server.

This server implements a small MCP-over-stdio JSON-RPC endpoint with no
third-party dependencies. It reads the lab interface contracts generated under
../LabInterfaceContracts and exposes lab-aware tools for authoring, review,
AI-assist grounding, and telemetry planning.
"""

from __future__ import annotations

import json
import sys
import traceback
from pathlib import Path
from typing import Any, Callable, Dict, List, Optional, Tuple


SERVER_NAME = "summer26-lab-mcp"
SERVER_VERSION = "0.1.0"

ROOT_DIR = Path(__file__).resolve().parents[1]
CONTRACT_DIR = ROOT_DIR / "LabInterfaceContracts"

CONTRACT_KINDS = {
    "lab_spec": "lab_spec",
    "objects": "objects",
    "steps": "steps",
    "rubric": "rubric",
    "telemetry_map": "telemetry_map",
}


def discover_lab_ids() -> List[str]:
    """Discover canonical workflow contracts, with legacy examples as fallback."""
    lab_ids = set()
    for pattern in ("*_lab_spec.json", "*_lab_spec.example.json"):
        for path in CONTRACT_DIR.glob(pattern):
            suffix = "_lab_spec.example.json" if path.name.endswith(".example.json") else "_lab_spec.json"
            lab_ids.add(path.name[: -len(suffix)])
    return sorted(lab_ids)


LAB_IDS = discover_lab_ids()


class McpError(Exception):
    def __init__(self, code: int, message: str, data: Optional[Any] = None):
        super().__init__(message)
        self.code = code
        self.message = message
        self.data = data


def contract_path(lab_id: str, kind: str) -> Path:
    require_lab_id(lab_id)
    if kind not in CONTRACT_KINDS:
        raise McpError(-32602, f"Unknown contract kind: {kind}")
    stem = f"{lab_id}_{CONTRACT_KINDS[kind]}"
    canonical = CONTRACT_DIR / f"{stem}.json"
    if canonical.exists():
        return canonical
    return CONTRACT_DIR / f"{stem}.example.json"


def require_lab_id(lab_id: str) -> None:
    if lab_id not in LAB_IDS:
        raise McpError(
            -32602,
            f"Unknown lab_id: {lab_id}",
            {"valid_lab_ids": LAB_IDS},
        )


def load_json(path: Path) -> Dict[str, Any]:
    if not path.exists():
        raise McpError(-32000, f"Contract file not found: {path}")
    with path.open("r", encoding="utf-8") as file:
        return json.load(file)


def load_contract(lab_id: str, kind: str) -> Dict[str, Any]:
    return load_json(contract_path(lab_id, kind))


def load_bundle(lab_id: str) -> Dict[str, Any]:
    require_lab_id(lab_id)
    return {
        "lab_id": lab_id,
        "lab_spec": load_contract(lab_id, "lab_spec"),
        "objects": load_contract(lab_id, "objects"),
        "steps": load_contract(lab_id, "steps"),
        "rubric": load_contract(lab_id, "rubric"),
        "telemetry_map": load_contract(lab_id, "telemetry_map"),
    }


def text_result(payload: Any) -> Dict[str, Any]:
    return {
        "content": [
            {
                "type": "text",
                "text": json.dumps(payload, ensure_ascii=False, indent=2),
            }
        ]
    }


def find_step(bundle: Dict[str, Any], step_id: str) -> Optional[Dict[str, Any]]:
    for step in bundle["steps"].get("steps", []):
        if step.get("step_id") == step_id:
            return step
    return None


def find_object(bundle: Dict[str, Any], object_id: str) -> Optional[Dict[str, Any]]:
    for obj in bundle["objects"].get("objects", []):
        if obj.get("object_id") == object_id:
            return obj
    return None


def rubric_for_step(bundle: Dict[str, Any], step_id: str) -> List[Dict[str, Any]]:
    rubrics = []
    for rubric in bundle["rubric"].get("rubrics", []):
        if rubric.get("step_id") == step_id:
            rubrics.extend(rubric.get("criteria", []))
    return rubrics


def tool_list_labs(args: Dict[str, Any]) -> Dict[str, Any]:
    labs = []
    for lab_id in LAB_IDS:
        spec = load_contract(lab_id, "lab_spec")
        labs.append(
            {
                "lab_id": lab_id,
                "title": spec.get("title"),
                "domain": spec.get("domain"),
                "unity_scene": spec.get("unity_scene"),
                "entry_step_id": spec.get("entry_step_id"),
            }
        )
    return text_result({"labs": labs})


def tool_get_unity_scene_map(args: Dict[str, Any]) -> Dict[str, Any]:
    return text_result(
        {
            "shared_scenes": [
                "Assets/Scenes/start.unity",
                "Assets/Scenes/startVR.unity",
            ],
            "lab_scenes": [
                {
                    "lab_id": lab_id,
                    "unity_scene": load_contract(lab_id, "lab_spec").get("unity_scene"),
                }
                for lab_id in LAB_IDS
            ],
            "entry_scripts": {
                "lab1_electrostatics": "Assets/Scene1.cs",
                "lab2_static_magnetic_field": "Assets/Scene2.cs",
                "lab3_wave_polarization": "Assets/Scene3.cs",
            },
        }
    )


def tool_get_lab_spec(args: Dict[str, Any]) -> Dict[str, Any]:
    return text_result(load_contract(args["lab_id"], "lab_spec"))


def tool_get_lab_steps(args: Dict[str, Any]) -> Dict[str, Any]:
    return text_result(load_contract(args["lab_id"], "steps"))


def tool_get_lab_objects(args: Dict[str, Any]) -> Dict[str, Any]:
    return text_result(load_contract(args["lab_id"], "objects"))


def tool_get_lab_rubric(args: Dict[str, Any]) -> Dict[str, Any]:
    return text_result(load_contract(args["lab_id"], "rubric"))


def tool_get_lab_telemetry_map(args: Dict[str, Any]) -> Dict[str, Any]:
    return text_result(load_contract(args["lab_id"], "telemetry_map"))


def tool_get_lab_contract_bundle(args: Dict[str, Any]) -> Dict[str, Any]:
    return text_result(load_bundle(args["lab_id"]))


def tool_get_step_context(args: Dict[str, Any]) -> Dict[str, Any]:
    lab_id = args["lab_id"]
    step_id = args["step_id"]
    bundle = load_bundle(lab_id)
    step = find_step(bundle, step_id)
    if step is None:
        raise McpError(-32602, f"Unknown step_id for {lab_id}: {step_id}")

    expected_objects = []
    for object_id in step.get("expected_object_ids", []):
        obj = find_object(bundle, object_id)
        expected_objects.append(obj or {"object_id": object_id, "missing": True})

    return text_result(
        {
            "lab_id": lab_id,
            "step": step,
            "expected_objects": expected_objects,
            "rubric_criteria": rubric_for_step(bundle, step_id),
            "assist_contract": bundle["lab_spec"].get("assist_contract", {}),
            "telemetry_profile": bundle["lab_spec"].get("telemetry_profile"),
        }
    )


def tool_get_object_context(args: Dict[str, Any]) -> Dict[str, Any]:
    lab_id = args["lab_id"]
    object_id = args["object_id"]
    bundle = load_bundle(lab_id)
    obj = find_object(bundle, object_id)
    if obj is None:
        raise McpError(-32602, f"Unknown object_id for {lab_id}: {object_id}")

    used_by_steps = []
    for step in bundle["steps"].get("steps", []):
        if object_id in step.get("expected_object_ids", []):
            used_by_steps.append(
                {
                    "step_id": step.get("step_id"),
                    "title": step.get("title"),
                    "expected_action": step.get("expected_action"),
                }
            )

    return text_result(
        {
            "lab_id": lab_id,
            "object": obj,
            "used_by_steps": used_by_steps,
        }
    )


def tool_build_assist_grounding_template(args: Dict[str, Any]) -> Dict[str, Any]:
    lab_id = args["lab_id"]
    step_id = args.get("step_id")
    selected_object_id = args.get("selected_object_id")
    bundle = load_bundle(lab_id)
    spec = bundle["lab_spec"]

    if step_id is None:
        step_id = spec.get("entry_step_id")
    step = find_step(bundle, step_id)
    if step is None:
        raise McpError(-32602, f"Unknown step_id for {lab_id}: {step_id}")

    selected_object = None
    if selected_object_id:
        selected_object = find_object(bundle, selected_object_id)
        if selected_object is None:
            raise McpError(
                -32602,
                f"Unknown selected_object_id for {lab_id}: {selected_object_id}",
            )

    objects = []
    for object_id in step.get("expected_object_ids", []):
        obj = find_object(bundle, object_id)
        if obj is not None:
            objects.append(obj)

    template = {
        "session_id": "<session_id>",
        "user_id": "<user_id>",
        "lab_id": lab_id,
        "step_id": step_id,
        "mode": "<reactive|proactive|debug>",
        "student_input_text": "<student question or empty string>",
        "selected_object_json": json.dumps(selected_object or {}, ensure_ascii=False),
        "objects_json": json.dumps(objects, ensure_ascii=False),
        "recent_actions_json": "[]",
        "diagnostic_hints_json": json.dumps(
            {
                "assist_triggers": step.get("assist_triggers", []),
                "rubric_criteria": rubric_for_step(bundle, step_id),
            },
            ensure_ascii=False,
        ),
        "trigger_reason_json": json.dumps(
            {
                "source": "mcp_template",
                "expected_action": step.get("expected_action"),
            },
            ensure_ascii=False,
        ),
        "step_rubric_json": json.dumps(rubric_for_step(bundle, step_id), ensure_ascii=False),
        "session_memory_json": "{}",
    }

    return text_result(
        {
            "gateway_route": spec.get("assist_contract", {}).get("gateway_route"),
            "response_root": spec.get("assist_contract", {}).get("response_root"),
            "required_grounding_fields": spec.get("assist_contract", {}).get(
                "required_grounding_fields", []
            ),
            "payload_template": template,
        }
    )


def validate_bundle(bundle: Dict[str, Any]) -> List[Dict[str, Any]]:
    issues = []
    lab_id = bundle["lab_id"]

    for kind in ["lab_spec", "objects", "steps", "rubric", "telemetry_map"]:
        contract_lab_id = bundle[kind].get("lab_id")
        if contract_lab_id != lab_id:
            issues.append(
                {
                    "severity": "error",
                    "kind": kind,
                    "message": f"lab_id mismatch: expected {lab_id}, got {contract_lab_id}",
                }
            )

    steps = bundle["steps"].get("steps", [])
    objects = bundle["objects"].get("objects", [])
    rubrics = bundle["rubric"].get("rubrics", [])
    object_ids = {obj.get("object_id") for obj in objects}
    step_ids = {step.get("step_id") for step in steps}
    rubric_step_ids = {rubric.get("step_id") for rubric in rubrics}
    entry_step_id = bundle["lab_spec"].get("entry_step_id")

    if entry_step_id not in step_ids:
        issues.append(
            {
                "severity": "error",
                "kind": "lab_spec",
                "message": f"entry_step_id does not exist in steps: {entry_step_id}",
            }
        )

    for step in steps:
        step_id = step.get("step_id")
        for object_id in step.get("expected_object_ids", []):
            if object_id not in object_ids:
                issues.append(
                    {
                        "severity": "error",
                        "kind": "steps",
                        "step_id": step_id,
                        "message": f"expected_object_id does not exist: {object_id}",
                    }
                )
        if step_id not in rubric_step_ids:
            issues.append(
                {
                    "severity": "warning",
                    "kind": "rubric",
                    "step_id": step_id,
                    "message": "step has no rubric entry",
                }
            )

    for rubric in rubrics:
        step_id = rubric.get("step_id")
        if step_id not in step_ids:
            issues.append(
                {
                    "severity": "error",
                    "kind": "rubric",
                    "step_id": step_id,
                    "message": "rubric references missing step",
                }
            )

    telemetry_events = bundle["telemetry_map"].get("event_types", [])
    if not any(event.get("event_type") == "assist_requested" for event in telemetry_events):
        issues.append(
            {
                "severity": "warning",
                "kind": "telemetry_map",
                "message": "telemetry map does not define assist_requested",
            }
        )

    return issues


def tool_validate_lab_contracts(args: Dict[str, Any]) -> Dict[str, Any]:
    lab_id = args.get("lab_id")
    lab_ids = [lab_id] if lab_id else LAB_IDS
    for candidate in lab_ids:
        require_lab_id(candidate)

    results = []
    for candidate in lab_ids:
        bundle = load_bundle(candidate)
        issues = validate_bundle(bundle)
        results.append(
            {
                "lab_id": candidate,
                "ok": len([issue for issue in issues if issue["severity"] == "error"]) == 0,
                "issues": issues,
            }
        )

    return text_result({"results": results})


def input_schema(required: List[str], properties: Dict[str, Any]) -> Dict[str, Any]:
    return {
        "type": "object",
        "required": required,
        "properties": properties,
        "additionalProperties": False,
    }


LAB_ID_PROPERTY = {
    "type": "string",
    "enum": LAB_IDS,
    "description": "Summer26 lab identifier.",
}


TOOLS: Dict[str, Tuple[str, Dict[str, Any], Callable[[Dict[str, Any]], Dict[str, Any]]]] = {
    "list_labs": (
        "List the Summer26 labs available through this MCP server.",
        input_schema([], {}),
        tool_list_labs,
    ),
    "get_unity_scene_map": (
        "Return shared scenes, lab scenes, and entry scripts.",
        input_schema([], {}),
        tool_get_unity_scene_map,
    ),
    "get_lab_spec": (
        "Return the lab spec contract for one lab.",
        input_schema(["lab_id"], {"lab_id": LAB_ID_PROPERTY}),
        tool_get_lab_spec,
    ),
    "get_lab_steps": (
        "Return the step contract for one lab.",
        input_schema(["lab_id"], {"lab_id": LAB_ID_PROPERTY}),
        tool_get_lab_steps,
    ),
    "get_lab_objects": (
        "Return the object contract for one lab.",
        input_schema(["lab_id"], {"lab_id": LAB_ID_PROPERTY}),
        tool_get_lab_objects,
    ),
    "get_lab_rubric": (
        "Return the rubric contract for one lab.",
        input_schema(["lab_id"], {"lab_id": LAB_ID_PROPERTY}),
        tool_get_lab_rubric,
    ),
    "get_lab_telemetry_map": (
        "Return the telemetry map contract for one lab.",
        input_schema(["lab_id"], {"lab_id": LAB_ID_PROPERTY}),
        tool_get_lab_telemetry_map,
    ),
    "get_lab_contract_bundle": (
        "Return spec, objects, steps, rubric, and telemetry map for one lab.",
        input_schema(["lab_id"], {"lab_id": LAB_ID_PROPERTY}),
        tool_get_lab_contract_bundle,
    ),
    "get_step_context": (
        "Return a lab step with its expected objects, rubric criteria, assist contract, and telemetry profile.",
        input_schema(
            ["lab_id", "step_id"],
            {
                "lab_id": LAB_ID_PROPERTY,
                "step_id": {
                    "type": "string",
                    "description": "Step identifier such as L1S1, L2S2, or L3S3.",
                },
            },
        ),
        tool_get_step_context,
    ),
    "get_object_context": (
        "Return an object contract and the steps that reference it.",
        input_schema(
            ["lab_id", "object_id"],
            {
                "lab_id": LAB_ID_PROPERTY,
                "object_id": {
                    "type": "string",
                    "description": "Object identifier from the lab objects contract.",
                },
            },
        ),
        tool_get_object_context,
    ),
    "build_assist_grounding_template": (
        "Build a grounded AI-assist payload template for a lab step.",
        input_schema(
            ["lab_id"],
            {
                "lab_id": LAB_ID_PROPERTY,
                "step_id": {
                    "type": "string",
                    "description": "Optional step identifier. Defaults to the lab entry step.",
                },
                "selected_object_id": {
                    "type": "string",
                    "description": "Optional selected object identifier to embed in selected_object_json.",
                },
            },
        ),
        tool_build_assist_grounding_template,
    ),
    "validate_lab_contracts": (
        "Validate one lab or all labs for cross-file contract consistency.",
        input_schema(
            [],
            {
                "lab_id": {
                    "type": "string",
                    "enum": LAB_IDS,
                    "description": "Optional lab identifier. If omitted, all labs are validated.",
                }
            },
        ),
        tool_validate_lab_contracts,
    ),
}


def list_tools() -> Dict[str, Any]:
    return {
        "tools": [
            {
                "name": name,
                "description": description,
                "inputSchema": schema,
            }
            for name, (description, schema, _handler) in TOOLS.items()
        ]
    }


def handle_request(request: Dict[str, Any]) -> Optional[Dict[str, Any]]:
    method = request.get("method")
    request_id = request.get("id")
    params = request.get("params") or {}

    try:
        if method == "initialize":
            result = {
                "protocolVersion": params.get("protocolVersion", "2024-11-05"),
                "capabilities": {"tools": {}},
                "serverInfo": {"name": SERVER_NAME, "version": SERVER_VERSION},
            }
            return {"jsonrpc": "2.0", "id": request_id, "result": result}

        if method == "notifications/initialized":
            return None

        if method == "tools/list":
            return {"jsonrpc": "2.0", "id": request_id, "result": list_tools()}

        if method == "tools/call":
            name = params.get("name")
            args = params.get("arguments") or {}
            if name not in TOOLS:
                raise McpError(-32601, f"Unknown tool: {name}")
            _description, _schema, handler = TOOLS[name]
            result = handler(args)
            return {"jsonrpc": "2.0", "id": request_id, "result": result}

        raise McpError(-32601, f"Unknown method: {method}")

    except McpError as error:
        payload = {
            "jsonrpc": "2.0",
            "id": request_id,
            "error": {"code": error.code, "message": error.message},
        }
        if error.data is not None:
            payload["error"]["data"] = error.data
        return payload
    except Exception as error:
        return {
            "jsonrpc": "2.0",
            "id": request_id,
            "error": {
                "code": -32603,
                "message": str(error),
                "data": traceback.format_exc(),
            },
        }


def read_content_length(header_line: str) -> Optional[int]:
    if not header_line.lower().startswith("content-length:"):
        return None
    return int(header_line.split(":", 1)[1].strip())


def read_message() -> Optional[Dict[str, Any]]:
    content_length = None

    while True:
        line = sys.stdin.buffer.readline()
        if line == b"":
            return None

        decoded = line.decode("utf-8").strip()
        if decoded == "":
            break

        value = read_content_length(decoded)
        if value is not None:
            content_length = value

    if content_length is None:
        return None

    body = sys.stdin.buffer.read(content_length)
    if not body:
        return None
    return json.loads(body.decode("utf-8"))


def write_message(message: Dict[str, Any]) -> None:
    body = json.dumps(message, ensure_ascii=False, separators=(",", ":")).encode("utf-8")
    sys.stdout.buffer.write(f"Content-Length: {len(body)}\r\n\r\n".encode("ascii"))
    sys.stdout.buffer.write(body)
    sys.stdout.buffer.flush()


def main() -> None:
    while True:
        request = read_message()
        if request is None:
            break

        response = handle_request(request)
        if response is not None and "id" in request:
            write_message(response)


if __name__ == "__main__":
    main()
