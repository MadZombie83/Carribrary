"""Structural checks only: not an iOS compile or XCTest substitute."""
from pathlib import Path
import json, plistlib, re, xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[1]
project=(root/'HomeLibrary.xcodeproj/project.pbxproj').read_text()
ids=re.findall(r'^([A-F0-9]{24}) =',project,re.M)
assert len(ids)==len(set(ids))
assert set(re.findall(r'\b[A-F0-9]{24}\b',project))==set(ids)
for file in list((root/'HomeLibrary').glob('*.swift'))+list((root/'HomeLibraryTests').glob('*.swift')):
    assert f'path = "{file.name}";' in project, file
info=plistlib.loads((root/'HomeLibrary/Info.plist').read_bytes())
assert 'NSCameraUsageDescription' in info and 'NSLocalNetworkUsageDescription' in info
assert 'NSAppTransportSecurity' not in info
privacy=plistlib.loads((root/'HomeLibrary/PrivacyInfo.xcprivacy').read_bytes())
assert not privacy['NSPrivacyTracking']
for path in root.rglob('Contents.json'): json.loads(path.read_text())
ET.parse(root/'HomeLibrary.xcodeproj/xcshareddata/xcschemes/HomeLibrary.xcscheme')
assert (root/'HomeLibrary/Assets.xcassets/AppIcon.appiconset/AppIcon.png').exists()
print('Project references, scheme, permission/transport plists, privacy manifest and assets: PASS')
try:
    from tree_sitter import Language, Parser
    import tree_sitter_swift
except ImportError:
    print('Swift grammar parsing skipped: optional tree-sitter packages unavailable.')
else:
    parser=Parser(Language(tree_sitter_swift.language()))
    files=list(root.rglob('*.swift'))
    for path in files:
        tree=parser.parse(path.read_bytes())
        assert not tree.root_node.has_error, f'Swift grammar error: {path}'
    print(f'Swift grammar parsing: PASS ({len(files)} files); no type checking or platform tests performed.')
