using System.Buffers.Binary;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Data.Sqlite;
namespace HomeLibrary;
// Verification uploads never become live data. Password is framed inside the body,
// never a URL, header, persistent setting or log. Only administrators reach this service.
public sealed record BackupVerificationReport(bool Verified,int Files,long Bytes,string Version,int LibraryFormat,string CreatedUtc,string Platform,bool SameWindowsComputerRequired,bool SupportedLibraryFormat,bool Portable=false);
public sealed class BackupVerification(string data) {
 readonly SemaphoreSlim active=new(1,1);
 public const long MaxRequestBytes=CompleteBackup.MaxArchiveBytes+1028;
 public static bool IsUpload(PathString path)=>path=="/api/backup/verify"||path=="/mobile/backup/verify"||path=="/companion/api/backup/verify";
 public static bool Binary(HttpRequest request)=>request.ContentType?.Split(';')[0].Trim().Equals("application/octet-stream",StringComparison.OrdinalIgnoreCase)==true;
 public async Task<IResult> Upload(HttpContext context){
  if(!Binary(context.Request))throw new LibraryError("Choose a complete .carribrary-backup file.",415);
  if(context.Request.ContentLength>MaxRequestBytes)throw new LibraryError("The complete backup exceeds the supported verification size.",413);
  var limit=context.Features.Get<IHttpMaxRequestBodySizeFeature>();if(limit!=null&&!limit.IsReadOnly)limit.MaxRequestBodySize=MaxRequestBytes;
  if(!await active.WaitAsync(0,context.RequestAborted))throw new LibraryError("A backup is already being verified. Wait for it to finish, then retry.",409);
  string? stage=null;
  using var timeout=CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);timeout.CancelAfter(TimeSpan.FromMinutes(8));var stop=timeout.Token;
  try{
   var prefix=new byte[4];await context.Request.Body.ReadExactlyAsync(prefix,stop);var size=BinaryPrimitives.ReadInt32LittleEndian(prefix);
   if(size is <2 or >1024)throw new LibraryError("Invalid backup verification request.");
   var metadata=new byte[size];await context.Request.Body.ReadExactlyAsync(metadata,stop);
   var input=JsonSerializer.Deserialize<CompleteBackupInput>(metadata,Library.Json)??throw new LibraryError("Enter the backup password.");BackupCipher.Password(input.Password);
   stage=CompleteBackup.PrivateDirectory(data,".complete-backup-verify-");var archive=Path.Combine(stage,"upload.carribrary-backup");
   await using(var output=new FileStream(archive,FileMode.CreateNew,FileAccess.Write,FileShare.None,65536,true)){
    var buffer=new byte[65536];long bytes=0;int n;while((n=await context.Request.Body.ReadAsync(buffer,stop))>0){bytes+=n;if(bytes>CompleteBackup.MaxArchiveBytes)throw new LibraryError("The complete backup exceeds the supported verification size.",413);await output.WriteAsync(buffer.AsMemory(0,n),stop);}
   }
   var report=await Task.Run(()=>Verify(archive,input.Password,stop),stop);return Results.Json(report,Library.Json);
  }catch(EndOfStreamException){throw new LibraryError("The backup upload was incomplete. Choose the saved file and retry.");}
  catch(OperationCanceledException)when(!context.RequestAborted.IsCancellationRequested){throw new LibraryError("Verification timed out. Use the offline verification tool for this backup.",503);}
  catch(Exception error)when(error is IOException or UnauthorizedAccessException or SqliteException){throw new LibraryError("The backup could not be verified. Check server free space and permissions, or use the offline verification tool. Current library data was not changed.",503);}
  finally{try{if(stage!=null&&Directory.Exists(stage))Directory.Delete(stage,true);}finally{active.Release();}}
 }
 public static BackupVerificationReport Verify(string archive,string password,CancellationToken stop=default){
  BackupCipher.Password(password);if(new FileInfo(archive).Length>CompleteBackup.MaxArchiveBytes)throw new LibraryError("The backup exceeds the supported verification size.");
  var stage=CompleteBackup.PrivateDirectory(Path.GetDirectoryName(Path.GetFullPath(archive))!,".complete-backup-verify-");
  try{
   var m=CompleteBackupRecovery.Unpack(archive,Path.Combine(stage,"checked"),password,stop);
   return InspectRecovered(Path.Combine(stage,"checked"),m);
  }finally{if(Directory.Exists(stage))Directory.Delete(stage,true);}
 }
 public static BackupVerificationReport InspectRecovered(string recovered,CompleteManifest m){
   using(var db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=Path.Combine(recovered,"data","library.sqlite"),Mode=SqliteOpenMode.ReadOnly,Pooling=false}.ToString())){
    db.Open();using var command=db.CreateCommand();command.CommandText="SELECT data FROM library WHERE id=1";var saved=command.ExecuteScalar() as string;
    if(saved==null)throw new LibraryError("The backup library database is missing its library record.");
    using var state=JsonDocument.Parse(saved);if(!state.RootElement.TryGetProperty("version",out var format)||!format.TryGetInt32(out var actual)||actual!=m.LibraryFormat)throw new LibraryError("The backup library format does not match its manifest.");
   }
   // Return no filenames, settings, keys, borrowers or private reading data.
   return new(true,m.Files.Count,m.Files.Sum(f=>f.Length),Label(m.Version),m.LibraryFormat,Label(m.CreatedUtc),Label(m.Platform),string.Equals(m.Platform,"Windows",StringComparison.OrdinalIgnoreCase)&&m.Format==1,m.LibraryFormat is >=1 and <=6,m.Format==2);
 }
 static string Label(string value)=>new((value??"").Where(c=>!char.IsControl(c)).Take(100).ToArray());
}
