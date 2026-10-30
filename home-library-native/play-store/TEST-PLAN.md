# Play test plan — current 2.0.53

Record device model, Android version, server platform/version, result and reproducible failures. Use fictional contacts and a separate test library.

1. Install over the last supported signed preview without uninstalling; confirm pairing, colors, camera choice and sort preferences. Also test a clean install and manual pairing fallback.
2. On Android 16: verify status/navigation bars, back navigation, keyboard, rotation, large text, landscape/tablet width and all dialogs. No controls should be hidden.
3. Add three books with different covers, insert ahead of existing rows, sort in both directions, filter, page and edit a cover URL. No stale cover may remain with another title; failed cover loads show the placeholder.
4. Camera permission denied and later granted: scan ISBN, copy barcode and QR; select/default rear camera; verify focus and manual fallback.
5. Add/edit/delete book and borrower; duplicates require explicit confirmation; records with active loans cannot be removed. Verify preserved history is disclosed.
6. Checkout optional due date, return, history and prepared SMS/email receipts. Sending requires the external app/user; no automatic message.
7. Home Wi-Fi and cellular/work connection; server stopped and restored; pending change retry must not duplicate loans/books; cached library is read-only offline.
8. Backup/export and restore on a separate server; phone revocation and forgetting connection; verify local cache is removed, server records retained.
9. Shared public catalog: covers, availability, QR, request submission, approve/decline/handover, revocation and borrower contact privacy.
10. Android 16 device/emulator in 16 KB page mode: cold launch and scan. ELF/ZIP validation is necessary but does not substitute for runtime testing.
11. Reviewer access: fresh device can reach every core flow with supplied review instructions; no expiring household credential required.
12. Check in-app privacy, SDK diagnostic disclosures, support contact, public policy URL and listing all match the final build.

Complete real phone/tablet screenshots using successful results. Do not submit this plan as evidence tests were run.

2.0.23 device checks:
13. About popup: manual and provider descriptions, missing-data behavior, source attribution, no HTML execution, preservation after editing older records.
14. Upload/reset a custom icon on server and Android; confirm banner and PWA manifest, backup/restore, and changed icon behavior after re-adding a Home-screen shortcut. Confirm shared colors persist through signup/signin/account.
15. iPhone Safari Home-screen PWA: install-first/pair, QR/manual code, 10-minute single use, 30-day expiry, revoke/logout, ISBN/copy/QR scans with ZXing, permission denial/manual entry, real camera focus and offline explanation.
16. Opt-in borrower/admin PWA push: no prompt before a tap, allow/deny, subscribe/test/unsubscribe, restart persistence, returned loan exclusion, deduplication, expiry/block/deletion/revocation. Confirm lock-screen text has no private book/contact information. Test actual Apple/Google delivery and Focus/OS permission settings; automated fake-transport tests do not prove delivery.
17. Native Android background alerts: permission denial/allow, initial inbox baseline, later request, about-15-minute check, open Requests from alert, reboot persistence, offline/low-battery behavior, disable and Forget connection. These are periodic checks, not instant push.
18. Account deletion: review counts, active-loan guard, exact username, stale-preview protection, guest-request opt-in, account-only versus full erasure, optional managed-backup cleanup and protection of unrelated people. Search Help for each feature and verify the actual build contains matching instructions.

Current additions to verify: account approval/renewal/purchase items in the bell; optional borrower self-checkout and pending physical returns; card issue/replace/revoke/PIN reset and PDF output; separate kiosk permission/revocation/timeout; format-7 backup/recovery and restored-card review. Use matching server and app versions. Actual devices and store requirements remain release gates under the feature freeze.
