# Builder Agent boundary

Work only inside this Builder Pack plus exact requirements/reference documents
the user explicitly uploads or attaches in the current conversation. An
attachment may resolve outside the pack; read that exact file without searching
its parent or sibling directories. Never search for an old Lab implementation.

Before changing code:

1. Use `build-unity-vr-lab-blind`.
2. Run its boundary verifier.
3. Read `Policies/reuse-policy.md` and `Policies/reuse-allowlist.json`.
4. Record every reused component or asset in
   `BuildEvidence/<lab_id>/reuse-report.md`.
5. Recursively read every prior Markdown lesson under
   `KnowledgeBase/rebuild-lessons/` except files named `README.md`, especially
   the curated `build-experience/` safeguards, and complete the current Lab's
   `prior-lessons-review.md`.

Do not read or search `KnowledgeBase/legacy-lab-specific-summaries/` during a
blind rebuild. That restricted archive contains old Lab implementation,
coordinates, class names, scene wiring, and answers. Only an explicit user
authorization naming an exact file may permit it for a non-blind comparison;
it never becomes mandatory Gate 0 context or an approved implementation source.

Workflow development and maintenance material is also outside the Builder
boundary. In particular, never read or search the sibling
`../EMVR_Workflow_Maintenance/` directory or any file named
`WORKFLOW_MAINTAINER_GUIDE.md`. It is maintainer-only context, is not a user
requirement or Gate 0 lesson, and must never be copied into a run Pack.

Execute exactly one workflow gate per substantive response. Complete and verify
that gate, request user approval, and make no next-gate changes until approval
is received and recorded. Treat an unambiguous progression message such as
`继续`, `可以`, `下一步`, `往下做`, `通过`, `同意`, `OK`, or `go ahead` as
approval of the currently completed gate; a fixed `批准 Gate N` phrase is not
required. Consolidate blockers instead of pausing multiple times. Do not infer
approval from silence, a question, a negated instruction, or a request for
gate-local changes, and never approve for the user.

