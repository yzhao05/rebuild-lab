# Workflow gates

## Rule for every gate

Before Gate 0, classify the workspace mode. A blind rebuild, recreation, or
comparison must be created at
`EMVR_Blind_BuilderPack/RebuildWorkspaces/EMVR_Blind_Rebuild_<lab_id>` with
`create_blind_run_pack.ps1 -LabId <lab_id>`. The rebuilding Agent runs this
command from the master Pack, verifies PASS, and gives the User the exact output
path. The User only switches workspace; never ask them to run PowerShell or copy
the Pack. A Builder Pack embedded in a host project for genuinely new-Lab
development uses `integrated-development` and is not subject to that
internal-location rule.
If the current root is already that child and its `.emvr-workspace.json` matches
the requested Lab ID, skip creation and initialize/resume there. Never invoke
the master copy tool from inside a valid child.
This workspace handoff is pre-Gate setup, not a Gate 0 blocker. Gate 0 work and
artifacts begin only after `labflow new` initializes the marked child.

Use one atomic conversation work package per gate:

1. execute and verify only the current gate;
2. submit one completion report with evidence;
3. request approval without requiring a fixed reply phrase;
4. make no next-gate changes while approval is pending;
5. after approval, record it and execute only the immediately following gate.

Missing input or a defect produces one consolidated blocker report and keeps
the same gate open. Rejection or requested changes also reopen the same gate.
When the current gate is complete and awaiting approval, treat an unambiguous
progression message such as `继续`, `可以`, `可以继续`, `下一步`, `往下做`,
`开始下一阶段`, `没问题`, `通过`, `确认`, `同意`, `OK`, `go ahead`, or
`proceed` as approval of that gate. The user does not need to name the gate.
Fixed wording such as `批准 Gate N` remains valid but is optional.

Do not count silence, tool output, the Agent's own assessment, a pure question,
a negated instruction, or a correction-only request as approval. If a message
mixes progression language with requested changes or an unresolved condition,
keep the current gate open unless the user clearly accepts it as-is. The Agent
never self-approves.

After the approving/progression message, record exactly that gate and preserve
the user's wording in the note:

```powershell
python Tools/labflow/labflow.py approve-gate --lab-id <lab_id> --gate <N> --note "<user approval>"
```

Gate 0 evidence uses `record --boundary-verifier`; Gate 4 uses
`record --scaffold-compile`; Gate 5 uses
`record --gate5-foundation` first and then
`record --domain-slice/--domain-compile`; Gate 6 uses only
`record --compile/--scene-built`; Gate 7 uses only `record --playtest`. Do not
combine evidence from different gates in one command. Any Lab-specific C# edit
invalidates compile evidence for the current gate until Unity recompiles from a
cleared Console with zero compiler errors.

For a compile failure, capture the earliest error and the first 20 relevant
messages after Clear/recompile. Classify it before editing. Fix current Lab
output directly; report protected foundation failures to the user. Never add
more Lab code while the current gate still has compiler errors.

The workflow foundation is read-only during a rebuild. If a gate exposes a
problem in a Skill, policy, validator, workflow tool, Common package, approved
asset/model, knowledge source, dependency, project setting, or pre-existing
cross-Lab Unity file, mark the gate blocked and report it to the user. Do not
repair it, work around it in the new Lab, or provide replacement code. Resume
only after the user reports that the foundation has been repaired.

Whenever a gate hands a Unity-owned action to the user, follow
`unity-beginner-handoff.md`. Treat the user as a first-time Unity user and give
the exact menu path, tab/window location, click sequence, current-Lab target,
success evidence, failure screenshots, and actions to avoid. An instruction
such as `run Test Runner`, `Clear Console`, `run the Builder`, or `enter Play
Mode` is incomplete by itself and cannot satisfy a gate handoff.

## 0. Boundary verified

The verifier passes; every `file:` dependency resolves to a real
`package.json`; `manifest.json` and `packages-lock.json` agree; Common resolves
from `UnityProject/LocalPackages`; Unity MCP and `UNITY_MCP_READY` are absent;
no Gold artifact or protected-foundation repair is present. Complete this gate
before writing Lab C#. Recursively read every prior lesson under
`KnowledgeBase/rebuild-lessons/` except files named `README.md`, including the
recommended pre-workflow construction experience, and complete the current Lab's
`prior-lessons-review.md`, listing applicable safeguards and explaining lessons
that do not apply. Do not inspect `KnowledgeBase/legacy-lab-specific-summaries/`;
it is restricted old-Lab detail, not Gate 0 knowledge. When no prior lesson
exists, record `none available`. Confirm this Pack contains only the current run
and no active legacy Agent bundle or example contracts.
Record the observed boundary-script PASS with `record --boundary-verifier
passed`; policy inspection alone is not sufficient.

## 1. Brief confirmed

Identity, learning goals, tasks, formulas/units, parameter ranges, expected
results, object inventory, desktop/XR paths, lifecycle, and evidence are explicit
and user-confirmed. Mandatory room and verified XR Prefab placement/adaptation, initial
visibility, first action, post-action visibility, and Game View layout are also
explicit; `new_scene` is not treated as permission to generate a primitive room.

## 2. Contracts ready

All five contracts parse, share `lab_id`, use unique IDs, and resolve object,
step, rubric, telemetry, and assist references.

## 3. Design approved

