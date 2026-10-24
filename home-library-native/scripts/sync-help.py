"""Synchronize user help for every release; --check validates without writing."""
from pathlib import Path
import json,re,sys
root=Path(__file__).resolve().parents[1]
source=root/'assets/help.json';data=json.loads(source.read_text())
version=re.search(r'Version="([^"]+)"',(root/'server/SetupStatus.cs').read_text())[1].removesuffix('-preview')
assert data.get('release')==version,'Update the help release and topics for this release.'
assert any('Deleting' in t['title'] and 'account' in t['title'] for t in data['topics'])
raw=source.read_bytes();targets=['server/wwwroot/help.json','android/app/src/main/assets/help.json','ios/HomeLibrary/help.json']
markdown='# Carribrary help — '+version+'\n\n'+'\n\n'.join('## '+t['title']+'\n\n'+t['body'] for t in data['topics'])+'\n'
for relative in targets:
 p=root/relative
 if '--check' in sys.argv:assert p.read_bytes()==raw,p
 else:p.write_bytes(raw)
p=root.parent/f'Carribrary-Help-{version}.md'
if '--check' in sys.argv:assert p.read_text()==markdown,p
else:p.write_text(markdown)
print('PASS current release help is identical on server, Android and iPhone; standalone guide matches')
