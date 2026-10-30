using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
namespace HomeLibrary;
public static class RecoveryWizard {
 public static async Task Run(string[] args){
  if(args.Length<1)throw new LibraryError("Open the installed Complete Backup Recovery shortcut.");
  var builder=WebApplication.CreateBuilder(new WebApplicationOptions{Args=[],ContentRootPath=AppContext.BaseDirectory});
  builder.Logging.ClearProviders();builder.WebHost.ConfigureKestrel(k=>{k.Listen(IPAddress.Loopback,0);k.Limits.MaxRequestBodySize=BackupVerification.MaxRequestBytes;});
  var settings=builder.Configuration.GetSection("Library").Get<Settings>()??new();var mode=args[0];
  var data=Environment.GetEnvironmentVariable("HOME_LIBRARY_DATA")??settings.DataDirectory;
  if(string.IsNullOrWhiteSpace(data))data=mode=="windows"?Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"HomeLibraryNative","data"):mode=="pi"?"/var/lib/home-library":throw new LibraryError("The Flatpak data location is unavailable.");
  data=Path.GetFullPath(data);if(!Directory.Exists(data))throw new LibraryError("The installed data folder is unavailable. Nothing was restored.");
  var host=new RecoveryPlatform(mode,data,settings);
  var work=Path.Combine(Path.GetDirectoryName(data)!,".carribrary-complete-recovery-"+Path.GetFileName(data));
  if(Directory.Exists(work)){CompleteRecovery.AssertTree(work);CompleteBackup.ProtectDirectory(work);}else{Directory.CreateDirectory(work);CompleteBackup.ProtectDirectory(work);}
  using var maintenance=new FileStream(Path.Combine(work,"wizard.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
  var core=new CompleteRecovery(data,work,host);using var state=new WizardSession(core);
  var app=builder.Build();string origin="";var secret=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));var cookie="carribrary-recovery-"+Guid.NewGuid().ToString("N");var session=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));var started=DateTimeOffset.UtcNow;var launchUsed=0;
  app.Use(async(c,next)=>{
   c.Response.Headers.CacheControl="no-store";c.Response.Headers["Referrer-Policy"]="no-referrer";c.Response.Headers["X-Content-Type-Options"]="nosniff";c.Response.Headers["Content-Security-Policy"]="default-src 'self'; script-src 'self'; style-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'none'; frame-ancestors 'none'";
   try{
    if(c.Request.Host.ToString()!=new Uri(origin).Authority||c.Connection.RemoteIpAddress==null||!IPAddress.IsLoopback(c.Connection.RemoteIpAddress)){c.Response.StatusCode=403;return;}
    if(c.Request.Method!="GET"&&(c.Request.Method!="POST"||c.Request.Headers.Origin!=origin)){c.Response.StatusCode=403;return;}
    if(c.Request.Path!="/upload"){var limit=c.Features.Get<IHttpMaxRequestBodySizeFeature>();if(limit!=null&&!limit.IsReadOnly)limit.MaxRequestBodySize=4096;}
    if(c.Request.Method=="POST"&&c.Request.Path!="/upload"&&!c.Request.HasJsonContentType()){c.Response.StatusCode=415;return;}
    if(c.Request.Path.StartsWithSegments("/api")||c.Request.Path=="/upload"){
     if(DateTimeOffset.UtcNow-started>TimeSpan.FromHours(2)||!Equal(c.Request.Cookies[cookie],session))throw new LibraryError("Reopen Complete Backup Recovery from its shortcut.",401);
    }
    await next();
   }catch(LibraryError error){c.Response.StatusCode=error.Status;await c.Response.WriteAsJsonAsync(new{error=error.Message});}
   catch(OperationCanceledException)when(!c.RequestAborted.IsCancellationRequested){c.Response.StatusCode=503;await c.Response.WriteAsJsonAsync(new{error="The operation timed out. Keep all recovery folders and reopen the tool to check its status."});}
   catch(Exception e)when(e is IOException or UnauthorizedAccessException or JsonException or Microsoft.Data.Sqlite.SqliteException){c.Response.StatusCode=400;await c.Response.WriteAsJsonAsync(new{error="The operation could not finish. Keep the archive and retained recovery folders. Check the password, free space and permissions, then retry."});}
   catch(Exception e){Console.Error.WriteLine("Recovery operation failed: "+e.GetType().Name);c.Response.StatusCode=500;await c.Response.WriteAsJsonAsync(new{error="Recovery stopped safely ("+e.GetType().Name+"). Keep all recovery folders and ask for help; do not move live data manually."});}
  });
  app.MapPost("/unlock",async(HttpContext c)=>{if(c.Request.ContentLength>256)return Results.BadRequest();var input=await c.Request.ReadFromJsonAsync<Dictionary<string,string>>();if(input==null||!input.TryGetValue("token",out var token)||!Equal(token,secret)||Interlocked.CompareExchange(ref launchUsed,1,0)!=0)return Results.Unauthorized();c.Response.Cookies.Append(cookie,session,new CookieOptions{HttpOnly=true,SameSite=SameSiteMode.Strict,Path="/",MaxAge=TimeSpan.FromHours(2)});return Results.Ok();});
  app.MapGet("/api/status",()=>Results.Json(new{mode,liveDirectory=data,workDirectory=work,pending=core.Pending()!=null,flatpak=mode=="flatpak",reviewRequired=File.Exists(Path.Combine(data,CompleteRecovery.ReviewMarker)),restored=core.Completed,liveAdminUrl=$"http://localhost:{settings.AdminPort}/?stop=1",dashboardUrl=$"http://localhost:{settings.AdminPort}/"},Library.Json));
  app.MapPost("/upload",async Task<IResult> (HttpContext c)=>await state.Upload(c));
  app.MapPost("/api/test",async()=>Results.Json(new{url=await state.Test()},Library.Json));
  app.MapPost("/api/stop-test",async()=>{await state.StopTestAction();return Results.Ok();});
  app.MapPost("/api/restore",async(RecoveryConfirmation input)=>{if(input.Confirmation!="RESTORE LIBRARY"||!input.Checked||!input.Warnings)throw new LibraryError("Complete the rehearsal checks, acknowledge the recovery warning and type RESTORE LIBRARY exactly.");var retained=await state.Restore();return Results.Json(new{retained,message="The restored server passed its startup check. Remote access and automatic notifications are paused. Open the restored local dashboard, review accounts/devices and record the local review, then use Restart after local review."});});
  app.MapPost("/api/resume",async()=>{await state.Resume();return Results.Ok(new{message="The server restarted after local review. Normal remote access can resume; notifications still need separate configuration."});});
  app.MapPost("/api/undo",async(RecoveryConfirmation input)=>{if(input.Confirmation!="UNDO RESTORE")throw new LibraryError("Type UNDO RESTORE exactly.");await state.Undo();return Results.Ok(new{message="The retained current data was put back and its startup check passed."});});
  app.MapPost("/api/close",async()=>{await state.StopTestAction();_ =Task.Run(async()=>{await Task.Delay(300);app.Lifetime.StopApplication();});return Results.Ok();});
  app.MapGet("/",()=>Results.File(Path.Combine(AppContext.BaseDirectory,"wwwroot","complete-recovery.html"),"text/html"));
  foreach(var asset in new[]{"complete-recovery.js","complete-recovery.css"})app.MapGet("/"+asset,()=>Results.File(Path.Combine(AppContext.BaseDirectory,"wwwroot",asset),asset.EndsWith(".js")?"text/javascript":"text/css"));
  await app.StartAsync();var address=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();origin=address.Replace("127.0.0.1","localhost");var url=origin+"/#"+secret;
  Console.WriteLine("RECOVERY-OPEN "+url);if(!args.Contains("--no-browser")){if(OperatingSystem.IsWindows())Process.Start(new ProcessStartInfo(url){UseShellExecute=true});else DesktopSession.Open(url);}
  using var expiry=new Timer(_=>app.Lifetime.StopApplication(),null,TimeSpan.FromHours(2),Timeout.InfiniteTimeSpan);
  await app.WaitForShutdownAsync();await app.DisposeAsync();
 }
 static bool Equal(string? a,string b)=>a!=null&&a.Length==b.Length&&CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(a),System.Text.Encoding.UTF8.GetBytes(b));
 public sealed record RecoveryConfirmation(string Confirmation,bool Checked=false,bool Warnings=false);
 sealed class WizardSession(CompleteRecovery core):IDisposable {
  readonly SemaphoreSlim gate=new(1,1);string? stage,verified;BackupVerificationReport? report;Process? test;bool tested;int[] ports=[];
  public async Task<IResult> Upload(HttpContext c){
   if(!BackupVerification.Binary(c.Request))throw new LibraryError("Choose a complete .carribrary-backup file.",415);
   if(!await gate.WaitAsync(0,c.RequestAborted))throw new LibraryError("Wait for the current recovery operation to finish.",409);
   try{
    if(test is {HasExited:false})throw new LibraryError("Stop the test library before selecting another backup.");Cleanup();
    stage=CompleteBackup.PrivateDirectory(core.Work,"session-");var archive=Path.Combine(stage,"upload.carribrary-backup");
    var size=new byte[4];await c.Request.Body.ReadExactlyAsync(size,c.RequestAborted);var n=BinaryPrimitives.ReadInt32LittleEndian(size);if(n is <2 or >1024)throw new LibraryError("Invalid backup upload.");var metadata=new byte[n];await c.Request.Body.ReadExactlyAsync(metadata,c.RequestAborted);var input=JsonSerializer.Deserialize<CompleteBackupInput>(metadata,Library.Json)??throw new LibraryError("Enter the backup password.");BackupCipher.Password(input.Password);
    using var timeout=CancellationTokenSource.CreateLinkedTokenSource(c.RequestAborted);timeout.CancelAfter(TimeSpan.FromMinutes(8));
    await using(var output=new FileStream(archive,FileMode.CreateNew,FileAccess.Write,FileShare.None,65536,true)){var buffer=new byte[65536];long total=0;int count;while((count=await c.Request.Body.ReadAsync(buffer,timeout.Token))>0){total+=count;if(total>CompleteBackup.MaxArchiveBytes)throw new LibraryError("The archive exceeds the supported size.",413);await output.WriteAsync(buffer.AsMemory(0,count),timeout.Token);}}
    var directory=Path.Combine(stage,"verified");var manifest=await Task.Run(()=>CompleteBackupRecovery.Unpack(archive,directory,input.Password,timeout.Token),timeout.Token);report=BackupVerification.InspectRecovered(directory,manifest);File.Delete(archive);verified=Path.Combine(directory,"data");
    var compatible=Compatible(report);return Results.Json(new{report,compatible,message=compatible?"Integrity verified. Next, start a separate recovery test.":"Integrity verified, but guided testing/restoration requires a portable supported-format or same-platform 2.0.43–2.0.44 format-4 or 2.0.45–2.0.46 format-5 or 2.0.47–2.0.52 format-6 or 2.0.53 format-7 backup. Windows machine-bound data needs its original computer."},Library.Json);
   }catch{Cleanup();throw;}finally{gate.Release();}
  }
  static bool Compatible(BackupVerificationReport r)=>(r.Portable||r.Platform==(OperatingSystem.IsWindows()?"Windows":"Linux"))&&((r.LibraryFormat==4&&System.Text.RegularExpressions.Regex.IsMatch(r.Version,@"^2\.0\.(43|44)(?:[.+-].*)?$"))||(r.LibraryFormat==6&&System.Text.RegularExpressions.Regex.IsMatch(r.Version,@"^2\.0\.(47|48|49|50|51|52)(?:[.+-].*)?$"))||(r.LibraryFormat==7&&System.Text.RegularExpressions.Regex.IsMatch(r.Version,@"^2\.0\.53(?:[.+-].*)?$"))||(r.LibraryFormat==5&&System.Text.RegularExpressions.Regex.IsMatch(r.Version,@"^2\.0\.(45|46)(?:[.+-].*)?$")));
  void Ready(){if(verified==null||report==null||!Compatible(report))throw new LibraryError("Verify a portable supported-format or same-platform 2.0.43–2.0.44 format-4 or 2.0.45–2.0.46 format-5 or 2.0.47–2.0.52 format-6 or 2.0.53 format-7 backup first.");if(core.Pending()!=null)throw new LibraryError("Undo the interrupted restore before continuing.");}
  public async Task<string> Test(){if(!await gate.WaitAsync(0))throw new LibraryError("Wait for the current recovery operation.",409);try{
   Ready();StopTest();var folder=Path.Combine(stage!,"test-"+Guid.NewGuid().ToString("N"));Copy(verified!,folder);CompleteRecovery.PauseNotifications(folder);if(OperatingSystem.IsWindows())RecoveryPlatform.ProvisionWindowsIdentity(folder);ports=FreePorts();
   var p=ServerProcess(folder);foreach(var arg in new[]{"--Library:RecoveryRehearsal=true","--Library:EnableDiscovery=false","--Library:PublicCatalogOrigin=","--Library:RemoteAddress=","--Library:AdminPort="+ports[0],"--Library:PhonePort="+ports[1],"--Library:CatalogPort="+ports[2],"--Library:DiscoveryPort="+ports[3]})p.ArgumentList.Add(arg);
   test=Process.Start(p)??throw new LibraryError("The recovered test server could not start.");
   using var client=new HttpClient(new HttpClientHandler{UseProxy=false}){Timeout=TimeSpan.FromSeconds(2)};
   for(var i=0;i<40;i++){if(test.HasExited)throw new LibraryError("The recovered test did not start. Check same-computer key compatibility and installation permissions. The working library was not changed.");try{var health=await client.GetFromJsonAsync<TestHealth>($"http://localhost:{ports[0]}/local-auth/health");if(health is {Ready:true,RecoveryRehearsal:true}&&health.Version==SetupStatus.Version){tested=true;return $"http://localhost:{ports[0]}";}}catch(HttpRequestException){}catch(TaskCanceledException){}await Task.Delay(300);}
   throw new LibraryError("The test startup check timed out. The working library was not changed.");
  }catch{StopTest();throw;}finally{gate.Release();}}
  sealed record TestHealth(bool Ready,bool RecoveryRehearsal,string Version);
  public async Task StopTestAction(){if(!await gate.WaitAsync(0))throw new LibraryError("Wait for recovery to finish.",409);try{StopTest();}finally{gate.Release();}}
  public async Task Resume(){if(!await gate.WaitAsync(0))throw new LibraryError("Wait for recovery to finish.",409);try{if(!File.Exists(core.JournalFile)||core.Pending()!=null||File.Exists(Path.Combine(core.Live,CompleteRecovery.ReviewMarker)))throw new LibraryError("First sign into the restored local dashboard, review accounts/devices and record the local review.");var j=JsonSerializer.Deserialize<RecoveryJournal>(File.ReadAllText(core.JournalFile),Library.Json);if(j?.Phase!="complete")throw new LibraryError("No completed restore is waiting for restart.");await hostResume();}finally{gate.Release();}}
  async Task hostResume()=>await core.RestartAfterReview();
  public async Task Undo(){if(!await gate.WaitAsync(0))throw new LibraryError("Wait for recovery to finish.",409);try{StopTest();await core.Undo();}finally{gate.Release();}}
  public void StopTest(){if(test!=null){if(!test.HasExited){test.Kill(true);if(!test.WaitForExit(10000))throw new LibraryError("The recovered test process has not stopped. Keep the helper open and retry; no live data was changed.");}test.Dispose();test=null;}}
  public async Task<string> Restore(){if(!await gate.WaitAsync(0))throw new LibraryError("Wait for the current recovery operation.",409);try{Ready();if(!tested)throw new LibraryError("Run a successful recovery rehearsal before replacing the live library.");StopTest();var candidate=Path.Combine(stage!,"restore-"+Guid.NewGuid().ToString("N"));Copy(verified!,candidate);CompleteRecovery.PauseNotifications(candidate);return await core.Restore(candidate);}finally{gate.Release();}}
  static ProcessStartInfo ServerProcess(string data){var path=Environment.ProcessPath!;var p=new ProcessStartInfo(path){UseShellExecute=false,CreateNoWindow=true};if(Path.GetFileNameWithoutExtension(path)=="dotnet")p.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory,"HomeLibrary.Server.dll"));p.Environment["HOME_LIBRARY_DATA"]=data;return p;}
  static int[] FreePorts(){var ports=new HashSet<int>();while(ports.Count<4){var listener=new System.Net.Sockets.TcpListener(IPAddress.Loopback,0);listener.Start();ports.Add(((IPEndPoint)listener.LocalEndpoint).Port);listener.Stop();}return ports.ToArray();}
  static void Copy(string source,string target){CompleteRecovery.AssertTree(source);if(Directory.Exists(target))throw new LibraryError("Recovery destination already exists.");Directory.CreateDirectory(target);foreach(var file in Directory.GetFiles(source))File.Copy(file,Path.Combine(target,Path.GetFileName(file)));foreach(var dir in Directory.GetDirectories(source))Copy(dir,Path.Combine(target,Path.GetFileName(dir)));}
  void Cleanup(){StopTest();report=null;verified=null;tested=false;if(stage!=null&&Directory.Exists(stage))Directory.Delete(stage,true);stage=null;}
  public void Dispose(){gate.Wait();try{Cleanup();}finally{gate.Release();gate.Dispose();}}
 }
}
