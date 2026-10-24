using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
namespace HomeLibrary;
// Offline utility. Never replaces a live data folder: unpack into a NEW folder,
// validate everything, then follow the stopped-server recovery instructions.
public static class CompleteBackupRecovery {
 public static CompleteManifest Unpack(string file,string destination,string password,CancellationToken stop=default){
  destination=Path.GetFullPath(destination);if(Directory.Exists(destination)||File.Exists(destination))throw new LibraryError("Choose a new recovery folder that does not already exist. Existing library data is never overwritten.");
  var parent=Path.GetDirectoryName(destination)!;if(!Directory.Exists(parent))throw new LibraryError("Create the recovery folder's parent directory first.");
  var stage=CompleteBackup.PrivateDirectory(parent,".carribrary-unpack-");
  try{
   var zipFile=Path.Combine(stage,"snapshot.zip");using(var input=File.OpenRead(file))using(var output=File.Create(zipFile))BackupCipher.Decrypt(input,output,password,stop);
   var unpacked=Path.Combine(stage,"recovered");Directory.CreateDirectory(unpacked);
   CompleteManifest manifest;
   using(var zip=ZipFile.OpenRead(zipFile)){
    var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach(var e in zip.Entries){var path=e.FullName;CompleteBackup.ValidateArchiveName(path);if(path.Length>500||path.StartsWith('/')||path.Contains('\\')||path.Contains(':')||path.Split('/').Any(x=>x is "" or "." or ".."||x.Any(char.IsControl))||!names.Add(path)||((e.ExternalAttributes>>16)&61440)==40960)throw new LibraryError("The backup contains unsafe or duplicate filenames.");}
    var metadata=zip.GetEntry("manifest.json")??throw new LibraryError("The complete backup manifest is missing.");if(metadata.Length>8*1024*1024)throw new LibraryError("The complete backup manifest is too large.");
    using(var input=metadata.Open())using(var buffer=new MemoryStream()){var chunk=new byte[65536];int n;while((n=input.Read(chunk))>0){if(buffer.Length+n>8*1024*1024)throw new LibraryError("The complete backup manifest is too large.");buffer.Write(chunk,0,n);}manifest=JsonSerializer.Deserialize<CompleteManifest>(buffer.ToArray(),Library.Json)??throw new LibraryError("Invalid complete backup manifest.");}
    if(manifest.Format!=1||manifest.Product!="Carribrary"||manifest.Files==null||manifest.Files.Count is <1 or >10000||manifest.Files.Any(x=>x==null||string.IsNullOrEmpty(x.Path)||x.Length<0||x.Length>CompleteBackup.MaxBytes||x.Sha256==null||x.Sha256.Length!=64)||manifest.Files.Sum(x=>x.Length)>CompleteBackup.MaxBytes||manifest.Files.Select(x=>x.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=manifest.Files.Count||zip.Entries.Count!=manifest.Files.Count+1)throw new LibraryError("Invalid complete backup file inventory.");
    if(!manifest.Files.Any(x=>x.Path=="data/library.sqlite"))throw new LibraryError("The backup has no library database.");
    foreach(var fileEntry in manifest.Files){
     stop.ThrowIfCancellationRequested();if(!fileEntry.Path.StartsWith("data/",StringComparison.Ordinal)&&!fileEntry.Path.StartsWith("configuration/",StringComparison.Ordinal))throw new LibraryError("Unexpected complete-backup contents.");
     var entry=zip.GetEntry(fileEntry.Path)??throw new LibraryError("A complete-backup file is missing.");if(entry.Length!=fileEntry.Length)throw new LibraryError("A complete-backup file has the wrong length.");
     var target=Path.GetFullPath(Path.Combine(unpacked,fileEntry.Path));if(!target.StartsWith(unpacked+Path.DirectorySeparatorChar,StringComparison.Ordinal))throw new LibraryError("Unsafe recovery path.");Directory.CreateDirectory(Path.GetDirectoryName(target)!);
     using(var source=entry.Open())using(var output=File.Create(target)){var buffer=new byte[65536];long written=0;int n;while((n=source.Read(buffer))>0){stop.ThrowIfCancellationRequested();written+=n;if(written>fileEntry.Length)throw new LibraryError("A complete-backup file expands beyond its declared length.");output.Write(buffer,0,n);}if(written!=fileEntry.Length)throw new LibraryError("A complete-backup file is incomplete.");}
     using(var source=File.OpenRead(target))if(Convert.ToHexString(SHA256.HashData(source)).ToLowerInvariant()!=fileEntry.Sha256)throw new LibraryError("A complete-backup file failed its integrity check.");
     if(target.EndsWith(".sqlite",StringComparison.OrdinalIgnoreCase)){using var db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=target,Mode=SqliteOpenMode.ReadOnly,Pooling=false}.ToString());db.Open();using var check=db.CreateCommand();check.CommandText="PRAGMA quick_check";if((string?)check.ExecuteScalar()!="ok")throw new LibraryError("A recovered database failed its integrity check.");}
    }
   }
   File.WriteAllText(Path.Combine(unpacked,"manifest.json"),JsonSerializer.Serialize(manifest,Library.Json));
   File.WriteAllText(Path.Combine(unpacked,"RECOVERY-READ-ME.txt"),"CARRIBRARY COMPLETE SERVER DATA BACKUP\n\nStop Carribrary before restoring. Keep a separate copy of the current data folder. Restore the entire recovered data folder together, including all databases and keyrings; never merge individual databases into a running library. Configuration files are supplied separately under configuration. Restore only to a compatible Carribrary version. Windows DPAPI-protected keys/settings require the same Windows computer; this archive is not a guaranteed cross-machine or cross-OS migration. External HTTPS proxy/certificates, system service/task definitions and environment-provided secrets are separate. This utility only unpacks into a new folder and never modifies the live server. Ask for recovery help if unsure.\n\nBorrower personal vaults remain encrypted and still need each borrower's private passphrase or recovery code. Restoring an earlier complete snapshot rewinds account deletions, passwords, device revocations and lending history. Review accounts and paired devices before making the restored server reachable. Compatible program-only recovery keeps current data instead. Historical copies can contain deleted accounts/data. Keep this recovered folder private and delete it when no longer needed.\n");
   CompleteBackup.ProtectDirectory(unpacked);Directory.Move(unpacked,destination);return manifest;
  }catch(InvalidDataException){throw new LibraryError("The decrypted backup archive is damaged.");}
  finally{if(Directory.Exists(stage))Directory.Delete(stage,true);}
 }
 public static void Command(string file,string destination){
  Console.Write("Backup password: ");string password;
  if(Console.IsInputRedirected)password=Console.ReadLine()??"";
  else{var text=new System.Text.StringBuilder();while(true){var key=Console.ReadKey(true);if(key.Key==ConsoleKey.Enter)break;if(key.Key==ConsoleKey.Backspace){if(text.Length>0)text.Length--;}else if(!char.IsControl(key.KeyChar))text.Append(key.KeyChar);}Console.WriteLine();password=text.ToString();}
  var manifest=Unpack(file,destination,password);Console.WriteLine($"Verified {manifest.Files.Count} files. Recovered into {Path.GetFullPath(destination)}. Read RECOVERY-READ-ME.txt before restoring a stopped server.");
 }
}
