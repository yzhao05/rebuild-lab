"""Portable filesystem verification without running a Lab or opening Unity."""
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest


@unittest.skipUnless(os.name == 'nt' and shutil.which('powershell'), 'Windows PowerShell regression')
class LongPathHashTests(unittest.TestCase):
    def test_release_verifier_hashes_exact_bytes_beyond_max_path_and_rejects_missing(self):
        tool=Path(__file__).resolve().parents[2]/'.agents/skills/build-unity-vr-lab-blind/scripts/verify_builder_pack.ps1'
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory)
            target=root/('long_'+'a'*70)/('long_'+'b'*70)/('long_'+'c'*70)/('long_'+'d'*40)/'source.asset'
            native_target=Path('\\\\?\\'+str(target))
            # The tested provider gets an ordinary long path; fixture I/O itself
            # must also support Windows long paths and clean up only its own tree.
            self.assertEqual(root.resolve().parent,Path(tempfile.gettempdir()).resolve())
            native_target.parent.mkdir(parents=True)
            self.addCleanup(shutil.rmtree,'\\\\?\\'+str(root),True)
            data='CRLF\r\nLF\n中文\r\n'.encode('utf-8');native_target.write_bytes(data)
            self.assertGreater(len(str(target)),260)
            script=root/'check.ps1'
            script.write_text('''param([string]$ToolPath, [string]$TargetPath)
$ErrorActionPreference = 'Stop'
$tokens = $null; $parseErrors = $null
$tree = [System.Management.Automation.Language.Parser]::ParseFile($ToolPath, [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count) { throw 'PowerShell source has parse errors' }
foreach ($name in @('Get-PortableNativePath', 'Get-PortableFileHash', 'Test-PortablePath', 'Get-PortableContent')) {
    $node = $tree.Find({param($n) $n -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq $name}, $false)
    if ($null -eq $node) { throw "Required hash helper is missing: $name" }
    . ([scriptblock]::Create($node.Extent.Text))
}
$hash = (Get-PortableFileHash -LiteralPath $TargetPath).Hash
$rejected = $false
try { Get-PortableFileHash -LiteralPath ($TargetPath + '.missing') | Out-Null } catch { $rejected = $true }
$raw = Get-PortableContent -LiteralPath $TargetPath -Raw
$lines = @(Get-PortableContent -LiteralPath $TargetPath)
@{sha256=$hash; missing_rejected=$rejected;
    file_exists=(Test-PortablePath -LiteralPath $TargetPath);
    directory_exists=(Test-PortablePath -LiteralPath ([System.IO.Path]::GetDirectoryName($TargetPath)));
    missing_exists=(Test-PortablePath -LiteralPath ($TargetPath + '.missing'));
    raw_preserves_crlf=$raw.Contains("`r`n"); line_count=$lines.Count;
    unicode_preserved=($lines[2].Length -eq 2)
} | ConvertTo-Json -Compress
''',encoding='utf-8-sig')
            env=dict(os.environ,PSModulePath=str(Path(os.environ['SystemRoot'])/'System32/WindowsPowerShell/v1.0/Modules'))
            run=subprocess.run(['powershell','-NoProfile','-ExecutionPolicy','Bypass','-File',str(script),'-ToolPath',str(tool),'-TargetPath',str(target)],capture_output=True,env=env)
            self.assertEqual(run.returncode,0,run.stderr.decode(errors='replace'))
            result=json.loads(run.stdout.decode('utf-8-sig'))
            self.assertEqual(result['sha256'],hashlib.sha256(data).hexdigest().upper())
            self.assertTrue(result['missing_rejected'])
            self.assertTrue(result['file_exists'])
            self.assertTrue(result['directory_exists'])
            self.assertFalse(result['missing_exists'])
            self.assertTrue(result['raw_preserves_crlf'])
            self.assertEqual(result['line_count'],3)
            self.assertTrue(result['unicode_preserved'])
            self.assertEqual(native_target.read_bytes(),data)
            shutil.rmtree('\\\\?\\'+str(root))


if __name__=='__main__':unittest.main()
