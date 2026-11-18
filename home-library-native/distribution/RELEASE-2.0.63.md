# Carribrary 2.0.63-preview source

This preview adds reciprocal Shared HTTPS setup for applications on the same
Windows computer. The app already running Caddy can retain the hosting role.
Carribrary and Family Command Center can each host the other. Local
administrators can also add other web apps by name, hostname and loopback port.

Existing managed HTTPS settings and the earlier Carribrary-hosted FCC connection
are preserved. Adding a route requires public-access confirmation and combined
Caddy validation. FCC-hosted routing applies after an FCC restart. Joining
Carribrary to another host applies after restarting its Windows service.
DNS names must point to the household public IP. Keep the current host's router
rules. Test both sites over cellular before sharing.

The local packaging launcher accepts a private compiler cache and puts builds in
release/VERSION. Linux packaging now honors the requested output folder too.

Owner-built Windows, Flatpak and Pi installers are published on the [preview release page](https://github.com/MadZombie83/Carribrary/releases/tag/v2.0.63-preview). See [validation](VALIDATION-2.0.63.md)
and [current source Help](../../Carribrary-Help-2.0.63.md). Match the Help version
to the package actually installed.
