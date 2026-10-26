# Carribrary 2.0.41 — package and hardware test checklist

Automated source checks do not certify native installers. Run this against owner-built packages before uploading them as a prerelease. Use a disposable library for fresh-install and recovery exercises; keep real private backups outside the release folder.

| Target | Fresh install | Update without uninstalling | Required device checks |
|---|---|---|---|
| Windows EXE | Enrollment requires setup proof; save recovery code; Dashboard opens | Complete backup first; install over previous version; verify books/borrowers/loans, settings and existing phone access | Service startup/restart, firewall and local sign-out/InPrivate login |
| Linux Flatpak | Correct app launches in Kubuntu; first-run guide and enrollment work | Update bundle with existing data retained | Close/reopen, desktop shortcut, export backup, library accessible afterward |
| Pi ARM64 DEB | Install on Pi 4/5 64-bit OS, service starts | Install updated DEB retaining /var/lib/home-library | Reboot, service status, LAN/public addresses and exports |
| Android APK | Pair a test phone; no key or private signing folder required on device | Install over previous signed APK; keep data; pairing survives | Scan ISBN and optional copy QR, denial/manual fallback, checkout/return, receipts, offline/reconnect |
| iPhone PWA | Safari HTTPS → Add to Home Screen → private admin pairing | Refresh/reopen after updating server; existing token recognized | Camera/ZXing/manual entry, Help, notifications if enabled, unpair/revocation |

For every platform: add/borrow/return a book, approve a request then physically hand it over, edit a household reading entry and finish it, review loan history, find every Settings tool, and verify confirmation cancellation leaves data unchanged. Try long library names and larger text. Never share private account recovery/signing files.

Plain public address: name/logo/tagline/art match selected theme; Browse/Request explain invitation access, Sign In and Help work. No shared/private tokens or book/contact/reading data appear in page source. Guest requests still use the librarian’s explicit shared link; revoked links remain unusable. Change branding and refresh the welcome page.

Backup/recovery: export a complete encrypted backup with a test password, verify its inventory and unpack only to a new offline folder. Program recovery must preserve the current data and honor the supported 2.0.39–2.0.41 boundary. Do not replace a running real library with a test snapshot. Local sessions expire across restart; already approved native/browser companion credentials remain authorized until revoked.

Record package name, platform, previous version, result and any issue. Upload only finished EXE/Flatpak/DEB/APK plus checksums, with no database, personal art, secrets or signing material. Keep GitHub private/prerelease until the owner chooses otherwise.
