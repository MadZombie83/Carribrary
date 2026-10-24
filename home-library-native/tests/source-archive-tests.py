from pathlib import Path
import importlib.util, tempfile, unittest, zipfile
from unittest.mock import patch
spec=importlib.util.spec_from_file_location('archive',Path(__file__).resolve().parents[1]/'scripts/package-source.py');archive=importlib.util.module_from_spec(spec);spec.loader.exec_module(archive)
class SourceArchiveTests(unittest.TestCase):
 def test_versioned_root_builder_and_no_overwrite(self):
  with tempfile.TemporaryDirectory() as d:
   root=Path(d)/'source';(root/'home-library-native/server').mkdir(parents=True)
   files={'home-library-native/server/SetupStatus.cs':'Version="2.0.38-preview";','home-library-native/Build-Carribrary.py':'# builder','Carribrary-Help-2.0.38.md':'# help'}
   for name,data in files.items():(root/name).write_text(data)
   with patch.object(archive.subprocess,'check_output',return_value=('\0'.join(files)+'\0').encode()):
    result=archive.package(root)
    with zipfile.ZipFile(result['archive']) as z:self.assertTrue(all(n.startswith('Carribrary-2.0.38/') for n in z.namelist()));self.assertEqual(z.read('Carribrary-2.0.38/Carribrary-Help-2.0.38.md'),b'# help')
    before=Path(result['archive']).read_bytes()
    with self.assertRaises(FileExistsError):archive.package(root)
    self.assertEqual(Path(result['archive']).read_bytes(),before)
 def test_runtime_and_symlink_rejected(self):
  with tempfile.TemporaryDirectory() as d:
   root=Path(d);(root/'home-library-native/server').mkdir(parents=True);(root/'home-library-native/server/SetupStatus.cs').write_text('Version="2.0.38-preview";');(root/'home-library-native/Build-Carribrary.py').write_text('# builder');(root/'Carribrary-Help-2.0.38.md').write_text('# help');(root/'data').mkdir();(root/'data/library.db').write_text('private')
   names=['home-library-native/server/SetupStatus.cs','home-library-native/Build-Carribrary.py','Carribrary-Help-2.0.38.md','data/library.db']
   with patch.object(archive.subprocess,'check_output',return_value=('\0'.join(names)+'\0').encode()):
    with self.assertRaises(ValueError):archive.package(root,root/'archive.zip')
   self.assertFalse((root/'archive.zip').exists())
   (root/'linked.txt').symlink_to(root/'Carribrary-Help-2.0.38.md')
   names[-1]='linked.txt'
   with patch.object(archive.subprocess,'check_output',return_value=('\0'.join(names)+'\0').encode()):
    with self.assertRaises(ValueError):archive.package(root,root/'archive.zip')
   self.assertFalse((root/'archive.zip').exists())
if __name__=='__main__':unittest.main()
