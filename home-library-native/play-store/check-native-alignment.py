#!/usr/bin/env python3
"""Check 64-bit ELF load segments in an Android APK or AAB (stdlib only)."""
import pathlib, struct, sys, zipfile

def check(data):
    if data[:5] != b'\x7fELF\x02':
        raise ValueError('Expected ELF64')
    endian = '<' if data[5] == 1 else '>'
    offset = struct.unpack_from(endian+'Q', data, 32)[0]
    size, count = struct.unpack_from(endian+'HH', data, 54)
    loads = 0
    for index in range(count):
        header = offset + size*index
        if struct.unpack_from(endian+'I', data, header)[0] != 1:
            continue
        loads += 1
        file_offset, address = struct.unpack_from(endian+'QQ', data, header+8)
        alignment = struct.unpack_from(endian+'Q', data, header+48)[0]
        if alignment < 16384 or file_offset % 16384 != address % 16384:
            raise ValueError(f'Load segment has insufficient alignment: {alignment}')
    if not loads:
        raise ValueError('No load segments')

if __name__ == '__main__':
    count = 0
    with zipfile.ZipFile(pathlib.Path(sys.argv[1])) as archive:
        for name in archive.namelist():
            if name.endswith('.so') and any('/'+abi+'/' in name for abi in ('arm64-v8a','x86_64')):
                check(archive.read(name)); count += 1
                print('PASS', name)
    if not count:
        raise SystemExit('No 64-bit native libraries found')
    print(f'Passed: {count} ELF64 libraries. ZIP alignment and runtime tests are separate.')
