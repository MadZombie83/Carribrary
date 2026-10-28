# Carribrary 2.0.47 — portable recovery, unified appearance and reading polish

Source only. Build the Windows EXE, Flatpak, Raspberry Pi ARM64 DEB and Android APK using the included builder. No release installers or APKs were created for this iteration.

## Replacement-computer backup and bounded retention

New complete backups use a password-encrypted portable format-2 archive. The archive contains recoverable copies of ASP.NET server keys; live Windows keys remain machine-protected. Windows destination recovery validates the archive, reprotects the key copies and prepares the recovered service certificate identity. Borrower private vaults still need their own passphrases/recovery codes.

Settings & Tools → Data & History → Portable replacement backup can create a complete archive every seven days. It retains four completed managed archives, verifies a new one before rotating old copies and preserves completed files when creation fails. Cleanup failures are reported; further creation waits for retention cleanup. Manual/downloaded copies are untouched. Routine catalog snapshots retain 16 scheduled and seven daily copies; manual and migration safety snapshots remain independently managed.

**Download a copy off the server.** Four copies on the same disk cannot protect against losing that disk. This feature does not configure cloud replication or an external destination. Archive password storage is protected on the server so unattended creation works. The archive password cannot be reset. Old format-1 Windows archives still need their original machine; create a portable archive while the old machine works.

Use Complete Backup Recovery from the server application menu on a replacement computer: verify, rehearse, confirm, inspect historical accounts/devices in local review and reconfigure remote networking. Do not run two live servers with one restored identity. External proxy/router/DuckDNS configuration is not a computer image. The restored weekly schedule stays paused pending deliberate reconfiguration.

## Appearance and navigation

Library & Appearance now has two cards: Library identity (name, icon, subtitle, saying) and Theme & artwork (presets, saved palettes and PNGs). Draft changes require Apply preset or Save & use. Builtin presets clear artwork. Saved custom themes restore their own image or no-image choice. New theme clears draft name/artwork while retaining starting colors. Old links route to the consolidated controls. Google Books moves to Books & Organization. Notification and browser-pairing controls have their own relevant Android settings rather than being embedded in Library identity.

Dashboard/Home Quick Actions includes Share Library and Books to Buy; settings cards remain. Lending limits lives inside Lending & holds rather than a separate card. See the complete tool inventory alongside these notes.

## Analytics and reading

Dashboard → Library analytics & insights, or Android Home, provides opt-in aggregate analytics, 7/30/90-day and all-time filters, daily metric charts and popular books. It counts website page loads, catalog views, detail opens, searches, zero results, requests, checkouts, returns and copies added. It stores no visitor identity, IP, search phrase, URL token or private reading information. Public DNT/GPC preferences are honored. Counts start when enabled; they are not unique-visitor statistics. Daily chart buckets retain 730 days; all-time totals continue. Distinct popularity entries are bounded (1000/day, 10000 all time). Clear requires CLEAR ANALYTICS and never clears books or private reading.

Reading Tracker / My reading adds current/total pages with percentage and progress bars. Finished reads fill the known total. Start a reread preserves each earlier session's dates, progress, rating and notes. Sessions are read-only; current reading remains editable. Book-level favorite quotes and notes accept optional pages and persist across rereads. There are limits of 1000 earlier sessions and 1000 annotations per entry.

Per-reader annual goals, monthly/yearly summaries, recorded-page totals and optional completion-date streaks extend the tracker. Genres appear when recorded; optional genre can be entered in book details. Unknown lengths and undated finishes are not guessed. The streak describes completed-book dates, not inferred daily reading. Borrower-owned progress, sessions, goals and annotations stay in the existing encrypted personal vault and are calculated only after client unlock. Household profiles remain household-admin managed. Native Android and the shared iPhone PWA expose these reading controls; native iPhone source is not an Xcode-verified build.

## Upgrade boundary

Library data upgrades to **format 6**; credentials remain **format 2**. A private pre-upgrade snapshot is retained. Existing catalog, reading and circulation references stay intact. Older programs reject format 6; automatic program-only rollback is restricted to compatible 2.0.47 executables. Guided complete recovery accepts supported 43/44 format-4, 45/46 format-5 and47 format-6 archives, with machine-bound/portable protections as applicable. A verified archive does not by itself certify hardware migration.

## Validation and owner testing

Automated server, browser, Android compilation, upgrade and packaging-source checks are recorded separately. Before distribution, build the packages and test real Windows service/DPAPI recovery onto a second computer, Flatpak file portals/sandbox, Pi ARM64 service, Android scanner/file picker and Safari installed-PWA behavior. This is a preview source release, not a claim that the device release matrix is complete.
