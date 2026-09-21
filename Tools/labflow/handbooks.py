"""Original Gate 8: 8A independent Lab books, then 8B incremental project book.

Evidence collection is available during development. Nothing approves a gate,
launches Unity, publishes into a host project, or edits a reference handbook.
"""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import re
import shutil
import subprocess
import tempfile
import zipfile
from datetime import datetime
from pathlib import Path
from xml.etree import ElementTree as ET
from xml.sax.saxutils import escape

_spec = importlib.util.spec_from_file_location("handbook_manuals", Path(__file__).with_name("manuals.py"))
m = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(m)

DEVELOPER_SECTIONS = (
    "Project Overview", "Development Environment and Setup", "Project Architecture",
    "Reusable Foundation", "Lab Implementation", "Extending and Modifying the Lab",
    "Testing and Troubleshooting", "Build Release and Collaboration",
)
DEVELOPER_CHECKS = ("same_accepted_version", "environment_and_entrypoints_verified",
    "architecture_and_code_match", "reuse_readonly_boundaries_correct", "extension_and_test_entrypoints_clear",
    "execution_and_document_checks_distinguished", "screenshots_and_diagrams_verified",
    "no_chat_private_paths_or_unverified_claims", "docx_pdf_content_match")
CROSS_CHECKS = ("same_version", "student_operations_match_implementation", "scope_controls_and_limits_consistent")
MASTER_CHECKS = ("only_current_lab_section_changed", "other_chapters_and_images_preserved",
    "shared_material_referenced_without_duplication", "eight_a_content_matches", "change_list_verified",
    "existing_issues_reported_without_unrequested_fixes", "docx_pdf_content_match")
W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"
ERRORS = (OSError, ValueError, TypeError, KeyError, AttributeError, zipfile.BadZipFile)


def digest_value(value):
    return hashlib.sha256(json.dumps(value, ensure_ascii=False, sort_keys=True).encode("utf-8")).hexdigest().upper()


def base(root, state):
    return m.base_path(root, state["lab_id"])


def load_json(path):
    value = m.read(path)
    if not isinstance(value, dict):
        raise ValueError("Evidence must be a JSON object: " + path.name)
    return value


def text(value):
    return isinstance(value, str) and bool(value.strip())


def clean_prose(value):
    if not text(value) or m.PLACEHOLDER.search(value) or re.search(r"[A-Za-z]:[\\/]|file://|/Users/|/home/|/tmp/|AppData[\\/]", value):
        raise ValueError("Handbook prose contains missing content, placeholders or a local/private path.")


def source_allowed(relative, lab_id, flow, state):
    prefixes = (f"LabSpecs/{lab_id}/", f"LabInterfaceContracts/{lab_id}_", f"BuildEvidence/{lab_id}/",
                f"UnityProject/Assets/Scripts/EMVR/Labs/{flow.pascal_name(lab_id)}/",
                f"UnityProject/Assets/Editor/{flow.pascal_name(lab_id)}/", f"UnityProject/Assets/Generated/{lab_id}/",
                "UnityProject/LocalPackages/com.emvr.lab-common/", "ReusableFoundation/Gate5MinimalSlice/",
                "UnityProject/Packages/", "UnityProject/ProjectSettings/", "Policies/")
    return (isinstance(relative, str) and (relative.startswith(prefixes) or relative == "UnityProject/" + state.get("scene", ""))
            and "/handbooks/" not in relative and not relative.endswith("workflow_state.json"))


def collect(root, state, flow):
    """Collect facts/links, not chat transcripts or inferred verification results."""
    lab = state["lab_id"]
    roots = (root / "LabSpecs" / lab, root / "UnityProject/Assets/Scripts/EMVR/Labs" / flow.pascal_name(lab),
             root / "UnityProject/Assets/Editor" / flow.pascal_name(lab), root / "BuildEvidence" / lab)
    candidates = [p for directory in roots if directory.exists() for p in directory.rglob("*") if p.is_file()]
    candidates += list((root / "LabInterfaceContracts").glob(lab + "_*.json"))
    candidates += [root / p for p in ("UnityProject/ProjectSettings/ProjectVersion.txt", "UnityProject/Packages/manifest.json",
                                    "UnityProject/Packages/packages-lock.json", "Policies/reuse-source-references.json")]
    files = {}
    for path in candidates:
        rel = path.relative_to(root).as_posix()
        if path.is_file() and source_allowed(rel, lab, flow, state) and "/developer-notes/" not in rel:
            files[rel] = m.sha(path)
    facts = {}
    version = root / "UnityProject/ProjectSettings/ProjectVersion.txt"
    if version.exists():
        facts["unity_version"] = version.read_text(encoding="utf-8-sig").strip()
    manifest = root / "UnityProject/Packages/manifest.json"
    if manifest.exists():
        facts["package_dependencies"] = load_json(manifest).get("dependencies", {})
    record = {"lab_id": lab, "observed_gate": flow.current_gate(state), "files": files, "environment_facts": facts,
              "verification": {k: v for k, v in state.get("evidence", {}).items() if v in ("passed", "failed", "pending")},
              "note": "File/config observations only. Source presence does not prove a procedure was executed."}
    directory = m.checked_path(root, f"BuildEvidence/{lab}/developer-notes")
    snapshot = directory / "snapshots" / (digest_value(record) + ".json")
    if not snapshot.exists():
        m.write(snapshot, record)
    m.write(directory / "materials.json", record)
    return record


