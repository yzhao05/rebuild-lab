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
That command also creates `LabSpecs/<lab_id>/source-references.json` from
`Policies/reuse-source-references.json`. Run `labflow source-references` to
validate and resolve it on the current machine; never persist the displayed
absolute paths.

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
`record --compile/--scene-built`; optional `record --playtest` observations belong to Gate 6. Do not
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
with a valid, read-only per-Lab source-reference manifest whose catalog and
manifest hashes are recorded in state. Every source path must be normalized
repository-relative POSIX and resolve inside the current Builder Pack; the
manifest and referenced source content must remain unchanged through completion.
Complete this gate before writing Lab C#. Recursively read every prior lesson under
`KnowledgeBase/rebuild-lessons/` except files named `README.md`, including the
recommended pre-workflow construction experience, and complete the current Lab's
`prior-lessons-review.md`, listing applicable safeguards and explaining lessons
that do not apply. Do not inspect `KnowledgeBase/legacy-lab-specific-summaries/`;
it is restricted old-Lab detail, not Gate 0 knowledge. When no prior lesson
exists, record `none available`. Confirm this Pack contains only the current run
and no active legacy Agent bundle or example contracts.
Record the observed boundary-script PASS with `record --boundary-verifier
passed`; policy inspection alone is not sufficient.
Immediately afterward, still in Gate 0, run
`python Tools/labflow/labflow.py capture-gate0-baseline --lab-id <lab_id>`.
This per-run baseline is created once, cannot overwrite an existing baseline,
and is checked by every later gate. Never edit the release frozen hashes or
recapture the baseline to accept a change. If a later comparison names a
changed protected file, stop and report that foundation defect. The current-run blocker
current-Lab lesson is the only planned baseline exclusion.

For a maintainer-repaired pre-scaffold Gate 4 run, follow the compatibility
receipt rules in `Policies/reuse-policy.md`. Preserve the original baseline,
`BuildEvidence/<lab_id>/protected-maintenance/*.json` and the state
`protected_maintenance` registrations. The Builder cannot issue, edit, remove,
rehash or register receipts. Rerun Boundary, source-references and status;
resume the same Gate 4 only when integrity passes. Prior approvals remain
unchanged, and scaffold generation/Unity compile evidence are still required.

## 1. Brief confirmed

Identity, learning goals, tasks, formulas/units, parameter ranges, expected
results, object inventory, desktop actions, lifecycle, and evidence are explicit
and user-confirmed. Every core desktop action is automatically assigned the
default Common/XRI-equivalent XR path with the same target, state transition,
visible feedback, and success result. The User is asked about XR mapping only
for an explicit semantic exception or an action the Common baseline cannot
express; the Brief records exceptions, normally `none`. Mandatory room and
verified XR Prefab placement/adaptation, initial
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
The design also names `LabSpecs/<lab_id>/source-references.json`, confirms that
the original reusable sources are read-only, and records only repository-relative
paths. API audits may cite these references; they may not duplicate or rewrite
the referenced code.

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
3. call the Common desktop pointer/drag and approved XR adapters as one
   functionally equivalent input path, plus object state, status,
   telemetry, assist, lifecycle, and replacement-spawn APIs required by the
   design; do not copy or rewrite Common source;
4. build a neutral validation unit in the new Lab scene containing the approved
   room with active floor/wall/roof geometry, the complete XR Interaction Setup
   (not a bare XR Origin), XR Device
   Simulator with validated Up/Down Y translation, controller rays configured
   through the Common tuner, exactly one XR UI EventSystem, one persistent
   interactable button, one active offstage neutral template, one visible
   collidable spawned object, desktop drag, Capture/Restore/Reset, and visible
   status;
5. run the exact three `EMVR.ReusableFoundation.Gate5.Tests` EditMode tests:
   `InactiveReplacementTemplateIsRejected`,
   `PersistentButtonSpawnsActiveVisibleCollidableObject`, and
   `DuplicateActiveEventSystemsAreRejected`;
6. make the builder reject missing room geometry and a world-space Canvas or
   TextMesh whose readable front normal (`-Transform.forward`) points away from
   the actual runtime camera; observe the
   room shell, non-mirrored UI, button click -> visible object, desktop drag,
   state reset, XR rig/rays, clean Console, and fail-closed behavior in Game
   View; then record `--gate5-foundation passed`.