Run the Skill's code inventory and read its mandatory reuse matrix before the
Brief/design review. Every accepted Lab must wire the universal Common baseline;
every capability present must use its designated Common implementation. Every
core desktop interaction must receive an equivalent XR path by default through
the Common/XRI input adapters: same target, state transition, visible feedback,
and success result. Gate 1 must not ask the User to design ordinary mouse-to-XR
bindings. Ask only when the supplied requirements demand a semantic exception
or the Common mapping cannot express the action, and record that exception.
Every
new Lab scene must instantiate the approved EMVR
`ApprovedAssets/EMVRRoom/Prefabs/Room_Big_Part_01.prefab`; the XRI
`Teleportation Environment` is only a sample ground and is not a room.
Copying a donor scene or substituting generated Cube walls is forbidden.
Use the complete EMVRLABS room in frozen `Policies/emvr-room-assembly.json`:
two Part 01, two Part 02, one Part 03, with its module placements and room-only
floor/light instance overrides. Two Part 01 copies alone form a smaller room
and do not reproduce the approved EMVRLABS room. Import all three Prefabs by
GUID closure. Preserve source/imported bytes, links and positive unit scale;
apply only recipe-listed instance overrides. Validate the 15 x 18 m extent,
actual floor support, joins and both sides of the room before saving.
All room Parts, meshes, materials, textures, .meta files and their generated
imports are read-only to the workflow. The full assembly recipe is read-only
as well. Never regenerate, unpack, simplify, replace, delete or edit a Part or
its dependencies; never edit source/import hashes to accept drift. Reuse all
five recipe instances every time: Part 01 x2, Part 02 x2, Part 03 x1. Apply only
the frozen recipe's existing instance settings and whole-room placement.
Importing all source files is not proof of complete scene reuse: Gate 5 must
also check the saved scene's exact linked-instance counts and reject removed,
added, disabled or rescaled module content. Do not accept a partial room.
All room Parts, meshes, materials, textures, .meta files and their generated
imports are read-only to the workflow. The full assembly recipe is read-only
as well. Never regenerate, unpack, simplify, replace, delete or edit a Part or
its dependencies; never edit source/import hashes to accept drift. Reuse all
five recipe instances every time: Part 01 x2, Part 02 x2, Part 03 x1. Apply only
the frozen recipe's existing instance settings and whole-room placement.
Importing all source files is not proof of complete scene reuse: Gate 5 must
also check the saved scene's exact linked-instance counts and reject removed,
added, disabled or rescaled module content. Do not accept a partial room.
XR is a required default, not optional sample
code: obey `Policies/reuse-policy.md` "Gate 5 XR defaults and ownership".
Reuse complete Setup/Simulator Prefabs and Common components; never fork or
reimplement available simulator, controller tracking, ray or input code.
Only an explicit user request may initiate a documented change; routine Lab
approval is not permission. Preserve source/import bytes and ray/model poses.
Gate 5 defaults to four original Part 03 point lights at intensity 5.0 using
existing frozen overrides, Flat ambient intensity 1.0 and the exact environment
settings in `Policies/reuse-policy.md`; add no Directional Light. Apply scene
settings before visual validation and keep room asset/recipe bytes unchanged.
Use read-only `Policies/emvr-room-lighting.json` and import the complete original
`ApprovedAssets/EMVRRoom/Lighting/RoomLighting.lighting`, including its GI values.
Follow reuse-policy's current-scene lighting compatibility adapter: preserve the
imported source, derive native RoomLighting_Execution for the current Lab, enforce
OnDemand there, and validate source provenance plus all original light parameters.
The native execution asset replaces the direct imported-settings scene reference;
this exception does not authorize editing source/import assets or room Parts.
Historical LightingData is sealed reference-only; regenerate current-scene GI
data in Unity after rebuilding, preserving all source/setting/Part bytes.
Gate 5 requires current lighting data and observed visual validation, not only
matching direct-light intensity. Follow reuse-policy's validated execution adapter,
source-cache retirement and exact-path save guard as mandatory defaults; do not
rewrite them without an explicit user change request. Build assigns settings and
saves the scene, then hands off manual lighting generation. Keep Auto Generate off;
never generate on import/startup/Play. A null saved LightingData reference blocks
acceptance; an older file timestamp alone does not prove stale GI. Only an explicit
user request initiates changes to these defaults.
The user-approved brighter default (2026-09-12) uses original Flat ambient RGB
x1.5: (0.13116321, 0.15650724, 0.212264115). Use the frozen lighting profile's
render_settings for scenes; original_render_settings is provenance only. Original
LightingSettings, all room Parts/recipe and four point lights at 5 remain unchanged.
The published RoomLighting text uses canonical LF (maintainer normalization,
2026-09-12); original values/GUID and metadata are retained. Source and imported
copies must still match byte-for-byte. Builder must never normalize a copy,
accept an EOL-only mismatch, or refresh import/release hashes on its own.
Require the scene builder to reject a room instance without floor, wall, and
roof geometry, and require the actual runtime camera to view the readable front
of every world-space Canvas/TextMesh (`-Transform.forward` in Unity). Require initial and post-first-action visible
states, a fail-closed scene builder, and reviewed Game View layout before
approval.

Use one isolated Builder Pack per Lab run. `labflow new` must refuse when any
earlier active/run-specific specification, contract, Unity output, or evidence
is visible. Approved assets remain read-only sources; import their GUID closure
only during Gate 5 into
`UnityProject/Assets/Generated/<lab_id>/ApprovedDependencies/` using the Skill's
importer. Gate 1/3 may plan the path but must not perform the copy.

For a blind rebuild/recreation/comparison task, create the workspace only at
`EMVR_Blind_BuilderPack/RebuildWorkspaces/EMVR_Blind_Rebuild_<lab_id>`,
using `Tools/create_blind_run_pack.ps1 -LabId <lab_id>`. The rebuilding Agent
must execute that command from the opened master Pack; it must never delegate
the PowerShell command or manual copying to the User. After PASS, the User only
opens the exact generated child path supplied by the Agent. Never create it in
the Pack's parent or choose another name. This location constraint does not
apply when the Pack is intentionally embedded in a host project to develop a
genuinely new Lab; that case uses `labflow new --mode integrated-development`.
If the current root already has a valid `.emvr-workspace.json` matching the
requested Lab ID and fixed child path, do not run the copy tool again; initialize
or resume the single run in that child.
Workspace creation/switching is pre-Gate setup, not a blocked Gate 0. Do not
write Gate 0 artifacts in the master Pack; Gate 0 starts only after `labflow new`
has initialized the marked child.

