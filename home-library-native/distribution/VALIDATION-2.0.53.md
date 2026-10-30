# Validation — Carribrary 2.0.53 source preview

Executed against disposable data only. No installed library, router, signing store or public deployment was modified. No release installers were built.

| Check | Result |
|---|---|
| .NET server/tests build | PASS, zero warnings/errors |
| Complete native server/model/security suite | PASS, including new self-service/card tests |
| Final targeted self-service tests after transaction-lock refinement | PASS |
| Browser regression suite | All 34 supported suites PASS; a stale exact Help-wording assertion was corrected and its suite rerun |
| Final self-service/card browser acceptance | PASS: real card QR decoding, CR80 PDF generation, long-name fitting, Letter/A4 9-card duplex layout, PWA checkout, pending return, kiosk enrollment/keyboard flow, lost-reply idempotent retry, immediate live-card revoke, endpoint isolation, all-off enforcement |
| Actual isolated 2.0.52→2.0.53 upgrade | PASS: existing catalog/loan/household reading, credentials, sharing, custom palette/art, native TLS and browser/phone pairing retained; format-7 pre-backup present; old executable rejects upgraded data |
| Complete backup capture/unpack with cards | PASS: permanent IDs/settings/card revocations retained; station credentials revoked after restore; active cards require review |
| Independent PowerShell recovery core | PASS: rollback compatibility, retained current data, health failure/undo, integrity/traversal/link rejection |
| Native Android release Kotlin compile | PASS, cached SDK; no APK/AAB packaged |
| Android administration tool inventory | PASS: previous tools plus self-service/cards routes retained |
| iOS source project/reference/plist validation | PASS; Swift parser unavailable, Xcode/device build not executed |
| Help synchronization | PASS: server/Android/iPhone/standalone current guide |
| Local builder unit checks | PASS, 6 tests with mocked compilers |
| Source archive unit checks | PASS, 2 tests: versioned root, no overwrite, unsafe/private entry rejection |
| Flatpak/Pi/Windows launcher layout checks | PASS, mocked payloads only; updated old expectation to retain existing Public Sharing action alongside Stop and Recovery |

Browser suites: self-service, public-connection, artwork-platforms, purchase-suggestions, admin-notifications, pwa-diagnostic, analytics-navigation, reading-polish, appearance-portable, theme-artwork, complete-recovery, backup-verification, acquisition-renewals, borrower-controls, summary-panels, release-polish, admin-landing, settings-pages, admin-pages, custom-themes, repair-requests, accounts, private-vault, account-deletion, borrower-app, data-controls, lending, circulation, collections-google, registration-security, turnstile, complete-backup, quick-start, ecosystem.

Print observation: Chrome PDF reports 242.88×153.12 points for the single CR80 sheet, within 0.05 mm of 85.725×53.975 mm. Android PDF code uses 243×153 points. Physical print scale, margins, crop and duplex alignment are not inferred from PDF measurements.

Not executed: new package installation on Windows/Kubuntu/Pi, physical Windows Edge Assigned Access, separate-computer station persistence/OS lockdown, physical Android/iPhone camera/PWA acceptance, real USB/webcam scanners and printing/lamination. These remain public-candidate release acceptance items. Successful source/Chrome checks do not substitute for them.

One earlier full-browser attempt was not executed because automatic approval review hit a usage limit. After the user's Continue, testing resumed through normal approval. The subsequently executed full run had one stale Help assertion, corrected and rerun successfully; final suite records are all passing. No approval bypass was used.
