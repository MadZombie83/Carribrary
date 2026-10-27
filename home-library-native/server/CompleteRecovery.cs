using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
namespace HomeLibrary;
public interface IRecoveryHost {
 Task Stop(CancellationToken stop);
 Task Start(CancellationToken stop);
 Task<bool> Healthy(CancellationToken stop);
 void PrepareData(string directory);
}
public sealed record RecoveryJournal(string Live,string Previous,string Failed,string Phase,string CreatedUtc);
// No live Store, credentials, notification workers or service changes are created
// by opening this engine. Live replacement always keeps the whole current folder.
public sealed class CompleteRecovery(string live,string work,IRecoveryHost host) {
 public string Live{get;}=Path.GetFullPath(live).TrimEnd(Path.DirectorySeparatorChar);
 public string Work{get;}=Path.GetFullPath(work).TrimEnd(Path.DirectorySeparatorChar);
 public const string ReviewMarker=".recovery-review-required";
 RecoveryJournal? ReadJournal(){using var input=new FileStream(JournalFile,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);return JsonSerializer.Deserialize<RecoveryJournal>(input,Library.Json);}
 public bool Completed=>File.Exists(JournalFile)&&ReadJournal()?.Phase=="complete";
 public string JournalFile=>Path.Combine(Work,"restore-journal.json");
 public RecoveryJournal? Pending(){
  if(!File.Exists(JournalFile))return null;var j=ReadJournal()??throw new LibraryError("Recovery journal is unreadable. Keep all recovery folders and ask for help.");
  if(j.Live!=Live||!Within(j.Previous,Work)||!Within(j.Failed,Work)||j.Previous==j.Failed)throw new LibraryError("Recovery journal paths are invalid. No folders were changed.");
  return j.Phase is "complete" or "undone"?null:j;
 }
 public async Task<string> Restore(string candidate,CancellationToken stop=default){
  if(Pending()!=null)throw new LibraryError("An interrupted restore needs Undo interrupted restore before another replacement.",409);
  candidate=Path.GetFullPath(candidate);if(!Within(candidate,Work)||Within(Live,candidate)||Within(candidate,Live))throw new LibraryError("Invalid recovery candidate path.");
  AssertTree(candidate);if(!File.Exists(Path.Combine(candidate,"library.sqlite")))throw new LibraryError("The recovered library database is missing.");
  var id=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N");
  var j=new RecoveryJournal(Live,Path.Combine(Work,"before-restore-"+id),Path.Combine(Work,"failed-restore-"+id),"prepared",DateTimeOffset.UtcNow.ToString("O"));
  // Cancellation is accepted before service shutdown. Once replacement begins,
  // finish/rollback even if a browser tab or upload connection goes away.
  var journalStarted=false;stop.ThrowIfCancellationRequested();await host.Stop(stop);
  try{
   if(!Directory.Exists(Live))throw new LibraryError("The current data folder is missing. No replacement was attempted.");AssertTree(Live);AssertStopped(Live);File.WriteAllText(Path.Combine(candidate,ReviewMarker),"Historical backup restored. Local administrator must review accounts/devices before remote access resumes.\n");host.PrepareData(candidate);
   Save(j with{Phase="moving-current"});journalStarted=true;Directory.Move(Live,j.Previous);
   Save(j with{Phase="installing-backup"});Directory.Move(candidate,Live);
   Save(j with{Phase="starting"});await host.Start(CancellationToken.None);
   if(!await host.Healthy(CancellationToken.None))throw new LibraryError("The recovered server did not pass its startup check.");
   Save(j with{Phase="complete"});return j.Previous;
  }catch{
   // If the journal exists, all recovery is from its checked, private paths.
   // Never delete either historical or failed data during rollback.
   if(journalStarted){try{await Undo(CancellationToken.None);}catch{throw new LibraryError("Recovery and automatic undo could not finish. All retained folders and the journal were kept. Keep the server offline and use Undo interrupted restore or ask for help.",503);}}
   else{await host.Start(CancellationToken.None);}
   throw new LibraryError("Restore did not finish. The previous data was retained and put back when replacement had begun. Check the original library before retrying.",503);
  }
 }
 public async Task RestartAfterReview(){if(File.Exists(Path.Combine(Live,ReviewMarker)))throw new LibraryError("Complete the authenticated local review first.");await host.Stop(CancellationToken.None);await host.Start(CancellationToken.None);if(!await host.Healthy(CancellationToken.None))throw new LibraryError("The reviewed server did not restart. Keep all retained data and ask for help.",503);}
 public async Task Undo(CancellationToken stop=default){
  var j=Pending()??throw new LibraryError("There is no interrupted restore to undo.");
  await host.Stop(stop);
  if(Directory.Exists(Live)){AssertTree(Live);AssertStopped(Live);}
  if(Directory.Exists(j.Previous)){
   AssertTree(j.Previous);
   // Persist intent before each move, making a second Undo safe after a crash.
   Save(j with{Phase="undo-moving-failed"});
   if(Directory.Exists(Live)){if(Directory.Exists(j.Failed))throw new LibraryError("Both current and retained failed folders exist. Keep them and ask for help.");Directory.Move(Live,j.Failed);}
   Save(j with{Phase="undo-installing-current"});Directory.Move(j.Previous,Live);
  }else if(!Directory.Exists(Live))throw new LibraryError("The retained current data folder is missing. Nothing was overwritten; ask for recovery help.");
  host.PrepareData(Live);await host.Start(CancellationToken.None);
  if(!await host.Healthy(CancellationToken.None))throw new LibraryError("The retained library did not restart. Recovery folders and journal were kept.",503);
  Save(j with{Phase="undone"});
 }
 void Save(RecoveryJournal j){Directory.CreateDirectory(Work);var path=JournalFile+".tmp";using(var output=new FileStream(path,FileMode.Create,FileAccess.Write,FileShare.None)){JsonSerializer.Serialize(output,j,Library.Json);output.Flush(true);}File.Move(path,JournalFile,true);}
 public static bool Within(string path,string parent)=>Path.GetFullPath(path).StartsWith(Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,OperatingSystem.IsWindows()?StringComparison.OrdinalIgnoreCase:StringComparison.Ordinal);
 public static void AssertTree(string path){
  if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new LibraryError("Recovery does not follow filesystem links. Use supervised recovery for this custom layout.");
  if(Directory.Exists(path))foreach(var child in Directory.EnumerateFileSystemEntries(path))AssertTree(child);
 }
 public static void AssertStopped(string data){
  foreach(var name in new[]{"local-admin.lock","desktop.lock"}){var path=Path.Combine(data,name);if(!File.Exists(path))continue;try{using var locked=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None);}catch(IOException){throw new LibraryError("Carribrary is still using this data folder. Stop it before restoring.",409);}}
 }
 public static void PauseNotifications(string data){foreach(var name in new[]{"reminder-settings.protected","web-push.protected"}){var path=Path.Combine(data,name);if(File.Exists(path))File.Move(path,path+".paused-by-recovery-"+Guid.NewGuid().ToString("N"));}}
}
