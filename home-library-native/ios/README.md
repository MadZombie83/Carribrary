# iPhone: use the hosted companion

For everyday use open your library HTTPS address followed by `/companion/` in Safari, Share → Add to Home Screen, then pair from the server. The server hosts this PWA; no Mac or native iOS build is required. The Swift project below remains optional source for future native development and has not been Xcode/device verified.

# Carribrary iPhone companion — source preview 0.1.27

This is a native SwiftUI companion for the existing Carribrary server, targeting iOS 17 and later. It includes a checked-in Xcode project, shared test scheme and the established Carribrary icon. No third-party iOS packages or project generators are required to open it.

**Status: source preview, not an installable or verified iPhone release.** The project was prepared on Linux. Swift grammar and project/resource structure were checked here; Xcode compilation, XCTest execution, Keychain identity discovery, TLS client-certificate authentication and real camera/UI behavior require a Mac and iPhone. Do not distribute this as a finished App Store build until the acceptance checks below pass.

## What is included

- Theme-colored banner with the server’s library name and editable sayings; search, Add book, Check out, Return, sorting and 10/25/50 page sizes beneath it. Catalog, On loan, Borrowers and More navigation at the bottom.
- Camera ISBN/EAN, copy-label and private pairing-QR scanning, plus manual entry.
- Open Library metadata lookup, cover thumbnails, book editing/deletion and explicit duplicate-copy confirmation. A new lookup clears previous metadata; each cover view is keyed to its URL.
- Borrower creation/deletion, lending, returns, loan history, due dates and prepared text/email receipts. The owner reviews the message and taps Send; there is no automatic messaging service.
- Requests: approve, decline and hand over a book; handover can create/match the borrower on the server.
- Shared catalog link/QR, enable/replace/disable sharing, library name/saying editing, paired phone creation/revocation, CSV export and JSON backup/restore.
- Read-only encrypted offline catalog. Mutations require a connection. An uncertain mutation is saved encrypted with a stable request ID and retried before another change; server revision checks resolve conflicts. This is not an offline editing/synchronization system.

The current Windows, Flatpak and Raspberry Pi server releases remain the authoritative database. Use server 2.0.29 for this source preview. Its permanent-copy-identity migration changes the library to format 2; keep a complete backup first and do not downgrade. Android remains the native companion.

## Open and run later on a Mac — no terminal commands required

1. Extract the iPhone source ZIP onto the Mac.
2. Install Xcode from Apple, then double-click `HomeLibrary.xcodeproj`.
3. Select the **HomeLibrary** project, the **HomeLibrary** target and **Signing & Capabilities**. Select your Apple development team. Choose a unique bundle identifier for your publisher before distribution. Automatic signing is already selected; no developer identity or private signing key is included.
4. Connect an iPhone by USB, trust the Mac and enable Developer Mode on the phone if Xcode requests it. Choose that phone as the run destination and click the triangular **Run** button. A simulator can test layout/manual entry but cannot validate the real scanner or text sending.
5. Choose **Product → Test** to run the included protocol/model tests.
6. On the Carribrary server, open **Paired phones → Add phone**. Use the HTTPS phone/API address (normally port 7443), not the admin or public catalog address. For use outside the home, choose the already configured reachable DuckDNS/domain address.
7. On the iPhone, tap **Connection → Scan pairing QR**, scan the private Add Phone code, then tap **Pair phone**. Allow camera and local network access. Verify the QR was displayed by your own server before trusting it. Pairing codes expire after five minutes.
8. The public **Share by QR** code is for browsing/requesting books; it is not an admin pairing code. Your wife can pair her phone once and then use the ordinary catalog/lending screens.

Without access to a Mac/Xcode signing environment, this source ZIP cannot be installed on an iPhone like an Android APK. After a verified signed build exists, TestFlight is the intended simple installation path for testers/family, followed by App Store distribution. Publisher account, signing, review and distribution setup remain future work.

## Connection and security

Pairing consumes the same version-1 `hl-pair:` QR used by Android. The app generates an RSA-2048 private key in its own Keychain, wraps Apple’s PKCS#1 public key as DER SubjectPublicKeyInfo, and signs the existing server pairing proof with RSA PKCS#1 v1.5 / SHA-256. The server issues that phone a client certificate. Only approved, unrevoked phone certificates can access `/mobile/*`.