def developer_source(root, state, flow):
    source = load_json(base(root, state) / "developer.source.json")
    accepted = m.accepted_digest(root, state, flow)
    if source.get("lab_id") != state["lab_id"] or source.get("accepted_content_digest") != accepted:
        raise ValueError("Developer Handbook must bind the same Gate 6 accepted version.")
    sections = source.get("sections")
    if not isinstance(sections, list) or [s.get("title") for s in sections] != list(DEVELOPER_SECTIONS):
        raise ValueError("Developer Handbook requires the eight fixed sections.")
    dependencies = {}
    def bind(relative):
        if not source_allowed(relative, state["lab_id"], flow, state):
            raise ValueError("Developer sources must describe the current project, not old handbook implementation: " + str(relative))
        path = m.checked_path(root, relative)
        dependencies[relative] = m.sha(path)
    for section in sections:
        if not isinstance(section.get("paragraphs"), list) or not section["paragraphs"] or not section.get("source_refs"):
            raise ValueError("Every developer section needs prose and real current-project sources.")
        for paragraph in section["paragraphs"]:
            clean_prose(paragraph)
        for ref in section["source_refs"]:
            bind(ref)
    procedures = source.get("procedures", [])
    if not isinstance(procedures, list) or {p.get("kind") for p in procedures} != {"setup", "run", "modify", "test", "release"}:
        raise ValueError("Document actual setup, run, modify, test and release procedures.")
    for procedure in procedures:
        clean_prose(procedure.get("title")); clean_prose(procedure.get("expected_result"))
        if not isinstance(procedure.get("steps"), list) or not procedure["steps"]:
            raise ValueError("Every procedure needs reproducible steps.")
        for step in procedure["steps"]:
            clean_prose(step)
        if procedure.get("verification") not in ("executed", "document_checked", "pending"):
            raise ValueError("Label each procedure executed, document_checked, or pending; never infer execution.")
        if procedure["verification"] == "pending":
            raise ValueError("Required procedure verification is pending; retain the pending draft, do not deliver or approve it.")
        clean_prose(procedure.get("observation"))
        if not procedure.get("evidence_refs"):
            raise ValueError("Procedure verification requires evidence references.")
        for ref in procedure["evidence_refs"]:
            if procedure["verification"] == "executed" and not ref.startswith(f"BuildEvidence/{state['lab_id']}/"):
                raise ValueError("Executed procedures require observed current-Lab evidence, not source code alone.")
            bind(ref)
    figures = source.get("figures", [])
    if not isinstance(figures, list) or not any(f.get("kind") == "architecture" for f in figures):
        raise ValueError("Include a current-project architecture diagram.")
    ids = set()
    for fig in figures:
        if not text(fig.get("id")) or fig["id"] in ids or fig.get("kind") not in ("architecture", "runtime", "inspector", "hierarchy", "flow"):
            raise ValueError("Developer figures require unique IDs and an explicit kind.")
        ids.add(fig["id"])
        clean_prose(fig.get("caption"))
        if type(fig.get("section")) is not int or not 1 <= fig["section"] <= 8:
            raise ValueError("Place every developer figure in a section 1-8.")
        m.checked_path(root, fig["path"], f"BuildEvidence/{state['lab_id']}")
        if fig.get("accepted_content_digest") != accepted or not text(fig.get("provenance")):
            raise ValueError("Figures need current-version binding and truthful diagram/capture provenance.")
        bind(fig["path"])
    clean_prose(source.get("known_limitations"))
    return source, dependencies


def developer_paths(root, state):
    slot = m.slots(state["lab_id"], m.metadata(root, state["lab_id"]))["developers_handbook"]
    return m.checked_path(root, slot["docx_path"]), m.checked_path(root, slot["path"])


