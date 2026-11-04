"""Keep Flatpak's displayed release aligned with the packaged server."""
import datetime
import re
import xml.etree.ElementTree as ET


def render_metainfo(template, version, release_date=None):
    if not re.fullmatch(r'\d+\.\d+\.\d+(?:-preview)?', version):
        raise ValueError('Invalid server release version')
    day = release_date or datetime.date.today().isoformat()
    datetime.date.fromisoformat(day)
    component = ET.fromstring(template)
    if component.tag != 'component' or component.findtext('id') != 'com.household.HomeLibrary':
        raise ValueError('Unexpected Flatpak component')
    releases = component.find('releases')
    if releases is None:
        releases = ET.SubElement(component, 'releases')
    for release in list(releases):
        if release.get('version') == version:
            releases.remove(release)
    releases.insert(0, ET.Element('release', version=version, date=day))
    ET.indent(component, space=' ')
    return '<?xml version="1.0" encoding="UTF-8"?>\n' + ET.tostring(component, encoding='unicode') + '\n'
