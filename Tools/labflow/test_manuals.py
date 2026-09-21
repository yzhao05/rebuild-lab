import argparse
import copy
import json
from pathlib import Path
import tempfile
import unittest
from unittest import mock

import labflow
import manuals


class StudentManualTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.lab = "synthetic_lab_0"
        self.meta = dict(manual_number=2, manual_title="Magnetostatic", language="English",
                         report_submission_requirements="Submit observations and reasoning.",
                         scoring_policy="ungraded", metadata_confirmed=True)
        self.brief = self.root / "LabSpecs" / self.lab / "brief.yaml"
        self.brief.parent.mkdir(parents=True)
        self.write_meta()
        self.question = dict(id="Q1", prompt="Describe the observed response.", criterion_ids=["C1"], points=None,
                             bonus=False, submission="Written observation.")
        self.rubric = self.root / "LabInterfaceContracts" / (self.lab + "_rubric.json")
        manuals.write(self.rubric, {"rubrics": [{"step_id": "S1", "criteria": [{"criterion_id": "C1"}]}],
                      "manual_assessment": {"policy": self.meta["scoring_policy"],
                                            "submission_requirements": self.meta["report_submission_requirements"],
                                            "questions": [self.question]}})
        self.state = {"lab_id": self.lab, "gate_approvals": {}}
        for gate in labflow.GATE_SEQUENCE[:-1]:
            labflow.record_gate_approval(self.state, gate, "Confirmed synthetic fixture")
        self.digest = manuals.content_digest(self.root, self.lab, labflow.gate5_lab_output_digest(self.root, self.lab))
        self.state["user_acceptance"]["content_digest"] = self.digest
        image = self.root / f"BuildEvidence/{self.lab}/screenshots/entry.png"
        image.parent.mkdir(parents=True)
        image.write_bytes(b"synthetic screenshot bytes for integrity tests, not an image")
        self.source = {"lab_id": self.lab, "accepted_content_digest": self.digest,
                       "sections": [{"title": title, "paragraphs": ["Synthetic student observation."],
                                     "source_refs": [self.brief.relative_to(self.root).as_posix()],
                                     "figure_ids": ["entry"] if i == 3 else []}
                                    for i, title in enumerate(manuals.SECTIONS)],
                       "figures": [{"id": "entry", "path": image.relative_to(self.root).as_posix(),
                                    "caption": "Synthetic entry", "kind": "runtime", "accepted_content_digest": self.digest,
                                    "capture_note": "Synthetic integrity fixture only."}], "questions": [self.question]}
        manuals.write(manuals.source_path(self.root, self.lab), self.source)

    def write_meta(self):
        self.brief.write_text("manual:\n" + "".join(f"  {k}: {json.dumps(v)}\n" for k, v in self.meta.items()), encoding="utf-8")

    def validate(self):
        return manuals.validate_source(self.root, self.state, labflow, self.source)

    def ready_evidence(self, pages=2):
        meta, dependencies = self.validate()
        base = manuals.base_path(self.root, self.lab)
        slots = manuals.slots(self.lab, meta)
        pdf = self.root / slots["lab_manual"]["path"]
        pdf.parent.mkdir(parents=True)
        pdf.write_bytes(b"synthetic PDF integrity fixture")
        manuals.write(base / "generation.json", {"lab_id": self.lab, "accepted_content_digest": self.digest,
                      "source_sha256": manuals.sha(manuals.source_path(self.root, self.lab)),
                      "dependencies": dependencies, "deliverables": slots, "pdf_sha256": manuals.sha(pdf), "page_count": pages})
        rendered = []
        for i in range(1, pages + 1):
            path = base / "renders" / f"page-{i}.png"
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(b"synthetic page " + str(i).encode())
            rendered.append({"page": i, "path": path.relative_to(self.root).as_posix(), "sha256": manuals.sha(path)})
        manuals.write(base / "rendering.json", {"generation_sha256": manuals.sha(base / "generation.json"), "pages": rendered})
        review = {"generation_sha256": manuals.sha(base / "generation.json"), "rendering_sha256": manuals.sha(base / "rendering.json"),
                  "reviewer": "Synthetic test reviewer", "reviewed_at": "2026-09-22T00:00:00Z",
                  "content": {k: {"passed": True, "observation": "Synthetic observation for integrity test."} for k in manuals.CONTENT_CHECKS},
                  "pages": [{"page": i, "observation": "Synthetic page reviewed.", **{k: True for k in manuals.LAYOUT_CHECKS}}
                            for i in range(1, pages + 1)]}
        manuals.write(base / "review.json", review)
        return base, pdf, review

    def test_explicit_course_number_and_historical_name_preserved(self):
        meta = manuals.metadata(self.root, self.lab)
        slots = manuals.slots(self.lab, meta)
        self.assertTrue(slots["lab_manual"]["path"].endswith("Lab 2 Magnetostatic.pdf"))
        self.assertEqual(slots["developers_handbook"]["status"], "required_8a")
        self.assertFalse((self.root / slots["developers_handbook"]["path"]).exists())

    def test_metadata_not_inferred_from_lab_id(self):
        for value in (None, True, -1, "2"):
            self.meta["manual_number"] = value
            self.write_meta()
            self.assertTrue(manuals.metadata_errors(self.root, self.lab))
        self.meta["manual_number"] = 0
        self.write_meta()
        self.assertFalse(manuals.metadata_errors(self.root, self.lab))

    def test_missing_unconfirmed_and_unsafe_metadata_rejected(self):
        original = copy.deepcopy(self.meta)
        for key, value in (("manual_title", "../wrong"), ("language", ""), ("metadata_confirmed", False), ("scoring_policy", "TBD")):
            self.meta = dict(original, **{key: value}); self.write_meta()
            self.assertTrue(manuals.metadata_errors(self.root, self.lab))

    def test_no_gate_six_acceptance_no_manual(self):
        self.state["gate_approvals"].pop("6")
        with self.assertRaisesRegex(ValueError, "Gate 6"):
            self.validate()

    def test_legacy_acceptance_without_snapshot_rejected(self):
        self.state["user_acceptance"].pop("content_digest")
        with self.assertRaisesRegex(ValueError, "snapshot"):
            self.validate()

    def test_changed_accepted_content_requires_reacceptance(self):
        self.brief.write_text(self.brief.read_text() + "\nchanged: true\n", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "content changed"):
            self.validate()

    def test_changed_common_dependency_invalidates_acceptance(self):
        path = self.root / "UnityProject/LocalPackages/com.emvr.lab-common/Runtime/Interaction.cs"
        path.parent.mkdir(parents=True)
        path.write_text("changed protected dependency", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "content changed"):
            self.validate()

    def test_fixed_six_sections(self):
        self.source["sections"].pop()
        with self.assertRaisesRegex(ValueError, "six fixed"):
            self.validate()

    def test_unity_guid_and_project_configuration_changes_invalidate_acceptance(self):
        for relative in ('UnityProject/Assets/Scenes/current.unity.meta',
                         'UnityProject/Packages/manifest.json',
                         'UnityProject/ProjectSettings/ProjectVersion.txt'):
            with self.subTest(relative=relative):
                path = self.root / relative
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text('Changed GUID or runtime configuration', encoding='utf-8')
                with self.assertRaisesRegex(ValueError, 'content changed'):
                    self.validate()
                path.unlink()

    def test_old_lab_evidence_reference_rejected(self):
        self.source["sections"][0]["source_refs"] = ["LabSpecs/old_lab/brief.yaml"]
        with self.assertRaisesRegex(ValueError, "current accepted"):
            self.validate()

    def test_unsafe_paths_rejected(self):
        for path in ("../outside", "C:/outside", "folder\\file", "/absolute", "BuildEvidence/../outside"):
            with self.assertRaises(ValueError):
                manuals.checked_path(self.root, path)

    def test_screenshot_requires_current_capture_provenance(self):
        self.source["figures"][0]["accepted_content_digest"] = "OLD"
        with self.assertRaisesRegex(ValueError, "screenshots require"):
            self.validate()

    def test_theory_diagram_cannot_replace_runtime_screenshot(self):
        self.source["figures"][0]["kind"] = "theory"
        with self.assertRaisesRegex(ValueError, "Collect actual"):
            self.validate()

    def test_tour_must_place_runtime_screenshot(self):
        self.source["sections"][3]["figure_ids"] = []
        self.source["sections"][1]["figure_ids"] = ["entry"]
        with self.assertRaisesRegex(ValueError, "Guided Tours"):
            self.validate()

    def test_questions_cannot_invent_points_or_submissions(self):
        self.source["questions"] = [dict(self.question, points=100)]
        with self.assertRaisesRegex(ValueError, "exactly match"):
            self.validate()

    def test_answer_key_and_placeholder_blocked(self):
        self.source["answer_key"] = "Hidden solution"
        with self.assertRaisesRegex(ValueError, "answer keys"):
            self.validate()
        del self.source["answer_key"]
        self.source["sections"][0]["paragraphs"] = ["TODO"]
        with self.assertRaisesRegex(ValueError, "placeholders"):
            self.validate()

    def test_generation_alone_cannot_pass(self):
        base, _, _ = self.ready_evidence()
        (base / "review.json").unlink()
        self.assertTrue(manuals.review_errors(self.root, self.state, labflow))

    def test_every_page_requires_explicit_review(self):
        base, _, review = self.ready_evidence()
        review["pages"].pop()
        manuals.write(base / "review.json", review)
        self.assertIn("every rendered page", str(manuals.review_errors(self.root, self.state, labflow)))

    def test_blank_observation_and_failed_layout_cannot_pass(self):
        base, _, review = self.ready_evidence()
        review["content"][manuals.CONTENT_CHECKS[0]]["observation"] = ""
        manuals.write(base / "review.json", review)
        self.assertTrue(manuals.review_errors(self.root, self.state, labflow))
        review["content"][manuals.CONTENT_CHECKS[0]]["observation"] = "Checked"
        review["pages"][0]["no_overflow"] = False
        manuals.write(base / "review.json", review)
        self.assertIn("layout defects", str(manuals.review_errors(self.root, self.state, labflow)))

    def test_null_observations_and_invalid_timestamp_cannot_pass(self):
        base, _, review = self.ready_evidence()
        review["content"][manuals.CONTENT_CHECKS[0]]["observation"] = None
        manuals.write(base / "review.json", review)
        self.assertTrue(manuals.review_errors(self.root, self.state, labflow))
        review["content"][manuals.CONTENT_CHECKS[0]]["observation"] = "Checked"
        review["reviewed_at"] = "2026-09-22"
        manuals.write(base / "review.json", review)
        self.assertTrue(manuals.review_errors(self.root, self.state, labflow))

    def test_pdf_or_render_tampering_invalidates_review(self):
        base, pdf, _ = self.ready_evidence()
        self.assertEqual([], manuals.review_errors(self.root, self.state, labflow))
        original = pdf.read_bytes(); pdf.write_bytes(b"changed")
        self.assertTrue(manuals.review_errors(self.root, self.state, labflow))
        pdf.write_bytes(original)
        (base / "renders/page-1.png").write_bytes(b"changed")
        self.assertIn("rendered page changed", str(manuals.review_errors(self.root, self.state, labflow)))

    def test_screenshot_or_source_change_invalidates_generation(self):
        self.ready_evidence()
        image = self.root / self.source["figures"][0]["path"]
        image.write_bytes(b"new screenshot")
        self.assertIn("stale", str(manuals.review_errors(self.root, self.state, labflow)))

    def test_student_pass_does_not_claim_developer_delivery(self):
        self.ready_evidence()
        before = copy.deepcopy(self.state)
        self.assertEqual([], manuals.review_errors(self.root, self.state, labflow))
        self.assertIn("Developer", str(manuals.gate_errors(self.root, self.state, labflow)))
        self.assertEqual(before, self.state)

    def test_old_archive_approval_cannot_finish_new_gate(self):
        labflow.record_gate_approval(self.state, 8, "Old archive")
        del self.state["gate_approvals"]["8"]["manual_model"]
        self.assertFalse(labflow.gate_is_approved(self.state, 8))

    def test_reopening_preserves_old_evidence_and_requires_new_acceptance(self):
        self.state["evidence"] = dict(labflow.default_evidence(), compile="passed", scene_built="passed")
        with mock.patch.object(labflow, "load_current_state", return_value=self.state), \
             mock.patch.object(labflow, "protected_baseline_errors", return_value=[]), \
             mock.patch.object(labflow, "source_reference_errors", return_value=[]), \
             mock.patch.object(labflow, "save_state") as save:
            labflow.command_reopen_acceptance(argparse.Namespace(project_root=self.root, lab_id=self.lab, note="Interaction changed"))
        self.assertEqual(labflow.current_gate(self.state), 6)
        self.assertNotIn("user_acceptance", self.state)
        self.assertEqual(self.state["evidence"]["compile"], "pending")
        self.assertEqual(self.state["superseded_scene_evidence"][0]["evidence"]["compile"], "passed")
        self.assertEqual(self.state["superseded_user_acceptance"][0]["content_digest"], self.digest)
        save.assert_called_once()

    def test_unknown_contract_criterion_and_implicit_grading_rejected(self):
        rubric = manuals.read(self.rubric)
        rubric["manual_assessment"]["questions"][0]["criterion_ids"] = ["unknown"]
        manuals.write(self.rubric, rubric)
        with self.assertRaisesRegex(ValueError, "existing rubric"):
            manuals.assessment(self.root, self.lab, self.meta)
        rubric["manual_assessment"]["questions"][0]["criterion_ids"] = ["C1"]
        del rubric["manual_assessment"]["questions"][0]["points"]
        manuals.write(self.rubric, rubric)
        with self.assertRaisesRegex(ValueError, "explicitly declares"):
            manuals.assessment(self.root, self.lab, self.meta)


if __name__ == "__main__":
    unittest.main()
