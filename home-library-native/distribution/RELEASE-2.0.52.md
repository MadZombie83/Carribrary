# Carribrary 2.0.52 preview

Local administrator GUI: Settings & Tools → System & Advanced → Public sharing setup, plus Flatpak/Pi application-menu entries. For one server at a new home using its own DNS account. No second-server routing changes.

Opt-in Caddy2.11.7 download from official release assets, pinned SHA256 verification, safe tar extraction and unprivileged managed process. Optional DuckDNS updater stores token encrypted; no token returned/logged by status. Caddy forwards only the existing public listener and has its administrative API disabled. No native Android installation/pairing/port changes.

Save setup then restart Carribrary. Router rules explicitly externalTCP80→8080 and443→8443. Certificate/identity check plus cellular-test guidance; GUI cannot change arbitrary router/firewall/ISP policies. Flatpak must remain running and awake. Windows retains its existing Connection setup. Existing external proxies remain unchanged. Download/cert folder excluded from complete backups; saved encrypted setup remains, with proxy paused during recovery/review. Repair download after restoration.

Formats6/2 unchanged; recovery47–52. Owner builds packages. No release installers built or neighbor/production configuration changed. Physical Flatpak/Pi router/certificate issuance needs owner validation before general release. See updated Help and Linux README.
