# Self-service architecture audit and implementation record

Approved scope: optional independent self-checkout/self-return, borrower cards without online accounts, linked-account digital cards and self-service, and restricted browser kiosks. Traditional circulation remains available. Developer builds release packages.

## Reuse inventory

- Library.cs / CirculationPolicy.cs: active loans, stable borrower IDs, due dates, holds, per-borrower/batch limits, final librarian returns.
- Store.cs: revision-checked SQLite transaction and idempotency receipts, complete snapshots/migrations.
- BorrowerAccounts.cs / AccountEndpoints.cs / ReadingAccess.cs: account authentication and linking; private reading vault remains separate.
- CopyIdentity.cs / library.js / copy-labels.js: permanent copy UUID reference, optional BK labels, ISBN and manual matching; real QR SVG and accurate browser printing.
- camera.js + bundled ZXing: remembered camera/autofocus and browser camera fallback; native Android already has a scanner.
- BrowserAdmins.cs / Identity.cs: useful device-pattern references; kiosks require their own restricted credentials and cannot reuse administrator authority.
- CompleteBackup.cs / CompleteRecovery.cs / SnapshotGate: protected SQLite snapshots and local restored-access review.
- themes, library-artwork.js, icon and branding: card and kiosk branding without hardcoded library identities.
- synchronized Help and existing Windows/Linux/Android packaging remain authoritative.

## Boundaries

Circulation authority stays in Store transactions. Both self-service permissions default off, as does kiosk enablement. Browser account identity is derived server-side, never supplied by borrower JSON. Kiosk station and short borrower session are revalidated for every request, including card/PIN changes and global disable. Card QR is an opaque identifier; card plus PIN plus authorized station is needed at a kiosk. No reading-vault data is used.

Pending physical receipt and transaction origin require library format7, with pre-migration backup and incompatible older program rollback blocked. Card/PIN/station registry is a separate protected SQLite store included in complete backups, not library-only JSON. Kiosk credentials must be invalidated after recovery; historical backups cannot know later offline revocations on another machine, so card review is required before reactivation. No release-candidate feature-complete claim until acceptance and regression evidence exists.

## Status

Architecture and source implementation completed. See SELF-SERVICE-IMPLEMENTATION-REPORT.md and VALIDATION-2.0.53.md for actual verification and outstanding physical-platform acceptance. This is a source preview, not a public-release readiness claim.
