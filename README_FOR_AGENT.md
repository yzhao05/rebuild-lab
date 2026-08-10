# README For Connected Agents

This file is the first document a connected AI agent should read before building, rebuilding, or extending a Summer26 Unity VR lab.

## 0. Project Rule

Do not start implementation from scratch. Start from the knowledge base, then use the lab contracts, MCP tools, shared Unity code, and shared Summer26 scene environment.

The common VR room/background is also reusable project infrastructure. A rebuild must not create a blank unrelated room unless the user explicitly asks for a new environment. Prefer reusing or cloning the existing Summer26 lab environment, including shared room/background objects, lighting style, XR setup, cameras, doors, canvases, and scene-entry conventions, then replace or adapt only the lab-specific experiment objects.

The user must complete Unity compilation and Play Mode testing in the Unity Editor. The agent should not proactively run Unity batchmode or silently wait on long Unity work.

## 1. Read These First

Before answering a build request, read the knowledge base in this order:

```text
AI_AGENT_CONTEXT_BUNDLE/Agents.md
AI_AGENT_CONTEXT_BUNDLE/WHAT_TO_SHOW_AI_FOR_AGENT_LAB.md
AI_AGENT_CONTEXT_BUNDLE/.agents/unity-vr-lab-agent.md
AI_AGENT_CONTEXT_BUNDLE/.agents/knowledge/common_lessons.md
AI_AGENT_CONTEXT_BUNDLE/.agents/knowledge/lab_issue_catalog.md
AI_AGENT_CONTEXT_BUNDLE/.agents/playbooks/build_new_lab.md
AI_AGENT_CONTEXT_BUNDLE/.agents/evals/unity_vr_lab_checklist.md
AI_AGENT_CONTEXT_BUNDLE/.agents/evals/issue_regression_checklist.md
```

Then inspect these project summaries:

```text
common/README.md
common/common_code_inventory.json
LabInterfaceContracts/README.md
LabMCP/README.md
```

For a lab-specific task, also read the prior summaries:

```text
AI_AGENT_CONTEXT_BUNDLE/summary/lab1/
AI_AGENT_CONTEXT_BUNDLE/summary/lab2/
AI_AGENT_CONTEXT_BUNDLE/summary/lab3/
```

## 2. MCP And Port Information

The local lab-specific MCP server is:

```text
LabMCP/lab_mcp_server.py
```

It uses MCP over stdio. It does not use a TCP port.

Example client config:

```text
LabMCP/mcp_config.example.json
```

Available MCP tools:

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

If the task mentions Unity MCP, Dify, an assist gateway, localhost, or a port, ask the user for the current host and port. Do not invent one. The lab contract route is `/v1/assist`, but no server host or port is fixed in this repository.

## 3. Ask Before Building

After the user gives a build request, ask targeted questions based on known risks in the knowledge base. Do this before implementing if the answer affects scene design or runtime behavior.

Ask for:

- Lab target: new lab, lab1, lab2, lab3, or modification of an existing scene.
- Unity environment: Unity version, Editor path, whether the Editor is already open, and whether it is compiling.
- Build route: whether the user wants only code/scaffold, an Editor menu scene builder, or a full runnable lab scene.
- Scene name and target path under `Assets/Scenes/`.
- Background/environment route: whether to reuse the existing Summer26 room/background from the current lab scene, a shared start/VR scene, or another named scene.
- Required physics objects, formulas, presets, report questions, and student tasks.
- Object roles: draggable, clickable, rotatable, display-only, state-changing, observation zone, result feedback.
- Interaction mode: VR only, desktop mouse testing, XR Device Simulator, or all of them.
- Feedback artifacts the user can provide after each major change: Game view screenshot, Scene view screenshot, Console screenshot, and Play Mode state.
- Any external MCP/assist gateway host and port, if external services are involved.
- Maximum acceptable generation time before the agent should stop and re-plan.

Ask especially when these known risks apply:

- Camera or text visibility matters.
- Thin or transparent objects are interactive.
- Dragging in a 3D scene is judged visually like a 2D plane.
- Scene generation may be long or invisible.
- The lab uses optical, magnetic, or field-line visualizations where correct math may still be visually unclear.

## 4. Reuse Contracts First

The canonical lab interfaces are in:

```text
LabInterfaceContracts/
```

Each lab has five JSON contracts:

- `*_lab_spec.example.json`
- `*_objects.example.json`
- `*_steps.example.json`
- `*_rubric.example.json`
- `*_telemetry_map.example.json`

Before building or modifying a lab:

1. Use `validate_lab_contracts` or parse the JSON files directly.
2. Use `get_lab_contract_bundle` for the target lab.
3. Use `get_step_context` to map each step to expected objects, rubric criteria, and assist triggers.
4. Use `get_object_context` to avoid inventing duplicate object IDs or incompatible object types.
5. Use `build_assist_grounding_template` when implementing or testing AI-assist payloads.

For a new lab, create the same five-contract set first, then build scripts/scenes against that contract.

## 5. Reuse Shared Unity Code And Scene Environment

Use the common code map in:

```text
common/README.md
common/common_code_inventory.json
```

Existing lab scenes:

