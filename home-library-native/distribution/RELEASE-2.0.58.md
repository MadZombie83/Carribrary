# Carribrary 2.0.58 preview — workflow confidence

Windows setup streams stage messages into installer details and shows a visible progress window with elapsed time and a local setup-log path. External HTTPS preparation/import helpers have bounded waits and captured output. The ten newest stage logs are retained. Snapshot creation is not force-killed by helper timeouts. Errors use the progress window as their visible owner; recovery behavior remains intact. The reported installation stall remains unconfirmed; this adds visibility and diagnosability rather than claiming its cause was reproduced.

Shared librarian/browser forms and borrower reading dialogs protect unsaved edits on Cancel, Back, navigation and reload. Save operations report progress, temporarily guard navigation and keep entered data on errors. Draft values stay in memory only. Confirmed preset/icon actions are marked saved without an unnecessary discard prompt.

Settings & Tools provides Help with this page in a separate tab, retry on load errors, and a clear-search action for empty results. Filtered catalog/borrower empty states explain how to recover. Circulation wording distinguishes approval/holds from physical handover and final return receipt. Update checks display an in-progress state.

Rebuild packages to use this source; published installers remain 2.0.57. No release installer or APK was built in this workspace. Windows GUI/install acceptance still requires a real Windows test after rebuilding.
