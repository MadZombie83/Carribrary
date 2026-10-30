# Carribrary 2.0.53 preview — optional self-service

Source-only update. Build the Windows, Flatpak, Android and DEB packages locally after reviewing the owner acceptance checklist below. Existing installations have not been changed. No router rules or public deployment are performed by this source update.

Settings & Tools → Borrowing & Reminders → Borrower self-service & kiosks controls self-checkout, self-return reporting and kiosk authorization independently. All three default OFF. People & Access → Library cards issues, replaces, revokes and prints cards for existing borrowers, including people without accounts or phones. Android includes these administration tools and card scanning at checkout. Borrower accounts show the same active digital card under Account.

A linked borrower can scan an existing ISBN/EAN or optional permanent copy QR, select an eligible physical copy and confirm. Manual entry/search remains available. Self-return reports remain active loans and unavailable until a librarian physically receives the book and uses the normal Return action. The notification bell and active-loan cards identify pending returns.

The restricted /kiosk/ browser page connects to the existing server through its trusted HTTPS hostname. A librarian provisions each station with a ten-minute one-use code. The kiosk uses card plus separate PIN, never an admin device token. It supports keyboard/USB scanner input and the shared camera scanner. OS lockdown is configured separately. Windows Edge Assigned Access and physical scanner/webcam acceptance still require owner testing; a fullscreen browser is not OS lockdown.

## Before upgrading

Download a complete password-encrypted backup and keep it off the server. Library format upgrades to **7**, assigning permanent borrower UUIDs alongside BR display numbers and preserving physical-copy UUIDs, accounts, pairings and circulation. A private pre-migration backup is saved. Credential format remains 2. Program-only rollback to 2.0.52 or earlier is blocked because those programs discard the new fields. Guided complete recovery still accepts supported older complete archives and migrates them.

Complete backups include the protected card/PIN/station registry, revocation ledger, settings and pending return events. Library-only JSON has no card/PIN/station registry. After complete recovery, station credentials are revoked and cards await explicit review; pair stations again. Known local revocations survive recovery. A historical backup restored to a new computer cannot know later revocations: review or replace cards, never assume all historical cards are legitimate.

Update server and native Android together. Browser companions receive changes after refresh. Existing pairing protocols, private-reading encryption and assisted circulation remain in use. PINs are not printed or included in QR codes. Permanent account-and-information deletion removes obsolete card/PIN registrations while keeping anonymous revocation tombstones.

## Owner acceptance before a public candidate

- Print one and several CR80 cards on Letter/A4 at Actual size / 100%; check dimensions, background graphics, duplex alignment and QR readability after lamination.
- On Windows 11 Pro/Edge and a separate Linux computer, authorize /kiosk/ in a dedicated standard OS/browser account; test restart persistence, timeout, Cancel, revocation and both independent circulation switches.
- Test a real QR-capable USB scanner and compatible webcam (trusted HTTPS and camera permission); retain manual entry and assisted borrowing.
- Use a physical Galaxy/Android and iPhone Safari PWA to test scanning, digital cards, phone checkout and pending return, with all existing account/TBR/private-reading flows.
- Build/install each platform package over a backed-up test installation. Verify clean complete recovery, local login, existing pairings and old copy labels. Physical Pi ARM64 remains experimental until hardware acceptance.

See SELF-SERVICE-IMPLEMENTATION-REPORT.md for executed checks and remaining limitations. This preview does not declare untested Windows/Pi/mobile hardware acceptance or public-release readiness.