| Lab | Scene | Main scripts |
|---|---|---|
| lab1 | `Assets/Scenes/Static_Electric_Field.unity` | `ElectricField.cs`, `Sphere.cs`, `Wire.cs`, `Plane.cs`, `ChargingBox.cs` |
| lab2 | `Assets/Scenes/Static_Magnetic_Field.unity` | `MagneticField.cs`, `Wire.cs`, `Solenoid.cs` |
| lab3 | `Assets/Scenes/Wave_Polarization.unity` | `WaveField.cs`, `Polarizer.cs`, `LinearPolarizer.cs`, `QuarterPlate.cs` |

Shared scene/environment elements to reuse:

- Common VR room/background assets and objects, for example scene objects named like `Room_Big_Part_01`.
- Doors, floor/walls, basic room geometry, and environmental props that make the three labs feel like one Summer26 lab sequence.
- Lighting and render settings from the existing lab scenes, unless the task is specifically to redesign lighting.
- XR interaction setup / rig objects and simulator-compatible camera setup already present in the project.
- Shared UI/canvas layout conventions for start, instruction, and lab navigation.
- Existing scene-entry conventions in `Assets/Scenes/start.unity` and `Assets/Scenes/startVR.unity`.

Shared systems to reuse:

- XR movement and hand support: `Continuous_Movement.cs`, `HandPresence.cs`, `GrabDetect*.cs`
- Scene loading: `Scene1.cs`, `Scene2.cs`, `Scene3.cs`, `LoadScene.cs`
- Step UI: `Step.cs`, `Start_Game.cs`
- AI assist framework: `Assets/Scripts/EMVR/Assist`
- Field visualization patterns: `ElectricField.cs`, `MagneticField.cs`, `WaveField.cs`

For rebuilding an existing lab, prefer one of these approaches:

1. Clone the existing lab scene as the base, then clean up or replace only the lab-specific experiment objects.
2. If creating via an Editor builder, instantiate or preserve the common room/background, lighting, XR setup, camera conventions, and shared UI from an existing Summer26 scene before adding lab-specific objects.
3. If a full clone is too risky, document exactly which shared environment objects are intentionally not reused and why.

Do not move Unity assets or scripts casually. Preserve `.meta` files if a move is truly required. Prefer adding adapters or new scripts over breaking existing scene references.

## 6. Unity Compilation Is User-Owned

The agent may create or edit scripts, JSON contracts, documentation, and Editor menu builders.

The user must:

- Open the project in Unity.
- Let Unity compile.
- Run any Editor menu scene-generation command.
- Enter Play Mode.
- Provide Console/Game/Scene screenshots or error text.

The agent should ask the user to perform these steps and then continue from the reported feedback.

Default rule:

```text
Do not proactively run Unity batchmode.
Do not hand-edit .unity scene YAML unless the user explicitly asks.
Prefer Editor menu scene generation and user-visible feedback.
```

## 7. Build Duration Watchdog

Track elapsed time for every build or generation phase.

Recommended thresholds:

- More than 5 minutes with no visible progress: stop and report status.
- More than 10 minutes for any Unity-related generation or compilation step: stop and ask the user for Unity Console / Editor status.
- Any repeated timeout or invisible progress: read `lab_issue_catalog.md`, especially the batchmode issues from Lab 1 and Lab 2, then split the task into smaller steps.

Known prior issue:

- Silent batchmode generation took too long and gave poor observability.
- Final fix was to avoid long invisible batchmode work, split the task, and use Unity Editor menu generation plus user screenshots.

When a long build happens:

1. Stop the current build/generation attempt.
2. State what phase exceeded the threshold.
3. Re-check `AI_AGENT_CONTEXT_BUNDLE/.agents/knowledge/lab_issue_catalog.md`.
4. Identify whether the situation matches a known issue.
5. Change the plan: reduce scope, generate smaller scripts, add an Editor menu entry, or ask the user to compile in Unity.
6. Continue only after the user confirms the Editor state.

## 8. Known Problems And First Fixes

Use this quick map before implementing:

| Problem | First fix |
|---|---|
| Long invisible Unity generation | Stop batchmode, split work, use Editor menu and user screenshots |
| Compile context unclear | Ask Unity version, Editor path, open/compiling state |
| Text unreadable or mirrored | Use short text, high contrast, runtime camera-facing layout |
| Camera/Game view wrong | Fix Play Mode camera, keep one active `AudioListener` |
| Rebuild has wrong/blank background | Reuse or clone the Summer26 room/background and XR setup before adding lab-specific objects |
| Material changes unstable | Use `sharedMaterial` in edit mode and `MaterialPropertyBlock` for per-renderer state |
| Transparent object blocks dragging | Use `Physics.RaycastAll` or filtering |
| Thin object hard to select | Add larger colliders / hit areas |
| 3D drag depth feels wrong | Snap to visualization plane and use screen-plane checks |
| Correct math not visible | Add presets, status bars, readable variables, and clear cause/effect feedback |
| README no longer matches scene | Update implementation README and experiment guide after interaction changes |

## 9. Expected Agent Flow

1. Read this file.
2. Read the knowledge base and issue catalog.
3. Inspect `LabInterfaceContracts`, `LabMCP`, and `common`.
4. Ask the user targeted questions based on known risk areas.
5. Validate or create JSON contracts.
6. Reuse shared scene environment, room/background, object, step, assist, and visualization code.
7. Implement the smallest runnable version.
8. Ask the user to compile and test in Unity.
9. Use user feedback and screenshots to iterate.
10. Update README / experiment guide when behavior changes.
