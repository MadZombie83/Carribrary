import SwiftUI

@main struct HomeLibraryApp: App {
    @StateObject private var store = LibraryStore()
    var body: some Scene { WindowGroup { LibraryView().environmentObject(store) } }
}
enum Palette: String, CaseIterable {
    case ocean = "Ocean blue", purple = "Purple and blue", forest = "Forest green", rose = "Rose", midnight = "Midnight"
    var color: Color {
        switch self { case .ocean: return Color(red: 0.08, green: 0.29, blue: 0.43)
        case .purple: return Color(red: 0.34, green: 0.20, blue: 0.57)
        case .forest: return Color(red: 0.12, green: 0.36, blue: 0.27)
        case .rose: return Color(red: 0.55, green: 0.20, blue: 0.35)
        case .midnight: return Color(red: 0.10, green: 0.14, blue: 0.25) }
    }
}
struct LibraryView: View {
    @EnvironmentObject var store: LibraryStore
    @AppStorage("palette") var palette = Palette.ocean.rawValue
    @AppStorage("collection") var collection = ""
    @AppStorage("sort") var sort = "Title"
    @AppStorage("pageSize") var pageSize = 10
    @AppStorage("descending") var descending = false
    @State private var search = ""; @State private var tab = "Catalog"; @State private var page = 0
    @State private var form: FormRoute?; @State private var administration = false
    @State private var help = false
    @State private var deletion: Book?
    var color: Color { (Palette(rawValue: palette) ?? .ocean).color }
    var activeCollection: String { store.state.availableCollections.contains { $0.id == collection } ? collection : "" }
    var books: [Book] {
        let books = store.state.books.filter { book in
            book.archived != true && (activeCollection.isEmpty || (book.collectionId ?? "general") == activeCollection) && (tab != "On loan" || store.state.active(book.id) != nil) &&
            (search.isEmpty || [book.title, book.author, book.series ?? "", book.isbn, book.id].joined(separator: " ").localizedCaseInsensitiveContains(search))
        }
        return books.sorted { a, b in
            func value(_ book: Book) -> String { switch sort { case "Author": return book.author; case "Series": return book.series ?? ""; case "Catalog number": return book.id; default: return book.title } }
            let comparison = value(a).localizedStandardCompare(value(b))
            if comparison == .orderedSame { return descending ? a.id > b.id : a.id < b.id }
            return descending ? comparison == .orderedDescending : comparison == .orderedAscending
        }
    }
    var body: some View {
        NavigationStack {
            VStack(spacing: 0) {
                VStack(alignment: .leading, spacing: 5) {
                    Label(store.state.libraryName, systemImage: "books.vertical").font(.title2.bold())
                    if let subtitle = store.state.bannerSubtitle, !subtitle.isEmpty { Text(subtitle).font(.subheadline) }
                    Text(store.online ? "Connected" : "Offline · cached library").font(.caption)
                }.foregroundStyle(.white).frame(maxWidth: .infinity, alignment: .leading).padding().background(color)
                ScrollView {
                    VStack(alignment: .leading, spacing: 14) {
                        if let saying = store.state.encouragingMessage, !saying.isEmpty { Text(saying).font(.title3).accessibilityAddTraits(.isHeader) }
                        TextField("Search title, author, series, ISBN or copy ID", text: $search).textFieldStyle(.roundedBorder).accessibilityLabel("Search library")
                        Picker("Collection", selection: $collection) { Text("All collections").tag(""); ForEach(store.state.availableCollections) { Text($0.name).tag($0.id) } }
                        HStack {
                            Button("Add book") { form = FormRoute(kind: "book") }
                            Button("Check out") { form = FormRoute(kind: "checkout") }
                            Button("Return") { form = FormRoute(kind: "return") }
                        }.buttonStyle(.bordered).disabled(!store.online || store.busy || store.hasPending)
                        HStack {
                            Picker("Sort", selection: $sort) { ForEach(["Title", "Author", "Series", "Catalog number"], id: \.self) { Text($0) } }
                            Button { descending.toggle() } label: { Image(systemName: descending ? "arrow.down" : "arrow.up") }.accessibilityLabel("Reverse sort order")
                            Picker("Per page", selection: $pageSize) { ForEach([10,25,50], id: \.self) { Text("\($0) per page").tag($0) } }
                        }.font(.subheadline)
                        if store.hasPending { Text("A change is awaiting confirmation. Tap Refresh to retry safely.").foregroundStyle(.orange) }
                        if let error = store.error { Text(error).foregroundStyle(.red).font(.subheadline) }
                        if tab == "Reading" { ReadingTrackerView().environmentObject(store) } else if tab == "Borrowers" { borrowerList } else { catalog }
                    }.padding()
                }.refreshable { await store.refresh() }
                HStack {
                    ForEach(["Catalog", "On loan", "Reading"], id: \.self) { item in
                        Button { tab = item; page = 0 } label: { VStack { Image(systemName: item == "Catalog" ? "books.vertical" : item == "On loan" ? "arrow.up.right" : "person.2"); Text(item).font(.caption) }.frame(maxWidth: .infinity).foregroundStyle(tab == item ? color : .secondary) }
                    }
                    Button { administration = true } label: { VStack { Image(systemName: "gearshape"); Text("More").font(.caption) }.frame(maxWidth: .infinity) }
                }.padding(.vertical, 10).background(.bar)
            }.tint(color).toolbar {
                ToolbarItem(placement: .topBarTrailing) { Button("Refresh") { Task { await store.refresh() } }.disabled(store.busy) }
                ToolbarItem(placement: .topBarTrailing) { Button("Help") { help = true } }
                ToolbarItem(placement: .topBarLeading) { Button("Connection") { form = FormRoute(kind: "pair") } }
            }
            .sheet(item: $form) { route in LibraryForm(route: route).environmentObject(store) }
            .sheet(isPresented: $help) { NavigationStack { HelpScreen().toolbar { Button("Done") { help = false } } } }
            .sheet(isPresented: $administration) { Administration().environmentObject(store) }
            .confirmationDialog("Delete \(deletion?.title ?? "book")?", isPresented: Binding(get: { deletion != nil }, set: { if !$0 { deletion = nil } }), titleVisibility: .visible) {
                Button("Delete book", role: .destructive) { if let book = deletion { Task { _ = await store.action("removeBook", ["copyId": book.id]) } }; deletion = nil }
            } message: { Text("Existing loan history is preserved. Return active loans first.") }
            .task { await store.refresh() }
            .onChange(of: collection) { page = 0 }.onChange(of: search) { page = 0 }.onChange(of: sort) { page = 0 }.onChange(of: descending) { page = 0 }.onChange(of: pageSize) { page = 0 }
        }
    }
    @ViewBuilder var catalog: some View {
        let list = books; let safePage = min(page, max(0, (list.count - 1) / pageSize))
        if list.isEmpty { ContentUnavailableView("No matching books", systemImage: "books.vertical", description: Text(store.pairing == nil ? "Tap Connection to pair with your server." : "Add a book or change your search.")) }
        ForEach(Array(list.dropFirst(safePage * pageSize).prefix(pageSize))) { book in
            VStack(alignment: .leading, spacing: 12) {
                HStack(alignment: .top) {
                    Cover(book: book)
                    VStack(alignment: .leading, spacing: 5) { Text(book.id).font(.caption.monospaced()); Text(book.title).font(.headline); Text(book.author).foregroundStyle(.secondary); if let series = book.series, !series.isEmpty { Text(series).font(.caption) } }
                    Spacer(minLength: 0)
                }
                Text([book.room, book.bookcase, book.shelf].compactMap { $0 }.filter { !$0.isEmpty }.joined(separator: " · ")).font(.caption); Text(book.publisher).font(.caption); Text("ISBN / EAN \(book.isbn)").font(.caption).foregroundStyle(.secondary)
                if let loan = store.state.active(book.id) {
                    Text("Loaned to \(store.state.borrowers.first { $0.id == loan.borrowerId }?.name ?? loan.borrowerId)")
                    Text(loan.dueDate.isEmpty ? "No due date" : "Due \(loan.dueDate)").font(.caption)
                    HStack { Button("Return") { form = FormRoute(kind: "return", copyId: book.id) }; Button("Receipt") { form = FormRoute(kind: "receipt", copyId: book.id) } }
                } else { HStack { Text((store.state.holds ?? []).contains { $0.copyId == book.id } ? "On hold" : "Available").font(.caption); Spacer(); Button("Check out") { form = FormRoute(kind: "checkout", copyId: book.id) }.disabled((store.state.holds ?? []).contains { $0.copyId == book.id }) } }
                HStack { Button("Edit details") { form = FormRoute(kind: "book", book: book) }; Spacer(); Button("Delete book", role: .destructive) { deletion = book } }
            }.padding().background(.background, in: RoundedRectangle(cornerRadius: 18)).overlay(RoundedRectangle(cornerRadius: 18).stroke(color.opacity(0.18))).disabled(!store.online || store.busy || store.hasPending)
        }
        HStack { Button("Previous") { page = safePage - 1 }.disabled(safePage == 0); Spacer(); Text("\(list.count) copies · Page \(safePage + 1) of \(max(1, (list.count + pageSize - 1) / pageSize))").font(.caption); Spacer(); Button("Next") { page = safePage + 1 }.disabled((safePage + 1) * pageSize >= list.count) }
    }
    var borrowerList: some View {
        VStack(alignment: .leading, spacing: 16) {
            Button("Add borrower") { form = FormRoute(kind: "borrower") }.disabled(!store.online)
            ForEach(store.state.borrowers.filter { $0.archived != true && (search.isEmpty || ($0.name + " " + $0.contact).localizedCaseInsensitiveContains(search)) }) { borrower in
                BorrowerRow(borrower: borrower).environmentObject(store)
            }
        }
    }
}
struct BorrowerRow: View {
    @EnvironmentObject var store: LibraryStore; let borrower: Borrower; @State var confirm = false; @State var edit = false
    var body: some View {
        HStack { VStack(alignment: .leading) { Text(borrower.name).font(.headline); Text(borrower.contact).font(.subheadline) }; Spacer(); Button("Edit / reminders"){edit=true}; Button("Delete", role: .destructive) { confirm = true }.disabled(!store.online || store.busy || store.hasPending) }
            .sheet(isPresented:$edit){BorrowerEditScreen(borrower:borrower).environmentObject(store)}
            .confirmationDialog("Delete \(borrower.name)?", isPresented: $confirm, titleVisibility: .visible) { Button("Delete borrower", role: .destructive) { Task { _ = await store.action("removeBorrower", ["borrowerId": borrower.id]) } } } message: { Text("Return active loans first. The record moves to the Recycle Bin and past loans remain in history.") }
    }
}
struct Cover: View {
    let book: Book
    var url: URL? {
        let raw = book.coverUrl.isEmpty ? "https://covers.openlibrary.org/b/isbn/\(book.isbn)-M.jpg?default=false" : book.coverUrl
        guard let url = URL(string: raw), url.scheme == "https" else { return nil }; return url
    }
    var googleLink: URL? { guard let components = URLComponents(string: book.coverUrl), components.host == "books.google.com", let id = components.queryItems?.first(where: { $0.name == "id" })?.value, id.range(of: "^[A-Za-z0-9_-]{1,100}$", options: .regularExpression) != nil else { return nil }; return URL(string: "https://books.google.com/books?id=" + id) }
    var body: some View { VStack(alignment: .leading) { AsyncImage(url: url) { image in image.resizable().scaledToFit() } placeholder: { Image(systemName: "book.closed").font(.largeTitle).foregroundStyle(.secondary) }.frame(width: 76, height: 110).id(url?.absoluteString ?? book.id).accessibilityLabel("Cover of \(book.title)"); if let googleLink { Link(destination: googleLink) { VStack(alignment: .leading) { Image("PoweredByGoogle").frame(width: 62, height: 30).padding(4).background(.white); Text("Google Books").font(.caption2) } } } } }
}
struct FormRoute: Identifiable { var id = UUID(); var kind: String; var copyId = ""; var book: Book? }
