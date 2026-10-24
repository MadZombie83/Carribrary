import SwiftUI

struct DeletedLibraryRecord: Codable, Identifiable {
 var id: String; var deletedAt: String; var preservedForHistory: Bool; var book: Book?; var borrower: Borrower?
 var label: String { book?.title ?? borrower?.name ?? "Deleted record" }
}
struct RecycleBinScreen: View {
 @EnvironmentObject var store: LibraryStore
 @State var remove: DeletedLibraryRecord?
 var body: some View { List {
  Text("Restore deleted books and borrowers. If a catalog number was reused, a free replacement number is assigned. Historical records remain archived after a recovery entry is permanently removed.")
  ForEach((store.state.recycleBin ?? []).reversed()) { record in Section(record.label) {
   Text(String(record.deletedAt.prefix(10)))
   Button("Restore") { Task { _ = await store.action("restoreDeleted", ["deletedId":record.id]) } }
   Button("Delete permanently", role: .destructive) { remove = record }
  } }
  if (store.state.recycleBin ?? []).isEmpty { Text("The Recycle Bin is empty.") }
 }.navigationTitle("Recycle Bin").disabled(store.busy || !store.online).confirmationDialog("Permanently remove this recovery entry?", isPresented: Binding(get:{remove != nil},set:{if !$0 {remove=nil}})) { Button("Delete permanently",role:.destructive) { if let record=remove { remove=nil;Task {_ = await store.action("purgeDeleted",["deletedId":record.id])} } } } }
}
struct BulkBookEditor: View {
 @EnvironmentObject var store: LibraryStore
 @State var search="";@State var selected=Set<String>();@State var enabled=Set<String>();@State var values:[String:String]=[:];@State var review=false
 let fields=["collectionId","author","publisher","series"]
 var matches:[Book] {store.state.books.filter{$0.archived != true && (search.isEmpty || ($0.id+" "+$0.title+" "+$0.author+" "+($0.series ?? "")).localizedCaseInsensitiveContains(search))}.sorted{$0.title.localizedStandardCompare($1.title) == .orderedAscending}}
 var body: some View {Form {
  Text("Select up to 500 copies. Only checked fields change. Blank text clears a field; ISBN, title, copy number and loans stay as they are.")
  Text("\(selected.count) selected")
  Button("Clear selection") {selected=[]}
  Section("Choose books") {ForEach(matches) {book in Toggle(book.id+" · "+book.title,isOn:Binding(get:{selected.contains(book.id)},set:{if $0 && selected.count<500 {selected.insert(book.id)} else {selected.remove(book.id)}}))}}
  Section("Changes") {ForEach(fields,id:\.self) {field in
   Toggle("Change "+(field=="collectionId" ? "collection":field),isOn:Binding(get:{enabled.contains(field)},set:{if $0 {enabled.insert(field)}else{enabled.remove(field)}}))
   if enabled.contains(field) {if field=="collectionId" {Picker("Collection",selection:Binding(get:{values[field] ?? "general"},set:{values[field]=$0})) {ForEach(store.state.availableCollections){Text($0.name).tag($0.id)}}}else{TextField("New "+field+" (blank clears)",text:Binding(get:{values[field] ?? ""},set:{values[field]=$0}))}}
  }}
  Button("Review changes"){review=true}.disabled(selected.isEmpty || enabled.isEmpty || !store.online || store.busy)
 }.searchable(text:$search).navigationTitle("Bulk edit books").confirmationDialog("Update \(selected.count) books?",isPresented:$review){Button("Apply changes"){let changes=Dictionary(uniqueKeysWithValues:enabled.map{($0,values[$0] ?? ($0=="collectionId" ? "general":""))});Task{if await store.action("bulkEditBooks",["copyIds":Array(selected),"changes":changes]){selected=[];enabled=[]}}}} message:{Text(enabled.sorted().map{$0+": "+(values[$0] ?? ($0=="collectionId" ? "general":"(clear)"))}.joined(separator:"\n"))}}
}
struct ReminderSettingsScreen: View {
 @EnvironmentObject var store: LibraryStore
 @State var values:[String:String]=[:];@State var enabled=false;@State var password="";@State var note="";@State var history:[String]=[];@State var busy=false;@State var test=false
 let fields=["host","port","username","fromEmail","daysBefore","overdueDays","sendHour"]
 func load() async {do {let data=try await store.api("/mobile/reminders");let json=(try JSONSerialization.jsonObject(with:data)) as? [String:Any] ?? [:];enabled=json["enabled"] as? Bool ?? false;for key in fields{values[key]=json[key].map{String(describing:$0)} ?? ""};history=(json["history"] as? [[String:Any]] ?? []).map{String(describing:$0["copyId"] ?? "")+" · "+String(describing:$0["stage"] ?? "")+" · "+String(describing:$0["status"] ?? "")}} catch{note=error.localizedDescription}}
 var body:some View {Form {
  Text("Server sends one email before due, on the due date, and after the overdue threshold. Checks every 15 minutes after the start hour in server time. Enable permission under each borrower. Texts remain tap-to-send.")
  Toggle("Enable automatic email reminders",isOn:$enabled)
  ForEach(fields,id:\.self){field in TextField(field,text:Binding(get:{values[field] ?? ""},set:{values[field]=$0})).textInputAutocapitalization(.never)}
  SecureField("SMTP app password (blank keeps saved)",text:$password)
  Text("SMTP STARTTLS only (usually port 587; not 465). Days before: 1–14, overdue days: 1–30, start hour: 0–23. Credentials stay encrypted on the server and out of library exports.")
  Button("Save settings"){Task{busy=true;defer{busy=false};do{var input:[String:Any]=values;input["enabled"]=enabled;if password.isEmpty{input["password"]=NSNull()}else{input["password"]=password};for key in ["port","daysBefore","overdueDays","sendHour"]{guard let value=Int(values[key] ?? "") else{throw AppError.message("Enter a number for "+key)};input[key]=value};_ = try await store.api("/mobile/reminders",body:input);password="";note="Saved";await load()}catch{note=error.localizedDescription}}}
  Button("Send test to sending email"){test=true}
  Text(note)
  Section("Recent attempts"){Text("Sent means accepted by provider. Check-delivery may mean an interrupted send: check the mailbox before sending manually. It will not be automatically retried.");ForEach(Array(history.enumerated()),id:\.offset){Text($0.element)}}
 }.navigationTitle("Email reminders").disabled(busy || !store.online).task{await load()}.confirmationDialog("Send one test to the saved sending email address?",isPresented:$test){Button("Send test"){Task{do{_ = try await store.api("/mobile/reminders/test",body:[:]);note="Test accepted by provider; check inbox/spam."}catch{note=error.localizedDescription}}}}}
}
struct SetupChecklistScreen: View {
 @EnvironmentObject var store:LibraryStore;@State var text="";@State var note=""
 var body:some View{List {Text(text);Button("Check saved hostname DNS"){Task{do{let json=(try JSONSerialization.jsonObject(with:await store.api("/mobile/setup/dns",body:[:]))) as? [String:Any] ?? [:];note=json["message"] as? String ?? ""}catch{note=error.localizedDescription}}};Text(note);Text("Test real remote access with Wi-Fi OFF. Keep admin/internal ports private. Export a library backup before updating and install over the existing app. Full instructions are in Help.")}.navigationTitle("Setup & updates").task{do{let json=(try JSONSerialization.jsonObject(with:await store.api("/mobile/setup"))) as? [String:Any] ?? [:];text=["version","platform","remoteAddress","publicOrigin","dataDirectory","latestBackup","remoteInstructions","updateInstructions"].map{$0+": "+String(describing:json[$0] ?? "Not configured")}.joined(separator:"\n\n")}catch{note=error.localizedDescription}}}
}

struct BorrowerEditScreen:View{
 @EnvironmentObject var store:LibraryStore;@Environment(\.dismiss)var dismiss;let borrower:Borrower
 @State var name="";@State var contact="";@State var consent=false
 var body:some View{NavigationStack{Form{TextField("Name",text:$name);TextField("Phone or email",text:$contact);Toggle("Borrower agreed to automatic email reminders",isOn:$consent);Text("Email only; optional. Phone numbers keep tap-to-send.");Button("Save"){Task{if await store.action("updateBorrower",["borrowerId":borrower.id,"name":name,"contact":contact,"reminderConsent":consent]){dismiss()}}}.disabled(store.busy || !store.online);Button("Cancel"){dismiss()}}.navigationTitle("Edit borrower").onAppear{name=borrower.name;contact=borrower.contact;consent=borrower.reminderConsent ?? false}}}
}
