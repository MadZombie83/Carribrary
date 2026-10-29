using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
namespace HomeLibrary;
public sealed record CompleteBackupInput(string Password);
public sealed record BackupEntry(string Path,long Length,string Sha256);
public sealed record CompleteManifest(int Format,string Product,string Version,int LibraryFormat,string CreatedUtc,string Platform,string KeyProtection,List<BackupEntry> Files);
public sealed class CompleteBackup(string data,Settings settings,SnapshotGate gate,IKeyManager? keys=null) {
 readonly SemaphoreSlim active=new(1,1);
 public const long MaxBytes=2L*1024*1024*1024;
 public const long MaxArchiveBytes=MaxBytes+64*1024*1024; // Current data only; historical backups are excluded.
 public async Task<IResult> Create(string password,CancellationToken stop){
  BackupCipher.Password(password);if(!await active.WaitAsync(0,stop))throw new LibraryError("A complete backup is already being prepared or downloaded. Wait for it to finish, then try again.",409);string? directory=null;var handedOff=false;
  try{
   using(var timeout=CancellationTokenSource.CreateLinkedTokenSource(stop)){
    timeout.CancelAfter(TimeSpan.FromMinutes(2));using var lease=await gate.Exclusive(timeout.Token);
    directory=await Task.Run(()=>Capture(data,settings,stop,keys),stop);
   }
   var zip=Path.Combine(directory,"snapshot.zip");var encrypted=Path.Combine(directory,"complete.carribrary-backup");
   await Task.Run(()=>{using var input=File.OpenRead(zip);using var output=File.Create(encrypted);BackupCipher.Encrypt(input,output,password,stop);},stop);File.Delete(zip);
   handedOff=true;return new Download(directory,encrypted,"Carribrary-complete-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".carribrary-backup",active);
  }catch(OperationCanceledException)when(!stop.IsCancellationRequested){if(directory!=null)Directory.Delete(directory,true);throw new LibraryError("Could not pause the server in time. Wait for current work to finish, then try again.",503);}
  catch(Exception error)when(error is IOException or UnauthorizedAccessException or SqliteException){if(directory!=null&&Directory.Exists(directory))Directory.Delete(directory,true);throw new LibraryError("The complete backup could not be created. Check free space and permissions on the server, then retry. Current library records were not changed.",503);}
  catch{if(directory!=null&&Directory.Exists(directory))Directory.Delete(directory,true);throw;}
  finally{if(!handedOff)active.Release();}
 }
 internal static string Capture(string data,Settings settings,CancellationToken stop,IKeyManager? keys=null){
  RejectLink(data);var directory=PrivateDirectory(data,".complete-backup-");
  try{
   using(var current=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=Path.Combine(data,"library.sqlite"),Mode=SqliteOpenMode.ReadOnly,Pooling=false}.ToString())){
    current.Open();using var command=current.CreateCommand();command.CommandText="SELECT data FROM library WHERE id=1";var state=JsonSerializer.Deserialize<LibraryState>((string)command.ExecuteScalar()!,Library.Json)!;var privateIds=state.Readers.Where(x=>x.BorrowerId!="").Select(x=>x.Id).ToHashSet();
    if(state.ReadingEntries.Any(x=>privateIds.Contains(x.ReaderId)))throw new LibraryError("Some borrower reading records still need private encryption setup. Ask those borrowers to finish encrypting their private lists before downloading a complete backup. Library JSON export remains available.");
   }
   var files=Files(data).ToArray();if(files.Length>10000||files.Sum(f=>new FileInfo(f).Length)>MaxBytes)throw new LibraryError("Current server data exceeds the 2 GB / 10,000-file complete-backup limit. Use a stopped-server backup instead.");
   var portable=keys==null?null:PortableKeyring.Export(keys);
   var manifest=new CompleteManifest(portable==null?1:2,"Carribrary",SetupStatus.Version,0,DateTimeOffset.UtcNow.ToString("O"),OperatingSystem.IsWindows()?"Windows":"Linux",portable!=null?PortableKeyring.Protection:OperatingSystem.IsWindows()?"Windows DPAPI: restore on the same Windows computer":"Included server keyring; borrower vault passphrases remain required",[]);
   using(var zip=new ZipArchive(File.Create(Path.Combine(directory,"snapshot.zip")),ZipArchiveMode.Create)){
    foreach(var file in files){stop.ThrowIfCancellationRequested();var relative=Path.GetRelativePath(data,file).Replace('\\','/');if(portable!=null&&relative.StartsWith("keyring/",StringComparison.Ordinal))continue;var source=file;
     if(file.EndsWith(".sqlite",StringComparison.OrdinalIgnoreCase)){
      source=Path.Combine(directory,Guid.NewGuid().ToString("N")+".sqlite");
      using(var db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=file,Mode=SqliteOpenMode.ReadOnly,Pooling=false}.ToString()))using(var target=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=source,Pooling=false}.ToString())){db.Open();target.Open();db.BackupDatabase(target);using var check=target.CreateCommand();check.CommandText="PRAGMA quick_check";if((string?)check.ExecuteScalar()!="ok")throw new LibraryError("A database snapshot failed its integrity check. No backup was downloaded.");}
     }
     Add(zip,source,"data/"+relative,manifest.Files,stop);if(source!=file)File.Delete(source);
    }
    if(portable!=null)foreach(var entry in portable){var name="data/keyring/"+entry.Key;using(var keyOutput=zip.CreateEntry(name).Open())keyOutput.Write(entry.Value);manifest.Files.Add(new(name,entry.Value.Length,Hash(entry.Value)));CryptographicOperations.ZeroMemory(entry.Value);}
    foreach(var file in Directory.GetFiles(AppContext.BaseDirectory,"appsettings*.json")){RejectLink(file);Add(zip,file,"configuration/"+Path.GetFileName(file),manifest.Files,stop);}
    var savedSettings=JsonSerializer.Deserialize<Settings>(JsonSerializer.Serialize(settings,Library.Json),Library.Json)!;savedSettings.DataDirectory=Path.GetFullPath(data);var effective=JsonSerializer.SerializeToUtf8Bytes(savedSettings,Library.Json);using(var s=zip.CreateEntry("configuration/effective-settings.json").Open())s.Write(effective);manifest.Files.Add(new("configuration/effective-settings.json",effective.Length,Hash(effective)));
    using(var db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=Path.Combine(data,"library.sqlite"),Mode=SqliteOpenMode.ReadOnly,Pooling=false}.ToString())){db.Open();using var cmd=db.CreateCommand();cmd.CommandText="SELECT data FROM library WHERE id=1";using var json=JsonDocument.Parse((string)cmd.ExecuteScalar()!);manifest=manifest with{LibraryFormat=json.RootElement.GetProperty("version").GetInt32()};}
    if(manifest.Files.Count>10000||manifest.Files.Sum(x=>x.Length)>MaxBytes||manifest.Files.Select(x=>x.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=manifest.Files.Count)throw new LibraryError("The data exceeds the complete-backup limits or contains conflicting filenames.");
    var metadata=JsonSerializer.SerializeToUtf8Bytes(manifest,Library.Json);if(metadata.Length>8*1024*1024)throw new LibraryError("The complete-backup file inventory is too large.");using var output=zip.CreateEntry("manifest.json").Open();output.Write(metadata);
   }
   return directory;
  }catch{Directory.Delete(directory,true);throw;}
 }
 public async Task SaveScheduled(string password,string destination,CancellationToken stop){var download=(Download)await Create(password,stop);try{await download.SaveNew(destination,stop);}finally{download.Dispose();}}
 static IEnumerable<string> Files(string root){
  foreach(var path in Directory.EnumerateFileSystemEntries(root)){
   var name=Path.GetFileName(path);if(name=="public-https"||name=="backups"||name=="portable-backups"||name=="recovery"||name.StartsWith(".complete-backup-",StringComparison.Ordinal)||name.EndsWith(".lock")||name.EndsWith(".tmp")||name.EndsWith(".sqlite-wal")||name.EndsWith(".sqlite-shm")||name.EndsWith(".carribrary-backup",StringComparison.OrdinalIgnoreCase)||name.EndsWith(".hlbackup",StringComparison.OrdinalIgnoreCase))continue;
   RejectLink(path);if(Directory.Exists(path)){foreach(var file in Files(path))yield return file;}else yield return path;
  }
 }
 static void RejectLink(string path){var info=new FileInfo(path);if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0||info.LinkTarget!=null)throw new LibraryError("Complete backups cannot follow filesystem links. Use a stopped-server backup for this custom data layout.");}
 internal static string PrivateDirectory(string parent,string prefix){var path=Path.Combine(parent,prefix+Guid.NewGuid().ToString("N"));if(OperatingSystem.IsWindows())Directory.CreateDirectory(path);else Directory.CreateDirectory(path,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);ProtectDirectory(path);return path;}
 internal static void ProtectDirectory(string path){
  if(!OperatingSystem.IsWindows()){File.SetUnixFileMode(path,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);return;}
  var security=new System.Security.AccessControl.DirectorySecurity();security.SetAccessRuleProtection(true,false);
  var inheritance=System.Security.AccessControl.InheritanceFlags.ContainerInherit|System.Security.AccessControl.InheritanceFlags.ObjectInherit;
  foreach(var sid in new[]{System.Security.Principal.WindowsIdentity.GetCurrent().User!,new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.BuiltinAdministratorsSid,null),new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.LocalSystemSid,null)})security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(sid,System.Security.AccessControl.FileSystemRights.FullControl,inheritance,System.Security.AccessControl.PropagationFlags.None,System.Security.AccessControl.AccessControlType.Allow));
  System.IO.FileSystemAclExtensions.SetAccessControl(new DirectoryInfo(path),security);
 }
 internal static void ValidateArchiveName(string name){
  if(string.IsNullOrEmpty(name)||name.Length>500||name.StartsWith('/')||name.Contains('\\')||name.Contains(':')||name.Split('/').Any(x=>x is "" or "." or ".."||x.TrimEnd(' ','.')!=x||x.Any(char.IsControl)||System.Text.RegularExpressions.Regex.IsMatch(x.Split('.')[0],@"^(CON|PRN|AUX|NUL|COM[1-9¹²³]|LPT[1-9¹²³])$",System.Text.RegularExpressions.RegexOptions.IgnoreCase|System.Text.RegularExpressions.RegexOptions.CultureInvariant)))throw new LibraryError("A server-data filename is not supported by complete backups.");
 }
 static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
 static void Add(ZipArchive zip,string source,string name,List<BackupEntry> entries,CancellationToken stop){
  ValidateArchiveName(name);
  using var input=new FileStream(source,FileMode.Open,FileAccess.Read,FileShare.Read);using var output=zip.CreateEntry(name,CompressionLevel.Optimal).Open();using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);var buffer=new byte[65536];long length=0;int n;
  while((n=input.Read(buffer))>0){stop.ThrowIfCancellationRequested();output.Write(buffer,0,n);hash.AppendData(buffer,0,n);length+=n;if(length>MaxBytes)throw new LibraryError("A server-data file exceeds the supported backup size.");}
  if(entries.Sum(x=>x.Length)+length>MaxBytes)throw new LibraryError("Current server data exceeds the supported 2 GB backup limit.");
  entries.Add(new(name,length,Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant()));
 }
 sealed class Download(string directory,string path,string name,SemaphoreSlim active):IResult,IDisposable {
  int disposed;
  public async Task SaveNew(string destination,CancellationToken stop){await using var input=File.OpenRead(path);await using var output=new FileStream(destination,FileMode.CreateNew,FileAccess.Write,FileShare.None);await input.CopyToAsync(output,stop);}
  public void Dispose(){if(Interlocked.Exchange(ref disposed,1)!=0)return;try{if(Directory.Exists(directory))Directory.Delete(directory,true);}finally{active.Release();}}
  public async Task ExecuteAsync(HttpContext context){try{context.Response.Headers.CacheControl="no-store";await Results.File(File.OpenRead(path),"application/octet-stream",name).ExecuteAsync(context);}finally{Dispose();}}
 }
}
