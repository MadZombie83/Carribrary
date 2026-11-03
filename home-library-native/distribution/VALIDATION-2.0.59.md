# 2.0.59 validation

- Full native server suite passed (exit 0), including supported complete-archive cohorts, portability/platform checks, future/mismatched/unverified rejection, restore retention, failed-start undo and interrupted journals.
- Browser suites passed: quick-start, workflow-polish, complete-recovery, backup-verification and server-updates. Complete recovery rehearsed an archive created by the current server in a separate test server; paired verification/revocation and password-error handling passed.
- Illustrated guide checked without overflow at 360×800, 390×844, 430×932, 667×375, 844×390, 768×1024, 1024×768 and 1440×1000. Browser resizing covered; physical installed-PWA orientation is not claimed.
- PowerShell recovery core passed: current cohort accepted; older/future/invalid points rejected; current data/settings retained; health failure undone; corrupt archives and unsafe paths rejected.
- Setup progress process arguments/output/timeout tests and all PowerShell parsing passed. Help sync/check, 91-topic documentation contracts, licensing, local-builder/archive and mock recovery-launcher checks passed.
- Release-sensitive tests were repaired: update fixture always uses a version newer than the current server; backup verification waits for the current Carribrary footer wording.

Evidence: output/release-guide-2.0.59, output/backup-safety-2.0.59, .build-tools/259-native.log and targeted browser logs. No release installers/APKs generated or installed. Real Windows GUI/service/firewall/installation acceptance remains owner testing.
