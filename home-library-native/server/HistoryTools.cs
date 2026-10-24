using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace HomeLibrary;
public sealed record HistorySelection(string ReaderId="",bool AllReaders=false,bool Reading=true,bool Loans=false,bool Requests=false,string Confirmation="",string PreviewToken="",bool HouseholdOnly=false);
public sealed record HistoryPreview(long Revision,string PreviewToken,int Reading,int Loans,int Requests,int ProtectedLoans,int ProtectedHolds,int UnfinishedReading);
public sealed record HistoryPlan(HistoryPreview Preview,string[] ReadingIds,string[] LoanIds,string[] RequestIds);
public sealed record HistoryClearResult(int Reading,int Loans,int Requests){[JsonIgnore] public string[] ErasedIds{get;init;}=[];}
public static class HistoryTools {
 public static HistoryPlan Plan(Snapshot snapshot,HistorySelection input){
  var s=snapshot.State;if(!input.Reading&&!input.Loans&&!input.Requests)throw new LibraryError("Choose at least one type of completed history.");
  var reader=input.AllReaders?null:s.Readers.Find(r=>r.Id==input.ReaderId)??throw new LibraryError("Choose a reader.");
  if(input.HouseholdOnly&&reader?.BorrowerId!=""&&reader!=null)throw new LibraryError("Borrower reading history is private.",403);
  bool VisibleReading(string id)=>!input.HouseholdOnly||s.Readers.Any(r=>r.Id==id&&r.BorrowerId=="");
  var readerId=reader?.Id??"";var borrowerId=reader?.BorrowerId??"";
  bool OwnLoan(Loan l)=>input.AllReaders||borrowerId!=""&&l.BorrowerId==borrowerId;
  var finished=s.ReadingEntries.Where(e=>VisibleReading(e.ReaderId)&&e.Status=="finished"&&(input.AllReaders||e.ReaderId==readerId)&&input.Reading).Select(e=>e.Id).ToArray();
  var loans=s.Loans.Where(l=>l.ReturnedDate!=null&&OwnLoan(l)&&input.Loans).Select(l=>l.Id).ToArray();
  var requests=s.Requests.Where(r=>!CirculationPolicy.Open(r)&&!s.Loans.Any(l=>l.Id==r.LoanId&&l.ReturnedDate==null)&&(loans.Contains(r.LoanId)||input.Requests&&(input.AllReaders||r.ReaderId==readerId||s.Loans.Any(l=>l.Id==r.LoanId&&OwnLoan(l))))).Select(r=>r.Id).ToArray();
  var fingerprint=JsonSerializer.Serialize(new{snapshot.Revision,input.ReaderId,input.AllReaders,input.Reading,input.Loans,input.Requests,input.HouseholdOnly},Library.Json);
  var token=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint)));
  return new(new(snapshot.Revision,token,finished.Length,loans.Length,requests.Length,s.Loans.Count(l=>l.ReturnedDate==null&&OwnLoan(l)),s.Holds.Count(h=>input.AllReaders||h.BorrowerId!=""&&h.BorrowerId==borrowerId||s.Requests.Any(r=>r.Id==h.RequestId&&r.ReaderId==readerId)),s.ReadingEntries.Count(e=>VisibleReading(e.ReaderId)&&e.Status!="finished"&&(input.AllReaders||e.ReaderId==readerId))),finished,loans,requests);
 }
 public static HistoryClearResult Apply(LibraryState s,JsonElement e){
  var input=e.Deserialize<HistorySelection>(Library.Json)??throw new LibraryError("Invalid history selection.");
  // Revision is supplied by Store, not trusted client input.
  if(!e.TryGetProperty("serverRevision",out var rev)||!rev.TryGetInt64(out var revision))throw new LibraryError("Preview history before clearing it.");
  var plan=Plan(new(revision,s),input);
  if(input.PreviewToken!=plan.Preview.PreviewToken)throw new LibraryError("The library or selection changed. Preview again.",409);
  if(input.Confirmation!="CLEAR HISTORY")throw new LibraryError("Type CLEAR HISTORY exactly to confirm.");
  s.ReadingEntries.RemoveAll(e=>plan.ReadingIds.Contains(e.Id));s.Requests.RemoveAll(r=>plan.RequestIds.Contains(r.Id));s.Loans.RemoveAll(l=>plan.LoanIds.Contains(l.Id));
  return new(plan.Preview.Reading,plan.Preview.Loans,plan.Preview.Requests){ErasedIds=plan.ReadingIds.Concat(plan.LoanIds).Concat(plan.RequestIds).ToArray()};
 }
}
