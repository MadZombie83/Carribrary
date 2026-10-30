# Carribrary 2.0.52 — guided Linux public sharing

**2.0.53 current source update:** Optional borrower self-checkout, pending physical-return reports, branded library cards/PINs and restricted browser kiosks, with native Android administration. All permissions default OFF. See [release notes](distribution/RELEASE-2.0.53.md) and [implementation report](distribution/SELF-SERVICE-IMPLEMENTATION-REPORT.md). Owner builds installers; physical kiosk/card acceptance remains. Library format 7; older program-only rollback is blocked.

**2.0.52 current source update:** Local GUI public-sharing setup for Flatpak/Pi, verified optional Caddy download, protected optional DuckDNS updater and explicit router checks. No terminal setup on Linux; single-server scope. Windows keeps its existing Connection setup. [Release notes](distribution/RELEASE-2.0.52.md). Formats6/2; source only, owner builds packages.

**2.0.51 earlier source update:** Removes the public root install button and its script; Account → This device installation is retained. Saved theme artwork follows borrower pages, librarian PWA and Android banners, including clearing on plain themes. [Release notes](distribution/RELEASE-2.0.51.md). Formats6/2 unchanged; source only, owner builds packages.

**2.0.50 earlier source update:** Shared-link guests and approved borrowers can suggest books or series to acquire, without an ISBN requirement. Admins review suggestions in Books to buy and the notification bell across server/PWA/Android. Shared-catalog and account pages explain that sign-up is optional and adds personal TBR/reading tracking. [Release notes](distribution/RELEASE-2.0.50.md). Library/credential formats remain6/2; source only, owner builds packages.

**2.0.47 earlier source update:** Unified Library identity / Theme & artwork, dashboard sharing/acquisition shortcuts, portable complete recovery and optional weekly backups retaining four. Opt-in aggregate dashboard analytics; reading page progress, preserved rereads, annual goals/statistics and quotes/page notes, including encrypted borrower vault support. Library format upgrades to 6; credentials stay 2. Compatible program-only rollback supports 2.0.47 only. [Release notes and owner checks](distribution/RELEASE-2.0.47.md). Source only; owner builds packages.

**2.0.46 earlier source update:** New theme clears the draft name and sidebar artwork instead of inheriting the last theme's PNG. Selecting a saved theme loads its colors and saved artwork; saving a new no-artwork theme does not alter existing themes. Android has the same reset and selection behavior. Help updated. [Release notes](distribution/RELEASE-2.0.46.md). Library/credential formats remain 5/2; compatible program-only recovery supports 2.0.45–2.0.46. Source only; owner builds packages.

**2.0.45 earlier source update:** Independent Complete Backup Recovery menu/tray wizard for Windows, Flatpak and Raspberry Pi: verify an archive, rehearse in a separate local copy, confirm replacement, retain current data and review historical accounts/devices before restoring remote access. Named custom themes now save sidebar artwork with their colors. Library format upgrades to 5 with a private pre-upgrade snapshot; credentials remain 2. Compatible program-only recovery is 2.0.45 only. [Release notes and owner checks](distribution/RELEASE-2.0.45.md). Source only; owner compiles packages.

**2.0.44 earlier source update:** Verify a saved complete backup from Data & History on the dashboard, iPhone PWA and native Android. The authenticated tool checks encryption, all file hashes, SQLite integrity and recorded library format in isolated protected storage, removes temporary data and never restores a live library. An independent `--verify-complete-backup ARCHIVE` command prompts for the password even when the dashboard cannot start. [Release notes and owner checks](distribution/RELEASE-2.0.44.md). Android JSON restore now accepts format 4; in-page Settings headers stay in normal flow. Library format stays 4 and credentials stay 2; compatible program recovery supports 2.0.43–2.0.44. Source only; owner builds packages.

**2.0.43 earlier source update:** Batch ISBN queues and mapped CSV imports, borrower renewals and opt-in email password recovery. [Release notes, limits and owner test checklist](distribution/RELEASE-2.0.43.md). Back up before upgrading: library format 4 adds ISBN-less books/import notes/renewals; older executables cannot read it safely. Program recovery is restricted to 2.0.43. Source only; owner builds packages.

**Earlier 2.0.42 source update:** Android and iPhone PWA share four colored whole-library summary panels, functional request bells and dashboard-style search/navigation. Android adds Home quick actions; both companions offer Home, Catalog, Loans, Reading and Settings. See [release notes and hardware checklist](distribution/RELEASE-2.0.42.md). Source only; owner builds packages. Compatible recovery: 2.0.39–2.0.42.

**Earlier 2.0.41 source update:** Plain public addresses open a branded welcome with invitation guidance, without revealing the catalog token. Local administrators open Dashboard. Android Settings & Tools has six searchable categories and refreshed remaining screens; household Reading/history/details match dashboard surfaces. See [release notes](distribution/RELEASE-2.0.41.md) and [package/hardware checklist](distribution/RELEASE-TEST-CHECKLIST-2.0.41.md). No installers built here; owner compiles packages. Recovery supports 2.0.39–2.0.41.

**Earlier 2.0.40 source update:** Shared catalog, borrower sign-in/account and the librarian iPhone PWA follow dashboard theme tokens, responsive cards and clear availability. Catalog-first navigation, guest requests, private reading, account controls and pairing remain. iPhone notification/unpair controls are visible under Settings & Tools home / People & Access. See [release notes](distribution/RELEASE-2.0.40.md). Source only; owner builds packages. Program recovery supports 2.0.39–2.0.40.

**Earlier 2.0.39 source update:** Catalog/Borrowers cards and Lending/Requests workspaces follow the dashboard style. Settings & Tools replaces the four old utility sidebar entries with six navigable category pages, searchable cards, breadcrumbs and in-page editors. Colors/themes, sharing utilities and collection management live there; ordinary catalog filters and lending actions remain. Library names automatically fit whole words, shrinking to a readable minimum before stacking the logo. Uploaded PNG artwork covers the entire sidebar below a readability layer, not just its bottom. See the [complete function inventory](distribution/ADMIN-TOOLS-INVENTORY-2.0.39.md). Source only: no installers built or live library changes. Existing settings, accounts, pairings and data are preserved. Older programs cannot read the new portrait artwork; program recovery is restricted to 2.0.39.