def build_developer(root, state, flow):
    from docx import Document
    from docx.shared import Inches, Pt
    from docx.oxml import OxmlElement
    from docx.oxml.ns import qn
    from PIL import Image
    source, dependencies = developer_source(root, state, flow)
    docx, _ = developer_paths(root, state)
    meta = m.metadata(root, state["lab_id"])
    doc = Document()
    section = doc.sections[0]
    section.page_width, section.page_height = Inches(8.27), Inches(11.69)
    section.top_margin = section.bottom_margin = Inches(.75)
    section.left_margin = section.right_margin = Inches(.85)
    doc.styles["Normal"].font.name = "Times New Roman"
    doc.styles["Normal"].font.size = Pt(11)
    for style in ("Title", "Heading 1", "Heading 2", "Caption"):
        doc.styles[style].font.color.rgb = __import__("docx").shared.RGBColor(0, 0, 0)
    title_properties = doc.styles['Title'].element.get_or_add_pPr()
    for border in list(title_properties.findall(qn('w:pBdr'))):
        title_properties.remove(border)
    footer = section.footer.paragraphs[0]
    footer.alignment = 1
    page_field = OxmlElement('w:fldSimple'); page_field.set(qn('w:instr'), 'PAGE')
    footer._p.append(page_field)
    doc.add_paragraph(f"Lab {meta['manual_number']} {meta['manual_title']} Developer Handbook", "Title")
    doc.add_paragraph("Implementation and maintenance guide for the accepted Lab. Verification labels distinguish executed procedures from document checks.")
    doc.add_paragraph("Accepted content: " + source["accepted_content_digest"])
    mapping = {2: "setup", 3: "run", 6: "modify", 7: "test", 8: "release"}
    for i, item in enumerate(source["sections"], 1):
        doc.add_heading(f"{i} {item['title']}", 1)
        for paragraph in item["paragraphs"]:
            doc.add_paragraph(paragraph)
        for procedure in source["procedures"]:
            if procedure["kind"] == mapping.get(i):
                doc.add_heading(procedure["title"], 2)
                label = 'Executed' if procedure['verification'] == 'executed' else 'Document checked only'
                doc.add_paragraph("Verification: " + label + ". " + procedure["observation"])
                for n, step in enumerate(procedure["steps"], 1):
                    doc.add_paragraph(str(n) + ". " + step)
                doc.add_paragraph("Expected result: " + procedure["expected_result"])
        for fig in source["figures"]:
            if fig["section"] == i:
                image_path = m.checked_path(root, fig['path'])
                with Image.open(image_path) as picture:
                    image_width = min(6.3, 6.0 * picture.width / picture.height)
                doc.add_picture(str(image_path), width=Inches(image_width))
                doc.paragraphs[-1].paragraph_format.keep_with_next = True
                doc.add_paragraph("Figure " + fig["id"] + " (" + fig["kind"] + "). " + fig["caption"], "Caption")
    doc.add_heading("Known limitations and deferrals", 1)
    doc.add_paragraph(source["known_limitations"])
    docx.parent.mkdir(parents=True, exist_ok=True)
    temp = docx.with_suffix(".tmp.docx")
    doc.save(temp); temp.replace(docx)
    manifest = {"kind": "developer", "accepted_content_digest": source["accepted_content_digest"],
                "source_sha256": m.sha(base(root, state) / "developer.source.json"), "dependencies": dependencies,
                "docx_path": docx.relative_to(root).as_posix(), "docx_sha256": m.sha(docx)}
    m.write(base(root, state) / "developer-generation.json", manifest)
    return manifest


def developer_generation(root, state, flow):
    source, dependencies = developer_source(root, state, flow)
    manifest = load_json(base(root, state) / "developer-generation.json")
    docx, _ = developer_paths(root, state)
    expected = {"accepted_content_digest": source["accepted_content_digest"], "dependencies": dependencies,
                "source_sha256": m.sha(base(root, state) / "developer.source.json"),
                "docx_path": docx.relative_to(root).as_posix(), "docx_sha256": m.sha(docx)}
    if any(manifest.get(k) != v for k, v in expected.items()):
        raise ValueError("Developer Handbook generation is stale.")
    return manifest


