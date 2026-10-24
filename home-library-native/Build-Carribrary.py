#!/usr/bin/env python3
"""Local package builder. Does not install/update a live server, upload, or create signing keys."""
from __future__ import annotations
import argparse, hashlib, importlib.util, json, os, platform, re, shutil, subprocess, sys
from pathlib import Path

PROJECT = Path(__file__).resolve().parent
WORKSPACE = PROJECT.parent
TOOLS = WORKSPACE / '.build-tools'
WINDOWS = os.name == 'nt'


def find_tool(override, name, candidates=()):
    value = os.environ.get(override)
    if value:
        found = shutil.which(value) or (str(Path(value).resolve()) if Path(value).is_file() else None)
        return found
    for candidate in candidates:
        if Path(candidate).is_file():
            return str(Path(candidate).resolve())
    return shutil.which(name)


def version():
    match = re.search(r'Version="([0-9]+\.[0-9]+\.[0-9]+(?:-preview)?)"', (PROJECT / 'server/SetupStatus.cs').read_text(encoding='utf-8'))
    if not match:
        raise RuntimeError('Cannot identify the source release.')
    return match[1]


def targets_for_host():
    return ['windows', 'android'] if WINDOWS else ['windows', 'linux', 'pi', 'android']


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--target', choices=['windows', 'linux', 'pi', 'android', 'all'])
    parser.add_argument('--check', action='store_true', help='Check prerequisites; do not compile.')
    parser.add_argument('--dry-run', action='store_true', help='Show build phases; do not compile.')
    parser.add_argument('--offline', action='store_true', help='Reuse restored .NET assets and cached Gradle dependencies.')
    parser.add_argument('--unsigned', action='store_true', help='Build unsigned Android outputs for developer use; not a phone update.')
    args = parser.parse_args(argv)
    target = args.target
    if not target:
        if not sys.stdin.isatty():
            parser.print_help()
            return 2
        options = targets_for_host() + ['all']
        print('Carribrary local builder. Your installed library will not be changed.')
        for i, item in enumerate(options, 1):
            print(f'{i}. {item}')
        choice = input('Package to build (number, or Enter to cancel): ').strip()
        if not choice:
            return 0
        if not choice.isdigit() or not 1 <= int(choice) <= len(options):
            print('Choose one of the displayed numbers.')
            return 2
        target = options[int(choice)-1]
    selected = targets_for_host() if target == 'all' else [target]
    if any(t in selected for t in ['linux', 'pi']) and platform.system() != 'Linux':
        print('Flatpak/DEB packaging requires Linux. Use the Linux laptop or a Linux/WSL environment; Windows and Android builds work on Windows.')
        return 2
    release = version()
    # All outputs remain in this source tree or its parent download folder.
    output = WORKSPACE
    logs = PROJECT / '.build-cache' / ('logs-' + release)
    env = os.environ.copy()
    env['PYTHONUTF8'] = '1'  # Canonical help/source files are UTF-8 on Windows too.
    bundled_dotnet = TOOLS / 'dotnet' / ('dotnet.exe' if WINDOWS else 'dotnet')
    dotnet = find_tool('CARRIBRARY_DOTNET', 'dotnet', [bundled_dotnet])
    if dotnet and Path(dotnet).resolve() == bundled_dotnet.resolve():
        env['DOTNET_ROOT'] = str(bundled_dotnet.parent)
        env['DOTNET_CLI_HOME'] = str(TOOLS / 'dotnet-home')
        env['NUGET_PACKAGES'] = str(TOOLS / 'nuget')
    nsis = find_tool('CARRIBRARY_MAKENSIS', 'makensis', [TOOLS/'nsis/usr/bin/makensis', Path(os.environ.get('ProgramFiles(x86)', 'C:/Program Files (x86)'))/'NSIS/makensis.exe', Path(os.environ.get('ProgramFiles', 'C:/Program Files'))/'NSIS/makensis.exe'])
    flatpak = find_tool('FLATPAK', 'flatpak', [TOOLS/'flatpak-tools/usr/bin/flatpak'])
    compose = find_tool('APPSTREAM_COMPOSE', 'appstreamcli', [TOOLS/'flatpak-tools/usr/libexec/appstreamcli-compose'])
    if not compose:
        compose = shutil.which('appstreamcli-compose')
    gradle = find_tool('CARRIBRARY_GRADLE', 'gradle', [TOOLS/'gradle-8.11.1/bin'/('gradle.bat' if WINDOWS else 'gradle')])
    if not gradle and (PROJECT/'android/gradle/wrapper/gradle-wrapper.jar').is_file():
        gradle = str(PROJECT/'android'/('gradlew.bat' if WINDOWS else 'gradlew'))
    java_candidates = list((TOOLS/'jdk').glob('*/bin/java')) if (TOOLS/'jdk').is_dir() else []
    if os.environ.get('JAVA_HOME'):
        java_candidates.insert(0, Path(os.environ['JAVA_HOME'])/'bin'/('java.exe' if WINDOWS else 'java'))
    java = find_tool('CARRIBRARY_JAVA', 'java', java_candidates)
    sdk = Path(os.environ.get('ANDROID_HOME') or os.environ.get('ANDROID_SDK_ROOT') or (TOOLS/'sdk' if (TOOLS/'sdk').is_dir() else Path(os.environ.get('LOCALAPPDATA', str(Path.home()))) / ('Android/Sdk' if WINDOWS else 'Android/Sdk')))
    build_tools = sdk/'build-tools/35.0.0'
    key = Path(os.environ.get('CARRIBRARY_KEYSTORE', str(TOOLS/'signing/home-library-release.p12')))
    password_file = Path(os.environ.get('CARRIBRARY_KEY_PASSWORD_FILE', str(TOOLS/'signing/signing-password.txt')))
    required = {}
    if any(t in selected for t in ['windows', 'linux', 'pi']):
        required['.NET SDK 10'] = dotnet
    if 'windows' in selected:
        required['NSIS compiler'] = nsis
    if 'linux' in selected:
        required['Flatpak CLI'] = flatpak
        required['AppStream compose'] = compose
    if any(t in selected for t in ['linux', 'pi']):
        required['dpkg-deb'] = shutil.which('dpkg-deb')
        required['Python Pillow'] = importlib.util.find_spec('PIL') is not None
    if 'android' in selected:
        required.update({'Gradle 8.11.1 / project wrapper': gradle, 'JDK 17 or compatible': java, 'Android platform 36': (sdk/'platforms/android-36/android.jar').is_file(), 'Android build-tools 35.0.0': build_tools.is_dir()})
        if not args.unsigned:
            required['EXISTING Android signing key (not generated)'] = key.is_file()
            required['Private signing-password file (contents never printed)'] = password_file.is_file()
    missing = [label for label, found in required.items() if not found]
    print(f'Carribrary {release}: '+', '.join(selected))
    for label, found in required.items():
        print(('OK      ' if found else 'MISSING ') + label)
    if missing:
        print('Install/configure the missing prerequisites described in BUILD-LOCALLY.md. Nothing was compiled or installed.')
        return 1
    if args.check:
        print('Prerequisite locations found. Build commands will also validate actual tool versions/compatibility. No compilation performed.')
        return 0
    if args.dry_run:
        for name in selected:
            print('Would build: '+name)
        print('Outputs: '+str(output)+'; logs: '+str(logs))
        return 0
    logs.mkdir(parents=True, exist_ok=True)
    artifacts = []
    completed = []

    def run(label, command, cwd=PROJECT):
        print('Running '+label+'...', flush=True)
        with (logs/(label+'.log')).open('w', encoding='utf-8') as log:
            result = subprocess.run([str(arg) for arg in command], cwd=cwd, env=env, stdout=log, stderr=subprocess.STDOUT)
        if result.returncode:
            raise RuntimeError(label+' failed. See '+str(logs/(label+'.log')))
        completed.append(label)

    def publish(rid, folder, tray=False):
        project = PROJECT/('tray/HomeLibrary.Tray.csproj' if tray else 'server/HomeLibrary.Server.csproj')
        command = [dotnet, 'publish', project, '-c', 'Release', '-r', rid, '--self-contained', 'true', '--disable-build-servers', '-o', PROJECT/'release'/folder]
        if args.offline:
            command.append('--no-restore')
        run('publish-'+('tray' if tray else rid), command)

    try:
        run('synchronized-help-check', [sys.executable, PROJECT/'scripts/sync-help.py', '--check'])
        if 'windows' in selected:
            publish('win-x64', 'server')
            publish('win-x64', 'tray', tray=True)
            run('windows-program-manifest', [sys.executable, PROJECT/'scripts/package-manifest.py'])
            prefix = '/' if WINDOWS else '-'
            if not WINDOWS and Path(nsis).resolve() == (TOOLS/'nsis/usr/bin/makensis').resolve():
                env['NSISDIR'] = str(TOOLS/'nsis/usr/share/nsis')
            filename = output/f'Carribrary-Setup-{release}.exe'
            run('windows-installer', [nsis, prefix+'V2', prefix+'DPROJECT='+str(PROJECT), prefix+'DPAYLOAD='+str(PROJECT/'release'), prefix+'DOUTPUT='+str(filename), PROJECT/'installer/HomeLibrary.nsi'])
            artifacts.append(filename)
        if any(t in selected for t in ['linux', 'pi']):
            if 'linux' in selected:
                publish('linux-x64', 'linux-x64')
            if 'pi' in selected:
                publish('linux-arm64', 'linux-arm64')
            env.update(XDG_CACHE_HOME=str(PROJECT/'.build-cache/cache'), XDG_CONFIG_HOME=str(PROJECT/'.build-cache/config'), XDG_DATA_HOME=str(PROJECT/'.build-cache/data'))
            if flatpak:
                env['FLATPAK'] = flatpak
            if compose:
                env['APPSTREAM_COMPOSE'] = compose
            if flatpak and Path(flatpak).resolve() == (TOOLS/'flatpak-tools/usr/bin/flatpak').resolve():
                env['LD_LIBRARY_PATH'] = str(TOOLS/'flatpak-tools/usr/lib/x86_64-linux-gnu')+os.pathsep+env.get('LD_LIBRARY_PATH', '')
            only = 'all' if 'linux' in selected and 'pi' in selected else 'flatpak' if 'linux' in selected else 'pi'
            run('linux-packages', [sys.executable, PROJECT/'linux/build-packages.py', '--only', only])
            if 'linux' in selected:
                artifacts.append(output/f'Carribrary-Linux-x86_64-{release}.flatpak')
            if 'pi' in selected:
                artifacts.append(output/f'Carribrary-Pi-ARM64-{release}.deb')
        if 'android' in selected:
            env.update(JAVA_HOME=str(Path(java).resolve().parent.parent), ANDROID_HOME=str(sdk), ANDROID_SDK_ROOT=str(sdk))
            if (TOOLS/'gradle-home').is_dir():
                env['GRADLE_USER_HOME'] = str(TOOLS/'gradle-home')
            env['PATH'] = str(Path(java).resolve().parent)+os.pathsep+env.get('PATH', '')
            command = [gradle, '--no-daemon', 'assembleRelease', 'bundleRelease', 'lintRelease']
            if args.offline:
                command.insert(1, '--offline')
            run('android-build-and-lint', command, PROJECT/'android')
            apk_in = PROJECT/'android/app/build/outputs/apk/release/app-release-unsigned.apk'
            aab_in = PROJECT/'android/app/build/outputs/bundle/release/app-release.aab'
            if args.unsigned:
                for src, suffix in [(apk_in, 'apk'), (aab_in, 'aab')]:
                    dst = output/f'Carribrary-Android-{release}-UNSIGNED.{suffix}'
                    shutil.copy2(src, dst)
                    artifacts.append(dst)
            else:
                apk = output/f'Carribrary-Android-{release}.apk'
                aab = output/f'Carribrary-Android-{release}.aab'
                apksigner = build_tools/('apksigner.bat' if WINDOWS else 'apksigner')
                zipalign = build_tools/('zipalign.exe' if WINDOWS else 'zipalign')
                jarsigner = Path(java).resolve().parent/('jarsigner.exe' if WINDOWS else 'jarsigner')
                run('android-sign-apk', [apksigner, 'sign', '--ks', key, '--ks-key-alias', os.environ.get('CARRIBRARY_KEY_ALIAS', 'home-library'), '--ks-pass', 'file:'+str(password_file), '--out', apk, apk_in])
                run('android-verify-apk', [apksigner, 'verify', '--verbose', apk])
                run('android-align-apk', [zipalign, '-c', '-P', '16', '4', apk])
                baseline = os.environ.get('CARRIBRARY_PREVIOUS_APK')
                if baseline:
                    def digest(path):
                        reply = subprocess.run([str(apksigner), 'verify', '--print-certs', str(path)], env=env, capture_output=True, text=True, check=True)
                        match = re.search(r'certificate SHA-256 digest: ([a-f0-9]+)', reply.stdout)
                        if not match:
                            raise RuntimeError('Cannot verify Android signing identity.')
                        return match[1]
                    if digest(apk) != digest(baseline):
                        raise RuntimeError('Signing key differs from the previous APK. Do not install this as an update.')
                shutil.copy2(aab_in, aab)
                run('android-sign-aab', [jarsigner, '-keystore', key, '-storepass:file', password_file, aab, os.environ.get('CARRIBRARY_KEY_ALIAS', 'home-library')])
                run('android-verify-aab', [jarsigner, '-verify', aab])
                artifacts.extend([apk, aab])
        manifest = output/f'Carribrary-{release}-local-SHA256.txt'
        manifest.write_text(''.join(hashlib.sha256(p.read_bytes()).hexdigest()+'  '+p.name+'\n' for p in artifacts), encoding='utf-8')
        summary = {'release': release, 'status': 'build-success', 'targets': selected, 'completedSteps': completed, 'outputs': [p.name for p in artifacts], 'physicalDeviceTesting': 'not performed', 'published': False}
        (logs/'build-summary.json').write_text(json.dumps(summary, indent=2)+'\n', encoding='utf-8')
        print('BUILD COMPLETE. No installation or upload performed.')
        for artifact in artifacts:
            print(str(artifact))
        print('Summary: '+str(logs/'build-summary.json'))
        return 0
    except (OSError, RuntimeError, subprocess.SubprocessError) as error:
        (logs/'build-summary.json').write_text(json.dumps({'release': release, 'status': 'failed', 'completedSteps': completed, 'error': str(error)}, indent=2)+'\n', encoding='utf-8')
        print(str(error))
        print('Your installed library was not changed. Review the named log before sharing an error excerpt; keep credentials/signing files private.')
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
