using System.Text.Json;
namespace HomeLibrary;

// Authoritative reservations and limits; every client uses the same rules.
public sealed class CopyHold {
 public string Id {get;set;}="";
 public string CopyId {get;set;}="";
 public string RequestId {get;set;}="";
 public string BorrowerId {get;set;}="";
 public string Stage {get;set;}="pending";
 public string CreatedAt {get;set;}="";
}
public static class CirculationPolicy {
 public static bool Open(BookRequest r)=>r.Status is "pending" or "approved";
 public static CopyHold? Hold(LibraryState s,string copy)=>s.Holds.Find(h=>h.CopyId==Library.Copy(copy));
 public static string Availability(LibraryState s,string copy)=>Library.Active(s,copy)!=null?"on-loan":Hold(s,copy)!=null?"on-hold":"available";
 static string Field(JsonElement e,string name)=>e.TryGetProperty(name,out var x)&&x.ValueKind==JsonValueKind.String?x.GetString()??"":"";
 public static CopyHold Reserve(LibraryState s,string copy,string borrower="",string request="",string stage="approved") {
  if(!s.Books.Any(b=>b.Id==copy&&!b.Archived))throw new LibraryError("This copy is no longer listed.");
  if(Availability(s,copy)!="available")throw new LibraryError("This copy is on hold or already on loan. Refresh and choose another available copy.",409);
  var hold=new CopyHold{Id=Guid.NewGuid().ToString("N"),CopyId=copy,BorrowerId=borrower,RequestId=request,Stage=stage,CreatedAt=DateTimeOffset.UtcNow.ToString("O")};s.Holds.Add(hold);return hold;
 }
 // Old overlapping requests remain intact. Only one receives a reservation;
 // unreserved legacy requests are shown for an explicit librarian decision.
 public static bool Migrate(LibraryState s) {
  if(s.CirculationVersion==1)return false;
  if(s.CirculationVersion!=0)throw new LibraryError("This library uses a newer circulation format. Update the server before opening it.");
  Library.Validate(s);
  foreach(var r in s.Requests.Where(Open).OrderBy(r=>r.Status=="approved"?0:1).ThenBy(r=>DateTimeOffset.Parse(r.SubmittedAt)).ThenBy(r=>r.Id,StringComparer.Ordinal))
   if(s.Books.Any(b=>b.Id==r.CopyId&&!b.Archived)&&Availability(s,r.CopyId)=="available")
    Reserve(s,r.CopyId,request:r.Id,stage:r.Status=="approved"?"approved":"pending");
  s.CirculationVersion=1;return true;
 }
 public static void Validate(LibraryState s) {
  if(s.CirculationVersion is not (0 or 1)||s.MaxBatchCheckout is <1 or >100||s.MaxActiveLoans is <1 or >100||s.Holds==null||s.Holds.Any(h=>h==null))throw new LibraryError("Invalid circulation settings in backup.");
  if(s.Holds.Select(h=>h.Id).Distinct().Count()!=s.Holds.Count||s.Holds.Select(h=>h.CopyId).Distinct().Count()!=s.Holds.Count)throw new LibraryError("Duplicate copy reservations in backup.");
  foreach(var h in s.Holds){
   if(!Guid.TryParseExact(h.Id,"N",out _)||!s.Books.Any(b=>b.Id==h.CopyId&&!b.Archived)||Library.Active(s,h.CopyId)!=null||!DateTimeOffset.TryParse(h.CreatedAt,out _)||h.Stage is not ("pending" or "approved" or "ready")||h.RequestId==null||h.BorrowerId==null)throw new LibraryError("Invalid copy reservation in backup.");
   if(h.RequestId!="") {var r=s.Requests.Find(r=>r.Id==h.RequestId&&r.CopyId==h.CopyId&&Open(r));if(r==null||h.BorrowerId!=""||(r.Status=="pending")!=(h.Stage=="pending"))throw new LibraryError("Invalid request reservation in backup.");}
   else if(!s.Borrowers.Any(b=>b.Id==h.BorrowerId&&!b.Archived)||h.Stage=="pending")throw new LibraryError("Invalid borrower reservation in backup.");
  }
 }
 public static CopyHold? CheckCheckout(LibraryState s,string copy,string borrower,JsonElement input) {
  if(s.Loans.Count(l=>l.BorrowerId==borrower&&l.ReturnedDate==null)>=s.MaxActiveLoans)throw new LibraryError($"This borrower has reached the limit of {s.MaxActiveLoans} active loans. Return books or change Lending limits first.",409);
  var hold=Hold(s,copy);if(hold==null)return null;
  if(hold.Id!=Field(input,"holdId")||hold.Stage=="pending")throw new LibraryError("This copy is on hold. Approve its request, then confirm handover from Requests / holds.",409);
  if(hold.RequestId!=""&&Field(input,"requestId")!=hold.RequestId||hold.BorrowerId!=""&&hold.BorrowerId!=borrower)throw new LibraryError("This copy is held for a different borrower or request.",409);
  return hold;
 }
 public static void Complete(LibraryState s,CopyHold? hold,Loan loan) {
  if(hold==null)return;
  if(hold.RequestId!=""){var r=s.Requests.Single(r=>r.Id==hold.RequestId);r.Status="fulfilled";r.LoanId=loan.Id;}
  s.Holds.Remove(hold);
 }
 public static object Apply(LibraryState s,string action,JsonElement e) {
  if(action=="setLendingLimits") {
   int Number(string key)=>e.TryGetProperty(key,out var x)&&x.ValueKind==JsonValueKind.Number&&x.TryGetInt32(out var n)&&n is >=1 and <=100?n:throw new LibraryError("Choose limits from 1 to 100.");
   s.MaxBatchCheckout=Number("maxBatchCheckout");s.MaxActiveLoans=Number("maxActiveLoans");return new{s.MaxBatchCheckout,s.MaxActiveLoans};
  }
  if(action=="createHold") {
   var borrower=Field(e,"borrowerId");if(!s.Borrowers.Any(b=>b.Id==borrower&&!b.Archived))throw new LibraryError("Choose an active borrower to hold this copy for.");
   return Reserve(s,Library.Copy(Field(e,"copyId")),borrower);
  }
  if(action is "releaseHold" or "readyHold" or "handoverHold") {
   var hold=s.Holds.Find(h=>h.Id==Field(e,"holdId"))??throw new LibraryError("This hold has changed. Refresh and review it.",409);
   if(action=="releaseHold"){if(hold.RequestId!="")s.Requests.Single(r=>r.Id==hold.RequestId).Status="declined";s.Holds.Remove(hold);return new{released=true};}
   if(hold.Stage=="pending")throw new LibraryError("Approve the request first.");
   if(action=="readyHold"){hold.Stage="ready";return hold;}
   if(!e.TryGetProperty("handoverConfirmed",out var confirm)||confirm.ValueKind!=JsonValueKind.True)throw new LibraryError("Confirm that the borrower has received this book.");
   if(hold.RequestId!="")return BookRequests.Apply(s,"fulfillRequest",JsonSerializer.SerializeToElement(new{requestId=hold.RequestId,borrowerId=Field(e,"borrowerId"),checkoutDate=Field(e,"checkoutDate"),dueDate=Field(e,"dueDate")},Library.Json));
   return Library.Apply(s,"checkout",JsonSerializer.SerializeToElement(new{copyId=hold.CopyId,borrowerId=hold.BorrowerId,holdId=hold.Id,checkoutDate=Field(e,"checkoutDate"),dueDate=Field(e,"dueDate")},Library.Json));
  }
  if(action is not ("batchCheckout" or "batchReturn"))throw new LibraryError("Unknown circulation action.");
  // A failed item must not change even the in-memory input state. Store also
  // commits the entire batch with one revision and one idempotency receipt.
  if(!e.TryGetProperty("copyIds",out var array)||array.ValueKind!=JsonValueKind.Array)throw new LibraryError("Select physical copies first.");
  var ids=array.EnumerateArray().Select(x=>x.ValueKind==JsonValueKind.String?Library.Copy(x.GetString()):throw new LibraryError("Invalid copy ID.")).ToArray();
  if(ids.Length==0||ids.Distinct().Count()!=ids.Length)throw new LibraryError("Select at least one copy, without duplicates.");
  if(action=="batchCheckout"&&ids.Length>s.MaxBatchCheckout)throw new LibraryError($"Check out up to {s.MaxBatchCheckout} books per batch.");
  if(action=="batchCheckout"&&(!e.TryGetProperty("handoverConfirmed",out var confirmation)||confirmation.ValueKind!=JsonValueKind.True))throw new LibraryError("Confirm that the borrower has received these books.");
  var next=JsonSerializer.Deserialize<LibraryState>(JsonSerializer.Serialize(s,Library.Json),Library.Json)!;var loans=new List<Loan>();
  foreach(var id in ids)try {
   if(action=="batchReturn")loans.Add((Loan)Library.Apply(next,"returnBook",JsonSerializer.SerializeToElement(new{copyId=id},Library.Json)));
   else loans.Add((Loan)Library.Apply(next,"checkout",JsonSerializer.SerializeToElement(new{copyId=id,borrowerId=Field(e,"borrowerId"),checkoutDate=Field(e,"checkoutDate"),dueDate=Field(e,"dueDate")},Library.Json)));
  }catch(LibraryError error){throw new LibraryError($"{id}: {error.Message} No books in this batch were changed.",error.Status);}
  s.Loans=next.Loans;return new{loans};
 }
}
