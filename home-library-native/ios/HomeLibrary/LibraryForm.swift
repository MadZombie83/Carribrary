import SwiftUI

struct LibraryForm: View {
    @EnvironmentObject var store: LibraryStore; @Environment(\.dismiss) var dismiss
    @AppStorage("collection") var selectedCollection = ""
    @State private var collectionId = "general"
    let route: FormRoute
    @State private var copy = ""; @State private var isbn = ""; @State private var title = ""; @State private var author = ""
    @State private var room = ""; @State private var bookcase = ""; @State private var shelf = ""
    @State private var publisher = ""; @State private var series = ""; @State private var cover = ""
    @State private var reminderConsent=false
    @State private var name = ""; @State private var contact = ""; @State private var borrower = ""
    @State private var checkout = LibraryStore.today; @State private var due = ""; @State private var pairText = ""
    @State private var scanning = false; @State private var looking = false; @State private var note = ""
    @State private var duplicates: [String] = []; @State private var confirmDuplicates = false
    @State private var coverResult: CoverResult?; @State private var findingCover = false
    @State private var receipt: Receipt?
    @State private var selectedCopy = ""
    var eligible: [Book] { store.state.circulationMatches(copy, returning: route.kind == "return") }
    var chosenCopy: String { eligible.count == 1 ? eligible[0].id : (eligible.contains { $0.id == selectedCopy } ? selectedCopy : "") }
    var body: some View {
        NavigationStack {
            Form {
                if route.kind == "pair" {
                    Text("On the server, open Paired phones → Add phone. Choose its HTTPS address, then scan the private pairing QR. Only scan a code from your own server.")
                    if let pair = store.pairing { Text("Paired server: \(pair.serverUrl)").font(.caption) }
                    TextField("Paste private pairing code", text: $pairText, axis: .vertical).textInputAutocapitalization(.never).autocorrectionDisabled()
                    Button("Scan pairing QR") { scanning = true }
                    Button("Pair phone") { Task { await store.pair(pairText); if store.online { dismiss() } } }.disabled(store.busy || store.hasPending)
                } else if route.kind == "book" {
                    TextField("ISBN / EAN", text: $isbn).keyboardType(.asciiCapable).autocorrectionDisabled()
                    Button("Scan ISBN") { scanning = true }
                    Button(looking ? "Looking up…" : "Look up book details") { Task { await lookup() } }.disabled(looking || isbn.isEmpty)
                    Picker("Collection", selection: $collectionId) { ForEach(store.state.availableCollections) { Text($0.name).tag($0.id) } }
                    TextField("Title", text: $title); TextField("Author", text: $author); TextField("Publisher", text: $publisher); TextField("Series", text: $series); TextField("Room (admin only)", text: $room); TextField("Bookcase (admin only)", text: $bookcase); TextField("Shelf (admin only)", text: $shelf)
                    TextField("HTTPS cover URL (optional)", text: $cover).keyboardType(.URL).textInputAutocapitalization(.never).autocorrectionDisabled()
                    Cover(book: Book(id: route.book?.id ?? "preview", isbn: isbn, title: title, author: author, publisher: publisher, coverUrl: cover, series: series, archived: false))
                    Button(findingCover ? "Checking cover sources…" : "Search Books for a cover") { Task { await findCovers() } }.disabled(findingCover || !store.online)
                    if let result = coverResult {
                        Text(result.message).font(.caption)
                        ForEach(result.covers) { candidate in
                            HStack(alignment: .top) {
                                Cover(book: Book(id: candidate.url, isbn: isbn, title: candidate.title, author: candidate.author, publisher: "", coverUrl: candidate.url, series: nil, archived: false))
                                VStack(alignment: .leading) {
                                    Text(candidate.source).font(.headline); Text(candidate.title)
                                    Text(candidate.exactEdition ? "ISBN-linked image — review cover" : "May be another edition — confirm cover").font(.caption)
                                    Button("Use this cover") { cover = candidate.url; coverResult = nil; note = "Cover selected. Save the book to keep it." }
                                    if let url = URL(string: candidate.sourceUrl) { Link("Source", destination: url) }
                                }
                            }
                        }
                    }
                    Button("Save book") { duplicates = matchingCopies(); if duplicates.isEmpty { Task { await saveBook([]) } } else { confirmDuplicates = true } }.disabled(store.busy || !store.online || store.hasPending)
                } else if route.kind == "borrower" {
                    TextField("Name", text: $name); TextField("Phone number or email (optional)", text: $contact).textInputAutocapitalization(.never)
                    Toggle("Borrower agreed to automatic email reminders",isOn:$reminderConsent)
                    Button("Add borrower") { Task { if await store.action("addBorrower", ["name": name, "contact": contact,"reminderConsent":reminderConsent]) { dismiss() } } }.disabled(store.busy || !store.online)
                } else if route.kind == "receipt" {
                    if let receipt { ReceiptView(receipt: receipt) } else { ProgressView() }
                } else {
                    TextField("ISBN / EAN, copy ID, title or author", text: $copy).textInputAutocapitalization(.characters).autocorrectionDisabled()
                    Button("Scan ISBN or copy label") { scanning = true }
                    Text("Scan the book’s existing barcode. Copy labels are optional; manual search works too.")
                    if eligible.isEmpty { Text(copy.isEmpty ? "Enter or scan a book to find a copy." : route.kind == "return" ? "No checked-out copy of this book was found." : "No available copy of this book was found.") }
                    else {
                        Text(eligible.count == 1 ? "Selected copy:" : "Choose the physical copy:")
                        ForEach(eligible) { book in
                            Button { selectedCopy = book.id } label: {
                                VStack(alignment: .leading) {
                                    Text("\(chosenCopy == book.id ? "✓ " : "")\(book.id) · \(book.title) · \(book.author) · \(store.state.availableCollections.first { $0.id == (book.collectionId ?? "general") }?.name ?? "General")")
                                    if let loan = store.state.active(book.id) { Text("Borrowed by \(store.state.borrowers.first { $0.id == loan.borrowerId }?.name ?? "Unknown") · Out \(loan.checkoutDate) · \(loan.dueDate.isEmpty ? "No due date" : "Due " + loan.dueDate)").font(.caption) }
                                }
                            }
                        }
                    }
                    if route.kind == "checkout" {
                        Picker("Borrower", selection: $borrower) { Text("Choose a borrower").tag(""); ForEach(store.state.borrowers.filter { $0.archived != true }) { Text($0.name).tag($0.id) } }
                        TextField("Checkout date YYYY-MM-DD", text: $checkout).keyboardType(.numbersAndPunctuation)
                        TextField("Due date YYYY-MM-DD (optional)", text: $due).keyboardType(.numbersAndPunctuation)
                        Button("Confirm checkout") { Task { copy = chosenCopy; if await store.action("checkout", ["copyId": copy, "borrowerId": borrower, "checkoutDate": checkout, "dueDate": due]) { await loadReceipt() } } }.disabled(store.busy || !store.online || store.hasPending || chosenCopy.isEmpty)
                        if let receipt { ReceiptView(receipt: receipt); Button("Done") { dismiss() } }
                    } else { Button("Confirm return") { Task { copy = chosenCopy; if await store.action("returnBook", ["copyId": copy]) { dismiss() } } }.disabled(store.busy || !store.online || store.hasPending || chosenCopy.isEmpty) }
                }
                if !note.isEmpty { Text(note) }; if let error = store.error { Text(error).foregroundStyle(.red) }
            }.navigationTitle(route.kind == "book" ? (route.book == nil ? "Add book" : "Edit book") : route.kind.capitalized)
                .toolbar { ToolbarItem(placement: .cancellationAction) { Button("Close") { dismiss() } } }
                .sheet(isPresented: $scanning) { NavigationStack { Scanner { code in scanning = false; if route.kind == "pair" { pairText = code } else if route.kind == "book" { isbn = code; title = ""; author = ""; publisher = ""; cover = ""; series = ""; coverResult = nil } else { copy = code } }.ignoresSafeArea(edges: .bottom).navigationTitle("Scan barcode").toolbar { Button("Cancel") { scanning = false } } } }
                .confirmationDialog("Another copy may already exist", isPresented: $confirmDuplicates, titleVisibility: .visible) { Button("Save another copy") { Task { await saveBook(duplicates) } } } message: { Text("Matching copies: \(duplicates.joined(separator: ", ")). Only continue if this is a separate physical copy.") }
                .onChange(of: copy) { selectedCopy = "" }
                .onChange(of: isbn) { coverResult = nil }
                .onChange(of: title) { coverResult = nil }
                .onChange(of: author) { coverResult = nil }
                .task {
                    copy = route.copyId; collectionId = route.book?.collectionId ?? (selectedCollection.isEmpty ? "general" : selectedCollection)
                    if let book = route.book { room = book.room ?? ""; bookcase = book.bookcase ?? ""; shelf = book.shelf ?? ""; isbn = book.isbn; title = book.title; author = book.author; publisher = book.publisher; cover = book.coverUrl; series = book.series ?? "" }
                    if route.kind == "receipt" { await loadReceipt() }
                }
        }
    }
    func matchingCopies() -> [String] {
        func edition(_ raw: String) -> String {
            let code = raw.uppercased().filter { !$0.isWhitespace && $0 != "-" }
            if code.count == 10, code.prefix(9).allSatisfy({ $0.isNumber }) {
                let prefix = "978" + String(code.prefix(9)); let digits = prefix.compactMap { $0.wholeNumberValue }; let sum = digits.enumerated().reduce(0) { $0 + $1.element * ($1.offset % 2 == 0 ? 1 : 3) }
                return prefix + String((10 - sum % 10) % 10)
            }; return code
        }
        func normalized(_ raw: String) -> String { raw.precomposedStringWithCanonicalMapping.uppercased().split(whereSeparator: { $0.isWhitespace }).joined(separator: " ") }
        if let old = route.book, edition(old.isbn) == edition(isbn), normalized(old.title) == normalized(title), normalized(old.author) == normalized(author) { return [] }
        return store.state.books.filter { $0.archived != true && $0.id != route.book?.id && (edition($0.isbn) == edition(isbn) || (!title.isEmpty && !author.isEmpty && normalized($0.title) == normalized(title) && normalized($0.author) == normalized(author))) }.map(\.id)
    }
    func saveBook(_ confirmed: [String]) async {
        let input: [String: Any] = ["copyId": route.book?.id ?? "", "isbn": isbn, "title": title, "author": author, "publisher": publisher, "series": series, "coverUrl": cover, "confirmedDuplicates": confirmed, "collectionId": collectionId, "room": room, "bookcase": bookcase, "shelf": shelf]
        if await store.action(route.book == nil ? "addBook" : "updateBook", input) { dismiss() }
    }
    func loadReceipt() async {
        do {
            let id = copy.isEmpty ? route.copyId : copy
            let encoded = id.addingPercentEncoding(withAllowedCharacters: .alphanumerics) ?? ""
            receipt = try JSONDecoder().decode(Receipt.self, from: await store.api("/mobile/receipts/\(encoded)"))
        } catch { note = error.localizedDescription }
    }
    func findCovers() async {
        let requestedISBN = isbn; let requestedTitle = title; let requestedAuthor = author
        findingCover = true; coverResult = nil; defer { findingCover = false }
        do {
            var components = URLComponents(); components.queryItems = [URLQueryItem(name: "isbn", value: isbn), URLQueryItem(name: "title", value: title), URLQueryItem(name: "author", value: author)]
            let result = try JSONDecoder().decode(CoverResult.self, from: await store.api("/mobile/covers?" + (components.percentEncodedQuery ?? "")))
            guard isbn == requestedISBN, title == requestedTitle, author == requestedAuthor else { return }; coverResult = result
        } catch { note = "Cover search unavailable. Check the connection and update the server to 2.0.12 or later." }
    }
    func lookup() async {
        let code = isbn.filter { !$0.isWhitespace && $0 != "-" }.uppercased()
        guard (code.count == 10 || code.count == 13), code.allSatisfy({ $0.isNumber || $0 == "X" }) else { note = "Enter a valid ISBN first."; return }
        looking = true; defer { looking = false }
        // Clear stale metadata before a new lookup; never reuse the last cover.
        title = ""; author = ""; publisher = ""; cover = ""; series = ""
        do {
            let url = URL(string: "https://openlibrary.org/api/books?bibkeys=ISBN:\(code)&format=json&jscmd=data")!
            var request = URLRequest(url: url); request.timeoutInterval = 15
            let (data, response) = try await URLSession.shared.data(for: request)
            guard (response as? HTTPURLResponse)?.statusCode == 200,
                  let json = try JSONSerialization.jsonObject(with: data) as? [String: Any], let book = json["ISBN:" + code] as? [String: Any] else { note = "No match. Enter details manually."; return }
            guard isbn.filter({ !$0.isWhitespace && $0 != "-" }).uppercased() == code else { return }
            title = book["title"] as? String ?? ""
            author = (book["authors"] as? [[String: Any]])?.compactMap { $0["name"] as? String }.joined(separator: ", ") ?? ""
            publisher = (book["publishers"] as? [[String: Any]])?.compactMap { $0["name"] as? String }.joined(separator: ", ") ?? ""
            cover = (book["cover"] as? [String: String])?["medium"] ?? ""; note = "Review the details before saving."; if cover.isEmpty { await findCovers() }
        } catch { note = "Lookup unavailable. Enter the details manually." }
    }
}
