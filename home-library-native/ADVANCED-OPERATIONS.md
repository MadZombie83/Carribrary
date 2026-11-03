# Advanced operations — Carribrary 2.0.59

For a server owner or technical helper. Ordinary librarian tasks use Help in the running app and its GUI. Features are frozen. Commands here are not invitations to delete or merge databases.

## Data and compatibility

Current library format is **7** and administrator credential format **2**. Program-only rollback currently permits 2.0.59; older programs reject or discard new fields. Guided Complete Backup Recovery accepts supported format-4 sources 2.0.43/44, format-5 2.0.45/46, format-6 2.0.47–52, and format-7 2.0.59, subject to platform/key protection. Prefer a verified portable complete archive made by the current version.

New complete archives are portable format 2. Legacy format-1 Windows archives remain tied to the original Windows machine. Borrower vaults require their own private passphrase/recovery code even inside a complete backup. A private archive contains server identity/key material; do not share it or send it to support. Protect the password separately.

| Host | Program / service | Private data |
|---|---|---|
| Windows standard install | Registered folder; service HomeLibraryNative, display Carribrary | ProgramData/HomeLibraryNative, including data/key material |
| Flatpak | com.household.HomeLibrary, stable | ~/.var/app/com.household.HomeLibrary/data/home-library |
| Pi | /opt/home-library/server; service home-library | /var/lib/home-library; optional /etc/home-library/home-library.env |

Use Setup & updates for actual paths on a customized installation. Do not casually copy live SQLite/WAL files. The GUI complete-backup tool makes consistent snapshots without stopping the library. Library JSON excludes accounts, vaults, card/PIN/station registry and other protected host configuration. CSV is only a catalog list.

Routine library snapshots retain 16 scheduled/seven daily files. Managed weekly portable archives retain four verified copies. Manual/migration snapshots, retained complete-recovery folders and Windows recovery points are separate and not automatically pruned. Keep off-device copies; same-drive archives do not protect drive loss.

## Local owner recovery if every administrator code is lost

Use saved recovery codes in the GUI first. Windows Administrator access only reads an existing setup code. Do not delete credential files.

1. Make a protected backup of the complete current server data; retain the working program/configuration.
2. Stop the correct Carribrary service/Flatpak and confirm that its data folder is not in use.
3. Run the matching installed server executable with `--prepare-local-admin-recovery`, using the same protected data-directory configuration as the service.
4. Keep the new one-time code private. Restart that server and use local administrator recovery.
5. Save the replacement recovery code and verify access.

Custom paths/service accounts require checking the actual installation configuration first. An authorized companion can request protected recovery preparation through the server API, but there is no promised dedicated recovery button in each companion. Never expose a recovery port or code publicly.

## Pi terminal installation and service controls

Only for Pi ARM64, not an Intel/AMD laptop:

```sh
sudo apt install ./Carribrary-Pi-ARM64-2.0.59-preview.deb
sudo systemctl status home-library
sudo systemctl restart home-library
```

`stop` / `start` can replace `restart`. Read errors with `journalctl -u home-library -n 50 --no-pager`; review output for private information before sharing it.

For a headless Pi administrator dashboard, enable SSH deliberately and use a local tunnel from a trusted desktop:

```sh
ssh -L 4173:127.0.0.1:4173 YOUR_PI_USER@YOUR_PI_ADDRESS
```

Keep that terminal open and visit `http://localhost:4173` on the desktop. Its local port 4173 must be free. The tunnel does not remove Carribrary login. Never forward the administrator port through the router.

## Headless Pi complete recovery

The graphical Pi launcher is simpler when a desktop exists.

1. Over SSH run `sudo home-library-recovery` on the Pi. It prints a private recovery URL and temporary port/token.
2. From another trusted desktop open a second SSH connection: `ssh -L PORT:127.0.0.1:PORT YOUR_PI_USER@YOUR_PI_ADDRESS`, replacing both PORT entries with the printed port.
3. Keep both connections open. Open the printed localhost recovery URL in the desktop browser.
4. Select the archive through that browser and follow Verify → recovery test → confirmed restore → local access review.
5. Finish / close the helper and close the tunnel.

The packaged Pi recovery flow supports `/var/lib/home-library`; custom service paths/ownership need supervised recovery. Do not bind the helper publicly or forward its port on the router. Closing a tab is not the same as stopping the helper.

## Offline complete-backup utilities

The normal independent Complete Backup Recovery GUI is preferred, even when the dashboard cannot open. If the executable works, it also supports `--verify-complete-backup ARCHIVE` (no live restore) and `--unpack-complete-backup ARCHIVE NEW-FOLDER` (new destination only). Password entry is private; never put the password in a command argument.

The unpack command does not install a server or replace live data. Follow its recovered `RECOVERY-READ-ME.txt`; keep the full database/keyring set together. Custom platform paths and Flatpak file grants need careful owner review. Do not manually merge individual databases or use an older executable on new data.

