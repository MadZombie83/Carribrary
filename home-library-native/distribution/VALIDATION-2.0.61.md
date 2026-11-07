# Carribrary 2.0.61 validation — pending

Status: **not yet run**. Nothing in this file is a recorded pass, and no hardware has been tested.

- [ ] `dotnet publish` succeeds for server (win-x64, linux-x64, linux-arm64) and tray
- [ ] Windows EXE builds, installs over 2.0.60 and preserves books, loans, accounts and settings
- [ ] Linux Flatpak and Pi ARM64 DEB build, install and start the service
- [ ] Android companion builds with versionCode 20061
- [ ] Borrower page `/account/` loads, lists loans, and completes a request and a return
- [ ] Kiosk page `/kiosk/` loads with no 404 for `api-client.js`, and scans work
- [ ] Reading link `/reading/` loads, and personal list and history actions succeed
- [ ] Paired-phone sign-in, private vault and account deletion behave as before
- [ ] `python3 scripts/sync-help.py --check` passes
- [ ] `python3 tests/flatpak-metadata.py` and the Playwright suites pass
- [ ] Printed-card, kiosk hardware and store checks repeated for this release
