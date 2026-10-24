# Validation — 2026-10-12

Source preview 0.1.3. This is not a signed IPA or a verified iOS build.

Completed on Linux:

- Swift grammar parsing passed for all 9 Swift source/test files using tree-sitter-swift. Grammar parsing is not Swift type checking.
- Xcode project object/reference uniqueness and source membership passed; shared scheme XML, Info.plist, privacy manifest and asset catalog JSON parsed successfully. No arbitrary transport-security exception is configured.
- The existing server built from current source for temporary protocol testing with 0 errors. One NU1900 warning: package vulnerability data could not be refreshed from NuGet because network access was unavailable for that build.
- RSA-2048 and RSA-3072 Apple-style PKCS#1 public-key wrapping produced byte-identical SubjectPublicKeyInfo compared with cryptography’s encoder.
- The live server suite passed with Apple-style public-key wrapping substituted into the pairing proof and isolated ports 4180 / 17443 / 4181. It covered pairing, client certificate authentication/revocation, lending/returns, receipts, history, safe deletion, backup restore, requests, sharing/public privacy, QR link revocation, revision/idempotent retries and CSRF protection. Test data lived only under /tmp, separate from any household library.

An initial run used an old local server build and failed the public sayings projection check. Rebuilding current source and using fresh temporary test data resolved the mismatch; the complete rerun passed.

Not performed:

- Xcode Debug/Release compilation, linking, archiving or signing.
- Execution of the included XCTest tests.
- iPhone Keychain identity lookup, URLSession client-certificate challenge/trust behavior, certificate-expiry failure checks, or camera scanning.
- Real UI, accessibility, rotation, receipts/composer and background/network interruption tests.
- App Store/TestFlight submission or review.

These remaining checks are release gates, detailed in README.md. The live tests verify the server protocol and equivalent key encoding, not the Apple implementation.

Cover fallback update: the iPhone source includes authenticated server cover search and explicit preview/selection. Swift grammar/project checks passed again. Server fallback fixtures and a live Open Library request passed. iOS compilation and device behavior remain unverified.

The ISBN/copy-ID circulation resolver has equivalent shared-fixture checks passing in JavaScript and Android JVM. Matching Swift XCTest cases have been added but remain unexecuted without a Mac. Checkout/return uses eligible physical-copy selection, manual search and explicit confirmation; AVFoundation hardware scanning is still unverified.

Source 0.1.3 includes named collections, matching Google cover attribution and the original 62x30 Google graphic. The current Swift grammar and project checks pass; these changes remain uncompiled and untested on an iPhone. Server and Android/browser behavior are verified separately.

2026-10-15 / 0.1.4: reading tracker and personal link management source added. Ten Swift files passed grammar parsing, project references, plists and asset checks. No Xcode/type checking/device verification available.

2026-10-16 / 0.1.5: Help and account administration source added; eleven Swift files passed grammar/project/plist checks. No Xcode/type checking or device test.

2026-10-19 / 0.1.8: explicit personal TBR filter and Mark as read, retaining existing record details. Thirteen Swift files passed grammar and project checks; no Xcode compilation or device verification available. The authenticated iPhone PWA TBR flow passed automated Chrome tests against a disposable server.

2026-10-20 / 0.1.9: holds, explicit handover, batches, lending limits and protected history/cache controls source added. Fifteen Swift files pass grammar/project/plist checks; native compilation, TLS/device behavior and camera testing still require Mac/Xcode and an iPhone. The iPhone PWA is updated through the server and its authenticated browser regression passes separately.

2026-10-20 / 0.1.10: Carribrary display name, Help and support branding updated while retaining bundle/storage/key identities. Fifteen Swift files pass grammar/project/plist checks. No Xcode compilation or iPhone device testing was performed.
