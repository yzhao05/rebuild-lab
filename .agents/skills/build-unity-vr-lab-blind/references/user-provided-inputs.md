# User-provided conversation inputs

## Allowed

Treat an exact file explicitly uploaded/attached by the user in the current
conversation and requested as a requirements/reference source as authorized,
even when the app resolves it to a temporary path outside the Builder Pack.

Examples:

- Lab handbooks and experiment guides in PDF or Word format;
- screenshots, diagrams, scanned pages, and reference images;
- tables or spreadsheets containing parameters and expected results;
- plain-text requirements and user-authored notes.

Read text, tables, formulas, captions, and diagrams needed for the current Lab.
Do not require the user to copy the attachment into the Builder Pack first.

## Scope limits

- Access only the exact attached file, not its directory or sibling files.
- Do not search from the attachment path toward old projects.
- Do not follow local links or references into Gold implementation files.
- Do not treat an uploaded repository, source archive, Unity project, `.unity`
  scene, prefab, or implementation script as reusable unless the user
  separately and explicitly approves that implementation source.
- Continue to exclude the Gold Reference even if a document mentions its class,
  scene, or path names.

## Extraction protocol

1. Record the display name and attachment reference under `source_material` in
   the Brief.
2. Extract supported requirements with page, section, table, or figure
   citations when available.
3. Mark explicit document facts `confirmed-from-document`.
4. Mark interpretations `inferred-needs-confirmation`.
5. Mark absent or ambiguous requirements `unresolved` and ask the user.
6. Do not invent formulas, parameters, expected results, interactions, or
   acceptance criteria.
