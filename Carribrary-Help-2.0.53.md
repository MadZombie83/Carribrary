# Carribrary help — 2.0.53

## Start here: choose the right guide

Carribrary keeps a library on one computer called the server. That computer must stay on while other people use it.

Librarian: start with Install and open the server, then First administrator sign-in. You can add, lend and return books without a phone, public website, Google key or borrower accounts.

Borrower: ask your librarian for the shared library link or QR code. Start with Browse and request without an account. An account is optional.

Android companion means the librarian's Android app. PWA means a browser app you can put on a Home screen. The iPhone librarian companion and borrower app use the browser.

These instructions describe 2.0.53. New self-checkout, self-return and kiosk settings are OFF until the librarian enables them. Older release notes describe older versions, not today's setup.

## About Carribrary: developer, downloads and license

Carribrary is developed by Chezburber Studios. Public support: chezburbermerch@gmail.com.

The server is free to use. Download its Windows, Linux or Raspberry Pi installer from the official GitHub Releases page: https://github.com/MadZombie83/Carribrary/releases . A public download/listing must be published before these links can serve new users; previews are for invited testing.

The optional Android librarian companion is planned as a one-time US$5 Google Play purchase, with local prices shown by Google Play. Borrowers pay nothing and use their library link in a browser. The server works without the paid companion.

Carribrary-owned code uses the Carribrary Source-Available Proprietary License. Its source can be read and built for your own permitted use; it is not an open-source license or permission to resell/rebrand/distribute Carribrary. Third-party code, licenses and trademarks remain with their owners and are excluded from those restrictions. See LICENSE.txt and THIRD-PARTY-NOTICES.txt in the server installation or source. Developer build and APK-testing instructions are separate from normal setup.

## Install and open the Windows server

Download the Windows installer from the official Carribrary GitHub Releases page: https://github.com/MadZombie83/Carribrary/releases . Choose the release recommended by Chezburber Studios. Preview releases are for testing.

1. On the computer that will keep the library, open Carribrary-Setup-2.0.53-preview.exe.
2. Approve the Windows administrator prompt. Follow the installer and finish.
3. Open Carribrary from the desktop or Start menu.
4. Complete First administrator sign-in. Success means you reach Dashboard.

The local address is http://localhost:4173 on that computer. You do not need Node.js or a separate .NET installation. Keep the computer awake when others need the library. If Windows warns about an unsigned installer, check that you obtained the expected file from the trusted release source before proceeding. Do not delete library data to fix installation trouble.

## Install the Linux Flatpak server

Use the x86_64 Flatpak for a normal Intel/AMD Linux desktop. The ARM64 DEB is for Raspberry Pi, not an Intel/AMD Kubuntu laptop.

1. Open your software manager, such as Discover. Install Flatpak support if it is missing.
2. Open Carribrary-Linux-x86_64-2.0.53-preview.flatpak and choose Install.
3. Launch Carribrary from the application menu. It opens your browser at http://localhost:4173.
4. Complete First administrator sign-in.

If the menu entry is missing, sign out of the desktop and sign in again. Closing the browser does not stop the server. Desktop logout or restart does stop this Flatpak server. Use desktop Autostart if you want it to launch after login; keep the computer awake. Updates keep data unless you explicitly delete application data.

## Install the Raspberry Pi server

Use a Raspberry Pi 4 or 5 with 64-bit Raspberry Pi OS and the ARM64 DEB. A 32-bit OS is not supported. Physical Pi acceptance testing is still required for this preview.

1. Copy Carribrary-Pi-ARM64-2.0.53-preview.deb to the Pi.
2. Open it in the Pi's package installer and follow its prompts.
3. Open Carribrary from the Pi desktop, or open http://localhost:4173 in its browser.
4. Complete First administrator sign-in.

The home-library service normally starts at boot. Keep the Pi powered and connected. A Pi without a desktop needs the advanced SSH instructions in linux/README.md. Do not open the administrator port to the internet. Windows, Flatpak and Pi are alternative server hosts; separate installations do not automatically share one database.

## First administrator sign-in

Carribrary needs its own login even when Windows or Linux is already signed in.

1. Open Carribrary on the server computer.
2. Find the private local-admin-setup.txt file location shown on the setup page. On Windows, Start → Carribrary → Administrator access can copy its setup code after an administrator prompt.
3. Paste the setup code into the page. Choose your username and a password of 12–128 characters. Confirm the password.
4. Save the recovery code somewhere private, away from the server computer.
5. Confirm that you saved it and open the library. You should see Dashboard.

Do not send setup codes, passwords or recovery codes to support. A first visitor cannot become administrator without the private setup code. Borrower passwords and private-reading passphrases are separate.

## Find your way around: Dashboard and Settings & Tools

Dashboard shows library totals, activity and Quick Actions. Catalog holds physical books. Borrowers holds lending contacts. Lending manages holds and batches. Requests handles borrowing requests. Reading tracker is for household reading.

1. Open Settings & Tools when you need a setting or utility.
2. Search by name, or choose Library & Appearance, Books & Organization, Borrowing & Reminders, People & Access, Data & History, or System & Advanced.
3. Open a card. Use its Back link to return.

Android uses Home and a bottom Settings button. The iPhone librarian browser app has similar navigation. Dashboard Quick Actions includes Add Book, holds, batch checkout, sharing, Books to Buy and Backup Now. Analytics has its own page under Data & History. Help in the top banner opens this searchable guide.

## Add another administrator or change a librarian password

Add only people you trust: every dashboard administrator has full management access.

1. Open Settings & Tools → People & Access → Dashboard administrators.
2. Choose Add administrator. Give that person their username, initial password and recovery code privately.
3. Have them sign in using a separate browser profile, or sign out before sharing your browser.

Each administrator has their own recovery code and sessions. Change my password needs your current password. Reset password or Remove administrator ends that person's local sessions. The primary administrator and the account you are using cannot be removed. Up to 100 administrators are supported.

Removing a dashboard account does not remove a borrower or revoke a separately paired phone. Revoke those devices separately.

## Recover an administrator login

Use your saved local administrator recovery code, not a borrower password or Windows password.

1. Open the local sign-in page on the server computer.
2. Choose Recover local administrator access.
3. Enter the requested username/code and new credentials.
4. Save the replacement recovery code before continuing.

Primary-owner recovery ends local dashboard sessions; paired devices remain separate. Local sessions also end after 30 minutes idle, eight hours total, or a server restart. Sign out when finished.

If every recovery code is lost, stop and use the protected stopped-server procedure in ADVANCED-OPERATIONS.md. Do not delete credential files. Windows Administrator access reads an existing setup code; it does not reset your password.

## Start, stop or restart the library server

Windows: open the Services app, find Carribrary (internal name HomeLibraryNative), then choose Start, Stop or Restart. The service normally starts with Windows. Closing the browser or tray does not stop it.

Flatpak: use the Carribrary launcher's Stop library server action. Sign in locally and confirm Stop when asked. Launch Carribrary again to restart it. Closing only the browser leaves it running; desktop logout stops it.

Raspberry Pi: use the desktop service manager for home-library. Advanced terminal commands are in linux/README.md.

Stop the server before updating it. Do not start several copies against one library. If an old manually started server occupies the port, stop that old server rather than deleting data.

## Update safely and keep existing data

1. Make a complete backup and save it off the server. See Create a complete server backup.
2. Keep its password separately. Verify the saved backup.
3. Install the newer server package over the existing installation. Do not uninstall or clear application data first.
4. On Android open Google Play and update Carribrary when an update is available. Do not clear phone storage. Testers should follow their separate test-update instructions.
5. Reopen or refresh browser apps. Check books, borrowers, loans and paired devices.

2.0.53 uses library format 7 and credential format 2. It saves a private pre-upgrade library snapshot. Earlier programs cannot safely open the new library; program-only rollback currently accepts 2.0.53 only. Complete data recovery is separate. There is no automatic update feed. The present feature set is frozen; release work is testing, fixes and clear instructions.

## The three connection addresses

Local administration: http://localhost:4173, opened on the server computer. On a phone, localhost means the phone itself, so this address will not reach your server.

Native Android librarian app: the paired phone address, usually https://SERVER-ADDRESS:7443. The private pairing QR tells the app which server to trust.

Borrowers, iPhone/browser companions and kiosks: the public trusted HTTPS hostname, such as https://mybooks.duckdns.org. Borrowers need the private shared library link to browse. Librarians use /companion/; authorized kiosk stations use /kiosk/.

Never forward administrator port 4173, internal browser port 4174 or discovery port 7445 on your router. Remote setup instructions explain which other ports to forward. A plain public address shows a welcome page and does not reveal the catalog link.

## Pair the optional Android librarian companion

The server works without this app. Borrowers do not need this paid librarian app.

1. Open Google Play and find Carribrary by Chezburber Studios. Purchase/install the optional librarian companion. The public listing is planned; until it is live, only invited testers should use the test instructions supplied by the developer. Do not download an unofficial copy.
2. For a first local test, connect phone and server to the same home network. Avoid guest Wi-Fi that separates devices.
3. On the server, open Setup & updates and note its phone address. For use away from home, finish remote setup first.
4. Open Settings & Tools → People & Access → Android phone pairing. Enter the actual phone address and create the private pairing QR.
5. In Android choose Scan pairing QR, allow the camera, and scan it. Use manual pairing text if needed.
6. Refresh. Success means the phone shows your library and is connected.

Pairing grants librarian access. Never use its QR as the borrower library QR. Revoke a lost phone from Android phone pairing. Normal updates do not require re-pairing.

## Pair the iPhone or browser librarian companion

This is the iPhone librarian browser app. Install it from Safari; you do not need the Android app.

1. Finish public trusted HTTPS setup on the server.
2. On iPhone open https://YOUR-HOST/companion/ in Safari. Use Share → Add to Home Screen and open that icon before pairing when possible.
3. On the server open Settings & Tools → People & Access → iPhone / browser pairing. Create a private pairing code.
4. Enter it in the companion, name the device, and choose Pair this companion.
5. Check that the library opens and Refresh works.

Codes work once within ten minutes. Approval lasts 30 days, unless revoked sooner. Safari and a Home-screen app can use separate browser storage. Revoke a lost browser from the pairing list; Sign out / unpair affects that browser. The borrower sharing QR never grants administration.

## Public access: what DuckDNS and HTTPS do

Public access lets borrowers and companions reach your server away from home. The server must stay on. This is optional for local librarian use.

DuckDNS gives your home a name, such as mybooks.duckdns.org, and keeps it pointing at your internet address. DuckDNS does not install Carribrary or create HTTPS by itself. Seeing http:// on its page is not proof that your Carribrary HTTPS is ready.

1. Visit https://www.duckdns.org/ and sign in using an offered provider.
2. Create an available subdomain. Copy the full hostname and keep your DuckDNS token private.
3. Follow the Windows or Linux public setup topic below.
4. Test from a phone with Wi-Fi OFF before sharing the library link.

Carribrary has an optional built-in updater, so a separate DuckDNS app is not required. A sharing link alone does not set up DNS, certificates or router forwarding.

## Public access on Windows: step by step

1. On the server, open Start → Carribrary → Connection setup.
2. Enter your full hostname, such as mybooks.duckdns.org. Enter your DuckDNS token if Carribrary should keep the address updated. Keep it private.
3. Choose Save remote connection. If you need borrower pages, the iPhone companion or kiosks, also choose Set up public book-list sharing and follow the public HTTPS prompts.
4. Reserve this computer's local address in your router. Add the port-forwarding rules shown by Windows setup: the native Android phone port is normally TCP 7443; Windows public HTTPS normally uses TCP 80 and 443.
5. Allow the matching rules through Windows Firewall if setup requests it.
6. Test the public HTTPS hostname and the paired Android app using mobile data with Wi-Fi OFF.

Never forward 4173, 4174 or 7445. A DNS or local-service check cannot prove outside access. If your router or internet provider blocks incoming connections, ask the network owner for help. Do not add Linux high-port rules to the Windows setup.

## Public sharing setup on Linux, Flatpak and Raspberry Pi

This guided setup is for one server on its own home internet connection. Do not run it alongside another proxy already using the same ports.

1. On the server open Settings & Tools → System & Advanced → Public sharing setup. The application menu also offers Carribrary Public Sharing Setup.
2. Enter your hostname without https://. Enter the DuckDNS token only if Carribrary should update that hostname.
3. Select Enable managed public HTTPS and allow the verified Caddy download. Caddy is the small helper that provides HTTPS. Choose Save setup.
4. Stop and reopen Flatpak, or restart the Pi home-library service. Saving alone does not finish the restart.
5. In your router, reserve the server's local address. Forward external TCP 80 to this computer's TCP 8080, and external TCP 443 to TCP 8443. Allow 8080 and 8443 in a Linux firewall if it is active.
6. Choose Check saved HTTPS address. Then test https://YOUR-HOST from a phone with Wi-Fi OFF.
7. Enable the library sharing link only after that test succeeds.

The public address has no :8443 suffix. Native Android remote access separately uses its configured phone port, normally 7443. Never forward 4173, 4174 or 7445. No separate Caddy package or terminal commands are needed for the guided flow.

