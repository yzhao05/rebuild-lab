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

Open this directory as the Agent workspace. Do not open its parent directory as
the workspace. For a controlled evaluation, run the Builder Agent under an
account or sandbox that cannot read the frozen Gold Reference.

Run the boundary check before starting a rebuild:

```powershell
powershell -ExecutionPolicy Bypass -File .agents/skills/build-unity-vr-lab-blind/scripts/verify_builder_pack.ps1
```

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

Use one isolated Builder Pack copy per Lab run. `labflow new` refuses to start
when another run's specifications, contracts, Unity output, or evidence remain.
This prevents a rebuild Agent from seeing an earlier implementation.

Create that isolated copy from the maintained master pack before starting a new
Lab (choose a new, empty destination for every run):

```powershell
powershell -ExecutionPolicy Bypass -File Tools/create_blind_run_pack.ps1 -Destination "E:\path\to\EMVR_Blind_Run_<lab_id>"
```

Open the generated destination itself as the Agent workspace. Do not run a new
Lab directly in a master pack that already contains another run.

`ApprovedAssets/` is outside the Unity import root. Import selected assets with
the Skill's `import_approved_asset_closure.py`; generated copies belong only
under `UnityProject/Assets/Generated/<lab_id>/ApprovedDependencies/`. Plan the
mandatory room in Gate 1/3, but execute this copy only during Gate 5.

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