**Earlier account/artwork update (2.0.36):** Removes the built-in sidebar drawing. All tools → Sidebar artwork now uploads/removes your own optional PNG, also accessible from Library settings. All tools → Dashboard accounts creates separate full-administrator sign-ins, passwords, personal recovery codes and local sessions. The original administrator migrates safely; paired-device tokens and borrower accounts remain independent. Back up before updating: protected dashboard credentials upgrade to format 2, unreadable by earlier programs, so program-only recovery is restricted to 2.0.36. The owner builds packages with [BUILD-LOCALLY.md](BUILD-LOCALLY.md); none were built/installed here.

An independently hosted ASP.NET Core / SQLite Windows library server, Windows tray companion, and Kotlin Android scanner/client. No ChatGPT hosting or Tailscale dependency. The selected Windows, Linux or Raspberry Pi server is the single authoritative database; Android caches an encrypted read-only snapshot while offline.

## Click-through Windows installer

After building, use **Carribrary-Setup-2.0.46-preview.exe** for installation without commands. Export a backup first, close the previous Node server window, then double-click the installer. The wizard can import your JSON backup; it configures the service, firewall, shortcuts and optional tray startup. Existing native data and connection settings are retained on updates. The Start menu **Connection setup** window configures a remote hostname, optional encrypted DuckDNS credentials and optional public sharing. Router port forwarding is still done in the router interface. See `INSTALLER-GUIDE.txt`. The personal installer is unsigned; physical Windows execution remains unverified here.

The ZIP/manual instructions below remain available for developers.

## Windows installation and migration

Use the **Windows x64 ZIP**, not the source ZIP. Windows 10/11 x64 is required. The server and tray include their .NET runtime; Node.js and a separate .NET install are unnecessary.

1. In the existing app, **Export backup** to a JSON file. Keep both that backup and the old installation. Stop the old server (Ctrl+C in its terminal, or disable its startup task/service) so port 4173 is free.
2. Extract the whole new ZIP into `C:\HomeLibraryNative`. This release replaces the server architecture: copying only `dist` will not install it.
3. Open **PowerShell as Administrator** in that folder. Unblock the downloaded files, then install:

   ```powershell
   Get-ChildItem -Recurse -File | Unblock-File
   .\Install-Service.ps1
   ```

   If PowerShell's script policy prevents execution, use a temporary process policy for these reviewed scripts: `Set-ExecutionPolicy -Scope Process RemoteSigned`. This ends when the window closes.
4. Import your old JSON backup into the empty installation:

   ```powershell
   Stop-Service HomeLibraryNative
   .\Import-Library.ps1 -BackupPath 'C:\Users\YOURNAME\Downloads\home-library-backup.json'
   Start-Service HomeLibraryNative
   ```

   The old Node app's `library.sqlite` can also be imported using the same script. Stop its server first. Import reads the old file without changing it. If this new library already contains records, use **Restore backup** in the new admin UI instead; it makes an encrypted rollback backup before replacement.
5. Open `http://localhost:4173` **on the Windows server**. Verify books, borrowers, current loans and loan history. An empty new install intentionally has no samples; import your current library rather than generating new sample records.
6. Optional: run `Enable-Tray-Autostart.ps1` as your normal Windows user. The tray launches at sign-in; the server service starts at boot even with nobody signed in. Exiting the tray does not stop the server. Disable sleep on the Plex computer if it must remain reachable.

The existing web admin keeps scanning/manual entry, saved camera selection, duplicate confirmation, copy IDs, borrowers, checkout/return, removal, search, title/author/series/catalog sorting, 10/25/50 pagination, themes, exports, and catalog sharing. Loan history and CSV book export are also available. Only the local Windows browser can open this admin interface; the native Android app provides phone access.

## Phone access from work without a purchased domain

A free dynamic DNS hostname is sufficient for the native app. DuckDNS is one supported option: https://www.duckdns.org/. Create your own account and subdomain. Never send your DDNS token in a chat or put it in a QR code.

In an Administrator PowerShell:

```powershell
.\Configure-DuckDNS.ps1 -Subdomain 'YOURNAME'
.\Configure-Remote.ps1 -Hostname 'YOURNAME.duckdns.org'
Restart-Service HomeLibraryNative
```

The first script privately asks for your token, protects it with Windows DPAPI, and creates a five-minute IP-update task. Alternatively, use your router's compatible DDNS updater and skip that script. DDNS only updates an address; library contents never go through DuckDNS.

Reserve the computer's LAN IP in the router. Forward **TCP 7443** from the router to that computer's TCP 7443. The installer creates the Windows firewall rule. Keep **4173, 4174 and UDP 7445** off the router's forwarding list. UDP 7445 is local Wi-Fi discovery only.

This requires an ISP-provided reachable public IP. Compare the router's WAN address with your public IPv4 address; a private/CGNAT WAN address or upstream router can prevent forwarding. Ask your ISP for a public address or arrange forwarding through both routers if necessary. This package does not silently add a cloud relay.

## Pair Android

After building, install **Carribrary-Android-2.0.39-preview.apk** on your phone. Transfer it using USB or your normal private file-transfer method; open it in Android Files and allow that install source if Android prompts. The APK is independently installable and does not require ChatGPT. Android source is available in the separate source ZIP. Keep the separate **Private Signing Backup ZIP** private: future updates must use its signing key to update this installation. The Android SDK license was accepted for the supplied build with your authorization.

1. On the Windows admin page choose **Paired phones**, enter `https://YOURNAME.duckdns.org:7443`, and generate a private pairing QR.
2. In the Android app choose **Scan pairing QR** and scan it from the Windows screen. A code expires after five minutes and is usable once. Manual pairing text is a fallback.
3. If the router doesn't support reaching its public address from home Wi-Fi, pair using cellular data. Once paired, the app can find the matching server over home Wi-Fi and still verifies its saved identity. Discovery may be blocked by guest Wi-Fi; the configured remote address remains the fallback.
4. Test catalog refresh and a checkout/return over cellular before removing the old remote-access setup.

Pairing binds the phone to the server's public key and gives it a device certificate. Phone requests use direct HTTPS with that pin and mutual TLS. The private key stays in Android Keystore. CameraX and bundled ML Kit handle scanning without a barcode cloud service; manual ISBN/copy entry is available. The app can view its encrypted cached library offline; changes require a live connection. An interrupted change remains available for explicit retry with the same request ID so it cannot silently create a second copy or loan.

