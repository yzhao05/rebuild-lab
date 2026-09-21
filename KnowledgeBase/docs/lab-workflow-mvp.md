# Unity VR Lab workflow MVP

The Skill and `references/workflow-gates.md` are authoritative. Execute and
approve exactly one gate at a time. A clear progression response is approval;
fixed wording is optional.

Gate 5 begins by materializing the protected
`ReusableFoundation/Gate5MinimalSlice` through
`.agents/skills/build-unity-vr-lab-blind/scripts/materialize_gate5_foundation.py`.
The mandatory room, XR Interaction Setup, and XR Device Simulator GUID closures
are imported at the same gate, not earlier.
The room source is the actual modular EMVR room under `ApprovedAssets/EMVRRoom`;
the XRI Teleportation Environment sample ground is not accepted as a room.
Gate 5 also rejects world-space UI viewed from its back by the runtime camera;
for Unity Canvas/TextMesh the readable front normal is `-Transform.forward`.
Its source and generated copy are immutable. The reusable foundation checkpoint
must pass before target-domain code is added.

Every core desktop interaction defaults to a functionally equivalent XR path
through Common/XRI: same target, state transition, visible feedback, and
success result. Gate 1 records that invariant automatically and asks the User
only about an explicit semantic exception or an unsupported action.

## Gate sequence

```text
0 boundary verified
1 Brief confirmed
2 contracts ready
3 design and API signatures approved
4 scaffold cleanly compiles
5 reusable foundation passes, then smallest domain slice works and compiles
6 integrated scene compiles/builds/validates and user accepts
8A independent student/developer manuals, then 8B incremental project master; one Gate 8 confirmation
Any code/logic blocker -> automatic retrospective -> original gate
```

## Core commands

After the rebuilding Agent has created the fixed internal child workspace and
the User has opened that child, create a clean blind-rebuild run without a donor
scene:

```powershell
python Tools/labflow/labflow.py new --mode blind-rebuild --lab-id <lab_id> --title "<title>" --domain <domain> --scene Assets/Scenes/<Output>.unity
python Tools/labflow/labflow.py source-references --lab-id <lab_id>
```

`new` generates `LabSpecs/<lab_id>/source-references.json` from the protected
catalog. It contains repository-relative POSIX references to unchanged,
read-only reusable sources. `source-references` resolves them against the
current Pack root, including after a GitHub clone; its absolute output is not
persisted.

After the Gate 0 verifier actually passes:

```powershell
python Tools/labflow/labflow.py record --lab-id <lab_id> --boundary-verifier passed --note "<observed PASS output>"
python Tools/labflow/labflow.py capture-gate0-baseline --lab-id <lab_id>
```

The second command is Agent-owned, Gate-0-only, and create-once. It does not
replace or update release frozen hashes. All later gates block if protected files no
longer match `BuildEvidence/<lab_id>/gate0-protected-baseline.json`; do not
recapture it to hide a difference.

For a maintainer-repaired pre-scaffold Gate 4 run, follow the compatibility
receipt rules in `Policies/reuse-policy.md`. Preserve the original baseline,
`BuildEvidence/<lab_id>/protected-maintenance/*.json` and the state
`protected_maintenance` registrations. The Builder cannot issue, edit, remove,
rehash or register receipts. Rerun Boundary, source-references and status;
resume the same Gate 4 only when integrity passes. Prior approvals remain
unchanged, and scaffold generation/Unity compile evidence are still required.

After each gate is complete and the user approves/progresses:

```powershell
python Tools/labflow/labflow.py approve-gate --lab-id <lab_id> --gate <N> --note "<user wording>"
```

Use `approve-gate` for every Gate, including Gate 1 and Gate 3. Validate the
five canonical contracts during Gate 2 with:

```powershell
python Tools/labflow/labflow.py validate --lab-id <lab_id>
```

During Gate 5, after Gate 4 approval, import the approved asset GUID closure
into the current Lab's generated directory. For the three mandatory Prefabs:

