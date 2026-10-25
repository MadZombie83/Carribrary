# Carribrary 2.0.39 — administration function inventory

Every legacy administration function retains its existing action/API handler and permission boundary. The six category routes are genuine navigable client pages; setting editors use the page rather than a modal backdrop. Operational dashboard and primary-sidebar shortcuts remain available. No library branding is embedded in these components.

| Existing function / control | New category | New page | Safety / behavior |
|---|---|---|---|
| Library name (`rename-library`) | Library & Appearance | `#settings/library-appearance/rename-library` | Change the name shown on library pages. |
| Library icon (`library-icon`) | Library & Appearance | `#settings/library-appearance/library-icon` | Upload your own icon, preserving transparency. |
| Library sayings (`edit-library-name`) | Library & Appearance | `#settings/library-appearance/edit-library-name` | Edit the subtitle and encouraging message. |
| Colors & saved themes (`palette`) | Library & Appearance | `#settings/library-appearance/palette` | Choose a preset or edit a named custom palette. |
| Custom theme editor (`custom-theme`) | Library & Appearance | `#settings/library-appearance/custom-theme` | Full color pickers, hex values and readable contrast. |
| Full sidebar overlay (`sidebar-artwork-settings`) | Library & Appearance | `#settings/library-appearance/sidebar-artwork-settings` | Upload a portrait PNG beneath the sidebar navigation. |
| Collections (`manage-collections`) | Books & Organization | `#settings/books-organization/manage-collections` | Organize books without separating household borrowers. |
| Print copy labels (`copy-labels`) | Books & Organization | `#settings/books-organization/copy-labels` | Optional labels for individual physical copies. |
| Copy numbering (`copy-numbering`) | Books & Organization | `#settings/books-organization/copy-numbering` | Choose gap filling or the next catalog number. |
| Books to buy (`books-to-buy`) | Books & Organization | `#settings/books-organization/books-to-buy` | Manage the private acquisition wishlist. |
| Bulk catalog editing (`bulk-edit`) | Books & Organization | `#settings/books-organization/bulk-edit` | Select copies in Catalog, then review changes together. |
| Share the library / QR (`share-qr`) | Books & Organization | `#settings/books-organization/share-qr` | Manage the public book-list link and QR. |
| Share a text book list (`share-list`) | Books & Organization | `#settings/books-organization/share-list` | Review the current Catalog search and sort before sharing. |
| Books on loan (`active-loans`) | Borrowing & Reminders | `#settings/borrowing-reminders/active-loans` | Review active loans, due dates and per-copy returns. |
| Lending & holds (`lending-tools`) | Borrowing & Reminders | `#settings/borrowing-reminders/lending-tools` | Holds, handover, batch checkout and batch returns. |
| Lending limits (`lending-limits`) | Borrowing & Reminders | `#settings/borrowing-reminders/lending-limits` | Set batch and per-borrower limits; returns stay unrestricted. |
| Book requests (`requests`) | Borrowing & Reminders | `#settings/borrowing-reminders/requests` | Review requests without starting loans before handover. |
| PWA notification status (`push-overview`) | Borrowing & Reminders | `#settings/borrowing-reminders/push-overview` | Review optional borrower reminders and admin request-notification delivery. |
| Automatic email reminders (`reminders`) | Borrowing & Reminders | `#settings/borrowing-reminders/reminders` | Configure SMTP and consent-based due-date reminders. |
| Borrower accounts (`accounts-button`) | People & Access | `#settings/people-access/accounts-button` | Approve, block, relink or remove borrower sign-ins. |
| Dashboard administrators (`dashboard-accounts`) | People & Access | `#settings/people-access/dashboard-accounts` | Individual administrator passwords, recovery and sessions. |
| Android phone pairing (`devices`) | People & Access | `#settings/people-access/devices` | Pair approved phones and revoke their access. |
| iPhone / browser pairing (`iphone-companion`) | People & Access | `#settings/people-access/iphone-companion` | Approve or revoke token-based browser companions. |
| Household readers (`household-readers`) | People & Access | `#settings/people-access/household-readers` | Manage household profiles in Reading tracker. |
| Backups (`export`) | Data & History | `#settings/data-history/export` | Complete password-protected server backup or library JSON export. |
| Export catalog CSV (`export-csv`) | Data & History | `#settings/data-history/export-csv` | Download book details for spreadsheets. |
| Loan history (`history`) | Data & History | `#settings/data-history/history` | Review checkouts and completed returns. |
| Recycle Bin (`recycle-bin`) | Data & History | `#settings/data-history/recycle-bin` | Review recoverable books and borrowers; permanent removal is confirmed. |
| Restore library export (`import`) | Data & History | `#settings/data-history/import` | Separate care section; original preview/confirmation retained. |
| Clear completed history (`history-controls`) | Data & History | `#settings/data-history/history-controls` | Separate care section; original preview/confirmation retained. |
| Setup & updates (`setup`) | System & Advanced | `#settings/system-advanced/setup` | Server details, DNS / DuckDNS checks, remote access, pairing and updates. |
| Google Books integration (`google-cover-settings`) | System & Advanced | `#settings/system-advanced/google-cover-settings` | Optional user-provided API key; never included in library exports. |
| Help & instructions (`help-button`) | System & Advanced | `#settings/system-advanced/help-button` | Search the synchronized guide and illustrated quick start. |

