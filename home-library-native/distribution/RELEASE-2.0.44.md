> Historical release record. For current installation, recovery and Help, use the current README and synchronized Help. These older instructions do not override current compatibility or safety requirements.

# Carribrary 2.0.44 — verify the backup you saved

## What changed

Server dashboard / iPhone companion: Settings & Tools → Data & History → Verify complete backup. It is also linked from Backups. Android: Settings → Data & History → Verify complete backup. Select a saved `.carribrary-backup`, enter its password and choose Verify saved backup. The tool uploads to your own server, checks authenticated encryption, file inventory and every hash, SQLite integrity and the library format against its manifest, then deletes temporary upload/recovered files. Live library records, credentials and the original archive are not changed. Results show only date, source version/platform, format, file count and size; no records, secrets or file paths. Passwords stay in request bodies, never URLs/preferences.

Normal completion, failure and cancellation clean private temporary files. A server crash can leave a protected staging folder excluded from future backups; support can remove it when no operation is active. Only one verification runs at a time. The existing complete backup creation tool remains separate and available. Temporary disk space must accommodate the encrypted upload, decrypted ZIP and recovered contents; original data is limited to 2 GB / 10,000 files with bounded archive overhead. Eight-minute verification budget; reverse proxies/network connections can impose shorter upload limits. Large backups can use the offline tool.

Offline, from the included server executable: `HomeLibrary.Server --verify-complete-backup ARCHIVE`. Windows uses `HomeLibrary.Server.exe`; managed development uses `dotnet HomeLibrary.Server.dll`. The password is prompted without echo; do not include it in the command. The command does not start a server, open the live Store or restore files. It needs writable space beside the archive for private temporary checks. Existing `--unpack-complete-backup ARCHIVE NEW-FOLDER` recovery is unchanged.

Verification establishes archive integrity, not guaranteed portability or a successful recovery rehearsal. Recovery still requires a stopped server and compatible program/data/credential versions. Windows DPAPI keys/settings may need the original computer. External HTTPS/proxy certificates, services/tasks and environment-only secrets are separate. Borrowers still need their private-list passphrase/recovery code. Historical restoration rewinds passwords, deletions, device revocations and loans; retain current data separately and review before exposing the recovered server.

## Corrections

- Android Restore library export accepts format-4 JSON, preserving its replacement confirmation and server validation. Complete encrypted archives are not JSON imports.
- Settings category editor headings no longer overlap in-page form content while scrolled.
- Illustrated quick start uses the current Data & History navigation and format-4 recovery boundary. Help is synchronized across platforms.

## Upgrade / delivery

Library format remains 4; credential format remains 2. Compatible program-only recovery supports 2.0.43–2.0.44. Earlier executables cannot retain format-4 acquisition/renewal fields. Back up before updates; retain current data before any historical full recovery. Source only: owner builds EXE, Flatpak, Pi DEB and APK. Android 20044 / 2.0.44-preview; native iPhone source 0.1.28/build 29, no Xcode build. Hosted iPhone PWA updates with the server.

## Owner checks after compilation

Verify a fresh complete backup from desktop, Android file picker and a paired iPhone/browser. Try an incorrect password and a truncated copy; confirm your catalog, accounts and pairings still work. Test a larger real archive and available disk space. Check offline verification with the server stopped. Rehearse actual recovery only on a separate private test installation; verification alone does not certify Windows DPAPI migration, physical device behavior or an operating-system replacement. Test Android restore of a small format-4 JSON export on disposable data before using replacement restore on a real library.
