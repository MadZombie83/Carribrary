# Play Store preparation — Carribrary 2.0.53

Owner/publisher checklist, not librarian Help or production approval. Features are frozen. Current source validation is in `../distribution/VALIDATION-2.0.53.md`; the old validation file in this folder records earlier builds only.

## Prepare an account and test build

1. Open [Google Play Console](https://play.google.com/console) with the intended publisher account. Create/verify a developer account if needed; do not assume a previous application completed enrollment.
2. Confirm account type, identity verification and production-access requirements in that Console. For affected new personal accounts, Google's current rule requires at least 12 testers continuously opted into a closed test for 14 days before applying for production access. Check [Google's current requirements](https://support.google.com/googleplay/android-developer/answer/14151465?hl=en).
3. Build the current matching server and signed Android APK/AAB. The AAB is for Play; the APK is for direct device testing. The hosted iPhone companion comes from the server and is not a Play artifact.
4. Keep `com.household.homelibrary` and the intended app-signing identity. Choose Play App Signing deliberately. An upload key is not necessarily the installed app-signing key. Never distribute signing folders/passwords.
5. Use an internal test track first and resolve Console errors. Do not roll out to production from this checklist.

## Before production review

- Owner's intended model: optional Android librarian companion for a one-time USD15 fee; free self-hosted server and free borrower access. Configure pricing/payment through the owner account. Do not accidentally publish a planned paid app as permanently free.
- Use only the dedicated public support email `chezburbermerch@gmail.com`; do not expose a personal email in listings, policies, commits or screenshots. Final publisher/business identity still needs owner confirmation.
- Finalize STORE-LISTING, privacy and Data safety drafts against actual behavior, ML Kit/Android dependencies, self-hosted data handling and current Play forms. Publish real HTTPS policy, support and account-deletion information. Draft local files are not public URLs.
- Resolve reviewer access using REVIEW-ACCESS.md. A ten-minute pairing code or personal household library is not reliable standing review access. Do not weaken pairing or add a demo/review feature under the freeze without explicit owner approval.
- Run TEST-PLAN.md on real hardware, including updates, native alerts, camera/PWA, 16KB compatibility and remote access. Source compile tests are not install tests.
- Verify server downloads and clear GUI setup/update/recovery instructions. Windows signing, Flathub requirements and physical Pi acceptance remain separate distribution checks.
- Recheck third-party distribution/attribution rights and provider terms. Optional user-supplied Google keys do not by themselves certify commercial rights.
- Audit every public artifact for credentials, library data, private email and signing material before release.

Account enrollment, agreements, payment/pricing, public policy hosting and store rollout remain owner-controlled. No new Play upload or public rollout is performed by this documentation revision.
