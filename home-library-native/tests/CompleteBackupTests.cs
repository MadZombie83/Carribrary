using HomeLibrary;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.DataProtection;
using System.IO.Compression;
using System.Text.Json;
public static class CompleteBackupTests {
 const string Password="A private test backup password";
 static void Check(bool value,string message){if(!value)throw new Exception("Complete backup: "+message);Console.WriteLine("PASS "+message);}
 public static async Task Run(string root,IDataProtectionProvider protection){
  Directory.CreateDirectory(root);var data=Path.Combine(root,"data");Directory.CreateDirectory(data);var snapshots=new SnapshotGate();
  // A writer waits for all current operations, then blocks new operations.
  var reader=await snapshots.Shared();var entered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
  var writer=Task.Run(async()=>{using var lease=await snapshots.Exclusive();entered.SetResult();await release.Task;});
  await Task.Delay(30);Check(!entered.Task.IsCompleted,"complete snapshot waits for in-flight work");reader.Dispose();await entered.Task;
  var later=snapshots.Shared().AsTask();Check(!later.IsCompleted,"complete snapshot pauses incoming work");release.SetResult();await writer;using(await later){}
  var payload=System.Security.Cryptography.RandomNumberGenerator.GetBytes(200000);using(var plain=new MemoryStream(payload))using(var encrypted=new MemoryStream()){BackupCipher.Encrypt(plain,encrypted,Password);encrypted.Position=0;using var recovered=new MemoryStream();BackupCipher.Decrypt(encrypted,recovered,Password);Check(payload.SequenceEqual(recovered.ToArray()),"streaming encryption roundtrips multiple authenticated chunks");}
  using(var store=new Store(data,protection)){
   var input=JsonSerializer.SerializeToElement(new{isbn="9780140328721",title="Snapshot book",author="Author"},Library.Json);store.Apply(new("addBook",input,store.Read().Revision));
   // Keep WAL connections open: copying only the database file would lose this row.
   using var accounts=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=Path.Combine(data,"borrower-accounts.sqlite"),Pooling=false}.ToString());accounts.Open();using(var cmd=accounts.CreateCommand()){cmd.CommandText="PRAGMA journal_mode=WAL;CREATE TABLE test_secret(value TEXT);INSERT INTO test_secret VALUES('CURRENT ACCOUNT SECRET')";cmd.ExecuteNonQuery();}
   Directory.CreateDirectory(Path.Combine(data,"keyring"));File.WriteAllText(Path.Combine(data,"keyring/key.xml"),"CURRENT KEYRING SECRET");File.WriteAllBytes(Path.Combine(data,"server.protected"),[1,2,3,4]);Directory.CreateDirectory(Path.Combine(data,"backups"));File.WriteAllText(Path.Combine(data,"backups/old.hlbackup"),"HISTORICAL SECRET");
   var temporary=CompleteBackup.Capture(data,new Settings{AdminPort=19473},default);
   var archive=Path.Combine(root,"complete.carribrary-backup");using(var plain=File.OpenRead(Path.Combine(temporary,"snapshot.zip")))using(var output=File.Create(archive))BackupCipher.Encrypt(plain,output,Password);
   Directory.Delete(temporary,true);
   var recovered=Path.Combine(root,"recovered");var manifest=CompleteBackupRecovery.Unpack(archive,recovered,Password);
   Check(manifest.LibraryFormat==7&&manifest.Files.Any(f=>f.Path=="data/library.sqlite")&&manifest.Files.Any(f=>f.Path=="data/server.protected"),"complete data inventory includes library and identity keys");
   Check(!Directory.Exists(Path.Combine(recovered,"data/backups"))&&!manifest.Files.Any(f=>f.Path.EndsWith("-wal")||f.Path.EndsWith("-shm")||f.Path.Contains(".complete-backup-")),"history, transient WAL files and staging folders are excluded");
   Check(File.ReadAllText(Path.Combine(recovered,"data/keyring/key.xml"))=="CURRENT KEYRING SECRET","full keyring is preserved");
   using(var restored=new SqliteConnection($"Data Source={Path.Combine(recovered,"data/borrower-accounts.sqlite")}")){restored.Open();using var cmd=restored.CreateCommand();cmd.CommandText="SELECT value FROM test_secret";Check((string?)cmd.ExecuteScalar()=="CURRENT ACCOUNT SECRET","online SQLite snapshot includes committed WAL account data");}
   Check(!System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(archive)).Contains("CURRENT ACCOUNT SECRET"),"downloaded archive does not expose plaintext credentials");
   Reject(()=>CompleteBackupRecovery.Unpack(archive,Path.Combine(root,"wrong-password"),"wrong password value"),"wrong backup password rejected without publishing recovered data");
   Check(!Directory.Exists(Path.Combine(root,"wrong-password")),"failed recovery leaves no destination or plaintext stage");
   Reject(()=>CompleteBackupRecovery.Unpack(archive,recovered,Password),"recovery never overwrites existing data");
   var bytes=File.ReadAllBytes(archive);var damaged=Path.Combine(root,"damaged.carribrary-backup");bytes[^20]^=1;File.WriteAllBytes(damaged,bytes);Reject(()=>CompleteBackupRecovery.Unpack(damaged,Path.Combine(root,"tampered"),Password),"tampered encrypted backup rejected");
   bytes=File.ReadAllBytes(archive);bytes[100]^=1;File.WriteAllBytes(damaged,bytes);Reject(()=>CompleteBackupRecovery.Unpack(damaged,Path.Combine(root,"ciphertext-tampered"),Password),"modified ciphertext fails authentication");
   File.WriteAllBytes(damaged,File.ReadAllBytes(archive)[..^20]);Reject(()=>CompleteBackupRecovery.Unpack(damaged,Path.Combine(root,"truncated"),Password),"truncated encrypted backup rejected");
   var service=new CompleteBackup(data,new Settings(),snapshots);var first=await service.Create(Password,default);
   try{await service.Create(Password,default);throw new Exception("Overlapping backup accepted");}catch(LibraryError error){Check(error.Status==409,"only one complete backup may be prepared/downloaded at a time");}finally{((IDisposable)first).Dispose();}
   var retry=await service.Create(Password,default);((IDisposable)retry).Dispose();Check(!Directory.GetDirectories(data,".complete-backup-*").Any(),"completed/disposed backup releases capacity and removes private staging");
   var change=(string action,object value)=>JsonSerializer.SerializeToElement(store.Apply(new(action,JsonSerializer.SerializeToElement(value,Library.Json),store.Read().Revision)),Library.Json).GetProperty("result");
   change("addBorrower",new{name="Private borrower"});var readerId=change("addReader",new{name="Private reader",borrowerId="BR-0001"}).GetProperty("id").GetString()!;
   change("saveReading",new{readerId,copyId="BK-0001",status="reading",notes="LEGACY PRIVATE READING SECRET"});
   Reject(()=>CompleteBackup.Capture(data,new Settings(),default),"downloaded complete backups never expose unmigrated private reading plaintext");
   var unsafeZip=new MemoryStream();using(var zip=new ZipArchive(unsafeZip,ZipArchiveMode.Create,true)){using var file=zip.CreateEntry("../escape").Open();file.WriteByte(1);}unsafeZip.Position=0;using(var output=File.Create(damaged))BackupCipher.Encrypt(unsafeZip,output,Password);Reject(()=>CompleteBackupRecovery.Unpack(damaged,Path.Combine(root,"unsafe"),Password),"archive path traversal rejected after authentication");
  }
  using(var canceled=new CancellationTokenSource()){canceled.Cancel();try{using var lease=await snapshots.Exclusive(canceled.Token);throw new Exception("Cancellation ignored");}catch(OperationCanceledException){}}
  using(await snapshots.Shared()){}Check(true,"snapshot gate remains usable after cancellation");
 }
 static void Reject(Action action,string message){try{action();throw new Exception("Expected rejection: "+message);}catch(LibraryError){Check(true,message);}}
}