def render_document(root, state, flow, kind):
    """Export the actual DOCX using a private, read-only Word instance, then render all pages."""
    manifest = developer_generation(root, state, flow) if kind == "developer" else master_generation(root, state, flow)
    docx = m.checked_path(root, manifest["docx_path"])
    pdf = docx.with_suffix(".pdf")
    before = m.sha(docx)
    # Word has a shorter path limit than Python. Export exact bytes from a short
    # temporary filename, then copy the PDF into the portable run output path.
    with tempfile.TemporaryDirectory(prefix="emvr-doc-") as temporary:
        short_docx, short_pdf = Path(temporary) / "input.docx", Path(temporary) / "output.pdf"
        shutil.copyfile(docx, short_docx)
        process = subprocess.run(["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File",
                    str(Path(__file__).with_name("export_handbook_pdf.ps1")), "-InputDocx", str(short_docx), "-OutputPdf", str(short_pdf)],
                    capture_output=True, timeout=180)
        if process.returncode:
            raise ValueError("Word PDF export failed: " + process.stderr.decode('utf-8', errors='replace')[-2000:])
        if m.sha(short_docx) != before:
            raise ValueError("The exporter changed its DOCX input; no PDF was accepted.")
        shutil.copyfile(short_pdf, pdf)
    if m.sha(docx) != before:
        raise ValueError("The PDF exporter changed the DOCX. Restore the reviewed document and investigate.")
    from pypdf import PdfReader
    count = len(PdfReader(str(pdf)).pages)
    export = {"generation_sha256": m.sha(base(root, state) / (kind + "-generation.json")),
              "docx_sha256": before, "pdf_path": pdf.relative_to(root).as_posix(),
              "pdf_sha256": m.sha(pdf), "page_count": count}
    m.write(base(root, state) / (kind + "-export.json"), export)
    folder = base(root, state) / (kind + "-renders") / m.sha(pdf)
    folder.mkdir(parents=True, exist_ok=True)
    subprocess.run(["pdftoppm", "-r", "120", "-png", str(pdf), str(folder / "page")], check=True, capture_output=True, timeout=180)
    pages = sorted(folder.glob("page-*.png"), key=lambda p: int(p.stem.split("-")[-1]))
    if count < 1 or len(pages) != count:
        raise ValueError("Render must cover every PDF page, including the entire project master.")
    result = {**export, "export_sha256": m.sha(base(root, state) / (kind + "-export.json")),
              "pages": [{"page": i, "path": p.relative_to(root).as_posix(), "sha256": m.sha(p)} for i, p in enumerate(pages, 1)]}
    m.write(base(root, state) / (kind + "-rendering.json"), result)
    return result


def observed_checks(review, checks):
    if not text(review.get("reviewer")) or not text(review.get("reviewed_at")):
        raise ValueError("Review needs reviewer and timestamp.")
    if datetime.fromisoformat(review["reviewed_at"].replace("Z", "+00:00")).tzinfo is None:
        raise ValueError("Review timestamp needs a timezone.")
    for key in checks:
        check = review.get("content", {}).get(key, {})
        if check.get("passed") is not True:
            raise ValueError("Review is pending or failed: " + key)
        clean_prose(check.get("observation"))


def document_errors(root, state, flow, kind):
    try:
        manifest = developer_generation(root, state, flow) if kind == "developer" else master_generation(root, state, flow)
        folder = base(root, state)
        rendering = load_json(folder / (kind + "-rendering.json"))
        export = load_json(folder / (kind + "-export.json"))
        review = load_json(folder / (kind + "-review.json"))
        if (rendering.get("export_sha256") != m.sha(folder / (kind + "-export.json"))
                or any(rendering.get(key) != value for key, value in export.items())
                or type(export.get("page_count")) is not int or export["page_count"] < 1):
            raise ValueError("Export page count/binding is missing or changed; rerender the full PDF.")
        if rendering.get("generation_sha256") != m.sha(folder / (kind + "-generation.json")) or rendering.get("docx_sha256") != manifest["docx_sha256"]:
            raise ValueError("Export/render evidence is stale.")
        expected_pdf = str(Path(manifest["docx_path"]).with_suffix(".pdf")).replace("\\", "/")
        if rendering.get("pdf_path") != expected_pdf or m.sha(m.checked_path(root, expected_pdf)) != rendering.get("pdf_sha256"):
            raise ValueError("PDF differs from the reviewed export.")
        if review.get("rendering_sha256") != m.sha(folder / (kind + "-rendering.json")):
            raise ValueError("Review is not bound to the latest render.")
        observed_checks(review, DEVELOPER_CHECKS if kind == "developer" else MASTER_CHECKS)
        pages = rendering.get("pages", [])
        numbers = list(range(1, export["page_count"] + 1))
        if not pages or [p["page"] for p in pages] != numbers or [p["page"] for p in review.get("pages", [])] != numbers:
            raise ValueError("Review every rendered page, not just changed project-book pages.")
        for page, reviewed in zip(pages, review["pages"]):
            if m.sha(m.checked_path(root, page["path"], f"BuildEvidence/{state['lab_id']}/handbooks/{kind}-renders")) != page["sha256"]:
                raise ValueError("A reviewed page changed.")
            clean_prose(reviewed.get("observation"))
            if any(reviewed.get(check) is not True for check in m.LAYOUT_CHECKS):
                raise ValueError("A PDF page has an unresolved layout check.")
        return []
    except ERRORS as error:
        return [kind.title() + " Handbook: " + str(error)]


def a_binding(root, state, flow):
    return {"accepted_content_digest": m.accepted_digest(root, state, flow),
            "student_generation_sha256": m.sha(base(root, state) / "generation.json"),
            "developer_generation_sha256": m.sha(base(root, state) / "developer-generation.json"),
            "student_review_sha256": m.sha(base(root, state) / "review.json"),
            "developer_review_sha256": m.sha(base(root, state) / "developer-review.json")}


