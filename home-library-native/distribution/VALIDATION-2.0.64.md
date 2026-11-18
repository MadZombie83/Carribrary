# Carribrary 2.0.64-preview validation

Completed:
- Android compileReleaseKotlin with the existing JDK 17/Android SDK and offline Gradle cache passed.
- Camera preview uses CameraX COMPATIBLE rendering and a clipped, bounded preview slot.
- Close is placed in a separate fixed top row. Landscape puts scrollable controls beside the preview.
- The current scan callback is read through rememberUpdatedState so the automatic-lookup switch affects subsequent scans.
- Help and privacy synchronization checks passed.

The camera-overlay fix still needs reproduction and verification on the owner's Android phone, including multiple scans, portrait/landscape rotation and Close.
No Android emulator, physical Bluetooth scanner, signed APK, AAB or installer was used to verify this change.
Test keyboard-scanner Enter suffix, repeated identical ISBN copies, automatic lookup on/off during scanning, and manual missing-details retry before distribution.
The toggle affects new queued rows. Already pending lookups can finish.