```powershell
python .agents/skills/build-unity-vr-lab-blind/scripts/import_approved_asset_closure.py --lab-id <lab_id> --source "EMVRRoom/Prefabs/Room_Big_Part_01.prefab"
python .agents/skills/build-unity-vr-lab-blind/scripts/import_approved_asset_closure.py --lab-id <lab_id> --source "XRInteractionToolkitSamples/Starter Assets/Prefabs/XR Interaction Setup.prefab"
python .agents/skills/build-unity-vr-lab-blind/scripts/import_approved_asset_closure.py --lab-id <lab_id> --source "XRInteractionToolkitSamples/XR Device Simulator/XR Device Simulator.prefab"
python .agents/skills/build-unity-vr-lab-blind/scripts/materialize_gate5_foundation.py --lab-id <lab_id>
```

Generate only the Gate 4 scaffold, then require a clean Unity compile before
adding domain logic:

```powershell
python Tools/labflow/labflow.py scaffold --lab-id <lab_id>
python Tools/labflow/labflow.py record --lab-id <lab_id> --scaffold-compile passed --note "<cleared Console evidence>"
```

At Gate 5 implement only one smallest end-to-end slice and require both domain
and compile evidence:

```powershell
python Tools/labflow/labflow.py record --lab-id <lab_id> --domain-slice passed --domain-compile passed --note "<tests and cleared Console evidence>"
```

First record the three foundation tests and neutral-scene check separately.
After adding the domain slice, rerun both the domain check and the three
foundation tests against unchanged current content, then record the foundation
result again. Gate 5 rejects stale content-digest evidence.

Record integrated scene evidence at Gate 6; playtest observations are optional at that same gate:

```powershell
python Tools/labflow/labflow.py record --lab-id <lab_id> --compile passed --scene-built passed --note "<Gate 6 evidence>"
python Tools/labflow/labflow.py record --lab-id <lab_id> --playtest passed --note "<optional Gate 6 observation>"
```

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
requests for changes do not approve it. Gate 8 generates and verifies handbooks after acceptance;
overall approval requires both 8A and 8B verification. There is no mandatory separate Gate 7 playtest.
Required interactions remain covered by Gate 5 and the approved Brief/acceptance.

Existing schema-2.0 runs retain Gates 0-5, source bindings, evidence and original
baseline/receipt history. Legacy Gate 6/8 approvals lack the new acceptance/handbook
meaning and cannot silently finish a run; reapprove these stages when reached.
Legacy Gate 7/9/10 records remain history and never drive the new sequence.

## Compile discipline

- Read `KnowledgeBase/docs/unity-compile-error-playbook.md` before Lab C#.
- Record exact Common/Unity/XRI API signatures in the design.
- Clear and recompile; diagnose the earliest error before cascades.
- Fix current Lab output directly. Report protected-foundation ownership.
- Add no more code while the current compile checkpoint is failing.
- Rerun the checkpoint after every Lab-specific C# edit.

The CLI never edits `.unity` YAML, starts Unity, or treats generated code as
compiled evidence.

Every Unity-owned handoff assumes that the user has never used Unity. The Agent
must provide a numbered click path with the exact top-menu location, expected
window/tab position, current target name, button, success result, failure
screenshots, and actions to avoid. In particular, EditMode testing must name
`Window > General > Test Runner`, the **EditMode** tab, exact test assembly and
expected tests/count, **Run Selected**, and the green/zero-failure result. See
the Skill reference `unity-beginner-handoff.md`; shorthand such as `run Test
Runner` is not a valid handoff.

Each run also owns `BuildEvidence/<lab_id>/reuse-report.md` and
`BuildEvidence/<lab_id>/validation-report.md`. A global or previous-run report
cannot satisfy Gate 8. Start a different Lab only from a pristine Builder Pack
copy; `labflow new` refuses mixed-run workspaces.



Original Gate 8 keeps its number and uses two ordered steps: 8A independent
student and single-Lab developer manuals; 8B incremental project Development
Handbook. Both bind the same accepted Gate 6 version. 8B preserves the existing
master and every unrelated chapter/image, replaces only the current Lab's tagged
section, and provides a before-version plus increment list. Review every page of
all PDFs, then request one Gate 8 confirmation. Collect developer material during
Gates 0-6 and resolved blockers. See
`.agents/skills/build-unity-vr-lab-blind/references/developer-handbooks.md`.
