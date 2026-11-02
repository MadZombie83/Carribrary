# 2.0.54-preview validation

Scope: source changes for administrator server-update discovery and borrower dashboard visual parity. No release installers built, installed or published by this work.

## Evidence

- Native build completed with zero warnings/errors.
- Server update model checks: channel filtering, numeric version ordering, same-version stable promotion, fixed official links, rejected drafts/external links, cached automatic/manual checks and invalid-response handling.
- Existing self-service model/security checks passed: disabled permissions, account/copy ownership, revocation, request replay, race handling, reported return pending receipt, backup and recovery safeguards.
- Help/privacy sync, Help completeness, third-party/licensing documentation and local builder/source archive unit checks passed.
- Browser regression results are recorded in the local regression logs; final run status is appended below after verification.

Library format 7 and credential format 2 are unchanged. No borrower-private reading data is used in the librarian update notice or borrower circulation overview. Borrower account APIs remain server-authorized; the new hold projection filters to the signed-in borrower's manual holds.

## Limits

Physical Android/iPhone, card printing, camera scanning, installed Windows/Flatpak/Pi packages and signed store artifacts still require owner acceptance. Cached test tooling is used; native iOS builds require macOS/Xcode. The update notice is delivered by this new server version, so an existing 2.0.53 server must first be updated manually once. Nothing modifies existing public 2.0.53 installers.

## Verified source checks

The full native server suite passed, including mobile-artwork validation/theme compatibility, borrower-owned hold projection and existing authentication/privacy/self-service/recovery checks. Android compileReleaseKotlin succeeded without an APK build. Browser checks passed for borrower layout, cross-platform artwork and mobile override/fallback, theme reset/restore, administrator update notices, product footers, encrypted private lists, borrower self-service, catalog controls and Settings & Tools pages. Administrator page/summary browser checks also passed after updating their responsive-navigation helpers for the shared drawer. Help now contains 88 synchronized topics.