Checkpoint 5B then adds only the smallest target-domain closure: one domain
object, one deterministic calculation, one visible result, one desktop
interaction with its default equivalent approved XR adapter, Common state/reset, one telemetry
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

## 6. Unity scene validated and user accepted

Unity compiles in dependency order, the Editor menu builds the scene, Common
validation passes, and the Console is clean after Clear. Errors stop the builder
before saving. Game View is readable and matches the approved Brief. Present the
result, required behavior checks and explicit deferrals to the user. A clear
progression response such as `继续` accepts this ready gate; do not require a fixed
acceptance phrase or a second Gate 9 approval. Record the original user wording.
Gate 6 approval advances directly to Gate 8. The former Gate 7 is removed.

## 8. Two sequential handbook steps using the original Gate 8 number

Follow [developer-handbooks.md](developer-handbooks.md). 8A generates independent
student PDF and single-Lab developer DOCX/PDF from the same Gate 6 accepted version;
verify implementation consistency and every rendered page. 8B then increments a
saved copy of the existing project Development Handbook, changing only its tagged
current-Lab section. Preserve other chapters/images, avoid duplicate additions,
record the increment list and inspect the entire exported master PDF. Reuse and
validation reports remain internal. Submit both steps together for one Gate 8
confirmation, never separate 8A approval. No original gates are renumbered.

## Automatic blocker recovery (formerly Gate 10)

The main sequence is `0 -> 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 8`. Gates 7 and 9
are removed; Gate 10 is now an automatic interruption, not an approval gate.
At any gate, an observed code failure or logic blocker immediately enters recovery
without asking permission to start a retrospective. Keep the originating gate.
Ordinary pending evidence, a question, or waiting for user approval is not a failure.

`record ... failed` and failed contract validation open recovery automatically.
For blockers observed outside those commands, the Agent immediately runs:

```powershell
python Tools/labflow/labflow.py retrospective --lab-id <lab_id> --kind <code|logic|protected-foundation> --note "<observed blocker and evidence>"
```

Classify ownership and diagnose the earliest root cause. Repair current-Lab output
within authorization, then rerun the failed check. Protected-foundation defects
still require maintainer repair; automatic recovery grants no permission to edit
sources, hashes, policies or baselines. Record their diagnosis and remain blocked.
Do not switch gates, self-approve, or invent passing evidence to leave recovery.

After the fix is verified, record any required passed evidence and run:

```powershell
python Tools/labflow/labflow.py retrospective --lab-id <lab_id> --resolve --root-cause "<verified cause>" --fix "<authorized change>" --verification "<observed verification>" --prevention "<earliest preventive check>" --lesson "<verified reusable lesson>"
```

This seals `BuildEvidence/<lab_id>/blocker-retrospectives/<sequence>.json` and
appends only to this run's `KnowledgeBase/rebuild-lessons/<lab_id>.md`. The tool
checks existing history/lesson hashes; never hand-edit a completed report or prior
lesson. Exclude target answers, Gold details, copied implementation, secrets,
personal paths and guesses. Unknown causes remain open, not verified lessons.
Repeated failures update the open incident; later incidents get new sequence IDs.
Resolution returns to the originating gate, with its approval still required.
There is no separate user approval for the retrospective itself.

Gate 6 includes user acceptance of the validated scene, result and declared deferrals.
When ready and presented, a clear user reply such as `继续`, `可以`, `通过`, `下一步`
or `OK` is acceptance; retain the user's wording. Questions, negations, silence and
requests for changes do not approve it. Gate 8 prepares and verifies handbooks after acceptance;
its approval requires all defined handbook deliverables, including the incremental project master. There is no mandatory separate Gate 7 playtest.
Required interactions remain covered by Gate 5 and the approved Brief/acceptance.

Existing schema-2.0 runs retain Gates 0-5, source bindings, evidence and original
baseline/receipt history. Legacy Gate 6/8 approvals lack the new acceptance/handbook
meaning and cannot silently finish a run; reapprove these stages when reached.
Legacy Gate 7/9/10 records remain history and never drive the new sequence.
