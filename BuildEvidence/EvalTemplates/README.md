# Workflow evals

Copy `templates/rebuild-eval.template.json` into
`BuildEvidence/<lab_id>/eval/<eval_id>.json` for the current rebuild. Store raw
screenshots and Console text in the same current-Lab evidence tree. Score the
workflow, not only the final scene.

Never overwrite an earlier accepted eval case; create a new version so workflow changes can be compared.

Set the beginner-handoff checks to true only when every requested Unity action
includes its exact menu/tab/button location and success/failure evidence. A Test
Runner handoff must explicitly identify `Window > General > Test Runner`,
**EditMode**, the exact assembly and tests/count, and **Run Selected**; a later
clarification prompted by the user does not make the original handoff complete.

For Gate 5, require the mandatory-room import manifest and verified source/copy
hashes, the materializer manifest/evidence, an unchanged generated foundation
copy, all 3/3 `EMVR.ReusableFoundation.Gate5.Tests` tests green, and the neutral
button/spawn/visibility check before scoring the domain slice. Both foundation
and domain evidence must carry current matching content digests.
