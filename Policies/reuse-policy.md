# Reuse policy

## Workspace modes

- A blind rebuild, recreation, or comparison must use the fixed workspace
  `EMVR_Blind_BuilderPack/RebuildWorkspaces/EMVR_Blind_Rebuild_<lab_id>` created by
  `Tools/create_blind_run_pack.ps1 -LabId <lab_id>`. The rebuilding Agent must
  execute this command and report the exact generated path; the User only
  switches workspace and must never be asked to run the command or copy the
  Pack. It must not be created in the Builder Pack's parent or under a different
  name.
- A valid fixed child whose `.emvr-workspace.json` matches the requested Lab ID
  is resumed in place. The Agent must not invoke the master copy tool again from
  that child.
- When this Builder Pack is deliberately embedded in a host project to develop
  a genuinely new Lab, the internal-location rule does not apply. Use
  `labflow new --mode integrated-development`; reuse and Gate rules remain in
  force.

## Category A: directly reusable

These resources were validated as cross-Lab infrastructure and may be imported
or called directly. They are user-owned, read-only inputs during a rebuild:

- `UnityProject/LocalPackages/com.emvr.lab-common/` (canonical Common package)
- `ApprovedAssets/`
- `ReusableFoundation/Gate5MinimalSlice/` through its controlled materializer
- Unity/XR package dependencies declared by the clean project

This category includes pointer/drag/grab behavior, XR rig and controller-ray
support, object identity/state, Capture/Restore/Reset, lifecycle, telemetry,
assist grounding, status UI, validation builders, controller models, materials,
audio, and general visual assets.

The Gate 5 reusable foundation has stricter copy semantics than ordinary
Category A assets. Run
`.agents/skills/build-unity-vr-lab-blind/scripts/materialize_gate5_foundation.py`
to verify its manifest and copy it byte-for-byte into
`UnityProject/Assets/Generated/<lab_id>/Gate5Foundation/`. Both the protected
source and the generated copy are immutable to the Builder Agent. The current
Lab must call or compose around the copied assemblies and Common APIs; it may
not fork, patch, rename, or hand-copy these helpers. Only domain code beside
this layer is editable.

Mandatory reuse is defined by the active Skill reference
`references/mandatory-reuse-matrix.md`. Universal Common responsibilities must
appear in every accepted Lab; capability-triggered Common implementations must
be used whenever that capability appears. Category A permission alone is not a
reason to omit the mandatory baseline.

The room/environment shell is mandatory direct reuse. Every Lab must instantiate
`ApprovedAssets/XRInteractionToolkitSamples/Starter Assets/DemoSceneAssets/Prefabs/Teleportation Environment.prefab`
inside its newly created scene, preserving its `.meta` and dependency closure.
Record this decision in the Brief/design, but execute the approved-asset copy
only during Gate 5 after Gate 4 approval. Gate 5 readiness verifies the import
manifest and every source/copy hash.
`new_scene` forbids donor Lab scene copying, including `DemoScene.unity`; it does
not permit primitive-generated walls as a substitute. A missing or broken room
Prefab is a foundation blocker.

The verified XR baseline is also mandatory direct reuse. Import and instantiate
`Starter Assets/Prefabs/XR Interaction Setup.prefab` as the top-level XR setup
and `XR Device Simulator/XR Device Simulator.prefab` as its desktop simulator.
Do not instantiate the bare `XR Origin (XR Rig).prefab` as a substitute for
the complete setup and do not reconstruct the controller rig in Lab-specific
code. The approved simulator controls must retain the validated Up/Down-arrow
`Keyboard Y Translate` bindings. Configure rays through Common
`EmVrXrRayRuntimeTuner`/`EmVrXrRayUtility`; never fork the old Lab-specific
ray implementation.

## Disabled optional integration: Unity MCP

Unity MCP is not part of the active rebuild workflow. Its package, OpenUPM
registry, `UNITY_MCP_READY` define, MCP tool calls, and NuGet DLLs under
`UnityProject/Assets` must remain disabled. Preserved MCP 0.79.1 dependency
materials live outside the Unity project under `OptionalIntegrations/UnityMCP/`
for a future, separate user-authorized maintenance decision. Its generated
tool Skills are also archived there instead of remaining under active
`.agents/skills` discovery.

Legacy Lab MCP/tooling material is likewise disabled under
`OptionalIntegrations/LabMCP/`. Neither optional integration may be consulted,
copied into the Unity project, or used to bypass a gate during a blind rebuild.

## Category B: conditionally reusable

Resources under `KnowledgeBase/ReusableCodeArchive/` may be inspected and
adapted only after checking that the selected part is independent of the target
Lab's physics, steps, answers, object IDs, scene wiring, and class names.
The active archive currently contains no approved code candidates; its README
is a staging policy, not reusable implementation. A future item becomes
eligible only after user-led curation and validation outside a Lab run.

Before using a Category B item:

1. Identify the smallest reusable responsibility.
2. Rename and decouple Lab-specific symbols.
3. Put the adaptation only in the current Lab's declared output. If it should
   become shared behavior, report that recommendation to the user; do not write
   it into Common during the rebuild.
4. Add tests or a minimal validation scene.
5. Record source, changes, and justification in
   `BuildEvidence/<lab_id>/reuse-report.md`.

Do not modify the archived Category B source.

## Category C: knowledge-only

Issue catalogs, summaries, playbooks, documentation, configuration examples,
and contract examples may guide architecture and debugging. Do not copy code or
scene wiring from prose descriptions. Examples are not confirmed requirements;
the current Brief and contracts are authoritative.

