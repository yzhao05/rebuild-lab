"""Synthetic recovery/version regressions; never execute an actual Lab gate."""
import contextlib
import copy
import importlib.util
import io
import json
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock

import labflow as f
import manuals as m
import handbooks as h
import test_labflow as fixtures
import test_handbooks as books


class VersionAndContractTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name); self.lab = 'lab_example'
        self.state = fixtures.CompactWorkflowTests().fixture(self.root, 6)
        self.state['brief_confirmation'] = {'confirmed': True}
        f.save_state(self.root, self.lab, self.state)
        self.code = self.root / 'UnityProject/Assets/Scripts/Example.cs'
        self.code.parent.mkdir(parents=True); self.code.write_text('version one')
        for name in ('protected_baseline_errors', 'source_reference_errors'):
            patch = mock.patch.object(f, name, return_value=[]); patch.start(); self.addCleanup(patch.stop)
        patch = contextlib.redirect_stdout(io.StringIO()); patch.__enter__(); self.addCleanup(patch.__exit__, None, None, None)

    def args(self, *words):
        return f.build_parser().parse_args(['--project-root', str(self.root), *words, '--lab-id', self.lab])

    def record(self, key):
        f.command_record(self.args('record', '--'+key, 'passed', '--note', 'Observed synthetic check'))

    def test_gate5_domain_compile_cannot_refresh_old_domain_assertion(self):
        self.state=fixtures.CompactWorkflowTests().fixture(self.root,5)
        self.state['evidence']['gate5_foundation']='passed'
        f.save_state(self.root,self.lab,self.state)
        self.record('domain-slice');self.record('domain-compile')
        old=f.load_state(self.root,self.lab)['evidence']['domain_slice_lab_output_digest']
        self.code.write_text('changed after domain observation')
        self.record('domain-compile')
        state=f.load_state(self.root,self.lab)
        self.assertEqual(state['evidence']['domain_slice_lab_output_digest'],old)
        self.assertNotIn('gate5_domain_lab_output_digest',state['evidence'])
        self.assertTrue(any('domain_slice' in e for e in f.gate5_evidence_freshness_errors(self.root,state)))
        self.record('domain-slice')
        self.assertFalse(any('domain evidence' in e for e in f.gate5_evidence_freshness_errors(self.root,f.load_state(self.root,self.lab))))

    def test_contract_only_recovery_does_not_require_or_refresh_stale_unity_checks(self):
        self.record('compile');self.record('scene-built')
        self.code.write_text('version changed before contract repair')
        with mock.patch.object(f,'validate_contracts',return_value=(['Invalid contract'],[],{})):
            f.command_validate(self.args('recheck-contracts'))
        with mock.patch.object(f,'validate_contracts',return_value=([],[],{})):
            f.command_validate(self.args('recheck-contracts'))
            self.assertEqual(f.command_retrospective(fixtures.CompactWorkflowTests().resolve_args(self.root)),0)
        state=f.load_state(self.root,self.lab)
        self.assertEqual(len(f.gate6_evidence_freshness_errors(self.root,state)),2)
        self.assertFalse(f.gate_is_approved(state,6))

    def test_completed_contract_failure_is_recorded_without_throwing_or_advancing(self):
        f.record_gate_approval(self.state,6,'synthetic acceptance')
        f.record_gate_approval(self.state,8,'synthetic handbook approval')
        f.save_state(self.root,self.lab,self.state)
        approvals=copy.deepcopy(self.state['gate_approvals'])
        with mock.patch.object(f,'validate_contracts',return_value=(['Changed invalid contract'],[],{})):
            self.assertEqual(f.command_validate(self.args('recheck-contracts')),1)
        state=f.load_state(self.root,self.lab)
        self.assertFalse(state['last_contract_validation']['ok'])
        self.assertEqual(state['gate_approvals'],approvals)

    def test_legacy_pass_flags_without_digests_are_not_ready(self):
        self.state['evidence'].update(compile='passed', scene_built='passed')
        self.assertEqual(len(f.gate6_evidence_freshness_errors(self.root, self.state)), 2)

    def test_each_observation_binds_its_own_version(self):
        self.record('compile')
        before = f.load_state(self.root, self.lab)['evidence']['compile_content_digest']
        self.code.write_text('version two')
        self.record('scene-built')
        state = f.load_state(self.root, self.lab)
        self.assertEqual(state['evidence']['compile_content_digest'], before)
        self.assertEqual(len(f.gate6_evidence_freshness_errors(self.root, state)), 1)
        self.assertIn('compile', f.gate6_evidence_freshness_errors(self.root, state)[0])
        self.record('compile')
        self.assertEqual(f.gate6_evidence_freshness_errors(self.root, f.load_state(self.root, self.lab)), [])

    def test_meta_only_change_invalidates_gate5_and_gate6_snapshots(self):
        meta = self.code.with_suffix('.cs.meta'); meta.write_text('guid: original')
        old5 = f.gate5_lab_output_digest(self.root, self.lab)
        self.record('compile'); self.record('scene-built')
        meta.write_text('guid: changed')
        self.assertNotEqual(old5, f.gate5_lab_output_digest(self.root, self.lab))
        self.assertEqual(len(f.gate6_evidence_freshness_errors(self.root, f.load_state(self.root,self.lab))), 2)

    def test_stale_pass_cannot_be_relabelled_by_approval(self):
        self.record('compile'); self.record('scene-built'); self.code.write_text('unobserved version')
        with self.assertRaises(RuntimeError):
            f.command_approve_gate(self.args('approve-gate', '--gate', '6', '--note', '继续'))
        self.assertFalse(f.gate_is_approved(f.load_state(self.root,self.lab),6))

    def test_recheck_contracts_does_not_advance_or_refresh_evidence(self):
        before = f.load_state(self.root,self.lab)
        with mock.patch.object(f, 'validate_contracts', return_value=([], [], {})):
            self.assertEqual(f.command_validate(self.args('recheck-contracts')),0)
        after=f.load_state(self.root,self.lab)
        self.assertEqual(before['gate_approvals'], after['gate_approvals'])
        self.assertEqual(before['evidence'], after['evidence'])
        self.assertEqual(before.get('stage'), after.get('stage'))
        self.assertFalse(f.contract_validation_freshness_errors(self.root,after))
        brief=self.root/f'LabSpecs/{self.lab}/brief.yaml';brief.write_text('manual: changed')
        self.assertTrue(f.contract_validation_freshness_errors(self.root,after))

    def test_contract_recovery_requires_real_recheck_for_current_incident(self):
        with mock.patch.object(f,'validate_contracts',return_value=(['Invalid contract'],[],{})):
            self.assertEqual(f.command_validate(self.args('recheck-contracts')),1)
        resolve=fixtures.CompactWorkflowTests().resolve_args(self.root)
        with mock.patch.object(f,'validate_contracts',return_value=([],[],{})):
            with self.assertRaisesRegex(RuntimeError,'contract recheck'):
                f.command_retrospective(resolve)
            self.assertEqual(f.command_validate(self.args('recheck-contracts')),0)
            self.assertEqual(f.command_retrospective(resolve),0)
        state=f.load_state(self.root,self.lab)
        self.assertEqual(f.current_gate(state),6)
        self.assertIsNone(state['blocker_recovery']['active'])

    def test_renewed_confirmation_preserves_approvals_and_old_evidence(self):
        before=f.load_state(self.root,self.lab)
        with mock.patch.object(f,'brief_readiness_errors',return_value=[]):
            f.command_reconfirm_inputs(self.args('reconfirm-inputs','--scope','brief','--note','继续'))
        after=f.load_state(self.root,self.lab)
        self.assertEqual(before['evidence'],after['evidence'])
        self.assertEqual(before['gate_approvals'],after['gate_approvals'])
        self.assertEqual(after['input_confirmations']['brief']['note'],'继续')
        self.assertEqual(f.current_gate(after),6)


