# Carribrary 2.0.53

Carribrary is a self-hosted library for physical books. One Windows, Linux or Raspberry Pi computer keeps the library. Librarians can use its browser dashboard, the Android companion or the iPhone/browser companion. Borrowers can use a shared link without an account. Optional accounts add private reading lists.

[Documentation audit and coverage](distribution/DOCUMENTATION-AUDIT-2.0.53.md).

**Feature freeze:** no new features are being added. Current work is documentation, testing, security, compatibility and bug fixes. See [FEATURE-FREEZE.md](FEATURE-FREEZE.md).

## Choose your next step

- **Installing a package:** [START-HERE.txt](START-HERE.txt), then [INSTALLER-GUIDE.txt](INSTALLER-GUIDE.txt).
- **Everyday use:** [current Help](../Carribrary-Help-2.0.53.md). Inside Carribrary, use Help in the top banner.
- **Linux or Pi:** [linux/README.md](linux/README.md).
- **iPhone:** use the hosted browser companion; see [ios/README.md](ios/README.md).
- **Building source:** [BUILD-LOCALLY.md](BUILD-LOCALLY.md). Source ZIPs do not install the app.
- **Advanced hosting/recovery:** [ADVANCED-OPERATIONS.md](ADVANCED-OPERATIONS.md).
- **Testing this preview:** [release checklist](distribution/RELEASE-2.0.53.md), [validation](distribution/VALIDATION-2.0.53.md), and [implementation report](distribution/SELF-SERVICE-IMPLEMENTATION-REPORT.md).

## Start with the basics

1. Install the matching server package on the computer that will keep the library.
2. Open Carribrary on that computer and create the first administrator login using its private setup code.
3. Save the recovery code away from the computer.
4. In Catalog add one book. In Borrowers add a lending contact if needed.
5. Check out and return that book, then create and verify a complete backup.
6. Add optional phone companions, public sharing or self-service only when you need them.

The local dashboard is `http://localhost:4173` on the server computer. Localhost on a phone means the phone itself. Keep the server running and awake when others need it. You do not need ChatGPT hosting, Tailscale, Node.js, a Google API key or a separate .NET runtime for installed server packages.

## Current release boundaries

Library format is **7**; administrator credentials are format **2**. Make a complete portable backup before upgrading. Earlier server programs cannot preserve the new data. Program-only recovery currently accepts 2.0.53; guided complete data recovery is separate.

Self-checkout, self-return reports and kiosks default OFF independently. A borrower return report leaves the book on loan until a librarian confirms physical receipt. Physical library cards do not require online accounts. Encrypted borrower personal reading is separate from librarian circulation data.

Server packages and the Android app must be built/updated together. Browser apps receive changes after refresh. Physical Windows/Edge, phone-camera, scanner, card-printing and Pi acceptance checks remain before public release. No public-store readiness is implied by a source compile.

Older release notes and older versioned Help files are historical records, not current instructions. Use the 2.0.53 guide above.
