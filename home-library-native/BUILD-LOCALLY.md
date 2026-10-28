# Build Carribrary locally

The AI edits source code; your computer runs the compilers. Run the builder after the source handoff, once changes to that source copy are finished; then share only the summary or a relevant error excerpt if help is needed. Compiler execution does not perform model inference. Any AI work reading code/logs or diagnosing failures still uses model usage; no fixed percentage saving is promised.

The builder never installs the output, starts/stops your live server, changes your library data, publishes to a store, or creates/replaces a signing key. Back up before manually installing any new server package.

## On this existing Linux laptop

The workspace already has the build tools cached. From a terminal run:

```sh
python3 Build-Carribrary.py
```

Choose a numbered package. **windows** builds the Windows installer on Linux; **linux** builds the x86_64 Flatpak; **pi** builds the Raspberry Pi ARM64 DEB; **android** builds, lints and signs the Android APK/AAB. **all** builds the targets supported by this host. To compile Linux packages, Python must have Pillow available; use the Python runtime with Pillow installed if the prerequisite check reports it missing.

You can also run `./Build-Carribrary.sh`. If your file manager opens shell scripts as text, use a terminal. This is a command-line menu, not a new desktop application.

## On Windows or another computer

1. Install Python 3.10 or newer (3.11+ recommended).
2. Extract the **source** ZIP to a writable work folder, separate from the installed server, then open its `home-library-native` folder. An installed application folder is not a source checkout.
3. Install the tools for the target below once.
4. Double-click **Build-Carribrary.cmd** and choose **windows** or **android**. The console stays open when finished. Read the last status and the output path. If `py` is unavailable, the launcher tries `python`.

The final installer/packages appear one directory above `home-library-native`. Build logs and `build-summary.json` are in `.build-cache/logs-VERSION/` inside the source folder. A per-build SHA-256 file is written beside the output packages. Existing installed apps are unchanged until you install a generated package yourself.

### Tools by target

| Target | Needed |
|---|---|
| Windows installer, built on Windows or Linux | .NET SDK 10 and NSIS 3; .NET must support Windows targeting. The tray project already enables it. |
| Android, built on Windows or Linux | Compatible JDK (17 or 21 recommended), Gradle 8.11.1 or the supplied Gradle wrapper, Android SDK platform 36 and build-tools 35.0.0. Accept the SDK licenses yourself through Android Studio/SDK Manager. |
| Linux Flatpak | Linux, .NET SDK 10, Flatpak CLI, AppStream compose, dpkg-deb and Python Pillow. The existing Linux packaging environment also needs the AppStream tooling required by that CLI. |
| Raspberry Pi DEB | Linux, .NET SDK 10, dpkg-deb and Python Pillow. You can cross-build ARM64 on an x86_64 Linux laptop; a Pi is needed for physical runtime testing. |
| Native iPhone | Mac, Xcode and appropriate Apple signing/testing setup. This script cannot produce a verified iPhone build on Windows/Linux. The hosted iPhone PWA comes from the server build. |

Windows does not directly produce Flatpak/DEB files. Use Linux (or a correctly configured Linux environment such as WSL) for those targets. No cloud build account is required for the supported local builds.

First builds need internet for compiler dependencies unless the required packages are already cached/restored. Installing tools and accepting their licenses are separate one-time setup steps; the builder does not silently perform either.

## Check or preview before compiling

```sh
python3 Build-Carribrary.py --target windows --check
python3 Build-Carribrary.py --target android --check
python3 Build-Carribrary.py --target pi --dry-run
```

On Windows use `py -3` instead of `python3` if needed. The check reports found/missing prerequisite locations, not a certification that every installed tool version is compatible. The actual build reports incompatible versions/errors in its logs.

For an already restored/cached build:

```sh
python3 Build-Carribrary.py --target windows --offline
```

`--offline` does not download missing packages; .NET needs existing restore assets, and Gradle needs cached dependencies. Do an ordinary initial build first when starting from a fresh source ZIP.

## Android signing: keep your existing key

To update an installed Android app, use the **same private signing key**. The signing folder must stay private and outside source/public downloads; only the signed APK goes on the phone. The AAB is for Play Console, not direct installation. Never send signing files/passwords to the AI or support.

The existing workspace key is detected automatically if present. On a different Windows computer, set private file locations in PowerShell:

```powershell
$env:CARRIBRARY_KEYSTORE = 'C:\Private\home-library-release.p12'
$env:CARRIBRARY_KEY_PASSWORD_FILE = 'C:\Private\signing-password.txt'
$env:CARRIBRARY_PREVIOUS_APK = 'C:\Private\previous-working.apk'
py -3 .\Build-Carribrary.py --target android
```