class HandbookRecoveryTests(unittest.TestCase):
    def setUp(self):
        self.fixture=books.HandbookWorkflowTests();self.fixture.setUp()
        self.addCleanup(self.fixture.doCleanups)
        self.root,self.lab,self.state=self.fixture.root,self.fixture.lab,self.fixture.state
        self.state['workflow_mode']='integrated-development'
        for name in ('protected_baseline_errors','source_reference_errors'):
            patch=mock.patch.object(f,name,return_value=[]);patch.start();self.addCleanup(patch.stop)
        patch=contextlib.redirect_stdout(io.StringIO());patch.__enter__();self.addCleanup(patch.__exit__,None,None,None)

    def open(self):
        f.begin_blocker(self.root,self.state,'logic','Observed handbook content mismatch')
        f.save_state(self.root,self.lab,self.state)

    def cli(self, tool, action):
        with mock.patch.object(sys,'argv',[tool.__name__,'--project-root',str(self.root),action,'--lab-id',self.lab]):
            return tool.main()

    def resolve(self):
        args=fixtures.CompactWorkflowTests().resolve_args(self.root);args.lab_id=self.lab
        return f.command_retrospective(args)

    def reopen(self):
        args=f.build_parser().parse_args(['--project-root',str(self.root),'reopen-acceptance','--lab-id',self.lab,'--note','User changed accepted teaching inputs'])
        return f.command_reopen_acceptance(args)

    def test_changed_accepted_version_can_backtrack_without_certifying_incident(self):
        self.open();self.fixture.fixture.brief.write_text(self.fixture.fixture.brief.read_text()+'\n# revised teaching input\n')
        self.assertEqual(self.reopen(),0)
        state=f.load_state(self.root,self.lab)
        self.assertEqual(f.current_gate(state),6)
        self.assertFalse(f.gate_is_approved(state,6))
        self.assertIsNone(state['blocker_recovery']['active'])
        report=f.read_json(self.root/state['blocker_recovery']['history'][-1]['path'])
        self.assertEqual(report['status'],'superseded_by_reacceptance')
        self.assertFalse(report['verified'])
        self.assertEqual(state['evidence']['compile'],'pending')
        self.assertFalse(f.blocker_history_errors(self.root,state))
        self.assertFalse((self.root/f'KnowledgeBase/rebuild-lessons/{self.lab}.md').exists())

    def test_unchanged_version_cannot_use_reopen_to_escape_handbook_failure(self):
        self.open()
        with self.assertRaisesRegex(RuntimeError,'accepted Lab content changed'):self.reopen()
        self.assertTrue(f.load_state(self.root,self.lab)['blocker_recovery']['active'])

    def test_backtrack_state_failure_preserves_open_incident_and_acceptance(self):
        self.open();self.fixture.fixture.brief.write_text(self.fixture.fixture.brief.read_text()+'\n# changed\n')
        state_path=f.state_path(self.root,self.lab);report_path=f.blocker_active_path(self.root,self.state)
        before={p:p.read_bytes() for p in (state_path,report_path)}
        with mock.patch.object(f,'save_state',side_effect=OSError('synthetic save failure')):
            with self.assertRaises(OSError):self.reopen()
        self.assertEqual(before,{p:p.read_bytes() for p in before})

    def test_student_check_and_generation_are_reachable_during_recovery(self):
        self.open();self.fixture.fixture.ready_evidence()
        self.assertEqual(self.cli(m,'check'),0)
        with mock.patch.object(m,'build',return_value={}) as build:
            self.assertEqual(self.cli(m,'build'),0);build.assert_called_once()
        self.assertTrue(f.load_state(self.root,self.lab)['blocker_recovery']['active'])

    def test_8a_repair_check_resolve_cycle_keeps_8b_pending(self):
        self.open()
        self.assertEqual(self.state['blocker_recovery']['active']['required_rechecks'],['handbooks_8a'])
        self.assertEqual(self.cli(h,'check-8a'),1)
        self.fixture.developer_ready()
        with self.assertRaisesRegex(RuntimeError,'check command'):
            self.resolve()
        self.assertEqual(self.cli(h,'check-8a'),0)
        self.assertEqual(self.resolve(),0)
        saved=f.load_state(self.root,self.lab)
        self.assertEqual(f.current_gate(saved),8)
        self.assertFalse(f.gate_is_approved(saved,8))
        self.assertTrue(h.gate_errors(self.root,saved,f))

    def test_8b_cannot_resolve_with_8a_only_check(self):
        self.fixture.developer_ready();self.open()
        self.assertEqual(self.state['blocker_recovery']['active']['required_rechecks'],['handbooks'])
        self.assertEqual(self.cli(h,'check-8a'),0)
        with self.assertRaises(RuntimeError):self.resolve()
        with mock.patch.object(self.fixture,'developer_ready'):
            self.fixture.master_ready()
        self.assertEqual(self.cli(h,'check'),0)
        self.assertEqual(self.resolve(),0)

    def test_changed_review_invalidates_recorded_recovery_proof(self):
        self.open();self.fixture.developer_ready();self.assertEqual(self.cli(h,'check-8a'),0)
        path=self.fixture.directory/'8a-review.json';review=m.read(path)
        review['reviewer']='Another synthetic reviewer';m.write(path,review)
        self.assertFalse(h.eight_a_errors(self.root,self.state,f))
        with self.assertRaisesRegex(RuntimeError,'check command'):self.resolve()
        self.assertEqual(self.cli(h,'check-8a'),0);self.assertEqual(self.resolve(),0)

    def test_recovery_does_not_permit_approval(self):
        self.fixture.master_ready();self.open();self.assertEqual(self.cli(h,'check'),0)
        args=f.build_parser().parse_args(['--project-root',str(self.root),'approve-gate','--lab-id',self.lab,'--gate','8','--note','继续'])
        with self.assertRaisesRegex(RuntimeError,'recovery'):f.command_approve_gate(args)

    def test_contract_recheck_can_resolve_before_reopening_stale_acceptance(self):
        self.state['brief_confirmation']={'confirmed':True}
        f.save_state(self.root,self.lab,self.state)
        args=f.build_parser().parse_args(['--project-root',str(self.root),'recheck-contracts','--lab-id',self.lab])
        with mock.patch.object(f,'validate_contracts',return_value=(['Revised contract missing metadata'],[],{})):
            self.assertEqual(f.command_validate(args),1)
        saved=f.load_state(self.root,self.lab)
        self.assertEqual(saved['blocker_recovery']['active']['required_rechecks'],['contracts'])
        with mock.patch.object(f,'validate_contracts',return_value=([],[],{})):
            self.assertEqual(f.command_validate(args),0)
            self.assertEqual(self.resolve(),0)
        self.assertFalse(f.gate_is_approved(f.load_state(self.root,self.lab),8))