Release frozen hashes are maintainer-owned and must not be edited by a rebuild
Agent. In Gate 0, after recording an observed Builder Boundary PASS, run
`python Tools/labflow/labflow.py capture-gate0-baseline --lab-id <lab_id>` once.
Never overwrite or recapture it. Gates 1–10 must stop if `labflow` reports any
protected file added, removed, or changed relative to that snapshot. The
current Lab's Gate 10 lesson is the only planned exclusion.

For a maintainer-repaired pre-scaffold Gate 4 run, follow the compatibility
receipt rules in `Policies/reuse-policy.md`. Preserve the original baseline,
`BuildEvidence/<lab_id>/protected-maintenance/*.json` and the state
`protected_maintenance` registrations. The Builder cannot issue, edit, remove,
rehash or register receipts. Rerun Boundary, source-references and status;
resume the same Gate 4 only when integrity passes. Prior approvals remain
unchanged, and scaffold generation/Unity compile evidence are still required.
For a maintainer-reviewed pending Gate 5 room-assembly correction, the separate
`gate5_room_assembly_guidance_v1` receipt profile permits only its five named
tooling/instruction files. The same immutable-baseline and maintainer-only
issuance rules apply. It does not change approved assets, reusable code,
source-reference bindings, prior approvals, or grant any Gate 5 evidence.
For a maintainer-reviewed pending Gate 5 room-assembly correction, the separate
`gate5_room_assembly_guidance_v1` receipt profile permits only its five named
tooling/instruction files. The same immutable-baseline and maintainer-only
issuance rules apply. It does not change approved assets, reusable code,
source-reference bindings, prior approvals, or grant any Gate 5 evidence.

Gate 5 also has a protected reusable source at
`ReusableFoundation/Gate5MinimalSlice/`. Copy it only with the Skill's
`materialize_gate5_foundation.py` into
`UnityProject/Assets/Generated/<lab_id>/Gate5Foundation/`. The source and the
generated copy are both immutable: do not hand-copy, patch, rename, fork, or
rewrite them. Current-Lab domain code must call or compose around them. A hash,
API, or behavior defect is a protected-foundation blocker owned by the user.

Unity MCP is disabled for this workflow. Do not call Unity MCP tools or its
generated Skills, add its package dependency, restore its active NuGet DLLs, or
set `UNITY_MCP_READY`. Hand Unity-owned actions and observations to the user.

Every Gate 5A user-facing reply must include this ordered lighting procedure:

1. Exit Play Mode.
2. Run the current Lab's Build menu once. For `ece329_electric_field_0`:
   `Tools > EMVR > Labs > Build Ece329 Electric Field 0`.
3. `Window > Rendering > Lighting > Generate Lighting, wait, then save.`

Use the actual generated Build menu name for another Lab. This supersedes the
older lighting-only one-line handoff and the instruction to omit Build when a
settings reference exists. Keep the normal reply concise; keep asset/reference
checks internal and add diagnostics only for an actual failure or user request.
Generate after that Build and save after completion. Do not ask for another Build
after generation unless content changes; a later Build requires regeneration.

For other Unity actions,
assume the user has never used Unity. Every handoff must give a numbered click
path with the exact Unity top-menu location, tab/window position, target name,
button, success result, failure screenshot/text, and actions to avoid. Read the
Skill's `references/unity-beginner-handoff.md`; never say only `run Test Runner`,
`Clear Console`, `open Game View`, `run the Builder`, or `enter Play Mode`.
For EditMode tests, explicitly direct the user to
`Window > General > Test Runner`, select **EditMode**, name the exact assembly
and expected tests/count, and click **Run Selected**.

Before writing Lab C#, require the boundary verifier to prove that every local
UPM `file:` dependency resolves, `manifest.json` agrees with
`packages-lock.json`, and the Common assembly foundation is available. Treat
namespace/type floods as cascades until the owning package is verified.

Before calling any Common, Unity, or XRI API from Lab-specific code, read its
real declaration and record its source/package version, assembly, exact member
signature, and planned call site in the design API audit. Never guess an API
from its type or member name. Read
`KnowledgeBase/docs/unity-compile-error-playbook.md` before writing or debugging
Lab C#.

