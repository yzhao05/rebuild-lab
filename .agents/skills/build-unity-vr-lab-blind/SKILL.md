---
name: build-unity-vr-lab-blind
description: Build or rebuild an EMVR Unity/VR teaching lab from approved specifications, user-uploaded conversation documents, validated Common components, approved models/assets, and curated knowledge without reading the Gold Reference implementation or using Unity MCP. Use when extracting Lab requirements from attached PDFs/images/documents, creating a new Lab, independently rebuilding Lab 1/2/3, continuing a LabSpecs workflow, generating a clean Editor-built scene, importing approved XR assets, or validating desktop/XR/state/lifecycle/telemetry behavior under the Builder Pack reuse boundary.
---

# Build Unity VR Lab Blind

Use the Builder Pack plus explicitly user-provided conversation documents as the
complete information boundary. Never search its parent or siblings for an older
Lab.

Never read workflow-development or maintenance material outside this Pack,
including `../EMVR_Workflow_Maintenance/` and any file named
`WORKFLOW_MAINTAINER_GUIDE.md`. Maintainer context is deliberately excluded from
clean run Packs and is not an authorized requirement, lesson, or implementation
source.

## Report the current gate

Treat each gate as one atomic conversation work package. In one substantive
response, perform all currently possible reading, writing, and validation for
exactly one gate. Do not split a complete gate into repeated `continue` pauses
or mix work from two gates in one response.

If required user information is missing, ask one consolidated blocker question
set (at most three related questions) and remain in the same gate. A blocked
response is not gate completion and does not request approval.

Start every substantive response with this localized block:

```text
Current gate: <gate>
Purpose: <one sentence>
Verified: <artifacts/evidence>
Remaining/blockers: <items or none>
Need from user: <exact requirement, approval, or Unity action>
```

After completing and verifying the gate, end with:

```text
Gate result: COMPLETE - AWAITING USER APPROVAL
Approval requested: reply with a clear progression intent (for example "继续",
"OK", "go ahead", or "批准 Gate <N>"), or reject/request changes.
Next-gate changes made: none
```

Interpret approval by intent, in the context of the currently completed gate.
Treat an unambiguous affirmative or progression message as approval, including
`继续`, `通过`, `同意`, `OK`, `go ahead`, `proceed`, `continue`, `next step`,
and equivalent wording in the user's language.
The user does not need to name the gate or use a fixed phrase. A message may
both approve the current gate and ask the Agent to perform the next gate; first
record the approval, then perform only that immediately following gate in the
same response.

Do not treat silence, a successful tool log, the Agent's own judgment, a pure
question (for example `Is it okay to proceed?`), a negated instruction (for example
`先别继续`), or a message that only requests corrections as approval. If a
message mixes progression language with a correction or unresolved condition,
remain in the current gate unless it clearly accepts the current gate as-is.
If the user rejects or requests changes, apply only gate-local corrections and
submit the same gate again.

After semantic approval, record it in `workflow_state.json` with `labflow
approve-gate`. The next substantive response may execute only Gate N+1. Never
skip a gate, pre-build the next gate while waiting, or approve a gate on the
user's behalf.

Record approval only after the user's approving/progression message, preserving
that message in the note:

```powershell
python Tools/labflow/labflow.py approve-gate --lab-id <lab_id> --gate <N> --note "<user approval>"
```

The command rejects out-of-order approval and a gate whose required evidence is
not complete. It records the Agent's semantic decision; it does not require a
specific approval phrase.

## Verify the boundary first

1. Confirm the working root contains `Policies/reuse-allowlist.json`.
2. Run `scripts/verify_builder_pack.ps1`.
3. Read `Policies/reuse-policy.md` and
   [references/reuse-categories.md](references/reuse-categories.md).
4. Run `scripts/list_code_inventory.ps1` and read
   [references/mandatory-reuse-matrix.md](references/mandatory-reuse-matrix.md).
5. Enumerate every `*.md` file under `KnowledgeBase/rebuild-lessons/`, excluding
   `README.md`; read each file and complete
   `LabSpecs/<lab_id>/prior-lessons-review.md`. If none exist, record `none
   available`. Carry applicable safeguards into the current design and Gate
   checks without importing another Lab's requirements. Files under
   `build-experience/` are curated construction lessons from work completed
   before this Gate workflow and are the recommended source for API defaults,
   lifecycle constraints, interaction pitfalls, and prevention checks.