## Public connection problems and advanced hosting

If home Wi-Fi works but mobile data fails:

1. Check that the server is on and awake.
2. Check the hostname and DuckDNS updater.
3. Check the router rules for your selected Windows or Linux setup, and the host firewall.
4. Test again with Wi-Fi OFF. Ask your internet provider whether incoming connections are allowed if it still fails.

Fix certificate warnings; do not bypass them. If the managed Caddy download is missing or damaged, allow its download again in Public sharing setup, then restart.

The guided tool does not configure two servers behind an existing proxy or bypass carrier-grade NAT. Existing manually managed proxies need the current Caddyfile example, including /kiosk/ and companion/account routes. ADVANCED-OPERATIONS.md has technical details. Tailscale is not required. Changing the hostname does not automatically change a native phone's saved pairing address.

## Add one book and check its details

1. Open Catalog → Add book.
2. Scan the existing ISBN/EAN barcode or type it. Choose the book-details lookup when offered.
3. Check the title, author, publisher and collection. A lookup can be incomplete or wrong; correct it before saving.
4. Choose a cover if available, then save.
5. Find the new book in Catalog and check its copy number, such as BK-0001.

Each physical copy needs its own catalog record. If a duplicate warning appears, check whether you already own that copy. Confirm an additional copy only when you mean to add another physical book. A title can be entered without an ISBN where the form allows it. Adding a book does not lend it.

## Find a missing cover or add a description

1. Open a book's Edit details / Edit book.
2. Choose Search Books for a cover.
3. Review the suggestions. Another ISBN may mean a different edition or an ebook cover.
4. Choose Use this cover only if it suits your book, then save changes.

Carribrary can try configured Open Library/Internet Archive and optional Google Books sources. Title-and-author alternatives can help when the ISBN has no image. Some books have no available cover. You can keep the placeholder or enter a suitable cover URL manually. Temporary provider errors do not mean your catalog is lost.

About this book shows a short description and its source. Librarians can enter their own description, up to 4,000 characters. Descriptions are public book information, not private reading notes.

## Optional Google Books integration

Google Books is not needed for normal library use. No developer API key is supplied with Carribrary.

1. Open Settings & Tools → Books & Organization → Google Books integration.
2. If you have your own Google Cloud Books API key, enter it and save the setting.
3. Reopen a book and try Search Books for a cover. Read the actual error if the provider rejects the request.

The API must be enabled in the same Google project as the saved key. This server makes the requests, so website-referrer or Android-app key restrictions can block it. Keep the key out of screenshots, public files and source code. Follow Google's current documentation when choosing key restrictions.

Missing results may be retried by ISBN or title/author; no cover is guaranteed. Provider terms and attribution still apply. See ADVANCED-OPERATIONS.md for setup details and current documentation links.

## Camera scanning and manual entry

1. Open the required scan action and allow camera access.
2. Choose the rear camera that gives a sharp picture. Use this camera by default saves the choice where available.
3. Hold the whole barcode flat in steady light. Move farther away if the camera cannot focus. Tap to focus in Android.
4. If scanning fails, close the camera and type the ISBN, copy ID or search text instead.

Browser cameras need trusted HTTPS and browser permission. On iPhone, use Safari or the installed browser app. The bundled ZXing reader handles compatible barcodes when the browser lacks its own reader. A USB scanner types into the focused field; a 1D-only scanner cannot read QR codes. Never let a bad scan replace a careful copy check.

## Batch add books: scan, review and save

Open Settings & Tools → Books & Organization → Batch add / CSV import.

1. Choose the target collection.
2. Queue ISBNs by typing, keyboard scanner or Continuous scan. Move a barcode out of view briefly before scanning another identical copy.
3. Pause if needed. Retry missing details and edit or remove rows. Books without an ISBN still need a title.
4. Choose Review batch. Fix errors and confirm Keep as additional physical copy only for intended duplicates.
5. Check the complete list and choose Save reviewed batch.

A batch can contain up to 1000 rows; the review shows 25 at a time. The save adds the reviewed books together or adds none. Existing books are not replaced. Closing the tool discards the unsaved queue. After an interrupted save, use the offered same-save retry or Android Retry pending change, then inspect the catalog before adding again.

## Import a CSV book list from another app

CSV is a spreadsheet-style book list. This import adds copies; it does not import accounts, loans or private reading.

1. Export CSV or TSV from the other app. Keep a copy of that original file.
2. Open Books & Organization → Batch add / CSV import and choose the file.
3. Check each column mapping: title, author, ISBN, series, publisher, cover, location, source number and notes.
4. Load the mapped rows, then review errors and duplicate warnings.
5. Correct or remove problem rows, review again, and save the batch.

Files must be UTF-8, at most 10 MB and 1000 book rows per batch, with unique headers and matching columns. Comma, semicolon and tab separators are supported. Blank ISBN is allowed with a title. Carribrary assigns new copy numbers; source numbers and extra fields become private import notes. Save the review report if you need help. Split larger files. Do not use Restore library export for CSV.

## Search, collections, sorting and page size

1. Open Catalog and enter a title, author, series, ISBN or copy number in Search.
2. Choose a Collection to narrow the list.
3. Choose Title, Author, Series or Catalog number in Sort by, then choose the order.
4. Choose 10, 25 or 50 books per page. Use Previous/Next for more results.

Android groups these controls under Filter & sort. Signed-in borrowers keep their catalog search/filter/sort controls; their private list may remain locked. Changing filters returns to page one.

To create or rename a collection, open Settings & Tools → Books & Organization → Collections. Collections organize one household library; they do not create separate passwords, borrowers or privacy boundaries. Theme colors belong under Library & Appearance, not Catalog.

## Shelf locations and bulk editing

Librarians can save room, bookcase and shelf in Edit book. These locations and private import notes are not shown to borrowers or the public catalog.

To change several books:
1. In Catalog choose Select books, then select the physical copies. Android uses Bulk edit.
2. Choose Edit selected. Up to 500 copies are supported.
3. Check only the fields you want to change: collection, author, publisher or series.
4. Review and confirm. A checked empty text field clears that field.

Bulk editing does not change titles, ISBNs, covers, copy numbers or loan history. A conflicting change requires another review; the edit does not partly save. Keep private reading notes in the reading list, not public descriptions.

## Copy numbers, permanent QR labels and printing

A BK number is a visible catalog number. The optional copy QR identifies that exact physical record, even if its visible number later changes.

1. Open Books & Organization → Copy numbering / Print copy labels. Android combines these as Copy labels & numbering.
2. Choose Fill available gaps or Always use the next number for future additions. Existing books are not renumbered.
3. To print, select up to 500 books, choose Letter 30-label or A4 24-label sheets, and choose the first unused label position across, then down.
4. Print / Save PDF, or Save label PDF on Android. Print at Actual size / 100%, without browser headers/footers. Test on plain paper first.

History-linked numbers stay protected. A reused BK-only label may point at a replacement copy; remove obsolete labels. A retired permanent QR cannot select the replacement. ISBN/EAN scanning and manual entry still work: custom labels are optional.

## Add a borrower: no account is required

A borrower is a lending contact, not an administrator or a sign-in account.

1. Open Borrowers. Android also lists it under People & Access.
2. Choose Add borrower.
3. Enter the person's name. Phone or email is optional.
4. Record email-reminder permission only if they agreed. Save.

Success means their name appears in the checkout selector. They can borrow with librarian assistance without a phone, email or online account. Edit borrower changes the contact and reminder consent. Deleting a borrower normally moves recoverable records to the Recycle Bin; it does not delete their separate sign-in account. Return active loans and resolve holds before removing a borrower.

## Librarian checkout: ISBN, copy label or manual entry

1. Choose Check out from Catalog or Quick Actions.
2. Scan/type the book's existing ISBN/EAN, optional permanent copy QR or BK number. You can also search by title or author.
3. If several available copies match, choose the physical one in your hands.
4. Select the borrower. A library card can fill the borrower for you; manual lookup remains available.
5. Check the checkout date and optional due date. Confirm only when the borrower receives the book.

Success means the copy appears On loan. Copies already borrowed or held for someone else are not normal checkout choices. Use Requests / Holds for an approved handover. Borrowing limits still apply. Changing Reading tracker status never checks out or returns a physical book.

## Librarian return and physical receipt

1. Receive the physical book.
2. Choose Return / Return book, or Confirm physical receipt on a pending-return card.
3. Scan/type its ISBN/EAN or optional copy label. If several matching copies are checked out, select the correct one.
4. Check the book and borrower, then confirm the return.

The loan is completed and the copy becomes available, unless another rule applies. If there is no matching active loan, check the selected library and copy; do not create another record to fix it.

A borrower or kiosk return report is only Return pending physical receipt. The book stays on loan and unavailable until you complete these librarian steps. Look in active loans or the notification bell for pending returns. Reading completion is separate.

## Send a borrowing receipt by text or email

1. After checkout, open Borrowing receipt. You can also open it from an active loan's book options.
2. Review the borrower, book, due date and contact information.
3. Open the prepared text or email in the device's messaging app.
4. Read it once more, then tap Send yourself.

This is tap-to-send, not an automatic text service. An email address prepares email; a phone number prepares a text where supported. No contact information means there is no ready recipient. Desktop or phone messaging apps must be available. Sending a receipt does not enable automatic reminders or prove that it was delivered.

## Requests, holds and handing over a book

A request reserves one physical copy while the librarian decides. Other copies of the same ISBN remain separate.

1. Open Requests and review the request.
2. Approve to keep the hold, or decline to release that request's hold.
3. Use Lending → Holds to mark it Ready for pickup / delivery if needed.
4. When the borrower actually receives the book, confirm handover with the borrower and dates. Only then does a loan begin.

You can also Create Hold for an existing borrower without a request. Release hold makes it available and declines a linked request. Holds do not expire automatically. Resolve a hold before removing its book or borrower. Guests may request without an account; their request does not create a private reading profile.

## Batch checkout, batch return and lending limits

1. Open Lending → Batch checkout or Batch return. Android: Borrowing & Reminders → Lending & holds / batch lending.
2. Scan ISBN/EAN or copy labels, or search. Choose the physical copy if several match.
3. Review the queue and remove accidental selections.
4. For checkout choose one borrower, dates, and confirm that the books have been received. For returns, confirm the physical books received.
5. Confirm the batch. It saves together or changes none.

The defaults are five books per checkout batch and five total active loans per borrower. Change each under Lending → Lending limits, from 1 to 100. Several batches cannot bypass the active-loan limit. Lowering a limit does not erase existing loans. Returns are not restricted by borrowing limits. Held copies use the Holds / Requests handover flow.

## Borrower renewals and librarian approval

Borrower:
1. Sign in and open My loans.
2. Choose Request renewal on an eligible loan.
3. Review and confirm. The old due date stays until approval.

Librarian:
1. Open Borrowing & Reminders → Borrower renewals, or its notification-bell item.
2. Review the proposed date and approve or decline.
3. Use the policy controls to allow/disable requests or automatic approval.

Defaults are 14 more days and at most two approved extensions. Policies allow 1–90 days and 1–20 extensions. Returned books, missing due dates, pending renewals, exhausted limits or conflicting requests/holds can block renewal. Overdue requests extend from today; others from the current due date. Returning a copy cancels its pending renewal. Guests ask the librarian. The admin notification bell includes pending renewals.

## Library sharing: link, QR code or text list

1. Finish public HTTPS setup and test it from mobile data.
2. Choose Share Library in Dashboard Quick Actions, or Books & Organization → Share the library / QR.
3. Enable sharing and copy the link or show/download its QR.
4. Open that link on another device and check the catalog before giving it to borrowers.

Anyone with the valid link can browse, request and suggest purchases. Treat it as an invitation; do not publish it unless you mean to invite everyone who sees it. Replace or disable the link to stop old links working.

Share a text book list is a separate option for sending the current catalog list. The plain server address shows a welcome page without revealing the catalog token. Borrower details, shelf locations and private reading notes are not in the shared catalog. Administrator pairing is a different private QR.

## Browse and request without an account

1. Open the librarian's shared library link or scan its library QR.
2. Search, filter by collection, sort and choose a page size.
3. Open About this book if you want a description.
4. Choose an available book's request action and enter the requested contact details.
5. Check the title/copy and submit once. Follow the librarian's pickup or delivery arrangements.

You do not need the Android librarian app or an account. On hold and On loan copies cannot be requested as available copies. A request is not checkout: the loan starts at physical handover. If the link is invalid or revoked, ask for a new one. Opening the plain welcome page does not grant catalog access.

## Suggest a book or series for the library to buy

Guests with a valid sharing link and approved accounts can make suggestions.

1. Choose Suggest a book or series to buy near the catalog controls.
2. Choose Book or Series and enter the title. Author, ISBN and a short reason are optional; any supplied ISBN must be valid.
3. Check the suggestion and send it once.

A suggestion does not buy, reserve or add a book, and approval is not guaranteed. It is private to librarians. Do not include contact information or private reading notes in the reason.

