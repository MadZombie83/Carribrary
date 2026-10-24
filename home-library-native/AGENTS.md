# Home Library maintenance

Every user-visible change must include matching help updates in the same release.
This includes new features, changed behavior, settings, confirmations and limits.

- Edit canonical `assets/help.json`, set its release to the current server release,
  and use `scripts/sync-help.py` to synchronize server, offline Android/iPhone help
  and the standalone versioned guide.
- Run `scripts/sync-help.py --check` before packaging. Rebuild affected packages
  after synchronizing help so users receive the current instructions.
- Update the borrower browser's Help, release instructions and privacy text when
  those workflows or data handling change. See DEVELOPMENT.md for release checks.