6. Do not read, search, summarize, or cite
   `KnowledgeBase/legacy-lab-specific-summaries/` during a blind rebuild. It is
   a restricted archive containing old Lab implementation details, coordinates,
   class names, scene wiring, and answers. It may be opened only when the user
   explicitly authorizes an exact file for a non-blind comparison task; that
   authorization does not make it a Gate 0 lesson or reusable source.
7. Refuse to search outside the Builder Pack, except for the exact files the
   user uploaded/attached in the current conversation and asked the Agent to
   use. Never enumerate an attachment's parent directory.
8. Create/update `BuildEvidence/<lab_id>/reuse-report.md` as soon as anything is
   reused. Never use another run's evidence file.

After the verifier actually passes, record Gate 0 evidence before requesting
approval:

```powershell
python Tools/labflow/labflow.py record --lab-id <lab_id> --boundary-verifier passed --note "<observed PASS output>"
```

Treat Unity MCP as disabled. Do not call Unity MCP tools or MCP-provided Skills,
do not add `com.ivanmurzak.unity.mcp`, and do not set `UNITY_MCP_READY`. Use
filesystem work plus the user-owned Unity validation handoff instead.

Treat a failed boundary check as a blocker. Do not weaken the check to proceed.
Read [references/foundation-preflight.md](references/foundation-preflight.md)
and finish its dependency checks before writing any Lab C#.

## Keep the workflow foundation read-only

During a rebuild, never modify the Builder Pack's workflow foundation:

- `AGENTS.md`, `.agents/`, `Policies/`, and `Tools/`;
- `LocalPackages/`, `ApprovedAssets/`, and `KnowledgeBase/`;
- `ReusableFoundation/`, including every Gate 5 source and manifest;
- `LabSpecs/templates/` and all restricted legacy material;
- `UnityProject/Packages/`, `UnityProject/ProjectSettings/`,
  `UnityProject/Assets/ApprovedReuse/`, `UnityProject/Assets/Plugins/`, and any
  other pre-existing cross-Lab implementation.

Write only the current `LabSpecs/<lab_id>/`, its five non-example contracts,
Lab-specific Unity files declared in its implementation handoff, and
`BuildEvidence/<lab_id>/`. Approved assets may be copied only through
`scripts/import_approved_asset_closure.py` into
`UnityProject/Assets/Generated/<lab_id>/ApprovedDependencies/`; this generated
copy is current-Lab output, while the approved source remains read-only. Read
Category A/B/C resources without changing their source.

The only reusable C# foundation-copy mechanism is the Gate 5 materializer. It verifies
and copies `ReusableFoundation/Gate5MinimalSlice/Source/` byte-for-byte into
`UnityProject/Assets/Generated/<lab_id>/Gate5Foundation/` and records its
manifest. Both the protected source and this generated copy are immutable to
the Builder Agent. Do not hand-copy, patch, rename, fork, or rewrite either.
Lab-specific domain code must call or compose around these assemblies from
separate declared output paths. If they need a change, use the foundation
defect report and wait for the user.
The only KnowledgeBase write exception is Gate 10: after Gate 9 user acceptance,
create and complete exactly
`KnowledgeBase/rebuild-lessons/<lab_id>.md` through `labflow retrospective`.
Never edit or overwrite a previous Lab's lesson file.

When a protected resource is defective or insufficient:

1. stop the current gate;
2. report the affected protected path, direct evidence, rebuild impact, and the
   user action required;
3. wait for the user to repair it and ask for verification.

Do not repair the resource, change the Skill or validator, weaken a policy,
copy a patched substitute into the Lab, hide the failure, or advance the gate.
Do not provide ready-to-paste replacement code, a patch, or a repair command
for the protected resource. Describe the defect and expected behavior in prose
so the user can make the change. A generic instruction such as `continue`,
`fix the Lab`, or `finish the rebuild` is not permission to alter the
foundation.

For namespace/type compile failures, verify the owning package and assembly
first. Do not delete or replace correct `EMVR.Common` usages merely because the
Common assembly failed to resolve. Stop after the first root dependency error;
do not continue generating Lab code through cascaded compiler failures.

Use this blocker report:

```text
Foundation defect: <short description>
Affected protected path: <path>
Evidence: <observed log/check/result>
Impact/current gate: <why work cannot safely continue>
Need from user: <repair or decision the user must perform>
```

## Accept user-provided conversation documents

Treat a PDF, image, Word document, spreadsheet, text file, or similar
requirements/reference document explicitly uploaded or attached by the user in
the current conversation as an authorized input. Its app-resolved or temporary
filesystem path may be outside the Builder Pack. Read that exact attachment
directly; do not ask the user to move it into the Builder Pack solely because of
its path.