The user approves the learning flow, clean-scene plan, Common APIs, approved
assets, new Lab code, interactions, feedback, and evidence plan. The design
contains every row of the mandatory Common/room/XR reuse audit plus environment, visibility,
builder-failure, and camera/UI decisions. It also records the real source path
or package version, assembly, and exact member signature for every Common,
Unity, and XRI API the new Lab will call. It names all three mandatory Prefabs'
generated import paths under
`Assets/Generated/<lab_id>/ApprovedDependencies/` and their import-manifest
evidence.

## 4. Scaffold ready

Runtime and Editor builder exist under `UnityProject/Assets`. The builder starts
from a new scene and adds only approved/generated content. No domain logic is
added yet, and the runtime skeleton calls no Common/domain API before Gate 5.
Unity compiles the scaffold from a cleared Console with zero compiler
errors, and `scaffold_compile` evidence is recorded before approval.

## 5. Domain slice validated

Gate 5 has two ordered checkpoints inside the same gate. Checkpoint 5A must
finish before any target-domain code is added:

1. run `import_approved_asset_closure.py` separately for the mandatory room,
   `Starter Assets/Prefabs/XR Interaction Setup.prefab`, and
   `XR Device Simulator/XR Device Simulator.prefab`; then run
   `materialize_gate5_foundation.py --lab-id <lab_id>`;
2. verify the source/copy hashes and preserve
   `BuildEvidence/<lab_id>/gate5-foundation-import.json`;
3. call the Common desktop pointer/drag, XR ray, object state, status,
   telemetry, assist, lifecycle, and replacement-spawn APIs required by the
   design; do not copy or rewrite Common source;
4. build a neutral validation unit in the new Lab scene containing the approved
   room, the complete XR Interaction Setup (not a bare XR Origin), XR Device
   Simulator with validated Up/Down Y translation, controller rays configured
   through the Common tuner, exactly one XR UI EventSystem, one persistent
   interactable button, one active offstage neutral template, one visible
   collidable spawned object, desktop drag, Capture/Restore/Reset, and visible
   status;
5. run the exact three `EMVR.ReusableFoundation.Gate5.Tests` EditMode tests:
   `InactiveReplacementTemplateIsRejected`,
   `PersistentButtonSpawnsActiveVisibleCollidableObject`, and
   `DuplicateActiveEventSystemsAreRejected`;
6. observe button click -> visible object, desktop drag, state reset, XR
   rig/rays present, clean Console, and fail-closed builder behavior; then record
   `--gate5-foundation passed`.

Checkpoint 5B then adds only the smallest target-domain closure: one domain
object, one deterministic calculation, one visible result, one desktop
interaction (plus the approved XR adapter), Common state/reset, one telemetry
event, one assist-grounding result, and focused domain EditMode tests. It
verifies the authored initial state and the state after the first
spawn/load/action. Unity recompiles from a cleared Console with zero compiler
errors. Because adding domain code invalidates the earlier foundation snapshot,
rerun the exact three foundation tests and neutral scene checks after 5B, then
record `gate5_foundation` again without changing files. The content-bound
`gate5_foundation`, `domain_slice`, and `domain_compile` evidence must all be
fresh and passing before approval.

The protected source and its generated copy are immutable. If either requires
a change, stop Gate 5 and report a foundation defect. Domain code must be added
beside the foundation, never inside it.
If EditMode tests are part of the evidence, the Agent must identify
`Window > General > Test Runner`, the **EditMode** tab, the exact current test
assembly and expected test list/count, **Run Selected**, and the required
green/zero-failure result before asking the user to act.

## 6. Unity scene validated

Unity compiles in dependency order (Common, then Lab assemblies), the Editor
menu builds the scene, Common validation passes, and the Console is clean after
Clear. The builder must fail without saving when it captures an exception/error;
its success log alone is not evidence. Game View UI is readable and does not
obscure the experiment. Unity MCP is not part of this gate.

## 7. Interaction and lifecycle passed

Desktop and XR simulator paths work; real hardware is passed or explicitly
deferred; Capture/Restore/Reset and Start/Lab/Back meet the Brief.

## 8. Acceptance package ready

`BuildEvidence/<lab_id>/reuse-report.md` and
`BuildEvidence/<lab_id>/validation-report.md`, import manifests, validation,
and reuse provenance are complete and ready for the user's final decision. No
global or previous-run evidence may satisfy this gate. This gate does not
itself accept the Lab.

## 9. User accepted

Present the completed Lab and remaining deferrals to the user. Acceptance is an
explicit user/evaluator decision, never automatic. The Gate 9 completion report
must remind the user that Gate 10 will next summarize verified errors and store
reusable lessons; Gate 9 is not the end of the workflow.

## 10. Retrospective captured

After Gate 9 approval, run `labflow retrospective`. Complete both
`BuildEvidence/retrospectives/<lab_id>.md` and
`KnowledgeBase/rebuild-lessons/<lab_id>.md` from verified evidence. Record the
ordered error timeline, root causes, ownership, fixes, proof, rejected
workarounds, deferred risks, prevention Gate, API/lifecycle constraints, and
future search cues. If no errors occurred, state that and preserve the checks
that prevented them. Remove every placeholder and exclude target answers, Gold
details, copied implementation, secrets, personal paths, and guesses. Gate 10
approval completes the workflow.