Normalized pre-workflow construction lessons live under
`KnowledgeBase/rebuild-lessons/build-experience/pre-workflow/`; verified Gate 10 lessons live at
`KnowledgeBase/rebuild-lessons/<lab_id>.md`. At Gate 0, the Builder Agent must
read every prior lesson file and record applicability in the current Lab's
`prior-lessons-review.md`. Lessons provide safeguards and debugging cues, not
target requirements or answers. They may preserve verified constraints such as
required initial values, API signatures, Unity lifecycle placement, or build
ordering only when those constraints are cross-Lab and stripped of old target
answers and wiring.

Raw old summaries are physically separated under
`KnowledgeBase/legacy-lab-specific-summaries/`. They may contain old Lab
implementation, exact coordinates, class names, object IDs, scene wiring, and
answers. The archived legacy Agent bundle and legacy example contracts live in
the same restricted tree, so the Builder Agent must not read or search them during a blind
rebuild. An explicit user authorization must name an exact file and a non-blind
comparison purpose; even then, it is not reusable code and must not replace the
current Brief or normalized build experience.

## Gold Reference: forbidden to the Builder Agent

The original Lab 1/2/3 scenes, implementation scripts, rebuild overlays, scene
GUID wiring, and complete source copies remain outside this pack. Do not search
for them. If a required fact is absent, stop and request it from the user.

Models and generic assets are approved even when their names mention a domain
apparatus, such as a solenoid or polarizer. Approval of a model does not approve
the old behavior code that once controlled it.

## Workflow-maintenance material: forbidden to the Builder Agent

Workflow development, dependency maps, internal change-impact notes, and
maintainer instructions are not rebuild inputs. They live outside the Builder
Pack, currently under the sibling `EMVR_Workflow_Maintenance/` directory. A
Builder Agent must not read, search, cite, or copy that directory or any file
named `WORKFLOW_MAINTAINER_GUIDE.md`. This restriction prevents maintenance
context from broadening the Builder's permissions or leaking old-run knowledge.

Maintenance must occur in a separate Agent context. A clean run Pack must not
contain maintenance material; if such a file appears inside the Pack, Gate 0 is
blocked until the user removes it from the run boundary.

## Beginner-safe Unity handoffs

Treat every user as a first-time Unity user for Unity-owned validation. A valid
handoff names the starting Edit/Play state, exact top-menu path, expected
window/tab location, exact target and button, observable success condition,
failure screenshot/text, and actions to avoid. Do not require the user to ask
where Test Runner, Console, Game view, a Builder menu, or Play Mode is located.

For EditMode testing, always identify `Window > General > Test Runner`, the
**EditMode** tab, the current Lab's exact test assembly and test names/count,
**Run Selected**, and the expected all-green/zero-failure result. Read those
identifiers from the current `.asmdef` and test source rather than guessing.

## Authorized conversation-document exception

A requirements/reference document explicitly uploaded or attached by the user
in the current conversation is an allowed input at its exact resolved path,
including an app temporary path outside the Builder Pack. This includes PDFs,
images, Word documents, spreadsheets, text requirements, and similar non-code
materials.

This exception does not authorize directory enumeration or sibling files. A
source repository, code archive, Unity project, scene, prefab, or implementation
script requires separate explicit approval and remains subject to the Gold
boundary.

## Foundation defect ownership

The Builder Agent owns only the current Lab's generated specifications,
non-example contracts, declared Lab-specific Unity output, and evidence. The
workflow infrastructure, repository `.gitattributes`/`.gitignore`, Skills, policies, tools, Common package, reusable
code, models/assets, knowledge base, dependencies, project/package settings,
templates, and pre-existing shared Unity files are protected foundation owned
by the user.

`ReusableFoundation/` is protected foundation. The controlled Gate 5 copy is
generated output only in location/provenance terms; it remains immutable. A
hash mismatch in either source or copy is a foundation blocker, not permission
to regenerate, patch, or replace it manually.

The sole controlled exception is Gate 10 after Gate 9 user acceptance. The
Builder Agent may create and complete exactly
`BuildEvidence/retrospectives/<current_lab_id>.md` and
`KnowledgeBase/rebuild-lessons/<current_lab_id>.md` using the workflow's
retrospective generator. It may not overwrite or revise an earlier
retrospective or lesson.

When a protected item is incorrect or incomplete, the Agent must stop the
current gate and report its path, evidence, impact, and the user action needed.
It must not edit the item, weaken validation, introduce a local workaround,
copy in a patched replacement, or provide ready-to-paste code, patches, or
repair commands. Work resumes only after the user performs the repair and asks
the Agent to verify it.

## Per-run isolation and approved asset import

Use exactly one active Lab run per Builder Pack. A new run must start from a
pristine foundation copy with no previous LabSpec, canonical contract,
Lab-specific Unity output, or run evidence. Resume an existing run in place;
never let a new target read an earlier run's output.

`ApprovedAssets/` is outside `UnityProject/Assets` and cannot be instantiated by
Unity directly. Copy only the selected asset plus its approved GUID dependency
closure with the Skill importer into
`UnityProject/Assets/Generated/<lab_id>/ApprovedDependencies/`. Preserve the
import manifest under `BuildEvidence/<lab_id>/approved-asset-imports/`; do not
copy into shared `UnityProject/Assets/ApprovedReuse/` during a run.