After any historical restore, review account/device access locally before reopening remote connections. Restored cards require review; station credentials are revoked. Known local card-revocation hashes merge during recovery. Another machine cannot infer revocations that happened after the archive date; review/replace questionable cards. Pause the original server before bringing a restored identity online. Reconfigure external router, certificates, service environment and paused message/backup schedules separately.

## Manually managed Caddy / existing proxy deployments

Do not run a second proxy beside Carribrary's managed Linux HTTPS helper on the same ports.

The normal managed Linux GUI forwards external TCP 80→host8080 and 443→host8443. A separately installed Caddy normally uses host80/443 instead. Windows Connection setup has its own configuration; do not mix these rule sets.

For an existing externally managed deployment, use `linux/Caddyfile.example` from the source bundle, replace the hostname and proxy only the loopback **public** listener, normally 4174. Set `Library__PublicCatalogOrigin=https://YOUR-HOST` in the actual service environment and restart. A Flatpak external-proxy deployment can use:

```sh
flatpak override --user --env=Library__PublicCatalogOrigin=https://YOUR-HOST com.household.HomeLibrary
```

Stop/reopen it after the override. Carribrary limits public routes itself. Restrictive old proxy rules must retain the welcome root, `/s/*`, `/reading/*`, `/account/*`, `/companion/*`, `/kiosk/*` and `/robots.txt` as needed; prefer the supplied whole-public-listener configuration so future public assets are not silently blocked. Never proxy or forward administrator4173. Do not expose SQLite files or recovery helpers. Native Android uses its own paired TLS endpoint, normally7443; UDP7445 discovery stays on the local network.

Changing DNS does not route two libraries by itself. Two-server hostname routing behind an existing proxy is a separate, manually managed deployment, not a supported automatic GUI path. Carrier-grade NAT or blocked inbound ports may need provider/network-owner assistance.

Official references: [Caddy installation](https://caddyserver.com/docs/install), [DuckDNS instructions](https://www.duckdns.org/install.jsp), [DuckDNS update specification](https://www.duckdns.org/spec.jsp).

## Optional Google Books key

In Google Cloud select the intended project, enable Books API, create your own key and enter it through **Books & Organization → Google Books integration**. Carribrary makes server-side requests. Website-referrer or Android package restrictions do not describe that server and can reject its calls; IP restrictions must match actual outbound public addresses. Prefer an API restriction to Books API. Check the current official console guidance rather than making a key public or embedding it in source. The saved key is not included in ordinary library JSON.

The server uses Google's `key` parameter; credentials do not belong in browser URLs/logs sent to support. Provider outages, quota limits or absent image metadata can still prevent covers. Alternative title/author results can be another edition; the librarian must review them. Optional integration does not certify commercial rights or replace source attribution/provider-term review.

Official references: [Books getting started](https://developers.google.com/books/docs/v1/getting_started), [API key restrictions](https://cloud.google.com/api-keys/docs/add-restrictions-api-keys), [Books terms](https://developers.google.com/books/terms).

## Optional registration challenge

Built-in approval and per-IP budgets work without a challenge provider: registration5/15min, sign-in20/15min, reset submission10/15min; librarian reset issuance6/minute. Local account lockouts remain separate. Shared networks can share a budget.

If needed, create a Cloudflare Turnstile widget for the exact public hostname and configure `BorrowerSecurity__TurnstileSiteKey` and `BorrowerSecurity__TurnstileSecretKey` in the service's protected environment (or the corresponding .NET configuration keys). Restart. Both blank leaves it off. Partial/bad configuration or failed provider verification blocks registration rather than bypassing it. Existing sign-in/admin flows do not depend on Turnstile.

Site key is public; secret is not. Do not put secrets in source, app screenshots or public files. Environment-only settings must be recreated on a replacement host. Consult [Cloudflare's current setup](https://developers.cloudflare.com/turnstile/get-started/).

## Notifications, privacy and limits

Email uses STARTTLS (commonly587), not implicit465/OAuth-only transport. Permission is per borrower; reset email is a separate opt-in. Browser Web Push needs outbound HTTPS to its browser/OS delivery service; no extra incoming port. Native Android checks periodically instead of using Firebase. The normal bell remains the authoritative attention list, including pending physical returns and account/renewal/purchase requests.

Personal vault size is capped at4MB; reading entries support1000 previous sessions and1000 annotations per book. Page numbers max100000; reading goals0–10000 for years1900–2200. Complete data backups cap2GB/10000files; JSON/CSV uploads cap10MB; CSV/batch review caps1000rows and printing/bulk edits500items. Analytics stores730 daily buckets, with bounded distinct-book popularity counts (1000/day,10000all-time). These safeguards are not arbitrary subscription tiers.

Account information deletion removes current owned data and obsolete card/PIN registrations. Revocation tombstones can remain anonymous. Older backups, exported files, sent messages, recovery points and other devices may retain historical records. A vault protects stored personal contents from normal admin access; a malicious server owner could change browser code to capture future secrets. Do not promise forensic erasure, remote wiping, independently audited security or guaranteed message delivery.
