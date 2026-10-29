#!/usr/bin/env python3
"""Package already-published Linux payloads; no system installation or root required."""
import os, pathlib, shutil, subprocess, argparse, re
from PIL import Image
root=pathlib.Path(__file__).resolve().parents[1]
version=re.search(r'Version="([^"]+)"',(root/'server/SetupStatus.cs').read_text())[1]
parser=argparse.ArgumentParser();parser.add_argument('--only',choices=['all','flatpak','pi'],default='all');only=parser.parse_args().only
stage=root/'release/linux-packages'
stage.mkdir(parents=True,exist_ok=True)
app_id='com.household.HomeLibrary'
def copy(src,dst):
 dst.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(src,dst)
def tree(src,dst):shutil.copytree(src,dst,dirs_exist_ok=True)
def icon(dst):
 dst.parent.mkdir(parents=True,exist_ok=True)
 Image.open(root/'assets/home-library.png').resize((512,512),Image.Resampling.LANCZOS).save(dst)
if only in ['all','flatpak']:
 # Flatpak directory layout can be exported with the low-level Flatpak CLI.
 flat=stage/'flatpak'
 if flat.exists():shutil.rmtree(flat)
 tree(root/'release/linux-x64',flat/'files/lib/home-library')
 copy(root/'linux/flatpak/home-library',flat/'files/bin/home-library')
 (flat/'files/bin/home-library').chmod(0o755)
 recovery=flat/'files/share/applications/com.household.HomeLibrary.Recovery.desktop'
 recovery.parent.mkdir(parents=True,exist_ok=True)
 recovery.write_text('[Desktop Entry]\nType=Application\nName=Carribrary Complete Backup Recovery\nComment=Verify, test or restore a complete backup\nExec=home-library --recovery %u\nIcon=com.household.HomeLibrary\nCategories=Office;Database;\nTerminal=false\nMimeType=x-scheme-handler/carribrary-recovery;\n')
 public=flat/'files/share/applications/com.household.HomeLibrary.PublicSharing.desktop'
 public.write_text('[Desktop Entry]\nType=Application\nName=Carribrary Public Sharing Setup\nComment=Guided hostname, HTTPS and router setup\nExec=home-library --public-sharing\nIcon=com.household.HomeLibrary\nCategories=Office;Network;\nTerminal=false\n')
 copy(root/'linux/flatpak/com.household.HomeLibrary.desktop',flat/f'files/share/applications/{app_id}.desktop')
 copy(root/'linux/flatpak/com.household.HomeLibrary.metainfo.xml',flat/f'files/share/metainfo/{app_id}.metainfo.xml')
 icon(flat/f'files/share/icons/hicolor/512x512/apps/{app_id}.png')
 copy(root/'linux/README.md',flat/'files/share/doc/home-library/README.md')
 copy(root/'THIRD-PARTY-NOTICES.txt',flat/'files/share/doc/home-library/THIRD-PARTY-NOTICES.txt')
 tree(root/'licenses',flat/'files/share/doc/home-library/licenses')
 (flat/'metadata').write_text(f'[Application]\nname={app_id}\nruntime=org.freedesktop.Platform/x86_64/25.08\nsdk=org.freedesktop.Sdk/x86_64/25.08\ncommand=home-library\n\n[Context]\nshared=network;\n\n[Environment]\nDOTNET_CLI_TELEMETRY_OPTOUT=1\n')
