using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace HomeLibrary;
public sealed class BookRequest {
 public string ReaderId{get;set;}="";
 public string Id{get;set;}="";public string CopyId{get;set;}="";public string Name{get;set;}="";public string Contact{get;set;}="";public string Message{get;set;}="";
 public string SubmittedAt{get;set;}="";public string Status{get;set;}="pending";public string LoanId{get;set;}="";
}
public sealed record PublicBookRequest(string CopyId,string Name,string Contact,string Message,string RequestId,string ExpectedIsbn="",string ExpectedTitle="");
public static class BookRequests {
 public static string ContactKey(string contact){
  contact=contact.Trim();if(contact.Any(char.IsControl))return "";
  if(MailAddress.TryCreate(contact,out var mail)&&mail.Address.Equals(contact,StringComparison.OrdinalIgnoreCase)&&contact.Contains('@'))return mail.Address.ToLowerInvariant();
  if(Regex.IsMatch(contact,@"^\+?[0-9 ().-]+$")){var digits=Regex.Replace(contact,"[^0-9]","");if(digits.Length is >=7 and <=15)return (contact.StartsWith('+')?"+":"")+digits;}
  return "";
 }
 public static void Validate(LibraryState state){
  if(state.Requests==null||state.Requests.Any(r=>r==null)||state.Requests.Select(r=>r.Id).Distinct().Count()!=state.Requests.Count)throw new LibraryError("Invalid book requests in backup.");
  foreach(var r in state.Requests){
   if(string.IsNullOrEmpty(r.Id)||!r.Id.StartsWith("RQ-")||!Guid.TryParseExact(r.Id[3..],"D",out _)||!state.Books.Any(b=>b.Id==r.CopyId)||string.IsNullOrWhiteSpace(r.Name)||r.Name.Length>100||r.Name.Any(char.IsControl)||r.Contact==null||r.Contact.Length>200||ContactKey(r.Contact)==""||r.Message==null||r.Message.Length>1000||!DateTimeOffset.TryParse(r.SubmittedAt,out _)||r.Status is not ("pending" or "approved" or "declined" or "cancelled" or "fulfilled")||r.LoanId==null)throw new LibraryError("Invalid book requests in backup.");
   if(r.ReaderId==null||r.ReaderId!=""&&!state.Readers.Any(x=>x.Id==r.ReaderId))throw new LibraryError("Invalid request reader in backup.");
   if(r.Status=="fulfilled"&&!state.Loans.Any(l=>l.Id==r.LoanId&&l.CopyId==r.CopyId))throw new LibraryError("Invalid fulfilled request in backup.");
   if(r.Status!="fulfilled"&&r.LoanId!="")throw new LibraryError("Invalid request loan in backup.");
  }
 }
 static string Field(JsonElement input,string name)=>input.TryGetProperty(name,out var v)&&v.ValueKind==JsonValueKind.String?v.GetString()??"":"";
 public static object Apply(LibraryState state,string action,JsonElement input){
  if(action=="requestBook"){
   var copy=Library.Copy(Field(input,"copyId"));var name=Field(input,"name").Trim();var contact=Field(input,"contact").Trim();var message=Field(input,"message").Trim();var requestId=Field(input,"requestId");
   if(!Guid.TryParseExact(requestId,"D",out _))throw new LibraryError("Invalid request ID.");
   var book=state.Books.Find(b=>b.Id==copy&&!b.Archived)??throw new LibraryError("This book is no longer listed.");
   if(Library.Code(Field(input,"expectedIsbn"))!=book.Isbn||Field(input,"expectedTitle")!=book.Title)throw new LibraryError("This catalog entry changed. Refresh the shared list and review the book before requesting it.",409);
   if(name.Length is <1 or >100||name.Any(char.IsControl))throw new LibraryError("Enter your name (up to 100 characters).");
   if(contact.Length>200||ContactKey(contact)=="")throw new LibraryError("Enter one phone number or email address.");
   if(message.Length>1000)throw new LibraryError("Keep your message under 1,000 characters.");
   if(CirculationPolicy.Availability(state,copy)!="available")throw new LibraryError("This copy is on hold or on loan. Choose another available copy.",409);
   if(state.Requests.Count(r=>r.Status is "pending" or "approved")>=100)throw new LibraryError("The request inbox is full. Please contact the library directly.",503);
   var request=new BookRequest{ReaderId=Field(input,"readerId"),Id="RQ-"+requestId,CopyId=copy,Name=name,Contact=contact,Message=message,SubmittedAt=DateTimeOffset.UtcNow.ToString("O")};state.Requests.Add(request);CirculationPolicy.Reserve(state,copy,request:request.Id,stage:"pending");return new{id=request.Id,status="pending"};
  }
  var current=state.Requests.Find(r=>r.Id==Field(input,"requestId"))??throw new LibraryError("Request not found.");
  if(action=="approveRequest"){
   if(current.Status is not ("pending" or "approved"))throw new LibraryError("Only open requests can be approved.");
   if(!state.Books.Any(b=>b.Id==current.CopyId&&!b.Archived))throw new LibraryError("This book has been removed from the catalog.");
   var approvalHold=CirculationPolicy.Hold(state,current.CopyId);
   if(approvalHold==null)approvalHold=CirculationPolicy.Reserve(state,current.CopyId,request:current.Id,stage:"pending");
   if(approvalHold.RequestId!=current.Id)throw new LibraryError("Another request holds this copy. Resolve that hold first.",409);
   current.Status="approved";approvalHold.Stage="approved";return current;
  }
  if(action is "declineRequest" or "cancelRequest"){if(current.Status is not ("pending" or "approved"))throw new LibraryError("This request is already closed.");current.Status=action=="cancelRequest"?"cancelled":"declined";state.Holds.RemoveAll(h=>h.RequestId==current.Id);return current;}
  if(action!="fulfillRequest")throw new LibraryError("Unknown request action.");
  if(current.Status!="approved")throw new LibraryError("Approve this request before checking out the book.");
  var reservation=CirculationPolicy.Hold(state,current.CopyId);
  if(reservation==null||reservation.RequestId!=current.Id)throw new LibraryError("This legacy request has no reservation. Resolve competing requests, then approve it again.",409);
  var borrowerId=Field(input,"borrowerId");
  if(borrowerId==""&&current.ReaderId!=""){var linked=state.Readers.Find(r=>r.Id==current.ReaderId)?.BorrowerId;if(state.Borrowers.Any(b=>b.Id==linked&&!b.Archived))borrowerId=linked!;}
  if(borrowerId==""){
   var match=state.Borrowers.Find(b=>!b.Archived&&b.Name.Trim().Equals(current.Name,StringComparison.OrdinalIgnoreCase)&&ContactKey(b.Contact)==ContactKey(current.Contact));
   if(match==null)match=(Borrower)Library.Apply(state,"addBorrower",JsonSerializer.SerializeToElement(new{name=current.Name,contact=current.Contact},Library.Json));
   borrowerId=match.Id;
  }
  var loan=(Loan)Library.Apply(state,"checkout",JsonSerializer.SerializeToElement(new{copyId=current.CopyId,borrowerId,holdId=reservation.Id,requestId=current.Id,checkoutDate=Field(input,"checkoutDate"),dueDate=Field(input,"dueDate")},Library.Json));
  current.Status="fulfilled";current.LoanId=loan.Id;return loan;
 }
}
