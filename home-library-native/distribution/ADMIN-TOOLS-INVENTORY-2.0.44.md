# Administrator tool inventory — 2.0.44 increment

All 2.0.43 routes remain available; no previous tool is removed or relocated. See the existing complete 2.0.39 inventory and 2.0.43 additions.

| Function | Dashboard / iPhone PWA | Android |
|---|---|---|
| Verify saved complete archive/password | Settings & Tools → Data & History → Verify complete backup | Settings → Data & History → Verify complete backup |
| Verification shortcut | Data & History → Backups → Verify complete backup | Dedicated Data & History entry |
| Create complete backup | Existing Backups tool | Existing Complete server data backup |
| Restore format-4 JSON | Existing Restore library export with confirmation | Existing Restore library backup with confirmation; picker now permits format 4 |

Independent verification runs via `--verify-complete-backup ARCHIVE` without opening the live server. Complete recovery remains the separate stopped-server/new-folder tool. This release adds no live full-data replacement, remote recovery bypass, database migration or new borrower access to administrator tools. Readable Settings headings use the same theme tokens on all dashboard/companion category pages.
