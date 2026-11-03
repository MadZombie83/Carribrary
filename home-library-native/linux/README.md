# Linux and Raspberry Pi — Carribrary 2.0.56

These are current installation instructions. Use [current Help](../../Carribrary-Help-2.0.56.md) for everyday lending and recovery.

## Choose the package

- Intel/AMD Linux desktop, including Kubuntu: `Carribrary-Linux-x86_64-2.0.56-preview.flatpak`.
- Pi 4/5 with 64-bit Raspberry Pi OS: `Carribrary-Pi-ARM64-2.0.56-preview.deb` (ARM64).

Do not use the ARM64 DEB on an Intel/AMD laptop. These are alternative server hosts, not automatically synchronized libraries.

## Flatpak: install and open

1. Install Flatpak support through your software manager if needed.
2. Open the bundle in Discover or the system Flatpak installer and install it.
3. Launch Carribrary from the application menu. Your browser opens `http://localhost:4173`.
4. Follow local administrator setup using the private file/code path shown. Save the recovery code off this computer.
5. Check Dashboard, then test one book, checkout, return and complete backup.

If the menu entry is missing, log out/in once. Closing the browser leaves the server running. The launcher's **Stop library server** action requires local sign-in/confirmation. Desktop logout/reboot stops the Flatpak; add it to desktop Autostart if wanted. Keep the computer awake.

Data: `~/.var/app/com.household.HomeLibrary/data/home-library`. Updates preserve it. Do not select delete application data during uninstall unless you intentionally want to erase it. Browser uploads/downloads handle files; the Flatpak needs no broad host-folder access.

## Pi desktop: install and open

1. Open the ARM64 DEB in the Pi package installer.
2. Launch Carribrary or open `http://localhost:4173` on the Pi.
3. Complete local administrator setup and save the recovery code.
4. Make and verify a complete portable backup.

The `home-library` service normally starts at boot. Program: `/opt/home-library/server`; data: `/var/lib/home-library`; optional service settings: `/etc/home-library/home-library.env`. Removing a package retains data; keep it during updates.

A headless Pi has no local browser. Use the SSH instructions in [ADVANCED-OPERATIONS.md](../ADVANCED-OPERATIONS.md); never expose the administrator port publicly.

## Public sharing: the normal guided setup

1. Create a hostname at [DuckDNS](https://www.duckdns.org/), or use your existing hostname/updater.
2. On this server sign in and open **Settings & Tools → System & Advanced → Public sharing setup**. The application menu also offers **Carribrary Public Sharing Setup**.
3. Enter the hostname without `https://`; optionally enter its private DuckDNS token.
4. Enable managed public HTTPS, allow the verified Caddy download, and save. Caddy is the HTTPS helper; no separate host package is needed.
5. Stop/reopen Flatpak, or restart the Pi service using its desktop service manager.
6. Reserve the server's local network address in the router.
7. Forward external **TCP 80 → this server TCP 8080** and **TCP 443 → this server TCP 8443**. Allow destination 8080/8443 in a host firewall if active.
8. Choose **Check saved HTTPS address**, then test `https://YOUR-HOST` from a phone with Wi-Fi **OFF**.
9. Only then enable the library-sharing link/QR.

The public address has no `:8443` suffix. Native Android separately uses its configured phone port, normally TCP 7443; follow pairing/remote instructions in Help. Never forward 4173, 4174 or 7445. The private Android certificate is trusted through pairing; it is not the browser certificate.

Do not enable this managed proxy beside another proxy using the same ports. It does not configure two servers behind one proxy or bypass internet-provider restrictions. Existing manually managed Caddy deployments use the separate advanced guide and `Caddyfile.example`; their router rules differ. Changing the hostname does not automatically update saved native phone addresses.

## Update and recover

1. Create/verify a complete portable backup and keep it off this drive.
2. Stop the Flatpak, then open the new bundle and update. For Pi install the new DEB over the old package.
3. Keep existing application data. Refresh companions and test access.

Library format is 7, credentials 2. Old packages cannot open upgraded data safely. Complete Backup Recovery is available from the application menu for verification, an isolated test and confirmed data restore. It is separate from installing a prior program version.

After complete restore, review local accounts/devices, review cards and provision kiosks again. Repair the managed Caddy download and check networking as needed; its downloaded program/certificate directory is excluded from complete backups. Saved protected setup remains. Do not run two live servers with one restored identity.

For limited terminal procedures and exact commands, use [the advanced guide](../ADVANCED-OPERATIONS.md). Ordinary borrowers and volunteer librarians do not need those commands for the GUI flows above.