if only in ['all','pi']:
 # Pi: a self-contained ARM64 server managed by systemd.
 deb=stage/'pi'
 if deb.exists():shutil.rmtree(deb)
 tree(root/'release/linux-arm64',deb/'opt/home-library/server')
 control=deb/'DEBIAN';control.mkdir()
 (control/'control').write_text(f'Package: home-library-server\nVersion: {version.replace("-preview","~preview")}\nArchitecture: arm64\nMaintainer: Carribrary <home-library@localhost>\nSection: misc\nPriority: optional\nRecommends: xdg-utils\nDepends: adduser, pkexec, libc6 (>= 2.35), libgcc-s1, libstdc++6, zlib1g, libssl3 | libssl3t64, libicu72 | libicu76 | libicu74 | libicu78 | libicu80\nDescription: Household library server and Android companion API\n Self-contained ASP.NET Core and SQLite server for 64-bit Raspberry Pi OS.\n')
 for name in ['postinst','prerm','postrm']:
  copy(root/f'linux/pi/{name}',control/name);(control/name).chmod(0o755)
 copy(root/'linux/pi/home-library-recovery',deb/'usr/bin/home-library-recovery');(deb/'usr/bin/home-library-recovery').chmod(0o755)
 recovery=deb/'usr/share/applications/com.household.HomeLibrary.Recovery.desktop';recovery.parent.mkdir(parents=True,exist_ok=True)
 recovery.write_text('[Desktop Entry]\nType=Application\nName=Carribrary Complete Backup Recovery\nComment=Verify, test or restore a complete backup\nExec=home-library-recovery %u\nIcon=com.household.HomeLibrary\nCategories=Office;Database;\nTerminal=true\nMimeType=x-scheme-handler/carribrary-recovery;\n')
 public=deb/'usr/share/applications/com.household.HomeLibrary.PublicSharing.desktop'
 public.write_text('[Desktop Entry]\nType=Application\nName=Carribrary Public Sharing Setup\nExec=xdg-open http://localhost:4173/?public-setup=1\nIcon=com.household.HomeLibrary\nCategories=Office;Network;\nTerminal=false\n')
 copy(root/'linux/pi/home-library.service',deb/'usr/lib/systemd/system/home-library.service')
 copy(root/'linux/pi/home-library.env',deb/'etc/home-library/home-library.env')
 (control/'conffiles').write_text('/etc/home-library/home-library.env\n')
 copy(root/'linux/README.md',deb/'usr/share/doc/home-library-server/README.md')
 copy(root/'THIRD-PARTY-NOTICES.txt',deb/'usr/share/doc/home-library-server/THIRD-PARTY-NOTICES.txt')
 tree(root/'licenses',deb/'usr/share/doc/home-library-server/licenses')
 copy(root/'linux/Caddyfile.example',deb/'usr/share/doc/home-library-server/Caddyfile.example')
 launcher=deb/'usr/bin/home-library';launcher.parent.mkdir(parents=True,exist_ok=True)
 launcher.write_text('#!/bin/sh\nexec xdg-open http://localhost:4173\n');launcher.chmod(0o755)
 desktop=deb/f'usr/share/applications/{app_id}.desktop';desktop.parent.mkdir(parents=True,exist_ok=True)
 desktop.write_text(f'[Desktop Entry]\nType=Application\nName=Carribrary\nComment=Open the household library admin\nExec=home-library\nIcon={app_id}\nCategories=Office;Database;\nTerminal=false\n')
 icon(deb/f'usr/share/icons/hicolor/512x512/apps/{app_id}.png')
 subprocess.run(['dpkg-deb','--root-owner-group','--build',str(deb),str(root.parent/f'Carribrary-Pi-ARM64-{version}.deb')],check=True)
if only in ['all','flatpak']:
 flatpak=os.environ.get('FLATPAK','flatpak')
 compose=os.environ.get('APPSTREAM_COMPOSE','appstreamcli')
 compose_command=[compose] if 'appstreamcli-compose' in pathlib.Path(compose).name else [compose,'compose']
 subprocess.run(compose_command+['--no-net','--prefix=/','--origin=flatpak',
  '--data-dir='+str(flat/'files/share/app-info/xmls'),
  '--icons-dir='+str(flat/'files/share/app-info/icons/flatpak'),
  '--hints-dir='+str(stage/'appstream-report'),str(flat/'files')],check=True)
 (flat/'files/share/app-info/xmls/flatpak.xml.gz').rename(flat/f'files/share/app-info/xmls/{app_id}.xml.gz')
 repo=stage/'repo'
 subprocess.run([flatpak,'build-finish',str(flat)],check=True)
 subprocess.run([flatpak,'build-export',str(repo),str(flat),'stable'],check=True)
 subprocess.run([flatpak,'build-update-repo',str(repo)],check=True)
 subprocess.run([flatpak,'build-bundle','--runtime-repo=https://dl.flathub.org/repo/flathub.flatpakrepo',str(repo),str(root.parent/f'Carribrary-Linux-x86_64-{version}.flatpak'),app_id,'stable'],check=True)
 print('Created requested Linux/Pi packages.')
