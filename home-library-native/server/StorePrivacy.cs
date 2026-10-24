using System.Text.Json;
using System.Security.Cryptography;
namespace HomeLibrary;
public sealed partial class Store {
 static LibraryState Clone(LibraryState s)=>JsonSerializer.Deserialize<LibraryState>(JsonSerializer.Serialize(s,Library.Json),Library.Json)!;
 static HashSet<string> PrivateReaders(LibraryState s)=>s.Readers.Where(r=>r.BorrowerId!="").Select(r=>r.Id).ToHashSet();
 public HistoryPreview AdminHistoryPreview(HistorySelection input){lock(gate)return HistoryTools.Plan(Read(),input with{HouseholdOnly=true}).Preview;}
 public Snapshot AdminRead(){lock(gate){var snap=Read();var s=Clone(snap.State);var ids=PrivateReaders(s);s.Readers.RemoveAll(r=>ids.Contains(r.Id));s.ReadingEntries.RemoveAll(r=>ids.Contains(r.ReaderId));return new(snap.Revision,s);}}
 public object AdminApply(ActionRequest request){lock(gate){var current=Read().State;var ids=PrivateReaders(current);
  if(request.Input.TryGetProperty("allowPrivateReplacement",out _))throw new LibraryError("Private profile replacement is managed through account approval.",403);
  var id=request.Input.TryGetProperty("readerId",out var value)&&value.ValueKind==JsonValueKind.String?value.GetString()??"":"";
  if(request.Action is "addReader" or "renameReader" && request.Input.TryGetProperty("borrowerId",out var borrowerLink)&&borrowerLink.ValueKind==JsonValueKind.String&&borrowerLink.GetString()!="")throw new LibraryError("Create household readers here. Link private borrower profiles through account approval.",403);
  if(ids.Contains(id))throw new LibraryError("Borrower personal records are private. Manage loans and requests instead.",403);
  if(request.Action=="clearHistory"){var input=request.Input.Deserialize<HistorySelection>(Library.Json)??throw new LibraryError("Invalid history selection.");request=request with{Input=JsonSerializer.SerializeToElement(input with{HouseholdOnly=true},Library.Json)};}
  if(request.Action=="restore"){
   var next=request.Input.Deserialize<LibraryState>(Library.Json)??throw new LibraryError("Invalid backup.");
   // Restore administrative records without replacing private borrower histories.
   var incomingPrivate=PrivateReaders(next);next.Readers.RemoveAll(r=>incomingPrivate.Contains(r.Id)||ids.Contains(r.Id));next.ReadingEntries.RemoveAll(e=>incomingPrivate.Contains(e.ReaderId)||ids.Contains(e.ReaderId));
   foreach(var reader in current.Readers.Where(r=>ids.Contains(r.Id))){next.Readers.Add(reader);if(!next.Borrowers.Any(b=>b.Id==reader.BorrowerId))next.Borrowers.Add(current.Borrowers.Single(b=>b.Id==reader.BorrowerId));}
   foreach(var entry in current.ReadingEntries.Where(e=>ids.Contains(e.ReaderId))){next.ReadingEntries.Add(entry);if(!next.Books.Any(b=>b.Id==entry.CopyId)){var book=Clone(current).Books.Single(b=>b.Id==entry.CopyId);book.Archived=true;if(!next.Collections.Any(c=>c.Id==book.CollectionId))book.CollectionId="general";next.Books.Add(book);}}
   request=request with{Input=JsonSerializer.SerializeToElement(next,Library.Json)};
  }
  var result=JsonSerializer.SerializeToElement(Apply(request),Library.Json);
  return new{state=AdminRead().State,revision=Read().Revision,result=result.GetProperty("result")};
 }}
 public object LegacyView(string reader){lock(gate){var fields=JsonSerializer.SerializeToElement(ReadingAccess.View(Read(),reader,true),Library.Json).Deserialize<Dictionary<string,JsonElement>>(Library.Json)!;fields["migrationToken"]=JsonSerializer.SerializeToElement(LegacyFingerprint(reader));return fields;}}
 public string LegacyFingerprint(string reader){lock(gate)return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(Read().State.ReadingEntries.Where(e=>e.ReaderId==reader).OrderBy(e=>e.Id),Library.Json)));}
 public object SaveVault(BorrowerVault vault,string account,string reader,VaultWrite input){lock(gate){var state=Read();var hasLegacy=state.State.ReadingEntries.Any(e=>e.ReaderId==reader);if(hasLegacy&&!input.MigrateLegacy)throw new LibraryError("Review and encrypt your existing personal records first.",409);if(hasLegacy&&(input.LegacyToken!=""?input.LegacyToken!=LegacyFingerprint(reader):input.LibraryRevision!=state.Revision))throw new LibraryError("Your existing records changed. Reload before encrypting them.",409);var saved=vault.Save(account,input);if(input.MigrateLegacy)EraseLegacyReading(reader);var json=JsonSerializer.SerializeToElement(saved,Library.Json);return new{revision=json.GetProperty("revision").GetInt64(),envelope=json.GetProperty("envelope"),libraryRevision=Read().Revision};}}
 public void EraseLegacyReading(string readerId){lock(gate){var snapshot=Read();var ids=snapshot.State.ReadingEntries.Where(e=>e.ReaderId==readerId).Select(e=>e.Id).Append(readerId).ToHashSet();var receipts=PersonalReceipts(ids);snapshot.State.ReadingEntries.RemoveAll(e=>e.ReaderId==readerId);Run("BEGIN IMMEDIATE");try{foreach(var id in receipts){using var c=db.CreateCommand();c.CommandText="DELETE FROM receipts WHERE id=$id";c.Parameters.AddWithValue("$id",id);c.ExecuteNonQuery();}using var update=db.CreateCommand();update.CommandText="UPDATE library SET revision=revision+1,data=$data WHERE id=1";update.Parameters.AddWithValue("$data",JsonSerializer.Serialize(snapshot.State,Library.Json));update.ExecuteNonQuery();Run("COMMIT");}catch{Run("ROLLBACK");throw;}Run("PRAGMA wal_checkpoint(TRUNCATE); VACUUM; PRAGMA wal_checkpoint(TRUNCATE);");}}
}
