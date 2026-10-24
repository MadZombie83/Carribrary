# Carribrary 2.0.37 — borrower account navigation

Approved borrowers start in Catalog. The borrower web/PWA now has Catalog, My reading, My loans and Account navigation, with a bottom bar on phones and navigation above the content on larger screens. Catalog cards group borrowing, TBR and About actions; search remains available and is retained between views.

My loans groups borrowing history and My book requests. The navigation explicitly labels the number of open requests. Existing confirmation-based cancellation, conflict messages, holds, approval and handover behavior stay. My reading keeps the default TBR filter and a clear lock/unlock control. Catalog browsing and borrowing do not require unlocking the private reading list.

Account groups encrypted-list lock/unlock, private wishlist, finished-reading clearing, encrypted personal backup/restore, install/Home screen, optional due-date notifications, help, operational history/account deletion and sign-out. Pending accounts retain install, help, sign-out and self-deletion before approval. Account removal confirmations and encryption boundaries are unchanged.

## Borrower self-deletion repair

The old borrower deletion dialog rejected the same username casing accepted at sign-in and ambiguously displayed an @ prefix. The borrower dialog now shows the authoritative sign-in username, disables automatic capitalization and compares deliberately typed confirmation after normalizing case, surrounding whitespace and one optional @ prefix. It does not autofill or skip confirmation. The server still validates the canonical username, current password and current preview; full deletion remains blocked by active loans. Scope changes clear the entered confirmation/password and require a fresh preview. Administrator deletion confirmations are unchanged.

## Transparent library icons

Web and native Android icon uploads preserve transparent source pixels and empty square padding instead of adding a white background. Re-upload the original transparent image to replace an icon flattened by an older version. Installed Home-screen shortcuts may need removing and adding again.

## Build and compatibility

Source-only update. Server/tray/Android packaging metadata is 2.0.37-preview (Android code 20037); native iPhone source is 0.1.21/build 22 and remains unverified with Xcode. Native Android administrator layout is unchanged in this iteration; its updated bundled help explains borrower navigation. The hosted iPhone administrator PWA is distinct from the borrower PWA and keeps its existing approved-token flow.

The server delivers borrower UI updates to the browser/PWA when reopened or refreshed after the server update. No new borrower app download is required. Build server packages locally from a fresh complete 2.0.37 source folder; avoid replacing a folder during an ongoing build. The repository includes the required root Carribrary-Help-2.0.37.md. Nothing installs automatically.

Library format remains 3 and protected administrator credentials remain format 2. No library records, pairing tokens, encryption format or business API were migrated. Windows program-only recovery now accepts the unchanged-format 2.0.36–2.0.37 cohort; pre-2.0.36 programs remain rejected. Back up privately before installing an update. This is not a cross-format downgrade or full-machine rollback guarantee.

No installers, APKs, AABs, Flatpaks, Pi DEBs or release bundles were built or published by this source iteration. No live installation or owner build folder was changed.
