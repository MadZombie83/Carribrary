#!/usr/bin/env python3
"""Run the dependency-free Android resolver checks with the cached Kotlin compiler.
Usage: python3 tests/run-circulation-kotlin.py /path/to/gradle-user-home /path/to/jdk
The production Android build remains Gradle's responsibility.
"""
from pathlib import Path
import os, subprocess, sys, tempfile
root=Path(__file__).resolve().parents[1]
cache=Path(sys.argv[1])/'caches/modules-2/files-2.1'
java=Path(sys.argv[2])/'bin/java'
def jar(group,artifact,version):
 return next((cache/group/artifact/version).rglob('*.jar'))
stdlib=jar('org.jetbrains.kotlin','kotlin-stdlib','2.1.20')
compiler=[jar('org.jetbrains.kotlin','kotlin-compiler-embeddable','2.1.20'),stdlib,jar('org.jetbrains.kotlin','kotlin-script-runtime','2.1.20'),jar('org.jetbrains.kotlin','kotlin-reflect','1.6.10'),jar('org.jetbrains.intellij.deps','trove4j','1.0.20200330'),jar('org.jetbrains.kotlinx','kotlinx-coroutines-core-jvm','1.8.0'),jar('org.jetbrains','annotations','13.0')]
with tempfile.TemporaryDirectory(prefix='hl-circulation-kotlin-') as temp:
 subprocess.run([str(java),'-cp',os.pathsep.join(map(str,compiler)),'org.jetbrains.kotlin.cli.jvm.K2JVMCompiler','-no-stdlib','-no-reflect','-classpath',str(stdlib),'-d',temp,str(root/'android/app/src/main/java/com/household/homelibrary/Circulation.kt'),str(root/'tests/CirculationCheck.kt')],check=True)
 subprocess.run([str(java),'-cp',temp+os.pathsep+str(stdlib),'com.household.homelibrary.CirculationCheckKt'],check=True)
