"""Exercise local build orchestration without compiling/installing or using signing material."""
from pathlib import Path
import importlib.util, tempfile, unittest, types
from unittest.mock import patch

spec=importlib.util.spec_from_file_location('builder',Path(__file__).resolve().parents[1]/'Build-Carribrary.py')
builder=importlib.util.module_from_spec(spec);spec.loader.exec_module(builder)

class LocalBuilderTests(unittest.TestCase):
 def setUp(self):
  self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
  self.root=Path(self.temp.name);self.project=self.root/'source';(self.project/'server').mkdir(parents=True)
  (self.project/'server/SetupStatus.cs').write_text('Version="2.0.34-preview";')
  self.fake=self.root/'compiler';self.fake.write_text('test executable placeholder')
  self.patches=[patch.object(builder,'PROJECT',self.project),patch.object(builder,'WORKSPACE',self.root),patch.object(builder,'find_tool',return_value=str(self.fake))]
  for p in self.patches:p.start();self.addCleanup(p.stop)
 def test_versioned_folders_reuse_tools_without_moving_source(self):
  folder=self.root/'Carribrary-2.0.38';folder.mkdir()
  legacy=self.root/'Carribrary-main'/'.build-tools';legacy.mkdir(parents=True)
  with patch.dict(builder.os.environ,{},clear=True):
   self.assertEqual(builder.tool_cache(folder),legacy)
   shared=self.root/'.build-tools';shared.mkdir()
   self.assertEqual(builder.tool_cache(folder),shared)
   local=folder/'.build-tools';local.mkdir()
   self.assertEqual(builder.tool_cache(folder),local)
  with patch.dict(builder.os.environ,{'CARRIBRARY_TOOLS':str(self.root/'chosen')}):self.assertEqual(builder.tool_cache(folder),self.root/'chosen')
  self.assertTrue(legacy.is_dir())
 def test_check_and_plan_never_execute_compilers(self):
  with patch.object(builder.subprocess,'run') as run:
   self.assertEqual(builder.main(['--target','windows','--check']),0)
   self.assertEqual(builder.main(['--target','windows','--dry-run']),0)
   run.assert_not_called()
  self.assertFalse((self.project/'.build-cache').exists())
 def test_missing_compiler_fails_before_output(self):
  with patch.object(builder,'find_tool',return_value=None),patch.object(builder.subprocess,'run') as run:
   self.assertEqual(builder.main(['--target','windows','--check']),1);run.assert_not_called()
  self.assertFalse((self.project/'.build-cache').exists())
 def test_windows_target_cross_compiles_then_packages_without_installing(self):
  commands=[]
  def fake_run(command,**kwargs):
   commands.append(command)
   for arg in command:
    if arg.startswith('-DOUTPUT=') or arg.startswith('/DOUTPUT='):Path(arg.split('=',1)[1]).write_bytes(b'fake installer, not executable')
   return types.SimpleNamespace(returncode=0)
  with patch.object(builder,'WINDOWS',False),patch.object(builder.subprocess,'run',side_effect=fake_run):
   self.assertEqual(builder.main(['--target','windows','--offline']),0)
  publishes=[c for c in commands if 'publish' in c];self.assertEqual(len(publishes),2)
  self.assertTrue(all('--no-restore' in c and 'win-x64' in c for c in publishes))
  self.assertTrue(any('-DOUTPUT=' in ' '.join(c) for c in commands))
  self.assertFalse(any(token in ['install','--import','--restore-encrypted','Start-Service','Stop-Service'] for c in commands for token in c))
  self.assertTrue((self.root/'Carribrary-2.0.34-preview-local-SHA256.txt').exists())
 def test_windows_nsis_uses_windows_switches(self):
  commands=[]
  def fake_run(command,**kwargs):
   commands.append(command)
   for arg in command:
    if arg.startswith('/DOUTPUT='):Path(arg.split('=',1)[1]).write_bytes(b'fake Windows installer')
   return types.SimpleNamespace(returncode=0)
  with patch.object(builder,'WINDOWS',True),patch.object(builder.subprocess,'run',side_effect=fake_run):
   self.assertEqual(builder.main(['--target','windows']),0)
  self.assertTrue(any('/V2' in c and any(a.startswith('/DOUTPUT=') for a in c) for c in commands))
 def test_linux_packages_rejected_on_windows_without_commands(self):
  with patch.object(builder,'WINDOWS',True),patch.object(builder.platform,'system',return_value='Windows'),patch.object(builder.subprocess,'run') as run:
   self.assertEqual(builder.main(['--target','pi']),2);run.assert_not_called()

if __name__=='__main__':unittest.main()