Librarians open Quick Actions → Books to Buy → Reader purchase suggestions, or the notification bell. Keep for consideration, decline or delete after review. Up to 500 suggestions are retained; delete reviewed ones to make room. Suggestions are separate from personal wishlists and the librarian's acquisition list.

## Why create a borrower account? It is optional

An account adds your own My loans, requests, renewal options, personal To Be Read (TBR) list, reading tracker and private wishlist. Browsing and requesting through the shared link still work without one.

1. Open the shared catalog and choose Create account / Sign in.
2. Choose Create an account. Enter your name, optional contact, username and a password of 12–128 characters.
3. Complete any displayed registration check, then submit.
4. Wait for librarian approval. Use Check approval when ready.
5. After approval you open Catalog first. My reading is the next main section.

The librarian must link your correct borrower record before you see its history. A matching name or email is not proof of ownership. Your private reading list has a separate setup/passphrase, explained below. Signing in does not grant administration.

## Approve, block, unblock or relink a borrower account

1. Open People & Access → Borrower accounts. Android calls it Borrower accounts & approvals.
2. Verify who is asking before selecting an existing borrower. Create a new borrower only if needed.
3. Approve / activate and ask the borrower to check approval again.

Block ends that account's access but keeps its records. Unblock returns it to pending; approve it again and select the verified borrower. Restore a removed borrower or select a replacement when appropriate. Old revoked sessions/reset links stay revoked.

An existing account keeps its own private profile when correctly relinked. A deleted username registered again is a new account; it cannot inherit the previous encrypted list. Librarians can manage operational loans and accounts, not unlock borrower personal reading. Use Delete account for deletion; Block and Delete borrower are different actions.

## Borrower account: Catalog, My reading, My loans and Account

Catalog opens after sign-in. It keeps search, Collection, Sort by, order and 10/25/50 per page controls. Browsing does not require unlocking your private list.

My reading holds your private TBR and finished reading. My loans shows your own loans, renewals and book requests. Account groups private-list settings, your digital library card, This device, Help, Data & privacy and Sign out.

Use Refresh after the library reconnects. On a shared device, lock your private list and sign out. A pending account can still use its available help, installation, sign-out and deletion controls while awaiting approval. Library loans/requests remain visible to librarians; encrypted personal reading does not. The Android native companion is for librarians, not this borrower account.

## Cancel a borrowing request

1. Sign into your account and open My book requests under My loans.
2. Choose Cancel request for the correct title and copy.
3. Review it, then choose Confirm cancellation. Keep request backs out.
4. Refresh if someone changed the request, then review its current state before trying again.

You can cancel only your own open request. Cancellation releases that request's hold; it does not return a loan, cancel another person's request or erase history. A closed request cannot cancel a later request for the same copy. Guests without an account should ask the librarian to cancel. Clearing history is not needed for ordinary cancellation.

## Borrower password recovery: email or librarian reset link

This resets your sign-in password, not the private-reading passphrase.

1. On sign-in choose Forgot your password? and enter your username.
2. If the librarian enabled email recovery and your account has a usable saved email, check that mailbox and spam folder.
3. Open the password-reset link within 30 minutes and choose a new 12–128 character password.
4. Sign in again. Old sign-in sessions end after a successful reset.

The response does not reveal whether an account exists. Wait at least ten minutes before asking for another email. The newest link replaces the previous one and works once. If email is unavailable, ask the librarian for a one-use password-reset link after identity checking. Old personal records awaiting encryption may require help first. Neither reset method unlocks encrypted reading notes.

## Enable email reminders and optional email password recovery

Use your email provider's SMTP settings, not your normal password if it requires an app password.

1. Open Borrowing & Reminders → Automatic email reminders.
2. Enter the STARTTLS host, username, sending address and app password. STARTTLS commonly uses port 587; implicit TLS/465 and OAuth-only accounts are not supported here.
3. Save and use Send test to sending email. Check that mailbox before enabling automatic sending.
4. Set the reminder timing and enable reminders if wanted.
5. Edit each borrower and record their agreement to receive email. A valid email and due date are needed.
6. Enable the separate borrower email password recovery option only if wanted. It can be on while due-date reminders are off.

The running server checks reminders every 15 minutes after its configured start hour. It can send before due, on due date and at the overdue threshold. Check-delivery means inspect the mailbox before resending. Provider acceptance is not proof of inbox delivery. Text receipts stay tap-to-send.

## Install the borrower browser app: Chrome and iPhone

Use the public HTTPS library link, not a private phone-pairing QR. Borrowers do not need to buy the Android librarian companion.

Android/Chrome:
1. Open the shared link in regular Chrome, not inside a messaging app or Incognito.
2. Use the shared catalog's install control, or sign in and open Account → This device → Install / add to Home screen.
3. Confirm Chrome's Install prompt. If unavailable, use its menu → Add to Home screen → Install where offered.
4. Open the new Home-screen icon and check that it reaches your library.

iPhone:
1. Open the shared link in Safari.
2. Choose Share → Add to Home Screen. Choose Open as Web App if shown, then Add.
3. Open the new icon.

Create shortcut alone is not full app installation. Chrome controls which options appear. The root welcome page has no install button. Installation grants no new access and does not make the private catalog work offline. Sign-in and approval still apply.

## Install or troubleshoot the librarian browser app

1. Open the public HTTPS /companion/ address in regular Chrome or Safari.
2. Use Install / add librarian app where offered; on iPhone use Safari → Share → Add to Home Screen.
3. Open its icon, then follow Pair the iPhone or browser librarian companion.

After pairing, installation controls are under Settings & Tools → People & Access → This companion. Notifications and Sign out / unpair are also separate controls there.

If only a shortcut is offered, check that HTTPS has no certificate warning, that the app is not already installed, and that you are in a supported regular browser. Close old tabs and reopen after an update. Do not clear library or account data just to refresh an icon. Safari and installed-app storage can differ. Pairing is still required; installing alone does not authorize an administrator.

## Optional self-checkout, self-return and kiosk permissions

Only a librarian can enable these trust-based options.

1. Open Borrowing & Reminders → Borrower self-service & kiosks.
2. Choose Allow borrower self-checkout, Allow borrower self-return reports, and/or Enable authorized kiosks. All three start OFF.
3. Set the loan length (1–365 days), kiosk idle timeout (30–300 seconds) and clear physical-return instructions.
4. Save permissions. Refresh open pages to see the buttons.

Checkout and return can be enabled separately. Kiosk enablement alone allows neither transaction. Turning an option off blocks it on the server immediately, even from an already open screen. Normal librarian lending still works. A book barcode or photographed QR cannot prove physical possession; use self-service only when it suits your library.

## Borrow using your phone: Self-checkout and Self-return

You need an approved account linked to your borrower record, and the librarian must enable the action.

1. Sign in and open Catalog.
2. Choose Self-checkout or Self-return above its controls.
3. Scan the book's ISBN/EAN or optional copy QR, or type its ISBN, BK number or title.
4. Choose the right physical copy if several eligible copies match. Check the title and due date.
5. Confirm the checkout or return report, then check My loans.

You do not scan a library card again while signed in. Normal limits and holds apply. You can report only your own active loan as returned. Return pending confirmation means deposit the book as instructed; it stays on loan until a librarian receives it. After a lost connection, check My loans or ask the librarian before repeating an uncertain transaction.

## Library cards: issue, replace, revoke and set a PIN

A physical card uses an existing borrower. No online account, phone or email is required.

1. Open People & Access → Library cards. Android calls it Library cards / PINs and also offers it in Borrowers.
2. Select the borrower.
3. For kiosk use, set a private PIN of 6–12 digits. Give it separately; never write it on the card. A blank optional PIN keeps an existing PIN or leaves a new borrower without one.
4. Choose Issue / replace card and confirm.
5. Check its Active status. Use Set / reset PIN to change verification later.

Revoke stops a lost card immediately; replacement stops its old card too. Neither deletes the borrower or loans. Revoked cards cannot be reactivated. Five wrong PINs lock verification for 15 minutes. The card QR identifies the card; it is not an account login or private-reading key.

## Print library cards or use a digital card

1. Open Library cards and select the active cards to print.
2. Choose one CR80 card, Letter or A4. CR80 is 3.375 × 2.125 inches; sheets hold eight cards.
3. Choose optional cut guides and front/back printing. Duplex uses flip on long edge.
4. Use Preview / Print and choose Save as PDF if wanted. Android uses Save selected cards as PDF.
5. Print at Actual size / 100%. Turn off browser headers/footers and enable background graphics. Test on plain paper before using card stock or laminating.

Cards use the saved library name, icon, colors, tagline and artwork. Very long text has physical space limits; check the preview. Test the QR with a scanner. Approved linked borrowers see the same active digital card under Account. Revoke also stops that digital card. Kiosk PINs never appear on printed or digital cards.

## Use a library card at librarian checkout

1. Open normal Check out.
2. Choose Scan library card, or use the card-identifier field with a USB scanner/manual entry on the browser form.
3. Check that the correct borrower is selected.
4. Scan the book's ISBN/EAN or optional copy label, choose the copy and dates, then confirm handover.

Android has Scan library card too. A forgotten card does not prevent assisted borrowing: select the borrower manually. Card scanning does not bypass loan limits or holds. A QR-capable scanner is required for cards; a 1D-only barcode scanner cannot read a QR. A library card does not sign someone into an online account.

## Authorize a separate kiosk computer

Use a dedicated standard OS account and browser with no librarian or personal account signed in. The kiosk connects to the existing server; do not install a second server.

1. Enable the desired self-service permissions and kiosks in Borrowing & Reminders.
2. In that tool, name the station and create a one-use kiosk code.
3. On the separate computer open https://YOUR-HOST/kiosk/ using trusted HTTPS.
4. Enter the code within ten minutes. This authorizes only that kiosk.
5. Test a card and PIN before leaving it unattended.

Authorization lasts 30 days unless revoked. Private browsing or OS kiosk resets may erase its cookie and require pairing again. Revoke kiosk immediately stops further use. Keep the server reachable. Windows/Linux desktop lockdown is a separate step; Carribrary's page alone cannot lock the operating system.

## Windows 11 Pro or Linux kiosk setup

First test the authorized /kiosk/ page in a dedicated browser account.

Windows 11 Pro:
1. As computer administrator, open Settings → Accounts → Other users → Set up a kiosk → Get started.
2. Create the dedicated kiosk user, choose Microsoft Edge and set the library's HTTPS /kiosk/ address.
3. Open that kiosk session and authorize it there. Do not use librarian login cookies.
4. Test scanner input, timeout, restart and cookie persistence. Ctrl+Alt+Delete exits Assigned Access for administration.

Linux:
1. Create a dedicated standard desktop user and browser profile.
2. Open and authorize the kiosk URL, then use fullscreen.
3. Have the computer owner configure desktop restrictions before unattended use. Fullscreen alone is not lockdown.

Edge kiosk privacy resets can require a new station code. Current Microsoft setup details: https://learn.microsoft.com/en-us/windows/configuration/assigned-access/quickstart-kiosk . Actual Windows/Linux kiosk and scanner acceptance must be tested on the station.

## Use the kiosk, USB scanner or webcam

1. Choose CHECK OUT A BOOK or RETURN A BOOK.
2. Scan/type your library card identifier and enter your private PIN.
3. Scan/type the book's ISBN/EAN or optional copy label. A USB scanner types into the focused field and can send Enter.
4. Choose the physical copy if necessary and confirm.
5. Read the result. For returns, follow the drop-off instructions. Return reports still need librarian physical receipt.

Use camera needs trusted HTTPS, a compatible camera and permission. Manual entry remains available. The screen clears after the receipt, Cancel / Start over, error or idle timeout. HELP explains the flow. If connection fails, do not assume success; reconnect and check with the librarian before repeating an uncertain loan. No offline kiosk loan queue is stored. Traditional assistance remains an option.

## Read privately: TBR, reading tracker and wishlists

TBR means To Be Read. It is a reading plan, not a hold or loan.

1. Sign into your borrower account and open My reading.
2. Set up or unlock the private list. Choose its separate private passphrase and save the recovery code when shown.
3. In Catalog choose Add to TBR / track reading, then choose Want to read or Currently reading.
4. Use Update reading for progress, dates, rating and notes.
5. Choose Mark as read when finished. It leaves TBR but stays in finished history.

Account → Private reading & wishlist has lock/unlock, a private wishlist and personal backup controls. Your wishlist does not send an acquisition request. Browsing and borrowing still work while the private list is locked. Lock it and sign out before sharing a device. Finishing reading does not return a physical book.

## Private-list passphrase, recovery code and backup

Your sign-in password and private-list passphrase do different jobs. The librarian cannot reset the private-list secret.

1. During private-list setup, save the recovery code away from the device.
2. To back it up, unlock the list and choose Export encrypted personal backup. Keep that file and the recovery code privately.
3. If you forget the passphrase, choose Use my recovery code instead and set a new passphrase. The existing recovery code remains valid.
4. To restore a personal backup, unlock the current list, choose its restore control, supply the backup's own passphrase/code and type REPLACE PRIVATE LIST when asked.

