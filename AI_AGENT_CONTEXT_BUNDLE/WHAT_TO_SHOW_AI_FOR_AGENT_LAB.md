# What To Show AI When Building Future Labs With The Agent

This file answers one practical question: when using an AI agent to build the next Unity/VR lab, what context should be shown to the AI first?

## Show These Files First

Give the AI these files before asking it to implement a new lab:

```text
Agents.md
.agents/unity-vr-lab-agent.md
.agents/knowledge/common_lessons.md
.agents/knowledge/lab_issue_catalog.md
.agents/playbooks/build_new_lab.md
.agents/evals/unity_vr_lab_checklist.md
.agents/evals/issue_regression_checklist.md
```

These files define the Unity project rules, the lab-building workflow, the repeated problems from Lab 1-3, and the acceptance checklist.

## Show Existing Lab Summaries

For any new lab, also show the AI the prior summaries:

```text
summary/lab1/
summary/lab2/
summary/lab3/
summary/lab1/rebuildlab1-2/README.md
summary/lab2/rebuildlab2-2/README.md
summary/lab3/rebuildlab3-2/README.md
```

The most important file for detailed issue reuse is:

```text
.agents/knowledge/lab_issue_catalog.md
```

It contains the concrete problem and final solution pairs from Lab 1, Lab 2, and Lab 3.

The second Lab 1/new-lab scaffold summary is also important when building anything beyond the original three labs:

```text
summary/lab1/rebuildlab1-2/README.md
```

It explains that future labs should reuse the Summer26 room/background, shared XR setup, assist framework, contracts, and common visualization/domain code, while keeping new experiment content in a separate generated layer.

For Lab 2-style magnetostatics work, also show:

```text
summary/lab2/rebuildlab2-2/README.md
```

It records the second Lab 2 new-lab prototype issues: duplicate XR rigs/EventSystems, main-thread magnetic-field cost, room/background accidentally hidden, UI accidentally hidden, and invisible field lines caused by zeroed current sources or fragile source registration.

For Lab 3-style wave-polarization work, also show:

```text
summary/lab3/rebuildlab3-2/README.md
```

It records the second Lab 3 new-lab prototype issues: donor `WaveField` clones masking the generated prototype, generated content disappearing after donor objects were hidden, Lab 3's experiment-area offset at `x = -28`, and careful reuse of `ChargedObjects.Polarizer` without letting old QWP rotation behavior break the new prototype.

## Show The Current Code Structure

Ask the AI to inspect these folders:

```text
Assets/Scripts/Common/
Assets/Scripts/Lab1_Electrostatics/
Assets/Scripts/Lab2_Magnetostatics/
Assets/Scripts/Lab3_WavePolarization/
Assets/Editor/
Assets/Scenes/
```

This helps it reuse the existing architecture instead of inventing a new one.

## Show The New Lab Requirements

For the new lab, provide:

- The experiment handbook or extracted requirements.
- The lab name and expected scene name.
- The required physics objects and formulas.
- The report questions or student tasks.
- The required presets.
- Which objects should be draggable, clickable, display-only, calculation regions, text, or result feedback.
- Whether real VR, XR Device Simulator, and desktop mouse testing are required.
- How mouse controls should simulate VR controls.

## Show Unity Environment Information

Tell the AI:

- Unity version.
- Unity Editor path.
- Whether Unity Editor is already open.
- Whether the project is currently compiling.
- Whether batchmode is allowed.

For this project, the default rule is:

```text
Do not proactively run Unity batchmode.
Use Unity Editor menu scene generation and user-provided Game/Console feedback.
```

## Show Runtime Feedback After Each Major Change

After the AI changes scripts or builders, run the scene in Unity yourself and show:

- Game view screenshot.
- Scene view screenshot if layout is unclear.
- Console screenshot.
- Whether the Editor is in Play Mode.
- What you clicked, dragged, or pressed.
- What you expected to happen.
- What actually happened.

Screenshots are essential because many previous issues were visual or interaction-related.

## The Most Important Agent Rules

The AI should remember:

- Do not edit `Library`, `Temp`, `Logs`, `Obj`, `Build`, or `UserSettings`.
- Do not hand-edit `.unity` scene YAML unless explicitly requested.
- Put scene builders under `Assets/Editor/<LabName>/`.
- Put generated scenes under `Assets/Scenes/`.
- Preserve `.meta` files when moving Unity assets.
- Prefer Editor menu scene generation over batchmode.
- Always reuse solved issues from `.agents/knowledge/lab_issue_catalog.md`.
- Every visible object must have a clear role.
- Desktop mouse testing should exist unless explicitly out of scope.
- Important interactions need visible status feedback.
- README files must match the actual scene behavior.

## Suggested Prompt For A Future Lab

Use a prompt like this:

```text
We are building a new Unity/VR teaching lab in this project.

First read:
- Agents.md
- .agents/unity-vr-lab-agent.md
- .agents/knowledge/common_lessons.md
- .agents/knowledge/lab_issue_catalog.md
- .agents/playbooks/build_new_lab.md
- .agents/evals/unity_vr_lab_checklist.md
- .agents/evals/issue_regression_checklist.md

Then inspect the existing Assets/Scripts and Assets/Editor lab structure.

Do not run Unity batchmode unless I explicitly ask.
Create or update an Editor menu scene builder under Assets/Editor.
Put generated scenes under Assets/Scenes.
Reuse prior Lab 1-3 solutions from the issue catalog.

Here is the new lab handbook/requirement:
<paste or attach lab requirement here>

Before implementation, give me a short scene and interaction design.
Then implement the minimum runnable version.
```

## Short Version

If you only show one thing besides the new lab requirements, show:

```text
.agents/unity-vr-lab-agent.md
.agents/knowledge/lab_issue_catalog.md
```

Those two files tell the AI how this lab-building agent should behave and what prior mistakes it must not repeat.
