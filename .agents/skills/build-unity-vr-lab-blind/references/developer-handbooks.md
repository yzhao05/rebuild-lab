# Original Gate 8: 8A Lab books, then 8B project handbook increment

This is Gate **8 from the original ten-gate numbering**. Keep the current main
sequence `0 -> 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 8`; do not renumber it or restore
Gates 7/9/10. 8A and 8B are sequential steps inside one Gate 8, with one final
user confirmation after both pass. Neither generating files nor completing 8A
alone is Gate completion. Workflow maintenance never executes a Lab's gates.

## Readers, version and deliverables

Both 8A books use the same Gate 6 accepted content snapshot:

- Student Lab Manual: what to learn, operate, observe and submit. Preserve the
  six-section rules in [student-lab-manual.md](student-lab-manual.md); no answers.
- Single-Lab Developer Handbook: install, understand, change, verify and debug
  the actual accepted project. It may explain implementation, but is a separate
  document, never appended to the student PDF.

Use confirmed course metadata, not the Lab ID suffix:

```text
Lab <number> <theme>.pdf
Lab <number> <theme> Developer Handbook.docx
Lab <number> <theme> Developer Handbook.pdf
```

8B delivers updated copies named:

```text
EM VR Lab Development Handbook.docx
EM VR Lab Development Handbook.pdf
```

All working outputs remain under `BuildEvidence/<lab_id>/handbooks/`, in
`lab-manual/`, `developer-handbook/` and `project-master/` respectively. The
original master and reference handbooks are read-only inputs. Publication to a
host `Docs/Handbook/` is a separate explicitly authorized release action, not an
implicit overwrite. 8B still delivers the incremented master copies in the run.

Use `handbooks.py` and `manuals.py` below. Both reject changed acceptance content.
Acceptance binds Unity `.meta` GUID/import settings, Packages and ProjectSettings
as well as Lab code and reusable dependencies. Subsequent changes to teaching,
controls, implementation or reusable dependencies
require Gate 6 revalidation/acceptance and renewed handbook checks. Changes to a
final 8A book/review also invalidate 8B; it must derive from the final 8A version.

## Accumulate material throughout development

Agent extracts available facts rather than asking the user to transcribe files.
Recording Gates 0-6 approval automatically collects the available current facts
before saving that approval. The explicit command also refreshes material during
the gate and after blocker resolution without approving anything.
At each completed gate and after a resolved blocker, run:

```text
python Tools/labflow/handbooks.py collect --lab-id <lab_id>
```

This records current source hashes, environment/package facts and existing
evidence statuses in `BuildEvidence/<lab_id>/developer-notes/materials.json`
and content-addressed snapshots. It never claims a procedure was executed merely
because its code exists. It collects no raw chat transcript. Retain the original
gate/blocker evidence for attribution; draft concise verified explanations as
work progresses, rather than reconstructing the history at Gate 8.

| Stage | Material to collect |
| --- | --- |
| Gate 0, after baseline capture | Unity/packages, platform/dependencies, source manifests, immutable boundaries |
| Gates 1-2 | confirmed scope, terminology, units, parameters, contracts |
| Gate 3 | architecture, audited APIs, responsibilities and extension points |
| Gates 4-5 | runtime/editor entrypoints, actual room/XR/Common/lighting reuse, domain implementation |
| Every resolved blocker | verified symptom, cause, repair, observed verification, prevention and limits |
| Gate 6 | accepted content, actual run steps, captures, supported modes and explicit deferrals |
| Gate 8A | reconcile collected material against final code; generate and verify the independent books |
| Gate 8B | derive a scoped master increment from final 8A; inspect the entire exported PDF |

Legacy runs may collect the surviving evidence when they reach Gate 8. Mark
missing history or observations honestly; do not fabricate historical snapshots.
The user primarily supplies course formatting requirements and Unity/device
observations/screenshots unavailable to the Agent. Ask only for missing facts
when actually running the gate, not during workflow maintenance.

## Developer Handbook content

Eight fixed sections:

1. **Project Overview**: actual objectives, implemented scope, supported modes,
   accepted content/commit when available, validation dates and known limits.
2. **Development Environment and Setup**: versions from project configuration;
   download/open/dependency/compile/build/run steps, with observable success results.
3. **Project Architecture**: real directory purposes, scene entry, Runtime/Editor
   split, component relationships, dataflow and a current architecture diagram.
4. **Reusable Foundation**: Common, all five frozen room instances and source
   modules, XR Setup, simulator/controller/rays, lighting execution and GI;
   identify exact sources, read-only imports and current-Lab extension positions.
5. **Lab Implementation**: formulas/units/assumptions, numerical/dataflow logic,
   visualization, equivalent desktop/XR controls, UI updates, state/reset.
6. **Extending and Modifying the Lab**: real examples of where/why to change,
   affected behavior and required validation. Do not paste entire source files
   or merely enumerate classes. Immutable sources remain immutable.
