# iPhone access — Carribrary 2.0.57

## Use the hosted librarian companion

This is the normal iPhone route. It needs the running server and trusted public HTTPS, not a Mac, APK or App Store build.

1. On iPhone open `https://YOUR-HOST/companion/` in Safari.
2. Choose Share → Add to Home Screen and open that icon before pairing when possible.
3. On the server open People & Access → iPhone / browser pairing and create a private code.
4. Enter it in the companion, name the device and pair within ten minutes.
5. Refresh and check the library. Keep this code/link private: it grants administration.

Approval lasts 30 days unless revoked. Safari and an installed browser app can use separate storage. Pair again if expired; revoke a lost browser from the server or Android. Sign out / unpair removes this browser's access.

Use Settings & Tools for library cards, self-service policies, lending, backups and the other admin tools. The banner Help button and full Help page stay within the authenticated companion. Browser cameras need trusted HTTPS and permission. Manual entry remains available. Private library API data is not stored for offline circulation.

## Borrower iPhone access is different

Borrowers open their sharing link in Safari. They may browse/request without an account. Share → Add to Home Screen installs the borrower browser app. Optional approved accounts add own loans, TBR and private reading. They do not use librarian pairing codes. The plain server welcome page has no install button.

## Optional native Swift project: developers only

The checked-in project is preparatory native iPhone source (0.1.37, build 38), not a verified app or current installation method. Linux/Windows can generate/reference-check it but cannot run Xcode or verify an iPhone build. No native iPhone feature parity, signed IPA or App Store readiness is claimed.

A developer with a Mac must open `HomeLibrary.xcodeproj` in Xcode, choose their own signing team, inspect permissions/privacy settings, build and run on a real device, and complete the listed security/transport/UI tests before distribution. Never include private server credentials or signing material in source. Source validation is not a device acceptance test.

See [current Help](../../Carribrary-Help-2.0.57.md), [implementation report](../distribution/SELF-SERVICE-IMPLEMENTATION-REPORT.md) and [feature freeze](../FEATURE-FREEZE.md). The hosted iPhone PWA receives this documentation after the server is rebuilt and refreshed; native bundled Help needs rebuilding its app.
