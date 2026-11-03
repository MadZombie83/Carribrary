# Carribrary — full feature list

**A shelf worth sharing.** Carribrary helps a household, church, reading group or small community organize physical books, lend them to people and keep reading together.

Developed by **Chezburber Studios**. This inventory describes **2.0.59-preview source**, checked November 3, 2026. The published server installers are currently **2.0.58-preview**; 2.0.59 must be built to receive its recovery repairs. Preview testing and real-device acceptance are ongoing. See [downloads](https://github.com/MadZombie83/Carribrary/releases), [screenshots](SCREENSHOTS.md) and [current source Help](Carribrary-Help-2.0.59.md).

## 1. One library, several ways to use it

- Free self-hosted server with a complete browser-based librarian dashboard. The optional mobile companion is not required to run or administer the library.
- Windows installer, Intel/AMD Linux desktop Flatpak, and Raspberry Pi ARM64 DEB for Pi 4/5 running 64-bit Raspberry Pi OS.
- Optional native Android librarian companion for scanning and administration. Google Play publication is planned; the intended price is a one-time US$15 purchase, subject to the eventual store listing/local pricing. No public APK/AAB download is supplied on GitHub.
- Hosted iPhone/browser librarian companion that can be added to the Home Screen. This is a browser/PWA companion, not an App Store release.
- Free borrower website/PWA, usable from a library-sharing link on phones, tablets and computers.
- Home-network operation and optional remote access through HTTPS. The server computer must remain on and reachable.
- One household library can contain multiple named collections—for example, adult books, children's books or church resources. Collections share borrowers and circulation; they are not separate servers or security boundaries.

## 2. A catalog for physical copies

- Individual records for physical copies; multiple copies of the same title/ISBN are supported.
- Separate ISBN/EAN and library copy number, plus permanent physical-copy identities.
- Titles, authors, series, publishers, genres where recorded, cover images and book descriptions/about panels.
- Collections and private shelf locations, including room, bookcase and shelf.
- Add, edit, delete/archive and restore catalog records.
- Search by title, author, series, ISBN or copy ID.
- Filter by collection; sort by title, author, series or catalog number in either order.
- Choose 10, 25 or 50 items per page.
- Duplicate warnings that can be confirmed when adding another intended physical copy.
- Configurable copy numbering: fill unused catalog-number gaps or continue with the next number. Permanent label identities remain separate from reusable display numbers.
- Optional copy barcode/QR labels and printing. Custom labels are a convenience, not a requirement for ordinary lending.

## 3. Faster catalog entry and organization

- Camera scanning, keyboard/USB scanners and manual entry fallbacks in supported interfaces.
- ISBN lookup for available book details; users can correct or enter missing information.
- Cover lookup from Open Library/Internet Archive sources, with optional Google Books fallback configured by the server owner using their own API key.
- Title-and-author cover searches when the ISBN has no suitable cover, with review before choosing a potentially different edition.
- Manual cover URLs and placeholders when no cover is available. Metadata and artwork availability are never guaranteed.
- Batch ISBN entry and continuous scanning queues, with retry, editing and duplicate review before saving.
- Up to 1,000 reviewed books per batch, added together or not added at all; existing records are not replaced.
- CSV import with column mapping and review, for suitable exports from other library apps.
- Bulk catalog editing for selected copies, with a review step and validation; up to 500 selected books.
- Catalog CSV export and library JSON export/restore. These are distinct from complete recovery backups.

## 4. Checkout, returns and holds

- Borrower records do not require an online account, email address or phone.
- Checkout and return by existing ISBN/EAN, optional copy label, or manual search/entry.
- Automatic copy selection when exactly one eligible copy matches; a choice of physical copies when several match.
- Checkout offers available copies; return resolves checked-out copies.
- Loan dates, due dates, current borrower, overdue visibility and circulation history.
- Requests and holds reserve a copy while the librarian reviews/prepares it.
- A hold does not become a loan until the book is handed to the borrower.
- Batch checkout and batch return, with configurable checkout/per-borrower limits; returns remain unrestricted by those limits.
- Borrower renewal requests with librarian approval and configurable renewal rules.
- Prepared borrowing receipts opened in the librarian's text/email app for the librarian to send.
- Clear distinction between a borrower reporting a return and the librarian confirming physical receipt.

## 5. Sharing and guest access

- Share a live catalog by invitation link or downloadable QR code.
- Share a text book list separately.
- Cover thumbnails, book descriptions, search, collection filters, sorting and pagination in the shared catalog.
- Visitors with a valid library link can request a book without creating an account.
- Visitors can suggest that the library acquire a book or series.
- Sharing links can be replaced or disabled.
- A branded welcome page at the plain server address does not reveal the private catalog-sharing token.
- Public catalog views omit borrower details, private reading information and shelf locations.

## 6. Optional borrower accounts

- Account creation is optional; guest borrowing requests remain available.
- Librarian approval before an account receives approved-account functionality.
- Catalog-first sign-in, with dedicated My reading, My loans and Account areas.
- The same catalog search/filter/sort/page-size controls remain available after sign-in.
- Own loans, due dates, requests, holds, renewal requests and permitted self-service actions.
- Cancel eligible borrowing requests.
- Optional email password recovery when configured, or a librarian-issued reset link.
- Librarian approval, blocking, unblocking, borrower linking/relinking and account/data removal controls.
- Borrowers can delete their own account, or account plus eligible linked information, with password/identity confirmation and a deletion preview.
- Clear eligible completed history using explicit safeguards; active circulation remains protected.
- Account → This device includes browser-app installation guidance, optional notification controls and device/privacy information.

## 7. Reading, TBR and personal lists

- Separate household reader profiles, including the librarian's own reading profile.
- Borrower-owned private TBR, reading tracker and personal wishlist inside the borrower account.
- Want to read, currently reading and finished states. Marking a book read removes it from TBR while preserving finished history.
- Current page, total pages, completion percentage and progress bars.
- Start/finish dates, star ratings and personal notes.
- Rereading sessions preserve previous reads instead of overwriting them.
- Annual book goals, completed reads, page totals, monthly/yearly summaries, recorded-genre breakdowns and completion streak statistics.
- Favorite quotes and personal notes with optional page numbers.
- Borrower personal reading is encrypted on the borrower side using a separate private passphrase and recovery code. Librarian circulation screens, analytics and exports do not expose those private contents.
- Encrypted personal backup/recovery and lock/unlock controls. Account-password reset does not replace the private-reading recovery secret.
- Household-managed reading is separate from borrower-encrypted reading and can be administered by librarians.
- Finishing a reading entry never returns the physical loan automatically. TBR is not a reservation.

## 8. Acquisition planning

- Librarian Books to buy wishlist, including ISBN scanning/lookup and manual additions.
- Review borrower/guest purchase suggestions for a book or series.
- Track acquisition plans and add acquired books to the catalog.
- Dashboard quick access to Books to Buy and Share Library.
- A borrower's private personal wishlist stays separate from purchase suggestions sent to the librarian.

## 9. Optional self-service and library cards

- Independently enable borrower self-checkout, borrower return reports and authorized kiosks. All start off.
- Approved linked borrowers can use permitted checkout/return actions from their phone browser/PWA.
- Return reports leave a copy on loan pending librarian confirmation of physical receipt.
- Issue, print, replace and revoke physical library cards; digital-card support is also available.
- Cards can help select a borrower at assisted checkout without requiring an online account.
- Restricted self-service station for a shared computer, with card/PIN authentication, book scanning or manual entry, clear receipts and idle reset.
- Authorized kiosk access is separate from full administrator access; kiosk permission alone does not enable borrowing or returning.
- Self-service is trust-based. A scanned barcode cannot prove possession of a physical book.

## 10. Reminders and notifications

- Automatic consent-based email due-date reminders after the server owner configures a sending account.
- Text receipts remain tap-to-send; no automatic SMS provider is required.
- Administrator notification bell for account, borrowing, renewal and purchase requests, holds, pending physical returns, overdue loans, restored-access review and delivery/backup warnings.
- Optional borrower PWA due-date push notifications on supported browsers/devices.
- Optional librarian PWA request alerts and native Android periodic background alerts.
- Generic notification content keeps private reading notes out of alerts.
- Browser/OS permission, supported installed-PWA behavior, a reachable server and delivery services affect notification availability. Email, text and push are separate choices; delivery is not guaranteed.

## 11. Branding and responsive design

- Editable library name, transparent custom icon, banner subtitle and encouraging saying.
- Built-in color presets, full color pickers/hex entry and named saved custom themes.
- Save sidebar artwork with each custom theme; selecting a theme restores its artwork, and no-artwork choices clear previous artwork.
- Full-height sidebar artwork rather than decoration confined to the bottom.
- Separate borrower mobile artwork, with fallback to existing artwork when appropriate.
- Independent portrait/landscape fit, horizontal/vertical focal points and readability overlays, with live previews.
- Full-image/contain and fill/cover choices without stretching the image.
- Separate server sidebar/header readability controls.
- Long library names fit available space, wrap at word boundaries and can place the logo above the title.
- Shared library identity across the welcome page, librarian dashboard, borrower pages, Help and companion interfaces.
- Responsive desktop/tablet/mobile layouts and collapsible navigation on small screens.
- Six Settings & Tools categories, with separate pages, settings search, grouped controls and contextual Help.
- Browser unsaved-change warnings, save progress, Cancel controls and entered-data retention after failed saves in supported forms.

## 12. Backups, recovery and deletion safeguards

- Password-encrypted complete server data backups containing the library, accounts, configuration, protected key material and encrypted borrower vaults.
- Verify archive integrity before recovery, including wrong-password/damage reporting.
- Optional weekly portable replacement-computer backups retaining four managed copies instead of unlimited accumulation.
- Separate graphical Complete Backup Recovery tool, including recovery rehearsal, restoration and retained previous data.
- Guided replacement-computer recovery for supported portable archives, followed by local review of historical account/device access before remote use.
- Interrupted-restore journal and undo handling; failed restored startup attempts preserve/reinstate prior data.
- Windows program-only Update and recovery is distinct from complete data restoration and preserves current data within its supported version cohort.
- Recycle Bin recovery for eligible removed books and borrowers, with safeguards for active circulation and permanently deleting recovery options.
- Confirmed completed-history clearing keeps active loans, holds and unfinished reading protected.
- Backups should be copied off the server; same-drive backups cannot protect against losing that drive.
- Borrower-private reading still requires the borrower's private passphrase/recovery code after a server restore.
- **Version note:** 2.0.59 source repairs stale recovery version checks in 2.0.58 installers. Use the release notes and compatible current software; do not bypass recovery checks or use an old installer to downgrade data.

## 13. Administration, privacy and security

- Individual local administrator accounts, first-run setup, secure password hashing, authenticated sessions, sign-out, expiration and recovery procedures.
- Previously approved remote companions retain token-based access without an extra local-dashboard login.
- Separate Android and browser/iPhone pairing, approved-device lists and server-side token revocation.
- Administrative and borrower permissions enforced by the server, not only by hiding controls.
- HTTPS/certificate-based remote connections, local-network discovery and QR pairing where supported.
- Guided optional public sharing setup, verified Caddy helper installation and DuckDNS instructions/integration. Router and hostname setup still require owner configuration.
- Borrower account approval, request validation, rate limits/lockouts and optional advanced registration protection.
- Selected public branding is separated from private administration and borrower information.
- Native Android has an encrypted cached library for offline reference; stale availability must be refreshed. Offline mutation retries require review.
- Borrower/librarian PWAs require the server and do not promise a full offline catalog. Revoking access does not erase prior downloads or offline copies.

## 14. Self-hosted analytics and support

- Optional Analytics & insights page under Settings & Tools → Data & History; collection starts off.
- Website/catalog activity, book opens, search/zero-result counts, requests, checkouts, returns, catalog growth and popular-book activity.
- Charts and 7-day, 30-day, 90-day and all-time filters.
- Aggregate activity counts, not unique visitor tracking. No stored IPs, visitor identities, search phrases, contact information, secret URL tokens or encrypted private-reading contents.
- Pause/clear analytics independently from the catalog and reading records.
- In-app server update notices linked to official GitHub releases; updates remain installer-driven rather than silently replacing the server.
- Searchable Help, illustrated Quick Start, installed guides, contextual settings Help, troubleshooting, version/copyright identification and third-party notices.
- Windows setup stage/progress feedback and retained local diagnostic logs.
- GitHub Issues/Discussions and dedicated support: chezburbermerch@gmail.com. Never post backups, credentials, private QR codes or borrower information.

## What it is—and what it is not

Carribrary manages **physical books and real lending**. It is not a hosted ebook store, an ebook-content service or an enterprise integrated library system. MARC support is not currently included. A library's own always-on server and any optional email/remote-access services remain the owner's responsibility.

The server is **free and source-available proprietary software**, not open source. See [LICENSE.txt](LICENSE.txt); third-party licenses remain separate. Screenshots use fictional records and original sample artwork. Features marked optional require setup/permission, and some browser/device capabilities vary.