def eight_a_errors(root, state, flow):
    errors = m.review_errors(root, state, flow) + document_errors(root, state, flow, "developer")
    try:
        review = load_json(base(root, state) / "8a-review.json")
        if review.get("binding") != a_binding(root, state, flow):
            raise ValueError("Cross-document review does not bind the final independent 8A books.")
        observed_checks(review, CROSS_CHECKS)
    except ERRORS as error:
        errors.append("Gate 8A consistency: " + str(error))
    return errors


def require_a(root, state, flow):
    errors = eight_a_errors(root, state, flow)
    if errors:
        raise ValueError("8B requires verified 8A first: " + "; ".join(errors))
    return digest_value(a_binding(root, state, flow)) + ":" + m.sha(base(root, state) / "8a-review.json")


def zip_parts(path):
    with zipfile.ZipFile(path) as archive:
        names = archive.namelist()
        if len(names) != len(set(names)) or "word/document.xml" not in names:
            raise ValueError("Master must be an unambiguous DOCX package.")
        return {name: archive.read(name) for name in names}


def stage_master(root, state, flow, source):
    binding = require_a(root, state, flow)
    source = Path(source).resolve()
    if source.suffix.lower() != ".docx":
        raise ValueError("Supply the explicitly authorized existing master DOCX, not a reconstructed replacement.")
    zip_parts(source)
    digest = m.sha(source)
    target = base(root, state) / "project-master" / "before" / digest / "EM VR Lab Development Handbook.docx"
    target.parent.mkdir(parents=True, exist_ok=True)
    if target.exists() and m.sha(target) != digest:
        raise ValueError("Saved before-version was modified; do not overwrite it.")
    if not target.exists():
        shutil.copyfile(source, target)
    record = {"path": target.relative_to(root).as_posix(), "sha256": digest, "8a_binding": binding,
              "source_label": "User-authorized existing project Development Handbook"}
    m.write(base(root, state) / "master-base.json", record)
    return record


def block_span(xml, lab_id):
    """Only tool-owned, unnested, tagged SDT blocks may be replaced."""
    tree = ET.fromstring(xml)
    tags = [e for e in tree.iter("{" + W + "}tag") if e.get("{" + W + "}val") == "EMVR-LAB:" + lab_id]
    if len(tags) > 1:
        raise ValueError("Duplicate current-Lab sections require review; never delete other sections automatically.")
    if tags:
        body = tree.find("{" + W + "}body")
        owned = [e for e in body if e.tag == "{" + W + "}sdt"
                 and e.find("{" + W + "}sdtPr/{" + W + "}tag") is tags[0]] if body is not None else []
        if len(owned) != 1:
            raise ValueError("Current-Lab section must be a top-level body block; nested ownership is ambiguous.")
    marker = ('<w:tag w:val="EMVR-LAB:' + lab_id + '"/>').encode()
    if xml.count(marker) > 1:
        raise ValueError("Duplicate current-Lab sections require review; never delete other sections automatically.")
    if marker not in xml:
        # Detect another serialization of our tag rather than silently appending.
        if any(e.get("{" + W + "}val") == "EMVR-LAB:" + lab_id for e in ET.fromstring(xml).iter("{" + W + "}tag")):
            raise ValueError("Current-Lab section serialization changed; review its boundaries before updating.")
        return None
    pos = xml.index(marker)
    start = xml.rfind(b"<w:sdt>", 0, pos)
    end = xml.find(b"</w:sdt>", pos)
    if start < 0 or end < 0 or xml[start:end].count(b"<w:sdt>") != 1:
        raise ValueError("Ambiguous current-Lab block boundaries.")
    return start, end + len(b"</w:sdt>")


