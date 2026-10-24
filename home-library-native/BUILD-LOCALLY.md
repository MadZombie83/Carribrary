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


## 2.0.37 borrower interface source update

Use a fresh complete source download for 2.0.37; do not overwrite a source folder while it is building. The repository root includes Carribrary-Help-2.0.37.md, required by the synchronized-help check. Build locally as before. This source changes the server-hosted borrower account/PWA and help; native Android administrator layout is unchanged. Source package metadata is 2.0.37 (Android code 20037); iPhone source 0.1.21/build 22 still requires Xcode verification. No packages are built here. Library format 3 and local credential format 2 stay; compatible program recovery is 2.0.36–2.0.37, with pre-2.0.36 programs still rejected.
