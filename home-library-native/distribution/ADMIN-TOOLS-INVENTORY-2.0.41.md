# Carribrary 2.0.41 — native and web tool locations

Web administration retains the [33-entry inventory](ADMIN-TOOLS-INVENTORY-2.0.39.md) and its original handlers. Local sign-in now starts on Dashboard; Settings links still open their intended category. Borrowers remain Catalog-first; paired-device authentication is unchanged.

Native Android now uses Settings & Tools with a category search and Back link. These are the same existing action IDs; no mutation, permission, secret storage, confirmation or offline/online guard was replaced.

| Category | Existing tools |
|---|---|
| Library & Appearance | Colors/custom themes; Library name/logo/sayings (Manage library) |
| Books & Organization | Books to buy; Share library/QR; Collections; Copy labels/numbering; Bulk edit (also retained under Catalog filters) |
| Borrowing & Reminders | Lending/holds/batch actions; Requests; Email reminders |
| People & Access | Borrowers; Borrower accounts/approvals; Paired admin phones |
| Data & History | Loan history; Data/privacy/history controls; Recycle Bin; CSV; Complete server backup; Library restore |
| System & Advanced | Setup/updates; Connection |

Refresh, Help and App info remain direct actions. Catalog/Loans/Reading remain bottom navigation. Request statuses Open/All/Closed filter only the visible list. Manage library uses three expandable sections: Library & appearance; Data/exports/recovery; Approved phones. Existing direct backup/restore/device routes open the appropriate section and retain their established action/confirmation behavior. Borrower deletion, account deletion, restore and revocation retain their existing confirmations and safeguards.

The server Reading Tracker groups its selected household-reader controls and reports only that reader’s totals. Its Edit/Finish/Remove/Add/Share actions stay in place. Administrative Loan history uses themed overview cards and responsive record cards; exports remain separate. Borrower private reading never becomes administrator-visible.

The public root has four access cards: Browse/Request explain the invitation requirement, Sign In opens the borrower account, and Help gives public guidance. It is not an alternate catalog endpoint and never discloses the sharing token. The companion header link opens the existing private pairing flow.