Gate 4 is a scaffold-only Unity compile checkpoint: no domain implementation,
zero compiler errors after Clear/recompile, and recorded `scaffold_compile`
evidence. Gate 5 first materializes and validates the immutable reusable
foundation, including its exact three EditMode tests and a neutral
button/spawn/visibility check. Only after that passes may Gate 5 add the
smallest end-to-end domain slice. `gate5_foundation`, `domain_slice`, and
`domain_compile` must all pass before approval. Do not add more code while the
current gate has compiler errors. After a Lab C# edit, rerun that gate's clean
compile check. After adding the domain slice, rerun and re-record both the
domain checks and the three foundation regressions against unchanged current
content; stale content-digest evidence cannot approve Gate 5.

On failure, diagnose the earliest error before cascades. Fix errors owned by
the current Lab output directly. If the earliest root cause belongs to a
protected package, Common source, shared asset, Skill/tool, dependency, or
project setting, stop and report the foundation defect instead.

After the Gate 8 evidence package, use Gate 9 for explicit user acceptance.
When submitting Gate 9, remind the user that the required Gate 10 retrospective
will follow acceptance. Gate 10 must write the run record to
`BuildEvidence/retrospectives/<lab_id>.md` and the normalized future-facing
lesson to `KnowledgeBase/rebuild-lessons/<lab_id>.md`; only Gate 10 may create
that exact KnowledgeBase file, and previous lessons remain read-only.

## Protected workflow foundation

Git distribution must preserve the reviewed bytes of every file. Keep the root
`.gitattributes` rule `* -text` and its binary declarations; do not add newline,
encoding, content-filter or archive-substitution overrides. Do not mass-convert
files to LF or CRLF, modify source hashes, or recapture a run baseline after
cloning. The published LightingSettings remains LF. Verify Boundary after clone
or ZIP extraction; preserve the master folder name `EMVR_Blind_BuilderPack`.
Publishing guidance is in README_USER.md. The maintainer-only
`gate5_git_byte_preservation_v1` receipt profile permits its eight named
tooling/instruction files at pending Gate 5; it never permits asset changes or
baseline replacement and grants no gate evidence.

During a Lab rebuild, treat the workflow foundation as user-owned and read-only.
This includes `.gitattributes`, `.gitignore`, `AGENTS.md`, `.agents/`, `Policies/`, `Tools/`, `LocalPackages/`,
`UnityProject/LocalPackages/`, `OptionalIntegrations/`,
`ApprovedAssets/`, `ReusableFoundation/`, `KnowledgeBase/`, `LabSpecs/templates/`, example contracts,
`UnityProject/Packages/`, `UnityProject/ProjectSettings/`, and pre-existing
shared content under `UnityProject/Assets/` such as `ApprovedReuse/` and
`Plugins/`.

Before reading or transferring reusable implementation, validate
`Policies/reuse-source-references.json` and the current
`LabSpecs/<lab_id>/source-references.json` with `labflow source-references`.
The catalog, per-Lab manifest, and every referenced source are read-only. Use
only the declared transfer mode. Persist repository-relative POSIX paths only;
the resolver's absolute output is temporary and must not enter generated files.

The Builder Agent may write only the current Lab's specification, five
non-example contracts, declared Lab-specific Unity output files, and
`BuildEvidence/<lab_id>/`. In Gate 10 only, after `labflow retrospective`
generates them, it may also create and complete
`BuildEvidence/retrospectives/<lab_id>.md` and
`KnowledgeBase/rebuild-lessons/<lab_id>.md`. It must not overwrite an existing
retrospective/lesson, edit a previous lesson, reusable source, or back-port a
fix into Common while rebuilding a Lab.

If inspection reveals a defect, missing dependency, stale rule, or required
change in the protected foundation, stop the current gate and report the
affected path, evidence, impact, and required user action. Do not modify the
protected file, weaken a verifier, copy a patched substitute into the Lab, or
provide a ready-to-paste patch, replacement code, or repair command. The user
owns that repair. A request to continue or fix the Lab does not authorize a
foundation change.

If the specification is incomplete, ask for the missing behavior, formula,
unit, parameter range, interaction, or acceptance result. Do not resolve a
missing requirement by locating a Gold scene or source file.

Never hand-edit a `.unity` scene YAML file. Generate scenes through an Editor
builder and ask the user to perform Unity-owned Play Mode checks.

The maintainer-only `gate5_emvrlabs_room_sources_v1` profile registers the
explicit room-source expansion during pending Gate 5. It permits only its
named tooling/inventory files and seven named additions. Original snapshots,
earlier receipts, source-reference bindings and approvals remain unchanged.
Builder cannot issue or modify maintenance receipts.
