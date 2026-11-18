# Carribrary 2.0.63-preview validation

Completed during source development:

- .NET server/test project compilation with zero warnings or errors.
- Public sharing regression tests, including old settings loading, preserved
  FCC route/DNS choices, generic loopback app routes, duplicate hostnames and
  reserved ports, and the joined public origin with recovery-review protection.
- Official Caddy 2.11.7 validation of combined routes.
- Real temporary local Caddy routing using FCC-generated configuration. Both
  HTTPS hosts responded, Carribrary's private setup/discovery stayed inaccessible
  through the public site, and the original FCC fixture remained reachable.
- FCC source compilation and three installation regression tests.
- Six Carribrary local-builder tests, compiler prerequisite check, dry run, and
  synchronized Help/privacy checks.

The local routing fixture used temporary certificates. It does not prove
public certificate issuance, router setup, or Windows service behavior.
Owner-built EXE, Flatpak and DEB were extracted and audited, then published as v2.0.63-preview. All server versions, Help, license, critical web assets and Flatpak AppStream labels matched. A private-data scan had zero findings for known owner email and common embedded credential patterns. Android packages were excluded from GitHub.

Before public installer publication, verify in a disposable Windows deployment:
1. Upgrade an existing Carribrary-hosted FCC connection without losing its route.
2. Test the inverse setup with FCC already serving HTTPS.
3. Check both hostnames from cellular and after a Windows restart.
4. Confirm current borrower and native companion workflows and backup recovery.
5. Review Firefox/Chrome/mobile layout and the click-through setup on Windows.

The packaging output-directory correction was included from the active build
workspace. Tests verify the builder interface. Final package hashes and embedded
versions require an audit of the finished owner-built files.
