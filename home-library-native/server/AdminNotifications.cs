using System.Text.Json;
namespace HomeLibrary;
// Only operational administration: never read borrower vaults or private reading records.
public sealed record AdminNotice(string Id,string Title,string Description,int Count,string Tool);
public static class AdminNotifications {
 public static List<AdminNotice> Build(LibraryState s,IEnumerable<BorrowerAccount> accounts,string today,bool recoveryReview=false,bool backupFailure=false,bool emailFailure=false,bool pushFailure=false){
  var result=new List<AdminNotice>();
  void Add(string id,string title,string text,int count,string tool){if(count>0)result.Add(new(id,title,text,count,tool));}
  Add("accounts","Account approvals","Review new accounts and confirm their borrower links.",accounts.Count(a=>a.Status=="pending"),"accounts-button");
  Add("requests","Book requests","Review borrowing requests awaiting your decision.",s.Requests.Count(r=>r.Status=="pending"),"requests");
  var loans=s.Loans.Where(l=>l.ReturnedDate==null&&s.Books.Any(b=>b.Id==l.CopyId&&!b.Archived)).ToArray();
  Add("renewals","Renewal requests","Review requests for more time before changing due dates.",loans.Sum(l=>l.Renewals.Count(r=>r.Status=="pending")),"renewals");
  Add("holds","Holds awaiting handover","A hold is not a loan. Confirm when the borrower receives the book.",s.Holds.Count(h=>h.Stage!="pending"&&!loans.Any(l=>l.CopyId==h.CopyId)),"holds");
  Add("overdue","Overdue books","Review outstanding loans and contact borrowers if needed.",loans.Count(l=>l.DueDate!=""&&string.CompareOrdinal(l.DueDate,today)<0),"active-loans");
  Add("recovery","Restored access needs review","Review restored accounts and devices before resuming remote access.",recoveryReview?1:0,"complete-recovery-review");
  Add("backup","Portable backup needs attention","Review the weekly backup status and available storage.",backupFailure?1:0,"portable-backups");
  Add("email","Email reminders need attention","Review settings and unconfirmed deliveries before retrying.",emailFailure?1:0,"reminders");
  Add("push","Notification delivery needs attention","Review device permissions and notification delivery status.",pushFailure?1:0,"push-overview");
  return result;
 }
 public static object Read(Store store,BorrowerAccounts accounts,Settings settings,PortableBackups backups,Reminders reminders,PushNotifications push){
  bool Failed(Func<object> status,Func<JsonElement,bool> check){try{return check(JsonSerializer.SerializeToElement(status(),Library.Json));}catch(LibraryError){return true;}}
  var backup=Failed(backups.Status,s=>s.GetProperty("lastError").GetString() is {Length:>0});
  var email=Failed(reminders.Status,s=>s.GetProperty("enabled").GetBoolean()&&s.GetProperty("history").EnumerateArray().Any(h=>h.GetProperty("status").GetString()!="sent"));
  var notification=Failed(push.Status,s=>s.GetProperty("history").EnumerateArray().Any(h=>(h.GetProperty("lastStatus").GetString()??"").Contains("unconfirmed",StringComparison.OrdinalIgnoreCase)));
  var library=store.Read().State;var people=accounts.List();
  var items=Build(library,people,Library.Today,settings.RecoveryReviewRequired,backup,email,notification);
  return new{items,count=items.Sum(n=>n.Count),eventIds=RequestEvents(library,people).Concat(items.Where(n=>n.Id is not ("accounts" or "requests" or "renewals")).Select(n=>n.Id+"|"+n.Count)).ToArray()};
 }
 // Stable identifiers for optional background alerts, independent of names/contact/private data.
 public static IEnumerable<string> RequestEvents(LibraryState s,IEnumerable<BorrowerAccount> accounts)=>
  s.Requests.Where(r=>r.Status=="pending").Select(r=>"request|"+r.Id)
  .Concat(accounts.Where(a=>a.Status=="pending").Select(a=>"account|"+a.Id))
  .Concat(s.Loans.Where(l=>l.ReturnedDate==null).SelectMany(l=>l.Renewals.Where(r=>r.Status=="pending").Select(r=>"renewal|"+r.Id)));
}