**The private pairing QR grants access. Do not share it with friends.** Revoke lost phones from the Windows admin. Device certificates last two years; re-pair before expiry. The server certificate lasts five years; there is no automated renewal workflow in this preview. Changing the server's identity requires re-pairing. Browser certificate warnings on port 7443 are expected: that port is for the pinned native app, not remote Chrome administration. No browser security override is required.

## Optional public read-only book QR

Public book sharing is separate from private phone pairing. It is off by default. Friends need a browser-trusted HTTPS address, so an optional Caddy reverse proxy serves **only** the dedicated read-only catalog listener.

You can use the same DuckDNS hostname: private Android connections use port 7443; the public browser list uses 443. Ensure nothing else on the Plex computer already uses 80 or 443. In Administrator PowerShell:

```powershell
.\Setup-PublicCatalog.ps1 -Hostname 'YOURNAME.duckdns.org'
```

This downloads Caddy from its official site, creates a startup task and firewall rule, and sets the public origin. Forward router **TCP 80 and 443** to this computer too. Caddy obtains and renews a publicly trusted certificate. DNS must point here and certificate-validation ports must be reachable. Open the public hostname from cellular and then use **Share catalog QR** in the Windows admin to enable a link. The link contains a random secret; anyone who receives it can see the list. Replace or disable it in the admin to revoke it. The list contains catalog number, ISBN, title, author, series and publisher, with no borrower names, contact details, loan history or edit endpoints.

The public proxy cannot access the admin or native API through its configured listener. Native Android traffic goes directly to Windows on 7443; Caddy does not decrypt it. To stop public access, disable the share link, stop the `Home Library Public Catalog` scheduled task and remove the 80/443 port forwards.

## Backups, updates and rollback

Data lives in `%ProgramData%\HomeLibraryNative\data`, independently of the application folder. Keep this folder, including its `keyring`, protected certificate files and device list. The live SQLite file is protected by Windows permissions; it is **not** encrypted at rest. Automatic snapshots are encrypted using Windows-protected keys: an initial snapshot, changed snapshots every 15 minutes, a daily pre-change snapshot, and a snapshot before restoring. Backups are in `data\backups`. They are not pruned automatically; monitor disk space.

For a portable backup or a move to a different computer, use **Export backup** in the admin. This JSON includes private borrower/loan data and is unencrypted, so store it privately. Encrypted `.hlbackup` files depend on the original Windows machine's DPAPI keys and the saved `keyring`; they are not portable by themselves. To restore one on the same machine, stop the service and run `server\HomeLibrary.Server.exe --restore-encrypted 'FULL_PATH.hlbackup'`, then start the service.

To update this native version: export a JSON backup, stop `HomeLibraryNative`, exit the tray, replace the new server/tray binaries, retain your customized `server\appsettings.json`, and start the service/tray again. Don't delete the ProgramData folder. To roll back to the old Node app, stop the native service and restart the preserved old app; its original data is unchanged. Changes made since migration will not appear there automatically—export/import a compatible JSON backup if needed. Uninstall-Service.ps1 removes only the native service/firewall rules, retaining data. Optional DDNS/public-proxy/tray startup entries must be removed separately.

## Build from source

Install .NET SDK 10.0.x. From this project's root:

```sh
dotnet run --project tests/HomeLibrary.Tests.csproj
dotnet publish server/HomeLibrary.Server.csproj -c Release -r win-x64 --self-contained true -o release/server
dotnet publish tray/HomeLibrary.Tray.csproj -c Release -r win-x64 --self-contained true -o release/tray
```

Android: install Android Studio, accept the Android SDK license, install platform/build tools 35, and open `android/`. Use JDK 17; the included Gradle wrapper downloads Gradle 8.11.1. From `android/` with `ANDROID_HOME` set, run `./gradlew assembleDebug` (Windows: `gradlew.bat assembleDebug`). The APK is `android/app/build/outputs/apk/debug/app-debug.apk`. Android 10+ is required. Keep a stable private signing key for releases; a different signing key prevents in-place updates. Do not commit signing keys or server/device secrets.

The test project verifies database persistence, ISBN validation, duplicate confirmation, copy identity, idempotent mutations, concurrency conflicts, loan constraints, retained history, encrypted snapshots, proof-based pairing, persistent certificates/revocation, and public catalog privacy/revocation. `tests/http_checks.py` additionally tests a running disposable fixture on admin 4180, phone 7443 and public 4181; it needs Python `cryptography`. The Android release build and lint complete with no errors, APK signing is verified, and the bundled ARM64 native libraries use 16 KB-compatible alignment. Windows service/tray behavior and actual phone camera operation require testing on their target devices.

## Architecture and extension points

- `server/`: minimal ASP.NET Core API, SQLite state/receipt transaction, protected identity/backup files, discovery, three separate listeners.
- `server/wwwroot/`: local web admin and public catalog, no frontend build step.
- `tray/`: service status/admin shortcuts; the tray never owns the database.
- `android/`: Kotlin/Compose, CameraX, ML Kit, pinned HTTPS client, Keystore and encrypted cache.
- `tests/`: domain and live transport/privacy integration checks.

The JSON state schema intentionally matches the earlier MVP for straightforward migration. Optimistic revisions reject concurrent stale edits; request receipts make explicit mobile retries idempotent. A normalized relational schema, automatic certificate renewal, stronger offline editing and richer labels can be added without introducing two independent master databases.

This is a preview migration package. ASP.NET and tray cross-compilation, server integration tests, and Android release build/lint/signature are verified here; Windows service startup, router configuration and Android hardware behavior must be verified on your equipment before replacing the working installation.

## Build the click-through installer

Publish the Windows server and tray to `release/server` and `release/tray` as shown above, install NSIS 3, and compile `installer/HomeLibrary.nsi`. Pass absolute `PAYLOAD`, `PROJECT` and `OUTPUT` definitions if building from another directory. For example: `makensis -DPAYLOAD=FULL_PATH_TO_release -DPROJECT=FULL_PATH_TO_home-library-native -DOUTPUT=FULL_PATH_TO_Setup.exe installer/HomeLibrary.nsi`. The compiler packages the self-contained binaries with service setup, backup selection, shortcuts and uninstall helpers. NSIS documentation: https://nsis.sourceforge.io/Docs/. The setup uses process-scoped PowerShell invocation and does not change the machine-wide execution policy.

