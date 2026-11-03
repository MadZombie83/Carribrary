"""Synchronize user help for every release; --check validates without writing."""
from pathlib import Path
import json,re,sys,subprocess
root=Path(__file__).resolve().parents[1]
source=root/'assets/help.json';data=json.loads(source.read_text())
full_version=re.search(r'Version="([^"]+)"',(root/'server/SetupStatus.cs').read_text())[1]
version=re.search(r'Version="([^"]+)"',(root/'server/SetupStatus.cs').read_text())[1].removesuffix('-preview')
assert data.get('release')==version,'Update the help release and topics for this release.'
recovery_rule="return $Version -match '^"+re.escape(version)+"(?:[.+-].*)?$'"
assert recovery_rule in (root/'installer/Recovery.Core.ps1').read_text(),'Review program-recovery compatibility for this release.'
patch=version.split('.')[-1]
assert '7=>patch>=53&&patch<='+patch+',' in (root/'server/RecoveryCompatibility.cs').read_text(),'Review complete-backup compatibility for this release.'
assert any('Deleting' in t['title'] and 'account' in t['title'] for t in data['topics'])
about=next(t for t in data['topics'] if t['title'].startswith('About Carribrary:'))
assert about['body'].startswith('Carribrary '+full_version+'\n'),'Update About to the actual release version.'
raw=source.read_bytes();targets=['server/wwwroot/help.json','android/app/src/main/assets/help.json','ios/HomeLibrary/help.json']
markdown='# Carribrary help — '+version+'\n\n'+'\n\n'.join('## '+t['title']+'\n\n'+t['body'] for t in data['topics'])+'\n'
for relative in targets:
 p=root/relative
 if '--check' in sys.argv:assert p.read_bytes()==raw,p
 else:p.write_bytes(raw)
p=root.parent/f'Carribrary-Help-{version}.md'
if '--check' in sys.argv:assert p.read_text()==markdown,p
else:p.write_text(markdown)
# Release labels in the illustrated guide must follow the same canonical release.
quick=root/'server/wwwroot/quick-start.html'
text=quick.read_text()
updated=re.sub(r'<title>Carribrary [^<]+ quick start</title>', '<title>Carribrary '+version+' quick start</title>',text)
updated=re.sub(r'Illustrated quick start · [^<]+', 'Illustrated quick start · '+version,updated)
updated=re.sub(r'current program-only compatibility is <strong>[^<]+</strong>', 'current program-only compatibility is <strong>'+version+' only</strong>',updated)
if '--check' in sys.argv:assert text==updated,'Illustrated quick start version is out of sync'
else:quick.write_text(updated)
# Keep visible product identification aligned with the actual server version.
footer='<!-- product-footer:start --><footer class="product-footer" data-product-footer><small>© 2026 Chezburber Studios · Carribrary '+full_version+'</small></footer><!-- product-footer:end -->'
footer_pages=['index.html','local-login.html','companion-login.html','help.html','quick-start.html','welcome.html','catalog.html','account.html','reading.html','delete-help.html','kiosk.html','complete-recovery.html']
for name in footer_pages:
 p=root/'server/wwwroot'/name;text=p.read_text()
 updated=re.sub(r'<!-- product-footer:start -->.*?<!-- product-footer:end -->','',text,flags=re.S)
 if name=='index.html':updated=re.sub(r'(<span id="app-version">)[^<]*',r'\g<1>Carribrary '+full_version,updated)
 target='</body>' if name=='welcome.html' else '</main>'
 at=updated.rfind(target);assert at>=0,name
 updated=updated[:at]+footer+updated[at:]
 if '--check' in sys.argv:assert text==updated,'Product footer out of sync: '+name
 else:p.write_text(updated)
subprocess.run([sys.executable,str(root/'scripts/sync-privacy.py')]+(['--check'] if '--check' in sys.argv else []),check=True)
print('PASS current release help is identical on server, Android and iPhone; standalone guide matches')
