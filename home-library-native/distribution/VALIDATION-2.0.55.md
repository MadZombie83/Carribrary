# Carribrary 2.0.55 validation

## Completed checks

- Server/test project Release compilation: no warnings or errors.
- Full native suite passed with exit code 0. Focused native tests: icon limits, invalid dimensions/oversize rejection, independent orientation settings, saved-theme/backup serialization, legacy client preservation, theme reset/restore, borrower dashboard boundaries and server-update fixtures passed.
- Browser tests passed: responsive-artwork, artwork-platforms, theme-artwork, appearance-portable, borrower-layout, product-info and server-updates. The final responsive-artwork and borrower-layout checks were repeated after contrast and preview fixes.
- Responsive-artwork covers 360 × 800, 390 × 844, 430 × 932, 667 × 375, 844 × 390, 768 × 1024, 1024 × 768 and 1440 × 1000. It rotates/resizes one document without reload, verifies independent cover/contain/focal settings, and tests full-image fitting in catalog and account headers.
- Detailed transparent PNG above the old 350,000-byte cap uploads and saves through the real administrator UI. Corrupt replacement cannot save a stale/partial icon. A deterministic generated fixture is committed; owner logos are not.
- Android offline compileReleaseKotlin passed; no APK produced.
- Synchronized Help: 89 topics across server, Android, iPhone and standalone guide. Help links, licensing, local builder, source archive and recovery launcher checks passed.
- iPhone project/source references passed; optional Swift grammar tooling unavailable.

## Visual evidence and limits

Browser screenshots are in output/responsive-artwork-2.0.55 in the development workspace: portrait, landscape, desktop, editor composition previews and simulated standalone landscape. The provided Queen B Halloween screenshot was reviewed as a layout reference; automated visual fixtures use generic corner markers rather than private owner artwork. Both provided church icons were measured locally: the detailed 512-pixel output is 463,578 bytes and the simpler output is 63,220 bytes. Both fit the new 2 MiB limit.

Standalone PWA display mode is simulated in Chromium; actual OS installation, Chrome/Safari rotation, physical iPhone/Android and Windows/Flatpak/Pi installer acceptance still need owner testing after rebuilding. No Mac/Xcode is available. No live installation or database was changed. Existing public 2.0.54 installer releases remain untouched.
