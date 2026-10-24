"""Hash the published Windows programs before NSIS packaging; contains no runtime data."""
from pathlib import Path
import hashlib,json,re
root=Path(__file__).resolve().parents[1]
files=[]
for folder in ['server','tray']:
 for p in sorted((root/'release'/folder).rglob('*')):
  if p.is_symlink():raise ValueError('Release payload must not contain links')
  if p.is_file():files.append({'path':p.relative_to(root/'release').as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
assert any(f['path']=='server/HomeLibrary.Server.dll' for f in files)
assert any(f['path']=='tray/HomeLibrary.Tray.dll' for f in files)
(root/'release/release-files.json').write_text(json.dumps({'version':re.search(r'Version="([^"]+)"',(root/'server/SetupStatus.cs').read_text())[1],'files':files},indent=2)+'\n')
print(f'Hashed {len(files)} Windows program files for setup verification')