7. **Testing and Troubleshooting**: exact test/menu entrypoints, expected results,
   verified failures/causes/fixes and limitations. Label every operational recipe
   `executed` or `document_checked`, with evidence; unresolved ones remain `pending`.
8. **Build Release and Collaboration**: actual build outputs, Git workflow,
   asset/.meta pairing, frozen byte integrity, release checks and document updates.

The attached historic development DOCX informs these themes only. Its old Unity
version, script names, scene wiring and installation instructions are not facts
about a new Lab. Old master content is consulted for 8B reuse of general prose
after 8A is verified, never to supply missing implementation for a blind rebuild.

Use actual hierarchy/Inspector/runtime captures. Label diagrams as diagrams, not
Unity captures. Missing required images or verification remains pending and blocks
delivery; do not invent them. Explain already known limitations explicitly.
Filter retrospective material: remove local temporary/private paths, chat process,
unsupported guesses, repeated logs and irrelevant maintainer receipt details.
Retain useful verified diagnosis and developer action paths. The master increment
does not become another Lab's requirement or an approved reusable-code source.

## 8A tool sequence and evidence

Install the Pack's `Tools/labflow/manual-requirements.txt` in the chosen Python
environment. PDF rasterization needs Poppler `pdftoppm`. Developer/master DOCX
export uses Microsoft Word on Windows via a separate hidden, read-only instance;
it closes without saving the DOCX or touching other open documents. If Word or
Poppler is unavailable, report that dependency and keep rendering pending. Do
not replace the master with a text reconstruction as a conversion workaround.

```text
python Tools/labflow/manuals.py init --lab-id <lab_id>
python Tools/labflow/handbooks.py init-developer --lab-id <lab_id>
python Tools/labflow/handbooks.py build-8a --lab-id <lab_id>
python Tools/labflow/manuals.py render --lab-id <lab_id>
python Tools/labflow/handbooks.py render-developer --lab-id <lab_id>
python Tools/labflow/handbooks.py prepare-review --lab-id <lab_id> --kind student
python Tools/labflow/handbooks.py prepare-review --lab-id <lab_id> --kind developer
```

Complete both source JSONs before build. `build-8a` generates the student PDF
first and the developer DOCX second in the same step; export produces its separate
PDF. A partial build is not success. Developer sections carry internal current
source references. `procedures` cover setup/run/modify/test/release; each has
steps, expected result, verification label, observation and evidence refs.
`executed` requires actual current-Lab evidence, never only code. A document-only
review is valid when labeled accurately; it must not become a runtime claim.

Developer figures use `id`, `kind` (architecture/runtime/inspector/hierarchy/flow),
`path`, `section` (1-8), `caption`, `accepted_content_digest` and `provenance`.
Collect them from the current Lab; architecture diagrams are authored from the
current source relationships. Every delivered procedure must have its verification
resolved; pending drafts are retained but cannot pass.

Keep the student `review.json` rules. `prepare-review` creates the matching
review with current hashes and every page listed; all checks remain false.
Review both books and fill real observations, then run:

```text
python Tools/labflow/handbooks.py prepare-review --lab-id <lab_id> --kind 8a
```

Complete the three cross-document checks in `8a-review.json`, then run
`python Tools/labflow/handbooks.py check-8a --lab-id <lab_id>`. The helper
never fills passing verdicts or overwrites existing reviews. Explicitly
archive a superseded review before preparing a replacement.
 The developer review binds
`developer-rendering.json` by SHA-256, records all developer content checks and
observations for **every page**. The cross-document review's `binding` is the
object automatically filled by `prepare-review --kind 8a`; its three checks
verify accepted version, operations/implementation and scope/controls/limits.
Record actual observations, reviewer and timezone-qualified review time. The
tool verifies hashes/page coverage; a human review establishes semantic truth.

## 8B is an increment, never a regenerated master

Only after 8A passes, stage the explicitly authorized existing master:

```text
python Tools/labflow/handbooks.py stage-master --lab-id <lab_id> --source "<authorized existing master DOCX>"
python Tools/labflow/handbooks.py init-master-addendum --lab-id <lab_id>
# Complete master-addendum.json before the following command.
python Tools/labflow/handbooks.py build-8b --lab-id <lab_id>
python Tools/labflow/handbooks.py render-master --lab-id <lab_id>
python Tools/labflow/handbooks.py prepare-review --lab-id <lab_id> --kind master
# Inspect every page and complete master-review.json before checking.
python Tools/labflow/handbooks.py check --lab-id <lab_id>
```

Staging saves the original bytes in `project-master/before/<sha256>/` and records
a portable `master-base.json`; the external absolute argument is never persisted.
If an existing master PDF is also supplied, retain its before-version alongside
the DOCX as read-only evidence. Do not edit the external original in place.

`init-master-addendum` fills the current Lab and final 8A binding from the
staged base. Complete `master-addendum.json`; existing work is never overwritten.
Each `shared_references` entry has `text` (the citation) and `target_text`
(the exact existing heading or paragraph in the saved master). The tool
resolves that target outside the current Lab section and rejects missing
or self-referential targets. Do not invent a section or page number. Only add:

