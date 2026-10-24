import SwiftUI
struct HelpTopic: Decodable, Identifiable { var title: String; var body: String; var id: String { title } }
struct HelpTopics: Decodable { var topics: [HelpTopic] }
struct HelpScreen: View {
 @State var search = ""
 var topics: [HelpTopic] { guard let url = Bundle.main.url(forResource: "help", withExtension: "json"), let data = try? Data(contentsOf: url), let help = try? JSONDecoder().decode(HelpTopics.self, from: data) else { return [] }; return help.topics }
 var body: some View { List(topics.filter { search.isEmpty || ($0.title + " " + $0.body).localizedCaseInsensitiveContains(search) }) { topic in DisclosureGroup(topic.title) { Text(topic.body) } }.searchable(text: $search, prompt: "Search help").navigationTitle("Carribrary help") }
}
struct BorrowerLoginAccount: Decodable, Identifiable { var id: String; var username: String; var name: String; var contact: String; var status: String; var readerId: String; var borrowerId: String }
struct LoginAccountList: Decodable { var accounts: [BorrowerLoginAccount] }
struct LoginAccountApproval: Decodable { var snapshot: Snapshot }
struct AccountRecoveryLink: Decodable { var url: String }
struct BorrowerAccountAdministration: View {
 @EnvironmentObject var store: LibraryStore
 @State var accounts: [BorrowerLoginAccount] = []; @State var choices: [String: String] = [:]; @State var note = ""; @State var busy = false; @State var reset: URL?; @State var pending: BorrowerLoginAccount?; @State var pendingAction = ""
 func load() async { do { accounts = try JSONDecoder().decode(LoginAccountList.self, from: await store.api("/mobile/accounts")).accounts } catch { note = error.localizedDescription } }
 func change(_ action: String, account: BorrowerLoginAccount) async { busy = true; defer { busy = false }; do { var input: [String: Any] = ["accountId": account.id]; if action == "approve" { let choice = choices[account.id] ?? ""; input["readerId"] = ["r:","restore:","replace:"].contains(where:{choice.hasPrefix($0)}) ? String(choice.split(separator:":",maxSplits:1).last ?? "") : ""; input["restoreBorrower"]=choice.hasPrefix("restore:"); input["createBorrower"]=choice.hasPrefix("replace:"); input["borrowerId"] = choice.hasPrefix("b:") ? String(choice.dropFirst(2)) : "" }; let data = try await store.api("/mobile/accounts/" + action, body: input); if action == "approve" { store.snapshot = try JSONDecoder().decode(LoginAccountApproval.self, from: data).snapshot }; if action == "reset" { reset = URL(string: try JSONDecoder().decode(AccountRecoveryLink.self, from: data).url) }; await load() } catch { note = error.localizedDescription } }
 var body: some View { Form {
  Text("Approve only after verifying which borrower/profile belongs to the person. This controls access to private history.")
  if !note.isEmpty { Text(note).foregroundStyle(.red) }
  if let reset { ShareLink("Share private, one-use reset link (30 minutes)", item: reset) }
  ForEach(accounts) { account in Section(account.name + " · @" + account.username) {
   Text(account.status); Text(account.contact)
   if account.status != "active" {
    Picker("Link account", selection: Binding(get: { choices[account.id] ?? "" }, set: { choices[account.id] = $0 })) {
     Text("Create new reader and borrower").tag("")
     ForEach(store.state.readers ?? []) { reader in
      if !(reader.borrowerId ?? "").isEmpty && !store.state.borrowers.contains(where:{$0.id==reader.borrowerId && $0.archived != true}) {Text("Restore borrower: "+reader.name).tag("restore:"+reader.id);Text("New borrower for reader: "+reader.name).tag("replace:"+reader.id)} else {Text("Reader: "+reader.name).tag("r:"+reader.id)}
     }
     ForEach(store.state.borrowers.filter { $0.archived != true }) { Text("Borrower: " + $0.name).tag("b:" + $0.id) }
    }
    Button(account.status == "blocked" ? "Unblock & approve" : "Approve / activate") { pending = account; pendingAction = "approve" }
   }
   NavigationLink("Delete account / personal information") {AccountDeletionScreen(account:account,onDeleted:{accounts.removeAll{$0.id==account.id};reset=nil}).environmentObject(store)}
   if account.status == "blocked" { Button("Unblock (review link next)") {pending=account;pendingAction="unblock"} }
   if account.status != "blocked" { Button("Block account", role: .destructive) { pending = account; pendingAction = "block" }; Button("Reset password") { pending = account; pendingAction = "reset" } }
  } }
 }.disabled(busy || !store.online).navigationTitle("Borrower accounts").task { await load() }.confirmationDialog("Confirm account change", isPresented: Binding(get: { pending != nil }, set: { if !$0 { pending = nil } })) { Button("Confirm") { if let account = pending { let action = pendingAction; pending = nil; Task { await change(action, account: account) } } } } message: { Text(pendingAction == "approve" ? "The selected profile grants its private reading and loan history. Verify this is the correct person." : pendingAction == "block" ? "Sign-ins and the personal reading QR will stop. History stays." : pendingAction == "unblock" ? "Unblock to pending approval. Old sessions and personal QR links stay revoked; review and approve the borrower/profile next." : "Give the one-use recovery link privately to the account holder.") } }
}