If both secrets are lost, old personal contents cannot be recovered. Backups do not contain the recovery code. Reloading or leaving the page locks the list. Conflicting device saves require refresh/unlock. The encrypted list has a 4 MB safeguard. This protects stored personal data, not a compromised device or a server owner who changes delivered web code.

## Household readers and the librarian’s own reading

Household reader profiles are managed by librarians. Borrower account reading is private and separate.

1. Open Reading tracker and choose Add reader for an unlinked household profile.
2. Give it a name, such as the librarian's own name or a household member's name.
3. Select that profile before adding TBR entries or editing reading.

Other approved administrators can see and edit household profiles. For reading private even from librarians, use a separate approved borrower account with its encrypted personal list. Librarians cannot open or edit borrower-private TBR, ratings, quotes or notes through admin tools. They can see operational loans, requests and due dates.

A personal reading QR for an unlinked household profile is a private access link: anyone with it can use that list. Do not confuse it with library sharing or admin pairing. Old borrower personal links do not replace account sign-in.

## Page progress and rereading history

1. In Reading tracker / My reading, edit the book's reading entry.
2. Enter current page and total pages. Use 0 for an unknown total. Whole-page values are limited to 100000; current cannot exceed a known total.
3. Save. A known total shows the completion percentage and progress bar.
4. Finish the read when done. To read it again, open Sessions, quotes & notes → Start a reread.

Start a reread keeps the previous dates, progress, rating and notes, then starts a fresh current session. Do not overwrite the old finished dates to begin again. Up to 1000 previous sessions are supported per entry. Removing the entry also removes its sessions/annotations after confirmation. Reading history and physical loan history remain separate.

## Reading goals, statistics, quotes and page notes

Set a goal:
1. Open Reading tracker / My reading, choose the summary year and Reading goal.
2. Enter 1–10000 books, or 0 to remove that year's goal, and save.
3. Review monthly/yearly totals. Each dated completed session, including rereads, counts separately.

Save a quote or note:
1. Add the book to the reading list, then open Sessions, quotes & notes.
2. Choose Favorite quote or Personal note, enter the text and optional page, then save.

Quotes/notes allow 4000 characters each, up to 1000 per book; page numbers are 1–100000. Book notes stay across rereads. Unknown lengths are not guessed in page statistics. Genres appear where recorded; completion streaks count consecutive finish dates, not daily reading time. Borrower goals, progress and notes stay in their encrypted list, separate from librarian analytics.

## Books to buy: the librarian’s acquisition wishlist

This list is for books the library wants to acquire, not copies already owned or a borrower's private wishlist.

1. Open Dashboard Quick Actions → Books to Buy, or Books & Organization → Books to buy.
2. Scan an ISBN in a store or enter the book details. Save it to the wishlist.
3. When you actually acquire a physical copy, use the purchase/add-copy action.
4. Check the details, collection/location and any duplicate-copy warning, then confirm.

The new copy is added and its acquisition wish is removed together. A failed/conflicting save keeps the wish. Android initially adds acquired copies to General; Edit book can move them. This needs a live server connection; there is no promised offline store queue. Reader purchase suggestions are a separate review section, not automatically approved purchases.

## Administrator notification bell and background alerts

Open the bell to see items needing attention. Choose Review to open the right tool, then resolve the item there. Opening the bell alone does not dismiss it.

It includes account approvals, book/renewal/purchase requests, holds awaiting handover, pending physical returns, overdue loans, restored-access review and backup/message delivery warnings. The badge counts attention items, not just book requests. Refresh after resolving them. If refresh fails, the old list may be stale.

The bell works in the server, paired browser companion and native Android. Background alerts are optional and separate; they do not replace checking the inbox. No personal reading-vault content appears in these summaries. Android needs its matching app update; a server update alone cannot replace native code.

## Optional borrower push reminders

Push means a browser notification, not a text message or email.

1. Sign into an approved borrower account.
2. Open Account → This device and choose Enable due-date notifications.
3. Allow browser/OS permission. On iPhone, first add the borrower app to the Home Screen and open that icon; supported iOS versions are required.
4. Send a test notification if offered. Use Turn off notifications on this device to stop them later.

While the server runs, checks occur each minute. Reminders can occur once up to two days before due, on due date and at least seven days overdue. Returned loans are excluded. Messages are generic; open My loans for details. Sign-out disables that device subscription. Email permission is separate. Focus mode, permissions, outages or an offline server may delay delivery; do not rely on notifications as guaranteed reminders.

## Optional librarian PWA and Android background notifications

iPhone/browser librarian app:
1. Pair the companion and open Settings & Tools → People & Access → This companion.
2. Enable request notifications and allow permission.
3. Test it. Turn it off there or Sign out / unpair to stop this browser's alerts.

These generic alerts cover new account, borrowing, renewal and purchase requests after subscribing. Server checks run about each minute; existing backlog is not re-sent. Review all attention items through the bell.

Native Android:
1. Open Borrowing & Reminders → Request notifications.
2. Enable background admin alerts and grant Android permission.
3. Use Test request notification if wanted.

Android checks about every 15 minutes and may be delayed by power settings or force-stop. Forget connection disables its alerts. PWA Web Push uses browser/OS delivery services; native periodic checks do not need Firebase. No alert contains private reading notes.

## Notification privacy and delivery problems

Push uses the browser/OS service, such as Apple or Google. Carribrary sends generic notifications without book titles, contacts or reading notes. The service can see delivery timing. Server-side subscription/settings secrets are protected; they are not in ordinary library JSON.

1. Check notification permission and Focus / battery settings on the device.
2. Check that the library server is running and reachable.
3. Open Borrowing & Reminders → PWA notification status for browser delivery warnings, or the native Android notification tool for its test.
4. Check the normal bell/My loans even if no notification arrived.

Uncertain deliveries are not blindly repeated. Email reminders use their separate settings. Advanced hosts must allow outgoing HTTPS to push services and retain public account/companion routes; no extra incoming push port is required.

## Library name, icon and encouraging sayings

1. Open Settings & Tools → Library & Appearance → Library identity.
2. Edit the library name, banner subtitle and encouraging saying. Leave a saying blank to hide it.
3. Choose an icon if wanted, preview it and save the identity.
4. Refresh browser pages and Android to see the change.

PNG, JPEG and WebP icons are fitted to square 192- and 512-pixel PNGs. Maximum input is 10 MB. Transparency is preserved where the original supports it; a flattened white background cannot be removed automatically. Use default library icon restores Carribrary branding. Native Android keeps its signed Carribrary launcher icon.

These are public branding fields used by welcome/shared pages, cards and companions. Do not put private information in them. Long sidebar names fit at word boundaries; the logo can move above the name when needed.

## Save a custom theme with its artwork

1. Open Library & Appearance → Theme & artwork. Android opens its theme/custom-theme controls in the same category.
2. Choose a preset, saved theme or New theme.
3. Adjust colors with the picker or six-digit hex value. Use dark text and light backgrounds if the contrast check blocks saving.
4. Choose a sidebar PNG, choose Use current library artwork, or choose No artwork for this theme.
5. Enter a unique name and choose Save & use theme.

New theme clears the draft name and image. It does not change the live library until saved. A saved theme brings back its own colors and artwork; a no-artwork theme clears the previous image. Applying a built-in preset clears artwork. Android preset selection can be local to the phone: Apply selected theme to shared library publishes it. Saving a custom theme publishes its colors/art. Refresh other devices.

## Artwork size and where it appears

Use a portrait PNG designed for the sidebar. Recommended canvas: 512 × 1536 pixels.

1. In Theme & artwork choose the PNG.
2. Review the fitted preview. It is saved with that theme only when you save.
3. Save and refresh the server, borrower pages and companions.

Input must be under 10 MB and at most 8192 pixels per side; the fitted PNG must be at most 2 MB. Older 512 × 256 artwork is accepted but may not suit tall sidebars; a portrait original looks better. Multiple saved images increase backup/cache size.

The desktop sidebar uses a readability layer. Phone/PWA headers crop and tint the same image; Android has no desktop sidebar. Cards may crop it too. Artwork and icons are public branding, not private storage. Remove the image and save to return to a plain theme.

## Library analytics & insights: optional and separate from reading

1. Open Settings & Tools → Data & History → Analytics & insights.
2. Enable collection only if you want it; it starts OFF.
3. Choose 7, 30, 90 days or All time and a chart metric.
4. Pause to stop future collection. Clear requires CLEAR ANALYTICS and does not delete books or reading.

Counts include website/catalog activity, book opens, searches with/without results, requests, lending, returns and catalog growth. Popularity comes from public book opens, not private reading. These are activity counts, not unique people.

No IP, visitor identity, search phrase, contact, secret URL token or encrypted reading data is stored. Browser privacy signals can suppress public counting. Collection is not retroactive. Daily charts keep 730 buckets; all-time totals continue, with bounded popularity storage. Anonymous historical totals are not removed by clearing one person's history. Analytics stays self-hosted and is included in complete backups.

## Backups: choose the right kind

Complete server backup (.carribrary-backup): the recovery choice. It includes the current catalog, loans, borrowers, accounts, encrypted private vaults, server keys/settings, cards/PIN configuration and kiosk metadata. New complete archives are portable and password-encrypted.

Library JSON: an administrative export, not full disaster recovery. It includes library/operational/household data, but not account credentials, private vaults or card/PIN/station credentials. Keep contacts private.

CSV: a book list for spreadsheets or moving book details. It is not a library recovery file.

Encrypted personal backup: each borrower's own private reading/wishlist export. It needs its original private passphrase or recovery code.

Keep a complete backup off the server drive and keep its password separately. Automatic catalog snapshots alone do not protect accounts or private vaults. A complete data backup is not a full computer/OS image.

## Create a complete server backup

1. Open Data & History → Backups. Android: Data & History → Complete server data backup.
2. Choose Create complete backup. Enter and confirm a password of 12–128 characters.
3. Save the .carribrary-backup file and wait until the download finishes. Keep the page/app open.
4. Copy it to another device, drive or your own private storage.
5. Keep the password separately, then verify the saved file.

You do not need to stop the server; it pauses changes briefly to take a consistent copy. Forgotten backup passwords cannot be reset. Use the server computer for large files. The limit is 2 GB of current data and 10,000 files. Existing archives, programs and external OS/proxy configuration are excluded. Borrower reading stays encrypted inside the archive and still needs its own secret. If old private reading needs encryption setup first, ask that borrower to complete it.

## Weekly portable backups: keep four

1. Open Data & History → Portable replacement backup. Android calls it Weekly portable backups.
2. Turn on the seven-day schedule.
3. Enter and confirm its archive password, then save. Blank fields keep an already saved password.
4. Use Create scheduled backup now for the first copy if wanted.
5. Download latest portable backup and move it off the server drive.

The running server checks about every 15 minutes and on startup. It verifies each new archive, keeps four managed completed automatic archives, and removes the oldest only after a successful new copy. Downloaded/manual copies are not deleted. Password changes affect future archives only; keep old passwords for old files.

Same-drive backups do not protect against drive loss. There is no automatic cloud upload or arbitrary backup-folder chooser. Recovery pauses an old schedule until you configure it again.

## Verify a saved complete backup

Verification checks the saved file and password without replacing your live library.

1. Open Data & History → Verify complete backup.
2. Choose the .carribrary-backup file.
3. Enter its backup password and choose Verify saved backup.
4. Keep the page open until the report appears. Check its date, source version and protection type.
5. Keep the original file/password safely off the server.

A wrong password or damaged file is not a pass. Check free space and retry; one verification runs at a time. The server needs room for the upload and temporary unpacked copies. The report does not prove that a replacement computer will work: do a recovery test too. Legacy Windows machine-bound archives need the original computer; new portable archives can move their saved keys. No backup is sent to the developer.

## Complete Backup Recovery: test before restoring

Use the recovery tool on the server computer, not an admin phone.

1. Open Start → Carribrary → Complete Backup Recovery on Windows, or Carribrary Complete Backup Recovery in the Linux/Pi application menu. Approve the required administrator prompt.
2. Choose your complete archive, enter its password and choose Verify backup.
3. Choose Start recovery test.
4. Open the separate test address in InPrivate/Incognito. Sign in with administrator credentials from that backup.
5. Check the RECOVERY TEST banner, books, borrowers and loans. Stop test when finished.

The test is a separate local copy; it does not replace the installed library or send automatic messages. Private reading still needs its separate passphrase/code. A missing menu handler can be bypassed by using the installed application-menu entry. A damaged program may need installer repair first. Do not use a library JSON file in this tool.

## Restore complete data on this or a replacement computer

1. Keep your current complete backup and password off the old computer. Verify and test it first.
2. On a replacement computer install Carribrary 2.0.53 or later compatible software. Open its Complete Backup Recovery tool.
3. Select the portable archive, enter its password, verify it and run the recovery test.
4. Review the warnings. Restoring replaces the current library with the backup date; later changes can be lost.
5. Check the required boxes and type RESTORE LIBRARY exactly, then confirm. The tool retains current data before replacing it.
6. Complete Review restored access before remote use, below.