def incremental_docx(source, destination, lab_id, paragraphs):
    if not re.fullmatch(r"[a-z][a-z0-9_]{2,63}", lab_id):
        raise ValueError("Invalid section identity.")
    if Path(source).resolve() == Path(destination).resolve():
        raise ValueError("Never overwrite the master base; create an incremental output copy.")
    parts = zip_parts(source)
    xml = parts["word/document.xml"]
    if b'xmlns:w="' + W.encode() + b'"' not in xml:
        raise ValueError("Unsupported master namespace serialization; preserve it and request a reviewed adapter.")
    runs = []
    for i, paragraph in enumerate(paragraphs):
        clean_prose(paragraph)
        props = '<w:pPr><w:outlineLvl w:val="0"/></w:pPr>' if i == 0 else ''
        runs.append('<w:p>' + props + '<w:r>' + ('<w:rPr><w:b/></w:rPr>' if i == 0 else '') + '<w:t xml:space="preserve">' + escape(paragraph) + '</w:t></w:r></w:p>')
    block = ('<w:sdt><w:sdtPr><w:tag w:val="EMVR-LAB:' + lab_id + '"/></w:sdtPr><w:sdtContent>' + ''.join(runs) + '</w:sdtContent></w:sdt>').encode('utf-8')
    span = block_span(xml, lab_id)
    if span:
        start, end = span
        updated = xml[:start] + block + xml[end:]
        action = "unchanged" if xml[start:end] == block else "replace_current_lab_section"
        outside = xml[:start] + xml[end:]
    else:
        body_end = xml.rfind(b"</w:body>")
        body = ET.fromstring(xml).find("{" + W + "}body")
        if body_end < 0 or body is None:
            raise ValueError("Master has no document body.")
        insert = xml.rfind(b"<w:sectPr", 0, body_end) if list(body) and list(body)[-1].tag == "{" + W + "}sectPr" else body_end
        if insert < 0:
            raise ValueError("Master section boundary is unsupported.")
        updated = xml[:insert] + block + xml[insert:]
        action, outside = "insert_current_lab_section", xml
    ET.fromstring(updated)
    destination = Path(destination); destination.parent.mkdir(parents=True, exist_ok=True)
    temp = destination.with_suffix(".tmp.docx")
    with zipfile.ZipFile(source) as original, zipfile.ZipFile(temp, 'w') as output:
        for info in original.infolist():
            output.writestr(info, updated if info.filename == "word/document.xml" else parts[info.filename])
    temp.replace(destination)
    verify_increment(source, destination, lab_id)
    return {"section_id": "EMVR-LAB:" + lab_id, "action": action,
            "before_section_sha256": hashlib.sha256(xml[span[0]:span[1]]).hexdigest().upper() if span else None,
            "after_section_sha256": hashlib.sha256(block).hexdigest().upper(),
            "unchanged_body_sha256": hashlib.sha256(outside).hexdigest().upper(),
            "unchanged_package_parts": len(parts) - 1}


def verify_increment(source, destination, lab_id):
    old, new = zip_parts(source), zip_parts(destination)
    if set(old) != set(new) or any(old[k] != new[k] for k in old if k != "word/document.xml"):
        raise ValueError("8B changed a non-body package part, image, style, relationship, or another chapter resource.")
    old_xml, new_xml = old['word/document.xml'], new['word/document.xml']
    previous, current = block_span(old_xml, lab_id), block_span(new_xml, lab_id)
    if not current:
        raise ValueError("8B current-Lab section is missing.")
    stripped_old = old_xml[:previous[0]] + old_xml[previous[1]:] if previous else old_xml
    stripped_new = new_xml[:current[0]] + new_xml[current[1]:]
    if stripped_old != stripped_new:
        raise ValueError("8B changed text outside the designated current-Lab section.")


def master_inputs(root, state, flow):
    binding = require_a(root, state, flow)
    directory = base(root, state)
    source = load_json(directory / "master-base.json")
    source_path = m.checked_path(root, source['path'], f"BuildEvidence/{state['lab_id']}/handbooks/project-master/before")
    if m.sha(source_path) != source.get('sha256') or source.get('8a_binding') != binding:
        raise ValueError("Master before-version or 8A binding changed; stage the reviewed base again.")
    patch = load_json(directory / 'master-addendum.json')
    if patch.get('lab_id') != state['lab_id'] or patch.get('8a_binding') != binding:
        raise ValueError("The 8B addendum must derive from the final verified 8A books.")
    fields = ('introduction', 'entry_points', 'module_relationships', 'unique_dependencies')
    paragraphs = [f"Lab {m.metadata(root,state['lab_id'])['manual_number']} {m.metadata(root,state['lab_id'])['manual_title']}"]
    for key in fields:
        entries = patch.get(key)
        if not isinstance(entries, list) or (key != 'unique_dependencies' and not entries):
            raise ValueError("8B requires scoped addendum entries: " + key)
        for entry in entries:
            clean_prose(entry); paragraphs.append(key.replace('_',' ').title() + ': ' + entry)
    # Resolve each shared citation against the unchanged base, excluding our own
    # section so a self-reference cannot masquerade as an existing shared guide.
    xml = zip_parts(source_path)['word/document.xml']
    span = block_span(xml, state['lab_id'])
    if span:
        xml = xml[:span[0]] + xml[span[1]:]
    base_paragraphs = {''.join(t.text or '' for t in p.iter('{' + W + '}t')).strip()
                       for p in ET.fromstring(xml).iter('{' + W + '}p')}
    refs = patch.get('shared_references')
    if not isinstance(refs, list) or not refs:
        raise ValueError('8B requires resolved shared_references to the saved master.')
    for ref in refs:
        if not isinstance(ref, dict):
            raise ValueError('Shared references require text and target_text from the saved master.')
        clean_prose(ref.get('text')); clean_prose(ref.get('target_text'))
        if ref['target_text'].strip() not in base_paragraphs:
            raise ValueError('Shared reference target is missing from the saved master: ' + ref['target_text'])
        paragraphs.append('Shared reference: ' + ref['text'] + ' [Master: ' + ref['target_text'] + ']')
    slots = m.slots(state['lab_id'], m.metadata(root,state['lab_id']))
    paragraphs += ['Student manual: ' + Path(slots['lab_manual']['path']).name,
                   'Developer handbook: ' + Path(slots['developers_handbook']['path']).name,
                   'Accepted content: ' + m.accepted_digest(root,state,flow)]
    if not isinstance(patch.get('existing_issues'), list):
        raise ValueError("List pre-existing master issues separately; never fix them implicitly.")
    return source_path, patch, paragraphs, binding


