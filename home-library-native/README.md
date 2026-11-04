# Carribrary 2.0.60

Developed by **Chezburber Studios**. Support: **chezburbermerch@gmail.com**.

Carribrary is a self-hosted library for physical books. One Windows, Linux or Raspberry Pi computer keeps the library. Librarians can use its browser dashboard, the Android companion or the iPhone/browser companion. Borrowers can use a shared link without an account. Optional accounts add private reading lists.

## Install Carribrary

Download the free server installer from [official GitHub Releases](https://github.com/MadZombie83/Carribrary/releases). Choose Windows EXE, Linux Flatpak, or Raspberry Pi ARM64 DEB. The optional Android librarian companion has a one-time US$15 base price. Check Google Play and the official release page for availability and local pricing. Borrowers use the browser free. Most users do not need source code or build tools.

Carribrary-owned code is [source-available proprietary software](LICENSE.txt). The server is free to run, including for your household, church or organization. Source viewing/private builds do not grant resale or redistribution rights. Third-party licenses remain separate; see [notices](THIRD-PARTY-NOTICES.txt).

## Choose your next step

- **Installing a package:** [START-HERE.txt](START-HERE.txt), then [INSTALLER-GUIDE.txt](INSTALLER-GUIDE.txt).
- **Everyday use:** [current Help](../Carribrary-Help-2.0.60.md). Inside Carribrary, use Help in the top banner.
- **Linux or Pi:** [linux/README.md](linux/README.md).
- **iPhone:** use the hosted browser companion; see [ios/README.md](ios/README.md).
- **Building source:** [BUILD-LOCALLY.md](BUILD-LOCALLY.md). Source ZIPs do not install the app.
- **Advanced hosting/recovery:** [ADVANCED-OPERATIONS.md](ADVANCED-OPERATIONS.md).
- **Testing this preview:** [release checklist](distribution/RELEASE-2.0.60.md), [validation](distribution/VALIDATION-2.0.60.md), and [implementation report](distribution/SELF-SERVICE-IMPLEMENTATION-REPORT.md).

## Start with the basics

1. Install the matching server package on the computer that will keep the library.
2. Open Carribrary on that computer and create the first administrator login using its private setup code.
3. Save the recovery code away from the computer.
4. In Catalog add one book. In Borrowers add a lending contact if needed.
5. Check out and return that book, then create and verify a complete backup.
6. Add optional phone companions, public sharing or self-service only when you need them.

The local dashboard is `http://localhost:4173` on the server computer. Localhost on a phone means the phone itself. Keep the server running and awake when others need it. You do not need ChatGPT hosting, Tailscale, Node.js, a Google API key or a separate .NET runtime for installed server packages.

## Current release boundaries

Library format is **7**; administrator credentials are format **2**. Make a complete portable backup before upgrading. Earlier server programs cannot preserve the new data. Program-only recovery currently accepts 2.0.60; guided complete data recovery is separate.

Self-checkout, self-return reports and kiosks default OFF independently. A borrower return report leaves the book on loan until a librarian confirms physical receipt. Physical library cards do not require online accounts. Encrypted borrower personal reading is separate from librarian circulation data.

Update Android through Google Play and refresh browser apps after updating the server.

Older release notes and older versioned Help files are historical records, not current instructions. Use the 2.0.60 guide above.

## Developer records

Developer/tester work follows [FEATURE-FREEZE.md](FEATURE-FREEZE.md), [documentation coverage](distribution/DOCUMENTATION-AUDIT-2.0.53.md) and the release validation/checklist. These records are separate from everyday installation and Help.

## 2.0.60 source changes

The illustrated Quick Start release labels now stay synchronized with Help. Windows program-only recovery accepts the documented current 2.0.60 cohort instead of the stale 2.0.53 rule. Regression checks cover version consistency, recovery safeguards and responsive guide layout. Matching 2.0.60 preview installers are published on the official GitHub Releases page.

## 2.0.60 source update

Libraries & network guides separate libraries on one computer or another computer through one public entry. Household readers can install their personal-link PWA. See [release notes](distribution/RELEASE-2.0.60.md) and [validation](distribution/VALIDATION-2.0.60.md), including device acceptance and additional-library recovery limits. No new installers are included in a source download.
