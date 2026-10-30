> Historical release record. For current installation, recovery and Help, use the current README and synchronized Help. These older instructions do not override current compatibility or safety requirements.

# Carribrary 2.0.43 — batch acquisition, CSV migration, renewals and email recovery

## Batch add and CSV

Windows/Linux/Pi server dashboard and iPhone PWA: Settings & Tools → Books & Organization → Batch add / CSV import. Android: Settings → Books & Organization → Batch add / CSV import. Queue ISBNs with manual entry, keyboard scanner or continuous camera scanning. Background Open Library lookups do not block queuing another ISBN. Move a barcode out of view briefly before deliberately scanning an identical copy again. Pause/retry metadata, edit/remove rows or add a titled book with no ISBN. Review errors/duplicates and confirm one all-or-nothing save. A pre-batch private backup is retained. Existing books are not replaced.

UTF-8 CSV/TSV imports support comma/semicolon/tab separators, quoted commas/newlines, escaped quotes, editable header mapping, blank ISBN, source numbers and private notes. Unsupported columns are retained in private import notes, not fabricated into active loans, accounts or reading histories. New Carribrary copy IDs are assigned. Source numbers never override existing identities. Up to 1000 rows/10 MB per reviewed batch, 25 review rows per page; split larger exports. Save a JSON review/error report. Exact exports from every library application are not certified; use column mapping and test a small real sample. Queues are unsaved and discarded on close; there is no persistent offline acquisition queue in this release.

Conflicting revisions reject the whole save; refresh/review again. Browser interrupted-save retry uses the same operation ID/body. Android retains its protected pending-operation retry flow: close the batch tool, resolve Retry pending change, and inspect the catalog before queuing more.

## Borrower renewals

Signed-in borrowers: My loans → Request renewal → confirm. Default 14-day extensions, at most two approved extensions per loan, librarian approval required. Administrators: Borrowing & Reminders → Borrower renewals; enable/disable requests, change days/limits, optionally auto-approve, approve/decline and see history. Android has the same policy/review tool.

Server checks loan ownership, pending requests, active loan/due date, count limits and conflicting holds/open book requests. Approval rechecks eligibility and original due date; old proposals whose date passed must be declined/re-requested. Overdue requests extend from today; otherwise from the current due date. Return cancels pending extensions. Opening a page never mutates a loan. The existing request bell counts book requests; check Borrower renewals separately for extension requests. This increment uses authenticated My loans, not a separate tokenized renewal-by-email flow.

## Email sign-in password recovery

Configure existing SMTP and opt into the separate borrower email password recovery switch, off by default. It can run without scheduled reminders. Forgot your password? accepts a username and always returns the same generic message. A bounded background worker sends only to the saved account email. Requests are rate-limited, same-account issuance is suppressed for ten minutes, links expire after 30 minutes and work once; reset revokes existing sessions. No token or recipient is logged publicly. Queue jobs do not survive a restart. Actual delivery requires testing the owner's SMTP provider.

Librarian-issued links remain the fallback, including accounts without email and legacy records awaiting encryption. Sign-in reset never unlocks or resets private encrypted reading-list credentials. No real borrower emails were sent during development tests.

## Upgrade and source delivery

Export a complete private backup first. Startup makes a pre-format-upgrade library backup and moves format 3 to format 4, preserving existing identity, accounts, pairings and vaults. Format 4 stores ISBN-less books, private import notes and renewal policy/history. Credential format stays 2. Earlier executables cannot preserve this data; program-only recovery is restricted to 2.0.43. Historical complete recovery is separate and may lose later changes. Do not downgrade the new data with an older installer.

Source-only delivery: owner builds EXE/Flatpak/Pi DEB/Android APK from the versioned source folder. Android versionCode 20043; native iPhone source 0.1.27/build 28 (no Xcode build). Hosted PWA comes from the server. No new installers or app packages were generated here.

## Owner acceptance checks

Test small real exports from the apps you want to migrate from before a large import. Check unsupported notes and source IDs, duplicates, no-ISBN checkout/return by title or optional label, undo via private backups, interrupted saves and a second administrator changing data during review. Test real camera continuous scanning and repeated physical copies on Android/iPhone. Request and approve/decline extensions, reach the limit, return with a pending request and verify history. Configure a test sending mailbox, verify recovery delivery/expiry/replay, existing session revocation and continued private-list locking. Test direct older-version upgrade on disposable data before your live library. Check Help and all platforms after building.
