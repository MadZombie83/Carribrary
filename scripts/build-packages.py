#!/usr/bin/env python3
"""Build Carribrary with this computer's existing tools and versioned output."""
from pathlib import Path
import os
import re
import subprocess
import sys

root = Path(__file__).resolve().parents[1]
source = root / "home-library-native"
tools = root / ".tools"
version_match = re.search(r'Version="([^"]+)"', (source / "server/SetupStatus.cs").read_text())
if not version_match:
    raise SystemExit("Cannot read Carribrary's source version.")
version = version_match.group(1)
environment = os.environ.copy()
environment["CARRIBRARY_TOOLS"] = str(tools.resolve())
environment["ANDROID_HOME"] = str(tools / "sdk")
environment["ANDROID_SDK_ROOT"] = str(tools / "sdk")
arguments = sys.argv[1:]
if "--output-dir" in arguments:
    raise SystemExit("This launcher puts packages in release/VERSION. Use the source builder directly for a custom location.")
if "--target" not in arguments:
    arguments = ["--target", "all", *arguments]
command = [
    sys.executable, str(source / "Build-Carribrary.py"),
    "--output-dir", str(root / "release" / version), *arguments,
]
raise SystemExit(subprocess.call(command, cwd=source, env=environment))
