# Reuse decision guide

## A — direct reuse

Use validated Common APIs and approved assets directly, without modifying their
source. Prefer Common for XR
pointer/ray/grab, mouse drag, stable IDs, snapshots, lifecycle, telemetry,
assist grounding, status, and validation.

Directly reusable does not mean optional. Apply the universal and
capability-triggered requirements in
[mandatory-reuse-matrix.md](mandatory-reuse-matrix.md). Record wiring and
evidence for every mandatory row in the current design.

The protected Gate 5 foundation is Category A with controlled-copy semantics,
not an editable Category B example. Run `materialize_gate5_foundation.py`; it
copies the manifest-verified source byte-for-byte into the current Lab. Do not
edit either `ReusableFoundation/Gate5MinimalSlice/Source/` or
`Assets/Generated/<lab_id>/Gate5Foundation/`. Domain code calls or composes
around it from separate Lab-owned paths.

## B — conditional reuse

Use an archived component only when all answers are yes:

1. Is its responsibility useful in two or more labs, or clearly independent of
   the target Lab's scientific answer?
2. Can it be named without Lab1/Lab2/Lab3 or domain-specific identity?
3. Does it avoid target formulas, presets, step order, rubric, and object IDs?
4. Can it be validated independently?
5. Has its provenance been recorded?

If all answers are yes, adapt a copy only inside the current Lab's declared
output and leave the archived source unchanged. If any answer is no, implement
it as new target-Lab code or request a user decision. If the needed correction
belongs in Common or another reusable source, report a foundation defect and
wait for the user rather than editing or supplying a patch for that source.

## C — knowledge-only

Use summaries and examples to recognize failure patterns and structure work.
Keep them read-only. Never infer that an old fix, example value, or example
scene is a current requirement.

Only normalized files under `KnowledgeBase/rebuild-lessons/` are mandatory
historical context. The legacy Agent bundle, old issue catalog, original
summaries, and legacy example contracts are restricted under
`KnowledgeBase/legacy-lab-specific-summaries/` and are unavailable in a blind
run.

## Gold — unavailable

Old complete scenes, implementation scripts, rebuild overlays, prefabs with
answer-bearing wiring, and GUID relationships are not inputs. Missing behavior
must come from the current specification or the user.
