import SwiftUI
struct DataControls: View {
 @EnvironmentObject var store: LibraryStore
 @State private var reader = "all"; @State private var reading = true; @State private var loans = false; @State private var requests = false
 @State private var preview: [String: Any]?; @State private var confirmation = ""; @State private var note = ""; @State private var clearCache = false
 @State private var deletionURL: URL?
 var selection: [String: Any] { ["readerId": reader == "all" ? "" : reader, "allReaders": reader == "all", "reading": reading, "loans": loans, "requests": requests] }
 var body: some View {
  Form {
   Text("Clear completed history. Active loans, holds, open requests, TBR and currently reading entries stay. Older backups, exports and other devices may retain earlier copies. No new pre-clear backup is made.")
   Picker("Scope", selection: $reader) { Text("Entire library history").tag("all"); ForEach(store.state.readers ?? []) { Text($0.name).tag($0.id) } }.onChange(of: reader) { _, _ in preview = nil }
   Toggle("Finished reading, notes and ratings", isOn: $reading).onChange(of: reading) { _, _ in preview = nil }
   Toggle("Returned loans and linked closed requests", isOn: $loans).onChange(of: loans) { _, _ in preview = nil }
   Toggle("Other closed requests", isOn: $requests).onChange(of: requests) { _, _ in preview = nil }
   Button("Preview removal") { Task { do { await store.refresh(); preview = try JSONSerialization.jsonObject(with: await store.api("/mobile/history/preview", body: selection)) as? [String: Any]; confirmation = ""; note = "" } catch { note = error.localizedDescription } } }.disabled(store.busy || !store.online)
   if let preview {
    Text("Remove \(preview["reading"] ?? 0) finished entries, \(preview["loans"] ?? 0) returned loans and \(preview["requests"] ?? 0) closed requests. Keep \(preview["protectedLoans"] ?? 0) active loans, \(preview["protectedHolds"] ?? 0) holds and \(preview["unfinishedReading"] ?? 0) unfinished entries.")
    TextField("Type CLEAR HISTORY", text: $confirmation).autocorrectionDisabled().textInputAutocapitalization(.characters)
    Button("Clear this history permanently", role: .destructive) { Task { var input = selection; input["previewToken"] = preview["previewToken"]; input["confirmation"] = confirmation; if await store.action("clearHistory", input) { self.preview = nil; note = "Completed history cleared" } } }.disabled(confirmation != "CLEAR HISTORY" || store.busy || !store.online || store.hasPending)
   }
   if let error = store.error { Text(error).foregroundStyle(.red) }; if !note.isEmpty { Text(note) }
   Section("This device") { Button("Clear downloaded library") { clearCache = true }; Text("Pairing and server records stay. Resolve pending changes first. Refresh downloads the library again.") }
   Section("Borrower accounts") { Text("Borrowers can delete their accounts in My data & privacy on the library’s browser account page. The admin companion uses pairing."); if let deletionURL { Link("Account and data deletion help", destination: deletionURL) } }
  }.navigationTitle("Data & privacy")
  .task { if let data = try? await store.api("/mobile/setup"), let status = try? JSONSerialization.jsonObject(with: data) as? [String: Any], let origin = status["publicOrigin"] as? String, let url = URL(string: origin), url.scheme == "https", var parts = URLComponents(url: url, resolvingAgainstBaseURL: false) { parts.path = "/account/delete-help"; parts.query = nil; parts.fragment = nil; deletionURL = parts.url } }
  .confirmationDialog("Clear this device’s downloaded library? Server records and pairing stay.", isPresented: $clearCache) { Button("Clear local downloads", role: .destructive) { do { try store.clearLocalDownloads(); note = "Local downloads cleared. Refresh downloads the library again." } catch { note = error.localizedDescription } } }
 }
}
