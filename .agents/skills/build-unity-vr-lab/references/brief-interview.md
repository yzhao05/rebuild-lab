# Brief interview protocol

Use this checklist only at `brief_ready` or when requirements change. The agent
writes the files; the user supplies and confirms experimental intent.

## Interview rules

1. Read supplied materials and the donor Lab before asking questions.
2. Do not ask for facts already supported by those sources.
3. Label sourced facts as `confirmed`, reasonable guesses as
   `inferred-needs-confirmation`, and absent facts as `unresolved`.
4. Ask at most three related questions per response, starting with items that
   would change the learning flow, physics, or scene architecture.
5. After each user answer, update `brief.yaml`, summarize the changes, and ask
   the next unresolved questions.
6. Do not remove a placeholder by inventing a requirement.
7. Present a final Brief summary and obtain explicit user confirmation before
   finalizing contracts.

## Required Brief coverage

### Identity and sources

- Lab title, stable `lab_id`, domain, rebuild/new-lab mode
- Handbook, screenshots, previous implementation, and donor scene

### Learning and task flow

- Observable learning goals
- Ordered student tasks and success conditions
- Common mistakes or concepts requiring AI assistance

### Domain model

- Formulas, units, sign conventions, assumptions, and expected results
- Valid parameter ranges and invalid-input behavior
- Presets with exact values and expected outcomes

### Objects and feedback

- Object identity, type, initial state, and experimental role
- Selectable, movable, adjustable, observable, and resettable objects
- Visible instruction, status, result, error, and completion feedback

### Interaction modes

- Desktop mouse path for the complete core experiment
- XR Device Simulator path
- Real VR controller path
- Equivalent experimental meaning across input modes

### Scene plan

- Donor scene and output scene
- Environment, lighting, cameras, XR rig, and navigation to preserve
- Generated regions and objects to add or replace

### Acceptance and evidence

- Correct initial, intermediate, completed, reset, and error states
- Initial/completed Game View evidence
- Console-after-Clear evidence
- Desktop and XR test expectations

## Final confirmation format

Present a compact summary under these headings:

```text
Lab identity
Learning goals
Core student flow
Physics and presets
Objects and feedback
Desktop/XR interactions
Scene preservation plan
Acceptance evidence
Unresolved items: none
```

End with one direct request: `请确认这份 Brief，或指出需要修改的项目。`
