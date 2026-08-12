---
knowledge_kind: rebuild_lessons
source_run_type: pre_workflow_legacy
source_scope: builder_common_xr_minimal_validation
verified: true
migrated_on: 2026-08-11
source_material:
  - KnowledgeBase/docs/unity-compile-error-playbook.md
  - LabSpecs/lab2_magnetostatics/workflow_state.json
  - user-validated Common minimal-validation and XR debugging evidence
---

# Pre-workflow Builder, Common, XR, and minimal-validation lessons

These lessons came from Builder Pack preparation and manual minimal-validation
work performed before Gate 10 existed. They are cross-Lab safeguards, not old
Lab implementation guidance.

## Verified reusable failure patterns

### Package and assembly foundation

- A local UPM path is resolved relative to `UnityProject/Packages/manifest.json`,
  not the repository root. The resolved folder must contain `package.json`, and
  `packages-lock.json` must use the identical dependency string.
- A stale readiness define can make Unity compile an optional integration before
  its generated DLLs exist, causing hundreds of missing-type errors. A readiness
  marker is evidence only when its complete, version-matched dependencies are
  present.
- Missing `EMVR.Common` and many missing Common interfaces can be cascades from
  one package/assembly failure. Diagnose the earliest owning error before
  rewriting otherwise-correct Lab calls.
- Multiple Common mirrors can drift. Use one canonical Unity package location;
  if a legacy mirror remains, require full-directory hash equality.

### Unity code initialization and scene building

- Unity native-backed objects such as `MaterialPropertyBlock`, `Material`,
  `Mesh`, `Texture2D`, and `RenderTexture` must not be created in a
  `MonoBehaviour` constructor or instance-field initializer. Initialize them in
  `Awake`, `OnEnable`, or a guarded lazy method.
- A scene builder can log success after an earlier exception and leave a partial
  scene. Capture Error/Exception/Assert, abort before save, restore/discard the
  partial scene, and emit success only after clean validation and save.
- Compilation and scene generation are separate evidence. A generated `.unity`
  asset does not prove Play Mode behavior or readable Game View layout.

### Environment and authored state

- A clean new scene does not mean rebuilding the room from primitive walls.
  Instantiate the approved reusable room Prefab and add only Lab apparatus and
  non-destructive adaptations.
- An initially empty visualization can be intentional, but the Brief must state
  the first required action and exact post-action visible state. A hidden
  template and a failed spawn look identical without this contract.
- Buttons, status text, experiment geometry, and runtime camera must be reviewed
  together in actual Game View; compile success cannot detect overlap or
  obstruction.

### Desktop/XR parity and ray tuning

- Mouse drag passing does not prove XR grab passing. Validate independent input,
  ray hit, select/grab, movement, release, feedback, and telemetry paths.
- Controller models, ray interactors, line visuals, and the transform being
  moved must belong to the same intended rig. Moving a separate helper or
  drawing a debug line can create a new center laser without changing the
  controller ray height.
- Height shortcuts must translate the controller/ray origin or the exact shared
  rig transform. They must not instantiate another laser, move only a visual
  model, or change an unrelated camera-centered ray.
- The XR Device Simulator ScreenSpaceOverlay warning describes an overlay that
  is not visible in VR and has rendering cost. It is not by itself proof that
  mouse or XR interaction failed; diagnose interaction separately.
- Telemetry debug output such as drag-started/ended confirms event emission but
  is not a warning and does not prove the acceptance condition passed.

### Validation semantics

- A minimal scene must begin READY and transition to PASS only after the
  required user action and observable result. Automatic startup PASS cannot
  validate interaction.
- Display the selected object, drag/grab state, position/result, telemetry, and
  assist status so the tester can identify which subsystem failed.
- Test mouse and XR simulator separately. Real hardware may be explicitly
  deferred, but the deferral must be recorded rather than silently treated as
  passed.

## Ownership and allowed fix location

- Manifest, lockfile, Common package, optional-integration DLLs/defines,
  ProjectSettings, approved room, and shared Skill/tool defects: protected
  foundation; stop and report to the user.
- Current-Lab API calls, lifecycle initialization, scene builder, UI layout,
  spawn behavior, and current-Lab rig wiring: fix in declared Lab output.
- Common/XR helper behavior verified as shared and defective: report it; do not
  back-port a repair during the Lab rebuild.

## Prevention checks by gate

- Gate 0: resolve every local package; compare manifest/lock; verify Common,
  disabled integrations, room Prefab, and prior lessons.
- Gate 1: author initial/first-action/post-action state and independent desktop/
  XR acceptance behavior.
- Gate 3: record real API signatures, rig transform ownership, environment
  reuse, builder failure policy, and Game View layout.
- Gate 4: compile only scaffold code from a cleared Console.
- Gate 5: require action-driven PASS for one mouse/XR-capable minimal slice.
- Gate 6: require clean compile, fail-closed scene build, Common validation, and
  unobstructed Game View.
- Gate 7: test desktop, simulator, lifecycle, telemetry, and hardware deferral.

## API and lifecycle constraints

- Inspect real installed Common/XRI members and assemblies before calling them.
- Keep Editor APIs out of runtime assemblies.
- Initialize native-backed objects only in Unity lifecycle or guarded lazy code.
- Move the transform that owns the actual controller ray when implementing ray
  height adjustment.
- Preserve one authoritative interaction/event owner to avoid duplicate rays,
  callbacks, toggles, and PASS logs.

## Search cues for future rebuilds

`package.json cannot be found`, `manifest lock mismatch`, `EMVR.Common missing`,
`UNITY_MCP_READY`, `NuGet DLL missing`, `CreateImpl is not allowed`,
`MaterialPropertyBlock null`, `success log after exception`, `partial scene`,
`empty initial state`, `spawn invisible`, `mouse works XR fails`,
`controller ray too high`, `center laser appears`, `automatic PASS`,
`XR Device Simulator UI warning`.

## Exclusions

Do not use this file to re-enable Unity MCP, modify protected foundation, infer
target-Lab answers, or copy old Lab implementation details.
