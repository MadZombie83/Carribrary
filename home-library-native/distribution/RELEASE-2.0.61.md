# Carribrary 2.0.61 — release notes

Source release prepared 2026-11-07. The owner builds, signs and publishes installers; nothing here
was packaged or published from the development workspace.

## Included

- One shared JSON transport (`server/wwwroot/api-client.js`) now serves the borrower account, kiosk,
  self-service and reading pages instead of four separately drifting copies. Request method,
  credentials, timeout, JSON strictness and unauthorized handling are unchanged, and each surface
  keeps its own error wording. The module is registered in the account, kiosk and reading allowlists.
- No feature, permission, data format or Help topic changes. Library format remains 7 and
  administrator credential format remains 2.
- Version identifiers, program-recovery compatibility (2.0.61 only) and complete-backup acceptance
  (format 7 from 2.0.53 through 2.0.61) advance with this release.

## Not yet verified

- Windows EXE, Linux Flatpak, Pi ARM64 DEB and Android packages still have to be built on the
  owner's machine. This workspace has no .NET SDK, NSIS or Gradle toolchain, so the C# changes have
  been reviewed but not compiled.
- Device, printed-card, kiosk hardware and store checks remain as recorded in
  `VALIDATION-2.0.60.md` until they are repeated for 2.0.61.
