import copy
import argparse
import contextlib
import io
import json
import tempfile
import unittest
import zipfile
from pathlib import Path
from unittest import mock

import handbooks as h
import labflow
import manuals as m
import test_manuals as fixtures


def make_master(path):
    path.parent.mkdir(parents=True, exist_ok=True)
    xml = ('<w:document xmlns:w="' + h.W + '"><w:body><w:p><w:r><w:t>Existing general setup and other Lab</w:t>'
           '</w:r></w:p><w:sectPr><w:pgSz w:w="11906" w:h="16838"/></w:sectPr></w:body></w:document>').encode()
    with zipfile.ZipFile(path, 'w') as z:
        z.writestr('word/document.xml', xml)
        z.writestr('word/media/old-image.png', b'original image bytes')
        z.writestr('word/styles.xml', b'original styles')
        z.writestr('word/_rels/document.xml.rels', b'original relationships')


class IncrementTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(); self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.original = self.root / 'base.docx'; make_master(self.original)
        self.lab = 'synthetic_lab_0'

    def test_increment_preserves_every_other_part_and_original_bytes(self):
        before = self.original.read_bytes(); dest = self.root / 'out.docx'
        result = h.incremental_docx(self.original, dest, self.lab, ['Current Lab', 'See the existing XR section.'])
        self.assertEqual(before, self.original.read_bytes())
        self.assertEqual(result['action'], 'insert_current_lab_section')
        self.assertEqual(result['unchanged_package_parts'], 3)
        h.verify_increment(self.original, dest, self.lab)

    def test_repeat_is_idempotent_and_preserves_other_lab(self):
        first, second, third = (self.root / name for name in ('first.docx','second.docx','third.docx'))
        h.incremental_docx(self.original, first, self.lab, ['Current Lab', 'One entry'])
        result = h.incremental_docx(first, second, self.lab, ['Current Lab', 'One entry'])
        self.assertEqual(result['action'], 'unchanged')
        self.assertEqual(first.read_bytes(), second.read_bytes())
        h.incremental_docx(second, third, self.lab, ['Current Lab', 'Updated entry'])
        xml=h.zip_parts(third)['word/document.xml']
        self.assertEqual(xml.count(b'EMVR-LAB:synthetic_lab_0'),1)
        self.assertIn(b'Existing general setup and other Lab',xml)

    def test_second_lab_section_is_unchanged_when_updating_first(self):
        one, two, three=(self.root/name for name in ('one.docx','two.docx','three.docx'))
        h.incremental_docx(self.original,one,'lab_one',['Lab One'])
        h.incremental_docx(one,two,'lab_two',['Lab Two'])
        h.incremental_docx(two,three,'lab_one',['Lab One updated'])
        h.verify_increment(two,three,'lab_one')
        self.assertIn(b'Lab Two',h.zip_parts(three)['word/document.xml'])

    def test_editing_other_chapter_or_image_is_rejected(self):
        dest=self.root/'out.docx';h.incremental_docx(self.original,dest,self.lab,['Current Lab'])
        for name in ('word/document.xml','word/media/old-image.png'):
            parts=h.zip_parts(dest);parts[name]=parts[name].replace(b'Existing general',b'Changed general') if name.endswith('.xml') else b'changed'
            bad=self.root/'bad.docx'
            with zipfile.ZipFile(bad,'w') as z:
                for key,value in parts.items():z.writestr(key,value)
            with self.assertRaises(ValueError):h.verify_increment(self.original,bad,self.lab)

    def test_in_place_write_and_duplicate_tag_rejected(self):
        with self.assertRaisesRegex(ValueError,'Never overwrite'):
            h.incremental_docx(self.original,self.original,self.lab,['Current Lab'])
        dest=self.root/'out.docx';h.incremental_docx(self.original,dest,self.lab,['Current Lab'])
        xml=h.zip_parts(dest)['word/document.xml'];start,end=h.block_span(xml,self.lab)
        xml=xml[:end]+xml[start:end]+xml[end:]
        with self.assertRaisesRegex(ValueError,'Duplicate'):h.block_span(xml,self.lab)

    def test_reformatted_tag_fails_instead_of_appending_duplicate(self):
        dest=self.root/'out.docx';h.incremental_docx(self.original,dest,self.lab,['Current Lab'])
        xml=h.zip_parts(dest)['word/document.xml'].replace(b'EMVR-LAB:synthetic_lab_0"/>',b'EMVR-LAB:synthetic_lab_0" />')
        with self.assertRaisesRegex(ValueError,'serialization changed'):h.block_span(xml,self.lab)

    def test_prose_is_escaped_without_injecting_xml(self):
        dest=self.root/'out.docx';h.incremental_docx(self.original,dest,self.lab,['Current Lab','Use A < B & C.'])
        self.assertIn(b'A &lt; B &amp; C',h.zip_parts(dest)['word/document.xml'])

    def test_nested_owned_block_is_rejected(self):
        dest=self.root/'out.docx';h.incremental_docx(self.original,dest,self.lab,['Current Lab'])
        xml=h.zip_parts(dest)['word/document.xml'];start,end=h.block_span(xml,self.lab)
        nested=xml[:start]+b'<w:sdt><w:sdtContent>'+xml[start:end]+b'</w:sdtContent></w:sdt>'+xml[end:]
        with self.assertRaisesRegex(ValueError,'top-level'):h.block_span(nested,self.lab)

    def test_semantic_duplicate_with_different_serialization_is_rejected(self):
        dest=self.root/'out.docx';h.incremental_docx(self.original,dest,self.lab,['Current Lab'])
        xml=h.zip_parts(dest)['word/document.xml'];start,end=h.block_span(xml,self.lab)
        duplicate=xml[:end]+xml[start:end].replace(b'"/>',b'" />')+xml[end:]
        with self.assertRaisesRegex(ValueError,'Duplicate'):h.block_span(duplicate,self.lab)


