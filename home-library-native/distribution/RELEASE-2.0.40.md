# Carribrary 2.0.40 — borrower pages and iPhone PWA polish

The shared catalog, borrower sign-in/account and librarian iPhone/browser PWA use the dashboard’s library colors, serif headings, rounded surfaces, grouped controls and responsive spacing. No individual library branding is embedded in the source.

- Shared catalog: branded banner, grouped account/install actions, catalog overview, search/filter card, consistent covers, availability badges and book actions. Guest requests remain available. Empty and unavailable states stay explicit.
- Borrower account: Catalog still opens first, My reading second. Context descriptions and catalog/own-loan counts orient readers. Account groups private reading, device/install/notifications, data/privacy, help and sign-out in responsive cards. Failed cover images become placeholders instead of broken images.
- iPhone/browser administrator PWA: pairing follows the library theme. Settings & Tools home and People & Access expose this device’s request notifications and confirmed sign-out/unpair, previously stranded in the hidden legacy footer. Other devices are unaffected. Existing paired sessions remain recognized; Help, scanning/manual entry and revocation keep their authentication boundaries.

Updated canonical, bundled and borrower account Help. Android native UI is unchanged in this pass; version 20040 and bundled Help align for owner builds. iPhone native source metadata is 0.1.24/build 25, not an Xcode-verified binary. The hosted PWA receives its changes from the server.

Library/credential formats stay 3/2. No new endpoint, schema, access permission or persistent cache introduced. Existing public branding caching and private-vault behavior remain unchanged. Program-only recovery accepts 2.0.39–2.0.40; older portrait-artwork-incompatible programs remain excluded. Export a private complete backup before updating.

Source-only delivery. No EXE, APK, AAB, Flatpak or DEB built or installed here. Owner compiles and tests packages separately; 2.0.39 GitHub assets remain intact. New source archive extracts under Carribrary-2.0.40.