On Flatpak, confirm Stop on the local dashboard when the tool asks before restoration/restart. Do not run two live servers with the same restored identity. Configure the new computer's network/router/HTTPS separately. Old machine-bound Windows archives cannot be moved after losing their original computer. This is data recovery, not a full OS image.

## Review restored access before remote use

A historical backup can bring back old accounts, passwords and permissions. Remote access stays paused until local review.

1. Open the restored local dashboard and sign in using the administrator credentials in the backup.
2. Review Dashboard administrators, Borrower accounts, Android phone pairing and iPhone / browser pairing. Block/revoke old access again if needed.
3. Open Library cards. Review legitimate restored active cards or issue replacements. Previously revoked cards cannot be reactivated.
4. Reauthorize each kiosk with a fresh station code; restored station credentials are revoked.
5. Choose Record local review in the banner, check the box and type RESUME REMOTE ACCESS.
6. In the recovery tool check review status and choose Restart after local review. Finish / close recovery tool when done.

Configure paused email, push, weekly backup and public HTTPS helpers deliberately. A backup on another computer cannot know about cards lost/revoked after its date. Stop the old server before bringing the replacement online.

## Recovery errors, retained data and storage

If restoration or undo is interrupted:
1. Reopen Complete Backup Recovery.
2. Use Undo interrupted restore if offered, typing UNDO RESTORE when asked.
3. Keep all retained folders and the journal if it still fails. Get help; do not merge individual databases or delete files to force startup.

Recovery keeps previous/failed data privately. Finish / close recovery tool removes normal temporary test/upload files; closing only its browser tab does not necessarily stop the helper. A crash can leave temporary private folders needing support cleanup.

Allow room for the archive, test copy, restored copy and retained current data. Routine catalog backups keep 16 scheduled and seven daily copies; manual/migration safety copies remain separate. Complete archives exclude older archives and external proxy certificates. Keep retained recovery folders private: they may contain deleted historical data and credentials.

## Library JSON restore and catalog CSV export

Export a book list using Data & History → Export catalog CSV. For an administrative library JSON, use Data & History → Backups → Export library JSON. Neither is a complete account/key/vault/card backup.

To restore JSON:
1. Make a complete backup of the current server first.
2. Open Data & History → Restore library export.
3. Choose a Carribrary library JSON, review the replacement warning and confirm as requested.
4. Check books, borrowers and loans after it finishes.

JSON restore replaces administrative records; it is not an add-to-catalog merge and does not import accounts or station/card credentials. Current borrower-private data remains separate. The JSON limit is 10 MB; current server/Android accept supported formats 1–7. Large artwork can exceed that limit; use complete recovery for a whole server. Use CSV import only for adding mapped book rows.

## Windows Update and recovery: program files only

This differs from Complete Backup Recovery. It restores compatible program files while keeping CURRENT data.

1. Open Start → Carribrary → Update and recovery, or its tray entry.
2. Select a compatible recovery point and review it.
3. Choose Restore selected application and follow its checks. The tool checks startup and attempts to put program files back if that fails.

For library format 7, current compatible program recovery is 2.0.53 only. Older recovery points may be retained but cannot safely open current data. Do not use old installers to downgrade it. Program rollback is blocked during restored-access review.

Create recovery point briefly stops the service. Points under ProgramData/HomeLibraryNative/recovery are private, on the same drive, and not automatically rotated. Delete recovery points requires DELETE RECOVERY POINTS and removes rollback ability, not the live library. Complete portable backups are still needed for computer loss.

## Recycle Bin: restore or permanently remove a record

1. Open Data & History → Recycle Bin.
2. Find the deleted book or borrower and choose Restore.
3. Check its restored number and details. If a book number has been reused, restoration chooses a free one instead of overwriting another book.

History-linked records remain archived as needed. Active loans and holds must be resolved before removal. Delete permanently needs confirmation and removes that recovery option; it does not remove records that must remain for protected history. Old records permanently deleted before the Recycle Bin existed cannot be recreated by it.

A permanent copy QR stays with the restored physical record even if its BK number changes. Restored cards and borrower account data have their separate access rules. Use account/data deletion, not just Delete borrower, to remove a sign-in account.

## Clear completed history safely

Borrower private reading:
1. Unlock the private list and choose Clear private finished reading.
2. Review what will be removed and type CLEAR HISTORY.

Returned loans or closed requests:
1. Open Account → Data & privacy → My data & privacy → Clear completed history.
2. Choose the scope, review the preview and use the shown confirmation.

Librarians use Data & History → Clear completed history for operational records and admin-managed household reading. They cannot preview or clear encrypted borrower-private contents through that tool.

Active loans, holds, open requests, TBR and unfinished reading remain protected. Clearing finished sessions can remove old completed rereads while preserving an active reread and its book notes. Account deletion is separate. Backups, sent emails and other devices may retain older information.

## Delete your own borrower account and linked data

Export your encrypted personal backup first if you want to keep it. Both deletion choices remove the current private vault.

1. Sign in and open Account → Data & privacy → My data & privacy → Delete my account.
2. Choose account only, or account and linked personal information.
3. Preview the affected records. Full information deletion requires active loans to be returned first.
4. Enter your current password and the sign-in username shown, not your display/reader name.
5. Confirm the permanent deletion.

Borrower username confirmation accepts capitalization differences, outside spaces and one leading @. Passwords still match exactly. Changing scope clears confirmation and needs a fresh preview.

Account only keeps operational borrower/loan/request records and can be used with books outstanding. Full deletion removes eligible linked information, not catalog books. If you cannot sign in, ask the library owner to verify identity and delete it. Older backups/emails/device copies cannot be remotely recalled.

## Deleting a borrower account and personal information: librarian steps

1. Open People & Access → Borrower accounts → Delete account / personal information.
2. Choose Sign-in account only or Account and linked personal information. Both remove that account's current encrypted private vault.
3. Verify the linked borrower and preview the scope. Do not infer ownership from shared contact details. Full erasure is blocked by active loans.
4. Type the displayed username exactly and choose Delete permanently. If the library changed, preview again.

Account-only deletion keeps operational history; full deletion removes eligible linked borrower/reader records, returned loans, requests and related recovery entries. It also removes obsolete card/PIN registrations. Books stay. Block/Unblock and Delete borrower are not account deletion.

Optional backup cleanup removes ALL stored automatic library backups for the library, not just that person, plus the matching older installation safety files named in the warning. Leave it off unless intended. Administrator-protected recovery points and external copies need separate deletion. After a connection interruption, refresh before repeating deletion.

## Clear device downloads, cache and offline copies

Native Android can show its encrypted cached library while offline. It cannot reliably show current availability until it reconnects.

1. Open the app's data/privacy or download/cache controls and review their scope before clearing.
2. Delete exported PDFs, CSVs or backups separately in the device's Files app if no longer wanted.
3. Refresh after reconnecting; resolve Retry pending change before starting another mutation.

Borrower and librarian browser apps do not keep private catalog/API responses in service-worker offline storage. They need the server. A saved icon is not a full offline library.

Revoking a device stops future server access but does not erase downloaded files or an old offline snapshot. Clearing thumbnails/downloads is not account deletion. Do not clear Android app storage merely to update an icon; it can remove pairing and pending work.

## Troubleshooting and getting help safely

1. Confirm you opened the right server/library and that it is running.
2. On the server try http://localhost:4173. If busy, stop an old server rather than starting more copies.
3. If a phone is offline, check its address, network, pairing and server power. Refresh when connected.
4. If a public link says Not found, check HTTPS setup and ask for the current enabled sharing link.
5. If an approved account has no history, ask the librarian to check its verified borrower link.

Missing covers can be normal; use the cover search rather than deleting books. For lost confirmations, check the actual loan/account before retrying.

Use installed diagnostics if needed. Share the version, exact error and reviewed diagnostic excerpt with support at chezburbermerch@gmail.com. Never send passwords, API keys, DuckDNS tokens, pairing/reset QR links, signing folders or private backups. The developer does not operate your self-hosted library or recover lost encryption secrets.

## Advanced settings and security limits

Most users do not need terminal commands or deployment settings. ADVANCED-OPERATIONS.md covers headless Pi SSH, protected owner recovery, manually managed proxies, storage paths, Google key setup and optional registration protection.

Account registration always needs librarian approval. Rate limits and lockouts can ask shared-network users to wait. Optional Cloudflare Turnstile is configured by the server owner through protected deployment settings; it does not replace approval. Environment-only secrets and external router/proxy configuration must be set up again on a replacement host.

Current data uses library format 7 and credentials format 2. Supported guided complete archives include earlier format-4/5/6 releases and current 2.0.53 format 7; legacy Windows machine-bound archives need their original computer. Do not mistake old release notes or earlier compatibility lists for current recovery instructions. Keep backups, review restored access and ask for help rather than changing raw databases.

## Third-party licenses and credits

Third-party components keep their own licenses. Carribrary restrictions do not limit their rights. These notices can also be found in the installed licenses folder.

Carribrary — third-party notices
Developer: Chezburber Studios. Support: chezburbermerch@gmail.com.

Carribrary-owned code is source-available proprietary software, LICENSE.txt.
That license does not replace, restrict or relicense third-party components.
Each component keeps its original notices, copyrights and license rights.

SERVER AND SELF-CONTAINED RUNTIMES
Microsoft .NET / ASP.NET Core / Windows Forms 10.0.12 and Microsoft.Data.Sqlite /
Windows service dependencies: MIT and their included third-party notices.
Full exact-restored-runtime license/notice files are in licenses/.
https://github.com/dotnet/runtime   https://github.com/dotnet/aspnetcore
SQLitePCLRaw 3.0.5: Apache-2.0, licenses/SQLITEPCLRAW-LICENSE.txt.
https://github.com/ericsink/SQLitePCL.raw
SQLite 3.53.4: public domain, licenses/SQLITE-PUBLIC-DOMAIN.txt.
https://sqlite.org/copyright.html
WebPush 1.0.13: MPL-2.0, unmodified. License and complete covered source are
included in licenses/WEBPUSH-LICENSE.txt and WEBPUSH-SOURCE-1.0.13.zip.
Exact upstream commit and instructions: licenses/WEBPUSH-SOURCE.txt.
You may obtain, modify and redistribute that covered source under MPL-2.0;
Carribrary's proprietary restrictions do not apply to it.
Portable.BouncyCastle 1.9.0: Bouncy Castle/MIT-style license, included in
licenses/BOUNCYCASTLE-LICENSE.html. https://www.bouncycastle.org/

BROWSER
Nayuki QR generator: MIT, original notice kept in qrcodegen.js and
licenses/NAYUKI-QR-LICENSE.txt. https://www.nayuki.io/page/qr-code-generator-library
ZXing Browser 0.2.1: MIT, licenses/ZXING-BROWSER-LICENSE.txt.
ZXing Library: Apache-2.0 and retained third-party license notices,
licenses/ZXING-LIBRARY-LICENSE.txt. https://github.com/zxing-js/library
The decoder includes ts-custom-error: MIT; original notice retained in
licenses/TS-CUSTOM-ERROR-LICENSE.txt and server/wwwroot/vendor/.

ANDROID
Exact 100 resolved release artifacts, versions and license URLs are listed in
licenses/ANDROID-RUNTIME-COMPONENTS.txt and the machine-readable distribution
inventory. Kotlin, coroutines, AndroidX, Compose, CameraX and ZXing use their
listed Apache-2.0 terms. CameraX also declares a libyuv BSD component; its
license is retained in licenses/LIBYUV-LICENSE.txt.
ZXing Core 3.5.3 upstream full license: licenses/ZXING-CORE-LICENSE.txt.
Original notices extracted from the cached release JAR/AAR files are preserved
under licenses/android-embedded-notices/. Common Apache text is also included.
AutoValue and Guava parent POM notice evidence is preserved separately.
Google ML Kit and some Google Android dependencies declare Google-specific
ML Kit / Android SDK terms; they are not blanket-relicensed as Apache or as
Carribrary source. See the per-component list, SDK notices and official terms:
https://developers.google.com/ml-kit/terms
https://developer.android.com/studio/terms
https://developers.google.com/ml-kit/android-data-disclosure

TOOLS, OPTIONAL DOWNLOADS AND PROVIDER MATERIAL
Gradle wrapper: Apache-2.0 with original LICENSE and NOTICE in licenses/.
The Android/JDK/.NET SDK/build caches and private signing files are not source
release assets. Their tool/runtime terms remain separate; verify final payloads.
Optional Caddy 2.11.7 is downloaded separately from official upstream with its
pinned hash after consent. Apache-2.0 LICENSE is retained with the download;
its upstream third-party notices must be checked against the downloaded binary.
Caddy is not bundled in this source archive. https://github.com/caddyserver/caddy
Google Books poweredby.png is bundled unaltered for source attribution. Google
trademarks remain Google's property; provider terms/branding apply separately.
https://developers.google.com/books/branding
Book covers, descriptions and metadata are provider/user material, not owned or
relicensed by Chezburber Studios. Open Library, Internet Archive and optional
Google Books terms apply. Preserve existing source credits and links.

