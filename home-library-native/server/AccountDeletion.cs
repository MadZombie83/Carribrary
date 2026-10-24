using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace HomeLibrary;
public sealed record AccountDeletionInput(string AccountId,bool DeleteInformation=false,string ReaderId="",string BorrowerId="",string[]? GuestRequestIds=null,bool RemoveBackups=false,string Confirmation="",string PreviewToken="");
public sealed record DeletionTarget(string Id,string Name);
public sealed record GuestDeletionCandidate(string Id,string Name,string Contact,string Title,string Status);
public sealed record AccountDeletionPreview(string AccountId,string Username,bool DeleteInformation,string PreviewToken,long Revision,DeletionTarget[] Readers,DeletionTarget[] Borrowers,int ReadingEntries,int Requests,int ReturnedLoans,int ActiveLoans,int RecoveryEntries,int StoredBackups,GuestDeletionCandidate[] GuestRequests,int Holds=0);
public sealed record AccountDeletionPlan(AccountDeletionPreview Preview,string[] ReaderIds,string[] BorrowerIds,string[] LoanIds,string[] RequestIds,string[] RecoveryIds);
public static class AccountDeletion {
 public static AccountDeletionPlan Plan(Snapshot snapshot,BorrowerAccount account,AccountDeletionInput input,IReadOnlyList<BorrowerAccount> others,int backups){
  if(input.RemoveBackups&&!input.DeleteInformation)throw new LibraryError("Choose account and personal information before removing stored backups.");
  var s=snapshot.State;var readers=new HashSet<string>();var borrowers=new HashSet<string>();
  if(input.DeleteInformation){
   var linked=s.Readers.Find(r=>r.Id==account.ReaderId);
   if(linked!=null){if(linked.BorrowerId!=account.BorrowerId)throw new LibraryError("The account's borrower link changed. Review/relink it first, or delete the sign-in account only.");readers.Add(linked.Id);if(linked.BorrowerId!="")borrowers.Add(linked.BorrowerId);}
   if(input.ReaderId!=""){if(!s.Readers.Any(r=>r.Id==input.ReaderId))throw new LibraryError("The chosen reading profile no longer exists.");readers.Add(input.ReaderId);}
   if(input.BorrowerId!=""){if(!s.Borrowers.Any(b=>b.Id==input.BorrowerId))throw new LibraryError("The chosen borrower no longer exists.");borrowers.Add(input.BorrowerId);}
   foreach(var reader in s.Readers.Where(r=>readers.Contains(r.Id)&&r.BorrowerId!=""))borrowers.Add(reader.BorrowerId);
   foreach(var reader in s.Readers.Where(r=>borrowers.Contains(r.BorrowerId)||readers.Contains(r.Id))){readers.Add(reader.Id);if(reader.BorrowerId!="")borrowers.Add(reader.BorrowerId);}
   if(others.Any(a=>a.Id!=account.Id&&(readers.Contains(a.ReaderId)||borrowers.Contains(a.BorrowerId))))throw new LibraryError("A selected borrower/profile is linked to another account. Review those links before deleting personal information.");
  }
  var loans=s.Loans.Where(l=>borrowers.Contains(l.BorrowerId)).ToArray();var loanIds=loans.Select(l=>l.Id).ToHashSet();
  var contacts=s.Borrowers.Where(b=>borrowers.Contains(b.Id)).Select(b=>BookRequests.ContactKey(b.Contact)).Append(BookRequests.ContactKey(account.Contact)).Where(k=>k!="").ToHashSet();
  var candidates=input.DeleteInformation?s.Requests.Where(r=>r.ReaderId==""&&!loanIds.Contains(r.LoanId)&&contacts.Contains(BookRequests.ContactKey(r.Contact))).ToArray():[];
  var guests=input.GuestRequestIds??[];if(guests.Length>500||guests.Distinct().Count()!=guests.Length||guests.Any(id=>!candidates.Any(r=>r.Id==id)))throw new LibraryError("Review the selected guest requests again.");
  var requests=s.Requests.Where(r=>readers.Contains(r.ReaderId)||loanIds.Contains(r.LoanId)||guests.Contains(r.Id)).Select(r=>r.Id).Order().ToArray();
  var recovery=s.RecycleBin.Where(r=>r.Borrower is {} old&&borrowers.Contains(old.Id)&&(r.PreservedForHistory||s.Borrowers.Any(b=>b.Id==old.Id&&b.Name==old.Name&&b.Contact==old.Contact))).Select(r=>r.Id).Order().ToArray();
  var fingerprint=JsonSerializer.Serialize(new{account.Id,account.Username,account.Name,account.Contact,account.Status,account.ReaderId,account.BorrowerId,snapshot.Revision,input.DeleteInformation,selectedReaderId=input.ReaderId,selectedBorrowerId=input.BorrowerId,input.RemoveBackups,guests=guests.Order(),readers=readers.Order(),borrowers=borrowers.Order()},Library.Json);
  var token=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint)));
  var preview=new AccountDeletionPreview(account.Id,account.Username,input.DeleteInformation,token,snapshot.Revision,s.Readers.Where(r=>readers.Contains(r.Id)).Select(r=>new DeletionTarget(r.Id,r.Name)).ToArray(),s.Borrowers.Where(b=>borrowers.Contains(b.Id)).Select(b=>new DeletionTarget(b.Id,b.Name)).ToArray(),s.ReadingEntries.Count(e=>readers.Contains(e.ReaderId)),requests.Length,loans.Count(l=>l.ReturnedDate!=null),loans.Count(l=>l.ReturnedDate==null),recovery.Length,backups,candidates.Select(r=>new GuestDeletionCandidate(r.Id,r.Name,r.Contact,s.Books.Find(b=>b.Id==r.CopyId)?.Title??r.CopyId,r.Status)).ToArray(),s.Holds.Count(h=>borrowers.Contains(h.BorrowerId)||requests.Contains(h.RequestId)));
  return new(preview,readers.ToArray(),borrowers.ToArray(),loanIds.ToArray(),requests,recovery);
 }
 public static void Confirm(AccountDeletionPlan plan,AccountDeletionInput input){if(!string.Equals(input.Confirmation,plan.Preview.Username,StringComparison.Ordinal))throw new LibraryError("Type the account username exactly to confirm deletion.");if(input.PreviewToken!=plan.Preview.PreviewToken)throw new LibraryError("The account or library changed. Preview the deletion again before confirming.",409);if(input.DeleteInformation&&plan.Preview.ActiveLoans>0)throw new LibraryError("Return this borrower's active loans before deleting personal information. You can delete the sign-in account only while books remain on loan.");}
 public static void Erase(LibraryState s,AccountDeletionPlan plan){s.Holds.RemoveAll(h=>plan.BorrowerIds.Contains(h.BorrowerId)||plan.RequestIds.Contains(h.RequestId));s.Requests.RemoveAll(r=>plan.RequestIds.Contains(r.Id));s.ReadingEntries.RemoveAll(r=>plan.ReaderIds.Contains(r.ReaderId));s.Readers.RemoveAll(r=>plan.ReaderIds.Contains(r.Id));s.Loans.RemoveAll(l=>plan.LoanIds.Contains(l.Id));s.Borrowers.RemoveAll(b=>plan.BorrowerIds.Contains(b.Id));s.RecycleBin.RemoveAll(r=>plan.RecoveryIds.Contains(r.Id));Library.Validate(s);}
}
