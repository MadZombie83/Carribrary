# Carribrary 2.0.54-preview — source changes

This iteration adds the owner-approved server update notice and aligns signed-in borrower pages with the existing dashboard design. The owner builds the installer packages. No 2.0.53 assets or release tags are replaced.

## Server updates

- Read-only checks of the fixed official GitHub releases API, shared cache (normally 24 hours), one-minute minimum between manual checks and a shorter retry interval after failure.
- Preview installations consider previews and stable releases; stable installations exclude previews. Versions compare numerically, including promotion from preview to stable.
- Dismissible notice with backup reminder, release notes and server downloads. Dismissal is per browser and per version. Server updates at the bottom of the administrator page permits manual rechecking.
- No installer download, execution, automatic update or database migration is performed by the checker. Local administrator sessions and approved paired tokens retain existing server-side authorization. Borrowers have no update endpoint.
- GitHub receives the server public IP and a generic update-check user-agent; no library/borrower data, configuration secrets or personal identifiers are sent in requests.

## Borrower dashboard

- Reuses admin-shell.css sidebar/card/responsive components, shared presentation icons, whole-word title fitting and a shared mobile drawer controller. Borrower navigation contains Catalog, My reading, My loans and Account only.
- Catalog still opens first. Overview cards show the account's own loans/due dates and requests/holds. Server projection includes only that borrower's manual holds; it exposes no other borrower identity or private reading data.
- Existing search, collection, sort, page size, suggestions, encrypted reading, renewal, deletion and PWA installation workflows remain available.
- Full-height saved sidebar artwork, icon, name, subtitle and saying follow the current library branding. Drawer supports Close, outside click, Escape, focus return and keyboard focus containment.
- Self-service buttons appear only from the authorized self-service status. Existing server checks remain authoritative. Return reports remain active loans until the librarian confirms physical receipt.

## Upgrade and validation

Library format remains 7 and credential format remains 2. Create and verify a complete backup before updating. Install over the existing installation; preserve data and signing identities. No additional administrator login is introduced for already paired devices.

Help, privacy text and build identities are synchronized for 2.0.54-preview. This change does not complete remaining physical-device, installer, scanner/card-printing or Play Store testing.

## Optional mobile borrower artwork

Theme & artwork can save a separate 512 × 1024 portrait PNG (2 MB resized limit) for borrower browser/PWA pages up to 900 CSS pixels wide. Main artwork remains the fallback and is unchanged on native Android librarian screens. Both image choices travel with saved themes and backups; New theme and preset selection clear both. Only public image URLs/hashes are added to branding responses. Older clients that omit the optional theme property preserve an existing saved mobile image; explicit empty values remove it. Library format remains 7; older versions do not render the optional image and may drop it when saving, so do not downgrade after configuring it.
