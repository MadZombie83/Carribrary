using HomeLibrary;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Buffers.Binary;
using System.Text.Json;
public static class BackupVerificationTests {
 const string Password="Private verification test password";
 static void Check(bool ok,string name){if(!ok)throw new Exception("Backup verification: "+name);Console.WriteLine("PASS "+name);}
 static DefaultHttpContext Request(byte[] data,string type="application/octet-stream",CancellationToken stop=default){var c=new DefaultHttpContext();c.Request.ContentType=type;c.Request.Body=new MemoryStream(data);c.Request.ContentLength=data.Length;c.RequestAborted=stop;return c;}
 static byte[] Body(byte[] archive,string password=Password){var metadata=JsonSerializer.SerializeToUtf8Bytes(new{password});var frame=new byte[4+metadata.Length+archive.Length];BinaryPrimitives.WriteInt32LittleEndian(frame,metadata.Length);metadata.CopyTo(frame,4);archive.CopyTo(frame,4+metadata.Length);return frame;}
 static async Task Reject(Func<Task> action,string name,int? status=null){try{await action();throw new Exception("Expected rejection: "+name);}catch(LibraryError error){Check(status==null||error.Status==status,name);}}
 public static async Task Run(string root,IDataProtectionProvider protection){
  Directory.CreateDirectory(root);var data=Path.Combine(root,"data");Directory.CreateDirectory(data);
  using var store=new Store(data,protection);store.Apply(new("addBook",JsonSerializer.SerializeToElement(new{isbn="9780140328721",title="Verification sample",author="Author"}),0));
  var before=JsonSerializer.Serialize(store.Read(),Library.Json);File.WriteAllText(Path.Combine(data,"server.protected"),"PRIVATE TEST KEY");
  var capture=CompleteBackup.Capture(data,new Settings(),default);var archive=Path.Combine(root,"saved.carribrary-backup");using(var input=File.OpenRead(Path.Combine(capture,"snapshot.zip")))using(var output=File.Create(archive))BackupCipher.Encrypt(input,output,Password);Directory.Delete(capture,true);
  // A cryptographically intact archive can still have contradictory metadata.
  var inconsistentZip=Path.Combine(root,"inconsistent.zip");using(var input=File.OpenRead(archive))using(var output=File.Create(inconsistentZip))BackupCipher.Decrypt(input,output,Password);
  using(var zip=System.IO.Compression.ZipFile.Open(inconsistentZip,System.IO.Compression.ZipArchiveMode.Update)){
   var entry=zip.GetEntry("manifest.json")!;CompleteManifest manifest;using(var input=entry.Open())manifest=JsonSerializer.Deserialize<CompleteManifest>(input,Library.Json)!;entry.Delete();using var output=zip.CreateEntry("manifest.json").Open();JsonSerializer.Serialize(output,manifest with{LibraryFormat=999},Library.Json);
  }
  var inconsistent=Path.Combine(root,"inconsistent.carribrary-backup");using(var input=File.OpenRead(inconsistentZip))using(var output=File.Create(inconsistent))BackupCipher.Encrypt(input,output,Password);File.Delete(inconsistentZip);
  await Reject(()=>{BackupVerification.Verify(inconsistent,Password);return Task.CompletedTask;},"manifest must agree with the actual library format");
  var bytes=File.ReadAllBytes(archive);var service=new BackupVerification(data);
  var report=BackupVerification.Verify(archive,Password);Check(report.Verified&&report.LibraryFormat==5&&report.SupportedLibraryFormat&&report.Files>=3,"offline verifier checks full archive and identifies format without starting a server");
  var result=(JsonHttpResult<BackupVerificationReport>)await service.Upload(Request(Body(bytes)));Check(result.Value!.Verified&&result.Value.Files==report.Files,"framed streamed upload verifies a saved complete archive");
  var json=JsonSerializer.Serialize(result.Value,Library.Json);Check(!json.Contains("PRIVATE TEST KEY")&&!json.Contains("library.sqlite")&&!json.Contains("Verification sample")&&!json.Contains(Password),"verification report exposes no records, file paths, passwords or keys");
  await Reject(async()=>{await service.Upload(Request(Body(bytes,"incorrect backup password")));},"wrong password rejected");
  var tampered=bytes.ToArray();tampered[^20]^=1;await Reject(async()=>{await service.Upload(Request(Body(tampered)));},"tampered archive rejected");
  await Reject(async()=>{await service.Upload(Request(Body(bytes[..^20])));},"truncated encrypted archive rejected");
  await Reject(async()=>{await service.Upload(Request(new byte[]{1,0}));},"truncated upload frame rejected");
  await Reject(async()=>{await service.Upload(Request(new byte[]{255,255,255,127}));},"unbounded password frame rejected");
  await Reject(async()=>{await service.Upload(Request(Body(bytes),"application/json"));},"wrong content type rejected",415);
  var oversize=Request(Body(bytes));oversize.Request.ContentLength=BackupVerification.MaxRequestBytes+1;await Reject(async()=>{await service.Upload(oversize);},"oversized request rejected before creating staging",413);
  using(var canceled=new CancellationTokenSource()){canceled.Cancel();try{await service.Upload(Request(Body(bytes),stop:canceled.Token));throw new Exception("Cancellation ignored");}catch(OperationCanceledException){Check(true,"canceled verification rejected");}}
  // A slow upload holds only the verification slot, not the current-library gate.
  using(var stop=new CancellationTokenSource()){var slow=Request([]);slow.Request.Body=new WaitingStream();slow.RequestAborted=stop.Token;var pending=service.Upload(slow);await Reject(async()=>{await service.Upload(Request(Body(bytes)));},"parallel upload is rejected without extra disk work",409);stop.Cancel();try{await pending;}catch(OperationCanceledException){} }
  Check(((JsonHttpResult<BackupVerificationReport>)await service.Upload(Request(Body(bytes)))).Value!.Verified,"capacity is released after failures and cancellation");
  Check(JsonSerializer.Serialize(store.Read(),Library.Json)==before,"verification and failures never mutate live library state");
  Check(File.ReadAllText(Path.Combine(data,"server.protected"))=="PRIVATE TEST KEY"&&File.ReadAllBytes(archive).SequenceEqual(bytes),"original saved archive and live credentials remain untouched");
  Check(!Directory.GetDirectories(data,".complete-backup-*").Any()&&!Directory.GetDirectories(root,".complete-backup-*").Any(),"successful, damaged, wrong-password and canceled checks remove private staging");
 }
 sealed class WaitingStream:Stream {
  public override bool CanRead=>true;public override bool CanSeek=>false;public override bool CanWrite=>false;public override long Length=>throw new NotSupportedException();public override long Position{get=>throw new NotSupportedException();set=>throw new NotSupportedException();}
  public override async ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken stop=default){await Task.Delay(Timeout.Infinite,stop);return 0;}
  public override int Read(byte[] b,int o,int c)=>throw new NotSupportedException();public override void Flush(){}public override long Seek(long o,SeekOrigin s)=>throw new NotSupportedException();public override void SetLength(long l)=>throw new NotSupportedException();public override void Write(byte[] b,int o,int c)=>throw new NotSupportedException();
 }
}