def build_master(root, state, flow):
    source, patch, paragraphs, binding = master_inputs(root,state,flow)
    destination = base(root,state) / 'project-master' / 'EM VR Lab Development Handbook.docx'
    changes = incremental_docx(source,destination,state['lab_id'],paragraphs)
    manifest = {'kind':'master','accepted_content_digest':m.accepted_digest(root,state,flow),'8a_binding':binding,
                'base_sha256':m.sha(source),'patch_sha256':m.sha(base(root,state)/'master-addendum.json'),
                'docx_path':destination.relative_to(root).as_posix(),'docx_sha256':m.sha(destination),
                'changes':changes,'existing_issues_not_modified':patch['existing_issues']}
    m.write(base(root,state)/'master-generation.json',manifest)
    m.write(base(root,state)/'master-change-list.json',{'8a_binding':binding,**changes,'existing_issues_not_modified':patch['existing_issues']})
    return manifest


def master_generation(root,state,flow):
    source, patch, paragraphs, binding = master_inputs(root,state,flow)
    manifest=load_json(base(root,state)/'master-generation.json')
    destination=base(root,state)/'project-master'/'EM VR Lab Development Handbook.docx'
    if (manifest.get('8a_binding')!=binding or manifest.get('base_sha256')!=m.sha(source)
        or manifest.get('patch_sha256')!=m.sha(base(root,state)/'master-addendum.json')
        or manifest.get('docx_path')!=destination.relative_to(root).as_posix() or manifest.get('docx_sha256')!=m.sha(destination)):
        raise ValueError('Project handbook generation is stale.')
    verify_increment(source,destination,state['lab_id'])
    expected={'8a_binding':binding,**manifest['changes'],'existing_issues_not_modified':patch['existing_issues']}
    if load_json(base(root,state)/'master-change-list.json')!=expected:
        raise ValueError('8B increment change list is missing or changed.')
    return manifest


def gate_errors(root,state,flow):
    errors=eight_a_errors(root,state,flow)
    if errors:
        return errors + ['Gate 8B waits for final, verified Gate 8A; no separate Gate number or approval.']
    errors = document_errors(root,state,flow,'master')
    if not errors and flow.gate_is_approved(state, 8) and state['gate_approvals']['8'].get('handbooks_digest') != approval_binding(root,state):
        errors.append('Gate 8 was approved for another handbook bundle. Reopen handbooks and obtain renewed user confirmation.')
    return errors


def approval_binding(root,state):
    names=('generation.json','review.json','developer-generation.json','developer-review.json',
           '8a-review.json','master-generation.json','master-rendering.json','master-review.json','master-change-list.json')
    return digest_value({name:m.sha(base(root,state)/name) for name in names})


def gate_step(root,state,flow):
    if eight_a_errors(root,state,flow):return '8A'
    if document_errors(root,state,flow,'master'):return '8B'
    return 'awaiting_joint_confirmation'


def prepare_review(root, state, flow, kind):
    """Fill mechanical bindings only. Every observation and verdict stays pending."""
    folder = base(root, state)
    name = 'review.json' if kind == 'student' else kind + '-review.json'
    path = folder / name
    if path.exists():
        raise ValueError('Review already exists; preserve it and explicitly archive it before preparing a replacement.')
    review = {'reviewer': '', 'reviewed_at': ''}
    if kind == '8a':
        errors = m.review_errors(root, state, flow) + document_errors(root, state, flow, 'developer')
        if errors:
            raise ValueError('Review both independent books first: ' + '; '.join(errors))
        review['binding'] = a_binding(root, state, flow)
        checks, count = CROSS_CHECKS, 0
    else:
        prefix = '' if kind == 'student' else kind + '-'
        manifest = (m.generation(root, state, flow) if kind == 'student' else
                    developer_generation(root, state, flow) if kind == 'developer' else master_generation(root, state, flow))
        rendering = load_json(folder / (prefix + 'rendering.json'))
        generation_sha = m.sha(folder / (prefix + 'generation.json'))
        if rendering.get('generation_sha256') != generation_sha:
            raise ValueError('Rerender the current generation before preparing its review.')
        if kind == 'student':
            count = manifest['page_count']; checks = m.CONTENT_CHECKS
            review['generation_sha256'] = generation_sha
        else:
            export = load_json(folder / (kind + '-export.json'))
            if (rendering.get('export_sha256') != m.sha(folder / (kind + '-export.json'))
                    or any(rendering.get(key) != value for key, value in export.items())):
                raise ValueError('Export/render binding changed; rerender before preparing review.')
            count = export.get('page_count'); checks = DEVELOPER_CHECKS if kind == 'developer' else MASTER_CHECKS
        if type(count) is not int or count < 1 or [p.get('page') for p in rendering.get('pages', [])] != list(range(1, count + 1)):
            raise ValueError('Render all PDF pages before preparing review.')
        review['rendering_sha256'] = m.sha(folder / (prefix + 'rendering.json'))
        review['pages'] = [{'page': i, 'observation': '', **{key: False for key in m.LAYOUT_CHECKS}} for i in range(1, count + 1)]
    review['content'] = {key: {'passed': False, 'observation': ''} for key in checks}
    m.write(path, review)
    return review


