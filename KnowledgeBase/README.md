# Knowledge base usage

- `docs/`: architecture and workflow documentation. Knowledge-only.
- `ReusableCodeArchive/`: Category B staging area. It currently contains no
  approved implementation candidate; new entries require independent minimal
  validation before use.
- `rebuild-lessons/build-experience/`: normalized construction experience from
  earlier rebuilds. It excludes old answers/wiring and is recommended mandatory
  reading at Gate 0.
- `rebuild-lessons/<lab_id>.md`: verified Gate 10 error/prevention lessons. Read
  every prior lesson at Gate 0; only Gate 10 may create the current Lab's file.
- `legacy-lab-specific-summaries/`: restricted archive containing old Lab
  implementation details, coordinates, class names, scene wiring, answers,
  old Agent guidance, refactor maps, and legacy contract/config examples.
  Do not read it during a blind rebuild.
- `legacy-lab-specific-summaries/legacy-agent-bundle/`: restricted old Agent
  guidance and issue catalogs; never read during blind rebuilds.

Use `UnityProject/LocalPackages/com.emvr.lab-common` as the canonical
implementation whenever an equivalent capability exists there. MCP-related
agent tools are disabled under `OptionalIntegrations/`.
