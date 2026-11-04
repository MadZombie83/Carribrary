# Carribrary 2.0.59 preview — release guide and recovery consistency

The illustrated Quick Start title, heading and program-recovery version now synchronize with canonical Help. Documentation checks reject stale guide labels and mismatched Windows recovery version rules.

Windows program-only recovery now accepts the documented current 2.0.59 cohort, replacing the stale 2.0.53-only filter. Previous releases are not newly approved for downgrade. Guided complete-data recovery now accepts supported format-7 archives from 2.0.53 through 2.0.59 instead of stopping at 2.0.53. Integrity, platform/portability checks, recovery rehearsal and restored-access review remain required. Future archives and mismatched formats remain blocked. Tests verify same-version recovery preserves current data/settings and that unsupported versions are rejected before stopping or changing anything.

Owner-built Windows EXE, Linux x86_64 Flatpak and Raspberry Pi ARM64 DEB installers are published as v2.0.59-preview with SHA256 checksums. The payloads match the tested source, including synchronized Help, corrected Quick Start and recovery helpers. Windows GUI acceptance still requires a real Windows test. Earlier releases remain historical.