Limit this exception to the exact attachment. Do not list or search its parent
directory, follow paths to sibling projects, or use it to locate Gold code. An
uploaded source repository, Unity project, code archive, scene, or prefab does
not become allowed automatically; require the user to explicitly approve it as
a reusable implementation source.

Read [references/user-provided-inputs.md](references/user-provided-inputs.md)
before extracting requirements. Record the attachment name and supporting
page/section in `LabSpecs/<lab_id>/brief.yaml`.

## Load only relevant context

Read:

- [references/project-map.md](references/project-map.md) for paths;
- [references/workflow-gates.md](references/workflow-gates.md) for gate evidence;
- [references/brief-interview.md](references/brief-interview.md) while defining
  or changing requirements;
- [references/user-provided-inputs.md](references/user-provided-inputs.md) when
  the user attaches a requirements/reference document;
- `KnowledgeBase/docs/unity-compile-error-playbook.md` before writing new Lab
  C# or diagnosing any Unity Console compiler error;
- every file under `KnowledgeBase/rebuild-lessons/`, excluding `README.md`, at
  Gate 0 of every new run; record the review in the current LabSpec;
- the current `LabSpecs/<lab_id>/workflow_state.json` when resuming;
- only the Common docs, approved assets, and knowledge files relevant to the
  current gate.

Do not treat example contracts or old issue summaries as target requirements.
Prefer the normalized files under `KnowledgeBase/rebuild-lessons/build-experience/`
over any old narrative summary. Never load the restricted Lab-specific archive
as background context for a blind rebuild.

## Create or resume a run

From the Builder Pack root:

```powershell
python Tools/labflow/labflow.py new --lab-id <lab_id> --title "<title>" --domain <domain> --scene Assets/Scenes/<Output>.unity
python Tools/labflow/labflow.py status --lab-id <lab_id>
```

The workflow creates a new scene; it never copies a donor scene.
`labflow new` requires a pristine Builder Pack with no other active or completed
run output. Resume the single existing run or start from a clean foundation
copy; never put two Lab runs in one Agent-visible Pack.

For a new Lab, create the isolated foundation copy before invoking `labflow
new` (the destination must not already exist):

```powershell
powershell -ExecutionPolicy Bypass -File Tools/create_blind_run_pack.ps1 -Destination "E:\path\to\EMVR_Blind_Run_<lab_id>"
```

Then open that destination as the workspace and run `labflow new` there. Do not
use the master pack or a prior run as the new Lab's workspace.

## Confirm the Brief

First extract every supported fact from user-provided conversation documents.
Then interview the user only for unresolved observable learning goals, ordered
tasks, formulas,
units, parameter ranges, expected results, object behavior, desktop/XR paths,
reset/lifecycle behavior, telemetry, assist context, environment/room strategy,
initial versus first-action visibility, UI/camera layout, and evidence. Ask no more
than three related questions at once.

Always plan to reuse the mandatory room baseline at
`ApprovedAssets/XRInteractionToolkitSamples/Starter Assets/DemoSceneAssets/Prefabs/Teleportation Environment.prefab`.
Record this source and the intended generated path in the Brief/design, but do
not copy it during Gate 1. The actual import belongs to Gate 5 so one response
does not perform work from multiple gates. At Gate 5, instantiate the result under
`Assets/Generated/<lab_id>/ApprovedDependencies/` in a new Lab scene. Preserve
the generated import manifest as provenance. Never copy `DemoScene.unity` and
never replace the room with generated primitive walls/floors. If the mandatory
Prefab or an approved dependency is missing, report a protected-foundation
blocker.

Label information as `confirmed`, `inferred-needs-confirmation`, or
`unresolved`. Do not convert inference into a requirement. Present the complete
Brief summary and obtain confirmation through either a direct approval or an
unambiguous progression message, then record Gate 1 using the standard command:

```powershell
python Tools/labflow/labflow.py approve-gate --lab-id <lab_id> --gate 1 --note "<confirmation>"
```

## Complete contracts and design

Finalize the five canonical JSON contracts in `LabInterfaceContracts` and run:

```powershell
python Tools/labflow/labflow.py validate --lab-id <lab_id>
```

Resolve every error before Lab-specific C#. Then write the shortest design in
`LabSpecs/<lab_id>/design.md`, naming every Common API and approved asset to use.
Include the mandatory Common/room reuse audit, room placement/adaptation plan, authored
initial state and expected post-action visible state, builder failure policy,
and Game View camera/UI layout. Add an API signature audit for every Common,
Unity, and XRI member that Lab-specific code will call: record the exact source
path or package version, assembly, type/member signature, and planned call
site. Read the real declaration; never infer an API from its name or memory. A
reusable component is mandatory when the matrix says so; `new_scene` never
waives reuse.
Obtain design approval through either a direct approval or an unambiguous
progression message and run:

```powershell
python Tools/labflow/labflow.py approve-gate --lab-id <lab_id> --gate 3 --note "<approval>"
```

## Scaffold, compile, then implement

Run:

```powershell
python Tools/labflow/labflow.py scaffold --lab-id <lab_id>
```

Keep Gate 4 limited to the generated runtime/editor skeleton. Before adding
domain logic, note that the runtime skeleton deliberately makes no Common or
domain API calls. Ask the user to open Unity, wait for compilation, Clear the
Console, force one clean recompile, and report the earliest error plus the first
20 relevant messages if it fails. Record the observed result:

```powershell
python Tools/labflow/labflow.py record --lab-id <lab_id> --scaffold-compile <passed|failed> --note "<observed Console evidence>"
```

Do not approve Gate 4 until `scaffold_compile` is `passed` with zero compiler
errors. After Gate 4 approval, Gate 5 must begin with the reusable foundation,
not target-domain code. First import the mandatory approved room and then
materialize the protected C# foundation. Run these exact commands from the
Builder Pack root:

```powershell
python .agents/skills/build-unity-vr-lab-blind/scripts/import_approved_asset_closure.py --lab-id <lab_id> --source "XRInteractionToolkitSamples/Starter Assets/DemoSceneAssets/Prefabs/Teleportation Environment.prefab"
```

```powershell
python .agents/skills/build-unity-vr-lab-blind/scripts/materialize_gate5_foundation.py --lab-id <lab_id>
```

Use the copied helpers plus direct Common calls to assemble the neutral
foundation checkpoint defined in `references/workflow-gates.md`: mandatory
room, camera/XR rig and rays, exactly one XR UI EventSystem, one persistent
interactable button, Common replacement spawner with an active offstage neutral
template, a visible collidable spawned object, desktop drag, state/reset,
status, telemetry, assist, and fail-closed build handling. Do not implement a
parallel Lab-specific pointer, XR, button-wiring, replacement-spawn, state,
status, telemetry, assist, or room system.

Before target-domain code, run the exact `EMVR.ReusableFoundation.Gate5.Tests`
EditMode assembly containing three tests:

- `InactiveReplacementTemplateIsRejected`
- `PersistentButtonSpawnsActiveVisibleCollidableObject`
- `DuplicateActiveEventSystemsAreRejected`

Also observe the neutral button -> visible object, drag, reset, and clean
Console path in the current Lab scene. Record this checkpoint separately:

```powershell
python Tools/labflow/labflow.py record --lab-id <lab_id> --gate5-foundation <passed|failed> --note "<3/3 tests, neutral scene, and clean-Console evidence>"
```

Only after `gate5_foundation` passes, implement one smallest end-to-end domain
slice: one domain object/result, one desktop interaction plus approved XR
adapter, one state/reset path, one telemetry event, and one assist-grounding
result. Compile again from a cleared Console before adding the rest of the Lab.
Record the domain results:

```powershell
python Tools/labflow/labflow.py record --lab-id <lab_id> --domain-slice <passed|failed> --domain-compile <passed|failed> --note "<tests and clean-Console evidence>"
```

Adding the domain slice intentionally makes the earlier foundation observation
stale. After the domain command passes, rerun the same three foundation tests,
neutral button/visible-object/drag/reset observation, and clean-Console check
against the combined current Lab. Record `--gate5-foundation passed` again in a
separate command. Do not edit files between the final domain and foundation
records. `labflow` stores content digests and refuses Gate 5 approval when
either evidence set is stale.

Do not approve Gate 5 unless all three results (`gate5_foundation`,
`domain_slice`, and `domain_compile`) pass. After any Lab-specific C# edit,
treat prior compile evidence for the current gate as stale and rerun the clean
compile check before requesting approval.

Implement under `UnityProject/Assets`. Reuse Category A directly. For Category
B, extract only the generic responsibility, decouple Lab-specific names and
answers into the current Lab's declared output, add validation, and record
provenance. Never edit the Category B source. Use Category C only for reasoning
and debugging. If a change belongs in Common or another protected reusable
resource, report it as a foundation defect and wait for the user.

Keep target physics, presets, steps, rubric rules, and report questions in the
new Lab. Do not move them into Common. Never hand-edit `.unity` YAML and never
run Unity batch mode unless the user explicitly requests it.

