using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
namespace HomeLibrary;
public sealed record PortableBackupConfig(bool Enabled,string Password,string Revision);
public sealed record PortableRunInput(string? RequestId=null);
public sealed record PortableBackupSetup(bool Enabled,string Password="",string Confirmation="",bool RetentionAccepted=false);
public sealed class PortableBackups {
 readonly string folder,configPath;readonly IDataProtector protect;readonly CompleteBackup backups;readonly SemaphoreSlim gate=new(1,1);string lastError="";
 public PortableBackups(string data,IDataProtectionProvider protection,CompleteBackup backups){folder=Path.Combine(data,"portable-backups");configPath=Path.Combine(data,"portable-backup-settings.protected");protect=protection.CreateProtector("Carribrary.PortableBackupSchedule.v1");this.backups=backups;}
 PortableBackupConfig? Config(){if(!File.Exists(configPath))return null;using var file=new FileStream(configPath,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);if(file.Length>16384)throw new LibraryError("Portable backup settings are damaged.");using var bytes=new MemoryStream();file.CopyTo(bytes);return JsonSerializer.Deserialize<PortableBackupConfig>(protect.Unprotect(bytes.ToArray()),Library.Json);}
 public bool IsRunning=>gate.CurrentCount==0;
 IEnumerable<FileInfo> Files(){if(!Directory.Exists(folder))return [];CompleteRecovery.AssertTree(folder);return new DirectoryInfo(folder).GetFiles("Carribrary-portable-*.carribrary-backup").Where(f=>System.Text.RegularExpressions.Regex.IsMatch(f.Name,@"^Carribrary-portable-\d{8}-\d{6}-[a-f0-9]{32}\.carribrary-backup$"));}
 public object Status(){var c=Config();var files=Files().OrderByDescending(f=>f.LastWriteTimeUtc).ToArray();return new{enabled=c?.Enabled??false,passwordSaved=c!=null&&c.Password.Length>=12,retention=4,intervalDays=7,count=files.Length,lastSuccessUtc=files.FirstOrDefault()?.LastWriteTimeUtc.ToString("O"),nextDueUtc=files.FirstOrDefault()?.LastWriteTimeUtc.AddDays(7).ToString("O"),lastError,location="Server data folder / portable-backups. Copy a download to another device or drive."};}
 public void Configure(PortableBackupSetup input){if(!gate.Wait(0))throw new LibraryError("Wait for the portable backup to finish before changing its schedule.",409);try{
  var existing=Config();var password=input.Password??"";if(input.Enabled){if(!input.RetentionAccepted)throw new LibraryError("Confirm that only the four newest automatic portable backups are retained.");if(password==""){if(!string.IsNullOrEmpty(input.Confirmation))throw new LibraryError("Enter a new password or leave both password fields blank.");password=existing?.Password??"";}else if(password!=input.Confirmation)throw new LibraryError("Backup passwords do not match.");BackupCipher.Password(password);}
  else password=existing?.Password??"";
  var bytes=protect.Protect(JsonSerializer.SerializeToUtf8Bytes(new PortableBackupConfig(input.Enabled,password,Guid.NewGuid().ToString("N")),Library.Json));File.WriteAllBytes(configPath+".tmp",bytes);File.Move(configPath+".tmp",configPath,true);lastError="";
 }finally{gate.Release();}}
 public async Task<bool> Run(bool force,CancellationToken stop,string? requestId=null){if(!await gate.WaitAsync(0,stop)){if(force)throw new LibraryError("A portable backup is already running. Wait for it to finish.",409);return false;}string? temp=null;try{
  var c=Config();if(c is not{Enabled:true})return false;
  var identifier=Guid.NewGuid();if(!string.IsNullOrEmpty(requestId)&&!Guid.TryParse(requestId,out identifier))throw new LibraryError("Invalid backup request identifier.");
  Directory.CreateDirectory(folder);CompleteRecovery.AssertTree(folder);CompleteBackup.ProtectDirectory(folder);
  foreach(var incomplete in Directory.GetFiles(folder,"Carribrary-portable-*.carribrary-backup.tmp"))if(System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(incomplete),@"^Carribrary-portable-\d{8}-\d{6}-[a-f0-9]{32}\.carribrary-backup\.tmp$"))File.Delete(incomplete);
  // Retry failed retention before permitting another file, preventing unbounded growth.
  foreach(var old in Files().OrderByDescending(f=>f.LastWriteTimeUtc).ThenByDescending(f=>f.Name).Skip(4))old.Delete();
  if(!string.IsNullOrEmpty(requestId)&&Files().Any(f=>f.Name.EndsWith("-"+identifier.ToString("N")+".carribrary-backup",StringComparison.Ordinal)))return false;
  var latest=Files().OrderByDescending(f=>f.LastWriteTimeUtc).FirstOrDefault();if(!force&&latest!=null&&latest.LastWriteTimeUtc>DateTime.UtcNow.AddDays(-7))return false;
  var name="Carribrary-portable-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+identifier.ToString("N")+".carribrary-backup";var final=Path.Combine(folder,name);temp=final+".tmp";
  await backups.SaveScheduled(c.Password,temp,stop); // successful authenticated encryption before publishing/retention
  if(!BackupVerification.Verify(temp,c.Password,stop).Portable)throw new LibraryError("The scheduled backup did not contain portable encryption keys.");File.Move(temp,final);temp=null;lastError="";
  // Only our fixed-name managed archives are eligible; downloads/other files stay untouched.
  foreach(var old in Files().Where(f=>f.FullName!=final).OrderByDescending(f=>f.LastWriteTimeUtc).ThenByDescending(f=>f.Name).Skip(3))old.Delete();return true;
 }catch(OperationCanceledException)when(stop.IsCancellationRequested){throw;}
 catch(Exception){lastError="Automatic portable backup failed. Check storage and permissions, then retry. Existing completed archives were retained unless cleanup had already begun.";throw new LibraryError(lastError,503);}
 finally{try{if(temp!=null&&File.Exists(temp))File.Delete(temp);}finally{gate.Release();}}}
 public IResult Latest(){var f=Files().OrderByDescending(f=>f.LastWriteTimeUtc).FirstOrDefault()??throw new LibraryError("No automatic portable backup is available yet.");return Results.File(new FileStream(f.FullName,FileMode.Open,FileAccess.Read,FileShare.Read|FileShare.Delete),"application/octet-stream",f.Name);}
}
public sealed class PortableBackupWorker(PortableBackups backups,ILogger<PortableBackupWorker> logger):BackgroundService {
 protected override async Task ExecuteAsync(CancellationToken stop){using var timer=new PeriodicTimer(TimeSpan.FromMinutes(15));do{try{await backups.Run(false,stop);}catch(OperationCanceledException)when(stop.IsCancellationRequested){return;}catch{logger.LogWarning("Automatic portable backup did not complete. Review portable backup status.");}}while(await timer.WaitForNextTickAsync(stop));}
}
