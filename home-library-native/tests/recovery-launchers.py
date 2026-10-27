#!/usr/bin/env python3
"""Check generated desktop/package layouts using mock payloads; never build/install a package."""
from pathlib import Path
import tempfile,shutil,subprocess,runpy,sys,configparser
from unittest.mock import patch
root=Path(__file__).resolve().parents[1]
with tempfile.TemporaryDirectory(prefix='carribrary-layout-test-') as temp:
 copy=Path(temp)/'source';copy.mkdir()
 for name in ['linux','assets','licenses']:
  shutil.copytree(root/name,copy/name)
 (copy/'server').mkdir();shutil.copy2(root/'server/SetupStatus.cs',copy/'server/SetupStatus.cs')
 shutil.copy2(root/'THIRD-PARTY-NOTICES.txt',copy/'THIRD-PARTY-NOTICES.txt')
 for target in ['linux-x64','linux-arm64']:
  p=copy/'release'/target;p.mkdir(parents=True);(p/'HomeLibrary.Server').write_text('mock payload; not executable')
 calls=[]
 def command(args,**kwargs):
  calls.append(args)
  if 'compose' in args or any('appstreamcli-compose' in str(a) for a in args):
   p=copy/'release/linux-packages/flatpak/files/share/app-info/xmls/flatpak.xml.gz';p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(b'mock metadata')
  return subprocess.CompletedProcess(args,0)
 with patch.object(sys,'argv',['build-packages.py']),patch('subprocess.run',command):runpy.run_path(str(copy/'linux/build-packages.py'),run_name='__main__')
 flat=copy/'release/linux-packages/flatpak';pi=copy/'release/linux-packages/pi'
 for base in [flat/'files/share/applications',pi/'usr/share/applications']:
  p=configparser.ConfigParser(interpolation=None);p.read(base/'com.household.HomeLibrary.Recovery.desktop');assert 'recovery' in p['Desktop Entry']['Exec'];assert 'x-scheme-handler/carribrary-recovery;' in p['Desktop Entry']['MimeType']
 normal=configparser.ConfigParser(interpolation=None);normal.read(flat/'files/share/applications/com.household.HomeLibrary.desktop');assert normal['Desktop Entry']['Actions']=='Stop;Recovery;'
 assert (pi/'usr/bin/home-library-recovery').stat().st_mode&0o111
 assert 'pkexec' in (pi/'DEBIAN/control').read_text()
 assert any('dpkg-deb' in c for c in calls) and any('build-bundle' in c for c in calls)
 for f in ['server/CompleteRecovery.cs','server/RecoveryWizard.cs','server/RecoveryPlatform.cs','server/wwwroot/complete-recovery.html','server/wwwroot/complete-recovery.js','server/wwwroot/complete-recovery.css','installer/Complete-Backup-Recovery.ps1']:assert (root/f).is_file()
 nsi=(root/'installer/HomeLibrary.nsi').read_text();assert 'Complete Backup Recovery.lnk' in nsi and 'carribrary-recovery\\shell\\open\\command' in nsi and 'DeleteRegKey HKCR "carribrary-recovery"' in nsi
 print('PASS mock Flatpak/Pi layouts preserve Stop and add menu/URI recovery launchers; Windows installer source includes shortcut/helper/protocol cleanup. No release packages generated.')
