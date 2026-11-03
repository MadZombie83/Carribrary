#!/usr/bin/env python3
"""Current documentation contracts: sync, task coverage, links and packaged guides."""
from pathlib import Path
import json,re,subprocess
from html.parser import HTMLParser
root=Path(__file__).resolve().parents[1]
subprocess.run(['python3',str(root/'scripts/sync-help.py'),'--check'],check=True)
j=json.loads((root/'assets/help.json').read_text());topics=j['topics'];titles=[t['title'] for t in topics];body='\n'.join(t['body'] for t in topics)
assert len(titles)==len(set(titles)) and all(t['body'].strip() for t in topics)
# These task families previously had contradictory, missing or obsolete directions.
required=['Install and open the Windows','Install the Linux Flatpak','Install the Raspberry Pi','First administrator','Recover an administrator','Pair the optional Android','Pair the iPhone','Public access on Windows','Public sharing setup on Linux','Batch add','Import a CSV','Librarian checkout','Librarian return','Borrower renewals','password recovery','email reminders','Optional self-checkout','Library cards','Authorize a separate kiosk','Private-list passphrase','Save a custom theme','Library analytics','Weekly portable','Verify a saved complete','replacement computer','Review restored access','Delete your own','Deleting a borrower account']
for phrase in required:
 assert any(phrase.lower() in t.lower() for t in titles),f'Missing task: {phrase}'
for phrase in ['Install and open the Windows','First administrator','Pair the optional Android','Public access on Windows','Batch add','Import a CSV','Librarian checkout','Librarian return','Create a complete server backup','Verify a saved complete backup','Delete your own']:
 t=next(t for t in topics if phrase.lower() in t['title'].lower());assert re.search(r'(?:^|\n\n)1\. ',t['body']),f'Numbered steps missing: {phrase}'
for stale in ['2.0.39-source.zip','no automated email','no automatic email recovery','compatible program recovery is 2.0.36']:
 assert stale not in body.lower(),f'Stale Help: {stale}'
class Guide(HTMLParser):
 def __init__(self):super().__init__();self.ids=[];self.anchors=[];self.images=[]
 def handle_starttag(self,tag,attrs):
  a=dict(attrs)
  if tag=='section':self.ids.append(a.get('id'))
  if tag=='a' and a.get('href','').startswith('#'):self.anchors.append(a['href'][1:])
  if tag=='img':self.images.append(a['src'])
g=Guide();g.feed((root/'server/wwwroot/quick-start.html').read_text())
assert len(g.ids)==9 and set(g.anchors)<=set(g.ids)
for name in g.images:assert (root/'server/wwwroot'/name).is_file(),name
for name in ['README.md','BUILD-LOCALLY.md','linux/README.md','ios/README.md']:
 p=root/name
 for target in re.findall(r'\]\(([^)]+)\)',p.read_text()):
  if ':' in target or target.startswith('#'):continue
  assert (p.parent/target.split('#')[0]).exists(),f'Broken documentation link: {name}: {target}'
assert 'FEATURE-FREEZE.md' in (root/'README.md').read_text()
assert '2.0.39-source.zip' not in (root/'BUILD-LOCALLY.md').read_text()
for name in ['installer/HomeLibrary.nsi','linux/build-packages.py']:
 s=(root/name).read_text();assert 'INSTALLED-README.md' in s and 'ADVANCED-OPERATIONS.md' in s
assert "server/wwwroot/quick-start.html" in (root/'scripts/build-quick-start-pdf.py').read_text()
print(f'PASS {len(topics)} unique current Help topics; numbered core tasks, current links, nine illustrated sections and installed guide inclusion')

# Customer-facing About and installed-version footer must not drift into release-planning copy.
about=next(t for t in topics if t['title'].startswith('About Carribrary'))['body']
assert '© 2026 Chezburber Studios' in about and re.search(r'Version="([^"]+)"',(root/'server/SetupStatus.cs').read_text())[1] in about
for phrase in ['must be published','invited testers','planned as','feature set is frozen']:
 assert phrase not in about

# Check release labels beyond the Help footer: old guides must fail before packaging.
version=j['release']
guide=(root/'server/wwwroot/quick-start.html').read_text()
assert '<title>Carribrary '+version+' quick start</title>' in guide
assert 'Illustrated quick start · '+version+'<' in guide
assert 'current program-only compatibility is <strong>'+version+' only</strong>' in guide
core=(root/'installer/Recovery.Core.ps1').read_text()
expected="return $Version -match '^"+re.escape(version)+"(?:[.+-].*)?$'"
assert expected in core,'Program-recovery allowlist must match the documented current-only cohort'
assert '7=>patch>=53&&patch<='+version.split('.')[-1]+',' in (root/'server/RecoveryCompatibility.cs').read_text(),'Complete-backup cohort must be reviewed for the current release'
label=(root/'installer/Recover-Previous.ps1').read_text()
assert 'Supported program versions: '+version+' only (library format 7)' in label
print('PASS Quick Start title/header/recovery guidance and Windows recovery allowlist match the current release')