Do not allocate Unity native-backed objects such as `MaterialPropertyBlock`,
`Material`, `Mesh`, `Texture2D`, or `RenderTexture` in a `MonoBehaviour`
constructor or instance-field initializer. Initialize them in `Awake`,
`OnEnable`, or a guarded lazy method and validate them before use.

Make every scene builder fail closed: capture build-time errors/exceptions,
abort before saving on any error, and print success only after validation and a
successful save. A success log is not evidence when the same build emitted an
error. A builder may reopen an existing output scene only when it contains the
current run's generated marker root; otherwise refuse to adopt it. Do not leave
or accept a partially initialized scene.

## Triage compiler errors before continuing

Clear the Console and compile once, then diagnose the earliest root error before
later cascades. Classify ownership before editing:

- package resolution, assembly availability, Common source, shared assets, or
  project settings: report a protected-foundation blocker;
- Lab-specific `asmdef`, namespace/import, API call, type mismatch, event
  signature, Editor/runtime boundary, or lifecycle code: fix the current Lab
  output directly and rerun the current gate compile check;
- missing Common types after an owning assembly failure: treat later messages
  as cascades and do not rewrite correct Common usages.

Never continue generating more Lab code while the current gate has compiler
errors. Do not claim that a Package constrained code generation unless the
earliest error proves package resolution, assembly visibility, or API-version
incompatibility.

## Hand off Unity-owned validation

Before asking the user to click anything in Unity, read and follow
[references/unity-beginner-handoff.md](references/unity-beginner-handoff.md).
Assume zero prior Unity experience. Give a complete numbered procedure in the
current response: exact top-menu path, expected tab/window location, exact
button/selection, current Lab target, success observation, failure screenshots,
and actions to avoid. Never use a shorthand such as `run Test Runner`, `Clear
Console`, `open Game View`, or `run the Builder` without those details, and do
not wait for the user to ask where a Unity feature is located.

Ask the user to open `UnityProject/`, then provide detailed instructions to:

1. wait for compilation and clear/report relevant Console errors;
2. run the generated `Tools > EMVR > Labs > Build ...` menu;
3. run `Tools > EMVR > Common > Validate Active Scene`;
4. test the complete desktop path;
5. test XR Device Simulator, then real hardware when available;
6. test Capture/Restore/Reset and Start/Lab/Back as applicable;
7. verify the authored initial state and the visible state after every spawn/
   load/first required action;
8. verify readable, non-overlapping UI and an unobstructed experiment region in
   the actual Game View;
9. capture initial/completed Game View and Console evidence.

For EditMode tests, always include the primary Unity 2022.3 menu path
`Window > General > Test Runner`, explain that the tab normally appears in the
lower panel, select the **EditMode** tab, give the exact current test assembly
and expected test names/count, identify **Run Selected**, define the green/pass
success state, and request Test Runner plus Console screenshots on failure.

Record only observed evidence:

```powershell
python Tools/labflow/labflow.py record --lab-id <lab_id> --compile <pending|passed|failed> --scene-built <pending|passed|failed> --note "<Gate 6 evidence>"
python Tools/labflow/labflow.py record --lab-id <lab_id> --playtest <pending|passed|failed> --note "<Gate 7 evidence>"
```

Compilation or scene generation alone is not acceptance.

## Complete acceptance and Gate 10 knowledge capture

Use Gate 8 to finish the evidence/provenance package. Use Gate 9 to present the
completed Lab for the user's final acceptance. In the Gate 9 completion report,
explicitly tell the user that approval will be followed by the required Gate 10
error-retrospective and reusable-lesson step; do not describe Gate 9 acceptance
as the end of the workflow.

After the user approves Gate 9, record that approval, then execute Gate 10 in
the next substantive response:

```powershell
python Tools/labflow/labflow.py retrospective --lab-id <lab_id>
```

Review `workflow_state.json`, ordered evidence notes, Console/build/playtest
evidence, current-Lab fixes, rejected workarounds, and reported foundation
defects. Replace every placeholder in both generated files:

- `BuildEvidence/retrospectives/<lab_id>.md`: complete run-specific timeline,
  verified root causes, fixes, verification, deferred risks, and Gate mapping;
- `KnowledgeBase/rebuild-lessons/<lab_id>.md`: only normalized, verified,
  cross-Lab error patterns, ownership, API/lifecycle constraints, prevention
  checks, and search cues for future Agents.

If the build had no errors, state that explicitly and record which safeguards
kept it clean. Exclude large logs, copied implementation code, target answers,
rubric logic, Gold details, personal paths, secrets, and unverified guesses.
Request Gate 10 approval only after both files contain no placeholders and pass
`labflow` readiness checks. Gate 10 approval marks the workflow complete.
