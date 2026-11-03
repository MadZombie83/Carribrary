# Carribrary 2.0.55 preview — branding repairs

Detailed library icons now have a 2 MiB limit for each generated PNG instead of 350 KB. Browser upload guidance names accepted formats, dimensions, input limits and output sizes. Failed replacement images cannot save a stale or partial selection. Browser icon dialogs share the same resizing code; Android accepts the matching server limit.

Theme & artwork includes independent portrait and landscape borrower-header settings: Full image (contain), Fill header (cover), horizontal/vertical focus and readability overlay, with composition previews. Full image is the default for existing uploads; unused space uses the theme color. Images retain their proportions, and rotation does not need a page refresh. Existing uploads remain supported; saved themes and backups retain fitting choices. New theme and built-in presets reset fitting and artwork. Older clients that omit header settings preserve them when editing the same saved theme.

The phone artwork recommendation is 512 pixels wide × 1024 pixels high. 900 pixels describes the navigation breakpoint, not upload dimensions. Borrower headers use the mobile override up to 1200 screen pixels, including landscape tablets; wider headers retain main artwork. Navigation drawers keep their existing 900-pixel behavior.

Source-only iteration: rebuild the Windows, Linux, Pi and Android packages to receive these changes. No installer packages were generated for this handoff. No owner artwork or credentials are included in the source.
