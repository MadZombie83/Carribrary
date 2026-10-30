# Reviewer access — unresolved production task for 2.0.53

Carribrary requires a paired household server to exercise catalog, borrower, checkout, return, requests and administration. Mark restricted functionality accurately in the App access form.

Current pairing is private, one-time and expiring. It is secure for a household but unsuitable as a standing Google review credential. Do not supply your personal library, household pairing QR or an expiring token as the only review instructions. Do not weaken pairing on household servers to make review work.

Before submitting for review, provide either:
1. A separate persistent review server with fictional data, isolated from your household library, and a reviewed, reusable enrollment path restricted to that server; or
2. A fully functional isolated demonstration library in the app that reviewers can reset and use without special hardware. Clearly identify it as sample data, and exercise core flows rather than showing only static screenshots.

Neither is implemented/deployed by this preparation pass. A server URL or pairing token has not been invented. Once one is available, document exact English steps and validate them from a clean phone off the home network. Allow sufficient reviewer access without waiting for the developer to respond.

Source: https://support.google.com/googleplay/android-developer/answer/15748846

Feature freeze: the options above are review-planning possibilities, not permission to implement new features. Resolve the publishing gate with the owner before adding or changing enrollment behavior. Use a separate test library, never the household database.
