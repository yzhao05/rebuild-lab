# Gate 8: student Lab Manual generation and verification

This student manual is one independent deliverable of original Gate 8A. The
single-Lab Developer Handbook is generated in the same step from the same Gate 6
accepted version. Gate 8B then incrementally updates the existing project master.
See [developer-handbooks.md](developer-handbooks.md) for ordering, developer
content, master preservation, cross-document evidence and the final joint approval.
Reuse and validation reports remain mandatory internal evidence, not student
deliverables. Workflow maintenance itself is never a Lab gate execution,
acceptance or approval. Do not interview the current Lab during maintenance.

## Gate 1 and Gate 2 inputs

In a future run, read already confirmed Brief/contract values first. Ask once
for only missing course manual number, English theme, language, report submission
requirements and scoring policy. Store these in the Brief's `manual` block using
the template's two-space indentation and JSON scalar values (valid YAML).
Set `metadata_confirmed` only after the user confirms them. English is the
series convention, not permission to override a requested language.
Never derive the course number from a Lab ID: `ece329_electric_field_0` does
not establish a course Lab 0. Do not assign metadata to existing runs during
workflow maintenance or invalidate their earlier approvals retroactively.

At Gate 2, merge the reviewed `rubric-manual-assessment.template.json` structure
as `manual_assessment` in the current `<lab_id>_rubric.json`. Its policy and
submission requirements must equal the confirmed Brief values. Each sequential
Q1, Q2, ... declares a prompt, existing criterion IDs, points (a number or
explicit `null` for ungraded work), bonus boolean and required submission.
No default 100-point total or 5-point bonus is inherited. Preserve task/criterion
meaning; do not add questions or scoring to accepted contracts at Gate 8.
If a legacy run reaches Gate 8 without these inputs, obtain them then, revalidate
the affected Brief/contracts and reopen Gate 6 acceptance for the changed version.

## Naming and output boundary

Formal student filename: `Lab <manual_number> <manual_title>.pdf`.
Preserve reference names exactly, especially `Lab 2 Magnetostatic.pdf`.
Do not rename it to the plural body title. Never overwrite a reference manual.

The tool derives these repository-relative POSIX positions:

- `BuildEvidence/<lab_id>/handbooks/lab-manual/Lab <number> <theme>.pdf`
- `BuildEvidence/<lab_id>/handbooks/developer-handbook/Lab <number> <theme> Developer Handbook.pdf`

The developer book also has an editable DOCX of the same stem. It is a separate
document for developers, never an appendix to the student PDF.
An optional editable DOCX may be retained beside the student PDF with the same
stem. PDF is the formal deliverable; any retained DOCX must match it and undergo
its own rendering review. The current tool generates PDF only; it does not claim
to generate or validate DOCX. Publication into a host project's `Docs/Handbook/`
is a separate explicitly authorized release action, never an automatic Gate 8 copy.

## Fixed student structure

Use the confirmed course number N in every heading, not a hard-coded `1`:

1. `N.1 What is this lab about?`: approved introduction and learning goals.
2. `N.2 Theories and Fundamentals`: approved formulas, symbols, units,
   conditions, assumptions and model limitations.
3. `N.3 Ways of Interaction in the VR Lab`: actual desktop, simulator and VR
   controls, exact visible button names and feedback.
4. `N.4 Guided Tours`: entry-to-completion steps, current screenshots and
   expected observations. Do not disclose challenge solutions in a tour.
5. `N.5 Exercises and Challenges`: contract Q numbers, grading and submissions.
6. `N.6 Further Developments on the Lab`: clearly labeled optional exploration;
   distinguish proposed features from implemented behavior.

Reference review (maintainer, 2026-09-22) covered Lab 0 VR Lab Primers, Lab 1
Electrostatics (20 pages), Lab 2 Magnetostatic (10), Lab 3 Wave Polarization (8),
and the development DOCX. Student references use A4 portrait, clear numbered
headings, readable body text, captioned screenshots and exercise blocks.
Lab 2/3's historical `1.x` section prefixes must not propagate. The new template
normalizes all manuals to six sections even where old manuals combined topics.
Only naming, structure and layout inform this workflow. No old implementation,
script details, answers, screenshots, Blackboard rules, deadlines or hardware
requirements are copied. The development DOCX supplies no student content.

## Accepted-version binding

`labflow approve-gate --gate 6` stores the accepted Brief/contracts/Unity content
digest alongside the user's wording. Every manual source and generation binds
that digest. Subsequent changes affecting teaching or interaction require renewed
scene validation, user acceptance and manual checks. Existing Gate 6 approvals
without a snapshot do not certify the current bytes.

