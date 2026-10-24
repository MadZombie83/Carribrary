import SwiftUI
struct LendingTools: View {
 @EnvironmentObject var store: LibraryStore
 @State private var mode = "Holds"; @State private var query = ""; @State private var selected: [String] = []
 @State private var borrower = ""; @State private var due = ""; @State private var date = LibraryStore.today
 @State private var received = false; @State private var scanning = false; @State private var handing: CopyHold?; @State private var releasing: CopyHold?
 @State private var batchLimit = "5"; @State private var activeLimit = "5"
 var enabled: Bool { store.online && !store.busy && !store.hasPending }
 var people: [Borrower] { store.state.borrowers.filter { $0.archived != true } }
 var holds: [CopyHold] { store.state.holds ?? [] }
 var candidates: [Book] { store.state.circulationMatches(query, returning: mode == "Batch return").filter { !selected.contains($0.id) } }
 func add(_ book: Book) { guard !selected.contains(book.id), mode != "Batch checkout" || selected.count < (store.state.maxBatchCheckout ?? 5) else { return }; selected.append(book.id); query = "" }
 var body: some View {
  Form {
   Picker("Lending", selection: $mode) { ForEach(["Holds", "Batch checkout", "Batch return", "Limits"], id: \.self) { Text($0) } }.onChange(of: mode) { _, _ in selected = []; query = ""; received = false }
   if let error = store.error { Text(error).foregroundStyle(.red) }
   if mode == "Holds" {
    Section("Reserved copies") {
     Text("Holds do not start loans or due dates. Approve pending requests first. Confirm handover only after the borrower receives the book.")
     ForEach(holds) { hold in
      VStack(alignment: .leading) {
       Text(store.state.books.first { $0.id == hold.copyId }?.title ?? hold.copyId).font(.headline)
       Text(hold.copyId + " · " + hold.stage)
       Text(store.state.requests?.first { $0.id == hold.requestId }?.name ?? people.first { $0.id == hold.borrowerId }?.name ?? "Borrower")
       if hold.stage != "pending" {
        Button("Ready for pickup / delivery") { Task { _ = await store.action("readyHold", ["holdId": hold.id]) } }.disabled(!enabled)
        Button("Confirm handover") { date = LibraryStore.today; due = ""; received = false; handing = hold }.disabled(!enabled)
       }
       Button("Release hold", role: .destructive) { releasing = hold }.disabled(!enabled)
      }
     }
    }
    Section("Hold an available copy") {
     TextField("ISBN, copy ID, title or author", text: $query); Button("Scan barcode") { scanning = true }
     Picker("Borrower", selection: $borrower) { Text("Choose").tag(""); ForEach(people) { Text($0.name).tag($0.id) } }
     ForEach(store.state.circulationMatches(query, returning: false)) { book in Button("Hold " + book.id + " · " + book.title) { Task { if await store.action("createHold", ["copyId": book.id, "borrowerId": borrower]) { query = "" } } }.disabled(!enabled || borrower.isEmpty) }
    }
   } else if mode == "Limits" {
    Text("Separate batch checkout and active-loan limits, from 1 to 100. Existing loans stay. Borrowing limits never restrict returns.")
    TextField("Maximum books per batch", text: $batchLimit).keyboardType(.numberPad)
    TextField("Maximum active loans per borrower", text: $activeLimit).keyboardType(.numberPad)
    Button("Save lending limits") { Task { _ = await store.action("setLendingLimits", ["maxBatchCheckout": Int(batchLimit) ?? 0, "maxActiveLoans": Int(activeLimit) ?? 0]) } }.disabled(!enabled)
   } else {
    Section("Scan or search") {
     Text(mode == "Batch checkout" ? "Select available copies for one borrower. Hand held books over from Holds / Requests." : "Select checked-out physical copies. No borrowing limit applies to returns.")
     TextField("ISBN, copy ID, title or author", text: $query); Button("Scan barcode") { scanning = true }
     ForEach(candidates) { book in Button("Add " + book.id + " · " + book.title) { add(book) } }
    }
    Section("Review selected books") {
     ForEach(selected, id: \.self) { id in HStack { Text(id + " · " + (store.state.books.first { $0.id == id }?.title ?? "")); Button("Remove") { selected.removeAll { $0 == id } } } }
     if mode == "Batch checkout" {
      Picker("Borrower", selection: $borrower) { Text("Choose").tag(""); ForEach(people) { Text($0.name).tag($0.id) } }
      TextField("Handover date YYYY-MM-DD", text: $date); TextField("Due date YYYY-MM-DD (optional)", text: $due)
      Toggle("The borrower has received these books", isOn: $received)
     }
     Text("All books save together. If a copy conflicts, no books in the batch change.")
     Button(mode == "Batch checkout" ? "Confirm batch checkout" : "Confirm batch return") { Task { if await store.action(mode == "Batch checkout" ? "batchCheckout" : "batchReturn", ["copyIds": selected, "borrowerId": borrower, "checkoutDate": date, "dueDate": due, "handoverConfirmed": received]) { selected = []; received = false } } }.disabled(!enabled || selected.isEmpty || (mode == "Batch checkout" && (!received || borrower.isEmpty)))
    }
   }
  }.navigationTitle("Lending & holds").onAppear { batchLimit = String(store.state.maxBatchCheckout ?? 5); activeLimit = String(store.state.maxActiveLoans ?? 5) }
  .sheet(isPresented: $scanning) { NavigationStack { Scanner { code in scanning = false; query = code; if mode != "Holds" { let matches = candidates; if matches.count == 1 { add(matches[0]) } } }.navigationTitle("Scan barcode").toolbar { Button("Cancel") { scanning = false } } } }
  .sheet(item: $handing) { hold in NavigationStack { Form {
   Text(store.state.books.first { $0.id == hold.copyId }?.title ?? hold.copyId)
   TextField("Handover date YYYY-MM-DD", text: $date); TextField("Due date (optional)", text: $due)
   Toggle("The borrower has received this book", isOn: $received)
   if let error = store.error { Text(error).foregroundStyle(.red) }
   Button("Check out held book") { Task { if await store.action("handoverHold", ["holdId": hold.id, "checkoutDate": date, "dueDate": due, "handoverConfirmed": received]) { handing = nil } } }.disabled(!enabled || !received)
  }.navigationTitle("Confirm handover").toolbar { Button("Cancel") { handing = nil } } } }
  .confirmationDialog("Release this hold? A linked request will be declined.", isPresented: Binding(get: { releasing != nil }, set: { if !$0 { releasing = nil } })) { Button("Release hold", role: .destructive) { if let hold = releasing { Task { _ = await store.action("releaseHold", ["holdId": hold.id]) } }; releasing = nil } }
 }
}