- current Lab introduction, entrypoints and module relationships;
- verified unique dependencies/new information;
- references to existing general environment/room/XR/lighting sections;
- references to the final student and single-Lab developer manuals.

Record discovered old errors in `existing_issues`; do not fix or rewrite them
under this increment. Do not recopy existing shared instructions. A missing shared
section must be reported in `existing_issues`; supplement verified needed facts
under `unique_dependencies`, and cite only actual existing shared targets.

The tool inserts one `EMVR-LAB:<lab_id>` tagged section before the final section
properties, or replaces exactly that section on subsequent runs. Rerunning the
same increment cannot duplicate it. If a current Lab already has an untagged
section, identify it for explicit migration review first; do not append a second
version or take ownership of an arbitrary old section. Ambiguous/rewritten tags
stop instead of guessing a replacement boundary.

Every other body byte and DOCX package part (including existing pictures, styles,
relationships, other Labs and section layout) must match the saved base. The
increment tool changes only its owned body block. It never rebuilds the whole
book through `python-docx`. Page numbers/field display can update during PDF
export. Explicit TOC/cross-reference edits, if needed, must be scoped and listed;
they are not permission to resave/rewrite unrelated chapters or bypass preservation.

The generated `master-change-list.json` identifies insertion/replacement, before
and after section hashes, preserved outside-body hash, unchanged package parts and
old issues left untouched. Review it alongside the DOCX/PDF. `master-review.json`
binds `master-rendering.json`, passes all master checks, and explicitly inspects
**all pages of the whole project PDF**, including unchanged chapters.
Developer/master export records retain the actual PDF page count separately
from render and review lists. Missing export evidence or truncated page lists
requires rerendering; shortening both lists cannot satisfy full coverage.

8B is an increment into an existing document, so its review scope differs from
the two newly authored 8A books:

- The new or updated current-Lab section must contain no placeholders. Confirm
  `new_or_updated_section_no_placeholders`; adding an issue record cannot waive
  unfinished content in the owned section. Authoring prompts such as
  `[Describe the structure ...]` are also rejected in newly written prose.
- Preserve defects already present in untouched base chapters. Record their
  original location and description in `master-addendum.json` `existing_issues`;
  verbatim quotes of original unfinished wording are permitted in this internal
  evidence. The generation record and increment change list retain these issues.
  They do not require unrequested rewriting and do not alone block Gate 8B.
- Inspect **every PDF page** for defects introduced by this increment: missing
  formula glyphs, degraded images, overflow, and broken cross-references. Pass
  `no_new_formula_glyph_defects`, `no_new_image_legibility_defects`,
  `no_new_overflow`, and `no_new_cross_reference_errors` only after comparison
  with the saved base. A defect caused or worsened by the increment must be fixed;
  it cannot be categorized as an inherited issue.
- On each master review page, `existing_issue_indexes` lists the zero-based
  entries in `existing_issues` that appear there; use `[]` when none appear.
  Keep observations factual; put verbatim unfinished text in the issue record.
  `prepare-review --kind master` prepares these fields and leaves checks pending.
  Do not reuse the new-document `no_placeholders` page check for the whole master.

Review DOCX/PDF consistency for the complete master. Missing page reviews still
block approval. Student and single-Lab developer manuals remain newly authored
documents: their full no-placeholder and layout requirements are unchanged.

## One final Gate 8 confirmation

After 8A consistency checks, 8B preservation/change-list checks and all three PDFs'
page reviews pass, present the independent student manual, developer DOCX/PDF,
incremented master DOCX/PDF and increment summary together for Gate 8 confirmation.
Preserve internal reuse/validation reports as evidence. Record only the user's
actual approving/progression wording with `labflow approve-gate --gate 8`.
8A has no separate gate approval; 8B must not start while 8A is unverified.

`labflow status` reports `gate8_step` as `8A`, `8B`, or
`awaiting_joint_confirmation`. Final Gate 8 approval binds the reviewed handbook
bundle; edited books cannot inherit an old confirmation. Before document-only
revisions run `labflow.py reopen-handbooks --lab-id <lab_id> --note "<reason>"`;
this archives the old Gate 8 approval while preserving Gate 6 acceptance. Lab
implementation/teaching changes still require `reopen-acceptance` and Gate 6
revalidation. Status checks completed bundles again instead of blindly trusting
an old completion flag.

## Recovery and version changes

Follow [workflow-gates.md](workflow-gates.md#recovery-checks-and-version-bound-acceptance)
for current-step repair/check commands, verified incident resolution, contract
rechecks, input confirmation and acceptance digests. Its version-change section
defines how an invalidated accepted Lab returns from blocked Gate 8 to Gate 6;
an unverified superseded incident must not appear as a successful troubleshooting
recipe in the Developer Handbook.
