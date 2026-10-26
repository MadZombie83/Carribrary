using System.Text.Json;
namespace HomeLibrary;
public sealed class RenewalPolicy {public bool Enabled{get;set;}=true;public bool Automatic{get;set;}public int Days{get;set;}=14;public int Maximum{get;set;}=2;}
public sealed class LoanRenewal {public string Id{get;set;}="";public string Status{get;set;}="pending";public string RequestedAt{get;set;}="";public string OldDueDate{get;set;}="";public string NewDueDate{get;set;}="";public string DecidedAt{get;set;}="";}
public static class Renewals {
 public static void Validate(LibraryState s){
  var p=s.RenewalPolicy;if(p==null||p.Days is <1 or >90||p.Maximum is <1 or >20)throw new LibraryError("Renewals allow 1–90 days and 1–20 extensions per loan.");
  foreach(var loan in s.Loans){if(loan.Renewals==null||loan.Renewals.Count>200||loan.Renewals.Select(r=>r.Id).Distinct().Count()!=loan.Renewals.Count||loan.Renewals.Count(r=>r.Status=="pending")>1)throw new LibraryError("Invalid renewal history.");foreach(var r in loan.Renewals)if(!Guid.TryParse(r.Id,out _)||r.Status is not ("pending" or "approved" or "declined" or "cancelled")||!DateTimeOffset.TryParse(r.RequestedAt,out _)||!DateOnly.TryParseExact(r.OldDueDate,"yyyy-MM-dd",out _)||!DateOnly.TryParseExact(r.NewDueDate,"yyyy-MM-dd",out var next)||string.CompareOrdinal(r.NewDueDate,r.OldDueDate)<=0)throw new LibraryError("Invalid renewal record.");}
 }
 public static string? Ineligible(LibraryState s,Loan loan){
  if(!s.RenewalPolicy.Enabled)return "Renewals are disabled by the librarian.";
  if(loan.ReturnedDate!=null)return "This loan has been returned.";
  if(!DateOnly.TryParseExact(loan.DueDate,"yyyy-MM-dd",out _))return "Ask the librarian to set a due date first.";
  if(loan.Renewals.Count(r=>r.Status=="approved")>=s.RenewalPolicy.Maximum)return "This loan has reached its renewal limit.";
  if(s.Holds.Any(h=>h.CopyId==loan.CopyId)||s.Requests.Any(r=>r.CopyId==loan.CopyId&&r.Status is "pending" or "approved"))return "Another request or hold prevents renewal.";
  return null;
 }
 public static object Apply(LibraryState s,string action,JsonElement input){
  if(action=="setRenewalPolicy"){var p=input.Deserialize<RenewalPolicy>(Library.Json)??throw new LibraryError("Invalid renewal policy.");if(p.Days is <1 or >90||p.Maximum is <1 or >20)throw new LibraryError("Use 1–90 days and 1–20 renewals.");s.RenewalPolicy=p;return p;}
  var id=input.TryGetProperty("loanId",out var l)&&l.ValueKind==JsonValueKind.String?l.GetString():"";var loan=s.Loans.Find(l=>l.Id==id)??throw new LibraryError("Loan not found.",404);
  if(action=="requestRenewal"){
   if(Ineligible(s,loan) is {} reason)throw new LibraryError(reason);if(loan.Renewals.Any(r=>r.Status=="pending"))throw new LibraryError("A renewal request is already awaiting review.");
   var basis=DateOnly.ParseExact(loan.DueDate,"yyyy-MM-dd");var today=DateOnly.ParseExact(Library.Today,"yyyy-MM-dd");if(basis<today)basis=today;
   var r=new LoanRenewal{Id=Guid.NewGuid().ToString("N"),RequestedAt=DateTimeOffset.UtcNow.ToString("O"),OldDueDate=loan.DueDate,NewDueDate=basis.AddDays(s.RenewalPolicy.Days).ToString("yyyy-MM-dd")};loan.Renewals.Add(r);if(s.RenewalPolicy.Automatic){r.Status="approved";r.DecidedAt=r.RequestedAt;loan.DueDate=r.NewDueDate;}return r;
  }
  var rid=input.TryGetProperty("renewalId",out var item)?item.GetString():"";var existing=loan.Renewals.Find(r=>r.Id==rid&&r.Status=="pending")??throw new LibraryError("This renewal is no longer pending.",409);
  if(action=="approveRenewal"){if(Ineligible(s,loan) is {} reason)throw new LibraryError(reason);if(loan.DueDate!=existing.OldDueDate)throw new LibraryError("The due date changed. Decline this request and review a fresh request.",409);if(string.CompareOrdinal(existing.NewDueDate,Library.Today)<=0)throw new LibraryError("The proposed return date has passed. Decline and request again.");loan.DueDate=existing.NewDueDate;existing.Status="approved";}else existing.Status="declined";
  existing.DecidedAt=DateTimeOffset.UtcNow.ToString("O");return existing;
 }
}
