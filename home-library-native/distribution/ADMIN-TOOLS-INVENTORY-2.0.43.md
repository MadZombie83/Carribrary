# Administration tool inventory — 2.0.43

All tools listed in [the previous complete inventory](ADMIN-TOOLS-INVENTORY-2.0.41.md) remain accessible. This release adds:

| Function | Server and iPhone/browser PWA | Native Android |
|---|---|---|
| Batch ISBN/manual acquisition | Settings & Tools → Books & Organization → Batch add / CSV import | Settings → Books & Organization → Batch add / CSV import |
| CSV header mapping, review, source notes, report | Same tool; Import library CSV; Download review report | Same tool; Choose CSV; Save review report |
| Renewal policy, requests and history | Settings & Tools → Borrowing & Reminders → Borrower renewals | Settings → Borrowing & Reminders → Borrower renewals |
| Enable email sign-in recovery | Borrowing & Reminders → Automatic email reminders → separate email recovery checkbox | Borrowing & Reminders → Email reminders → separate email recovery switch |
| Private import/source notes | Catalog → Edit details → Private import notes | Batch review → Edit or Catalog → Edit book → Private import notes |

Borrowers request extensions under signed-in My loans and recover sign-in passwords from Forgot your password? Neither grants access to another person’s records or resets encrypted-list credentials. Personal reading links and guest requests remain available but do not authorize account loan renewals.

The CSV tool adds records; JSON Restore remains a separate destructive operation with existing safeguards. Existing batch checkout/return, bulk editing, backups, account controls, device pairing/revocation and personal-data boundaries remain intact. Imported active-loan/account data is not fabricated from spreadsheet columns. All new mutations use the existing authenticated/revision-aware server pipeline.
