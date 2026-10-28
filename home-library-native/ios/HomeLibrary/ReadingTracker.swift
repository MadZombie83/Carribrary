import SwiftUI
struct ReaderProfile: Codable, Identifiable { var id: String; var name: String; var borrowerId: String?; var annualGoals: [String:Int]? = nil }
struct ReadingEntry: Codable, Identifiable { var id: String; var readerId: String; var copyId: String; var status: String; var startedDate: String; var finishedDate: String; var rating: Int; var notes: String; var currentPage: Int? = nil; var totalPages: Int? = nil; var sessions: [ReadingSession]? = nil; var annotations: [ReadingAnnotation]? = nil }
struct ReadingSession: Codable, Identifiable { var id: String; var startedDate: String; var finishedDate: String; var currentPage: Int; var totalPages: Int; var rating: Int; var notes: String }
struct ReadingAnnotation: Codable, Identifiable { var id: String; var kind: String; var text: String; var page: Int? }
private let readingLabels = ["want": "Want to read", "reading": "Currently reading", "finished": "Finished"]
struct ReadingTrackerView: View {
 @EnvironmentObject var store: LibraryStore
 @AppStorage("readingReader") var selection = ""
 @State var filter = "tbr"; @State var showingProfile = false; @State var profileName = ""; @State var borrowerId = ""; @State var editProfile = false
 @State var entryBook: Book?; @State var sharing = false; @State var confirmDelete = false; @State var removeCopy: String?
 var readers: [ReaderProfile] { store.state.readers ?? [] }
 var reader: ReaderProfile? { readers.first { $0.id == selection } ?? readers.first }
 var entries: [ReadingEntry] { (store.state.readingEntries ?? []).filter { $0.readerId == reader?.id } }
 var enabled: Bool { store.online && !store.busy && !store.hasPending }
 var body: some View {
  VStack(alignment: .leading, spacing: 12) {
   Text("Reading tracker").font(.title2)
   Text("Admin-managed household TBR. Borrowers use their own encrypted account lists. Unread and currently reading books remain until finished. Reading history stays; loans are separate.").font(.caption)
   Picker("Reader", selection: $selection) { if selection.isEmpty { Text(reader?.name ?? "Choose").tag("") }; ForEach(readers) { Text($0.name).tag($0.id) } }
   Button("Add household reader") { editProfile = false; profileName = ""; borrowerId = ""; showingProfile = true }.disabled(!enabled)
   if let reader {
    HStack { Button("Edit reader") { editProfile = true; profileName = reader.name; borrowerId = reader.borrowerId ?? ""; showingProfile = true }; Button("Personal link / QR") { sharing = true } }.disabled(!enabled)
    Button("Delete reader", role: .destructive) { confirmDelete = true }.disabled(!enabled)
    Picker("Status", selection: $filter) { Text("To Be Read (TBR)").tag("tbr"); Text("All reading history").tag(""); ForEach(["want", "reading", "finished"], id: \.self) { Text(readingLabels[$0] ?? $0).tag($0) } }
    Menu("Add book to TBR / reading list") { ForEach(store.state.books.filter { $0.archived != true }.sorted { $0.title < $1.title }) { book in Button(book.title + " · " + book.id) { entryBook = book } } }.disabled(!enabled)
    ForEach(entries.filter { filter.isEmpty || (filter == "tbr" ? ["want", "reading"].contains($0.status) : $0.status == filter) }) { entry in
     if let book = store.state.books.first(where: { $0.id == entry.copyId }) {
      VStack(alignment: .leading, spacing: 8) {
       HStack { Cover(book: book); VStack(alignment: .leading) { Text(book.title).font(.headline); Text(book.author); Text(book.id).font(.caption) } }
       Text(readingLabels[entry.status] ?? entry.status)
       if entry.rating > 0 { Text("\(entry.rating)/5 ★") }
       if !entry.startedDate.isEmpty { Text("Started \(entry.startedDate)").font(.caption) }
       if !entry.finishedDate.isEmpty { Text("Finished \(entry.finishedDate)").font(.caption) }
       if !entry.notes.isEmpty { Text(entry.notes) }
       if book.archived == true { Text("Removed from catalog · reading history retained").font(.caption) }
       if entry.status != "finished" { Button("Mark as read") { Task { _ = await store.action("saveReading", ["readerId": reader.id, "copyId": entry.copyId, "status": "finished", "startedDate": entry.startedDate, "finishedDate": LibraryStore.today, "rating": entry.rating, "notes": entry.notes]) } }.disabled(!enabled) }
       HStack { Button("Edit reading") { entryBook = book }; Button("Remove entry", role: .destructive) { removeCopy = book.id } }.disabled(!enabled)
      }.padding().background(.background, in: RoundedRectangle(cornerRadius: 14))
     }
    }
   } else { Text("Add a reader to start their list.") }
   if let error = store.error { Text(error).foregroundStyle(.red) }
  }
  .sheet(isPresented: $showingProfile) { NavigationStack { Form {
   TextField("Reader name", text: $profileName)
   if let error = store.error { Text(error).foregroundStyle(.red) }
   Button("Save reader") { Task { if await store.action(editProfile ? "renameReader" : "addReader", ["readerId": reader?.id ?? "", "name": profileName, "borrowerId": ""]) { showingProfile = false } } }.disabled(!enabled || profileName.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty)
  }.navigationTitle("Reader profile").toolbar { Button("Close") { showingProfile = false } } } }
  .sheet(item: $entryBook) { book in if let reader { ReadingEntryForm(book: book, reader: reader, old: entries.first { $0.copyId == book.id }).environmentObject(store) } }
  .sheet(isPresented: $sharing) { if let reader { PersonalReadingShare(reader: reader).environmentObject(store) } }
  .confirmationDialog("Delete this reader? Remove their reading entries first. Request history requires keeping the profile.", isPresented: $confirmDelete) { Button("Delete reader", role: .destructive) { if let reader { Task { _ = await store.action("removeReader", ["readerId": reader.id]) } } } }
  .confirmationDialog("Remove this reading entry? The catalog book stays.", isPresented: Binding(get: { removeCopy != nil }, set: { if !$0 { removeCopy = nil } })) { Button("Remove entry", role: .destructive) { if let copy = removeCopy, let reader { Task { _ = await store.action("removeReading", ["readerId": reader.id, "copyId": copy]) } }; removeCopy = nil } }
 }
}
struct ReadingEntryForm: View {
 @EnvironmentObject var store: LibraryStore; @Environment(\.dismiss) var dismiss
 let book: Book; let reader: ReaderProfile; let old: ReadingEntry?
 @State var status = "want"; @State var started = ""; @State var finished = ""; @State var rating = 0; @State var notes = ""
 var body: some View { NavigationStack { Form {
  Text(book.title).font(.headline)
  Picker("Status", selection: $status) { ForEach(["want", "reading", "finished"], id: \.self) { Text(readingLabels[$0] ?? $0).tag($0) } }
  if status != "want" { TextField("Started YYYY-MM-DD (optional)", text: $started) }
  if status == "finished" { TextField("Finished YYYY-MM-DD (optional)", text: $finished) }
  Picker("Rating", selection: $rating) { Text("Unrated").tag(0); ForEach(1...5, id: \.self) { Text("\($0) stars").tag($0) } }
  TextField("Notes", text: $notes, axis: .vertical).lineLimit(3...8)
  if let error = store.error { Text(error).foregroundStyle(.red) }
  Button("Save reading entry") { Task { if await store.action("saveReading", ["readerId": reader.id, "copyId": book.id, "status": status, "startedDate": status == "want" ? "" : started, "finishedDate": status == "finished" ? finished : "", "rating": rating, "notes": notes]) { dismiss() } } }.disabled(!store.online || store.busy || store.hasPending)
 }.navigationTitle(reader.name).toolbar { Button("Close") { dismiss() } }.onAppear { if let old { status = old.status; started = old.startedDate; finished = old.finishedDate; rating = old.rating; notes = old.notes } } } }
}
struct PersonalReadingShare: View {
 @EnvironmentObject var store: LibraryStore; @Environment(\.dismiss) var dismiss; let reader: ReaderProfile
 @State var sharing: Sharing?; @State var error = ""; @State var confirm = ""
 func load(_ action: String? = nil) async { do { sharing = try JSONDecoder().decode(Sharing.self, from: await store.api("/mobile/readers/\(reader.id)/sharing", body: action.map { ["action": $0] })); error = "" } catch { self.error = error.localizedDescription } }
 var body: some View { NavigationStack { ScrollView { VStack(spacing: 16) {
  Text("Only this reader's reading list and requests. Anyone holding this link can use it. Keep it private.")
  if let raw = sharing?.url, let url = URL(string: raw), !raw.isEmpty { QRView(text: raw); ShareLink("Share private link", item: url); Button("Replace link / revoke old QR") { confirm = "replace" }; Button("Revoke access", role: .destructive) { confirm = "disable" } }
  else if sharing?.configured == true { Button("Create personal link / QR") { Task { await load("enable") } } }
  else { Text("Configure public HTTPS sharing on your server first.") }
  if !error.isEmpty { Text(error).foregroundStyle(.red) }
 }.padding() }.navigationTitle(reader.name).toolbar { Button("Close") { dismiss() } }.task { await load() }.confirmationDialog("Revoke the existing personal link and QR? Reading history stays.", isPresented: Binding(get: { !confirm.isEmpty }, set: { if !$0 { confirm = "" } })) { Button("Confirm") { let action = confirm; confirm = ""; Task { await load(action) } } } } }
}
