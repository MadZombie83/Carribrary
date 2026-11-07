from pathlib import Path
import importlib.util
import re
import xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('flatpak_metadata',root/'linux/flatpak_metadata.py')
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
template=(root/'linux/flatpak/com.household.HomeLibrary.metainfo.xml').read_text()
version=re.search(r'Version="([^"]+)"',(root/'server/SetupStatus.cs').read_text())[1]
# Simulate an unchanged old template while a future server is packaged.
for current in [version,'2.0.62-preview']:
 document=ET.fromstring(module.render_metainfo(template,current,'2026-11-04'))
 assert document.find('releases/release').get('version')==current
 assert len(document.findall(f"releases/release[@version='{current}']"))==1
 assert document.find('launchable').text=='com.household.HomeLibrary.desktop'
 assert document.find('project_license').text=='LicenseRef-proprietary'
 assert document.find("releases/release[@version='2.0.59-preview']") is not None
try:module.render_metainfo(template,'2.0.60\ninvalid')
except ValueError:pass
else:raise AssertionError('Invalid release accepted')
print('PASS Flatpak displayed release follows the actual packaged server, including a stale template and future-version build; history and branding preserved')
