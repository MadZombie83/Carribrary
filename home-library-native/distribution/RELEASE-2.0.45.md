> Historical release record. For current installation, recovery and Help, use the current README and synchronized Help. These older instructions do not override current compatibility or safety requirements.

# Carribrary 2.0.45 — guided complete recovery and saved theme artwork

## Recovery without typing commands

Windows: Start → Carribrary → Complete Backup Recovery (also in the tray). Flatpak: Carribrary Complete Backup Recovery in the application menu, or Recovery on the main launcher. Raspberry Pi desktop: the same named menu entry, with administrator authorization. Data & History → Complete Backup Recovery explains the launcher and offers its installed URI handler on the server computer. Phone companions cannot run an operating-system recovery operation.

The independent loopback browser wizard runs even when the working dashboard cannot start, provided the installed program and permissions work. Select a complete `.carribrary-backup` and enter its password privately. Verify, start a separate recovery test, open its address in InPrivate/Incognito and sign in using credentials from the backup. The disposable copy has its own login cookies, a visible RECOVERY TEST banner, local-only listeners and no automatic email/push/discovery. Verification alone is not a recovery rehearsal.

After testing, confirmed restoration stops the matching Windows/Pi service; Flatpak requires the existing authenticated dashboard Stop confirmation. The wizard retains the entire current data folder separately, replaces the folder as a unit and checks startup. Failed startup attempts to restore the retained current folder, keeping failed data for diagnosis. An interrupted restore can be undone from the reopened wizard. Original archives and retained current/failed folders are not deleted by Finish. Private temporary verified/test copies are removed on normal Finish; crash remnants can remain private. Allow ample disk space for multiple copies. Do not manually move data during recovery.

Historical data can revive deleted accounts, old passwords and revoked devices. Restored data starts in local review mode with remote/public access and automatic notifications paused. Sign in locally, review accounts/devices and record the explicit review before resuming through the wizard. Automatic email/push configuration is retained in paused files and requires deliberate setup again. Keep archive passwords and borrower private-list recovery credentials separately. A private one-use launch capability and expiring local session protect the wizard; it is not a public recovery endpoint.

Guided recovery supports same-OS 2.0.43/2.0.44 format-4 archives and 2.0.45 format-5 archives, testing their upgrade separately first. Earlier archives can be integrity-verified but need supervised compatible recovery. Windows DPAPI data normally needs the original computer. Current installed connection configuration stays current; archive configuration is available in the original archive and temporary staging until Finish. External proxy certificates, OS services and environment-only configuration are not machine-image backups. Pi supports the packaged service/data location; custom layouts require supervised recovery. A damaged executable may need installer repair first.

The existing verification and developer offline unpack commands remain available. Program-only rollback is separate; a selected-point argument error in its Windows recovery launcher is corrected.

## Colors and sidebar artwork belong together

Library & Appearance → Custom theme editor saves each named palette with a sidebar PNG or an explicit no-artwork choice. Selecting that theme restores both; an empty choice clears the previous overlay. The editor previews saved art and offers Use current library artwork / No artwork for this theme. Changing or removing artwork while a custom theme is active updates the saved theme too. Android provides the same capture/clear choices; publishing a saved custom theme applies its art on the server. Built-in presets retain the current overlay, and legacy themes retain that behavior until saved with an art choice. Native phone color choices remain local until published. Responsive phone layouts need not display a sidebar.

Each PNG retains the existing dimensions/size limits. Multiple images enlarge snapshots and JSON exports: use complete backups if an export exceeds the existing 10 MB JSON restore limit. No personal brand/artwork is bundled.

## Upgrade and delivery

Library format becomes **5** with a private `before-theme-art-v5` snapshot before migration. Credential format remains 2. Older format-4 executables reject format 5 instead of silently dropping per-theme artwork. Compatible program-only recovery therefore supports **2.0.45 only**. Do not install an older program over live format-5 data. Existing books, loans, sharing, accounts, vaults and pairing remain. Android version 20045 / 2.0.45-preview; native iPhone source 0.1.29/build 30, not an Xcode-verified build. Hosted PWA changes arrive with the server.

Source only: the owner compiles EXE, Flatpak, Pi ARM64 DEB and Android APK. No release installers were built here.

## Owner checks after compilation

Use disposable data first. On each target OS verify menu/tray/URI launching, correct and incorrect passwords, isolated login/books, confirmed restore, retained current folder, review quarantine and resume. Test Windows UAC/service/ACL behavior, actual Flatpak sandbox file picker/portal/Stop behavior, Pi polkit/service ownership and interrupted restart. Test theme A/art A → theme B/art B → no-art theme on desktop, Android and iPhone PWA; check legacy theme migration and full backup restoration. Local mock/integration tests do not certify those physical installer/device behaviors. Keep the successful original-library backup and password outside the installation.