The phone pins the server’s SHA-256 public-key fingerprint from the private QR and binds requests to the paired HTTPS host/port. It validates the pinned certificate as its explicit trust anchor, supplies its Keychain identity for client authentication, rejects redirects and has no trust-all or HTTP fallback. RSA keys are Keychain protected; they are not Secure Enclave keys. Apple client-identity lookup and trust behavior must be validated on a real device before release.

The cache and pending action file use AES-GCM with a device-only Keychain key, are excluded from device backup and use complete file protection. Credentials remain device-only. Reinstalling/replacing a device requires pairing again. Revoking a device on the server removes its API access, but cannot remotely erase its offline cache; protect the phone with a passcode.

This preview uses the QR’s configured address directly. It does not implement Android’s UDP automatic discovery, which would need additional Apple network design/permissions. A router that cannot reach its public domain from inside home Wi-Fi may need split DNS/hairpin NAT configuration; changing an already pinned address is intentionally not an insecure fallback.

ISBN lookup contacts Open Library directly over HTTPS. Find a cover uses the authenticated server API (requires server 2.0.12 or later), which searches Open Library edition/cover IDs, matching Internet Archive item thumbnails and, if configured, Google Books as a third source. Google-sourced covers include the supplied attribution graphic and a book link. ISBN/title/author are sent to those services; borrower information is not. Cover suggestions are previewed before the user chooses and saves one. Cover URLs can also contact the provider recorded for a book; HTTP-only cover URLs display a placeholder rather than relaxing iOS transport security. Offline book metadata is cached; cover-image offline availability is not guaranteed. The app has no analytics, advertising SDK or publisher-operated cloud service. Exported backups include private borrower contact/history data. The privacy manifest declares app preferences access; review privacy disclosures against the final published behavior and replace publisher/support placeholders before submission.

## Mac/iPhone release acceptance checks

Use a separate test server and sample records, not the household production library.

- Build both Debug and Release, run all XCTest tests, and archive successfully. Resolve all compiler/signing/asset/privacy-manifest errors.
- Pair over home Wi-Fi and cellular using the actual server. Confirm client identity lookup works after force quit/relaunch and phone reboot. Declining camera/local-network permissions must leave manual entry/help usable.
- Reject expired/used pairing codes, HTTP addresses, different server keys, expired certificates and redirects. Revoke the phone and verify requests stop. Test pairing to another server without exposing the previous server’s cache.
- Add two different books quickly; verify title/author/cover remain matched after lookup, sorting, paging and edits. Verify duplicate confirmation, ISBN-10/13 edition matching and manual entry.
- Add/remove borrowers, check out with/without due date, return, view history, and prepare both text and email receipts. Cancel composers without sending; the app must never send automatically.
- Submit a shared catalog request and approve/decline/hand over. Verify borrower privacy on the public catalog and unchanged public cover thumbnails.
- Change themes/name/sayings, relaunch and refresh other clients. Verify shared sayings update and independent phone theme persists.
- Interrupt a checkout after server acceptance, relaunch and refresh. Verify exactly one loan exists. Test a concurrent Android edit causing revision conflict, with a clear refreshed result and no duplicate action.
- Verify cache remains browsable offline, mutations remain disabled, export/import round-trips all records, malformed backups are rejected, restore requires confirmation, and sharing/revocation actions behave correctly.
- Check Dynamic Type, VoiceOver, dark mode, small screens, landscape, iPad, keyboard avoidance and camera dismissal.

## Files and maintenance

`HomeLibrary/*.swift` contains the app; `HomeLibraryTests/ProtocolTests.swift` contains the Mac-side tests. `scripts/generate_project.py` regenerates the deterministic checked-in project if source files are added. `scripts/validate_source.py` performs Linux-side structural validation (and optional tree-sitter Swift parsing). It does not compile Swift or run XCTest.

