# Carribrary 2.0.53 — documentation audit

Reviewed October 30, 2026. **Feature set frozen.** This revision changes instructions, Help presentation, guide images and delivery of documentation. It adds no product features, data migration or installer builds. App version remains 2.0.53.

## Current guides

The canonical `assets/help.json` contains 83 task-based topics plus About and third-party legal notices, replacing 109 overlapping topics and accumulated historical additions. Server, Android, iPhone source and the standalone versioned guide are synchronized from that file. The in-app and full-page Help preserve numbered line breaks. `START-HERE.txt`, `INSTALLER-GUIDE.txt`, platform READMEs and `BUILD-LOCALLY.md` point to the current workflow. Technical commands and uncommon recovery procedures are in `ADVANCED-OPERATIONS.md` rather than the basic getting-started sequence.

The nine-step illustrated Quick Start covers installation, connections, optional phones, adding books, borrowers, lending, returns, complete backups and recovery. Its three fictional-library screenshots were regenerated against the current browser interface. The PDF is generated from the HTML text rather than maintaining a second, older set of directions. Its nine rendered pages were visually checked for overflow, broken glyphs, missing screenshots and detached steps. Older release notes are explicitly historical, not current setup instructions.

## Completeness review

The browser settings/action inventory and Android settings inventory were compared with these current task groups. Sub-actions remain grouped under their task; there is no requirement to create one Help topic per button.

| Existing area | Current Help coverage |
|---|---|
| Dashboard, navigation, login | Orientation; Dashboard and six Settings & Tools categories; first login; multiple administrators; sign-out, recovery and sessions |
| Windows, Flatpak, Pi, Android, iPhone/browser | Install/start/stop/update; local vs public addresses; optional phone installation and pairing; unverified native iPhone build distinguished from working PWA |
| Public setup | DuckDNS account/hostname/token; platform-specific GUI setup; router reservations and forwarding; external mobile-data test; troubleshooting and advanced proxy guide |
| Catalog and organization | Single/batch/CSV entry; duplicates; covers and descriptions; optional Google; search/collections/sort/page size; shelf locations; bulk editing; permanent labels/numbering/printing |
| Borrowers and circulation | Contact and consent; ISBN/copy/manual checkout and return; physical handover; holds/requests; receipts; batches/limits; renewals; active-loan and history review |
| Sharing and accounts | Library link/QR/text; guest requests; purchase suggestions; optional accounts; approval/block/unblock/relink; account self-deletion and administrative deletion; email/librarian reset |
| Browser installation | Borrower Account → This device; Chrome and iPhone steps; distinct librarian companion; root landing page has no installation button |
| Self-service and cards | Three independent default-OFF switches; phone borrowing/return reports; pending physical receipt; issue/replace/revoke/PIN; digital and printed cards; station authorization and OS kiosk setup |
| Reading | Private borrower TBR/tracker/wishlist; independent secret/recovery/export; household readers and librarian profile; pages/rereads/goals/quotes/notes; acquisition wishlist kept separate |
| Alerts and email | Full administrator bell; optional browser push/native polling; SMTP and consent; recovery-email opt-in; delivery/privacy limitations |
| Appearance | Unified identity and theme/artwork pages; saved themes; image specifications; where branding appears; no colors in Catalog |
| Data & History | Separate analytics page; complete/library JSON/CSV distinctions; weekly four-copy retention; verification; isolated recovery test; replacement computer; restored-access review; program-only recovery |
| Destructive controls | Recycle Bin; permanent removal; scoped history clearing; account deletion; backup/recovery-point retention; disconnected device/export caveats |
| Advanced and troubleshooting | Storage paths/formats; lost-owner recovery; headless Pi; optional Google/Turnstile; network limits; safe support information |

## Obsolete or conflicting directions corrected

- Removed old version-specific build sections that instructed use of 2.0.34–39, format 3 or obsolete rollback limits. Current library/credential formats are 7/2; current program-only compatibility is 2.0.53. Historical full recovery is a separate operation.
- Corrected old sidebar/tool names, appearance placement, Google settings placement, Dashboard sign-in destination and notification routes.
- Removed claims that email password recovery does not exist. Explained its optional SMTP configuration and the librarian-issued alternative.
- Distinguished normal ISBN scanning from optional copy labels; requests/holds from physical checkout; self-return reports from completed physical returns.
- Distinguished complete portable archives from library JSON, CSV, borrower vault exports and program recovery. Explained weekly retention, off-device copies and replacement-computer access review.
- Updated privacy/deletion drafts to remove contradictory plaintext-reading claims, obsolete policy change headings and duplicated product names. Both deletion scopes remove the current encrypted vault; old external copies remain separate. These remain owner-review drafts, not a legal/store approval.
- Installed Windows/Linux/Pi README now describes the installed application rather than linking to absent source-tree files. Advanced instructions are included in installed documentation layouts.

## Plain-language review

Basic guides define server, companion and browser app before using them. Common tasks use numbered actions in execution order, name the visible controls and describe a successful result. Optional setup is identified before it is introduced. Destructive actions keep their preview/password/typed-confirmation warnings. Troubleshooting explains what to check next and what private information not to share. Technical details are separated rather than erased.

This is an editorial review for clear, school-age-readable instructions, not a certified reading-grade assessment or a substitute for a first-time user's walkthrough. Router screens, operating-system permission dialogs and browser installation labels vary; the guide says where that variation matters.

## Verification

- `tests/help-documentation.py`: current topic coverage, unique/nonempty topics, numbered core tasks, local documentation links, nine Quick Start anchors/images, current build guidance and packaged advanced-guide inclusion.
- `scripts/sync-help.py --check`: server, Android, iPhone and standalone Help identical.
- Browser suites: `quick-start`, `accounts`, `account-deletion`, `ecosystem` passed against the current documentation and interface.
- Local builder/source-archive tests and mock recovery-launcher packaging tests passed. Mock tools do not create distributable installers.
- Android settings inventory passed; release Kotlin compile passed using cached tools (no APK/AAB generated).
- iPhone project/source-reference checks passed; Xcode and physical Safari verification remain separate. Optional Swift grammar parser unavailable in this environment.
- Printed PDF rendered and visually reviewed; refreshed screenshots use fictional test data.

Build updated packages locally to deliver the bundled changes. Existing installed applications were not modified by editing source. The remaining owner hardware, package-upgrade, scanner/printing and physical Pi checks in the release checklist still apply. This audit does not declare a public release ready.

## Installer-first publishing correction

Normal instructions now lead with GitHub server installers and the planned Google Play companion. Source building/APK sideloading is confined to developer or invited-tester material. Chezburber Studios and the approved support contact are filled in; unpublished store/policy/download URLs remain explicit release gates. The custom source-available proprietary license and dependency attribution review are described in PUBLISHING-AND-LICENSE-REVIEW.md.
