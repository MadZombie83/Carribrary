# Distribution and licensing — Carribrary 2.0.53

Developer/publisher display name: **Chezburber Studios**.
Approved public support contact: **chezburbermerch@gmail.com**.

## Normal user path

- Free Windows, Linux Flatpak and Pi ARM64 server installers: official `MadZombie83/Carribrary` GitHub Releases, once published for the recommended release.
- Optional paid Android librarian companion: Google Play, planned one-time US$15 with regional price/tax shown by Google Play. Public listing is not yet live. Invited testers use their supplied test path.
- Borrowers: free shared-link browser/PWA. They do not need the paid companion.
- iPhone librarians: hosted Safari companion/PWA.
- Source code: optional inspection/developer/private-build path, separate from normal installation. GitHub's automatic Source code ZIP is not an installer and cannot be removed from a normal tagged release; release descriptions should lead with the three server installer assets, not the source download.

Do not invent a Play listing URL or imply that private GitHub downloads are accessible to the public. Before publication, set actual public download/policy/deletion URLs and verify access in a signed-out browser. Do not publish Android APK/AAB assets by default; release AAB goes privately to Play Console. Check Play App Signing and the test-to-store update path before migrating tester phones.

## Chosen model

`LICENSE.txt` is the **Carribrary Source-Available Proprietary License 1.0** for Carribrary-original material. It permits free server operation for households, churches, schools and internal organizational/business libraries, source inspection and private server builds. It reserves rebranding, redistribution/resale and selling a hosted service to unrelated customers unless separately authorized. Normal librarian/borrower remote access and library membership/book fees remain allowed. The paid companion purchase is separate; borrower access is free.

This is a custom proprietary license, not an OSI-approved open-source license. Do not label the project MIT, Apache, GPL or “open source.” AppStream continues to use `LicenseRef-proprietary`. GitHub may display a custom/other license, which is expected. Publishing viewable source is not the same as granting unrestricted reuse. Source-host-required viewing/forking and all third-party rights are explicitly preserved.

Legal review before public distribution remains a release gate. This document records the owner's business choice and the proposed operational interpretation; it is not legal advice or a certification of enforceability, rightsholder identity or consumer-law compliance. Confirm that Chezburber Studios is the correct rightsholder/trading name and that the contribution history gives the publisher the necessary rights. Do not silently accept contributions under an incompatible inbound license; use a reviewed contributor agreement if outside contributions are accepted later.

## Dependency/attribution review performed

The inventory `THIRD-PARTY-INVENTORY-2.0.53.json` records the 100 exact resolved Android release artifacts and the restored server NuGet packages. It uses cached release resolution/POM/NuGet metadata rather than a guessed list of direct dependencies. Parent license evidence for ZXing/Guava and the AutoValue POM notice is recorded. Forty-two license/notice files were extracted from resolved Android JAR/AAR archives. Common license texts, libyuv BSD terms and pinned ZXing/SQLitePCLRaw texts are preserved in `licenses/`.

Full .NET, ASP.NET and Windows Desktop runtime license/third-party notice files from the restored 10.0.12 packages were added. WebPush is MPL-2.0: its existing full covered source ZIP and license remain supplied, without Carribrary's restrictions applying to that component. Bouncy Castle, browser ZXing, ts-custom-error, Nayuki and Gradle notices remain included. Google SDK/ML Kit terms are distinguished from Apache-licensed AndroidX/Kotlin dependencies. Existing Google Books branding/credit and provider disclaimers remain.

`THIRD-PARTY-NOTICES.txt` describes those components and rights. Android includes legal assets and an existing-Help credits topic with the resolved list and original/common notices. Windows, Flatpak and DEB include the proprietary license and full third-party license directory. The proprietary terms do not override covered source, attribution, redistribution or other rights granted by a third-party license.

## Remaining release gates — do not mark cleared by a source check

1. Review the custom proprietary license and ownership/contribution history.
2. Build the final server installers and signed Play AAB, then inspect their actual payloads for the expected license/notice assets, runtime notices and native libraries. Reconcile any dependency changes with this inventory; do not assume another machine resolves the same versions.
3. Verify official Caddy's separately downloaded binary and upstream third-party notices. Caddy is not bundled here, but its distribution/installation path still needs the applicable notices.
4. Review Google ML Kit/Android SDK/Books terms and disclosures, book-cover/content rights and attribution, and optional provider terms. A user's own API key is not a waiver of provider terms.
5. Verify Android Play App Signing, upgrade compatibility, store declarations, reviewer access, actual screenshots and the public privacy/account-deletion URLs. Do not claim submission or approval from this metadata update.
6. Keep the repository private until the owner deliberately makes it public. Use only the approved public contact; commit authors use GitHub noreply. No personal email, API keys, signing keys or live library data belong in release assets.

Features remain frozen. Documentation, legal metadata and distribution corrections do not authorize new product features.
