> HISTORICAL TEST RECORD: results below apply to the named older builds, not current 2.0.53 package acceptance. Use ../distribution/VALIDATION-2.0.53.md and the current TEST-PLAN.md.

# Validation record — Android 2.0.9-test

Completed October 4, 2026:

- Gradle bundleRelease, assembleRelease and lintRelease: BUILD SUCCESSFUL.
- Android API 36 SDK; bundle manifest minSdk 29, targetSdk 36, versionCode 20009.
- App ID com.household.homelibrary; APK signed by the same existing certificate as 2.0.8 preview.
- AAB JAR signature verified. Android signing uses a self-signed certificate; JAR PKIX/timestamp warnings are expected and are not Play acceptance verification.
- Google bundletool 1.18.0 validate: passed.
- Bundle configuration requests PAGE_ALIGNMENT_16K.
- All eight ARM64/x86_64 native libraries have compatible ELF load-segment alignment.
- Direct APK and universal APK generated from the AAB passed zipalign -c -P 16 4. Both APK signatures verified and matched the previous preview signing identity.
- Final permissions: INTERNET, CAMERA, ACCESS_NETWORK_STATE and the app's generated signature-protected receiver permission. No AD_ID, contacts, location, SMS-send or broad-storage permission in the final manifest.
- Bundled privacy/notices exist. Publisher/contact placeholders deliberately remain for internal testing; they must be replaced before public release.
- Camera explanation and unpaired server requirement are implemented. Thumbnail identity fix from 2.0.8 is retained.

Not performed: Play Console upload/review, real Android 16/16 KB runtime checks, physical Pi installation, final store screenshot capture, final Data safety submission, reusable review enrollment, public hosting, payment profile/account setup or production rollout. Automated packaging checks do not prove Play approval or hardware behavior.

## 2.0.12 preview cover fallback update

The Android candidate adds cover previews/selection through the paired server (requires 2.0.12), retains its signing identity and updates external-provider privacy disclosures. No Google Books integration or API key is included. Release compilation/lint, APK signature comparison, signed AAB verification, bundletool validation and native ELF alignment checks passed. Physical phone/UI testing and Play publication remain outstanding.

## 2.0.23-preview — October 16, 2026

Server unit/model tests include protected browser pairing and revocation, metadata cache/plain-text/source handling, shared theme validation, and fake-transport reminder/request notification timing, deduplication and account-revocation cleanup. Headless Chrome over a disposable local HTTPS proxy verifies signup colors, custom icon resizing/manifest, isolated browser admin cookies/API guards, editing, network-only scoped workers, EAN decoding with bundled ZXing and permission-denied manual fallback. Borrower install/offline/shared-link tests and existing account/deletion/TLS/mTLS regressions also pass. No live push service was used to send a real notification in these tests.

Android 2.0.23 builds/lints against API 36 (min29), versionCode20023. New requested permissions are POST_NOTIFICATIONS and RECEIVE_BOOT_COMPLETED, solely for user-enabled periodic request alerts. Job service requires the system BIND_JOB_SERVICE permission. No ad ID, contacts, location, SEND_SMS or broad storage permission was added. Existing signing identity is preserved and final APK/AAB signatures, bundle structure and 16KB alignment are checked during packaging. Native iPhone source is grammar/project checked only; the hosted iPhone PWA needs real Safari/camera/push tests. Physical Android job scheduling/permission, Windows service update and Pi/Flatpak installs remain device checks; no Play upload or production approval is claimed.
