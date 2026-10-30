CURRENT REVIEW TARGET: Carribrary 2.0.53. Draft for owner review, not an approved or published store policy/listing. Historical change notes below are not current setup instructions. Public support: chezburbermerch@gmail.com.

# Data safety worksheet — review before submitting

This is an implementation audit and a proposed mapping, not a completed Play Console declaration. Confirm the exact categories in the current form, including the SDK Index entry for the resolved ML Kit version. Do not declare “no data collected” merely because the household hosts its own server.

| Data flow | What the implementation does | Declaration considerations |
|---|---|---|
| Household server | Receives library records, borrower name/contact, loans, requests, device name and pairing identity; returns the library to authorized phones over TLS | Name, email, phone and other user content; device identifiers where appropriate. Evaluate collection under Google's definitions for user-operated servers. Do not omit it without confirming an applicable exception. |
| Local cache | Encrypted library and pending changes; preferences/pairing stored locally; keys in Keystore | Local-only storage alone is not off-device collection. Server transmission is a separate flow. |
| Barcode recognition | Bundled ML Kit processes frames locally | No app-uploaded camera frames. Google SDK diagnostic collection still applies. |
| Google ML Kit | Google documents app/device information, installation identifiers, scanner events and performance/error metrics | Proposed types: device/other IDs, app interactions, diagnostics and other app performance data; purposes analytics and app functionality as supported by SDK disclosure. This is not an optional in-app analytics toggle. Verify before answering required/optional questions. |
| Metadata/covers | Book identifiers/title/author sent to Open Library, Internet Archive or optional Google Books; covers fetched from the selected provider or user-provided HTTPS URL | Requested ISBN/URL and ordinary network metadata go to providers. Clarify user-directed content requests under the form's collection/sharing definitions. |
| Receipts | User opens an external messaging/email app with recipient and receipt | User-initiated sharing; the app has no SEND_SMS permission and sends no message itself. |
| Public list/request form | Catalog exposed when admin enables sharing; requester supplies contact/message to household server | User-directed sharing; requester privacy notice also belongs on the public server form before commercial launch. No private borrower contacts on public catalog. |
| Exports | User chooses destination/app; backups include private records | User-directed transfer. Previously exported files are retained by the chosen destination. |

No advertising SDK or publisher-operated analytics backend was added. ML Kit diagnostics mean this is not an “analytics-free” claim.
Data is transmitted over HTTPS in the audited Android paths; hardware-backed key capability varies by device. Do not claim every phone has hardware-backed storage.
Borrower accounts are created on the user-operated household server; the publisher has no hosted account backend. Native and browser pairing grants full household administration. Review the final account/deletion declaration against this design.
Book/borrower deletion preserves history when required by the library, so it is not complete erasure. The policy describes this distinction. The owner can now delete a sign-in account alone or selected linked personal information, with a preview and active-loan guard; optional cleanup covers installation-managed backups, not prior external exports.
Camera is optional: manual entry remains available. No address-book, location, SMS-send or broad storage access is needed.

References:
- https://support.google.com/googleplay/android-developer/answer/10787469
- https://developers.google.com/ml-kit/android-data-disclosure
- https://support.google.com/googleplay/android-developer/answer/10144311

Additional current data flows to audit:
- Optional native Android notification permission and persisted periodic job; request-ID hashes stay in app-private preferences for deduplication. The job contacts only the paired household server, about every 15 minutes subject to Android scheduling. It does not use Firebase messaging or send private book/contact data in notifications. Disable/Forget connection cancels it.
- Icons and manual descriptions are public library catalog/branding fields; image selection uses the system document picker, without broad photo/storage access. The app fits an image locally before upload.
- On-demand About metadata may query Open Library and configured Google Books with book identifiers; it does not share reading notes.
- Borrower and iPhone PWAs use separate opt-in Web Push and browser permissions. Endpoints, client keys and VAPID keys are protected on the household server. Push services receive endpoint/timing, with empty messages and generic local alert text; native Android does not use these services for its periodic alerts.
- Borrower accounts, personal reading notes and browser admin credentials have their own privacy/access boundaries. Account deletion removes push subscriptions immediately in the management API; external exports, already-delivered emails and other phones’ existing caches require separate cleanup.

Borrower privacy: borrower personal reading/TBR, notes, ratings and private
wishlists are browser-encrypted with a separate passphrase/recovery code. Native
admin clients do not receive them. Normal library exports omit borrower personal
records/vaults; borrowers can export an encrypted personal backup. Both account
deletion scopes remove its vault. Prior backups/caches cannot be recalled. A host
actively modifying delivered browser code is outside this protection. Household
reading remains admin-managed; operational lending is still visible to admins.

Current self-service review: cards/PIN verifiers, station tokens, pending physical-return records and security attempt counters stay on the household server. Card PDFs are administrator-directed exports. Also review optional SMTP password-reset emails, anonymous purchase suggestions and aggregate self-hosted analytics against the current policy draft. No new publisher backend is introduced.