The dependency inventory is an engineering attribution review, not a legal
certification. Reconcile it with each final signed APK/AAB and Windows/Flatpak/DEB
payload before public distribution; do not omit upstream notices supplied by
runtime/native dependencies. Apple native releases remain unverified without Xcode.


Android runtime components — Carribrary 2.0.53

androidx.activity:activity:1.10.1
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.activity:activity-compose:1.10.1
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.activity:activity-ktx:1.10.1
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.annotation:annotation-experimental:1.4.1
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.annotation:annotation-jvm:1.9.1
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.appcompat:appcompat:1.6.1
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.appcompat:appcompat-resources:1.6.1
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.arch.core:core-common:2.2.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.arch.core:core-runtime:2.2.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.autofill:autofill:1.0.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.camera:camera-camera2:1.4.2
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.camera:camera-core:1.4.2
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt
  BSD License — https://chromium.googlesource.com/libyuv/libyuv/+/refs/heads/main/README.chromium

androidx.camera:camera-lifecycle:1.4.2
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.camera:camera-video:1.4.2
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.camera:camera-view:1.4.2
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.collection:collection-jvm:1.5.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.collection:collection-ktx:1.5.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.compose.animation:animation-android:1.8.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.compose.animation:animation-core-android:1.8.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.compose.foundation:foundation-android:1.8.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.compose.foundation:foundation-layout-android:1.8.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.compose.material3:material3-android:1.3.2
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.compose.material:material-icons-core-android:1.7.8
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.compose.material:material-ripple-android:1.8.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.compose.runtime:runtime-android:1.8.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.compose.runtime:runtime-saveable-android:1.8.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.compose.ui:ui-android:1.8.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.compose.ui:ui-geometry-android:1.8.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.compose.ui:ui-graphics-android:1.8.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.compose.ui:ui-text-android:1.8.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.compose.ui:ui-tooling-preview-android:1.8.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.compose.ui:ui-unit-android:1.8.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.compose.ui:ui-util-android:1.8.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.concurrent:concurrent-futures:1.1.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.concurrent:concurrent-futures-ktx:1.1.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.core:core:1.13.1
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.core:core-ktx:1.13.1
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.core:core-viewtree:1.0.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.cursoradapter:cursoradapter:1.0.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.customview:customview:1.0.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.customview:customview-poolingcontainer:1.0.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.drawerlayout:drawerlayout:1.0.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.emoji2:emoji2:1.4.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.emoji2:emoji2-views-helper:1.4.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.exifinterface:exifinterface:1.3.2
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.fragment:fragment:1.3.6
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.graphics:graphics-path:1.0.1
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.interpolator:interpolator:1.0.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.lifecycle:lifecycle-common-java8:2.9.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.lifecycle:lifecycle-common-jvm:2.9.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.lifecycle:lifecycle-livedata:2.9.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.lifecycle:lifecycle-livedata-core:2.9.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.lifecycle:lifecycle-livedata-core-ktx:2.9.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.lifecycle:lifecycle-process:2.9.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.lifecycle:lifecycle-runtime-android:2.9.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.lifecycle:lifecycle-runtime-compose-android:2.9.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.lifecycle:lifecycle-runtime-ktx-android:2.9.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.lifecycle:lifecycle-viewmodel:2.9.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.lifecycle:lifecycle-viewmodel-android:2.9.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.lifecycle:lifecycle-viewmodel-ktx:2.9.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.lifecycle:lifecycle-viewmodel-savedstate-android:2.9.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.loader:loader:1.0.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.profileinstaller:profileinstaller:1.4.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.resourceinspection:resourceinspection-annotation:1.0.1
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.savedstate:savedstate-android:1.3.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.savedstate:savedstate-ktx:1.3.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.startup:startup-runtime:1.1.1
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.tracing:tracing:1.2.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.tracing:tracing-ktx:1.2.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.vectordrawable:vectordrawable:1.1.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.vectordrawable:vectordrawable-animated:1.1.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.versionedparcelable:versionedparcelable:1.1.1
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

androidx.viewpager:viewpager:1.0.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

com.google.android.datatransport:transport-api:2.2.1
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

com.google.android.datatransport:transport-backend-cct:2.3.3
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

com.google.android.datatransport:transport-runtime:2.2.6
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

com.google.android.gms:play-services-base:18.5.0
  Android Software Development Kit License — https://developer.android.com/studio/terms.html

com.google.android.gms:play-services-basement:18.4.0
  Android Software Development Kit License — https://developer.android.com/studio/terms.html

com.google.android.gms:play-services-mlkit-barcode-scanning:18.3.1
  ML Kit Terms of Service — https://developers.google.com/ml-kit/terms

com.google.android.gms:play-services-tasks:18.2.0
  Android Software Development Kit License — https://developer.android.com/studio/terms.html

com.google.android.odml:image:1.0.0-beta1
  Android Software Development Kit License — https://developer.android.com/studio/terms.html

com.google.auto.value:auto-value-annotations:1.6.3
  Apache-2.0 — https://repo.maven.apache.org/maven2/com/google/auto/value/auto-value-annotations/1.6.3/auto-value-annotations-1.6.3.pom

com.google.firebase:firebase-annotations:16.0.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

com.google.firebase:firebase-components:16.1.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

com.google.firebase:firebase-encoders:16.1.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

com.google.firebase:firebase-encoders-json:17.1.0
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

com.google.guava:listenablefuture:1.0
  Apache-2.0 — https://repo.maven.apache.org/maven2/com/google/guava/guava-parent/26.0-android/guava-parent-26.0-android.pom

com.google.mlkit:barcode-scanning:17.3.0
  ML Kit Terms of Service — https://developers.google.com/ml-kit/terms

com.google.mlkit:barcode-scanning-common:17.0.0
  ML Kit Terms of Service — https://developers.google.com/ml-kit/terms

com.google.mlkit:common:18.11.0
  ML Kit Terms of Service — https://developers.google.com/ml-kit/terms

com.google.mlkit:vision-common:17.3.0
  ML Kit Terms of Service — https://developers.google.com/ml-kit/terms

com.google.mlkit:vision-interfaces:16.3.0
  ML Kit Terms of Service — https://developers.google.com/ml-kit/terms

com.google.zxing:core:3.5.3
  Apache-2.0 — https://github.com/zxing/zxing/blob/zxing-3.5.3/LICENSE

javax.inject:javax.inject:1
  The Apache Software License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

org.jetbrains.kotlin:kotlin-stdlib:2.1.20
  The Apache License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt

org.jetbrains.kotlinx:kotlinx-coroutines-android:1.10.2
  Apache-2.0 — https://www.apache.org/licenses/LICENSE-2.0.txt

org.jetbrains.kotlinx:kotlinx-coroutines-core-jvm:1.10.2
  Apache-2.0 — https://www.apache.org/licenses/LICENSE-2.0.txt

org.jetbrains.kotlinx:kotlinx-serialization-core-jvm:1.7.3
  The Apache Software License, Version 2.0 — https://www.apache.org/licenses/LICENSE-2.0.txt

org.jetbrains:annotations:23.0.0
  The Apache Software License, Version 2.0 — https://www.apache.org/licenses/LICENSE-2.0.txt

org.jspecify:jspecify:1.0.0
  The Apache License, Version 2.0 — http://www.apache.org/licenses/LICENSE-2.0.txt



APACHE-2.0.txt

                                 Apache License
                           Version 2.0, January 2004
                        http://www.apache.org/licenses/

   TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

   1. Definitions.

      "License" shall mean the terms and conditions for use, reproduction,
      and distribution as defined by Sections 1 through 9 of this document.

      "Licensor" shall mean the copyright owner or entity authorized by
      the copyright owner that is granting the License.

      "Legal Entity" shall mean the union of the acting entity and all
      other entities that control, are controlled by, or are under common
      control with that entity. For the purposes of this definition,
      "control" means (i) the power, direct or indirect, to cause the
      direction or management of such entity, whether by contract or
      otherwise, or (ii) ownership of fifty percent (50%) or more of the
      outstanding shares, or (iii) beneficial ownership of such entity.

      "You" (or "Your") shall mean an individual or Legal Entity
      exercising permissions granted by this License.

      "Source" form shall mean the preferred form for making modifications,
      including but not limited to software source code, documentation
      source, and configuration files.

      "Object" form shall mean any form resulting from mechanical
      transformation or translation of a Source form, including but
      not limited to compiled object code, generated documentation,
      and conversions to other media types.

      "Work" shall mean the work of authorship, whether in Source or
      Object form, made available under the License, as indicated by a
      copyright notice that is included in or attached to the work
      (an example is provided in the Appendix below).

      "Derivative Works" shall mean any work, whether in Source or Object
      form, that is based on (or derived from) the Work and for which the
      editorial revisions, annotations, elaborations, or other modifications
      represent, as a whole, an original work of authorship. For the purposes
      of this License, Derivative Works shall not include works that remain
      separable from, or merely link (or bind by name) to the interfaces of,
      the Work and Derivative Works thereof.

      "Contribution" shall mean any work of authorship, including
      the original version of the Work and any modifications or additions
      to that Work or Derivative Works thereof, that is intentionally
      submitted to Licensor for inclusion in the Work by the copyright owner
      or by an individual or Legal Entity authorized to submit on behalf of
      the copyright owner. For the purposes of this definition, "submitted"
      means any form of electronic, verbal, or written communication sent
      to the Licensor or its representatives, including but not limited to
      communication on electronic mailing lists, source code control systems,
      and issue tracking systems that are managed by, or on behalf of, the
      Licensor for the purpose of discussing and improving the Work, but
      excluding communication that is conspicuously marked or otherwise
      designated in writing by the copyright owner as "Not a Contribution."

      "Contributor" shall mean Licensor and any individual or Legal Entity
      on behalf of whom a Contribution has been received by Licensor and
      subsequently incorporated within the Work.

   2. Grant of Copyright License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      copyright license to reproduce, prepare Derivative Works of,
      publicly display, publicly perform, sublicense, and distribute the
      Work and such Derivative Works in Source or Object form.

   3. Grant of Patent License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      (except as stated in this section) patent license to make, have made,
      use, offer to sell, sell, import, and otherwise transfer the Work,
      where such license applies only to those patent claims licensable
      by such Contributor that are necessarily infringed by their
      Contribution(s) alone or by combination of their Contribution(s)
      with the Work to which such Contribution(s) was submitted. If You
      institute patent litigation against any entity (including a
      cross-claim or counterclaim in a lawsuit) alleging that the Work
      or a Contribution incorporated within the Work constitutes direct
      or contributory patent infringement, then any patent licenses
      granted to You under this License for that Work shall terminate
      as of the date such litigation is filed.

   4. Redistribution. You may reproduce and distribute copies of the
      Work or Derivative Works thereof in any medium, with or without
      modifications, and in Source or Object form, provided that You
      meet the following conditions:

      (a) You must give any other recipients of the Work or
          Derivative Works a copy of this License; and

      (b) You must cause any modified files to carry prominent notices
          stating that You changed the files; and

      (c) You must retain, in the Source form of any Derivative Works
          that You distribute, all copyright, patent, trademark, and
          attribution notices from the Source form of the Work,
          excluding those notices that do not pertain to any part of
          the Derivative Works; and

      (d) If the Work includes a "NOTICE" text file as part of its
          distribution, then any Derivative Works that You distribute must
          include a readable copy of the attribution notices contained
          within such NOTICE file, excluding those notices that do not
          pertain to any part of the Derivative Works, in at least one
          of the following places: within a NOTICE text file distributed
          as part of the Derivative Works; within the Source form or
          documentation, if provided along with the Derivative Works; or,
          within a display generated by the Derivative Works, if and
          wherever such third-party notices normally appear. The contents
          of the NOTICE file are for informational purposes only and
          do not modify the License. You may add Your own attribution
          notices within Derivative Works that You distribute, alongside
          or as an addendum to the NOTICE text from the Work, provided
          that such additional attribution notices cannot be construed
          as modifying the License.

      You may add Your own copyright statement to Your modifications and
      may provide additional or different license terms and conditions
      for use, reproduction, or distribution of Your modifications, or
      for any such Derivative Works as a whole, provided Your use,
      reproduction, and distribution of the Work otherwise complies with
      the conditions stated in this License.

   5. Submission of Contributions. Unless You explicitly state otherwise,
      any Contribution intentionally submitted for inclusion in the Work
      by You to the Licensor shall be under the terms and conditions of
      this License, without any additional terms or conditions.
      Notwithstanding the above, nothing herein shall supersede or modify
      the terms of any separate license agreement you may have executed
      with Licensor regarding such Contributions.

   6. Trademarks. This License does not grant permission to use the trade
      names, trademarks, service marks, or product names of the Licensor,
      except as required for reasonable and customary use in describing the
      origin of the Work and reproducing the content of the NOTICE file.

   7. Disclaimer of Warranty. Unless required by applicable law or
      agreed to in writing, Licensor provides the Work (and each
      Contributor provides its Contributions) on an "AS IS" BASIS,
      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
      implied, including, without limitation, any warranties or conditions
      of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
      PARTICULAR PURPOSE. You are solely responsible for determining the
      appropriateness of using or redistributing the Work and assume any
      risks associated with Your exercise of permissions under this License.

   8. Limitation of Liability. In no event and under no legal theory,
      whether in tort (including negligence), contract, or otherwise,
      unless required by applicable law (such as deliberate and grossly
      negligent acts) or agreed to in writing, shall any Contributor be
      liable to You for damages, including any direct, indirect, special,
      incidental, or consequential damages of any character arising as a
      result of this License or out of the use or inability to use the
      Work (including but not limited to damages for loss of goodwill,
      work stoppage, computer failure or malfunction, or any and all
      other commercial damages or losses), even if such Contributor
      has been advised of the possibility of such damages.

   9. Accepting Warranty or Additional Liability. While redistributing
      the Work or Derivative Works thereof, You may choose to offer,
      and charge a fee for, acceptance of support, warranty, indemnity,
      or other liability obligations and/or rights consistent with this
      License. However, in accepting such obligations, You may act only
      on Your own behalf and on Your sole responsibility, not on behalf
      of any other Contributor, and only if You agree to indemnify,
      defend, and hold each Contributor harmless for any liability
      incurred by, or claims asserted against, such Contributor by reason
      of your accepting any such warranty or additional liability.

   END OF TERMS AND CONDITIONS


