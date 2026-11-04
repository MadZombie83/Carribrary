# Carribrary

Developed by **Chezburber Studios**. Support: **chezburbermerch@gmail.com**.

A self-hosted library for physical books. Keep the server on a Windows, Linux or Raspberry Pi computer; librarians and borrowers connect to it.

[Full feature list](FEATURES.md) · [Screenshot gallery](SCREENSHOTS.md)

## Try the temporary borrower demo

[Open the public borrower test library](https://testlibrary.duckdns.org/s/L9PHAHbPWwiBki05Ad8ldyiY1ZN-AAUq/)

**Public test library — use fictional names and contact details. Test data may be deleted or reset.** Browse the catalog, try borrowing requests, and optionally create a test account for TBR and reading tracking. No installation or purchase is required to try it in a browser.

See [demo instructions and feedback](TESTING-DEMO.md), including phone Home-screen installation. The demo depends on a volunteer-hosted server and may be unavailable occasionally. Administrator testing is arranged individually; administrator pairing links are not published.

[![Carribrary librarian dashboard — fictional sample library](docs/screenshots/2.0.59/01-librarian-dashboard.png)](SCREENSHOTS.md)

## Install

Download the free server installer from the [2.0.60 public preview](https://github.com/MadZombie83/Carribrary/releases/tag/v2.0.60-preview): Windows EXE, Linux x86_64 Flatpak, or Pi ARM64 DEB. This is a testing release; make a complete backup before updating an existing library. The optional Android librarian companion is planned as a one-time US$15 Google Play purchase and is not yet publicly available. No Android APK or AAB is distributed here. Borrowers use their free browser/PWA link. iPhone librarians use the hosted Safari companion.

Most users do not need source code or build tools. Follow [installation instructions](home-library-native/INSTALLER-GUIDE.txt), then Help in the app.

## License

Carribrary-owned code is **source-available proprietary software**, not open source. The server is free to run for households, churches, schools and organizational libraries. You may inspect source and build the server for your own permitted use. Redistribution, resale, rebranding and offering a paid hosted service to unrelated customers require separate permission. See [LICENSE.txt](LICENSE.txt). Third-party rights remain separate: [notices](home-library-native/THIRD-PARTY-NOTICES.txt).

## Current preview

Current public preview: **2.0.60-preview**, with verified owner-built Windows, Flatpak and Raspberry Pi installers on the release page. See [current Help](Carribrary-Help-2.0.60.md), [release details](home-library-native/distribution/RELEASE-2.0.60.md) and [validation](home-library-native/distribution/VALIDATION-2.0.60.md). This is not the final stable launch or a Google Play release. Keep a verified portable complete backup on another device before updating. Each independent library needs its own backup.

This preview adds guided **Libraries & network** setup for independent libraries on one computer or separate computers, plus an installable **household reader PWA**. Windows/Pi/iPhone and outside-network acceptance remain ongoing. Managed-slot complete recovery still needs assistance; see the documented limits before restoring.

## Questions and bug reports

Use [Discussions](https://github.com/MadZombie83/Carribrary/discussions) for setup questions and testing feedback, and [Issues](https://github.com/MadZombie83/Carribrary/issues/new/choose) for reproducible bugs. Start with [Support](SUPPORT.md). Never post library backups, borrower information, passwords, API keys, pairing codes or private sharing links. Report suspected security problems privately using [Security](SECURITY.md).

## Developers and testers

[Current validation](home-library-native/distribution/VALIDATION-2.0.60.md) records completed checks; use the [2.0.60 device checklist](home-library-native/distribution/DEVICE-CHECKLIST-2.0.60.md) for remaining real-device tests.

[Build source](home-library-native/BUILD-LOCALLY.md) or follow [Android test instructions](home-library-native/distribution/ANDROID-TESTING.md). Extract a source download to a new folder; do not merge it into installed programs or old build folders. Keep keys, library data and private configuration out of Git. Older Help/release files are historical records, not current setup directions.

## Current source: 2.0.60

The source and published installers include Libraries & network, household reader installation, synchronized 97-topic Help and offline-safe recovery guidance. [Feature list](FEATURES.md) · [Help](Carribrary-Help-2.0.60.md) · [Release details and limits](home-library-native/distribution/RELEASE-2.0.60.md). Source downloads do not install or update Carribrary; most people should use an installer.
