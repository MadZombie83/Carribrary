# Carribrary 2.0.41 — branded front door and remaining screen polish

## Public library welcome

The plain public server address opens a themed welcome page with the saved library logo, name, tagline, encouraging saying and optional sidebar PNG artwork. It follows the reference layout: centered masthead, four access cards and invitation guidance. Browse/Request show instructions for obtaining the librarian’s private shared link; Sign In opens borrower authentication; Help opens public access guidance. The librarian companion is linked separately.

The welcome HTML uses an explicit allowlist of presentation fields. It never includes sharing tokens, book/contact/account records, private reading, pairing codes or private links. It is not a new public catalog or account-creation bypass. Invalid/revoked catalog links still fail. No private contact address is published automatically. Existing artwork becomes public branding on this page; the artwork editor and Help state that clearly. Each library supplies its own art/logo; no Queen B or other owner branding is bundled.

## Administrator and Android

Local administrator sign-in/reopen now opens Dashboard; explicit Settings deep links remain. Borrowers stay Catalog-first, and existing paired-device starting behavior is preserved. Household Reading Tracker shows only the selected household reader’s TBR/current/finished totals and grouped controls. Reading/details/history share dashboard surfaces, notes and action grouping; borrower account vaults remain private.

Native Android’s Settings button replaces More. Six searchable categories preserve all prior tool actions; category Back navigation and refreshed Reading, Requests, Borrowers and Loan History use shared heading/card/status components. Request Open/All/Closed filters are display-only. Manage library groups appearance, recovery and approved phones into expandable cards. All confirmation, busy/offline, consent, revocation and handover boundaries remain. About now opens its single book-details dialog instead of also mounting an unrelated tools dialog.

Canonical Help and borrower/phone guides are updated. Android 20041 / 2.0.41-preview; native iPhone source 0.1.25/build 26 (no Xcode/device build). Hosted web/PWA styling comes from the server.

See the [tool-location inventory](ADMIN-TOOLS-INVENTORY-2.0.41.md) for native/web parity.

## Build and verification

Owner builds packages from Carribrary-2.0.41-source.zip, which extracts into its own versioned folder. Export a private complete backup first. Library/credential formats remain 3/2; no data migration or new permission/cache. Program recovery accepts compatible 2.0.39–2.0.41; historical security/format boundaries stay protected.

No EXE installer, APK/AAB, Flatpak or DEB was packaged or installed here. Automated checks use disposable data. Windows/Pi cross-publish outputs are compile checks, not proof of platform installation. Physical Windows service/installer, Flatpak/DEB installation and phone camera/Safari testing remain owner/hardware checks; use the accompanying test checklist before distributing packages. Existing 2.0.39 GitHub release assets are unchanged.
