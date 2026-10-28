# Carribrary for Linux and Raspberry Pi — 2.0.47 preview

LOCAL ADMINISTRATOR SIGN-IN AND SEPARATE ACCOUNTS

The local dashboard now requires a Carribrary username and password even when
Windows is signed in. After installation, open Carribrary and follow the setup
page. Open the private local-admin-setup.txt file at the data-folder location
shown there using an authorized server administrator account. Paste its one-time
code, create your username/password, and save the recovery code privately.
Do not send either code to support. Existing books, borrower accounts and approved
phones stay in place. Paired Android/iPhone/browser companions keep their existing
automatic authorization without another password screen.

Local sessions expire after 30 minutes idle, eight hours total, or a server
restart. Sign out explicitly when finished. Recover local administrator access
uses your saved recovery code, not your Windows or borrower password. Lost-code
recovery is documented in Help and requires protected server-data access.
Revoked devices immediately lose server access; downloaded offline data is not
erased remotely. Automatic program recovery permits only 2.0.39 custom-theme
programs; earlier binaries cannot decrypt upgraded credentials. Back up before updating.
Physical Windows service/ACL/installer and Pi/Safari device checks remain necessary.


Same catalog, borrowers, loans, receipts, requests, covers, colors, sorting, sharing and backups as the Windows server. Works with the signed Carribrary Android companion. No ChatGPT hosting, Tailscale or separately installed .NET runtime is required. Windows and Linux/Pi are alternative hosts for one library; they do not synchronize independent databases.

## Linux desktop: Flatpak (x86_64)

1. Install Flatpak support through your distribution's software manager if needed.
2. Open **Carribrary-Linux-x86_64-2.0.39-preview.flatpak** with Discover/GNOME Software or your Flatpak installer. It may download the Freedesktop runtime from Flathub.
3. Launch **Carribrary** from your applications menu. It starts the server and opens **http://localhost:4173** in your normal browser.
4. The server keeps running when the browser closes so the phone can connect. Launching Carribrary again reopens the existing library. Use the launcher’s **Stop library server** desktop action to stop it. Desktop logout/reboot stops it; add Carribrary in your desktop's Autostart settings if desired.
5. Restore a JSON backup if migrating. Use **Paired phones** in the admin page to create a private QR for the new host.

Terminal alternatives, if your software manager cannot open a bundle:

```sh
flatpak install --user ./Carribrary-Linux-x86_64-2.0.39-preview.flatpak
flatpak run --user com.household.HomeLibrary
flatpak run --user com.household.HomeLibrary --stop
```

The Flatpak uses network access for phone connections and metadata/cover lookups, and the desktop portal to open your browser. It has no host filesystem permission. Data lives in **~/.var/app/com.household.HomeLibrary/data/home-library**. Updates retain it. Export a backup before changing/removing installations; do not choose “delete application data” during uninstall.

The browser admin’s backup restore/upload and backup downloads work through your normal browser. The server does not need direct access to your Documents folder.

## Raspberry Pi 4/5: 64-bit Raspberry Pi OS

Use 64-bit Raspberry Pi OS Bookworm or newer (Debian 12/13), **arm64**. This package does not support a 32-bit OS.

1. Copy **Carribrary-Pi-ARM64-2.0.39-preview.deb** to the Pi.
2. Open it with the Pi’s graphical package installer if available. Otherwise, open a terminal in the downloaded file’s folder and run this one installation command:

```sh
sudo apt install ./Carribrary-Pi-ARM64-2.0.39-preview.deb
```

3. The installer creates a dedicated service user, starts Carribrary and enables it at boot.
4. On a Pi with a desktop, open **Carribrary** from the applications menu or visit **http://localhost:4173**.
5. On a headless Pi, connect from another computer using an SSH tunnel:

```sh
ssh -L 4173:127.0.0.1:4173 YOUR_PI_USER@YOUR_PI_ADDRESS
```

Then open **http://localhost:4173** on that computer. Keep its port 4173 free (stop another local library server first). SSH must be enabled in Raspberry Pi settings. The admin stays bound to loopback; do not expose it on the router.

6. Restore your exported JSON backup and pair the phone. In **Paired phones**, enter **https://PI_LAN_IP:7443** for local-only access, or **https://YOUR_DUCKDNS_NAME:7443** for home and work access.
7. Install future DEB updates over the existing version. Data, identities and configuration stay intact.

Useful service controls:

```sh
sudo systemctl status home-library
sudo systemctl restart home-library
sudo systemctl stop home-library
sudo systemctl start home-library
journalctl -u home-library -n 50 --no-pager
```

Program: **/opt/home-library/server**. Private data: **/var/lib/home-library**, owned by the service user with mode 0700. Optional address settings: **/etc/home-library/home-library.env**. Removing the package keeps data and the service user. Do not remove the data folder when updating.

## Phone and remote connection setup

The existing Android 2.0.6 works with these servers. Android **2.0.7** updates the instructions and status text for any server platform.

Moving from Windows changes the server's cryptographic identity. Export/import a **JSON library backup**, then use Android **Connection → Forget connection** and scan a new private pairing QR from the new host. Do not copy Windows DPAPI-protected identities/key files to Linux. Backups carry books, borrowers, loans and requests; phone pairing and public sharing settings are separate.

For work access, reserve the new host's LAN address and change the router's **TCP 7443** forwarding to that host. Keep DuckDNS pointing at your home public IP using your existing updater or a Linux DuckDNS updater. Choose one active authoritative server. The service's optional **Library__RemoteAddress=https://YOUR_NAME.duckdns.org:7443** setting only pre-fills pairing; it does not configure DuckDNS or the router.

If you use a Linux firewall, allow TCP 7443 for paired phones and local-network UDP 7445 for discovery. Do not forward UDP 7445 or admin/catalog ports 4173/4174 on the router. Native phone TLS is pinned through pairing; browser certificate overrides are unnecessary.

## Public book-list QR and borrower requests

Public sharing requires browser-trusted HTTPS, separate from the private native phone endpoint. On Linux/Pi:

1. Install Caddy using its official instructions: https://caddyserver.com/docs/install .
2. Copy the supplied **Caddyfile.example** to Caddy's configuration, replacing the hostname with your DuckDNS hostname. Its routes proxy only /s/*, /reading/*, /account/* and /robots.txt to loopback port 4174.
3. Set **Library__PublicCatalogOrigin=https://YOUR_NAME.duckdns.org** in the Pi's environment file and restart Carribrary. With Flatpak, configure a persistent Flatpak environment override for this setting:

```sh
flatpak override --user --env=Library__PublicCatalogOrigin=https://YOUR_NAME.duckdns.org com.household.HomeLibrary
```

Stop/start the Flatpak after changing the setting.
4. Forward TCP **80 and 443** to the selected host and allow them through its firewall. Reload Caddy after configuration. Port 7443 remains the private Android endpoint.
5. Open the public hostname from cellular data, then create a new share QR/link from Carribrary.

When migrating, old Windows sharing tokens are not imported by JSON backup; create a new public link and share it again. Keep the existing Windows sharing setup if Windows remains your server.

## Backups and Linux storage

Automatic encrypted library snapshots are stored in the data folder's backups directory. The corresponding private keyring is protected by filesystem permissions on Linux (Windows instead uses machine DPAPI). Keep the full private data directory and keyring together for host disaster recovery, and keep these copies private. JSON exports are portable, contain private contacts/loan history, and do not require the old host's keys.

## Build from source

Requires .NET SDK 10, Python 3 with Pillow, dpkg-deb, Flatpak packaging tools and the AppStream Compose addon (commonly the `appstream-compose` package). Publish both self-contained targets first, then package:

```sh
dotnet publish ../server -c Release -r linux-x64 --self-contained true -o ../release/linux-x64
dotnet publish ../server -c Release -r linux-arm64 --self-contained true -o ../release/linux-arm64
python3 build-packages.py
```

The build script uses Flatpak's low-level build-export/build-bundle commands and does not install a service or app. The delivered Flatpak is x86_64; the Pi DEB is ARM64. ARM64 desktop Flatpak and 32-bit Pi OS are not included.

The self-contained Linux binary and the actual Flatpak sandbox passed HTTPS pairing, administration, deletion/history, restore, sharing and revocation checks. Desktop single-instance and shutdown controls were checked. AppStream metadata has a missing-public-homepage warning for this personal preview. The ARM64 package is cross-built; physical Pi startup and phone pairing need testing on your Pi.