`CARRIBRARY_PREVIOUS_APK` enables an explicit certificate-digest comparison and rejects a different key. `CARRIBRARY_KEY_ALIAS` defaults to `home-library`. Passwords are read by the signing tool from the file, not printed or included in the command line as plaintext.

For developer compilation without release credentials, use `--unsigned`. It produces clearly named UNSIGNED APK/AAB files; they are not a signed phone update. The script never generates a replacement key to get around a missing one.

## Tool location overrides

Normally PATH, standard Android/JDK locations or the existing workspace cache are enough. Explicit executable overrides are available: `CARRIBRARY_DOTNET`, `CARRIBRARY_MAKENSIS`, `CARRIBRARY_GRADLE`, `CARRIBRARY_JAVA`, `FLATPAK`, `APPSTREAM_COMPOSE`. Android SDK uses `ANDROID_HOME` or `ANDROID_SDK_ROOT`; JDK uses `JAVA_HOME` when provided.

## If a build fails

1. Read the step name and its named log file.
2. Check `build-summary.json` for the last completed step.
3. Review the relevant error lines before sharing; logs may contain local paths or information from your tool configuration. Do not share private keys, passwords, SDK credentials or an entire workspace.
4. Send the summary/error excerpt with the target you selected. The AI can repair the source; you can rerun the builder afterward.

A successful build is not a physical Windows/Pi/phone/Safari test. Run appropriate tests before distribution. Nothing is uploaded by this script.

## 2.0.34 source update

The iPhone/browser companion Help button now fetches help within `/companion/`; full-help links stay in that paired scope. This fixes a root-path response being parsed as JSON. The local server's Help still works at its normal root. If help temporarily fails, a readable message is displayed. Install the newly built server update and reopen the hosted companion to receive the repair. Local administrator login and existing approved companion credentials remain unchanged.

## 2.0.35 redesign source iteration

Use this source tree (or extract the entire 2.0.35 source ZIP) before running the builder. The first iteration changes admin web/paired-browser layout; borrower and native Android layouts are unchanged. Server, tray and Android packaging metadata are aligned to 2.0.35; offline help includes the new navigation. The native iPhone source project is 0.1.19/build 20, still unverified without Xcode. No 2.0.35 distributable packages were created here. Existing signing keys remain separate and private.

## 2.0.36 account/artwork source update

Build from the entire 2.0.36 source tree. Server/tray/Android metadata and all help are aligned; native iPhone source is 0.1.20/build 21 (still requires Xcode verification). No distributable 2.0.36 packages were built here. Back up before installing your build. This upgrades the protected local administrator credentials to format 2; the original sign-in/recovery survives, but earlier programs cannot decrypt the new store. Recovery is restricted to 2.0.36; a supervised return to an earlier release requires a complete matching pre-update backup, not old programs over current data. Existing paired-device tokens remain independent.


## Earlier 2.0.37 borrower interface source update

Use a fresh complete source download for 2.0.39; do not overwrite a source folder while it is building. The repository root includes Carribrary-Help-2.0.39.md, required by the synchronized-help check. Build locally as before. This source changes the server-hosted borrower account/PWA and help; native Android administrator layout is unchanged. Source package metadata is 2.0.39 (Android code 20039); iPhone source 0.1.21/build 22 still requires Xcode verification. No packages are built here. Library format 3 and local credential format 2 stay; compatible program recovery is 2.0.39, with pre-2.0.36 programs still rejected.


## Current 2.0.39: Android design, custom colors and versioned source folders

Use Carribrary-2.0.39-source.zip, which extracts into Carribrary-2.0.39. Inside is home-library-native with the existing Build-Carribrary launcher and matching root Help. GitHub Download ZIP uses Carribrary-main; rename that extracted top folder to Carribrary-2.0.39 yourself if using it. Do not merge source versions or overwrite a running build. Reuse existing private signing material for Android updates.

Android has refreshed cards, navigation and Book options. Colors → Custom theme provides eight color roles, full spectrum/hex controls, named save/edit/select and contrast checks. Save & use activates a custom palette on the server. Android preset/ saved selection remains a phone preference; Settings → Use my colors for shared library applies it library-wide. Browser dashboard, borrower pages, login/help and iPhone PWA follow server branding. Only public branding is available before login; all administrative operations remain authenticated. Refresh/reopen web apps after upgrading; rebuild/install Android to receive native changes.

Saved themes are additive library fields with validated custom IDs. Upgrade preserves existing library data, defaults and tokens, but earlier executables reject custom IDs or discard unknown fields. Program-only recovery accepts 2.0.39 only; complete historical recovery remains a separate supervised operation. Library and credential format numbers stay 3 and 2. Back up before installing. No installer, APK, AAB, Flatpak or DEB was built or installed in this source iteration.

