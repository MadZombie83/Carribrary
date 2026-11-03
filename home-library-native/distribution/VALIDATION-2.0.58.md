# Carribrary 2.0.58 validation

## Completed checks

- Release server/test compilation passed. Full native suite passed with exit code 0.
- Browser suites passed: workflow-polish, settings-pages, theme-artwork, appearance-portable, server-updates, admin-pages, circulation, lending, borrower-controls, borrower-layout and private-vault. Relevant suites were repeated after the shared action-row/status changes.
- New workflow tests verify declined close/hash-navigation prompts preserve drafts, confirmed discard leaves, save errors retain entries, successful retries persist, busy/save feedback and navigation locks clear, contextual Help works, and empty settings search can be cleared. Mobile controls and draft cancellation are included.
- Existing settings inventory/search/Back/busy-backup safeguards, presets/saved artwork, administrator pages, barcode eligibility/ambiguity and checkout/return behavior, loan holds, borrower controls and encrypted private reading continue passing.
- Setup progress scripts and all source PowerShell parse. Cross-platform process tests verify argument/output handling, nonzero failure and bounded timeout. NSIS preprocessing passed with the new helper included. These checks do not execute a Windows installation.
- 91 synchronized Help topics across server, Android, iPhone and standalone guide. Help/link, licensing, builder, source archive, recovery-launcher and iOS-reference checks passed. Optional Swift grammar tooling unavailable.

## Limits and acceptance

Windows WinForms progress, actual service/firewall changes, upgrade/recovery and PowerShell 5.1 execution still require owner testing on real Windows after rebuilding. The external installation-stall report remains unconfirmed; no root cause is claimed. Snapshot copying is not force-terminated by helper timeouts. No release installer or APK was built in this workspace. Native Android screens were not changed; browser companion and borrower workflows receive the new form behavior. Physical installed-PWA tests remain owner acceptance.

Evidence: output/workflow-polish-2.0.58 and development logs .build-tools/258-*. Setup logs stay local; field/draft values are not persisted or logged by the new guard. No borrowing/account permission model, encrypted-vault format or library database format changed.
