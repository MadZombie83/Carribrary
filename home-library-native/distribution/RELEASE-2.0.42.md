# Carribrary 2.0.42 — matching companion dashboard panels and navigation

Android Home/Catalog and the iPhone/browser companion Home/Catalog now share the dashboard's four semantic summary panels: Physical copies, Available, On loan and Active requests. Phones use two columns; wider layouts use four. The physical-copy panel follows the active palette, with green availability, orange loans and blue requests. Available excludes loans and holds. Active requests includes pending and approved requests; closed requests are excluded. These are whole-library totals, independent of catalog filtering. Offline Android uses its existing cached snapshot, not live counts.

Android adds Home with lending quick actions and people/lending shortcuts; its bottom bar is Home, Catalog, Loans, Reading and Settings. The branded header retains Help and adds an active-request bell. Its rounded search field includes a search icon; typing from Home opens Catalog. Borrowers, history and every Settings tool remain available. The iPhone PWA shows the same five navigation destinations, a visible mobile header search and request bell. Header search submits into Catalog. The server dashboard also gains the functional request bell. Bells open the existing request review workflow; badges are active counts, not unread counts or a new push subscription.

Existing catalog-first companion startup, local Dashboard startup, permissions, pairing, privacy, database formats and lending confirmation flows remain. Library/credential formats stay 3/2. Recovery accepts compatible 2.0.39–2.0.42 applications. Back up before updating, including when moving directly from 2.0.39. Keep Android signing materials private and reuse the same signing identity.

Android versionCode 20042 / 2.0.42-preview; iPhone native source 0.1.26/build 27 with synchronized Help. This change targets native Android and the hosted iPhone PWA, not a verified Xcode build.

Build the server and Android packages from Carribrary-2.0.42-source.zip in its own versioned folder. Refresh/reopen the iPhone PWA after installing the server; install the rebuilt Android update for native changes. No EXE installer, APK/AAB, Flatpak or DEB was produced here. Existing 2.0.39 GitHub release assets are untouched.

## Owner device checks after compiling

- Test the same library on Android, iPhone PWA and server: counts agree after Refresh.
- Create a hold, check out and return a book; available/loan totals update correctly. Approved requests remain active until handover/closure.
- Try Home shortcuts, all five bottom destinations, Borrowers from Home/Settings, header search, the bell, Help and optional push settings.
- Try purple-blue, ocean, green and a saved custom theme; check small phones, rotated screens and large text.
- Verify offline Android clearly shows cached state and disallows writes. Test actual camera/manual barcode flows and existing paired tokens.
- Export a private complete backup and keep the old installer before upgrading directly from 2.0.39. Restore/recovery and physical installers remain separate hardware checks.

## Signed-in borrower catalog parity

Approved borrower accounts and personal reading-link catalogs retain search (title, author, series, ISBN/copy ID and publisher), collection filtering, Title/Author/Series/Catalog-number sorting and 10/25/50 page sizes. Catalog controls use ordinary catalog fields already visible to guest readers; private reading and other people’s records are not exposed. My reading keeps its independent status filter; My loans and Account do not display catalog-only controls. Search/filter/page-size changes reset pagination.
