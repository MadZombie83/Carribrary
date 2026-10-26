using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace HomeLibrary;
public sealed partial class Store:IDisposable {
 readonly SqliteConnection db;readonly object gate=new();readonly string directory;readonly IDataProtector protector;
 public Store(string directory,IDataProtectionProvider protection){this.directory=directory;Directory.CreateDirectory(directory);Directory.CreateDirectory(Path.Combine(directory,"backups"));protector=protection.CreateProtector("HomeLibrary.Backup.v1");
 db=new SqliteConnection($"Data Source={Path.Combine(directory,"library.sqlite")}");db.Open();Run("PRAGMA journal_mode=WAL; PRAGMA secure_delete=ON; PRAGMA busy_timeout=5000; PRAGMA synchronous=FULL; CREATE TABLE IF NOT EXISTS library(id INTEGER PRIMARY KEY CHECK(id=1),revision INTEGER NOT NULL,data TEXT NOT NULL); CREATE TABLE IF NOT EXISTS receipts(id TEXT PRIMARY KEY,hash TEXT NOT NULL,result TEXT NOT NULL);");
 using var cmd=db.CreateCommand();cmd.CommandText="INSERT OR IGNORE INTO library VALUES(1,0,$data)";cmd.Parameters.AddWithValue("$data",JsonSerializer.Serialize(new LibraryState{Version=3,CirculationVersion=1},Library.Json));cmd.ExecuteNonQuery();UpgradeCopyIdentity();UpgradeCirculation();UpgradeCatalogV4();Read();}
 void UpgradeCopyIdentity(){lock(gate){
  Run("BEGIN IMMEDIATE");try{var before=Read();if(before.State.Version is 3 or 4){Run("COMMIT");return;}
   Backup(before.State,$"before-copy-identity-v{(before.State.Version==1?2:3)}-{DateTime.UtcNow:yyyyMMdd-HHmmss}-r{before.Revision}");
   CopyIdentity.Migrate(before.State);Library.Validate(before.State);
   using var update=db.CreateCommand();update.CommandText="UPDATE library SET revision=revision+1,data=$data WHERE id=1";update.Parameters.AddWithValue("$data",JsonSerializer.Serialize(before.State,Library.Json));update.ExecuteNonQuery();Run("COMMIT");
  }catch{Run("ROLLBACK");throw;}
 }}
 void UpgradeCirculation(){lock(gate){var before=Read();if(before.State.CirculationVersion==1)return;
  Backup(before.State,$"before-circulation-v1-{DateTime.UtcNow:yyyyMMdd-HHmmss}-r{before.Revision}");
  CirculationPolicy.Migrate(before.State);Library.Validate(before.State);
  using var update=db.CreateCommand();update.CommandText="UPDATE library SET revision=revision+1,data=$data WHERE id=1 AND revision=$revision";update.Parameters.AddWithValue("$data",JsonSerializer.Serialize(before.State,Library.Json));update.Parameters.AddWithValue("$revision",before.Revision);if(update.ExecuteNonQuery()!=1)throw new LibraryError("Library changed during migration. Restart to retry.",409);
 }}
 void UpgradeCatalogV4(){lock(gate){var snapshot=Read();if(snapshot.State.Version==4)return;Backup(snapshot.State,$"before-catalog-v4-{DateTime.UtcNow:yyyyMMdd-HHmmss}-r{snapshot.Revision}");snapshot.State.Version=4;Library.Validate(snapshot.State);using var update=db.CreateCommand();update.CommandText="UPDATE library SET data=$data WHERE id=1 AND revision=$revision";update.Parameters.AddWithValue("$data",JsonSerializer.Serialize(snapshot.State,Library.Json));update.Parameters.AddWithValue("$revision",snapshot.Revision);if(update.ExecuteNonQuery()!=1)throw new LibraryError("Library changed during format upgrade.",409);}}
 void Run(string sql){using var cmd=db.CreateCommand();cmd.CommandText=sql;cmd.ExecuteNonQuery();}
 public Snapshot Read(){lock(gate){using var cmd=db.CreateCommand();cmd.CommandText="SELECT revision,data FROM library WHERE id=1";using var reader=cmd.ExecuteReader();reader.Read();var state=JsonSerializer.Deserialize<LibraryState>(reader.GetString(1),Library.Json)??throw new LibraryError("Invalid stored library.");Library.Validate(state);return new(reader.GetInt64(0),state);}}
 public void Backup(LibraryState state,string name){lock(gate){state=Clone(state);var privateIds=PrivateReaders(state);state.Readers.RemoveAll(r=>privateIds.Contains(r.Id));state.ReadingEntries.RemoveAll(e=>privateIds.Contains(e.ReaderId));var bytes=protector.Protect(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(state,Library.Json)));var file=Path.Combine(directory,"backups",name+".hlbackup");File.WriteAllBytes(file+".tmp",bytes);File.Move(file+".tmp",file,true);}}
 public long ScheduledBackup(long previous){lock(gate){var snapshot=Read();if(snapshot.Revision!=previous)Backup(snapshot.State,$"scheduled-{DateTime.UtcNow:yyyyMMdd-HHmmss}-r{snapshot.Revision}");return snapshot.Revision;}}
 public LibraryState RestoreEncrypted(byte[] bytes)=>JsonSerializer.Deserialize<LibraryState>(protector.Unprotect(bytes),Library.Json)??throw new LibraryError("Invalid encrypted backup.");
 public object SubmitRequest(PublicBookRequest input){lock(gate){
  var data=JsonSerializer.SerializeToElement(input,Library.Json);
  var result=JsonSerializer.SerializeToElement(Apply(new("requestBook",data,Read().Revision,input.RequestId)),Library.Json);
  return result.GetProperty("result").Clone(); // Never return the private library snapshot publicly.
 }}
 public ReaderProfile AccountReader(string name,string contact,string readerId,string borrowerId,bool restoreBorrower=false,bool createBorrower=false,bool freshPrivateProfile=false){lock(gate){
  var state=Read().State;ReaderProfile? reader=null;
  if(readerId!="")reader=state.Readers.Find(r=>r.Id==readerId)??throw new LibraryError("Choose an existing reading profile.");
  if(restoreBorrower&&createBorrower)throw new LibraryError("Choose restore or replacement, not both.");
  if(reader!=null)borrowerId=createBorrower?"":borrowerId!=""?borrowerId:reader.BorrowerId;
  if(restoreBorrower&&borrowerId!=""){
   if(!state.Borrowers.Any(b=>b.Id==borrowerId))throw new LibraryError("The old borrower is unavailable; choose a replacement borrower.");
   if(state.Borrowers.Any(b=>b.Id==borrowerId&&b.Archived)){Apply(new("restoreBorrower",JsonSerializer.SerializeToElement(new{borrowerId},Library.Json),Read().Revision));state=Read().State;}
  }
  Borrower? borrower=null;
  if(borrowerId!="")borrower=state.Borrowers.Find(b=>b.Id==borrowerId&&!b.Archived)??throw new LibraryError("Choose an active borrower. Edit the reading profile's borrower link first if necessary.");
  if(reader==null&&borrower!=null&&!freshPrivateProfile)reader=state.Readers.Find(r=>r.BorrowerId==borrower.Id);
  if(reader!=null&&borrower!=null&&reader.BorrowerId==borrower.Id)return reader;
  if(reader==null){var chosen=borrower?.Name??name;if(!freshPrivateProfile&&state.Readers.Any(r=>r.Name.Trim().Equals(chosen.Trim(),StringComparison.OrdinalIgnoreCase)))throw new LibraryError("A reading profile with this name exists. Select it explicitly to link the account.");}
  JsonElement Result(string action,object input)=>JsonSerializer.SerializeToElement(Apply(new(action,JsonSerializer.SerializeToElement(input,Library.Json),Read().Revision)),Library.Json).GetProperty("result");
  if(borrower==null){var result=Result("addBorrower",new{name=reader?.Name??name,contact});borrower=JsonSerializer.Deserialize<Borrower>(result,Library.Json)!;}
  var saved=reader==null?Result("addReader",new{name=borrower.Name,borrowerId=borrower.Id,allowPrivateReplacement=freshPrivateProfile}):Result("renameReader",new{readerId=reader.Id,name=reader.Name,borrowerId=borrower.Id});return JsonSerializer.Deserialize<ReaderProfile>(saved,Library.Json)!;
 }}
 public HistoryPreview PreviewHistory(HistorySelection input){lock(gate)return HistoryTools.Plan(Read(),input).Preview;}
 public HistoryPreview PersonalHistory(string readerId,HistorySelection input){lock(gate){if(!Read().State.Readers.Any(r=>r.Id==readerId))throw new LibraryError("Reader unavailable.",401);return HistoryTools.Plan(Read(),input with{ReaderId=readerId,AllReaders=false}).Preview;}}
 public object AccountView(BorrowerAccount account){lock(gate){var reader=BorrowerAccounts.ApprovedReader(account,this);return ReadingAccess.View(Read(),reader,true);}}
 public object AccountAction(BorrowerAccount account,ActionRequest request){lock(gate){var reader=BorrowerAccounts.ApprovedReader(account,this);return PersonalAction(reader,request,true);}}
 public object AccountRequest(BorrowerAccount account,PublicBookRequest request){lock(gate){var reader=BorrowerAccounts.ApprovedReader(account,this);return PersonalRequest(reader,request);}}
 public object PersonalAction(string readerId,ActionRequest request,bool includeLoans=false){lock(gate){
  if(!Read().State.Readers.Any(r=>r.Id==readerId))throw new LibraryError("Personal reading link is unavailable.",401);
  if(request.Action=="clearHistory"){var selection=request.Input.Deserialize<HistorySelection>(Library.Json)??throw new LibraryError("Invalid history selection.");selection=selection with{ReaderId=readerId,AllReaders=false};Apply(request with{Input=JsonSerializer.SerializeToElement(selection,Library.Json),RequestId=null});return ReadingAccess.View(Read(),readerId,includeLoans);}
  if(request.Action=="requestRenewal"){if(!includeLoans)throw new LibraryError("Sign in to your borrower account to request renewal.",403);var id=request.Input.GetProperty("loanId").GetString();var owner=Read().State.Readers.Find(r=>r.Id==readerId)!;if(!Read().State.Loans.Any(l=>l.Id==id&&l.BorrowerId==owner.BorrowerId))throw new LibraryError("Loan not found.",404);Apply(request with{RequestId=null});return ReadingAccess.View(Read(),readerId,includeLoans);}
  if(request.Action=="cancelRequest"){var id=request.Input.TryGetProperty("requestId",out var item)&&item.ValueKind==JsonValueKind.String?item.GetString():null;if(!Read().State.Requests.Any(r=>r.Id==id&&r.ReaderId==readerId))throw new LibraryError("Request not found.",404);Apply(request with{RequestId=null});return ReadingAccess.View(Read(),readerId,includeLoans);}
  if(request.Action is not ("saveReading" or "removeReading"))throw new LibraryError("Only your reading entries can be changed here.",403);
  var input=JsonSerializer.Deserialize<Dictionary<string,JsonElement>>(request.Input.GetRawText(),Library.Json)??throw new LibraryError("Invalid reading input.");input["readerId"]=JsonSerializer.SerializeToElement(readerId);
  Apply(request with{Input=JsonSerializer.SerializeToElement(input,Library.Json),RequestId=null});return ReadingAccess.View(Read(),readerId,includeLoans);
 }}
 public object PersonalRequest(string readerId,PublicBookRequest input){lock(gate){
  var reader=Read().State.Readers.Find(r=>r.Id==readerId)??throw new LibraryError("Personal reading link is unavailable.",401);
  var fields=JsonSerializer.Deserialize<Dictionary<string,JsonElement>>(JsonSerializer.Serialize(input,Library.Json),Library.Json)!;fields["readerId"]=JsonSerializer.SerializeToElement(readerId);fields["name"]=JsonSerializer.SerializeToElement(reader.Name);
  var result=JsonSerializer.SerializeToElement(Apply(new("requestBook",JsonSerializer.SerializeToElement(fields,Library.Json),Read().Revision,input.RequestId)),Library.Json);return result.GetProperty("result").Clone();
 }}
 public object Apply(ActionRequest request){lock(gate){
 if(request.Revision<0)throw new LibraryError("A valid revision is required.");if(request.Input.ValueKind!=JsonValueKind.Object)throw new LibraryError("Invalid action input.");
 if(request.RequestId!=null&&!Guid.TryParse(request.RequestId,out _))throw new LibraryError("Invalid request ID.");
 var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Action+"\n"+request.Input.GetRawText())));
 Run("BEGIN IMMEDIATE");try{
 var current=Read();
 if(request.RequestId!=null){using var receipt=db.CreateCommand();receipt.CommandText="SELECT hash,result FROM receipts WHERE id=$id";receipt.Parameters.AddWithValue("$id",request.RequestId);using var r=receipt.ExecuteReader();if(r.Read()){if(r.GetString(0)!=hash)throw new LibraryError("Request ID was already used for a different change.");var result=JsonSerializer.Deserialize<JsonElement>(r.GetString(1));r.Close();Run("COMMIT");return new{state=current.State,revision=current.Revision,result};}}
 if(current.Revision!=request.Revision)throw new LibraryError("Someone else changed the library. Refresh and review the form before trying again.",409);
 var next=current.State;object actionResult;
 if(request.Action=="restore"){
  next=JsonSerializer.Deserialize<LibraryState>(request.Input,Library.Json)??throw new LibraryError("Invalid backup.");CopyIdentity.Migrate(next);next.Version=4;CirculationPolicy.Migrate(next);Library.Validate(next);Backup(current.State,$"before-restore-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-r{current.Revision}");actionResult=new{restored=true};
 }else if(request.Action=="clearHistory"){var input=JsonSerializer.Deserialize<Dictionary<string,JsonElement>>(request.Input.GetRawText(),Library.Json)!;input["serverRevision"]=JsonSerializer.SerializeToElement(current.Revision);actionResult=Library.Apply(next,request.Action,JsonSerializer.SerializeToElement(input,Library.Json));}
 else {if(request.Action=="addBooksBatch")Backup(current.State,$"before-batch-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-r{current.Revision}");actionResult=Library.Apply(next,request.Action,request.Input);}
 Library.Validate(next);var daily="daily-"+Library.Today;if(actionResult is not HistoryClearResult&&!File.Exists(Path.Combine(directory,"backups",daily+".hlbackup")))Backup(current.State,daily);
 if(actionResult is HistoryClearResult cleared)foreach(var id in PersonalReceipts(cleared.ErasedIds.ToHashSet())){using var remove=db.CreateCommand();remove.CommandText="DELETE FROM receipts WHERE id=$id";remove.Parameters.AddWithValue("$id",id);remove.ExecuteNonQuery();}
 using var update=db.CreateCommand();update.CommandText="UPDATE library SET revision=revision+1,data=$data WHERE id=1";update.Parameters.AddWithValue("$data",JsonSerializer.Serialize(next,Library.Json));update.ExecuteNonQuery();
 if(request.RequestId!=null){using var receipt=db.CreateCommand();receipt.CommandText="INSERT INTO receipts VALUES($id,$hash,$result)";receipt.Parameters.AddWithValue("$id",request.RequestId);receipt.Parameters.AddWithValue("$hash",hash);receipt.Parameters.AddWithValue("$result",JsonSerializer.Serialize(actionResult,Library.Json));receipt.ExecuteNonQuery();}
 Run("COMMIT");return new{state=next,revision=current.Revision+1,result=actionResult};
 }catch{Run("ROLLBACK");throw;}
 }}
 string[] PersonalReceipts(HashSet<string> identifiers){
  bool Matches(JsonElement value)=>value.ValueKind switch{JsonValueKind.String=>identifiers.Contains(value.GetString()??""),JsonValueKind.Object=>value.EnumerateObject().Any(p=>Matches(p.Value)),JsonValueKind.Array=>value.EnumerateArray().Any(Matches),_=>false};
  var result=new List<string>();using var cmd=db.CreateCommand();cmd.CommandText="SELECT id,result FROM receipts";using var rows=cmd.ExecuteReader();while(rows.Read()){try{using var doc=JsonDocument.Parse(rows.GetString(1));if(Matches(doc.RootElement))result.Add(rows.GetString(0));}catch(JsonException){result.Add(rows.GetString(0));}}return result.ToArray();
 }
 public string[] DeletionBackups(){lock(gate){var folders=new[]{(Path.Combine(directory,"backups"),"*.hlbackup"),(Path.Combine(directory,"backups"),"*.hlbackup.tmp"),(Path.Combine(Path.GetDirectoryName(directory)!,"upgrade-backups"),"before-update-*.zip")};return folders.Where(x=>Directory.Exists(x.Item1)&&(File.GetAttributes(x.Item1)&FileAttributes.ReparsePoint)==0).SelectMany(x=>Directory.GetFiles(x.Item1,x.Item2,SearchOption.TopDirectoryOnly)).Where(f=>(File.GetAttributes(f)&FileAttributes.ReparsePoint)==0).ToArray();}}
 public AccountDeletionPreview PreviewAccountDeletion(BorrowerAccount account,AccountDeletionInput input,IReadOnlyList<BorrowerAccount> accounts){lock(gate)return AccountDeletion.Plan(Read(),account,input,accounts,DeletionBackups().Length).Preview;}
 public (AccountDeletionPlan Plan,string[] Warnings) EraseAccountInformation(BorrowerAccount account,AccountDeletionInput input,IReadOnlyList<BorrowerAccount> accounts,Action<AccountDeletionPlan> revoke){lock(gate){
  var snapshot=Read();var plan=AccountDeletion.Plan(snapshot,account,input,accounts,DeletionBackups().Length);AccountDeletion.Confirm(plan,input);revoke(plan);var warnings=new List<string>();
  if(input.DeleteInformation){
   var state=snapshot.State;var identifiers=plan.ReaderIds.Concat(plan.BorrowerIds).Concat(plan.LoanIds).Concat(plan.RequestIds).Concat(state.ReadingEntries.Where(e=>plan.ReaderIds.Contains(e.ReaderId)).Select(e=>e.Id)).ToHashSet();var receipts=PersonalReceipts(identifiers);AccountDeletion.Erase(state,plan);Run("BEGIN IMMEDIATE");try{foreach(var id in receipts){using var remove=db.CreateCommand();remove.CommandText="DELETE FROM receipts WHERE id=$id";remove.Parameters.AddWithValue("$id",id);remove.ExecuteNonQuery();}using var cmd=db.CreateCommand();cmd.CommandText="UPDATE library SET revision=revision+1,data=$data WHERE id=1;";cmd.Parameters.AddWithValue("$data",JsonSerializer.Serialize(state,Library.Json));cmd.ExecuteNonQuery();Run("COMMIT");}catch{Run("ROLLBACK");throw;}
   // Deliberately do not create a pre-deletion backup containing the erased data.
   try{Run("PRAGMA wal_checkpoint(TRUNCATE); VACUUM; PRAGMA wal_checkpoint(TRUNCATE);");}catch(SqliteException){warnings.Add("Database compaction could not finish; the current library records were deleted.");}
  }
  if(input.RemoveBackups){foreach(var file in DeletionBackups())try{File.Delete(file);}catch(Exception error)when(error is IOException or UnauthorizedAccessException){warnings.Add("A stored application backup could not be deleted. Review the server backup folders.");}}
  return (plan,warnings.Distinct().ToArray());
 }}
 public void Import(string file,bool sqlite=false){
 if(Read().State.Books.Count>0||Read().State.Borrowers.Count>0||Read().State.Loans.Count>0||Read().State.Readers.Count>0||Read().State.ReadingEntries.Count>0)throw new LibraryError("Import into an empty installation only; use Restore backup to replace an existing library.");
 string json;if(sqlite){using var source=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=Path.GetFullPath(file),Mode=SqliteOpenMode.ReadOnly}.ToString());source.Open();using var cmd=source.CreateCommand();cmd.CommandText="SELECT data FROM library WHERE id=1";json=cmd.ExecuteScalar()?.ToString()??throw new LibraryError("No library in source database.");}else json=File.ReadAllText(file);
 using var doc=JsonDocument.Parse(json);Apply(new("restore",doc.RootElement.Clone(),Read().Revision));
 }
 public void Dispose(){lock(gate)db.Dispose();}
}
