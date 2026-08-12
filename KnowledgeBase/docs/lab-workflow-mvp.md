# Unity VR Lab workflow MVP

The Skill and `references/workflow-gates.md` are authoritative. Execute and
approve exactly one gate at a time. A clear progression response is approval;
fixed wording is optional.

Gate 5 begins by materializing the protected
`ReusableFoundation/Gate5MinimalSlice` through
`.agents/skills/build-unity-vr-lab-blind/scripts/materialize_gate5_foundation.py`.
The mandatory room GUID closure is imported at the same gate, not earlier.
Its source and generated copy are immutable. The reusable foundation checkpoint
must pass before target-domain code is added.

## Gate sequence

```text
0 boundary verified
1 Brief confirmed
2 contracts ready
3 design and API signatures approved
4 scaffold cleanly compiles
5 reusable foundation passes, then smallest domain slice works and compiles
6 integrated scene compiles/builds/validates
7 interaction and lifecycle pass
8 acceptance package ready
9 user accepted; announce Gate 10
10 retrospective and reusable lesson captured
```

## Core commands

Create a clean run without a donor scene:

```powershell
python Tools/labflow/labflow.py new --lab-id <lab_id> --title "<title>" --domain <domain> --scene Assets/Scenes/<Output>.unity
```

After the Gate 0 verifier actually passes:

```powershell
python Tools/labflow/labflow.py record --lab-id <lab_id> --boundary-verifier passed --note "<observed PASS output>"
```

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
into the current Lab's generated directory. For the mandatory room:

```powershell
python .agents/skills/build-unity-vr-lab-blind/scripts/import_approved_asset_closure.py --lab-id <lab_id> --source "XRInteractionToolkitSamples/Starter Assets/DemoSceneAssets/Prefabs/Teleportation Environment.prefab"
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

Keep integrated scene and playtest evidence in their own gates:

```powershell
python Tools/labflow/labflow.py record --lab-id <lab_id> --compile passed --scene-built passed --note "<Gate 6 evidence>"
python Tools/labflow/labflow.py record --lab-id <lab_id> --playtest passed --note "<Gate 7 evidence>"
```

After the Gate 8 evidence package, Gate 9 obtains final user acceptance and
must announce the required Gate 10 retrospective. After Gate 9 approval:

```powershell
python Tools/labflow/labflow.py retrospective --lab-id <lab_id>
```

Complete both generated files and remove every placeholder:

- `BuildEvidence/retrospectives/<lab_id>.md`
- `KnowledgeBase/rebuild-lessons/<lab_id>.md`

Approve Gate 10 only after both pass readiness checks. At Gate 0 of every later
run, read every prior lesson and complete
`LabSpecs/<lab_id>/prior-lessons-review.md` before approval.

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