==============================================================================
Licenses for included components:

------------------------------------------------------------------------------
Eclipse Public License 1.0
https://opensource.org/licenses/EPL-1.0

junit:junit
org.sonatype.aether:aether-api
org.sonatype.aether:aether-connector-wagon
org.sonatype.aether:aether-impl
org.sonatype.aether:aether-spi
org.sonatype.aether:aether-util

------------------------------------------------------------------------------
3-Clause BSD
https://opensource.org/licenses/BSD-3-Clause

com.google.code.findbugs:jsr305

org.hamcrest:hamcrest-core
BSD License

Copyright (c) 2000-2015 www.hamcrest.org
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright notice, this list of
conditions and the following disclaimer. Redistributions in binary form must reproduce
the above copyright notice, this list of conditions and the following disclaimer in
the documentation and/or other materials provided with the distribution.

Neither the name of Hamcrest nor the names of its contributors may be used to endorse
or promote products derived from this software without specific prior written
permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY
EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT
SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED
TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR
BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY
WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH
DAMAGE.

com.esotericsoftware.kryo:kryo
com.esotericsoftware.minlog:minlog
Copyright (c) 2008-2018, Nathan Sweet All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
Neither the name of Esoteric Software nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

org.ow2.asm:asm
org.ow2.asm:asm-analysis
org.ow2.asm:asm-commons
org.ow2.asm:asm-tree
org.ow2.asm:asm-util
ASM: a very small and fast Java bytecode manipulation framework
 Copyright (c) 2000-2011 INRIA, France Telecom
 All rights reserved.

 Redistribution and use in source and binary forms, with or without
 modification, are permitted provided that the following conditions
 are met:
 1. Redistributions of source code must retain the above copyright
    notice, this list of conditions and the following disclaimer.
 2. Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in the
    documentation and/or other materials provided with the distribution.
 3. Neither the name of the copyright holders nor the names of its
    contributors may be used to endorse or promote products derived from
    this software without specific prior written permission.

 THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 THE POSSIBILITY OF SUCH DAMAGE.

------------------------------------------------------------------------------
MIT

com.googlecode.plist:dd-plist
dd-plist - An open source library to parse and generate property lists
Copyright (C) 2016 Daniel Dreibrodt

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

