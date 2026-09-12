# Workflow evals

Copy `templates/rebuild-eval.template.json` into
`BuildEvidence/<lab_id>/eval/<eval_id>.json` for the current rebuild. Store raw
screenshots and Console text in the same current-Lab evidence tree. Score the
workflow, not only the final scene.

Require `gate0_protected_baseline_captured=true` only when the release Builder
Boundary passed first, `BuildEvidence/<lab_id>/gate0-protected-baseline.json`
was created exactly once in Gate 0, and no later gate reported a protected-file
difference. A recaptured baseline or Builder-edited release hash is a failure.

Require the portable-source checks only when
`LabSpecs/<lab_id>/source-references.json` matches the protected catalog and
its state-recorded SHA-256 values, all source paths are repository-relative
POSIX, all source access is read-only, and no persisted artifact contains the
runtime resolver's machine-specific absolute paths. Those runtime paths may
legitimately differ after a GitHub clone.

Never overwrite an earlier accepted eval case; create a new version so workflow changes can be compared.

Set the beginner-handoff checks to true only when every requested Unity action
includes its exact menu/tab/button location and success/failure evidence. A Test
Runner handoff must explicitly identify `Window > General > Test Runner`,
**EditMode**, the exact assembly and tests/count, and **Run Selected**; a later
clarification prompted by the user does not make the original handoff complete.

Set `default_desktop_xr_equivalence_preserved=true` only when each core desktop
action and its XR adapter target the same object and produce the same state
transition, visible feedback, and success result. Keep
`desktop_xr_exceptions_explicitly_confirmed=true` when there are no exceptions
or every exception has direct User confirmation; ordinary bindings must not be
treated as missing Gate 1 input.

For Gate 5, require the mandatory-room import manifest and verified source/copy
hashes, the materializer manifest/evidence, an unchanged generated foundation
copy, all 3/3 `EMVR.ReusableFoundation.Gate5.Tests` tests green, and the neutral
button/spawn/visibility check before scoring the domain slice. Both foundation
and domain evidence must carry current matching content digests.
