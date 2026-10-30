# Build Carribrary 2.0.53 locally — developers only

Normal users install a server package from GitHub Releases and obtain the optional Android companion from Google Play when published. They do not need this guide. Invited APK testers use distribution/ANDROID-TESTING.md.

The AI edits source code; your computer runs the compilers. Run the builder after the source handoff, once changes to that source copy are finished; then share only the summary or a relevant error excerpt if help is needed. Compiler execution does not perform model inference. Any AI work reading code/logs or diagnosing failures still uses model usage; no fixed percentage saving is promised.

The builder never installs the output, starts/stops your live server, changes your library data, publishes to a store, or creates/replaces a signing key. Back up before manually installing any new server package.

## On a Linux build computer

1. Extract the source into a new empty folder. Do not merge with an old source or installed library.
2. Open a terminal in its home-library-native folder.
3. Run:

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

## Current release and documentation revision

This is the frozen 2.0.53 feature set. Documentation-only revisions keep that app version. Use a new empty extraction folder for each downloaded revision; do not overwrite an older work folder. GitHub Download ZIP may still use Carribrary-main, so rename that extracted work folder yourself before building. The versioned source archive includes the matching standalone Help.

Build the updated server packages and native Android to deliver synchronized Help; browser PWAs receive it from the rebuilt server after refresh. Current library/credential formats are 7/2. Existing data and signing keys stay outside source folders. Create and verify a complete backup before installing your output. No source download itself updates an installed server.
