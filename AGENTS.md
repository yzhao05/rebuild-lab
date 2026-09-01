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
Assume the user has never used Unity. Every handoff must give a numbered click
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

During a Lab rebuild, treat the workflow foundation as user-owned and read-only.
This includes `.gitattributes`, `.gitignore`, `AGENTS.md`, `.agents/`, `Policies/`, `Tools/`, `LocalPackages/`,
`UnityProject/LocalPackages/`, `OptionalIntegrations/`,
`ApprovedAssets/`, `ReusableFoundation/`, `KnowledgeBase/`, `LabSpecs/templates/`, example contracts,
`UnityProject/Packages/`, `UnityProject/ProjectSettings/`, and pre-existing
shared content under `UnityProject/Assets/` such as `ApprovedReuse/` and
`Plugins/`.

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
