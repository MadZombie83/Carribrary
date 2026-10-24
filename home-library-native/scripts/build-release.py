#!/usr/bin/env python3
"""Legacy workspace-specific helper; prefer ../Build-Carribrary.py on other computers.
Bounded, repeatable packaging phases using the project's existing toolchains/scripts.
Usage: build-release.py publish|windows|linux|android-sign
Tool locations default to workspace .build-tools and can be overridden with environment variables.
No phase installs software, touches a running library, publishes a release or creates signing keys.
"""
from pathlib import Path
import os,sys,subprocess,re,hashlib,zipfile
root=Path(__file__).resolve().parents[2];project=root/'home-library-native';tools=root/'.build-tools';logs=tools/'release-233';logs.mkdir(exist_ok=True)
version=re.search(r'Version="([^"]+)"',(project/'server/SetupStatus.cs').read_text())[1]
assert re.fullmatch(r'\d+\.\d+\.\d+-preview',version)
env=os.environ.copy();env.update(DOTNET_ROOT=str(tools/'dotnet'),DOTNET_CLI_HOME=str(tools/'dotnet-home'),NUGET_PACKAGES=str(tools/'nuget'))
def run(name,args,**kwargs):
 print(name,flush=True)
 with open(logs/(name+'.log'),'w') as log:subprocess.run([str(x) for x in args],env=env,stdout=log,stderr=subprocess.STDOUT,check=True,**kwargs)
dotnet=os.environ.get('CARRIBRARY_DOTNET',str(tools/'dotnet/dotnet'))
phase=sys.argv[1]
if phase=='publish':
 for rid,folder in [('win-x64','server'),('linux-x64','linux-x64'),('linux-arm64','linux-arm64')]:
  run('publish-'+rid,[dotnet,'publish',project/'server/HomeLibrary.Server.csproj','-c','Release','-r',rid,'--self-contained','true','--no-restore','--disable-build-servers','-o',project/'release'/folder])
 run('publish-tray',[dotnet,'publish',project/'tray/HomeLibrary.Tray.csproj','-c','Release','-r','win-x64','--self-contained','true','--no-restore','--disable-build-servers','-o',project/'release/tray'])
elif phase=='windows':
 run('manifest',[sys.executable,project/'scripts/package-manifest.py'])
 env['NSISDIR']=str(tools/'nsis/usr/share/nsis')
 run('nsis',[os.environ.get('CARRIBRARY_MAKENSIS',str(tools/'nsis/usr/bin/makensis')),'-V2','-DPROJECT='+str(project),'-DPAYLOAD='+str(project/'release'),'-DOUTPUT='+str(root/f'Carribrary-Setup-{version}.exe'),project/'installer/HomeLibrary.nsi'])
elif phase=='linux':
 env.update(FLATPAK=str(tools/'flatpak-tools/usr/bin/flatpak'),APPSTREAM_COMPOSE=str(tools/'flatpak-tools/usr/libexec/appstreamcli-compose'),LD_LIBRARY_PATH=str(tools/'flatpak-tools/usr/lib/x86_64-linux-gnu')+os.pathsep+env.get('LD_LIBRARY_PATH',''))
 run('linux-packages',[sys.executable,project/'linux/build-packages.py'])
elif phase=='android-sign':
 java=next((tools/'jdk').iterdir());env.update(JAVA_HOME=str(java));env['PATH']=str(java/'bin')+os.pathsep+env.get('PATH','')
 key=tools/'signing/home-library-release.p12';password=tools/'signing/signing-password.txt';assert key.is_file() and password.is_file(),'Use existing signing identity; never regenerate it'
 sdk=tools/'sdk/build-tools/35.0.0';apk=root/f'Carribrary-Android-{version}.apk';aab=root/f'Carribrary-Android-{version}.aab'
 run('apk-sign',[sdk/'apksigner','sign','--ks',key,'--ks-key-alias','home-library','--ks-pass','file:'+str(password),'--out',apk,project/'android/app/build/outputs/apk/release/app-release-unsigned.apk'])
 run('apk-verify',[sdk/'apksigner','verify','--verbose',apk]);run('apk-align',[sdk/'zipalign','-c','-P','16','4',apk])
 # Capture identity information privately; print only equality, never certificate subject/email.
 def signer(path):
  result=subprocess.run([sdk/'apksigner','verify','--print-certs',path],env=env,capture_output=True,text=True,check=True)
  return re.search(r'certificate SHA-256 digest: ([a-f0-9]+)',result.stdout)[1]
 assert signer(apk)==signer(root/'Carribrary-Android-2.0.32-preview.apk'),'Signing identity changed'
 import shutil
 shutil.copy2(project/'android/app/build/outputs/bundle/release/app-release.aab',aab)
 run('aab-sign',[java/'bin/jarsigner','-keystore',key,'-storetype','PKCS12','-storepass:file',password,aab,'home-library'])
 run('aab-verify',[java/'bin/jarsigner','-verify',aab])
 print('PASS matching APK identity, signature/alignment and AAB signature',flush=True)
else:raise SystemExit('Unknown packaging phase')
