using System.Text.Json;
using System.Text.RegularExpressions;
namespace HomeLibrary;
public sealed class SelfServiceSettings {
 public bool Checkout{get;set;}public bool Return{get;set;}public bool Kiosk{get;set;}
 public int LoanDays{get;set;}=14;public int IdleSeconds{get;set;}=90;
 public string ReturnInstructions{get;set;}="Place the book in the library’s designated return area. A librarian will confirm receipt.";
}
public sealed record SelfServiceInput(string Code,string RequestId,string CopyUid="");
public static class SelfService {
 public static bool Pending(Loan loan)=>loan.ReturnedDate==null&&loan.ReturnRequestedAt!="";
 public static void Validate(LibraryState s){if(s.SelfService==null||s.SelfService.LoanDays is <1 or >365||s.SelfService.IdleSeconds is <30 or >300||s.SelfService.ReturnInstructions==null||s.SelfService.ReturnInstructions.Length>1000)throw new LibraryError("Invalid self-service settings.");foreach(var l in s.Loans)if(l.Source is not ("librarian" or "borrower-pwa" or "kiosk")||l.ReturnSource is not ("" or "librarian")||l.ReturnRequestedSource is not ("" or "borrower-pwa" or "kiosk")||l.ReturnRequestedAt!=""&&!DateTimeOffset.TryParse(l.ReturnRequestedAt,out _)||l.KioskId!=""&&!Guid.TryParseExact(l.KioskId,"N",out _))throw new LibraryError("Invalid circulation audit information.");}
 public static Borrower Borrower(LibraryState s,string id)=>s.Borrowers.Find(b=>b.Id==id&&!b.Archived)??throw new LibraryError("Your borrower record is unavailable. Ask the librarian to check your link.",403);
 public static void Allowed(LibraryState s,bool returning,string source){if(source is not ("borrower-pwa" or "kiosk")||returning&&!s.SelfService.Return||!returning&&!s.SelfService.Checkout||source=="kiosk"&&!s.SelfService.Kiosk)throw new LibraryError("The librarian has disabled this self-service operation.",403);}
 static bool OwnHold(LibraryState s,CopyHold h,string borrower)=>h.Stage!="pending"&&(h.BorrowerId==borrower||h.RequestId!=""&&s.Requests.Any(r=>r.Id==h.RequestId&&r.ReaderId!=""&&s.Readers.Any(p=>p.Id==r.ReaderId&&p.BorrowerId==borrower)));
 public static Book[] Match(LibraryState s,string borrower,string raw,bool returning,string source){
  Allowed(s,returning,source);Borrower(s,borrower);raw=(raw??"").Trim();if(raw.Length is <1 or >300)throw new LibraryError("Scan a book label, ISBN or enter a title/copy ID.");
  var reference=Regex.Match(raw,@"^carribrary:copy:v1:([a-f0-9]{32})$",RegexOptions.IgnoreCase);var copy=Library.Copy(raw);var code=Library.Code(raw);var numeric=Regex.IsMatch(code,@"^[\dX]+$");
  var matches=s.Books.Where(b=>!b.Archived).Where(b=>reference.Success?b.CopyUid==reference.Groups[1].Value.ToLowerInvariant():raw.StartsWith("carribrary:",StringComparison.OrdinalIgnoreCase)?false:copy.StartsWith("BK-")?b.Id==copy:numeric?Library.ValidCode(code)&&Library.Edition(b.Isbn)==Library.Edition(code):(b.Title+" "+b.Author+" "+b.Series).Contains(raw,StringComparison.OrdinalIgnoreCase));
  return matches.Where(b=>returning?Library.Active(s,b.Id) is{} loan&&loan.BorrowerId==borrower:Library.Active(s,b.Id)==null&&(CirculationPolicy.Hold(s,b.Id) is not{} hold||OwnHold(s,hold,borrower))).Take(100).ToArray();
 }
 public static object Preview(LibraryState s,string borrower,string code,bool returning,string source){var books=Match(s,borrower,code,returning,source);return new{copies=books.Select(b=>new{b.Id,b.CopyUid,b.Title,b.Author,b.CoverUrl,availability=returning?"on-loan":"available",pendingReturn=Library.Active(s,b.Id) is{} l&&Pending(l),dueDate=returning?Library.Active(s,b.Id)?.DueDate:DateOnly.Parse(Library.Today).AddDays(s.SelfService.LoanDays).ToString("yyyy-MM-dd")}),returnInstructions=s.SelfService.ReturnInstructions};}
 public static Loan Circulate(LibraryState s,string borrower,SelfServiceInput input,bool returning,string source,string kiosk){
  Allowed(s,returning,source);Borrower(s,borrower);var choices=Match(s,borrower,input.Code,returning,source);var book=choices.SingleOrDefault(b=>b.CopyUid==input.CopyUid)??throw new LibraryError("Choose the physical copy again; its availability may have changed.",409);
  if(returning){var loan=Library.Active(s,book.Id)!;if(loan.ReturnRequestedAt==""){loan.ReturnRequestedAt=DateTimeOffset.UtcNow.ToString("O");loan.ReturnRequestedSource=source;loan.ReturnKioskId=kiosk;}return loan;}
  var hold=CirculationPolicy.Hold(s,book.Id);var result=(Loan)Library.Apply(s,"checkout",JsonSerializer.SerializeToElement(new{copyId=book.Id,borrowerId=borrower,checkoutDate=Library.Today,dueDate=DateOnly.Parse(Library.Today).AddDays(s.SelfService.LoanDays).ToString("yyyy-MM-dd"),holdId=hold?.Id??"",requestId=hold?.RequestId??""},Library.Json));result.Source=source;result.KioskId=kiosk;return result;
 }
}
public sealed partial class Store {
 internal T Locked<T>(Func<LibraryState,T> run){lock(gate)return run(Read().State);}
 public object SelfPreview(string borrower,string code,bool returning,string source)=>Locked(s=>SelfService.Preview(s,borrower,code,returning,source));
 public object SelfCirculate(string borrower,SelfServiceInput input,bool returning,string source,string kiosk=""){lock(gate){var state=Read();SelfService.Allowed(state.State,returning,source);SelfService.Borrower(state.State,borrower);if(!Guid.TryParseExact(input.RequestId,"D",out _))throw new LibraryError("A unique transaction ID is required.");var data=JsonSerializer.SerializeToElement(new{borrower,input.Code,input.CopyUid,returning,source,kiosk},Library.Json);var snapshot=JsonSerializer.SerializeToElement(ApplyCore(new("self-service",data,state.Revision,input.RequestId),s=>SelfService.Circulate(s,borrower,input,returning,source,kiosk)),Library.Json);return new{loan=snapshot.GetProperty("result").Clone(),returnInstructions=state.State.SelfService.ReturnInstructions};}}
}