## Primary and nested operations retained

- Dashboard, Catalog, Borrowers, Lending, Requests and Reading tracker remain primary. Header catalog search, Help, Add book, account identity and sign-out remain. A visible header connection status replaces the old footer-only status.
- Catalog: add/edit/delete/recover copies, ISBN/copy-label/manual lookup, duplicates, covers/Google credit/About, search, collection, sorting, paging, selected-copy editing, checkout, returns, reading and receipts.
- Borrowers: add/edit/consent, delete/recover and active-loan counts. Household readers remain under Reading tracker; encrypted borrower personal data is never fetched by administration pages.
- Lending: holds, ready/release/handover, batch checkout/return, batch and per-borrower limits, active loans. Requests: approve/decline/physical handover, search/status filters. Approval still does not start a loan.
- Appearance: subtitle and encouraging saying; name; icon upload/reset; preset theme and saved custom theme create/edit/select; sidebar overlay upload/preview/remove. Old library settings shortcuts for icons, Google covers, notification delivery and sidebar artwork still work inside their existing editor.
- Access: administrator creation/password/recovery/removal; borrower approval/block/unblock/link/relink/delete; Android address/QR/manual pairing/copy/expiration/revoke; iPhone code/QR/device revocation; push opt-in/status. All nested controls keep their existing guards.
- Data: complete password-protected backup, smaller JSON export, CSV export, JSON file chooser/review/restore, history, scoped preview/typed history clearing, Recycle Bin restore/purge. Complete-backup recovery remains an offline operation described by the setup guide.
- Advanced: SMTP save/test/delivery history; Google key configure/test/disable; setup/status/DNS/DuckDNS/remote guidance, pairing and backup shortcuts; help search, full guide, illustrated quick start and print/PDF. OS-managed update/recovery/start/stop helpers remain in the installed applications.
- Dashboard Add book, Hold, Batch checkout and Backup shortcuts keep direct access; other dashboard links retain their existing handlers.

## Page behavior and safe navigation

Settings homepage and category URLs support browser Back/Forward and direct links. Inline editor Back returns to its category. No automatic file restore/download occurs merely by opening its route: CSV has an explicit download button and Restore an explicit file chooser. Existing backup busy/cancel protections block navigation while preparing the protected archive. Colors and custom-theme controls are absent from Catalog. Destructive account, device and Recycle Bin actions retain the existing confirmation within their tools.

## Sidebar image and title specifications

PNG input: under 10 MB, at most 8192 pixels per side. Saved portrait overlay: 512 × 1536 pixels, up to 2 MB, fitted with alpha preserved. Existing 512 × 256 saved artwork is accepted, with possible cover cropping; re-upload a portrait original for best results. The image covers the full sidebar below the interactive layers, with a dark scrim and pointer-events disabled. No artwork is installed by default. Preferred name type reduces only to 1 rem, then stacks the logo above the title. Very long unbroken names scroll inside their label rather than splitting characters. Preset/custom themes use the same fitter and components.
