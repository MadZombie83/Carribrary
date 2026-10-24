import SwiftUI
struct AccountDeletionScreen: View {
 @EnvironmentObject var store:LibraryStore
 let account:BorrowerLoginAccount;var onDeleted:()->Void
 @State var full=false;@State var reader="";@State var borrower="";@State var removeBackups=false;@State var guests=Set<String>();@State var preview:[String:Any]?;@State var username="";@State var busy=false;@State var note="";@State var deleted=false;@State var guestDirty=false
 var input:[String:Any]{["accountId":account.id,"deleteInformation":full,"readerId":full ? reader:"","borrowerId":full ? borrower:"","removeBackups":full && removeBackups,"guestRequestIds":full ? Array(guests):[]]}
 func rows(_ key:String)->[[String:Any]]{preview?[key] as? [[String:Any]] ?? []}
 func count(_ key:String)->Int{preview?[key] as? Int ?? 0}
 func review()async{busy=true;defer{busy=false};do{preview=try JSONSerialization.jsonObject(with:await store.api("/mobile/accounts/deletion-preview",body:input)) as? [String:Any];username="";guestDirty=false;note=""}catch{note=error.localizedDescription}}
 func erase()async{guard let preview else{return};guard !store.hasPending else{note="Resolve Retry pending change before deleting an account.";return};busy=true;defer{busy=false};do{var body=input;body["previewToken"]=preview["previewToken"];body["confirmation"]=username;let data=try await store.api("/mobile/accounts/delete",body:body);let result=try JSONSerialization.jsonObject(with:data) as? [String:Any] ?? [:];if let snapshot=result["snapshot"] {try store.acceptAccountDeletion(JSONDecoder().decode(Snapshot.self,from:JSONSerialization.data(withJSONObject:snapshot)))};note=(result["warnings"] as? [String] ?? []).joined(separator:"\n");deleted=true;onDeleted()}catch{note=error.localizedDescription;self.preview=nil}}
 var body:some View{Form{
  Text("@"+account.username+" · "+account.name)
  if !note.isEmpty{Text(note).foregroundStyle(.red)}
  if deleted{Text("Account deleted. Refresh paired devices to remove old cached information.")}else{
   Toggle("Also delete linked personal information permanently",isOn:$full).onChange(of:full){_,_ in preview=nil;guests=[]}
   Text(full ? "Reading notes/ratings, requests, returned loans and borrower recovery entries will be removed. Return active loans first. Books stay. No Recycle Bin recovery.":"Delete sign-in credentials and the encrypted private list; keep operational borrower, loans and requests. Export an encrypted personal backup first if wanted. All sign-ins and reset links end.")
   if full{
    Text("Linked records are included automatically. Add a verified profile or borrower only if it also belongs to this person, including an unlinked pending account.")
    Picker("Additional reading profile",selection:$reader){Text("None").tag("");ForEach(store.state.readers ?? []){Text($0.name).tag($0.id)}}.onChange(of:reader){_,_ in preview=nil;guests=[]}
    Picker("Additional borrower",selection:$borrower){Text("None").tag("");ForEach(store.state.borrowers){Text($0.name+" · "+$0.id).tag($0.id)}}.onChange(of:borrower){_,_ in preview=nil;guests=[]}
    Toggle("Delete ALL stored automatic/safety backups for this installation (whole library)",isOn:$removeBackups).onChange(of:removeBackups){_,_ in preview=nil}
   }
   Text("Exported backups, emails and offline devices may retain older copies. Stored backup deletion cannot recall copies elsewhere or securely erase external storage. Refresh paired devices.")
   Button("Preview deletion"){Task{await review()}}
   if preview != nil{
    Section("Review deletion"){
     if full{
      Text("Borrowers: "+rows("borrowers").map{String(describing:$0["name"] ?? "")}.joined(separator:", "))
      Text("Readers: "+rows("readers").map{String(describing:$0["name"] ?? "")}.joined(separator:", "))
      Text("\(count("readingEntries")) reading entries · \(count("requests")) requests · \(count("returnedLoans")) returned loans · \(count("recoveryEntries")) recovery entries · \(count("holds")) holds released")
      if count("activeLoans")>0{Text("Return \(count("activeLoans")) active loans first.").foregroundStyle(.red)}
      Text(removeBackups ? "Delete all \(count("storedBackups")) stored automatic/safety backups.":"Keep \(count("storedBackups")) stored backups; older information may remain in them.")
      if !rows("guestRequests").isEmpty{Text("Guest requests with matching contact: select only this person's. Shared contact does not prove ownership. Unselected requests stay.");ForEach(rows("guestRequests").indices,id:\.self){index in let row=rows("guestRequests")[index];let id=row["id"] as? String ?? "";Toggle(String(describing:row["name"] ?? "")+" · "+String(describing:row["title"] ?? ""),isOn:Binding(get:{guests.contains(id)},set:{if $0{guests.insert(id)}else{guests.remove(id)};guestDirty=true}))};Button("Update preview for selected guest requests"){Task{await review()}}}
     }
     TextField("Type username "+account.username+" to confirm",text:$username).textInputAutocapitalization(.never).autocorrectionDisabled()
     Button("Delete permanently",role:.destructive){Task{await erase()}}.disabled(username != account.username || count("activeLoans")>0 || guestDirty)
    }
   }
  }
 }.disabled(busy || !store.online).navigationTitle("Delete account")}
}