Installer 2.0.1 repairs app-data ACLs and copied read-only attributes without replacing library or key contents. Its settings mirror is written as UTF-8 text, and existing-service paths are updated through the Windows service API to preserve quoting. Startup/setup failures open a read-only diagnostic report. When retrying a failed installation after an earlier import, leave the backup selector blank; your native data is retained.

Installer 2.0.2 also provisions the existing HTTPS certificate in the Windows machine certificate store during elevated setup and grants LocalService read access only to its private key. The service opens that persisted certificate rather than importing a PFX at startup. The device-signing CA stays in memory; existing protected certificates and phone pairing identities are preserved.

Android 2.0.1 fixes hardware RSA signing used by Conscrypt mutual TLS. Its new phone key allows PAD_NONE as well as PKCS1/PSS and DIGEST_NONE, because Conscrypt performs PSS encoding before the private-key upcall. The key remains non-exportable in Android Keystore; TLS 1.2/1.3, server pinning, and client certificate verification remain enabled. Existing phone key permissions are immutable, so install the update in place, then use Connection > Forget connection and enroll with a new Windows pairing QR. The old key and credentials are never silently replaced during an ordinary refresh. New enrollment verifies raw and PKCS1 hardware signing before sending the pairing request. Startup now preserves useful connection errors instead of hiding them behind a generic offline message.

Windows 2.0.3 / Android 2.0.2: Windows footer > Library name renames the collection and includes the name in SQLite, backups, phone state and the read-only catalog. Old backups default to Carribrary. Paired phones can manage public-sharing links through authenticated /mobile/sharing; only the Windows admin can rename the library. Android Colors offers the five web palettes; sort, direction and page size persist per phone. Covers use the stored URL or an ISBN-based Open Library fallback, with bounded HTTPS downloads and a placeholder if unavailable. Share library renders QR codes locally with ZXing and can send the public link or PNG through Android's share sheet. Public HTTPS proxy setup and ports 80/443 are still required for friends without a paired app. Install the Windows update first, then install the signed Android update over the current app; existing pairing is preserved. The launcher/product remains Carribrary; the editable name identifies the collection.

Windows 2.0.4 / Android 2.0.3: borrowing receipts open after a successful checkout, and Borrowing receipt on active loans reopens them later. A private receipt contains the library name, borrower name, book title/author, copy ID, checkout date and optional due date. A single email address selects the email composer; a formatted phone number selects the SMS composer. Missing or mixed contacts offer Copy receipt. The user reviews and sends through their own message app; the library does not send messages itself or record opening the composer as delivery. Windows email uses the default mail application; Windows texting requires a compatible linked-phone handler, with copying and Android texting as fallbacks. The receipt endpoint requires the local admin interface or an approved phone and is not part of the public catalog. Sending is independent of saving the checkout: a failed receipt load never rolls back or repeats the saved loan. No scheduled reminder service is included. Update Windows first, then install the APK over the existing app; pairing is preserved.

Windows 2.0.5 added lazy-loaded cover thumbnails to the public book list. The public projection includes coverUrl alongside the existing book fields, with no contacts or loans. Stored covers are tried over HTTPS, then an ISBN-based Open Library cover, then a placeholder. Images send no referrer so the private shared-list token is not disclosed to image hosts. Existing sharing links and phone pairing remain valid. This is a Windows-only update; Android APK 2.0.3 accompanied that release.

Book requests (Windows 2.0.6 / Android 2.0.4)

Friends open the existing shared QR/link in a browser and choose Request to borrow. They enter a name, phone number or email, and optional message. Contact details are private. The Windows and paired Android Requests inbox can approve or decline. Approval does not reserve a copy or send a notification. Use Hand over when the book is actually collected; choose an optional due date and existing borrower, or let the app find/create the borrower from the request. Checkout then offers the usual receipt composer; tap Send yourself. Requests are included in encrypted backups.

Install the new Windows installer over the existing installation, then install the Android APK as an update. Keep the original signing key, data and certificates; no new pairing or shared QR is needed. Existing public HTTPS/Caddy setup is required for the shared request page. Public submission limits: 10 attempts per IP per 15 minutes, 100 open requests, 8 KB request body. Retries with the same request ID are safe.

Windows 2.0.7 / Android 2.0.5: matching Carribrary artwork for the desktop and Start menu shortcuts, Windows tray/executable/setup, Android launcher and web browser tab. Install over the current versions; data, pairing and sharing links are preserved. If Windows retains a cached shortcut icon, restart Explorer or sign out and back in. The separate Carribrary.ico file can also be selected in Shortcut Properties > Change Icon.

Windows 2.0.23 / Android 2.0.6: trusted paired phones have the same library administration permissions as the Windows admin. Borrowers > Delete borrower and catalog > Edit book / Delete book require confirmation for removal. Outstanding loans block removal; historical records are archived. History includes returned loans and archived records. Manage library offers library renaming, JSON backup export/restore through Android’s file picker, CSV export, book-list text sharing, and private pairing/revocation of phones. Restore requires confirmation, validates on the server, and saves an encrypted rollback backup on Windows. A phone can revoke itself after confirmation and will need pairing again. Initial Windows installation, router/HTTPS configuration and service maintenance still happen on Windows. Install Windows first, then update the APK; pairing and data are preserved.

## Linux desktop and Raspberry Pi (2.0.9 preview)

HomeLibrary-Linux-x86_64-2.0.9-preview.flatpak provides the desktop launcher and bundled server. HomeLibrary-Pi-ARM64-2.0.9-preview.deb installs a boot-starting systemd service on 64-bit Raspberry Pi OS Bookworm or newer. See linux/README.md for installation, migration, pairing, router/DDNS/HTTPS setup and backups. Android 2.0.7 uses platform-neutral server wording and remains compatible with Windows 2.0.23. Moving the authoritative host requires JSON import, new phone pairing and a new public share link; existing Windows DPAPI identities cannot simply be copied to Linux.

## Google Play preparation (Android 2.0.9-test)

The Android test candidate targets API 36, retains the 2.0.23 cover-state fix, and includes in-app privacy/notices and server setup expectations. See play-store/START-HERE.md for the signed AAB, store drafts, alignment validation, test plan and unfinished production requirements. Publisher contact details, public downloads/privacy hosting, screenshots and reusable reviewer access must be completed before production submission. This is not a published or Google-approved release.

## iPhone companion source preview

