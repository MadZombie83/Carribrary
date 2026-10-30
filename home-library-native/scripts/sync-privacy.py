#!/usr/bin/env python3
"""Sync the customer notice to Android and owner-review policy exports."""
from pathlib import Path
from html import escape
import sys
root=Path(__file__).resolve().parents[1];notice=(root/'assets/privacy.txt').read_text()
assert 'Publisher: Chezburber Studios' in notice and 'Privacy contact: chezburbermerch@gmail.com' in notice
assert '[DEVELOPER' not in notice and '[PUBLIC SUPPORT' not in notice
header='OWNER REVIEW DRAFT: Review this notice and publish/verify the actual privacy and deletion-page URLs before Play submission. This file does not publish a policy or certify legal/store compliance. Customer text below is synchronized with the Android notice.\n\n'
html='<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Carribrary privacy policy — owner review draft</title><body style="font-family:system-ui;max-width:48rem;margin:2rem auto;padding:1rem;line-height:1.6;overflow-wrap:anywhere"><p><strong>'+escape(header.strip())+'</strong></p>'
html+=''.join('<p>'+escape(t).replace('\n','<br>')+'</p>' for t in notice.split('\n\n') if t.strip())+'</body></html>\n'
for name,text in [('android/app/src/main/assets/privacy.txt',notice),('play-store/PRIVACY-POLICY-DRAFT.txt',header+notice),('play-store/privacy-policy-draft.html',html)]:
 p=root/name
 if '--check' in sys.argv:assert p.read_text()==text,'Privacy notice out of sync: '+name
 else:p.write_text(text)
print('PASS Android privacy notice and owner-review exports match the current canonical customer text')