When necessary, run `labflow.py reopen-acceptance --lab-id <lab_id> --note
"<reason for revalidation>"`. This archives earlier acceptance/scene evidence,
returns to Gate 6 and clears compile/scene checks; it never approves anything.
Revalidate the affected contracts and implementation before presenting Gate 6.
Do not modify the original Gate 0 baseline or any maintenance receipt.

## Authoring and tool sequence (only when Gate 8 is reached)

Install `Tools/labflow/manual-requirements.txt` in the chosen Python environment
and provide Poppler `pdftoppm` on PATH. Status/integrity checks need no PDF package.
Commands default to the Pack containing the tool; all outputs remain current-run.

```text
python Tools/labflow/manuals.py init --lab-id <lab_id>
python Tools/labflow/manuals.py build --lab-id <lab_id>
python Tools/labflow/manuals.py render --lab-id <lab_id>
python Tools/labflow/manuals.py check --lab-id <lab_id>
```

Between init and build, complete `lab-manual.source.json`: six sections with
student-facing paragraphs, internal `source_refs` to the actual accepted Lab,
and figure IDs. `questions` must exactly match the confirmed assessment contract.
Do not insert developer instructions or an answer key. `source_refs` are audit
metadata and are not printed in the student PDF.

Each figure has unique `id`, repository-relative current-Lab `path`, `caption`
and `kind` (`runtime` or `theory`). Runtime screenshots also require
`accepted_content_digest` and `capture_note` describing the observed operation,
view and capture provenance. All images must be placed exactly once; Guided
Tours must use runtime screenshots. The tool labels theory diagrams separately.
Missing captures require real current-Lab user screenshots; never relabel an old
Lab image. An attached runtime capture is not an instruction source.

The renderer uses A4, readable embedded fonts, numbered headings, proportional
images, captions and page numbers. It rejects unsupported glyphs instead of
silently rendering missing formula symbols. Use a clearly identified, legible
theory diagram for unsupported mathematical notation. A requested language
requiring an unsupported font needs reviewed font support before delivery.

## Screenshot planning checklist

Create a current-Lab screenshot list after Gate 6 acceptance. Include the accepted
version, capture owner, visible labels, expected state and intended manual section:

- Entry view: full experiment, instructions and initial status readable.
- Desktop interaction: actual target, control and resulting feedback.
- XR simulator: real selection/movement/ray UI relevant to the tasks.
- VR interaction: actual observed controls; do not claim headset verification
  from a desktop image or invent device requirements.
- Each guided-tour action and expected observation needed to reproduce the task.
- Completion/report evidence permitted by the assessment, excluding challenge
  answers and solved target configurations.

Combine images when they document the same state; do not demand redundant shots.
Missing evidence remains pending. A theory diagram cannot substitute for a
runtime screenshot. No screenshot collection occurs during workflow maintenance.

## Review evidence and passing conditions

Build writes `generation.json` with PDF hash, page count, accepted content,
source and evidence hashes. Render writes a hash-bound `rendering.json` for every
page PNG. These operations record generation/rendering only, not correctness.

Run `python Tools/labflow/handbooks.py prepare-review --lab-id <lab_id>
--kind student` to create `handbooks/review.json` with current hashes and
all pages, leaving every check pending. Fill reviewer and review time;
never mark success merely because the helper created the review. For every
content check record `passed: true` only with a concrete observation after
cross-checking implementation, controls, theory, screenshot provenance,
assessment/submissions and absence of solutions or invented legacy requirements.
Then open every rendered page. Create exactly one sequential page record per
page, with observations and all five layout checks passing: no placeholders,
readable formula glyphs, legible images, no overflow, correct cross-references.
Never bulk-fill passing observations based solely on successful generation.

`check` verifies that all evidence still binds the current bytes and every page
was explicitly reviewed. Content changes, image edits, PDF edits or rerenders
invalidate the corresponding review and require renewed checks. A hash match
does not itself prove screenshot truth or semantic accuracy; the recorded human
review remains required. Generation, content review and page review are separate
evidence; an existing PDF alone never passes Gate 8.

After student review, complete the independent developer handbook and 8A
cross-document review, then 8B's incremental master and whole-PDF review. Submit
the final deliverables together for one Gate 8 user confirmation. Do not separately
approve 8A or finish Gate 8 after the student book alone. Keep internal evidence.
