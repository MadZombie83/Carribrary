# Self-service security review — DRAFT, NOT A RELEASE

The user explicitly authorized this scope after the automatic-review stop. Implementation resumed in disposable test databases and local test servers. Installed libraries and the published 2.0.52 branch remain unchanged. No release installers have been produced. Validation is in progress; see the final implementation report for current results.

## Concrete implementation scope for approval

1. Introduce library format7 with a protected pre-upgrade snapshot, permanent borrower UUIDs (alongside existing BR display IDs), self-service policy and loan audit/pending-return fields. Older program-only rollback must be blocked; complete matching backup recovery remains available.
2. Add a protected separate card/PIN/kiosk registry. Cards identify existing borrowers independently of accounts. PINs use salted password hashing, never appear on cards, and have guessing/lockout protection. Revocation is checked per transaction.
3. Add borrower self-service APIs requiring an active linked account. Identity comes from the authenticated account; request JSON cannot select another borrower. All checkout transactions reuse Store/CirculationPolicy locks, limits, holds and idempotency receipts.
4. Add independent checkout/return/kiosk switches, all OFF by default. A reported return remains an active loan, unavailable until librarian physical receipt confirmation.
5. Add separate restricted kiosk device cookies and short borrower sessions, issued through one-use librarian-authorized pairing. Card+PIN+valid station required. No admin token or reading vault access, no secrets in URLs. Every request rechecks permissions/revocation and times out/clears on cancel/errors.
6. Add branded printable CR80 cards, digital account representation, administrator/Android management and scanning, borrower phone flows and standalone browser kiosk. Existing ISBN/manual entry remains; copy labels are optional.
7. Complete backups include registry/settings/audit fields. Restored kiosk credentials are invalidated; cards require deliberate review. Preserve known local card-revocation tombstones during recovery. A historical backup moved to a new machine cannot know subsequent offline revocations, so reactivation must never happen silently.
8. Test against disposable databases and localhost browser fixtures. No automatic router changes, public deployment, live library migration or OS kiosk lockdown. No release-candidate feature-complete claim before actual validation.

## Status and findings

- Architecture inventory complete: SELF-SERVICE-IMPLEMENTATION.md.
- Model, transactions, card registry, restricted routes, browser/PWA UI and native Android controls are implemented. .NET and Android Kotlin compile checks pass; circulation/card security unit checks pass. All 34 supported browser suites and recovery regression checks pass; see VALIDATION-2.0.53.md.
- Automatic approval review rejected the endpoint/build/test command, citing schema migration, recovery changes, public endpoints, PIN/cookie authentication and authorization boundaries. No retry/workaround used.
- Authorized follow-up requirements: keep revocation ledger inside writable server data, merge known tombstones during recovery; optional card PIN for traditional circulation; account lock must wrap linked-account operations; validate registry/restore boundaries; finish UI and tests before publication.
- Help/version synchronization, regression evidence and the directive’s 14-part report are complete. Physical Windows kiosk/USB/webcam, printed cards, phone/Pi hardware and owner-built package acceptance remain owner tests.
