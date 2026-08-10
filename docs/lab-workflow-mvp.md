# Unity VR Lab Workflow MVP

## State sequence

```text
brief_ready
  -> contracts_ready
  -> design_approved
  -> scaffold_ready
  -> unity_compiled
  -> scene_built
  -> playtest_passed
  -> accepted
```

## Commands

Create a run:

```powershell
python tools/labflow/labflow.py new --lab-id rebuild_lab1_v3 --title "Lab 1 Rebuild V3" --domain electrostatics --scene Assets/Scenes/Rebuild_Lab1_V3.unity --donor-scene Assets/Scenes/Static_Electric_Field.unity
```

After editing the brief and contracts:

```powershell
python tools/labflow/labflow.py confirm-brief --lab-id rebuild_lab1_v3 --note "User confirmed the complete Brief summary."
python tools/labflow/labflow.py validate --lab-id rebuild_lab1_v3
```

Write `LabSpecs/rebuild_lab1_v3/design.md`, review it with the user, then record approval:

```powershell
python tools/labflow/labflow.py approve --lab-id rebuild_lab1_v3 --note "User approved scene and interaction design."
```

Create compile-safe runtime and Editor builder skeletons:

```powershell
python tools/labflow/labflow.py scaffold --lab-id rebuild_lab1_v3
```

Inspect progress:

```powershell
python tools/labflow/labflow.py status --lab-id rebuild_lab1_v3
```

Record Unity evidence after the user tests in the Editor:

```powershell
python tools/labflow/labflow.py record --lab-id rebuild_lab1_v3 --compile passed --scene-built passed --playtest pending --note "Initial Game View captured."
```

## Gates

- Contract gate: five JSON files parse, share one `lab_id`, and cross-references resolve.
- Brief confirmation gate: placeholders are removed and explicit user confirmation is recorded before contracts can advance the workflow.
- Design gate: `design.md` exists and approval is recorded.
- Unity gate: user confirms compilation and runs the generated Editor menu.
- Acceptance gate: checklist plus Game/Console evidence confirms the core experiment flow.

The CLI never edits `.unity` YAML and never starts Unity batchmode.
