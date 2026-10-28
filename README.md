# Carribrary

Self-hosted household book lending: catalog physical copies, manage borrowers and holds, scan ISBN/EAN or optional copy labels, track lending, and share a library with free borrower access.

## Source status

Private development repository. Current source: **2.0.47**, including unified appearance, portable recovery/weekly backups retaining four, opt-in library analytics and encrypted reading progress/rereads/goals/quotes. Previously built installers are not rebuilt by committing this source. No installer, APK, AAB or signing material is included here.

- [What changed in 2.0.47](home-library-native/distribution/RELEASE-2.0.47.md)
- [Administration tool inventory](home-library-native/distribution/ADMIN-TOOLS-INVENTORY-2.0.47.md)
- [Package and hardware test checklist](home-library-native/distribution/RELEASE-2.0.47.md)
- [Current help guide](Carribrary-Help-2.0.47.md)
- [Build packages locally](home-library-native/BUILD-LOCALLY.md)
- [Server setup and architecture](home-library-native/README.md)
- [Windows installer instructions](home-library-native/INSTALLER-GUIDE.txt)
- [Linux and Raspberry Pi](home-library-native/linux/README.md)
- [iPhone source limitations](home-library-native/ios/README.md)
- [Development and verification](home-library-native/DEVELOPMENT.md)

The server uses ASP.NET Core/.NET 10 and SQLite. Android is a Kotlin companion; iPhone/browser companions can use the server-hosted PWA. Native iPhone source requires a Mac/Xcode and has not been verified as an iPhone build.

Keep the `home-library-native` folder: the local builder places tool caches and output alongside it. First create a private, complete backup before updating an existing installation. The protected administrator-credential format introduced in 2.0.36 is incompatible with earlier programs. Library data upgrades to format 6; credentials remain 2. A private pre-upgrade snapshot is retained. Older programs reject format 6 rather than discard reading sessions/goals/annotations. Program-only rollback supports compatible 2.0.47 programs only. Guided complete recovery accepts supported 2.0.43/44 format-4, 2.0.45/46 format-5 and 2.0.47 format-6 archives, with original-machine/portable key restrictions as applicable. Keep a complete backup off the server drive.

## Privacy and credentials

Configure your own optional services on your own server. No API key is supplied. Never commit library databases, backups, pairing codes, DDNS/SMTP/push secrets, certificates, private signing keys, diagnostics or personal library branding. The repository intentionally omits owner briefs, private branding and local build caches.

Public support: **chezburbermerch@gmail.com**.

## Licensing and distribution

No open-source license has been selected for Carribrary. Keep this repository private until its source licensing and publication terms are decided. Third-party code and notices retain their respective licenses; see [notices](home-library-native/THIRD-PARTY-NOTICES.txt). Android store binaries are kept private; the planned paid companion is separate from the free server and borrower interface.

The supplied Carribrary-2.0.47-source.zip extracts into Carribrary-2.0.47. GitHub Download ZIP still extracts as Carribrary-main; rename its top-level folder to the release version before building. Never merge a fresh source version into an older build folder.