Apple references: [authentication challenges](https://developer.apple.com/documentation/foundation/handling-an-authentication-challenge), [manual server trust](https://developer.apple.com/documentation/Foundation/performing-manual-server-trust-authentication), [client identity credential](https://developer.apple.com/documentation/foundation/urlcredential/init%28identity%3Acertificates%3Apersistence%3A%29), [Keychain identity lookup discussion with Apple DTS](https://developer.apple.com/forums/thread/762996), [local network privacy](https://developer.apple.com/documentation/technotes/tn3179-understanding-local-network-privacy), [privacy manifest reasons](https://developer.apple.com/documentation/technotes/tn3183-adding-required-reason-api-entries-to-your-privacy-manifest).

Reading tracker (source preview): household/borrower profiles, reading entries and personal link/QR management mirror server 2.0.20. Personal borrower browser pages work independently of this native source project. This source has not been compiled or tested with Xcode or an iPhone.

0.1.5 adds searchable Help, four main navigation destinations, and borrower account approval/recovery controls in More. Borrowers use the server’s browser sign-in page on Safari; the native project remains for admins. Source parsing only, no Xcode or device verification.

0.1.6 adds Recycle Bin, bulk metadata editing, SMTP reminder settings, borrower reminder permission, setup checklist and explicit account unblocking/relinking. Source grammar/project checks only; still needs Mac/Xcode/device verification.

0.1.7 adds account-only deletion and permanent personal-information deletion with preview, confirmation and optional stored-backup cleanup; bundled Help is updated. No Mac/Xcode/device validation is claimed.

The shared borrower website can also be added to an iPhone Home Screen through Safari; this works separately from building the native admin companion. Offline help describes both roles.

## iPhone companion, descriptions, branding and optional push (2.0.23)

The iPhone admin companion is hosted by the server at **/companion/**. Open it in Safari, Share → Add to Home Screen, open the icon, then pair using a private one-use code from the server’s **iPhone companion** button or Android Manage library. Codes expire in 10 minutes; browser admin authorization lasts 30 days. Manage/revoke these browsers separately from native paired phones. The shared-library QR grants no admin privileges. Private catalog responses are not cached for offline use. Android stays native. Linux/Pi restrictive Caddy allowlists must add /companion/*; no additional ports. Windows public HTTPS proxy already covers this path.

ZXing Browser 0.2.1 is bundled for Safari barcode scanning with manual fallback; no third-party script CDN. About this book opens a short description, using manual text first, then Open Library and optional Google Books. The popup credits the source; metadata lookup does not save over a book. Library settings can upload a custom image, automatically fit to square PNGs, or restore the default. Server colors now persist across borrower catalog, signup, sign-in, account and Home-screen launch. Android can publish its local colors through Manage library. Custom images and manual descriptions are public library branding/catalog data and included in backups. Native Android’s launcher icon remains Carribrary.

Borrowers can opt into due-date Web Push from an approved account. iPhone admin PWAs can opt into new-request Web Push. iPhone requires iOS 16.4+ and a Home-screen installation. Permission is requested only after a tap. Server creates VAPID keys automatically; no messaging-provider account is needed. Push messages are empty and workers show generic text. Endpoints/keys are protected on the server, no private borrower/book contents are sent through push. Schedule: up to two days before due, due date, and once at least seven days overdue; new admin requests are checked every minute. Returned loans, blocked/deleted accounts and revoked browsers are excluded. Delivery depends on the browser/OS/service, is deduplicated, and is not guaranteed. Email and tap-to-send texts remain independent. Native Android request alerts use the separate periodic scheduler described below.

Android native request alerts: Manage library → Enable background request alerts, then grant notification permission. Android polls the paired server about every 15 minutes, subject to battery/OS scheduling; these are not instant push. Existing pending requests are the initial baseline. Generic alerts open Requests; disable or Forget connection cancels them. No PWA or Firebase account is needed; only request-ID hashes are kept for deduplication.

## 0.1.8 source / server 2.0.24
Personal TBR includes Want to read and Currently reading. Mark as read retains history. The installed iPhone PWA receives the catalog-first borrower and admin TBR updates from the server. Native Swift source still requires Xcode compilation and device validation; this Linux workspace cannot supply a verified IPA.

## 2.0.25 preview: lending and data controls
See distribution/RELEASE-2.0.25.md for holds, atomic batch lending, five-book default limits, protected history clearing and borrower self-service account deletion. Back up and update the server first, then paired clients. Books to Buy and whole-catalog borrower downloads are still planned. Google Books paid-distribution permission and the published store deletion URL remain unresolved release gates.


## Carribrary 2.0.26: the new product name

Home Library is now Carribrary. Export a backup and install over the existing server, then update Android with the Carribrary APK. Do not uninstall or clear app data. Library names, custom library icons, books, borrowers, accounts, reading lists, settings, sharing links and phone pairing are preserved. Windows shortcuts and the service display name now use Carribrary; existing installations keep their registered install directory. The book-and-house artwork is unchanged.

Technical identities intentionally stay compatible: Android com.household.homelibrary, Flatpak com.household.HomeLibrary on stable, Pi home-library-server/home-library, Windows HomeLibraryNative, old scheduled task names and data/key directories. These are not separate installations. iPhone and borrower Home-screen apps keep the household library name/icon; reopen or refresh after updating the server. Native iPhone source 0.1.11 still needs Mac/Xcode verification.


Carribrary 2.0.27 — private lists, Books to buy and shelf locations

Update the server first, then Android, retaining application data. Borrowers set up
a separate private-list passphrase and save their recovery code at first use.
Personal TBR, reading, notes, ratings and wishlists are encrypted on the borrower
device; admins manage lending but cannot view these through Carribrary. Unlinked
household reading profiles remain admin-managed. Old plaintext backups, exports
and device caches are not retroactively encrypted. A host that modifies delivered
browser code is outside this protection. Keep the recovery code safely: resetting
the sign-in password cannot unlock private lists.

Library JSON exports and automatic library snapshots exclude borrower vaults and
credentials. Borrowers can export/import their own encrypted private backup. For
server disaster recovery, stop the server and privately back up the complete data
directory and keyring together; platform key protection may prevent portability.
Account deletion removes the encrypted vault in either deletion scope.

Books to buy holds intended acquisitions without allocating Copy IDs. Confirm
purchase to add a physical copy. Room, bookcase and shelf are admin-only fields;
borrower catalogs, shared lists and private reading views omit those locations.
Use the matching 2.0.27 Help for complete steps.


2.0.29 recovery and illustrated onboarding
Server Help → Illustrated quick start has current fictional-library screenshots,
short success checks and Print / save as PDF. Normal offline phone Help remains
available as text. Library JSON exports are not complete account/key/vault backups.

Windows: Start → Carribrary → Update and recovery (administrator prompt) works
independently of the main server. Create recovery point briefly stops the library
and saves verified server/tray programs and the complete library-data directory.
Updates do this automatically, then verify new files, startup and catalog access.
Compatible failed updates attempt program recovery. Select a prior point and
Restore selected application to keep CURRENT data while restoring compatible
program files. Only 2.0.29 program compatibility is supported here. Historical data
is never automatically restored; incompatible schema rollback remains planned.

Points live under ProgramData/HomeLibraryNative/recovery, accessible only to
Windows administrators and SYSTEM. Local registry seals bind trusted manifests.
Recovery-attempt safety copies are retained but not offered as prior versions.
Use Delete recovery points and type DELETE RECOVERY POINTS to remove them, losing
rollback capability. Ordinary account/backup cleanup cannot remove these points.
Old snapshots/external copies can retain historical private data. No automatic
retention cleanup is applied; monitor disk space. Uninstall keeps recovery data.

These are same-machine core-library recovery points, not complete Windows or
portable disaster backups. Caddy's external certificate folder and scheduled-task
definitions are separate; program recovery keeps the current proxy/tasks in place.
Windows key protection can tie data to the original machine. Keep separate private
off-device backups. Linux/Pi retain existing stopped-server backups and package
updates; guided rollback there is planned. Native iPhone source requires Xcode.

2.0.29: Book.copyUid preserves permanent copy identities in encoded state. Library format 2 requires server 2.0.29 or later; retain a complete backup before upgrading. Mac/Xcode/device verification is still required.

2.0.31: optional password-protected complete server-data backup is available in Data. Ordinary library JSON export/restore remains separate. Restore complete archives with the server stopped using the offline recovery utility. This iPhone source remains unverified without Mac/Xcode; hosted iPhone PWA uses the same Backups dialog as the server.