A native SwiftUI iOS companion source preview is available in [ios/README.md](ios/README.md). Open the checked-in Xcode project on a Mac; no project generator is needed. Linux checks cover source grammar, project structure and server protocol compatibility. An actual iPhone build and Apple-specific camera/Keychain/TLS tests remain unverified until Mac access is available. This addition does not change existing server or Android releases.

## Cover search (2.0.12 preview)

In Add book or Edit details, use **Find a cover**, review the preview and choose **Use this cover**, then save the book. Metadata lookup opens the fallback search if no cover URL is available. The server tries the exact ISBN, then Open Library edition/cover IDs, then matching Internet Archive item thumbnails. Work-level/different-edition covers are labeled suggestions, and scanned items can have a title-page thumbnail rather than a retail jacket. Missing providers never prevent manual entry or saving a book. Existing books/covers are not rewritten automatically. Selected URLs appear on the shared catalog after saving.

Only authenticated administrators/paired phones can query cover search; the public catalog cannot invoke it. Outbound provider hosts and redirects are restricted, JSON responses and lookup duration are bounded, and no borrower data is sent. No new account, API key or paid service is needed. The 2.0.12 release did not include Google Books. Version 2.0.15 adds it as an optional keyed source with attribution across all displays (see below).

## Catalog number reuse (server 2.0.13)

New books use the lowest available BK number. Permanently deleting a book with no loan/request history frees its number; archived books and every number referenced by a loan or borrowing request stay reserved. Existing books are not renumbered. The legacy nextCopy field is kept for backup compatibility, but its high-water value no longer blocks reuse of a free number. Remove a permanently deleted copy’s printed label before reusing that number. Public request submissions include the ISBN/title the visitor viewed, and stale pages must refresh if the entry changed. Android 2.0.12 and the iPhone companion receive allocation results from the server and need no client update.

## Checkout and return scanning (2.0.14)

Scan the existing ISBN/EAN barcode, an optional BK copy barcode/QR, or manually enter an ISBN/copy ID or search a title/author. Only available copies appear for checkout; only active loans appear for return. One eligible copy is selected automatically; multiple matches require choosing a physical copy. Return choices show the borrower and loan dates. Nothing changes until Confirm checkout/Confirm return is tapped. ISBN-10 and its equivalent ISBN-13 match the same edition; different editions remain separate. Archived books and returned loans are excluded. Individual copy labels are optional. The server still validates the exact selected copy, revision and loan state before saving; a conflict refreshes the library so the user can review again.

Install the 2.0.14 server package and Android APK over your existing versions; no re-pairing is required. iPhone source 0.1.2 includes the matching flow and XCTest cases but requires Xcode/Mac verification.

## Collections and Google Books covers (2.0.15)

Named collections keep Kids, Family, School and other books separate within one household library. There is no fixed collection-count limit. Existing books load into General; rename it or add other collections using Manage collections, then change a book's collection in Edit details. Catalog, loan and shared-list selectors filter the books. Checkout/return scans and manual searches cover the whole household library even when the catalog is filtered, and choices show collection names to distinguish copies. Borrowers, requests, loans, sharing tokens and copy-ID allocation remain shared. Collections are organizational filters, not permissions or independent libraries. Empty collections can be deleted; General stays available and historical archived books retain their collection. JSON/encrypted backups preserve collections and CSV exports include their names.

Google Books is an optional third cover source, used only when Open Library and Internet Archive yield no cover. The server searches the exact ISBN and offers an ISBN-matched thumbnail for review; it does not mix Google and other providers' search results. Every Google-sourced cover has the supplied Powered by Google graphic and a link to that book in Google Books, including the public shared list. Attribution is derived from the Google image URL, so it persists in existing book/backup formats.

### Enable Google Books without commands

1. Open the server admin interface and choose Library settings → Google Books covers.
2. Follow Enable Books API, create/select a Google Cloud project, and enable Books API.
3. Follow Create an API key, create a key, and restrict its API access to Books API. Configure application restrictions suitable for your server if available.
4. Paste the key into the server settings screen and save. Do not send it in chat or distribute it with installers.
5. Edit a book without a cover → Search Books for a cover → review and select a suggestion → Save.

The key stays protected in the server data directory, never in the catalog or library export. No Google sign-in is required on phones. Leave the key field blank and save to disable lookups; existing cover credits remain. Advanced deployments can instead set GoogleBooks__ApiKey in the server environment. Each household can use its own optional key, avoiding a shared publisher credential inside distributed APKs. Quotas and provider availability still apply. A key-free installation continues to use Open Library/Internet Archive.

Official Google documentation: https://developers.google.com/books/docs/v1/using and https://developers.google.com/books/branding .

## Google lookup diagnostics (server 2.0.16)

Saving a key through Library settings takes effect on the next request without restarting the server. Open Google Books covers → Test saved key to verify API access. It reports missing/invalid keys, API enablement, application/API restrictions, quota exhaustion and temporary network/provider failures without exposing credentials or raw provider responses. A successful connection verifies access, not cover availability for every ISBN.

Google fallback now checks up to five search results in the provider's order, skipping unrelated ISBNs and records without usable images. ISBN-10/13 equivalence remains supported. Cover lookup messages distinguish rejected requests from missing images. Android/iPhone display these server-provided messages without needing a new client build; retain Android 2.0.15 and iPhone source 0.1.3.

## Google request compatibility (server 2.0.17)

The server now sends the saved key using the Books API's documented key= query parameter, rather than the general Google x-goog-api-key header. It attaches the key only to HTTPS Books API requests on www.googleapis.com. Cover/image URLs and redirects to other providers never receive the key. The named HTTP client's request logging is removed and disabled to keep credential-bearing URLs out of server logs. HTTPS upgrading and missing-image/HTTP-error handling remain in place. Keep Android 2.0.15; no client update is needed.

This changes the request format; it does not prove why a specific valid-looking key was rejected. Test saved key checks Google's actual response from your server. Public anonymous requests can sometimes return book data, but Google documents application identification for Books API access, and anonymous shared quotas are unreliable.

## Google cover search fallback (server 2.0.18)
If ISBN-13 has no usable Google cover, lookups try equivalent ISBN-10 and then title/author. Individual volume details are checked when search fields are incomplete. Covers require a matching ISBN; title-only and other-edition records are rejected. Temporary 500/502/503/504 responses are retried once, within a 10-second Google budget. Existing Open Library/Internet Archive priority and manual review remain. No companion update is needed.

