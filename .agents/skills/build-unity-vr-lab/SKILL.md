---
name: build-unity-vr-lab
description: Build or rebuild a Summer26 EMVR Unity lab through the repository's contract-first, stage-by-stage user workflow. Use when the user asks to rebuild an existing lab, create a new Unity/VR lab, generate a donor-scene-based lab scaffold, define lab contracts, or continue a lab whose workflow state is in LabSpecs. Reports the current stage, interviews the user for missing Brief requirements, advances one approved gate at a time, and enforces contract validation, package reuse, Unity handoff, and evidence recording.
---

# Build Unity VR Lab

Use the repository workflow as the source of truth. Do not skip a gate because
the requested lab resembles an older one.

## Follow the mandatory interaction protocol

At the start of every substantive user-facing response, show this compact block:

```text
Current stage: <workflow stage>
Stage purpose: <one sentence>
Verified: <verified artifacts or evidence>
Remaining/blockers: <missing items or none>
Need from user: <the exact information, approval, or Unity action needed next>
```

Localize these labels to the user's current language.

Run `status` before reporting a stage when a workflow state exists. Never claim
a state transition until the relevant command or user-owned evidence confirms
it. Work only inside the reported stage. After completing a gate, report the
new state and wait for the user to explicitly say to continue before changing
files for the next stage.

Do not hide questions inside a long progress message. Ask no more than three
short, related questions at a time.

## Start from project context

1. Read `AGENTS.md` and `README_FOR_AGENT.md`.
2. Read `LabSpecs/<lab_id>/workflow_state.json` when continuing a run.
3. Read [references/project-map.md](references/project-map.md).
4. Read [references/workflow-gates.md](references/workflow-gates.md).
5. Read [references/brief-interview.md](references/brief-interview.md) at
   `brief_ready` or whenever Brief requirements change.
6. Inspect relevant historical issues and donor-lab code without copying stale
   scene wiring or duplicated common-package scripts.

## Create or resume a run

For a new run, choose a stable lowercase `lab_id` and execute:

```powershell
python tools/labflow/labflow.py new --lab-id <lab_id> --title "<title>" --domain <domain> --scene Assets/Scenes/<Output>.unity --donor-scene Assets/Scenes/<Donor>.unity
```

For an existing run, execute:

```powershell
python tools/labflow/labflow.py status --lab-id <lab_id>
```

Then perform only the reported next stage.

## Interview the user at brief_ready

Treat the user as the requirements authority and the agent as the file author.
Do not tell the user to edit YAML unless they explicitly prefer to. Extract
facts from supplied handbooks, screenshots, previous labs, and messages first;
do not ask for information already present.

Track each Brief item as `confirmed`, `inferred-needs-confirmation`, or
`unresolved`. Never silently convert an inference into a confirmed experiment
requirement. Ask the next one to three highest-impact unresolved questions,
update `brief.yaml` after each answer, and report what changed.

Before creating final contracts, present a concise Brief summary covering the
checklist in `references/brief-interview.md` and ask the user to explicitly
confirm or correct it. Remain at `brief_ready` until that confirmation.

After explicit confirmation, run:

```powershell
python tools/labflow/labflow.py confirm-brief --lab-id <lab_id> --note "<what the user confirmed>"
```

Report that Brief confirmation is recorded. Only then finalize the five
contracts. Do not treat agreement with an individual interview answer as
confirmation of the complete Brief.

## Complete the contract gate

Edit `LabSpecs/<lab_id>/brief.yaml` and all five canonical JSON contracts in
`LabInterfaceContracts`. Replace every placeholder with requirements grounded
in the user's handbook, learning goals, formulas, presets, expected results,
objects, desktop/XR interactions, and evidence needs.

Run:

```powershell
python tools/labflow/labflow.py validate --lab-id <lab_id>
```

Resolve every error before writing lab-specific C#. Treat warnings explicitly.
After validation passes, report `contracts_ready` and pause for the user to
explicitly say to continue before drafting the design.

## Complete the design gate

Write `LabSpecs/<lab_id>/design.md` with the shortest learning flow, donor-scene
preservation plan, generated objects, interactions, feedback, formulas,
presets, and acceptance evidence. Present the design to the user. Do not record
approval until the user explicitly approves it.

After approval, change `Approval status: NOT APPROVED` to `Approval status:
APPROVED`, then run:

```powershell
python tools/labflow/labflow.py approve --lab-id <lab_id> --note "<what the user approved>"
```

Report `design_approved` and pause. Do not scaffold in the same interaction.

## Scaffold, then implement

Run:

```powershell
python tools/labflow/labflow.py scaffold --lab-id <lab_id>
```

Use the generated controller and Editor builder as boundaries. Extend them with
the smallest runnable domain slice. Reuse `com.emvr.lab-common` for shared
status, telemetry, interaction, lifecycle, and validation behavior. Preserve
the donor scene's room, lighting, camera, XR rig, and navigation unless the
approved design says otherwise.

Never hand-edit `.unity` YAML or proactively run Unity batchmode.

## Hand off Unity-owned checks

Ask the user to:

1. Wait for compilation and report Console errors after Clear.
2. Run the generated `Tools > EMVR > Labs > Build ...` menu.
3. Run `Tools > EMVR > Common > Validate Active Scene`.
4. Test the desktop core flow, then XR simulation or hardware.
5. Provide initial/completed Game View and Console evidence.

Record only evidence the user actually observed:

```powershell
python tools/labflow/labflow.py record --lab-id <lab_id> --compile <pending|passed|failed> --scene-built <pending|passed|failed> --playtest <pending|passed|failed> --note "<evidence>"
```

Do not call the lab accepted merely because scaffolding or compilation passed.
