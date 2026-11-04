# Carribrary 2.0.59 — remaining device tests

Use a test library for exercises that change or delete records. Record the device, installed version, result and any exact error. Never send passwords, private QR codes, borrower details or backup files to support.

## First: Windows update and recovery rehearsal

1. In your current library, make a complete backup. Download it, verify it and keep a copy on another drive. Keep its password separately.
2. Run the official 2.0.59 Windows installer over the existing installation. Keep existing data; do not uninstall first. Check that setup shows its stage, elapsed time and log location and finishes without getting stuck.
3. Open Carribrary. Check the footer says 2.0.59-preview. Confirm the library name, artwork, books, borrowers, loans and already-paired phone are still present.
4. Open the local address in an InPrivate window. It should require administrator sign-in even though Windows itself is already signed in.
5. Open Illustrated quick start. Its heading and footer should both show 2.0.59.
6. Open Start → Carribrary → Complete Backup Recovery. Select a complete backup, enter its password, then verify it.
7. Choose Start recovery test. Open its separate RECOVERY TEST library and check the books, borrowers and loans. Stop the test and close the tool afterward. Do not restore over the working library as part of this rehearsal.
8. Confirm the original library still works. Record whether every step passed.

## Linux Flatpak on a separate test computer

- Install/update 2.0.59, open it from the application menu and verify its footer.
- Test initial administrator setup on a clean test installation, then close/reopen and sign in.
- Add, lend and return a sample book; verify a complete backup and run the separate recovery test.
- Follow the graphical public-sharing setup on the tester's own network. Test the HTTPS address from a phone with Wi-Fi off, then open the borrower invitation link.
- Check Stop/restart and the desktop/menu recovery launcher. Confirm an update keeps data.

## Android and iPhone/browser apps

- Confirm existing librarian pairing survives the server update.
- Scan an existing ISBN for checkout and return. Also scan an optional copy QR label and test manual entry when camera permission is denied.
- Test multiple copies of one title: only eligible copies are offered.
- On iPhone/Safari and Android/Chrome, install the appropriate browser app through its supported installation path. Open from the Home Screen, rotate the phone and check artwork, navigation, Help and keyboard access.
- Borrower: guest requests still work; signing in retains catalog search/filter/sort controls. Check private reading lock/unlock and own loans.
- Request a book, approve/hold it, then hand it over. Report a return as a borrower; confirm it stays pending until the librarian receives it.
- If notifications are enabled, test an actual notification and confirm disabling/sign-out stops the device subscription.
- Pair a disposable extra device, revoke it, and confirm its next server request is denied. Do not revoke the only working administrator device without another way to sign in.

## Raspberry Pi

This requires a real Pi 4/5 with 64-bit Raspberry Pi OS. Package inspection on an Intel/AMD laptop is not a Pi installation test.

- Fresh install, administrator setup and service startup.
- Reboot, update while retaining data, phone pairing and local/public access.
- Checkout/return, backup verification and the recovery rehearsal.

## Shared station and printing, if being advertised

- Test a real USB/webcam scanner, physical card/PIN and printed copy label.
- Check the station has no administrator access; an idle station clears the last user's information.
- Check self-service permissions start off, can be enabled separately and are enforced after disabling them.
- Print cards/labels at actual scale; check scan reliability and margins.

## Before a stable or Play Store release

Finish the device tests above and resolve failures. Android also needs the final signed build, Play testing/submission, actual public policy/account-deletion URLs, reviewer access and final artifact/license review. Source compilation and browser automation do not complete those steps.