def init_master_addendum(root, state, flow):
    binding = require_a(root, state, flow)
    folder = base(root, state)
    record = load_json(folder / 'master-base.json')
    source = m.checked_path(root, record['path'], f"BuildEvidence/{state['lab_id']}/handbooks/project-master/before")
    if record.get('8a_binding') != binding or record.get('sha256') != m.sha(source):
        raise ValueError('Stage the reviewed master against the final 8A first.')
    path = folder / 'master-addendum.json'
    if path.exists():
        raise ValueError('Addendum already exists; preserve and revise its current content.')
    value = load_json(root / 'LabSpecs/templates/project-handbook-addendum.template.json')
    value.update(lab_id=state['lab_id'], **{'8a_binding': binding})
    m.write(path, value)
    return value


def main():
    import labflow as flow
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project-root',type=Path,default=Path(__file__).resolve().parents[2])
    parser.add_argument('action',choices=('collect','init-developer','build-8a','render-developer','prepare-review','check-8a','stage-master','init-master-addendum','build-8b','render-master','check'))
    parser.add_argument('--lab-id',required=True)
    parser.add_argument('--kind', choices=('student', 'developer', '8a', 'master'), help='Review to prepare; all checks remain pending.')
    parser.add_argument('--source',type=Path,help='Explicitly authorized master DOCX, read only; never persisted as an absolute path.')
    args=parser.parse_args();root=args.project_root.resolve()
    try:
        lab=flow.require_lab_id(args.lab_id);state=flow.load_current_state(root,lab)
        errors=flow.protected_baseline_errors(root,state)+flow.source_reference_errors(root,state)
        if errors: raise ValueError('; '.join(errors))
        if args.action=='collect':
            collect(root,state,flow)
        else:
            if flow.current_gate(state)!=8 or state.get('blocker_recovery',{}).get('active'):
                raise ValueError('Handbook execution belongs to original Gate 8 after Gate 6 acceptance, without an active blocker.')
            if flow.gate_is_approved(state,8) and args.action not in ('check','check-8a'):
                raise ValueError('Reopen handbooks before revising an approved Gate 8 bundle.')
            accepted=m.accepted_digest(root,state,flow)
            if args.action=='init-developer':
                path=base(root,state)/'developer.source.json'
                if path.exists():raise ValueError('Developer source exists; revise it without overwriting its work.')
                value=load_json(root/'LabSpecs/templates/developer-handbook.template.json')
                value.update(lab_id=lab,accepted_content_digest=accepted);m.write(path,value)
            elif args.action=='build-8a':
                collect(root,state,flow)
                developer_source(root,state,flow)
                m.build(root,state,flow);build_developer(root,state,flow)
            elif args.action=='render-developer':render_document(root,state,flow,'developer')
            elif args.action=='prepare-review':
                if args.kind is None:raise ValueError('Specify --kind student, developer, 8a or master.')
                prepare_review(root,state,flow,args.kind)
            elif args.action=='stage-master':
                if args.source is None:raise ValueError('Supply the authorized existing master DOCX with --source.')
                stage_master(root,state,flow,args.source)
            elif args.action=='init-master-addendum':init_master_addendum(root,state,flow)
            elif args.action=='build-8b':build_master(root,state,flow)
            elif args.action=='render-master':render_document(root,state,flow,'master')
            else:
                errors=eight_a_errors(root,state,flow) if args.action=='check-8a' else gate_errors(root,state,flow)
                if errors:raise ValueError('; '.join(errors))
        print('Handbook '+args.action+' completed. No Gate approval, Unity action or host publication recorded.')
        return 0
    except ERRORS + (ImportError,subprocess.SubprocessError) as error:
        print('ERROR: '+str(error));return 1


if __name__=='__main__':
    raise SystemExit(main())