class PortableLightingTests(unittest.TestCase):
    def test_source_manifest_carries_implementation_and_separate_regressions(self):
        root=Path(__file__).resolve().parents[2]
        manifest=json.loads((root/'ReusableFoundation/Gate5MinimalSlice/manifest.json').read_text())
        entries={x['path']:x['sha256'] for x in manifest['files']}
        for name in ('Editor/EmVrRoomLighting.cs','Tests/LightingEditMode/EmVrRoomLightingTests.cs',
                     'Tests/LightingEditMode/EMVR.ReusableFoundation.Lighting.Tests.asmdef'):
            self.assertEqual(entries[name],f.file_sha256(root/'ReusableFoundation/Gate5MinimalSlice/Source'/name))
        code=(root/'ReusableFoundation/Gate5MinimalSlice/Source/Editor/EmVrRoomLighting.cs').read_text()
        self.assertNotIn('ece329_electric_field_0',code)

    def test_clean_gate5_import_includes_byte_exact_lighting_source(self):
        root=Path(__file__).resolve().parents[2]
        path=root/'.agents/skills/build-unity-vr-lab-blind/scripts/materialize_gate5_foundation.py'
        spec=importlib.util.spec_from_file_location('closure_importer',path);tool=importlib.util.module_from_spec(spec);spec.loader.exec_module(tool)
        import shutil, os, stat
        with tempfile.TemporaryDirectory() as directory:
            out=Path(directory)
            try:
                shutil.copytree(root/'ReusableFoundation',out/'ReusableFoundation')
                fixtures.CompactWorkflowTests().fixture(out,5)
                with mock.patch.object(sys,'argv',['importer','--builder-root',str(out),'--lab-id','lab_example']),contextlib.redirect_stdout(io.StringIO()):
                    self.assertEqual(tool.main(),0)
                    self.assertEqual(tool.main(),0)
                self.assertEqual(f.gate5_materialization_errors(out,'lab_example'),[])
                imported=out/'UnityProject/Assets/Generated/lab_example/Gate5Foundation/Editor/EmVrRoomLighting.cs'
                self.assertEqual(imported.read_bytes(),(root/'ReusableFoundation/Gate5MinimalSlice/Source/Editor/EmVrRoomLighting.cs').read_bytes())
            finally:
                for p in out.rglob('*'):
                    if p.is_file():os.chmod(p,stat.S_IWRITE|stat.S_IREAD)


if __name__=='__main__':unittest.main()
