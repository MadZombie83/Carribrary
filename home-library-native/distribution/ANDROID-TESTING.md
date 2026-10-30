# Android installation — invited testers only

Normal customers obtain the optional paid Carribrary librarian companion from Google Play once it is published. Borrowers use their library's free browser app and do not need this companion. The free server installers are provided on the official GitHub Releases page.

Before publication, the developer may supply a signed test APK or a Play testing link. An APK is the Android installation file. Never obtain it from an unofficial mirror.

1. Prefer the Play testing invitation if one is provided; follow that invitation and install through Google Play.
2. If the developer supplies a signed test APK, download only that file on the phone.
3. Open the file and approve installation for this source only when Android asks. Turn that permission off afterward if you no longer need it.
4. Open Carribrary and follow the normal librarian pairing Help.
5. For a test update, install the developer-supplied replacement over the old app. Do not uninstall or clear app data to update.
6. If Android says the signatures do not match, stop and contact the developer. Do not erase your working app to force the update.

Private signing folders, keystores, passwords and source ZIPs never belong on a tester's phone. Play and locally signed test builds can have different signing identities: do not assume a sideloaded test build upgrades in place to the Play build. The developer must verify the chosen Play App Signing/update path before release. Keep circulation changes synced and follow the supplied transition instructions.

Do not publish APKs/AABs or signing materials in GitHub Releases by default. Public GitHub downloads are server installers. The developer uploads the release AAB privately to Play Console. A paid Play listing may use Play-managed testing; follow the current test invitation rather than guessing whether payment is required.