org.bouncycastle:bcpg-jdk15on
org.bouncycastle:bcprov-jdk15on
Copyright (c) 2000 - 2019 The Legion of the Bouncy Castle Inc. (https://www.bouncycastle.org)

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

org.slf4j:jcl-over-slf4j
org.slf4j:jul-to-slf4j
org.slf4j:log4j-over-slf4j
org.slf4j:slf4j-api
 Copyright (c) 2004-2017 QOS.ch
 All rights reserved.

 Permission is hereby granted, free  of charge, to any person obtaining
 a  copy  of this  software  and  associated  documentation files  (the
 "Software"), to  deal in  the Software without  restriction, including
 without limitation  the rights to  use, copy, modify,  merge, publish,
 distribute,  sublicense, and/or sell  copies of  the Software,  and to
 permit persons to whom the Software  is furnished to do so, subject to
 the following conditions:

 The  above  copyright  notice  and  this permission  notice  shall  be
 included in all copies or substantial portions of the Software.

 THE  SOFTWARE IS  PROVIDED  "AS  IS", WITHOUT  WARRANTY  OF ANY  KIND,
 EXPRESS OR  IMPLIED, INCLUDING  BUT NOT LIMITED  TO THE  WARRANTIES OF
 MERCHANTABILITY,    FITNESS    FOR    A   PARTICULAR    PURPOSE    AND
 NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 OF CONTRACT, TORT OR OTHERWISE,  ARISING FROM, OUT OF OR IN CONNECTION
 WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


------------------------------------------------------------------------------
CDDL
https://opensource.org/licenses/CDDL-1.0

com.sun.xml.bind:jaxb-impl

------------------------------------------------------------------------------
LGPL 2.1
https://www.gnu.org/licenses/old-licenses/lgpl-2.1.en.html

org.samba.jcifs:jcifs

org.jetbrains.intellij.deps:trove4j

------------------------------------------------------------------------------
License for the GNU Trove library included by the Kotlin embeddable compiler
------------------------------------------------------------------------------
The source code for GNU Trove is licensed under the Lesser GNU Public License (LGPL).

    Copyright (c) 2001, Eric D. Friedman All Rights Reserved. This library is free software; you can redistribute it and/or modify it under
    the terms of the GNU Lesser General Public License as published by the Free Software Foundation; either version 2.1 of the License, or
    (at your option) any later version. This library is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
    even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU Lesser General Public License along with this program; if not, write to the Free Software
    Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.


Two classes (HashFunctions and PrimeFinder) included in Trove are licensed under the following terms:

    Copyright (c) 1999 CERN - European Organization for Nuclear Research. Permission to use, copy, modify, distribute and sell this software
    and its documentation for any purpose is hereby granted without fee, provided that the above copyright notice appear in all copies and
    that both that copyright notice and this permission notice appear in supporting documentation. CERN makes no representations about the
    suitability of this software for any purpose. It is provided "as is" without expressed or implied warranty.

The source code of modified GNU Trove library is available at
    https://github.com/JetBrains/intellij-deps-trove4j (with trove4j_changes.txt describing the changes)

------------------------------------------------------------------------------
Eclipse Distribution License 1.0
https://www.eclipse.org/org/documents/edl-v10.php

org.eclipse.jgit:org.eclipse.jgit

------------------------------------------------------------------------------
BSD-style

com.jcraft:jzlib
com.github.mwiede:jsch

Copyright (c) 2002-2015 Atsuhiko Yamanaka, JCraft,Inc.
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

  1. Redistributions of source code must retain the above copyright notice,
     this list of conditions and the following disclaimer.

  2. Redistributions in binary form must reproduce the above copyright
     notice, this list of conditions and the following disclaimer in
     the documentation and/or other materials provided with the distribution.

  3. The names of the authors may not be used to endorse or promote products
     derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL JCRAFT,
INC. OR ANY CONTRIBUTORS TO THIS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,
INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

------------------------------------------------------------------------------
Eclipse Public License 2.0
https://www.eclipse.org/legal/epl-2.0/

org.junit.platform:junit-platform-launcher

------------------------------------------------------------------------------
Mozilla Public License 2.0
https://www.mozilla.org/en-US/MPL/2.0/

org.mozilla:rhino


NAYUKI-QR-LICENSE.txt
/*
 * QR Code generator library (compiled from TypeScript)
 *
 * Copyright (c) Project Nayuki. (MIT License)
 * https://www.nayuki.io/page/qr-code-generator-library
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of
 * this software and associated documentation files (the "Software"), to deal in
 * the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 * - The above copyright notice and this permission notice shall be included in
 *   all copies or substantial portions of the Software.
 * - The Software is provided "as is", without warranty of any kind, express or
 *   implied, including but not limited to the warranties of merchantability,
 *   fitness for a particular purpose and noninfringement. In no event shall the
 *   authors or copyright holders be liable for any claim, damages or other
 *   liability, whether in an action of contract, tort or otherwise, arising from,
 *   out of or in connection with the Software or the use or other dealings in the
 *   Software.
 */


TS-CUSTOM-ERROR-LICENSE.txt
The MIT License

Copyright (c) 2019 Adrien Gibrat https://github.com/adriengibrat

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.


ZXING-BROWSER-LICENSE.txt
MIT License

Copyright (c) 2018 ZXing for JS

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.


ZXING-CORE-LICENSE.txt
                                 Apache License
                           Version 2.0, January 2004
                        http://www.apache.org/licenses/

   TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

   1. Definitions.

      "License" shall mean the terms and conditions for use, reproduction,
      and distribution as defined by Sections 1 through 9 of this document.

      "Licensor" shall mean the copyright owner or entity authorized by
      the copyright owner that is granting the License.

      "Legal Entity" shall mean the union of the acting entity and all
      other entities that control, are controlled by, or are under common
      control with that entity. For the purposes of this definition,
      "control" means (i) the power, direct or indirect, to cause the
      direction or management of such entity, whether by contract or
      otherwise, or (ii) ownership of fifty percent (50%) or more of the
      outstanding shares, or (iii) beneficial ownership of such entity.

      "You" (or "Your") shall mean an individual or Legal Entity
      exercising permissions granted by this License.

      "Source" form shall mean the preferred form for making modifications,
      including but not limited to software source code, documentation
      source, and configuration files.

      "Object" form shall mean any form resulting from mechanical
      transformation or translation of a Source form, including but
      not limited to compiled object code, generated documentation,
      and conversions to other media types.

      "Work" shall mean the work of authorship, whether in Source or
      Object form, made available under the License, as indicated by a
      copyright notice that is included in or attached to the work
      (an example is provided in the Appendix below).

      "Derivative Works" shall mean any work, whether in Source or Object
      form, that is based on (or derived from) the Work and for which the
      editorial revisions, annotations, elaborations, or other modifications
      represent, as a whole, an original work of authorship. For the purposes
      of this License, Derivative Works shall not include works that remain
      separable from, or merely link (or bind by name) to the interfaces of,
      the Work and Derivative Works thereof.

      "Contribution" shall mean any work of authorship, including
      the original version of the Work and any modifications or additions
      to that Work or Derivative Works thereof, that is intentionally
      submitted to Licensor for inclusion in the Work by the copyright owner
      or by an individual or Legal Entity authorized to submit on behalf of
      the copyright owner. For the purposes of this definition, "submitted"
      means any form of electronic, verbal, or written communication sent
      to the Licensor or its representatives, including but not limited to
      communication on electronic mailing lists, source code control systems,
      and issue tracking systems that are managed by, or on behalf of, the
      Licensor for the purpose of discussing and improving the Work, but
      excluding communication that is conspicuously marked or otherwise
      designated in writing by the copyright owner as "Not a Contribution."

      "Contributor" shall mean Licensor and any individual or Legal Entity
      on behalf of whom a Contribution has been received by Licensor and
      subsequently incorporated within the Work.

   2. Grant of Copyright License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      copyright license to reproduce, prepare Derivative Works of,
      publicly display, publicly perform, sublicense, and distribute the
      Work and such Derivative Works in Source or Object form.

   3. Grant of Patent License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      (except as stated in this section) patent license to make, have made,
      use, offer to sell, sell, import, and otherwise transfer the Work,
      where such license applies only to those patent claims licensable
      by such Contributor that are necessarily infringed by their
      Contribution(s) alone or by combination of their Contribution(s)
      with the Work to which such Contribution(s) was submitted. If You
      institute patent litigation against any entity (including a
      cross-claim or counterclaim in a lawsuit) alleging that the Work
      or a Contribution incorporated within the Work constitutes direct
      or contributory patent infringement, then any patent licenses
      granted to You under this License for that Work shall terminate
      as of the date such litigation is filed.

   4. Redistribution. You may reproduce and distribute copies of the
      Work or Derivative Works thereof in any medium, with or without
      modifications, and in Source or Object form, provided that You
      meet the following conditions:

      (a) You must give any other recipients of the Work or
          Derivative Works a copy of this License; and

      (b) You must cause any modified files to carry prominent notices
          stating that You changed the files; and

      (c) You must retain, in the Source form of any Derivative Works
          that You distribute, all copyright, patent, trademark, and
          attribution notices from the Source form of the Work,
          excluding those notices that do not pertain to any part of
          the Derivative Works; and

      (d) If the Work includes a "NOTICE" text file as part of its
          distribution, then any Derivative Works that You distribute must
          include a readable copy of the attribution notices contained
          within such NOTICE file, excluding those notices that do not
          pertain to any part of the Derivative Works, in at least one
          of the following places: within a NOTICE text file distributed
          as part of the Derivative Works; within the Source form or
          documentation, if provided along with the Derivative Works; or,
          within a display generated by the Derivative Works, if and
          wherever such third-party notices normally appear. The contents
          of the NOTICE file are for informational purposes only and
          do not modify the License. You may add Your own attribution
          notices within Derivative Works that You distribute, alongside
          or as an addendum to the NOTICE text from the Work, provided
          that such additional attribution notices cannot be construed
          as modifying the License.

      You may add Your own copyright statement to Your modifications and
      may provide additional or different license terms and conditions
      for use, reproduction, or distribution of Your modifications, or
      for any such Derivative Works as a whole, provided Your use,
      reproduction, and distribution of the Work otherwise complies with
      the conditions stated in this License.

   5. Submission of Contributions. Unless You explicitly state otherwise,
      any Contribution intentionally submitted for inclusion in the Work
      by You to the Licensor shall be under the terms and conditions of
      this License, without any additional terms or conditions.
      Notwithstanding the above, nothing herein shall supersede or modify
      the terms of any separate license agreement you may have executed
      with Licensor regarding such Contributions.

   6. Trademarks. This License does not grant permission to use the trade
      names, trademarks, service marks, or product names of the Licensor,
      except as required for reasonable and customary use in describing the
      origin of the Work and reproducing the content of the NOTICE file.

   7. Disclaimer of Warranty. Unless required by applicable law or
      agreed to in writing, Licensor provides the Work (and each
      Contributor provides its Contributions) on an "AS IS" BASIS,
      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
      implied, including, without limitation, any warranties or conditions
      of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
      PARTICULAR PURPOSE. You are solely responsible for determining the
      appropriateness of using or redistributing the Work and assume any
      risks associated with Your exercise of permissions under this License.

   8. Limitation of Liability. In no event and under no legal theory,
      whether in tort (including negligence), contract, or otherwise,
      unless required by applicable law (such as deliberate and grossly
      negligent acts) or agreed to in writing, shall any Contributor be
      liable to You for damages, including any direct, indirect, special,
      incidental, or consequential damages of any character arising as a
      result of this License or out of the use or inability to use the
      Work (including but not limited to damages for loss of goodwill,
      work stoppage, computer failure or malfunction, or any and all
      other commercial damages or losses), even if such Contributor
      has been advised of the possibility of such damages.

   9. Accepting Warranty or Additional Liability. While redistributing
      the Work or Derivative Works thereof, You may choose to offer,
      and charge a fee for, acceptance of support, warranty, indemnity,
      or other liability obligations and/or rights consistent with this
      License. However, in accepting such obligations, You may act only
      on Your own behalf and on Your sole responsibility, not on behalf
      of any other Contributor, and only if You agree to indemnify,
      defend, and hold each Contributor harmless for any liability
      incurred by, or claims asserted against, such Contributor by reason
      of your accepting any such warranty or additional liability.

   END OF TERMS AND CONDITIONS

   APPENDIX: How to apply the Apache License to your work.

      To apply the Apache License to your work, attach the following
      boilerplate notice, with the fields enclosed by brackets "[]"
      replaced with your own identifying information. (Don't include
      the brackets!)  The text should be enclosed in the appropriate
      comment syntax for the file format. We also recommend that a
      file or class name and description of purpose be included on the
      same "printed page" as the copyright notice for easier
      identification within third-party archives.

   Copyright [yyyy] [name of copyright owner]

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.

========================================================================
jai-imageio
========================================================================

Copyright (c) 2005 Sun Microsystems, Inc.
Copyright © 2010-2014 University of Manchester
Copyright © 2010-2015 Stian Soiland-Reyes
Copyright © 2015 Peter Hull
All Rights Reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions
are met:

- Redistribution of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistribution in binary form must reproduce the above copyright
  notice, this list of conditions and the following disclaimer in
  the documentation and/or other materials provided with the
  distribution.

Neither the name of Sun Microsystems, Inc. or the names of
contributors may be used to endorse or promote products derived
from this software without specific prior written permission.

This software is provided "AS IS," without a warranty of any
kind. ALL EXPRESS OR IMPLIED CONDITIONS, REPRESENTATIONS AND
WARRANTIES, INCLUDING ANY IMPLIED WARRANTY OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE OR NON-INFRINGEMENT, ARE HEREBY
EXCLUDED. SUN MIDROSYSTEMS, INC. ("SUN") AND ITS LICENSORS SHALL
NOT BE LIABLE FOR ANY DAMAGES SUFFERED BY LICENSEE AS A RESULT OF
USING, MODIFYING OR DISTRIBUTING THIS SOFTWARE OR ITS
DERIVATIVES. IN NO EVENT WILL SUN OR ITS LICENSORS BE LIABLE FOR
ANY LOST REVENUE, PROFIT OR DATA, OR FOR DIRECT, INDIRECT, SPECIAL,
CONSEQUENTIAL, INCIDENTAL OR PUNITIVE DAMAGES, HOWEVER CAUSED AND
REGARDLESS OF THE THEORY OF LIABILITY, ARISING OUT OF THE USE OF OR
INABILITY TO USE THIS SOFTWARE, EVEN IF SUN HAS BEEN ADVISED OF THE
POSSIBILITY OF SUCH DAMAGES.

You acknowledge that this software is not designed or intended for
use in the design, construction, operation or maintenance of any
nuclear facility.

LIBYUV-LICENSE.txt
Copyright 2011 The LibYuv Project Authors. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are
met:

  * Redistributions of source code must retain the above copyright
    notice, this list of conditions and the following disclaimer.

  * Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in
    the documentation and/or other materials provided with the
    distribution.

  * Neither the name of Google nor the names of its contributors may
    be used to endorse or promote products derived from this software
    without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.


AUTO-VALUE-POM-NOTICE.txt
Copyright (C) 2012 Google, Inc.

  Licensed under the Apache License, Version 2.0 (the "License");
  you may not use this file except in compliance with the License.
  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.


809fa1ed2145-LICENSE.txt

                                 Apache License
                           Version 2.0, January 2004
                        http://www.apache.org/licenses/

   TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

   1. Definitions.

      "License" shall mean the terms and conditions for use, reproduction,
      and distribution as defined by Sections 1 through 9 of this document.

      "Licensor" shall mean the copyright owner or entity authorized by
      the copyright owner that is granting the License.

      "Legal Entity" shall mean the union of the acting entity and all
      other entities that control, are controlled by, or are under common
      control with that entity. For the purposes of this definition,
      "control" means (i) the power, direct or indirect, to cause the
      direction or management of such entity, whether by contract or
      otherwise, or (ii) ownership of fifty percent (50%) or more of the
      outstanding shares, or (iii) beneficial ownership of such entity.

      "You" (or "Your") shall mean an individual or Legal Entity
      exercising permissions granted by this License.

      "Source" form shall mean the preferred form for making modifications,
      including but not limited to software source code, documentation
      source, and configuration files.

      "Object" form shall mean any form resulting from mechanical
      transformation or translation of a Source form, including but
      not limited to compiled object code, generated documentation,
      and conversions to other media types.

      "Work" shall mean the work of authorship, whether in Source or
      Object form, made available under the License, as indicated by a
      copyright notice that is included in or attached to the work
      (an example is provided in the Appendix below).

      "Derivative Works" shall mean any work, whether in Source or Object
      form, that is based on (or derived from) the Work and for which the
      editorial revisions, annotations, elaborations, or other modifications
      represent, as a whole, an original work of authorship. For the purposes
      of this License, Derivative Works shall not include works that remain
      separable from, or merely link (or bind by name) to the interfaces of,
      the Work and Derivative Works thereof.

      "Contribution" shall mean any work of authorship, including
      the original version of the Work and any modifications or additions
      to that Work or Derivative Works thereof, that is intentionally
      submitted to Licensor for inclusion in the Work by the copyright owner
      or by an individual or Legal Entity authorized to submit on behalf of
      the copyright owner. For the purposes of this definition, "submitted"
      means any form of electronic, verbal, or written communication sent
      to the Licensor or its representatives, including but not limited to
      communication on electronic mailing lists, source code control systems,
      and issue tracking systems that are managed by, or on behalf of, the
      Licensor for the purpose of discussing and improving the Work, but
      excluding communication that is conspicuously marked or otherwise
      designated in writing by the copyright owner as "Not a Contribution."

      "Contributor" shall mean Licensor and any individual or Legal Entity
      on behalf of whom a Contribution has been received by Licensor and
      subsequently incorporated within the Work.

   2. Grant of Copyright License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      copyright license to reproduce, prepare Derivative Works of,
      publicly display, publicly perform, sublicense, and distribute the
      Work and such Derivative Works in Source or Object form.

   3. Grant of Patent License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      (except as stated in this section) patent license to make, have made,
      use, offer to sell, sell, import, and otherwise transfer the Work,
      where such license applies only to those patent claims licensable
      by such Contributor that are necessarily infringed by their
      Contribution(s) alone or by combination of their Contribution(s)
      with the Work to which such Contribution(s) was submitted. If You
      institute patent litigation against any entity (including a
      cross-claim or counterclaim in a lawsuit) alleging that the Work
      or a Contribution incorporated within the Work constitutes direct
      or contributory patent infringement, then any patent licenses
      granted to You under this License for that Work shall terminate
      as of the date such litigation is filed.

   4. Redistribution. You may reproduce and distribute copies of the
      Work or Derivative Works thereof in any medium, with or without
      modifications, and in Source or Object form, provided that You
      meet the following conditions:

      (a) You must give any other recipients of the Work or
          Derivative Works a copy of this License; and

      (b) You must cause any modified files to carry prominent notices
          stating that You changed the files; and

      (c) You must retain, in the Source form of any Derivative Works
          that You distribute, all copyright, patent, trademark, and
          attribution notices from the Source form of the Work,
          excluding those notices that do not pertain to any part of
          the Derivative Works; and

      (d) If the Work includes a "NOTICE" text file as part of its
          distribution, then any Derivative Works that You distribute must
          include a readable copy of the attribution notices contained
          within such NOTICE file, excluding those notices that do not
          pertain to any part of the Derivative Works, in at least one
          of the following places: within a NOTICE text file distributed
          as part of the Derivative Works; within the Source form or
          documentation, if provided along with the Derivative Works; or,
          within a display generated by the Derivative Works, if and
          wherever such third-party notices normally appear. The contents
          of the NOTICE file are for informational purposes only and
          do not modify the License. You may add Your own attribution
          notices within Derivative Works that You distribute, alongside
          or as an addendum to the NOTICE text from the Work, provided
          that such additional attribution notices cannot be construed
          as modifying the License.

      You may add Your own copyright statement to Your modifications and
      may provide additional or different license terms and conditions
      for use, reproduction, or distribution of Your modifications, or
      for any such Derivative Works as a whole, provided Your use,
      reproduction, and distribution of the Work otherwise complies with
      the conditions stated in this License.

   5. Submission of Contributions. Unless You explicitly state otherwise,
      any Contribution intentionally submitted for inclusion in the Work
      by You to the Licensor shall be under the terms and conditions of
      this License, without any additional terms or conditions.
      Notwithstanding the above, nothing herein shall supersede or modify
      the terms of any separate license agreement you may have executed
      with Licensor regarding such Contributions.

   6. Trademarks. This License does not grant permission to use the trade
      names, trademarks, service marks, or product names of the Licensor,
      except as required for reasonable and customary use in describing the
      origin of the Work and reproducing the content of the NOTICE file.

   7. Disclaimer of Warranty. Unless required by applicable law or
      agreed to in writing, Licensor provides the Work (and each
      Contributor provides its Contributions) on an "AS IS" BASIS,
      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
      implied, including, without limitation, any warranties or conditions
      of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
      PARTICULAR PURPOSE. You are solely responsible for determining the
      appropriateness of using or redistributing the Work and assume any
      risks associated with Your exercise of permissions under this License.

   8. Limitation of Liability. In no event and under no legal theory,
      whether in tort (including negligence), contract, or otherwise,
      unless required by applicable law (such as deliberate and grossly
      negligent acts) or agreed to in writing, shall any Contributor be
      liable to You for damages, including any direct, indirect, special,
      incidental, or consequential damages of any character arising as a
      result of this License or out of the use or inability to use the
      Work (including but not limited to damages for loss of goodwill,
      work stoppage, computer failure or malfunction, or any and all
      other commercial damages or losses), even if such Contributor
      has been advised of the possibility of such damages.

   9. Accepting Warranty or Additional Liability. While redistributing
      the Work or Derivative Works thereof, You may choose to offer,
      and charge a fee for, acceptance of support, warranty, indemnity,
      or other liability obligations and/or rights consistent with this
      License. However, in accepting such obligations, You may act only
      on Your own behalf and on Your sole responsibility, not on behalf
      of any other Contributor, and only if You agree to indemnify,
      defend, and hold each Contributor harmless for any liability
      incurred by, or claims asserted against, such Contributor by reason
      of your accepting any such warranty or additional liability.

   END OF TERMS AND CONDITIONS
