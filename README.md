# EMVR Blind Builder Pack

中文用户使用说明（包括 AI 调用格式和 Gate 0–10 操作）：
[README_USER.md](README_USER.md)

This directory is the self-contained workspace given to the rebuilding Agent.
It was curated from `EMVR_Workflow_Baseline_Backup_20260810`; the frozen
baseline remains the Gold Reference and is not copied into this pack.

## Layout

- `UnityProject/`: clean Unity 2022.3 project; no original Lab 1/2/3 scenes or
  implementation scripts.
- `UnityProject/LocalPackages/com.emvr.lab-common/`: canonical validated
  reusable runtime package.
- `ApprovedAssets/`: approved models, XR/XRI assets, materials, audio, and UI
  resources. Copy only the assets required by the approved design.
- `ReusableFoundation/Gate5MinimalSlice/`: protected cross-Lab button,
  EventSystem, spawn-visibility, fail-closed builder, and regression-test code.
  It is copied only by the Gate 5 materializer and never edited by a Builder
  Agent.
- `LabSpecs/` and `LabInterfaceContracts/`: the single current run's
  behavior/specification inputs.
- `KnowledgeBase/`: reusable lessons, playbooks, examples, and conditional code
  archives, including `docs/unity-compile-error-playbook.md`. This is not a
  source of target-Lab answers.
- `.agents/skills/build-unity-vr-lab-blind/`: the enforced rebuild workflow.
- `Policies/`: machine-readable reuse and Gold-exclusion rules.
- `BuildEvidence/<lab_id>/`: per-run provenance and validation records.
- `Tools/labflow/`: contract-first workflow state machine and clean-scene
  scaffold generator.

## Operating boundary

For a blind rebuild, open this master Pack only long enough to create the fixed
child workspace described below; perform the rebuild in that child, not here or
in the Pack's parent. In integrated-development mode, use the host-project
integration root. For a controlled evaluation, run the Builder Agent under an
account or sandbox that cannot read the frozen Gold Reference.

Run the boundary check before starting a rebuild:

```powershell
powershell -ExecutionPolicy Bypass -File .agents/skills/build-unity-vr-lab-blind/scripts/verify_builder_pack.ps1
```

The committed `.gitattributes` uses `* -text` to preserve all reviewed file
bytes in Git, regardless of `core.autocrlf`. Existing LF/CRLF text and binary
assets must not be mass-converted. GitHub does not require CRLF. Preserving
bytes also keeps the frozen hashes valid in source ZIP/TAR downloads.
The committed `.gitignore` keeps internal rebuild workspaces and generated
Unity/Python/IDE files outside the maintained source revision.

Publish the clean master, including hidden `.agents`, `.gitattributes` and
`.gitignore`, rather than an active Lab workspace. For an existing repository,
stage new files with `git add -A` and refresh tracked blobs with
`git add --renormalize .` under the new attributes before reviewing/committing.
Do this only in the clean publishing copy. Validate a fresh clone and source
archive before upload. Downloaded root folders may have a branch suffix;
rename the extracted master to `EMVR_Blind_BuilderPack` before creating a run.
See README_USER.md for the download/upload steps.

Open `UnityProject/` in Unity. The local package dependency is self-contained
and points only to `../LocalPackages/com.emvr.lab-common`.

Unity MCP is currently disabled and is not used by the rebuild workflow. Its
preserved 0.79.1 NuGet material is stored outside Unity import scope under
`OptionalIntegrations/UnityMCP/`. MCP-generated Agent Skills are archived in
the same location, leaving only the blind builder Skill active. Unity
validation is handed to the user.
Every such handoff assumes no Unity experience and must include exact menu,
window/tab, click, success, and failure-evidence instructions. In particular,
Test Runner is opened from `Window > General > Test Runner`; the Agent must also
name the exact EditMode assembly, tests/count, and **Run Selected** action.