## Plain Google title/author search (server 2.0.19)
Google fallback now also searches plain title plus author when field-filter searches fail. Exact ISBN-linked covers take priority. Same-title/same-author records with a different valid ISBN can appear as explicitly marked other-edition/ebook suggestions; choose only after reviewing the image. Unidentified records and different authors are rejected. Nothing automatically changes an existing cover. Transient 500/502/503/504 responses retry with one- and two-second backoff, within the existing ten-second total Google budget. No Android update is required: its cover dialog already displays the edition warning and requires Use this cover then Save.

## Reading tracker and personal borrower pages (2.0.20)
Open Reading tracker in the admin interface or Android Library options. Add a household reader, or link a profile to an existing borrower. Each person has independent Want to read, Currently reading and Finished lists, optional start/finish dates, 1–5 star ratings and private notes (2,000 characters). Select the reader, add a catalog book, and save its reading entry. Reading status and lending are independent; returning a book never marks it finished. Removing a catalog book archives it when needed to preserve reading history. Empty profiles can be deleted; request history requires keeping the profile. Backups include reader profiles and entries.

Choose Personal link / QR for a reader to create their own browser page. Public HTTPS sharing must already be configured, but the generic catalog-sharing link need not be enabled. The personal page needs no app installation and works on Android or iPhone. It can edit only that reader’s reading list, browse catalog books and submit requests to the admin inbox; approval and checkout remain admin tasks. Each personal link is a bearer credential: anyone holding it can use it. Send it privately; replace or revoke it to stop access. Tokens are in URL fragments and then Authorization headers, not request URLs. Credentials are protected in reading-access.protected, excluded from library exports. Restoring the library does not restore lost personal-link credentials; issue new links when moving to a new host. All paired admins can see all reading profiles. Public /s/ catalog data never includes reader notes.

