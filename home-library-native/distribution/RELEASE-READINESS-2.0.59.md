# Carribrary 2.0.59 — release readiness checkpoint

Checked November 3, 2026. Feature additions remain frozen. The next gate is physical installer/device acceptance, followed by final release/store preparation.

## Automated results

- **39 browser suites pass**, including targeted reruns after older test assumptions were corrected.
- Full 2.0.59 native server suite previously passed; no application code changed during this checkpoint.
- Current Android source compiles successfully using cached release dependencies. No APK/AAB or release installer was built.
- The newly added unsaved-edit and responsive-artwork suites now run in the default full browser regression command.

Five older tests assumed the former mobile navigation/footer layout or exact old theme/icon text. They now use the current menu helper, the explicit Catalog navigation entry, semantic theme tokens, normal Settings navigation and current upload feedback. Account isolation, forbidden API actions, password recovery, private-vault confidentiality and revocation assertions remain in place. This pass found no new application defect and needs no new build number or installer rebuild.

## Browser suites passed

workflow-polish, responsive-artwork, server-updates, borrower-layout, product-info, self-service, public-connection, artwork-platforms, purchase-suggestions, admin-notifications, pwa-diagnostic, analytics-navigation, reading-polish, appearance-portable, theme-artwork, complete-recovery, backup-verification, acquisition-renewals, borrower-controls, summary-panels, release-polish, admin-landing, settings-pages, admin-pages, custom-themes, repair-requests, accounts, private-vault, account-deletion, borrower-app, data-controls, lending, circulation, collections-google, registration-security, turnstile, complete-backup, quick-start, ecosystem.

## Next: actual Windows update and recovery rehearsal

Follow [the device checklist](DEVICE-CHECKLIST-2.0.59.md), starting with its Windows section. Verify a complete backup, install 2.0.59 while retaining data, confirm login/paired-phone preservation and run the separate recovery test. A successful browser test is not proof that Windows service, permissions, firewall or installer GUI behavior works on a particular computer.

## Remaining release gates

- Current-package Windows and Flatpak fresh-install/update/reboot/launch acceptance.
- Real Raspberry Pi ARM64 installation/service/reboot acceptance; no Pi hardware result is claimed.
- Real Android/iPhone camera, installed-PWA behavior, notification delivery and update/pairing checks.
- Physical scanner/card printing and authorized shared-station behavior when those options are offered.
- Final signed Android artifact, dependency/license review, public policy/account-deletion/reviewer access and Play Console testing/submission. Google Play production availability is not claimed.

Earlier owner workflow/recovery passes remain useful history, but are not automatically marked as current 2.0.59 package acceptance. Public server preview testing can continue while these gates remain open. Existing installer releases are unchanged by this report.