The workflow now requires a clean Unity scaffold compile at Gate 4. Gate 5
first copies and verifies the immutable reusable minimal foundation, runs its
three generic EditMode regressions plus a neutral button/spawn/visibility
scene check, and only then adds and compiles the smallest domain slice.
After the domain code is present, both the domain check and the three
foundation regressions are rerun against the same unchanged content; Gate 5
stores content digests and rejects stale evidence.
Integrated scene compilation remains a separate Gate 6 check, so compiler
errors cannot accumulate across the build.
Gate 9 records final user acceptance and announces the required Gate 10
retrospective. Gate 10 stores the run audit under `BuildEvidence/retrospectives/`
and the normalized future-facing lesson under
`KnowledgeBase/rebuild-lessons/`; every later rebuild reviews those lessons at
Gate 0.

Integrity has two levels. Release frozen hashes remain maintainer-owned in
`Policies/pack-inventory.json` and the Gate 5 manifest. After that release
Boundary passes, the rebuilding Agent creates a one-time per-run snapshot with
`python Tools/labflow/labflow.py capture-gate0-baseline --lab-id <lab_id>`.
Gates 1–10 compare protected files against
`BuildEvidence/<lab_id>/gate0-protected-baseline.json`; the Agent cannot update
or overwrite the snapshot to accept a later change.

For a maintainer-repaired pre-scaffold Gate 4 run, follow the compatibility
receipt rules in `Policies/reuse-policy.md`. Preserve the original baseline,
`BuildEvidence/<lab_id>/protected-maintenance/*.json` and the state
`protected_maintenance` registrations. The Builder cannot issue, edit, remove,
rehash or register receipts. Rerun Boundary, source-references and status;
resume the same Gate 4 only when integrity passes. Prior approvals remain
unchanged, and scaffold generation/Unity compile evidence are still required.

Use one isolated Builder Pack copy per Lab run. `labflow new` refuses to start
when another run's specifications, contracts, Unity output, or evidence remain.
This prevents a rebuild Agent from seeing an earlier implementation.

For a blind rebuild/recreation/comparison, the rebuilding Agent—not the User—
creates the fixed internal child workspace from the maintained master Pack:

```powershell
powershell -ExecutionPolicy Bypass -File Tools/create_blind_run_pack.ps1 -LabId <lab_id>
```

The only allowed rebuild workspace name/path is
`RebuildWorkspaces/EMVR_Blind_Rebuild_<lab_id>` inside this Pack. Open that
child itself as the Agent workspace. The Agent must run the PowerShell command
and report its exact absolute output path; the User only switches to that path
and must not be asked to run the copy command. Do not run implementation in the
master Pack. If this Pack is embedded in a host project to develop a genuinely
new Lab rather than rebuild one, use `labflow new --mode
integrated-development`; the internal child-location restriction does not apply
in that mode.

`labflow new` also creates the immutable
`LabSpecs/<lab_id>/source-references.json` binding. It passes references to the
original reusable code/assets without modifying their content and stores only
repository-relative POSIX paths. On this machine or after a GitHub clone, the
Agent resolves them from the currently opened Pack root with:

```powershell
python Tools/labflow/labflow.py source-references --lab-id <lab_id>
```

Absolute paths printed by that command are temporary diagnostics and must not
be saved in project files.

`ApprovedAssets/` is outside the Unity import root. Import selected assets with
the Skill's `import_approved_asset_closure.py`; generated copies belong only
under `UnityProject/Assets/Generated/<lab_id>/ApprovedDependencies/`. Plan the
mandatory room and verified XR Setup/Simulator baselines in Gate 1/3, but
execute these copies only during Gate 5.

Core desktop interactions use functionally equivalent XR behavior by default
through Common/XRI. Gate 1 records the default automatically and requests User
input only for an explicit semantic exception or an action the baseline cannot
represent.

At Gate 5, materialize the protected source separately:

```powershell
python .agents/skills/build-unity-vr-lab-blind/scripts/materialize_gate5_foundation.py --lab-id <lab_id>
```

This writes only an immutable byte-identical copy under
`UnityProject/Assets/Generated/<lab_id>/Gate5Foundation/` and its provenance
record. Lab-specific code lives beside it and must not alter it.

If `labflow status` reports an older state schema, run
`python Tools/labflow/labflow.py migrate-state --lab-id <lab_id>`. Migration
preserves the files but invalidates all old approvals/evidence and resumes at
Gate 0, because older evidence was not bound to current content.
