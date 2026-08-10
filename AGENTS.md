# Summer26 Unity VR Lab Workflow

## Project boundary

- Treat this directory as the canonical Unity project for rebuild and new-lab work.
- Read `README_FOR_AGENT.md` before planning or implementation.
- Read the historical issue catalog and acceptance checklists referenced there.
- Use `com.emvr.lab-common`; do not copy its runtime scripts into `Assets`.
- Keep domain formulas, presets, and experiment rules under `Assets/Scripts/EMVR/Labs/<LabName>/`.

## Safety rules

- Never edit `Library`, `Temp`, `Logs`, `obj`, `Build`, or `UserSettings`.
- Do not hand-edit `.unity` scene YAML.
- Do not proactively run Unity batchmode.
- Create scene builders under `Assets/Editor/<LabName>/` and scenes under `Assets/Scenes/`.
- Preserve the donor scene's room, lighting, XR rig, cameras, and navigation unless the brief explicitly replaces them.
- Keep exactly one active `AudioListener` and at most one active `EventSystem`.

## Required workflow

1. Invoke `$build-unity-vr-lab` for rebuild or new-lab implementation.
2. Run `status` and begin each substantive user-facing response with the current stage, stage purpose, completed work, blockers, and the exact input or Unity action needed from the user.
3. At `brief_ready`, interview the user in batches of at most three questions. Do not invent missing learning goals, formulas, presets, expected results, interactions, or acceptance criteria. The agent writes `brief.yaml`; the user supplies and confirms the content.
4. Ask the user to confirm the summarized Brief before producing final contracts, then record it with `python tools/labflow/labflow.py confirm-brief --lab-id <lab_id> --note "<user confirmation>"`.
5. Create or review `LabSpecs/<lab_id>/brief.yaml`.
6. Generate and validate all five JSON contracts before writing lab C#.
7. Present a short scene and interaction design to the user.
8. Record design approval with `python tools/labflow/labflow.py approve --lab-id <lab_id>`.
9. Run scaffold only after contract validation and design approval.
10. Implement the smallest runnable domain layer while reusing `com.emvr.lab-common`.
11. Ask the user to compile, run the Editor menu builder, enter Play Mode, and provide Console/Game/Scene evidence.
12. Record results and update the eval case; do not call the lab complete without evidence.

After completing a gate, report the new stage and wait for the user to say to continue before changing files for the next stage. Do not combine two stages into one silent implementation pass.

## Validation commands

```powershell
python tools/labflow/labflow.py validate --lab-id <lab_id>
python tools/labflow/labflow.py status --lab-id <lab_id>
```

In Unity, also run:

```text
Tools > EMVR > Common > Validate Active Scene
```