Windows public sharing already proxies the new /reading/ paths. Existing Linux/Pi Caddy configs that only allow /s/* must also allow /reading/*; see linux/Caddyfile.example, validate and reload Caddy. No additional port forwarding is required.

## Help, Android navigation and borrower sign-in (2.0.21)
The Android bottom bar has Catalog, Loans, Reading and More. More groups People & lending, Sharing & collections, and Settings & backups. Sort/collection/page controls are under Filter & sort. Help in the top banner is searchable and works offline; the web admin also has Help.

The existing shared catalog QR now links to Create account / Sign in. Visitors choose a username and a 12–128 character password/passphrase; accounts begin pending. On the phone use More → People & lending → Borrower accounts & approvals, or Borrower accounts in the web admin. Explicitly select that person's existing borrower/profile, or create a new one. Registration never matches private records by name/contact alone. Approved accounts can view their own active/returned loans, reading entries and requests. Borrowing requests still require admin approval and checkout. A borrower uses the browser on Android or iPhone; the native companion remains the admin app.

Passwords use ASP.NET Core Identity salted PBKDF2 hashing (210,000 iterations). Session/recovery tokens are random and stored hashed. Sessions use Secure, HttpOnly, SameSite=Strict cookies for up to 24 hours. Login has rate limiting and a 15-minute account lock after five failed attempts. Librarians can generate 30-minute single-use reset links; resets end all sessions. Blocking ends sessions and revokes the linked profile's personal QR. A profile's borrower link changing causes existing account access to fail closed until the librarian relinks it. No automated email verification/recovery service is required.

Credentials live in borrower-accounts.sqlite and its SQLite sidecar files, separately from library.json/encrypted library exports. Those exports preserve reading/loan records but not sign-in accounts or personal-link secrets. To preserve credentials when moving hosts, stop the server and protect/copy its complete data directory (and the platform protection keys), or have users re-register and explicitly relink their existing profiles. Never distribute that private directory with application packages.

Existing Linux/Pi Caddy path allowlists must add /account/* alongside /s/* and /reading/*, then validate/reload Caddy. Windows public-sharing proxy already forwards these paths. Public HTTPS remains required; no additional router ports are needed.

Server Help → Open full server help page provides searchable setup and recovery instructions at /help.html on the protected admin interface, including print/save-as-PDF through the browser. The same guide topics are bundled offline in the Android app.


## Household tools (2.0.23)

### Recovery and reused copy numbers

Delete moves a book or borrower to the Recycle Bin. Open Recycle Bin from the server footer or Android More → Settings & backups. Restore returns the record. If an unreferenced book number has been reused, restoration assigns a free number and does not overwrite another copy. History-linked numbers stay reserved. Delete permanently removes the recovery option; records needed for loan/reading history stay archived. Recovery is included in library backups. Records permanently deleted before this update cannot be reconstructed by this feature.

### Bulk editing physical books

On the server choose Select books, select individual copies or the current page, then Edit selected. Android has Bulk edit in Catalog. Choose up to 500 copies. Check the fields to change: collection, author, publisher or series. An empty checked text field clears it. Review the selection and confirm. Titles, ISBNs, copy numbers, covers, loans and reading history do not change. The entire edit fails without partial changes if a selected copy was removed or another client saved first.

### Automatic due-date email reminders

Optional. Configure Email reminders from the server footer or Android More → Settings & backups. Use your provider’s SMTP STARTTLS host, usually port 587 (implicit TLS/465 is not supported), SMTP username, sending email and an app password. OAuth-only providers are not supported in this version. Save settings, send an explicit test to the sending mailbox, then enable. Edit each borrower and record their permission for automatic email. Valid email contacts only; phone numbers keep tap-to-send. One reminder is sent in the configured window before due, one on the due date, and one once the overdue threshold is reached. Returned loans are excluded. The server checks every 15 minutes after the configured start hour in its own time zone, and must be running. No due date means no automatic reminder. Sent means accepted by the provider, not guaranteed inbox delivery. Check-delivery may mean failure or interrupted delivery: check the mailbox before sending manually; uncertain attempts are not automatically retried. SMTP credentials and the delivery ledger stay separate from library exports, encrypted settings use the server keyring. Full private backups preserve them. Turning reminders off or clearing a borrower’s permission stops future sends. Changing a due date creates a new reminder schedule.

### Unblock and relink borrower accounts

Open Borrower accounts on the server or Android More → People & lending → Borrower accounts & approvals. Unblock returns an account to pending; choose the verified borrower/profile and approve next. Alternatively Unblock & approve combines those steps. If a reading profile’s borrower was deleted, choose Restore borrower to recover the old identity and history, or New borrower for reader to keep the reading profile with a fresh borrower record. A replacement borrower does not inherit the old borrower’s loans. Existing active borrowers can be selected explicitly. Unblocking clears sign-in lockout, but revoked sessions, reset links and private reading QR links stay revoked; the person signs in again. Restoring a borrower leaves reminder permission off until recorded again.

### Setup checklist and safer updates

Open Setup & updates from the server footer, Android More → Settings & backups, or the Windows Start/tray menu. It shows server version, private data folder, backup status, paired phones, addresses, port instructions and a DNS check. On Windows use Connection setup to save the hostname and DuckDNS settings; the wizard can open the router page and check DNS/local service. Reserve a stable LAN address. Forward TCP phone port (normally 7443), plus 80/443 only if public browser sharing is configured. Never forward admin 4173, internal catalog 4174 or discovery 7445. Test with Wi-Fi OFF; DNS/local checks cannot prove outside access or bypass ISP restrictions. Export a backup, install new Windows/Android versions over the old versions and keep data. Windows installer now stops the server and makes a complete private ZIP under the data folder’s parent/upgrade-backups before updating. The ZIP contains credentials and key material, is restricted to service/admin accounts, and must not be shared. These safety ZIPs are not automatically pruned. Do not use them as portable unencrypted public exports; Windows key protection can be tied to that computer. Linux Flatpak: quit the app and open the new bundle in your software manager to update. Pi: install the new ARM64 DEB; service data remains under /var/lib/home-library. No official automatic update feed exists yet.


## Account deletion and help maintenance (2.0.23)

Server: Borrower accounts → Delete account / personal information. Android: More → People & lending → Borrower accounts & approvals → Delete account / personal information. The same management flow is included in the iPhone source. Choose Sign-in account only to remove username/login credentials, sessions and reset links while keeping operational borrower, loans and requests. Both deletion scopes remove the account’s encrypted private lists and legacy personal reading. Export an encrypted personal backup first. Private borrower links cannot be recreated by the librarian. Choose Account and linked personal information to permanently remove linked borrower/reader records, reading notes/ratings/dates, their requests, returned-loan history, and matching borrower Recycle Bin entries. Physical books stay in the catalog. Full erasure is blocked until active loans are returned. Account-only deletion is allowed with active loans. For pending/unlinked accounts or old removed links, select an additional verified reader/borrower explicitly; no history is claimed by name or contact. The preview lists exactly which people and record counts are included. Records tied to another sign-in account cannot be erased this way. Other guest requests with matching contact details are kept unless explicitly selected and re-previewed; shared contact details do not prove ownership. Type the displayed username exactly and choose Delete permanently. A changed account/library requires a fresh preview. This is permanent and bypasses the Recycle Bin. Sign-in usernames become available for new registration, which starts pending without inherited access. The normal Delete borrower action still uses the Recycle Bin and does not delete a sign-in account. Block/Unblock also keep records; they are not deletion. Optional backup cleanup removes ALL stored automatic library backups and matching Windows pre-update safety ZIPs in the installation backup folders, for the entire library, not just this person. Leave it off to keep backups, which may retain older information and can restore it later. No pre-deletion backup is made for full erasure. New scheduled backups use the current library. Library-owner exports, other backup systems, sent emails and offline device caches cannot be recalled; refresh paired devices and manage those copies separately. This is deletion of live application records, not a guarantee of forensic erasure of disks. Borrowers can ask the library owner for deletion using Help with my account on the shared sign-in page.

Every user-visible change must update the canonical help and ship synchronized server/phone instructions. See DEVELOPMENT.md and scripts/sync-help.py.


## Borrower Home screen web app (2.0.23)

Borrowers can save the shared library as a Home screen app without installing the household admin APK. On an Android phone, open the usual PUBLIC library QR/link in Chrome. Tap Install / add to Home screen on the book list or borrower sign-in page. If Chrome offers the install prompt, confirm Install. Otherwise tap Chrome’s ⋮ menu → Add to Home screen → Install (or Create shortcut), then confirm. If the link opened inside a messaging app, choose Open in Chrome first. Open the library icon from the Home screen afterward. It opens in a separate standalone window when supported. Guests can still browse and request books without an account; signed-in, approved borrowers get their own loans, reading and requests. Installation never grants admin access or bypasses sign-in/approval. No APK, signing folder or private phone-pairing QR is needed. Use the public HTTPS sharing link, not the private paired-phone server address. The app remembers only the last shared catalog path on that browser, so launching can reopen the catalog or the signed-in account. Private reading links, reset tokens, contacts, passwords and API responses are not saved by this web app for offline use. Normal secure sign-in cookies still apply and sign-in can expire. The server and connection are required; if unreachable, a simple offline explanation is shown instead of cached private data. On a shared phone, sign out. If the owner replaces/disables a sharing link, ask for the latest QR; opening the new valid link updates the remembered catalog. Already-approved accounts may still sign in independently. Web app changes load from the updated server on reopen/refresh; Chrome controls installation availability and icon/name update timing. On iPhone, open the shared link in Safari and use Share → Add to Home Screen → Add. This borrower web app is separate from the native iPhone admin source project.

Manifest: /account/manifest.webmanifest; stable launch: /account/launch; network-only worker: /account/borrower-sw.js. Existing /account/* proxy rules already cover these. No new router ports. Chrome documentation: https://support.google.com/chrome/answer/9658361?co=GENIE.Platform%3DAndroid&hl=en

## iPhone companion, descriptions, branding and optional push (2.0.23)

The iPhone admin companion is hosted by the server at **/companion/**. Open it in Safari, Share → Add to Home Screen, open the icon, then pair using a private one-use code from the server’s **iPhone companion** button or Android Manage library. Codes expire in 10 minutes; browser admin authorization lasts 30 days. Manage/revoke these browsers separately from native paired phones. The shared-library QR grants no admin privileges. Private catalog responses are not cached for offline use. Android stays native. Linux/Pi restrictive Caddy allowlists must add /companion/*; no additional ports. Windows public HTTPS proxy already covers this path.

ZXing Browser 0.2.1 is bundled for Safari barcode scanning with manual fallback; no third-party script CDN. About this book opens a short description, using manual text first, then Open Library and optional Google Books. The popup credits the source; metadata lookup does not save over a book. Library settings can upload a custom image, automatically fit to square PNGs, or restore the default. Server colors now persist across borrower catalog, signup, sign-in, account and Home-screen launch. Android can publish its local colors through Manage library. Custom images and manual descriptions are public library branding/catalog data and included in backups. Native Android’s launcher icon remains Carribrary.

Borrowers can opt into due-date Web Push from an approved account. iPhone admin PWAs can opt into new-request Web Push. iPhone requires iOS 16.4+ and a Home-screen installation. Permission is requested only after a tap. Server creates VAPID keys automatically; no messaging-provider account is needed. Push messages are empty and workers show generic text. Endpoints/keys are protected on the server, no private borrower/book contents are sent through push. Schedule: up to two days before due, due date, and once at least seven days overdue; new admin requests are checked every minute. Returned loans, blocked/deleted accounts and revoked browsers are excluded. Delivery depends on the browser/OS/service, is deduplicated, and is not guaranteed. Email and tap-to-send texts remain independent. Native Android request alerts use the separate periodic scheduler described below.

Android native request alerts: Manage library → Enable background request alerts, then grant notification permission. Android polls the paired server about every 15 minutes, subject to battery/OS scheduling; these are not instant push. Existing pending requests are the initial baseline. Generic alerts open Requests; disable or Forget connection cancels them. No PWA or Firebase account is needed; only request-ID hashes are kept for deduplication.

## 2.0.24 preview — catalog-first borrower experience and TBR

Borrowers now land on Catalog after sign-in. My reading is second and defaults to To Be Read: Want to read and Currently reading. Mark as read removes the book from TBR while retaining its history, notes and rating. Admins and household readers use Reading and choose their own profile. No backup migration or re-pairing is needed. Install the matching server and Android update for both interfaces. The iPhone PWA updates from the server; native Swift source still requires Xcode verification.

This is the first Carribrary roadmap increment. Books to Buy is included in 2.0.27; exclusive holds and batch lending are included; see distribution/CARRIBRARY-IMPLEMENTATION-PLAN.md. The product rename ships in 2.0.26; stable technical package identities are retained for updates.

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


2.0.31 recovery and illustrated onboarding
Server Help → Illustrated quick start has current fictional-library screenshots,
short success checks and Print / save as PDF. Normal offline phone Help remains
available as text. Library JSON exports are not complete account/key/vault backups.

Windows: Start → Carribrary → Update and recovery (administrator prompt) works
independently of the main server. Create recovery point briefly stops the library
and saves verified server/tray programs and the complete library-data directory.
Updates do this automatically, then verify new files, startup and catalog access.
Compatible failed updates attempt program recovery. Select a prior point and
Restore selected application to keep CURRENT data while restoring compatible
program files. Only 2.0.31 program compatibility is supported here. Historical data
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

2.0.31 FORMAT UPGRADE: Back up the complete stopped server before updating. The library migrates once to format 3 with a permanent internal identity for each physical copy. Visible BK numbers and gap filling stay the same. 2.0.30 and earlier cannot open the migrated library; do not downgrade. Ordinary Windows program recovery is limited to 2.0.31. Current-format JSON backups preserve identities; importing an older backup assigns identities absent from that backup. Keep private database/keyring backups for supervised recovery.


2.0.31 — COMPLETE SERVER DATA BACKUP
Open Backups > Create complete backup on the server. Android: More > Settings & backups > Complete server data backup. Choose and confirm a 12-128-character backup password, save the .carribrary-backup file and wait for completion. Keep the password separately: it cannot be reset. The server briefly pauses changes while capturing current databases consistently, then resumes automatically. No manual shutdown/folder-copying is needed to create this archive. Library export (JSON) remains a smaller books/lending export without sign-ins, private vaults or server keys.

Complete archives include current databases, accounts, encrypted borrower vaults, pairing identities, keyring and saved configuration. They exclude old backups/recovery points, programs, external HTTPS/Caddy certificates, OS service/task definitions and environment-only secrets. Current data limit: 2 GB / 10,000 files. Windows DPAPI keys/settings require the same Windows computer; this is not a guaranteed cross-machine migration. Borrowers still need their own vault passphrase/recovery code. Keep archives private; copies can retain data deleted later.

RECOVERY (OFFLINE, NEW FOLDER ONLY): Stop the server. Keep the existing data folder separately. Run HomeLibrary.Server --unpack-complete-backup ARCHIVE NEW-FOLDER using the compatible server executable (Windows: HomeLibrary.Server.exe). It asks for the password without displaying it, checks encryption, file hashes and database integrity, and unpacks only into a new folder. Follow RECOVERY-READ-ME.txt there. Do not merge individual databases into a live server. Restore library export accepts JSON only, not this archive. Programs and external proxy/service configuration are separate. Get recovery help if unsure.

Complete snapshot recovery warning: Restoring an earlier complete snapshot rewinds account deletions, passwords, device revocations and lending history. Review accounts and paired devices before making the restored server reachable. Compatible program-only recovery keeps current data instead.

2.0.31: Optional permanent-copy QR labels and copy-number allocation choice. Create a complete backup before upgrading. Library format 3 protects the new preference; versions 2.0.30 and earlier cannot open it. Use the matching Help for label printing, sheet alignment and numbering policy. Existing history-linked numbers remain reserved.

2.0.32 repair update: cancellation confirmation/feedback and request status on all borrower tabs are corrected. History safeguards and operational hold rules remain. Registration protection is optional; default local rate limits and librarian approval work without Cloudflare. See the matching Help for private deployment configuration. Library format remains 3, with Windows program recovery compatibility for 2.0.31–2.0.32. Back up first and update the server before companion apps.

Windows local setup: Start → Carribrary → Administrator access opens an elevated read-only code viewer. Copy its one-time setup code into the local login page, create credentials and save the recovery code. Finishing setup and opening the library offers this viewer when an unused code exists. It does not reset credentials or bypass Carribrary sign-in.


Earlier 2.0.37 borrower navigation source update: approved borrowers start in Catalog. My reading remains second; My loans includes borrowing requests and confirmation-based cancellation. Account groups private-list controls, install, optional notifications, help, data/history/deletion and sign-out. Pending accounts retain help, install and deletion access. The server delivers this update to borrower browsers/PWAs; no new borrower install is needed. That iteration kept native Android styling unchanged; 2.0.39 refreshes it. Current library format 3, administrator credential format 2, and pairings remain. Program recovery supports 2.0.39; earlier programs remain incompatible. Build packages locally; this source update does not install them.
