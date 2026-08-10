# AI Agent Context Bundle

This folder contains the files to show a future AI agent before building a new Unity/VR teaching lab.

## Included

- `Agents.md`: repository-level Unity project rules.
- `WHAT_TO_SHOW_AI_FOR_AGENT_LAB.md`: practical guide for what context to provide.
- `.agents/`: Unity VR Lab agent instructions, knowledge base, playbooks, templates, and checklists.
- `summary/`: Lab 1, Lab 2, Lab 3, and new-lab scaffold summaries and issue categories.

## Summary Index

- `summary/lab1/`: Lab 1 electrostatics build and issue summaries, including `rebuildlab1-2/README.md` for the second new-lab/rebuild experience.
- `summary/lab2/`: Lab 2 magnetostatics build and issue summaries, including `rebuildlab2-2/README.md` for the second Lab 2 new-lab prototype/debugging experience.
- `summary/lab3/`: Lab 3 wave-polarization build and issue summaries, including `rebuildlab3-2/README.md` for the second new-lab prototype/debugging experience.

## How To Use

When starting a new AI conversation or a new custom agent, show this folder first. At minimum, ask the AI to read:

```text
Agents.md
WHAT_TO_SHOW_AI_FOR_AGENT_LAB.md
.agents/unity-vr-lab-agent.md
.agents/knowledge/common_lessons.md
.agents/knowledge/lab_issue_catalog.md
.agents/playbooks/build_new_lab.md
.agents/evals/unity_vr_lab_checklist.md
.agents/evals/issue_regression_checklist.md
summary/lab1/rebuildlab1-2/README.md
summary/lab2/rebuildlab2-2/README.md
summary/lab3/rebuildlab3-2/README.md
```

Then provide the new lab handbook or requirements.

## Important Rule

For this Unity project, the agent should not proactively run Unity batchmode unless explicitly requested. It should prefer Unity Editor menu scene generation and user-provided Game/Console screenshots.
