#!/usr/bin/env python3
"""Publishing metadata/notice delivery contracts; not a legal certification."""
from pathlib import Path
import json,xml.etree.ElementTree as E
r=Path(__file__).resolve().parents[1]
license=(r/'LICENSE.txt').read_text()
assert 'Chezburber Studios' in license and 'Third-party rights take precedence' in license
assert 'not an open-source' in license and 'without a software license fee' in license
for n in ['server/HomeLibrary.Server.csproj','tray/HomeLibrary.Tray.csproj']:
 assert E.parse(r/n).findtext('.//Company')=='Chezburber Studios'
assert E.parse(r/'linux/flatpak/com.household.HomeLibrary.metainfo.xml').findtext('.//developer/name')=='Chezburber Studios'
assert 'Maintainer: Chezburber Studios <chezburbermerch@gmail.com>' in (r/'linux/build-packages.py').read_text()
for n in ['play-store/STORE-LISTING.md','play-store/PRIVACY-POLICY-DRAFT.txt','play-store/privacy-policy-draft.html']:
 assert '[DEVELOPER NAME' not in (r/n).read_text()
j=json.loads((r/'distribution/THIRD-PARTY-INVENTORY-2.0.53.json').read_text());a=[x for x in j['components'] if x['platform']=='Android']
assert len(a)==j['androidArtifactCount']==100
assert len({x['component'] for x in a})==len(a)
assert all(x['licenses'] and all(l['name'] and l['url'] for l in x['licenses']) for x in a)
for x in a:
 for p in x['embeddedNotices']:assert (r/p).is_file()
assert any(any('ML Kit' in l['name'] for l in x['licenses']) for x in a)
assert (r/'licenses/WEBPUSH-SOURCE-1.0.13.zip').is_file()
for n in ['LIBYUV-LICENSE.txt','APACHE-2.0.txt','SQLITEPCLRAW-LICENSE.txt','NAYUKI-QR-LICENSE.txt']:
 assert (r/'licenses'/n).stat().st_size>100
for n in ['LICENSE.txt','THIRD-PARTY-NOTICES.txt']:
 assert (r/'android/app/src/main/assets/legal'/n).read_bytes()==(r/n).read_bytes()
h=json.loads((r/'assets/help.json').read_text());pair=next(x for x in h['topics'] if x['title']=='Pair the optional Android librarian companion')['body']
assert 'Google Play' in pair and 'Chezburber Studios' in pair and 'Install the matching Carribrary Android APK' not in pair
assert any(x['title']=='Third-party licenses and credits' for x in h['topics'])
assert 'Pricing: US$15.00 one-time paid download' in (r/'play-store/STORE-LISTING.md').read_text()
assert 'Owner-confirmed Android base price: **US$15.00' in (r/'distribution/PUBLICATION-PLAN.md').read_text()
notice=(r/'assets/privacy.txt').read_text()
assert (r/'android/app/src/main/assets/privacy.txt').read_text()==notice
for stale in ['[DEVELOPER','[PUBLIC SUPPORT','internal test draft','must be published','before Play submission','no automated email verification or recovery provider']:
 assert stale not in notice
assert 'Encrypted personal lists' in notice and 'email recovery' in notice and 'Optional borrower self-service' in notice
print('PASS publishing identity, installer-first Help, 100-component Android inventory, separate third-party rights and packaged legal assets. Final signed artifact/legal review remains required.')