class HandbookWorkflowTests(unittest.TestCase):
    def setUp(self):
        self.fixture=fixtures.StudentManualTests();self.fixture.setUp();self.addCleanup(self.fixture.temp.cleanup)
        self.root,self.state,self.lab=self.fixture.root,self.fixture.state,self.fixture.lab
        self.state['schema_version']=labflow.CURRENT_STATE_SCHEMA
        self.directory=m.base_path(self.root,self.lab)
        self.source={'lab_id':self.lab,'accepted_content_digest':self.fixture.digest,
            'sections':[{'title':title,'paragraphs':['Synthetic implementation explanation.'],
                         'source_refs':[self.fixture.brief.relative_to(self.root).as_posix()]} for title in h.DEVELOPER_SECTIONS],
            'procedures':[{'kind':kind,'title':'Synthetic '+kind,'steps':['Inspect current implementation.'],
                'expected_result':'The declared entry is present.','verification':'document_checked',
                'observation':'Inspected synthetic source.','evidence_refs':[self.fixture.brief.relative_to(self.root).as_posix()]}
                for kind in ('setup','run','modify','test','release')],
            'figures':[{'id':'architecture','kind':'architecture','section':3,'path':self.fixture.source['figures'][0]['path'],
                       'caption':'Synthetic diagram','accepted_content_digest':self.fixture.digest,'provenance':'Synthetic unit fixture'}],
            'known_limitations':'Only synthetic verification is represented.'}
        m.write(self.directory/'developer.source.json',self.source)

    def developer_ready(self):
        self.fixture.ready_evidence()
        source,deps=h.developer_source(self.root,self.state,labflow)
        docx,pdf=h.developer_paths(self.root,self.state);docx.parent.mkdir(parents=True,exist_ok=True)
        make_master(docx);pdf.write_bytes(b'synthetic PDF integrity fixture')
        manifest={'kind':'developer','accepted_content_digest':self.fixture.digest,'source_sha256':m.sha(self.directory/'developer.source.json'),
            'dependencies':deps,'docx_path':docx.relative_to(self.root).as_posix(),'docx_sha256':m.sha(docx)}
        m.write(self.directory/'developer-generation.json',manifest)
        self.document_review('developer',docx,pdf,h.DEVELOPER_CHECKS)
        m.write(self.directory/'8a-review.json',{'reviewer':'Synthetic reviewer','reviewed_at':'2026-09-22T00:00:00Z',
            'binding':h.a_binding(self.root,self.state,labflow),'content':{k:{'passed':True,'observation':'Synthetic consistency observation.'} for k in h.CROSS_CHECKS}})

    def document_review(self,kind,docx,pdf,checks):
        pages=[]
        for i in (1,2):
            page=self.directory/(kind+'-renders')/('page-'+str(i)+'.png');page.parent.mkdir(exist_ok=True,parents=True);page.write_bytes(b'synthetic page')
            pages.append({'page':i,'path':page.relative_to(self.root).as_posix(),'sha256':m.sha(page)})
        export={'generation_sha256':m.sha(self.directory/(kind+'-generation.json')),'docx_sha256':m.sha(docx),
            'pdf_path':pdf.relative_to(self.root).as_posix(),'pdf_sha256':m.sha(pdf),'page_count':2}
        m.write(self.directory/(kind+'-export.json'),export)
        rendering={**export,'export_sha256':m.sha(self.directory/(kind+'-export.json')),'pages':pages}
        m.write(self.directory/(kind+'-rendering.json'),rendering)
        m.write(self.directory/(kind+'-review.json'),{'reviewer':'Synthetic reviewer','reviewed_at':'2026-09-22T00:00:00Z',
            'rendering_sha256':m.sha(self.directory/(kind+'-rendering.json')),
            'content':{k:{'passed':True,'observation':'Synthetic evidence observation.'} for k in checks},
            'pages':[{'page':i,'observation':'Synthetic page observation.',**{k:True for k in m.LAYOUT_CHECKS}} for i in (1,2)]})

    def master_ready(self):
        self.developer_ready()
        original=self.root/'authorized-existing-master.docx';make_master(original)
        h.stage_master(self.root,self.state,labflow,original)
        patch={'lab_id':self.lab,'8a_binding':h.require_a(self.root,self.state,labflow),'introduction':['Synthetic Lab introduction.'],
               'entry_points':['Use current Editor entry.'],'module_relationships':['Controller calls model.'],
               'unique_dependencies':[],'shared_references':[{'text':'See existing general setup.','target_text':'Existing general setup and other Lab'}],
               'existing_issues':['An old paragraph needs separate review.']}
        m.write(self.directory/'master-addendum.json',patch)
        manifest=h.build_master(self.root,self.state,labflow)
        docx=self.root/manifest['docx_path'];pdf=docx.with_suffix('.pdf');pdf.write_bytes(b'synthetic master PDF')
        self.document_review('master',docx,pdf,h.MASTER_CHECKS)
        return docx

    def test_names_are_separate_and_use_confirmed_number(self):
        paths=m.slots(self.lab,self.fixture.meta)
        self.assertTrue(paths['developers_handbook']['path'].endswith('Lab 2 Magnetostatic Developer Handbook.pdf'))
        self.assertTrue(paths['project_handbook']['docx_path'].endswith('EM VR Lab Development Handbook.docx'))

    def test_collection_accumulates_without_approval_and_is_idempotent(self):
        before=copy.deepcopy(self.state)
        h.collect(self.root,self.state,labflow);h.collect(self.root,self.state,labflow)
        self.assertEqual(len(list((self.root/f'BuildEvidence/{self.lab}/developer-notes/snapshots').glob('*.json'))),1)
        self.assertEqual(self.state,before)

    def test_old_handbook_is_not_an_implementation_source(self):
        self.source['sections'][0]['source_refs']=['Docs/Handbook/old.docx'];m.write(self.directory/'developer.source.json',self.source)
        with self.assertRaisesRegex(ValueError,'not old handbook'):h.developer_source(self.root,self.state,labflow)

    def test_executed_claim_cannot_be_just_code_review(self):
        self.source['procedures'][0]['verification']='executed';m.write(self.directory/'developer.source.json',self.source)
        with self.assertRaisesRegex(ValueError,'observed current-Lab evidence'):h.developer_source(self.root,self.state,labflow)

    def test_pending_verification_blocks_delivery(self):
        self.source['procedures'][0]['verification']='pending';m.write(self.directory/'developer.source.json',self.source)
        with self.assertRaisesRegex(ValueError,'pending'):h.developer_source(self.root,self.state,labflow)

    def test_private_paths_are_not_published(self):
        for value in ('Open C:/Users/me/private/file','Open /tmp/secret','TODO'):
            with self.assertRaises(ValueError):h.clean_prose(value)

    def test_architecture_and_version_required(self):
        self.source['figures']=[];m.write(self.directory/'developer.source.json',self.source)
        with self.assertRaisesRegex(ValueError,'architecture'):h.developer_source(self.root,self.state,labflow)

    def test_8b_cannot_stage_before_8a(self):
        original=self.root/'authorized-existing-master.docx';make_master(original)
        with self.assertRaisesRegex(ValueError,'8B requires verified 8A'):h.stage_master(self.root,self.state,labflow,original)
        self.assertFalse((self.directory/'master-base.json').exists())

    def test_8a_needs_cross_document_review(self):
        self.developer_ready();(self.directory/'8a-review.json').unlink()
        self.assertTrue(h.eight_a_errors(self.root,self.state,labflow))

    def test_verified_8a_still_requires_8b(self):
        self.developer_ready()
        self.assertFalse(h.eight_a_errors(self.root,self.state,labflow))
        self.assertTrue(h.gate_errors(self.root,self.state,labflow))

    def test_full_8a_8b_can_pass_without_automatic_user_approval(self):
        before=copy.deepcopy(self.state);self.master_ready()
        self.assertEqual(h.gate_errors(self.root,self.state,labflow),[])
        self.assertEqual(self.state,before)
        self.assertFalse(labflow.gate_is_approved(self.state,8))

    def test_whole_master_pdf_needs_page_review(self):
        self.master_ready();review=m.read(self.directory/'master-review.json');review['pages'].pop()
        m.write(self.directory/'master-review.json',review)
        self.assertIn('every rendered page',str(h.gate_errors(self.root,self.state,labflow)))

    def test_truncated_render_and_review_lists_do_not_hide_exported_pages(self):
        self.master_ready()
        for kind in ('developer','master'):
            with self.subTest(kind=kind):
                rendering=m.read(self.directory/(kind+'-rendering.json'));rendering['pages'].pop()
                m.write(self.directory/(kind+'-rendering.json'),rendering)
                review=m.read(self.directory/(kind+'-review.json'));review['pages'].pop()
                review['rendering_sha256']=m.sha(self.directory/(kind+'-rendering.json'))
                m.write(self.directory/(kind+'-review.json'),review)
                self.assertTrue(h.document_errors(self.root,self.state,labflow,kind))

    def test_export_count_binding_cannot_be_changed_by_render_manifest(self):
        self.developer_ready()
        rendering=m.read(self.directory/'developer-rendering.json');rendering['page_count']=1
        m.write(self.directory/'developer-rendering.json',rendering)
        self.assertIn('Export page count',str(h.document_errors(self.root,self.state,labflow,'developer')))

    def test_broken_master_shared_reference_is_rejected(self):
        self.master_ready()
        patch=m.read(self.directory/'master-addendum.json')
        patch['shared_references'][0]['target_text']='Nonexistent XR instructions'
        m.write(self.directory/'master-addendum.json',patch)
        with self.assertRaisesRegex(ValueError,'target is missing'):h.build_master(self.root,self.state,labflow)

    def test_master_reference_cannot_resolve_to_its_own_new_section(self):
        self.master_ready()
        original=self.root/'authorized-existing-master.docx'
        updated=self.root/'master-with-current-lab.docx'
        h.incremental_docx(original,updated,self.lab,['Only in current Lab'])
        h.stage_master(self.root,self.state,labflow,updated)
        patch=m.read(self.directory/'master-addendum.json')
        patch['shared_references'][0]['target_text']='Only in current Lab'
        m.write(self.directory/'master-addendum.json',patch)
        with self.assertRaisesRegex(ValueError,'target is missing'):h.build_master(self.root,self.state,labflow)

    def test_prepare_review_binds_pages_without_claiming_pass_or_overwriting(self):
        self.developer_ready()
        path=self.directory/'developer-review.json';path.unlink()
        review=h.prepare_review(self.root,self.state,labflow,'developer')
        self.assertEqual([p['page'] for p in review['pages']],[1,2])
        self.assertEqual(review['rendering_sha256'],m.sha(self.directory/'developer-rendering.json'))
        self.assertTrue(all(c['passed'] is False for c in review['content'].values()))
        self.assertTrue(h.document_errors(self.root,self.state,labflow,'developer'))
        before=path.read_bytes()
        with self.assertRaisesRegex(ValueError,'already exists'):h.prepare_review(self.root,self.state,labflow,'developer')
        self.assertEqual(path.read_bytes(),before)

    def test_prepare_student_and_consistency_reviews_have_correct_bindings(self):
        self.developer_ready()
        (self.directory/'8a-review.json').unlink()
        review=h.prepare_review(self.root,self.state,labflow,'8a')
        self.assertEqual(review['binding'],h.a_binding(self.root,self.state,labflow))
        self.assertTrue(h.eight_a_errors(self.root,self.state,labflow))
        (self.directory/'review.json').unlink()
        review=h.prepare_review(self.root,self.state,labflow,'student')
        self.assertEqual(review['generation_sha256'],m.sha(self.directory/'generation.json'))
        self.assertEqual(len(review['pages']),2)

    def test_addendum_initialization_requires_staged_base_and_preserves_work(self):
        self.master_ready();(self.directory/'master-addendum.json').unlink()
        template=self.root/'LabSpecs/templates/project-handbook-addendum.template.json'
        m.write(template,{'introduction':[],'shared_references':[{'text':'','target_text':''}]})
        patch=h.init_master_addendum(self.root,self.state,labflow)
        self.assertEqual(patch['8a_binding'],h.require_a(self.root,self.state,labflow))
        self.assertEqual(patch['lab_id'],self.lab)
        with self.assertRaisesRegex(ValueError,'already exists'):h.init_master_addendum(self.root,self.state,labflow)

    def status_payload(self, blockers):
        output=io.StringIO()
        with mock.patch.object(labflow,'load_state',return_value=self.state), \
             mock.patch.object(labflow,'validate_workspace_mode'), \
             mock.patch.object(labflow,'validate_contracts',return_value=([],[],{})), \
             mock.patch.object(labflow,'gate_readiness_errors',return_value=blockers), \
             mock.patch.object(labflow,'next_action',return_value='Revalidate'),contextlib.redirect_stdout(output):
            labflow.command_status(argparse.Namespace(project_root=self.root,lab_id=self.lab))
        return json.loads(output.getvalue())

    def test_status_does_not_call_stale_handbooks_complete(self):
        self.master_ready();labflow.record_gate_approval(self.state,8,'Approved previous books')
        payload=self.status_payload(['Handbook content changed'])
        self.assertFalse(payload['workflow_complete'])
        self.assertEqual(payload['stage'],'handbooks_revalidation')
        self.assertTrue(payload['accepted'])

    def test_status_distinguishes_recorded_acceptance_from_current_content(self):
        self.master_ready();labflow.record_gate_approval(self.state,8,'Approved previous books')
        self.fixture.brief.write_text(self.fixture.brief.read_text()+'\nchanged: true\n',encoding='utf-8')
        before=copy.deepcopy(self.state)
        payload=self.status_payload(['Accepted content changed'])
        self.assertFalse(payload['accepted']);self.assertTrue(payload['acceptance_recorded'])
        self.assertEqual(payload['stage'],'acceptance_revalidation')
        self.assertTrue(payload['acceptance_errors'])
        self.assertEqual(self.state,before)

    def test_final_8a_change_invalidates_8b(self):
        self.master_ready();review=m.read(self.directory/'8a-review.json');review['content']['same_version']['observation']='Updated review observation.'
        m.write(self.directory/'8a-review.json',review)
        self.assertIn('binding changed',str(h.gate_errors(self.root,self.state,labflow)))

    def test_saved_before_version_and_change_list_are_required(self):
        self.master_ready();(self.directory/'master-change-list.json').unlink()
        self.assertTrue(h.gate_errors(self.root,self.state,labflow))

    def test_status_step_follows_8a_then_8b_then_joint_confirmation(self):
        self.assertEqual(h.gate_step(self.root,self.state,labflow),'8A')
        self.developer_ready()
        self.assertEqual(h.gate_step(self.root,self.state,labflow),'8B')
        # Avoid rebuilding existing fixture PDF directories.
        with mock.patch.object(h,'eight_a_errors',return_value=[]),mock.patch.object(h,'document_errors',return_value=[]):
            self.assertEqual(h.gate_step(self.root,self.state,labflow),'awaiting_joint_confirmation')

    def test_old_gate8_approval_cannot_accept_new_book_bundle(self):
        self.master_ready()
        labflow.record_gate_approval(self.state,8,'User approved previous bundle')
        self.state['gate_approvals']['8']['handbooks_digest']='OLD'
        self.assertIn('another handbook bundle',str(h.gate_errors(self.root,self.state,labflow)))

    def test_document_reopening_does_not_invalidate_lab_acceptance(self):
        labflow.record_gate_approval(self.state,8,'Previous user confirmation')
        acceptance=copy.deepcopy(self.state['user_acceptance'])
        with mock.patch.object(labflow,'load_current_state',return_value=self.state), \
             mock.patch.object(labflow,'protected_baseline_errors',return_value=[]), \
             mock.patch.object(labflow,'source_reference_errors',return_value=[]), \
             mock.patch.object(labflow,'save_state'):
            labflow.command_reopen_handbooks(argparse.Namespace(project_root=self.root,lab_id=self.lab,note='Revise document wording'))
        self.assertEqual(self.state['user_acceptance'],acceptance)
        self.assertFalse(labflow.gate_is_approved(self.state,8))
        self.assertEqual(labflow.current_gate(self.state),8)

    def test_gate_six_approval_automatically_collects_developer_materials(self):
        self.state['gate_approvals'].pop('6')
        self.state.pop('user_acceptance')
        self.state['evidence']=dict(labflow.default_evidence(),compile='passed',scene_built='passed')
        with mock.patch.object(labflow,'load_current_state',return_value=self.state), \
             mock.patch.object(labflow,'protected_baseline_errors',return_value=[]), \
             mock.patch.object(labflow,'source_reference_errors',return_value=[]), \
             mock.patch.object(labflow,'manual_tools',return_value=m), \
             mock.patch.object(m,'handbook_tools',return_value=h), \
             mock.patch.object(h,'collect',wraps=h.collect) as collect_once, \
             mock.patch.object(labflow,'save_state'):
            labflow.command_approve_gate(argparse.Namespace(project_root=self.root,lab_id=self.lab,gate=6,note='User says continue'))
        collect_once.assert_called_once()
        record=m.read(self.root/f'BuildEvidence/{self.lab}/developer-notes/materials.json')
        self.assertEqual(record['observed_gate'],6)
        self.assertEqual(self.state['user_acceptance']['note'],'User says continue')

    def test_final_gate8_approval_binds_both_steps(self):
        self.master_ready()
        for relative in labflow.evidence_relative_paths(self.lab):
            path=self.root/relative;path.write_text(self.lab+' verified evidence',encoding='utf-8')
        with mock.patch.object(labflow,'load_current_state',return_value=self.state), \
             mock.patch.object(labflow,'protected_baseline_errors',return_value=[]), \
             mock.patch.object(labflow,'source_reference_errors',return_value=[]), \
             mock.patch.object(labflow,'save_state'):
            labflow.command_approve_gate(argparse.Namespace(project_root=self.root,lab_id=self.lab,gate=8,note='Both steps accepted'))
        self.assertEqual(self.state['gate_approvals']['8']['handbooks_digest'],h.approval_binding(self.root,self.state))
        self.assertFalse(h.gate_errors(self.root,self.state,labflow))





if __name__=='__main__':unittest.main()