Reading tracker and personal borrower links are included in 2.0.25. Existing Caddy configurations must add /reading/* to their catalog path matcher and be reloaded. Reading profiles and backups remain on your own host.

For 2.0.25 borrower accounts, add /account/* to an existing Caddy path allowlist and reload Caddy. New packages include the updated example. Sign-in credentials are stored separately from library exports; stop the server before a full data-directory backup.


## 2.0.25 household tools
The server footer now has Recycle Bin, Email reminders, and Setup & updates. Android exposes the same tools through More. Automatic email requires your own SMTP STARTTLS sending account and permission recorded on each borrower; it stays disabled by default. Bulk edit supports collection/author/publisher/series on selected physical copies. Blocked borrower accounts can be unblocked and linked to a restored or replacement borrower. Export a backup before installing updates; keep the existing data directory. Linux/Pi keep the existing manual package update workflow and router/Caddy setup; the checklist and DNS check explain it but do not configure a router or automatically apply updates.

## iPhone companion, descriptions, branding and optional push (2.0.25)

The iPhone admin companion is hosted by the server at **/companion/**. Open it in Safari, Share → Add to Home Screen, open the icon, then pair using a private one-use code from the server’s **iPhone companion** button or Android Manage library. Codes expire in 10 minutes; browser admin authorization lasts 30 days. Manage/revoke these browsers separately from native paired phones. The shared-library QR grants no admin privileges. Private catalog responses are not cached for offline use. Android stays native. Linux/Pi restrictive Caddy allowlists must add /companion/*; no additional ports. Windows public HTTPS proxy already covers this path.

ZXing Browser 0.2.1 is bundled for Safari barcode scanning with manual fallback; no third-party script CDN. About this book opens a short description, using manual text first, then Open Library and optional Google Books. The popup credits the source; metadata lookup does not save over a book. Library settings can upload a custom image, automatically fit to square PNGs, or restore the default. Server colors now persist across borrower catalog, signup, sign-in, account and Home-screen launch. Android can publish its local colors through Manage library. Custom images and manual descriptions are public library branding/catalog data and included in backups. Native Android’s launcher icon remains Carribrary.

Borrowers can opt into due-date Web Push from an approved account. iPhone admin PWAs can opt into new-request Web Push. iPhone requires iOS 16.4+ and a Home-screen installation. Permission is requested only after a tap. Server creates VAPID keys automatically; no messaging-provider account is needed. Push messages are empty and workers show generic text. Endpoints/keys are protected on the server, no private borrower/book contents are sent through push. Schedule: up to two days before due, due date, and once at least seven days overdue; new admin requests are checked every minute. Returned loans, blocked/deleted accounts and revoked browsers are excluded. Delivery depends on the browser/OS/service, is deduplicated, and is not guaranteed. Email and tap-to-send texts remain independent. Native Android request alerts use the separate periodic scheduler described below.

Android native request alerts: Manage library → Enable background request alerts, then grant notification permission. Android polls the paired server about every 15 minutes, subject to battery/OS scheduling; these are not instant push. Existing pending requests are the initial baseline. Generic alerts open Requests; disable or Forget connection cancels them. No PWA or Firebase account is needed; only request-ID hashes are kept for deduplication.

## 2.0.25 personal TBR
Borrower sign-in opens Catalog first. My reading defaults to To Be Read, which includes unread and currently reading books. Mark as read preserves history, notes and rating. Existing backups and pairings are retained. Books to Buy, exclusive holds and batch lending are planned separately.

## 2.0.33 preview: lending and data controls
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
program files. Only custom-theme 2.0.39 program compatibility is supported here. Historical data
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

2.0.31 FORMAT UPGRADE: Back up the complete stopped server before updating. The library migrates once to format 3 with a permanent internal identity for each physical copy. Visible BK numbers and gap filling stay the same. 2.0.30 and earlier cannot open the migrated library; do not downgrade. Ordinary Windows program recovery is limited to multi-administrator 2.0.36 programs. Current-format JSON backups preserve identities; importing an older backup assigns identities absent from that backup. Keep private database/keyring backups for supervised recovery.


2.0.31 — COMPLETE SERVER DATA BACKUP
Open Backups > Create complete backup on the server. Android: More > Settings & backups > Complete server data backup. Choose and confirm a 12-128-character backup password, save the .carribrary-backup file and wait for completion. Keep the password separately: it cannot be reset. The server briefly pauses changes while capturing current databases consistently, then resumes automatically. No manual shutdown/folder-copying is needed to create this archive. Library export (JSON) remains a smaller books/lending export without sign-ins, private vaults or server keys.

Complete archives include current databases, accounts, encrypted borrower vaults, pairing identities, keyring and saved configuration. They exclude old backups/recovery points, programs, external HTTPS/Caddy certificates, OS service/task definitions and environment-only secrets. Current data limit: 2 GB / 10,000 files. Windows DPAPI keys/settings require the same Windows computer; this is not a guaranteed cross-machine migration. Borrowers still need their own vault passphrase/recovery code. Keep archives private; copies can retain data deleted later.

RECOVERY (OFFLINE, NEW FOLDER ONLY): Stop the server. Keep the existing data folder separately. Run HomeLibrary.Server --unpack-complete-backup ARCHIVE NEW-FOLDER using the compatible server executable (Windows: HomeLibrary.Server.exe). It asks for the password without displaying it, checks encryption, file hashes and database integrity, and unpacks only into a new folder. Follow RECOVERY-READ-ME.txt there. Do not merge individual databases into a live server. Restore library export accepts JSON only, not this archive. Programs and external proxy/service configuration are separate. Get recovery help if unsure.

Flatpak offline unpack example (after stopping Carribrary):

    flatpak run --filesystem=~/Downloads:ro --filesystem=~/Documents --command=/app/lib/home-library/HomeLibrary.Server com.household.HomeLibrary --unpack-complete-backup /home/YOUR_USER/Downloads/YOUR_BACKUP.carribrary-backup /home/YOUR_USER/Documents/Carribrary-recovered

Replace YOUR_USER and YOUR_BACKUP with your own paths. The destination must not exist. This invocation grants temporary access only for recovery; it does not install a permanent filesystem override. The password is prompted, not passed as an argument. Read RECOVERY-READ-ME.txt in the recovered folder before replacing the stopped server's data. A Windows-created archive can be inspected elsewhere, but its DPAPI-protected keys/settings still require the original Windows computer for use.

Pi offline unpack: use /opt/home-library/server/HomeLibrary.Server --unpack-complete-backup ARCHIVE NEW-FOLDER after stopping home-library. Keep the private recovered data together with its keyring and preserve service-user ownership when restoring.

Complete snapshot recovery warning: Restoring an earlier complete snapshot rewinds account deletions, passwords, device revocations and lending history. Review accounts and paired devices before making the restored server reachable. Compatible program-only recovery keeps current data instead.

2.0.31: Optional permanent-copy QR labels and copy-number allocation choice. Create a complete backup before upgrading. Library format 3 protects the new preference; versions 2.0.30 and earlier cannot open it. Use the matching Help for label printing, sheet alignment and numbering policy. Existing history-linked numbers remain reserved.

2.0.32 repair update: cancellation confirmation/feedback and request status on all borrower tabs are corrected. History safeguards and operational hold rules remain. Registration protection is optional; default local rate limits and librarian approval work without Cloudflare. See the matching Help for private deployment configuration. Library format remains 3, with Windows program recovery compatibility for 2.0.36 custom-theme credentials. Back up first and update the server before companion apps.

2.0.36 source iteration: responsive administrator sidebar/mobile navigation and Home dashboard; Catalog remains the starting view. Home summaries show the whole active library. Quick Actions open existing workflows; Backup Now opens backup options, not an immediate backup. More / All tools retains existing tools. Borrower and native Android layout redesign follows separately. Build packages locally; this source handoff does not include rebuilt installers.

2.0.36: Back up before updating. Existing local administrator credentials are upgraded safely to support separate dashboard accounts. Earlier programs cannot decrypt the new credentials; do not downgrade. Existing inventory, borrower vaults, original username/password/recovery and approved-device tokens stay intact. All tools → Dashboard accounts manages additional full administrators. All tools / Library settings → Sidebar artwork uploads/removes your PNG; no default illustration is embedded.


Earlier 2.0.37 borrower navigation source update: approved borrowers start in Catalog. My reading remains second; My loans includes borrowing requests and confirmation-based cancellation. Account groups private-list controls, install, optional notifications, help, data/history/deletion and sign-out. Pending accounts retain help, install and deletion access. The server delivers this update to borrower browsers/PWAs; no new borrower install is needed. Native Android administrator styling is unchanged. Current library format 3, administrator credential format 2, and pairings remain. Program recovery supports 2.0.39; earlier programs remain incompatible. Build packages locally; this source update does not install them.


## Current 2.0.39: Android design, custom colors and versioned source folders

Use Carribrary-2.0.39-source.zip, which extracts into Carribrary-2.0.39. Inside is home-library-native with the existing Build-Carribrary launcher and matching root Help. GitHub Download ZIP uses Carribrary-main; rename that extracted top folder to Carribrary-2.0.39 yourself if using it. Do not merge source versions or overwrite a running build. Reuse existing private signing material for Android updates.

Android has refreshed cards, navigation and Book options. Colors → Custom theme provides eight color roles, full spectrum/hex controls, named save/edit/select and contrast checks. Save & use activates a custom palette on the server. Android preset/ saved selection remains a phone preference; Settings → Use my colors for shared library applies it library-wide. Browser dashboard, borrower pages, login/help and iPhone PWA follow server branding. Only public branding is available before login; all administrative operations remain authenticated. Refresh/reopen web apps after upgrading; rebuild/install Android to receive native changes.

Saved themes are additive library fields with validated custom IDs. Upgrade preserves existing library data, defaults and tokens, but earlier executables reject custom IDs or discard unknown fields. Program-only recovery accepts 2.0.39 only; complete historical recovery remains a separate supervised operation. Library and credential format numbers stay 3 and 2. Back up before installing. No installer, APK, AAB, Flatpak or DEB was built or installed in this source iteration.


## Current 2.0.39: Settings & Tools, title fitting and full sidebar overlays

The browser administrator has six navigable settings categories with search, breadcrumbs and in-page editors. Catalog, Borrowers, Lending and Requests follow shared dashboard tokens. Colors and custom themes are under Library & Appearance; book sharing and collection management are under Books & Organization. Daily checkout/return and dashboard shortcuts remain directly accessible. See distribution/ADMIN-TOOLS-INVENTORY-2.0.39.md for every relocated function.

Library names use whole-word fitting on every palette: preferred size, reduction to 1 rem, then logo-above-title layout. Extremely long unbroken names scroll inside the label. Upload a portrait PNG under Library & Appearance → Full sidebar overlay; preferred canvas 512 × 1536, input under 10 MB/8192 pixels per side, resized PNG up to 2 MB. Existing 512 × 256 data is accepted but may crop; re-upload a portrait original for best results. No image or personal library brand is bundled. The overlay cannot intercept navigation clicks.

No authentication, pairing or loan/history migration is introduced. Library/credential format versions stay 3/2. Earlier executables reject full-sidebar PNG dimensions, so program-only recovery now supports 2.0.39 only. Keep complete historical recovery points private and separate. Source archive extracts under Carribrary-2.0.39. Build packages locally; no installers were built or installed in this iteration.

Current source 2.0.40: borrower catalog/account and iPhone PWA polish. Use Carribrary-2.0.40-source.zip in a new versioned folder. Existing data/pairing are preserved. Program-only recovery supports 2.0.39–2.0.40. Build packages locally; no installers supplied with this source iteration.

Current source 2.0.41: branded public welcome, administrator Dashboard landing and Android/reading/detail polish. Extract Carribrary-2.0.41-source.zip into its new versioned folder; do not merge it into another build. Review distribution/RELEASE-TEST-CHECKLIST-2.0.41.md after compiling. Compatible program recovery: 2.0.39–2.0.41. No installer packages built in this source delivery.

Current source 2.0.45: Complete Backup Recovery is an independent application-menu wizard (Windows also tray), with archive verification, isolated rehearsal, confirmed restore, retained current data and local historical-access review. Named custom themes save colors and sidebar PNG together. Library format upgrades to 5; credential format remains 2. Compatible program-only recovery is 2.0.45 only. Guided complete recovery also supports same-OS 2.0.43/2.0.44 format-4 archives. See distribution/RELEASE-2.0.45.md and current Help. Build from the new numbered source folder; do not overwrite previous source, signing files or private backups. No release installers were built in this iteration.

Current source 2.0.46: New theme clears name/artwork draft; saved themes restore their own PNG with colors. Use current library artwork explicitly copies the current overlay. Closing without saving preserves the live library. Library/credential formats remain 5/2, compatible program-only recovery supports 2.0.45–2.0.46. See distribution/RELEASE-2.0.46.md. Extract into the separate numbered source folder; owner builds release packages.

2.0.47 recovery: create a portable complete archive before replacing the computer. Optional weekly backups retain four managed copies; download one off-device. Library format 6 protects rereading sessions/annotations/goals from older server serializers. Complete recovery and program rollback are different operations; see current Help and release notes.
