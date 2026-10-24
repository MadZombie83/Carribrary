import SwiftUI
struct AcquisitionList: View {
    @EnvironmentObject var store: LibraryStore
    @State private var editing: Acquisition?; @State private var buying: Acquisition?; @State private var removing: Acquisition?
    var body: some View {
        List {
            Section { Text("Private acquisition wishlist. These are not owned copies and do not appear in the shared catalog."); Button("Scan / add a book") { editing = Acquisition(id: "", isbn: "", title: "", author: "", publisher: "", coverUrl: "", notes: "", addedDate: "") }.disabled(!store.online) }
            ForEach((store.state.booksToBuy ?? []).sorted { $0.title.localizedCaseInsensitiveCompare($1.title) == .orderedAscending }) { item in
                VStack(alignment: .leading) {
                    Text(item.title).font(.headline); Text(item.author); Text("ISBN \(item.isbn)").font(.caption)
                    if !item.notes.isEmpty { Text(item.notes) }
                    Button("Edit") { editing = item }; Button("Acquired — add to library") { buying = item }; Button("Remove from wishlist", role: .destructive) { removing = item }
                }
            }
        }.navigationTitle("Books to buy")
        .sheet(item: $editing) { AcquisitionEditor(item: $0).environmentObject(store) }
        .sheet(item: $buying) { AcquisitionHandover(item: $0).environmentObject(store) }
        .alert("Remove wishlist entry?", isPresented: Binding(get: { removing != nil }, set: { if !$0 { removing = nil } })) { Button("Remove", role: .destructive) { if let item = removing { Task { _ = await store.action("removeAcquisition", ["id": item.id]); removing = nil } } }; Button("Cancel", role: .cancel) { removing = nil } }
    }
}
struct AcquisitionEditor: View {
    @EnvironmentObject var store: LibraryStore; @Environment(\.dismiss) private var dismiss
    @State var item: Acquisition
    @State private var scanning = false; @State private var working = false; @State private var note = ""; @State private var covers: CoverResult?
    var body: some View {
        NavigationStack { Form {
            TextField("ISBN / EAN", text: $item.isbn).onChange(of: item.isbn) { item.coverUrl = ""; covers = nil }
            Button("Scan ISBN barcode") { scanning = true }
            Button("Look up book details") { Task { await lookup() } }.disabled(working)
            TextField("Title", text: $item.title); TextField("Author", text: $item.author); TextField("Publisher", text: $item.publisher); TextField("Cover URL (optional)", text: $item.coverUrl); TextField("Private acquisition notes", text: $item.notes, axis: .vertical)
            Cover(book: Book(id: item.id, isbn: item.isbn, title: item.title, author: item.author, publisher: item.publisher, coverUrl: item.coverUrl, series: nil, archived: false))
            Button("Search Books for a cover") { Task { await findCovers() } }.disabled(working)
            if let covers { Text(covers.message); ForEach(covers.covers) { c in VStack(alignment: .leading) { Cover(book: Book(id: c.url, isbn: item.isbn, title: c.title, author: c.author, publisher: "", coverUrl: c.url, series: nil, archived: false)); Button("Use this cover") { item.coverUrl = c.url; self.covers = nil } } } }
            Text("No catalog number is assigned until you acquire the book.")
            if !note.isEmpty { Text(note) }; if let error = store.error { Text(error) }
            Button("Save to wishlist") { Task { var input: [String: Any] = ["isbn": item.isbn, "title": item.title, "author": item.author, "publisher": item.publisher, "coverUrl": item.coverUrl, "notes": item.notes]; if !item.id.isEmpty { input["id"] = item.id }; if await store.action("saveAcquisition", input) { dismiss() } } }.disabled(working || store.busy || !store.online || item.title.isEmpty)
        }.navigationTitle("Wishlist entry").toolbar { Button("Close") { dismiss() } }
        .sheet(isPresented: $scanning) { NavigationStack { Scanner { isbn in scanning = false; item.isbn = isbn; item.title = ""; item.author = ""; item.publisher = ""; item.coverUrl = ""; covers = nil }.toolbar { Button("Cancel") { scanning = false } } } } }
    }
    func lookup() async {
        working = true; defer { working = false }; let isbn = item.isbn.replacingOccurrences(of: "-", with: "").trimmingCharacters(in: .whitespacesAndNewlines)
        guard let url = URL(string: "https://openlibrary.org/api/books?bibkeys=ISBN:\(isbn)&format=json&jscmd=data") else { note = "Enter an ISBN first"; return }
        do { let (data,response) = try await URLSession.shared.data(for: URLRequest(url: url, timeoutInterval: 12)); guard (response as? HTTPURLResponse)?.statusCode == 200, let object = try JSONSerialization.jsonObject(with: data) as? [String: Any], let book = object["ISBN:\(isbn)"] as? [String: Any] else { note = "No match found; enter details manually."; return }; item.title = book["title"] as? String ?? ""; item.author = (book["authors"] as? [[String: Any]])?.compactMap { $0["name"] as? String }.joined(separator: ", ") ?? ""; item.publisher = (book["publishers"] as? [[String: Any]])?.compactMap { $0["name"] as? String }.joined(separator: ", ") ?? ""; if item.coverUrl.isEmpty { item.coverUrl = (book["cover"] as? [String: String])?["medium"] ?? "" }; note = "Review details before saving." } catch { note = "Lookup unavailable; enter details manually." }
    }
    func findCovers() async { working = true; defer { working = false }; do { var query = URLComponents(); query.queryItems = [URLQueryItem(name: "isbn", value: item.isbn),URLQueryItem(name: "title", value: item.title),URLQueryItem(name: "author", value: item.author)]; covers = try JSONDecoder().decode(CoverResult.self, from: await store.api("/mobile/covers?" + (query.percentEncodedQuery ?? ""))) } catch { note = error.localizedDescription } }
}
struct AcquisitionHandover: View {
    @EnvironmentObject var store: LibraryStore; @Environment(\.dismiss) private var dismiss
    let item: Acquisition
    @State private var owned = false; @State private var allowDuplicate = false; @State private var collection = "general"; @State private var room = ""; @State private var bookcase = ""; @State private var shelf = ""
    // Confirm any matching ISBN/title conservatively; the server makes the final duplicate decision.
    var matches: [Book] { store.state.books.filter { $0.archived != true && ($0.isbn == item.isbn || $0.title.localizedCaseInsensitiveCompare(item.title) == .orderedSame) } }
    var body: some View { NavigationStack { Form { Text(item.title); Picker("Collection", selection: $collection) { ForEach(store.state.availableCollections) { Text($0.name).tag($0.id) } }; TextField("Room (private)", text: $room); TextField("Bookcase (private)", text: $bookcase); TextField("Shelf (private)", text: $shelf); Toggle("I now own this physical book", isOn: $owned); if !matches.isEmpty { Text("Possible owned copies: " + matches.map(\.id).joined(separator: ", ")); Toggle("Add another physical copy", isOn: $allowDuplicate) }; if let error = store.error { Text(error) }; Button("Add physical copy") { Task { if await store.action("acquireBook", ["id": item.id, "purchaseConfirmed": true, "confirmedDuplicates": matches.map(\.id), "collectionId": collection, "room": room, "bookcase": bookcase, "shelf": shelf]) { dismiss() } } }.disabled(store.busy || !store.online || !owned || (!matches.isEmpty && !allowDuplicate)) }.navigationTitle("Acquired book").toolbar { Button("Cancel") { dismiss() } } }
}
}
