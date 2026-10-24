#!/usr/bin/env python3
"""Archive the audited Git index under a versioned extraction folder; never build/install software."""
from pathlib import Path, PurePosixPath
import argparse, hashlib, json, re, subprocess, zipfile


def package(root, output=None):
    root=Path(root).resolve()
    version=re.search(r'Version="(\d+\.\d+\.\d+)(?:-preview)?"',(root/'home-library-native/server/SetupStatus.cs').read_text())[1]
    prefix=f'Carribrary-{version}'
    destination=Path(output).resolve() if output else root.parent/f'{prefix}-source.zip'
    names=subprocess.check_output(['git','-C',str(root),'ls-files','-z']).decode().split('\0')
    names=[name for name in names if name]
    required={'home-library-native/Build-Carribrary.py',f'Carribrary-Help-{version}.md'}
    if not required.issubset(names):raise ValueError('Source index is missing the builder or matching help guide.')
    for name in names:
        path=PurePosixPath(name);source=root/name
        if path.is_absolute() or '..' in path.parts or source.is_symlink() or not source.is_file():raise ValueError('Unsafe source entry: '+name)
        if any(part in {'.git','.build-tools','.build-cache','data','signing','bin','obj','build','release'} for part in path.parts) or path.suffix.lower() in {'.apk','.aab','.p12','.pfx','.jks','.keystore','.pem','.sqlite','.db','.exe','.flatpak','.deb'}:raise ValueError('Private/runtime or compiled source entry: '+name)
    if destination.exists():raise FileExistsError('Archive already exists; choose a new output path instead of overwriting it.')
    created=False
    try:
        with zipfile.ZipFile(destination,'x',compression=zipfile.ZIP_DEFLATED,compresslevel=6) as archive:
            created=True
            for name in sorted(names):archive.write(root/name,f'{prefix}/{name}')
        with zipfile.ZipFile(destination) as archive:
            if archive.testzip() is not None or len(archive.namelist())!=len(names):raise ValueError('Archive verification failed.')
            if any(not name.startswith(prefix+'/') for name in archive.namelist()):raise ValueError('Unversioned archive entry.')
    except Exception:
        if created:destination.unlink(missing_ok=True)
        raise
    return {'archive':str(destination),'folder':prefix,'files':len(names),'sha256':hashlib.sha256(destination.read_bytes()).hexdigest()}


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source-root',type=Path,default=Path(__file__).resolve().parents[2])
    parser.add_argument('--output',type=Path)
    args=parser.parse_args()
    print(json.dumps(package(args.source_root,args.output)))