Compiler caches across versioned folders: the builder checks the new folder's .build-tools, then its parent's .build-tools, then the sibling Carribrary-main/.build-tools from the earlier extraction workflow. Existing caches and signing configuration can therefore be reused without merging sources. Set CARRIBRARY_TOOLS to choose a different private tool-cache folder. The builder never moves or replaces old source folders.


## Current 2.0.39: Settings & Tools, title fitting and full sidebar overlays

The browser administrator has six navigable settings categories with search, breadcrumbs and in-page editors. Catalog, Borrowers, Lending and Requests follow shared dashboard tokens. Colors and custom themes are under Library & Appearance; book sharing and collection management are under Books & Organization. Daily checkout/return and dashboard shortcuts remain directly accessible. See distribution/ADMIN-TOOLS-INVENTORY-2.0.39.md for every relocated function.

Library names use whole-word fitting on every palette: preferred size, reduction to 1 rem, then logo-above-title layout. Extremely long unbroken names scroll inside the label. Upload a portrait PNG under Library & Appearance → Full sidebar overlay; preferred canvas 512 × 1536, input under 10 MB/8192 pixels per side, resized PNG up to 2 MB. Existing 512 × 256 data is accepted but may crop; re-upload a portrait original for best results. No image or personal library brand is bundled. The overlay cannot intercept navigation clicks.

No authentication, pairing or loan/history migration is introduced. Library/credential format versions stay 3/2. Earlier executables reject full-sidebar PNG dimensions, so program-only recovery now supports 2.0.39 only. Keep complete historical recovery points private and separate. Source archive extracts under Carribrary-2.0.39. Build packages locally; no installers were built or installed in this iteration.

Current source 2.0.40: borrower catalog/account and iPhone PWA polish. Use Carribrary-2.0.40-source.zip in a new versioned folder. Existing data/pairing are preserved. Program-only recovery supports 2.0.39–2.0.40. Build packages locally; no installers supplied with this source iteration.

Current source 2.0.41: branded public welcome, administrator Dashboard landing and Android/reading/detail polish. Extract Carribrary-2.0.41-source.zip into its new versioned folder; do not merge it into another build. Review distribution/RELEASE-TEST-CHECKLIST-2.0.41.md after compiling. Compatible program recovery: 2.0.39–2.0.41. No installer packages built in this source delivery.

Current source 2.0.42: companion summary panels, Home/navigation/search and request bells. Use Carribrary-2.0.42-source.zip in its own folder. See distribution/RELEASE-2.0.42.md for verification and owner device checks. Source only; no installers built. Compatible program recovery: 2.0.39–2.0.42; library/credential formats remain 3/2.

Current source 2.0.43: batch ISBN acquisition/CSV mapping, borrower renewals and opt-in SMTP password recovery. See distribution/RELEASE-2.0.43.md for limits and tests. Use the separately versioned source folder. Export a complete private backup first: library format upgrades to 4; program-only recovery permits 2.0.43 only, not earlier programs. Credential format remains 2. Owner builds installers.

Current source 2.0.44: saved complete-backup verification on server/iPhone PWA/Android, independent offline verification and format-4 Android JSON restore. See distribution/RELEASE-2.0.44.md. Library format remains 4 and credential format remains 2; compatible program recovery supports 2.0.43–2.0.44. Build from the separate numbered source folder and retain a complete private backup before updating. No release packages were compiled by the agent.

Current source 2.0.45: Complete Backup Recovery is an independent application-menu wizard (Windows also tray), with archive verification, isolated rehearsal, confirmed restore, retained current data and local historical-access review. Named custom themes save colors and sidebar PNG together. Library format upgrades to 5; credential format remains 2. Compatible program-only recovery is 2.0.45 only. Guided complete recovery also supports same-OS 2.0.43/2.0.44 format-4 archives. See distribution/RELEASE-2.0.45.md and current Help. Build from the new numbered source folder; do not overwrite previous source, signing files or private backups. No release installers were built in this iteration.

Current source 2.0.48: New theme clears name/artwork draft; saved themes restore their own PNG with colors. Use current library artwork explicitly copies the current overlay. Closing without saving preserves the live library. Library data upgrades to format 6; credentials remain 2. Compatible program-only recovery supports 2.0.48 only. See distribution/RELEASE-2.0.48.md. Extract into the separate numbered source folder; owner builds release packages.

This source also consolidates Library identity and Theme & artwork, adds portable weekly complete backups retaining four, opt-in library analytics and reading progress/rereads/goals/quotes. Build after Help synchronization; run tests before distributing packages. Portable backups still need an off-device copy.
