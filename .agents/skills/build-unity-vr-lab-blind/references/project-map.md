# Project map

- `AGENTS.md`: repository boundary and Unity safety rules.
- `Policies/reuse-policy.md`: authoritative three-category reuse policy.
- `Policies/reuse-allowlist.json`: machine-readable allowlist.
- `Policies/gold-exclusions.json`: forbidden target artifacts and uses.
- `UnityProject/`: clean Unity project and generated Lab code/scenes.
- `UnityProject/LocalPackages/com.emvr.lab-common/`: canonical validated shared
  package used by the Unity manifest.
- `LocalPackages/com.emvr.lab-common/`: legacy integrity mirror; read-only and
  required to hash-match the canonical package while it remains present.
- `OptionalIntegrations/UnityMCP/`: disabled MCP materials outside Unity import
  scope; never use during a rebuild.
- `OptionalIntegrations/LabMCP/`: disabled legacy Lab MCP/tooling material
  outside active Unity and Skill scope; never use during a rebuild.
- `ApprovedAssets/`: approved models, XR/XRI resources, materials, and audio.
- `ReusableFoundation/Gate5MinimalSlice/`: protected cross-Lab Gate 5 wiring,
  fail-closed builder helpers, and generic regression tests. Copy only through
  `scripts/materialize_gate5_foundation.py`; do not edit source or copy.
- `LabSpecs/<lab_id>/`: Brief, design, state, and implementation handoff.
- `LabInterfaceContracts/`: exactly five canonical JSON contracts for the
  single current run; legacy examples are restricted elsewhere.
- `KnowledgeBase/ReusableCodeArchive/`: Category B staging area. It is
  currently empty except for policy documentation; new candidates require
  separate user curation and validation before a rebuild may use them.
- `KnowledgeBase/rebuild-lessons/build-experience/`: normalized, cross-Lab
  construction experience from pre-workflow rebuilds; recommended and required
  Gate 0 reading, never a source of target requirements.
- `KnowledgeBase/rebuild-lessons/<lab_id>.md`: verified Gate 10 lessons; read
  every prior file at Gate 0. Gate 10 may create only the current root-level
  `<lab_id>.md`.
- `KnowledgeBase/legacy-lab-specific-summaries/`: restricted old summaries that
  may expose implementation, coordinates, class names, wiring, and answers;
  never read during blind rebuilds or treat as Gate 0 context.
- `Tools/labflow/labflow.py`: gate state and clean-scene scaffold tool.
- `references/unity-beginner-handoff.md`: mandatory click-by-click standard for
  every user-owned Unity action, including the exact Test Runner location.
- `scripts/import_approved_asset_closure.py`: copies an approved GUID dependency
  closure into current-Lab generated output and records a manifest. A real copy
  is allowed only at Gate 5; `.unity` scene sources are rejected.
- `scripts/materialize_gate5_foundation.py`: verifies the protected Gate 5
  manifest, creates a byte-identical read-only current-Lab copy at
  `UnityProject/Assets/Generated/<lab_id>/Gate5Foundation/`, and records
  `BuildEvidence/<lab_id>/gate5-foundation-import.json`.
- `BuildEvidence/<lab_id>/`: current-run provenance and validation evidence.
- `BuildEvidence/EvalTemplates/`: read-only evaluator templates.
- Current-conversation document attachments: authorized requirement inputs at
  their exact resolved paths; their surrounding directories remain forbidden.

Workflow-maintenance documents are intentionally absent from this map and from
clean run Packs. Do not read a sibling `EMVR_Workflow_Maintenance/` directory or
any `WORKFLOW_MAINTAINER_GUIDE.md`; those materials are for a separate maintainer
identity and are not Builder inputs.

Never use a path outside this map to locate an implementation answer.

During a rebuild, every mapped path is read-only except the current
`LabSpecs/<lab_id>/`, its five non-example contracts, Lab-specific Unity output
paths declared in its implementation handoff, generated approved dependencies
under `UnityProject/Assets/Generated/<lab_id>/`, and
`BuildEvidence/<lab_id>/`. Report
problems in all other paths to the user without changing them or supplying a
replacement patch. The sole exception is the controlled Gate 10 creation of
`KnowledgeBase/rebuild-lessons/<current_lab_id>.md`; previous lessons remain
read-only.

The Gate 5 generated foundation is an additional controlled exception to
ordinary output ownership: the materializer may create it, but the Builder
Agent may not edit it. Lab-specific adapters, domain code, and scene wiring
must remain in the declared editable Lab paths beside that copy.
