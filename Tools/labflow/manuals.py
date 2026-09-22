"""Gate 8 student handbook tooling. Build/render never constitute human review.

No Unity, release publication, baseline maintenance or user approval is performed.
Only the current run's BuildEvidence/<lab_id>/handbooks directory is writable.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import subprocess
from datetime import datetime
from pathlib import Path, PurePosixPath
from xml.sax.saxutils import escape

SECTIONS = (
    "What is this lab about?", "Theories and Fundamentals",
    "Ways of Interaction in the VR Lab", "Guided Tours",
    "Exercises and Challenges", "Further Developments on the Lab",
)
CONTENT_CHECKS = (
    "accepted_implementation_matches", "language_matches_brief", "theory_symbols_units_limits_verified",
    "desktop_simulator_vr_controls_verified", "current_lab_screenshots_verified",
    "exercises_and_submission_match_contract", "no_answers_or_tour_solution_leaks",
    "extensions_clearly_not_implemented", "no_unapproved_legacy_requirements",
)
LAYOUT_CHECKS = (
    "no_placeholders", "formula_glyphs_readable", "images_legible",
    "no_overflow", "cross_references_correct",
)
PLACEHOLDER = re.compile(r"\b(TODO|TBD|placeholder|replace_with)\b|<insert", re.I)


def read(path):
    return json.loads(Path(path).read_text(encoding="utf-8-sig"))


def write(path, value):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix + ".tmp")
    temporary.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    temporary.replace(path)


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()


def checked_path(root, relative, prefix=None):
    if not isinstance(relative, str) or "\\" in relative or ":" in relative:
        raise ValueError("Paths must be repository-relative POSIX paths.")
    p = PurePosixPath(relative)
    if p.is_absolute() or not p.parts or any(x in (".", "..") for x in relative.split("/")):
        raise ValueError("Unsafe repository-relative path: " + relative)
    if prefix and not relative.startswith(prefix.rstrip("/") + "/"):
        raise ValueError("Path is outside the current run's permitted area: " + relative)
    result = (root / relative).resolve()
    result.relative_to(root.resolve())
    if result != root.resolve() / Path(*p.parts):
        raise ValueError("Manual paths must not traverse symlinks or junctions.")
    return result


def metadata(root, lab_id):
    """Read a flat YAML manual block with JSON scalars (valid portable YAML).

    This deliberately does not infer defaults or require PyYAML for status.
    """
    content = (root / "LabSpecs" / lab_id / "brief.yaml").read_text(encoding="utf-8-sig")
    match = re.search(r"^manual:\s*\n((?:[ \t]+[^\n]*\n|\n)*)", content + "\n", re.M)
    if not match:
        raise ValueError("Brief requires a confirmed manual block; never infer its number from lab_id.")
    result = {}
    for line in match[1].splitlines():
        if not line.strip() or line.lstrip().startswith("#"):
            continue
        field = re.fullmatch(r"  ([a-z_]+):\s*(.+)", line)
        if not field or field[1] in result:
            raise ValueError("Manual metadata requires unique two-space fields with JSON scalar values.")
        result[field[1]] = json.loads(field[2])
    if type(result.get("manual_number")) is not int or result["manual_number"] < 0:
        raise ValueError("Confirm the course manual_number as an integer; lab_id is not its source.")
    for key in ("manual_title", "language", "report_submission_requirements", "scoring_policy"):
        if not isinstance(result.get(key), str) or not result[key].strip() or PLACEHOLDER.search(result[key]):
            raise ValueError("Confirm Brief manual." + key + " before proceeding.")
    title = result["manual_title"]
    if not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9 ,&'()\-]{0,119}", title) or title != title.strip():
        raise ValueError("manual_title must be a safe English filename theme, without extension.")
    if result.get("metadata_confirmed") is not True:
        raise ValueError("Manual metadata must be confirmed by the user in the Brief.")
    return result


def metadata_errors(root, lab_id):
    try:
        metadata(root, lab_id)
        return []
    except (RuntimeError, ValueError, OSError, TypeError) as error:
        return [str(error)]


def slots(lab_id, meta):
    base = f"BuildEvidence/{lab_id}/handbooks"
    return {
        "lab_manual": {"path": f"{base}/lab-manual/Lab {meta['manual_number']} {meta['manual_title']}.pdf", "status": "required"},
        "developers_handbook": {"path": f"{base}/developer-handbook/Lab {meta['manual_number']} {meta['manual_title']} Developer Handbook.pdf",
                                "docx_path": f"{base}/developer-handbook/Lab {meta['manual_number']} {meta['manual_title']} Developer Handbook.docx", "status": "required_8a"},
        "project_handbook": {"path": f"{base}/project-master/EM VR Lab Development Handbook.pdf",
                             "docx_path": f"{base}/project-master/EM VR Lab Development Handbook.docx", "status": "required_increment_8b"},
    }


def content_digest(root, lab_id, lab_digest):
    """Bind accepted behavior, Unity GUIDs/import settings and runtime configuration."""
    lines = [lab_digest]
    for relative in ("ReusableFoundation/Gate5MinimalSlice", "UnityProject/LocalPackages/com.emvr.lab-common",
                     "UnityProject/Packages", "UnityProject/ProjectSettings"):
        directory = root / relative
        if directory.exists():
            for path in sorted(p for p in directory.rglob("*") if p.is_file()):
                lines.append(path.relative_to(root).as_posix() + "\t" + sha(path))
    assets = root / "UnityProject/Assets"
    if assets.exists():
        for path in sorted(p for p in assets.rglob("*.meta") if p.is_file()):
            lines.append(path.relative_to(root).as_posix() + "\t" + sha(path))
    return hashlib.sha256("\n".join(lines).encode("utf-8")).hexdigest().upper()


def accepted_digest(root, state, flow):
    acceptance = state.get("user_acceptance", {})
    digest = acceptance.get("content_digest")
    if not flow.gate_is_approved(state, 6) or acceptance.get("confirmed") is not True or not digest:
        raise ValueError("Gate 6 user acceptance with a content snapshot is required; legacy acceptance needs revalidation.")
    if digest != content_digest(root, state["lab_id"], flow.gate5_lab_output_digest(root, state["lab_id"])):
        raise ValueError("Accepted Lab content changed. Reopen Gate 6, revalidate and obtain user acceptance again.")
    return digest


def base_path(root, lab_id):
    return checked_path(root, f"BuildEvidence/{lab_id}/handbooks")


def source_path(root, lab_id):
    return base_path(root, lab_id) / "lab-manual.source.json"


def assessment(root, lab_id, meta):
    rubric = read(root / "LabInterfaceContracts" / f"{lab_id}_rubric.json")
    spec = rubric.get("manual_assessment")
    if not isinstance(spec, dict) or spec.get("policy") != meta["scoring_policy"] or spec.get("submission_requirements") != meta["report_submission_requirements"]:
        raise ValueError("The confirmed rubric.manual_assessment must match Brief scoring/submission requirements.")
    questions = spec.get("questions")
    if not isinstance(questions, list) or not questions or not all(isinstance(q, dict) for q in questions):
        raise ValueError("The rubric must define manual_assessment.questions; never inherit historical 100+5 points.")
    criteria = {c["criterion_id"] for row in rubric.get("rubrics", []) for c in row.get("criteria", [])}
    for i, question in enumerate(questions, 1):
        if question.get("id") != f"Q{i}" or not isinstance(question.get("prompt"), str) or not question["prompt"].strip() or not isinstance(question.get("submission"), str) or not question["submission"].strip():
            raise ValueError("Contract questions require sequential Q numbers, prompts and submission requirements.")
        refs = question.get("criterion_ids")
        if not isinstance(refs, list) or not refs or not set(refs) <= criteria:
            raise ValueError("Every question must identify existing rubric criteria.")
        points = question.get("points")
        if points is not None and (type(points) not in (int, float) or not 0 <= points < 1000000):
            raise ValueError("Question points must be nonnegative finite numbers or explicitly null for ungraded work.")
        if "points" not in question or type(question.get("bonus")) is not bool:
            raise ValueError("Each contract question explicitly declares points and bonus; no default grading scheme.")
    return questions


def validate_source(root, state, flow, source):
    if not isinstance(source, dict):
        raise ValueError("Manual source must be a JSON object.")
    lab_id = state["lab_id"]
    digest = accepted_digest(root, state, flow)
    meta = metadata(root, lab_id)
    if source.get("lab_id") != lab_id or source.get("accepted_content_digest") != digest:
        raise ValueError("Manual source must bind the current Lab's Gate 6 accepted content.")
    if PLACEHOLDER.search(json.dumps(source, ensure_ascii=False)):
        raise ValueError("Manual source still contains placeholders.")
    if any(k.lower() in ("answers", "answer_key", "solutions", "standard_answers") for k in source):
        raise ValueError("Student manuals must not include answer keys.")
    sections = source.get("sections", [])
    if not isinstance(sections, list) or len(sections) != 6 or not all(isinstance(s, dict) for s in sections) or [s.get("title") for s in sections] != list(SECTIONS):
        raise ValueError("Manual must contain the six fixed sections in order.")
    dependencies = {}
    allowed_refs = (
        f"LabSpecs/{lab_id}/", f"LabInterfaceContracts/{lab_id}_",
        f"UnityProject/Assets/Scripts/EMVR/Labs/{flow.pascal_name(lab_id)}/",
        f"UnityProject/Assets/Editor/{flow.pascal_name(lab_id)}/",
        f"UnityProject/Assets/Generated/{lab_id}/",
        f"BuildEvidence/{lab_id}/",
    )
    def bind(relative):
        path = checked_path(root, relative)
        if not path.is_file():
            raise ValueError("Manual evidence is missing: " + relative)
        dependencies[relative] = sha(path)
        return path
    for section in sections:
        if not isinstance(section.get("paragraphs"), list) or not section["paragraphs"] or not all(isinstance(x, str) and x.strip() for x in section["paragraphs"]):
            raise ValueError("Each manual section needs substantive student-facing paragraphs.")
        if not isinstance(section.get("figure_ids"), list) or not all(isinstance(x, str) for x in section["figure_ids"]):
            raise ValueError("Section figure_ids must be a list of figure IDs.")
        refs = section.get("source_refs")
        if not isinstance(refs, list) or not refs:
            raise ValueError("Every section must cite actual current-Lab sources internally.")
        for ref in refs:
            is_scene = ref == "UnityProject/" + state.get("scene", "")
            if not isinstance(ref, str) or (not ref.startswith(allowed_refs) and not is_scene) or "/handbooks/" in ref or ref.endswith("workflow_state.json"):
                raise ValueError("Manual content references must identify current accepted Lab sources, not old manuals or self-reference.")
            bind(ref)
    figures = source.get("figures", [])
    if not isinstance(figures, list) or not all(isinstance(f, dict) for f in figures):
        raise ValueError("Figures must be a list of evidence objects.")
    ids = set()
    runtime_count = 0
    for figure in figures:
        if not isinstance(figure.get("id"), str) or figure["id"] in ids:
            raise ValueError("Figure IDs must be unique strings.")
        ids.add(figure["id"])
        path = checked_path(root, figure["path"], f"BuildEvidence/{lab_id}")
        if path.suffix.lower() not in (".png", ".jpg", ".jpeg") or not figure.get("caption"):
            raise ValueError("Figures require a caption and PNG/JPEG image.")
        if figure.get("kind") == "runtime":
            runtime_count += 1
            if figure.get("accepted_content_digest") != digest or not figure.get("capture_note"):
                raise ValueError("Runtime screenshots require current acceptance binding and a capture observation.")
        elif figure.get("kind") != "theory":
            raise ValueError("Figures must distinguish runtime screenshots from theory diagrams.")
        bind(figure["path"])
    if not runtime_count:
        raise ValueError("Collect actual current-Lab screenshots; previous-Lab screenshots cannot substitute.")
    used = [fid for section in sections for fid in section.get("figure_ids", [])]
    if set(used) != ids or len(used) != len(ids):
        raise ValueError("Every figure must be placed exactly once in the six sections.")
    if not any(f.get("kind") == "runtime" and f["id"] in sections[3].get("figure_ids", []) for f in figures):
        raise ValueError("Guided Tours must contain actual current-Lab screenshots.")
    if source.get("questions") != assessment(root, lab_id, meta):
        raise ValueError("Manual questions, points, criterion mapping and submissions must exactly match the confirmed rubric.")
    return meta, dependencies


def render_pdf(root, source, meta, destination):
    # Dependencies are optional for status/readiness; required only for generation.
    import reportlab
    from reportlab.lib import colors
    from reportlab.lib.enums import TA_CENTER
    from reportlab.lib.pagesizes import A4
    from reportlab.lib.styles import ParagraphStyle
    from reportlab.pdfbase import pdfmetrics
    from reportlab.pdfbase.ttfonts import TTFont
    from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Image, KeepTogether
    font_dir = Path(reportlab.__file__).parent / "fonts"
    windows_fonts = Path(os.environ.get("SystemRoot", "C:/Windows")) / "Fonts"
    candidates = ((windows_fonts / "times.ttf", windows_fonts / "timesbd.ttf"),
                  (Path("/usr/share/fonts/truetype/dejavu/DejaVuSerif.ttf"), Path("/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf")),
                  (Path("/System/Library/Fonts/Supplemental/Times New Roman.ttf"), Path("/System/Library/Fonts/Supplemental/Times New Roman Bold.ttf")),
                  (font_dir / "Vera.ttf", font_dir / "VeraBd.ttf"))
    regular, bold = next(pair for pair in candidates if all(p.is_file() for p in pair))
    for name, path in (("Manual", regular), ("ManualBold", bold)):
        pdfmetrics.registerFont(TTFont(name, str(path)))
    styles = {
        "body": ParagraphStyle("body", fontName="Manual", fontSize=10, leading=15, spaceAfter=9),
        "heading": ParagraphStyle("heading", fontName="ManualBold", fontSize=13, leading=18, spaceBefore=16, spaceAfter=10, keepWithNext=True),
        "title": ParagraphStyle("title", fontName="ManualBold", fontSize=18, leading=24, alignment=TA_CENTER, spaceAfter=22),
        "caption": ParagraphStyle("caption", fontName="Manual", fontSize=9, leading=12, alignment=TA_CENTER, spaceAfter=12, textColor=colors.HexColor("#333333")),
    }
    def para(text, style="body"):
        glyphs = pdfmetrics.getFont(styles[style].fontName).face.charToGlyph
        missing = {c for c in text if not c.isspace() and ord(c) not in glyphs}
        if missing:
            raise ValueError("Unsupported font glyphs; supply a clearly labeled theory diagram for formulas or add reviewed font support: " + repr(sorted(missing)))
        return Paragraph(escape(text).replace("\n", "<br/>"), styles[style])
    story = [para(f"Lab {meta['manual_number']} {meta['manual_title']}", "title")]
    figures = {f["id"]: f for f in source["figures"]}
    width = A4[0] - 120
    for i, section in enumerate(source["sections"], 1):
        story.append(para(f"{meta['manual_number']}.{i} {section['title']}", "heading"))
        story.extend(para(p) for p in section["paragraphs"])
        if i == 5:
            story.append(para("Submission: " + meta["report_submission_requirements"]))
            for q in source["questions"]:
                points = "ungraded" if q["points"] is None else str(q["points"]) + " points"
                if q["bonus"]:
                    points += ", bonus"
                story.append(KeepTogether([para(q["id"] + " (" + points + "). " + q["prompt"]),
                                           para("Submit: " + q["submission"])]))
        for fid in section.get("figure_ids", []):
            fig = figures[fid]
            img = Image(str(checked_path(root, fig["path"])))
            ratio = min(width / img.imageWidth, 330 / img.imageHeight)
            img.drawWidth = img.imageWidth * ratio
            img.drawHeight = img.imageHeight * ratio
            caption = "Figure " + fid + ". " + ("Theory diagram" if fig["kind"] == "theory" else "Current Lab screenshot") + " - " + fig["caption"]
            story.append(KeepTogether([Spacer(1, 8), img, para(caption, "caption")]))
    def footer(canvas, doc):
        canvas.setFont("Manual", 8)
        canvas.drawCentredString(A4[0] / 2, 30, str(doc.page))
    temp = destination.with_suffix(".tmp.pdf")
    destination.parent.mkdir(parents=True, exist_ok=True)
    try:
        SimpleDocTemplate(str(temp), pagesize=A4, leftMargin=60, rightMargin=60,
                          topMargin=50, bottomMargin=50, title=destination.stem).build(story, onFirstPage=footer, onLaterPages=footer)
        temp.replace(destination)
    finally:
        temp.unlink(missing_ok=True)


def build(root, state, flow):
    lab_id = state["lab_id"]
    source = read(source_path(root, lab_id))
    meta, dependencies = validate_source(root, state, flow, source)
    deliverables = slots(lab_id, meta)
    pdf = checked_path(root, deliverables["lab_manual"]["path"])
    render_pdf(root, source, meta, pdf)
    from pypdf import PdfReader
    pages = len(PdfReader(str(pdf)).pages)
    manifest = {"schema_version": "1.0.0", "lab_id": lab_id,
                "accepted_content_digest": source["accepted_content_digest"],
                "source_sha256": sha(source_path(root, lab_id)), "dependencies": dependencies,
                "deliverables": deliverables, "pdf_sha256": sha(pdf), "page_count": pages}
    write(base_path(root, lab_id) / "generation.json", manifest)
    return manifest


def generation(root, state, flow):
    lab_id = state["lab_id"]
    source = read(source_path(root, lab_id))
    meta, dependencies = validate_source(root, state, flow, source)
    manifest = read(base_path(root, lab_id) / "generation.json")
    expected = {"lab_id": lab_id, "accepted_content_digest": source["accepted_content_digest"],
                "source_sha256": sha(source_path(root, lab_id)), "dependencies": dependencies,
                "deliverables": slots(lab_id, meta)}
    if any(manifest.get(k) != v for k, v in expected.items()):
        raise ValueError("Manual generation evidence is stale; regenerate and repeat content/layout reviews.")
    pdf = checked_path(root, manifest["deliverables"]["lab_manual"]["path"])
    if sha(pdf) != manifest.get("pdf_sha256") or type(manifest.get("page_count")) is not int or manifest["page_count"] < 1:
        raise ValueError("Generated manual PDF changed or page evidence is invalid.")
    return manifest


def render_pages(root, state, flow):
    manifest = generation(root, state, flow)
    base = base_path(root, state["lab_id"])
    dest = base / "renders" / manifest["pdf_sha256"]
    dest.mkdir(parents=True, exist_ok=True)
    pdf = checked_path(root, manifest["deliverables"]["lab_manual"]["path"])
    subprocess.run(["pdftoppm", "-r", "120", "-png", str(pdf), str(dest / "page")], check=True, capture_output=True)
    images = sorted(dest.glob("page-*.png"), key=lambda p: int(p.stem.split("-")[-1]))
    if len(images) != manifest["page_count"]:
        raise ValueError("PDF render count does not match every generated page.")
    record = {"generation_sha256": sha(base / "generation.json"),
              "pages": [{"page": i, "path": p.relative_to(root).as_posix(), "sha256": sha(p)} for i, p in enumerate(images, 1)]}
    write(base / "rendering.json", record)
    return record


def review_errors(root, state, flow):
    """Machine checks bind explicit human observations; they cannot see correctness."""
    try:
        manifest = generation(root, state, flow)
        base = base_path(root, state["lab_id"])
        rendering = read(base / "rendering.json")
        review = read(base / "review.json")
        generation_sha = sha(base / "generation.json")
        if rendering.get("generation_sha256") != generation_sha or review.get("generation_sha256") != generation_sha:
            raise ValueError("Content/layout review is not bound to this generation.")
        if review.get("rendering_sha256") != sha(base / "rendering.json"):
            raise ValueError("Layout review is not bound to the current page renders.")
        pages = rendering.get("pages", [])
        if [p.get("page") for p in pages] != list(range(1, manifest["page_count"] + 1)):
            raise ValueError("Render evidence must include every page exactly once.")
        for page in pages:
            path = checked_path(root, page["path"], f"BuildEvidence/{state['lab_id']}/handbooks/renders")
            if sha(path) != page.get("sha256"):
                raise ValueError("A rendered page changed after review.")
        for key in CONTENT_CHECKS:
            check = review.get("content", {}).get(key, {})
            if check.get("passed") is not True or not isinstance(check.get("observation"), str) or not check["observation"].strip():
                raise ValueError("Content review requires an observed result: " + key)
        reviewed = review.get("pages", [])
        if [p.get("page") for p in reviewed] != [p["page"] for p in pages]:
            raise ValueError("Layout review must explicitly inspect every rendered page.")
        for page in reviewed:
            if not isinstance(page.get("observation"), str) or not page["observation"].strip() or any(page.get(k) is not True for k in LAYOUT_CHECKS):
                raise ValueError("Page review has missing observations or unresolved layout defects.")
        if not isinstance(review.get("reviewer"), str) or not review["reviewer"].strip() or not isinstance(review.get("reviewed_at"), str) or PLACEHOLDER.search(json.dumps(review)):
            raise ValueError("Review requires reviewer, time and substantive observations without placeholders.")
        if datetime.fromisoformat(review["reviewed_at"].replace("Z", "+00:00")).tzinfo is None:
            raise ValueError("Review time must include a timezone.")
        return []
    except (OSError, ValueError, TypeError, KeyError, AttributeError) as error:
        return ["Lab Manual: " + str(error)]


def handbook_tools():
    import importlib.util
    spec = importlib.util.spec_from_file_location("emvr_handbooks", Path(__file__).with_name("handbooks.py"))
    tools = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(tools)
    return tools


def gate_errors(root, state, flow):
    return handbook_tools().gate_errors(root, state, flow)


def gate_step(root, state, flow):
    return handbook_tools().gate_step(root, state, flow)


def approval_binding(root, state):
    return handbook_tools().approval_binding(root, state)


def main():
    import labflow as flow
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project-root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("action", choices=("init", "build", "render", "check"))
    parser.add_argument("--lab-id", required=True)
    args = parser.parse_args()
    root = args.project_root.resolve()
    try:
        lab_id = flow.require_lab_id(args.lab_id)
        state = flow.load_current_state(root, lab_id)
        errors = flow.protected_baseline_errors(root, state) + flow.source_reference_errors(root, state)
        if errors:
            raise ValueError("; ".join(errors))
        flow.require_handbook_operation(root, state)
        if flow.gate_is_approved(state,8) and args.action != 'check':
            raise ValueError('Reopen handbooks before revising an approved Gate 8 bundle.')
        digest = accepted_digest(root, state, flow)
        meta = metadata(root, lab_id)
        if args.action == "init":
            path = source_path(root, lab_id)
            if path.exists():
                raise ValueError("Manual source already exists; preserve it and revise it deliberately.")
            template = read(root / "LabSpecs/templates/lab-manual.template.json")
            template.update(lab_id=lab_id, accepted_content_digest=digest, questions=assessment(root, lab_id, meta))
            write(path, template)
            write(base_path(root, lab_id) / "deliverables.json", slots(lab_id, meta))
        elif args.action == "build":
            build(root, state, flow)
        elif args.action == "render":
            render_pages(root, state, flow)
        else:
            errors = review_errors(root, state, flow)
            if errors:
                raise ValueError("; ".join(errors))
        print("Student manual " + args.action + " completed. No user approval or Gate completion recorded; complete developer review and then the incremental project handbook before Gate 8 approval.")
        return 0
    except (RuntimeError, ValueError, OSError, KeyError, TypeError, AttributeError, ImportError, subprocess.CalledProcessError) as error:
        print("ERROR: " + str(error))
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
