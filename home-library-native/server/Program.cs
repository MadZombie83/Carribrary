using HomeLibrary;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using System.Net;
using System.Security.Authentication;
using System.Text.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Features;
if(args.Length>0&&args[0]=="--verify-complete-backup"){
 if(args.Length!=2){Console.Error.WriteLine("Usage: HomeLibrary.Server --verify-complete-backup ARCHIVE. The password is prompted; do not pass it as an argument.");Environment.ExitCode=1;return;}
 try{var report=BackupVerification.Verify(args[1],CompleteBackupRecovery.PasswordPrompt());Console.WriteLine($"Integrity verified: {report.Files} files, library format {report.LibraryFormat}. No library was restored. Compatibility, Windows machine-bound keys and external configuration still need separate recovery checks.");}catch(Exception error){Console.Error.WriteLine(error is LibraryError?error.Message:"Verification could not finish. Check the file, password, free space and permissions.");Environment.ExitCode=1;}return;
}
if(args.Length>0&&args[0]=="--unpack-complete-backup"){
 if(args.Length!=3){Console.Error.WriteLine("Usage: HomeLibrary.Server --unpack-complete-backup ARCHIVE NEW-FOLDER. The password is prompted; do not pass it as an argument.");Environment.ExitCode=1;return;}
 try{CompleteBackupRecovery.Command(args[1],args[2]);}catch(Exception error){Console.Error.WriteLine(error is LibraryError?error.Message:"The backup could not be unpacked. Check the file, password, destination permissions and available disk space.");Environment.ExitCode=1;}return;
}
if(args.Length>0&&args[0]=="--complete-recovery"){
 try{await RecoveryWizard.Run(args.Skip(1).ToArray());}catch(Exception error){Console.Error.WriteLine(error is LibraryError?error.Message:"Complete Backup Recovery could not open. Keep all backups/data and check installation permissions.");Environment.ExitCode=1;}return;
}
var builder=WebApplication.CreateBuilder(new WebApplicationOptions{Args=args,ContentRootPath=AppContext.BaseDirectory});
builder.Host.UseWindowsService(options=>options.ServiceName="HomeLibraryNative");
var settings=builder.Configuration.GetSection("Library").Get<Settings>()??new();
if(settings.RecoveryRehearsal){settings.EnableDiscovery=false;settings.PublicCatalogOrigin="";settings.RemoteAddress="";}
var data=Environment.GetEnvironmentVariable("HOME_LIBRARY_DATA")??settings.DataDirectory;
if(string.IsNullOrWhiteSpace(data))data=OperatingSystem.IsWindows()?Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"HomeLibraryNative","data"):Path.Combine(AppContext.BaseDirectory,"data");
data=Path.GetFullPath(data);Directory.CreateDirectory(data);
if(File.Exists(Path.Combine(data,CompleteRecovery.ReviewMarker))){settings.RecoveryReviewRequired=true;settings.EnableDiscovery=false;settings.PublicCatalogOrigin="";}
if(OperatingSystem.IsLinux())File.SetUnixFileMode(data,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
if(args.Contains("--stop-desktop")){if(!DesktopSession.IsFlatpak)throw new LibraryError("Desktop shutdown is only available inside the Carribrary Flatpak.");await DesktopSession.Stop(settings.AdminPort);return;}
using var desktopLock=args.Contains("--desktop")?DesktopSession.Acquire(data):null;
if(args.Contains("--desktop")&&desktopLock==null){DesktopSession.Open($"http://localhost:{settings.AdminPort}");return;}
if(new[]{settings.AdminPort,settings.PhonePort,settings.CatalogPort,settings.DiscoveryPort}.Any(p=>p<1||p>65535)||new[]{settings.AdminPort,settings.PhonePort,settings.CatalogPort}.Distinct().Count()!=3)throw new LibraryError("Invalid or overlapping ports.");
var protection=builder.Services.AddDataProtection().SetApplicationName("HomeLibrary.Native.v1").PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(data,"keyring")));
if(OperatingSystem.IsWindows())protection.ProtectKeysWithDpapi(true);
builder.Services.AddSingleton(sp=>new LocalAdministrators(data,sp.GetRequiredService<IDataProtectionProvider>()));
builder.Services.AddSingleton(sp=>new BrowserAdmins(data,settings.PublicCatalogOrigin,sp.GetRequiredService<IDataProtectionProvider>()));
builder.Services.AddHttpClient("backup-verification-proxy",client=>client.Timeout=TimeSpan.FromMinutes(10)).RemoveAllLoggers().ConfigurePrimaryHttpMessageHandler(()=>new HttpClientHandler{AllowAutoRedirect=false,UseCookies=false,UseProxy=false});
builder.Services.AddHttpClient("companion-proxy",client=>client.Timeout=TimeSpan.FromMinutes(3)).ConfigurePrimaryHttpMessageHandler(()=>new HttpClientHandler{AllowAutoRedirect=false,UseCookies=false,UseProxy=false});
builder.Services.AddSingleton<SnapshotGate>();
builder.Services.AddSingleton(sp=>new BackupVerification(data));
builder.Services.AddSingleton(sp=>new CompleteBackup(data,settings,sp.GetRequiredService<SnapshotGate>(),sp.GetRequiredService<Microsoft.AspNetCore.DataProtection.KeyManagement.IKeyManager>()));
builder.Services.AddSingleton(sp=>new PortableBackups(data,sp.GetRequiredService<IDataProtectionProvider>(),sp.GetRequiredService<CompleteBackup>()));if(!settings.RecoveryRehearsal&&!settings.RecoveryReviewRequired)builder.Services.AddHostedService<PortableBackupWorker>();
builder.Services.AddSingleton<IPushTransport,WebPushTransport>();
builder.Services.AddSingleton(sp=>new PushNotifications(data,settings.PublicCatalogOrigin,sp.GetRequiredService<IDataProtectionProvider>(),sp.GetRequiredService<IPushTransport>(),sp.GetRequiredService<Store>(),sp.GetRequiredService<BorrowerAccounts>(),sp.GetRequiredService<BrowserAdmins>(),sp.GetRequiredService<PurchaseSuggestions>()));if(!settings.RecoveryRehearsal&&!settings.RecoveryReviewRequired)builder.Services.AddHostedService<PushWorker>();
builder.Services.AddHttpClient("registration-verification",client=>client.Timeout=TimeSpan.FromSeconds(8)).RemoveAllLoggers().ConfigurePrimaryHttpMessageHandler(()=>new HttpClientHandler{AllowAutoRedirect=false,UseCookies=false});
builder.Services.AddSingleton(sp=>new RegistrationProtection(sp.GetRequiredService<IHttpClientFactory>().CreateClient("registration-verification"),builder.Configuration["BorrowerSecurity:TurnstileSiteKey"]??"",builder.Configuration["BorrowerSecurity:TurnstileSecretKey"]??"",settings.PublicCatalogOrigin));
builder.Services.AddSingleton(sp=>new BorrowerAccounts(data));
builder.Services.AddSingleton(sp=>new PurchaseSuggestions(data,sp.GetRequiredService<IDataProtectionProvider>()));
builder.Services.AddSingleton(sp=>new BorrowerVault(data));
builder.Services.AddSingleton(sp=>new Store(data,sp.GetRequiredService<IDataProtectionProvider>()));
builder.Services.AddSingleton(sp=>new Identity(data,sp.GetRequiredService<IDataProtectionProvider>(),args.Contains("--prepare-windows-identity")));builder.Services.AddSingleton(settings);
builder.Services.AddSingleton<IReminderSender,SmtpReminderSender>();
builder.Services.AddSingleton(sp=>new Reminders(data,sp.GetRequiredService<IDataProtectionProvider>(),sp.GetRequiredService<Store>(),sp.GetRequiredService<IReminderSender>()));if(!settings.RecoveryRehearsal&&!settings.RecoveryReviewRequired)builder.Services.AddHostedService<ReminderWorker>();builder.Services.AddSingleton<AccountRecovery>();if(!settings.RecoveryRehearsal&&!settings.RecoveryReviewRequired)builder.Services.AddHostedService(sp=>sp.GetRequiredService<AccountRecovery>());
builder.Services.AddSingleton(sp=>new CoverSettings(data,sp.GetRequiredService<IDataProtectionProvider>(),builder.Configuration["GoogleBooks:ApiKey"]??""));
builder.Logging.AddFilter("System.Net.Http.HttpClient.covers",Microsoft.Extensions.Logging.LogLevel.None);
builder.Services.AddHttpClient("covers",client=>{client.Timeout=TimeSpan.FromSeconds(6);client.DefaultRequestHeaders.UserAgent.ParseAdd("HomeLibrary/2.0.23");}).RemoveAllLoggers().ConfigurePrimaryHttpMessageHandler(()=>new HttpClientHandler{AllowAutoRedirect=false}).AddTypedClient((http,sp)=>new CoverLookup(http,sp.GetRequiredService<CoverSettings>().Key));
builder.Services.AddHttpClient("book-about",client=>{client.Timeout=TimeSpan.FromSeconds(12);client.DefaultRequestHeaders.UserAgent.ParseAdd("HomeLibrary/2.0.23");}).RemoveAllLoggers().ConfigurePrimaryHttpMessageHandler(()=>new HttpClientHandler{AllowAutoRedirect=false});
builder.Services.AddSingleton(sp=>new BookAbout(sp.GetRequiredService<IHttpClientFactory>().CreateClient("book-about"),sp.GetRequiredService<CoverSettings>()));
builder.Services.AddSingleton(sp=>new LibraryInsights(data,sp.GetRequiredService<IDataProtectionProvider>(),!settings.RecoveryRehearsal&&!settings.RecoveryReviewRequired));
builder.Services.AddSingleton(new Sharing(data,settings.PublicCatalogOrigin));builder.Services.AddSingleton(sp=>new ReadingAccess(data,settings.PublicCatalogOrigin,sp.GetRequiredService<IDataProtectionProvider>()));if(!settings.RecoveryRehearsal&&!settings.RecoveryReviewRequired)builder.Services.AddHostedService<Discovery>();builder.Services.AddHostedService<BackupWorker>();
builder.Services.ConfigureHttpJsonOptions(options=>options.SerializerOptions.PropertyNamingPolicy=JsonNamingPolicy.CamelCase);
builder.Services.AddRateLimiter(AccountRateLimits.Configure);
// Resolve identity through the final service provider; no duplicate provider/key store.
builder.WebHost.ConfigureKestrel((context,kestrel)=>{
 kestrel.Limits.MaxRequestBodySize=10*1024*1024;kestrel.Limits.RequestHeadersTimeout=TimeSpan.FromSeconds(15);
 kestrel.Listen(IPAddress.Loopback,settings.AdminPort);
 kestrel.Listen((settings.RecoveryRehearsal||settings.RecoveryReviewRequired)?IPAddress.Loopback:IPAddress.Any,settings.PhonePort,listen=>listen.UseHttps(https=>{
  https.ServerCertificateSelector=(_,_)=>identityHolder!.Server;
  https.ClientCertificateMode=ClientCertificateMode.AllowCertificate;
  https.ClientCertificateValidation=(cert,_,_)=>identityHolder!.Approved(cert)!=null;
  https.SslProtocols=SslProtocols.Tls12|SslProtocols.Tls13;
 }));
 kestrel.Listen(IPAddress.Loopback,settings.CatalogPort);
});
var app=builder.Build();
if(args.Length==1&&args[0]=="--prepare-local-admin-recovery"){
 try{using var recovery=new LocalAdministrators(data,app.Services.GetRequiredService<IDataProtectionProvider>());recovery.PrepareRecovery();Console.WriteLine("A one-time administrator setup/recovery code was written to "+recovery.SetupFile+". Restart Carribrary and use local account recovery. Keep this file private.");}
 catch(IOException){Console.Error.WriteLine("Stop Carribrary before preparing administrator recovery. No credentials were reset.");Environment.ExitCode=1;}
 return;
}
var localAdministrators=app.Services.GetRequiredService<LocalAdministrators>();
identityHolder=app.Services.GetRequiredService<Identity>();var store=app.Services.GetRequiredService<Store>();store.Changed+=app.Services.GetRequiredService<LibraryInsights>().Observe;foreach(var deleted in app.Services.GetRequiredService<BorrowerAccounts>().PendingVaultDeletes())try{app.Services.GetRequiredService<BorrowerVault>().Delete(deleted);app.Services.GetRequiredService<BorrowerAccounts>().CompleteVaultDelete(deleted);}catch(Microsoft.Data.Sqlite.SqliteException){app.Logger.LogWarning("Encrypted-vault cleanup is pending. Check storage permissions and restart the server.");}
if(args.Length==1&&args[0]=="--prepare-windows-identity"){Console.WriteLine("Windows HTTPS identity prepared for the Carribrary service.");return;}
if(args.Length>=2&&args[0] is "--import" or "--import-db"){store.Import(args[1],args[0]=="--import-db");Console.WriteLine($"Imported {store.Read().State.Books.Count} copies into {data}");return;}
if(args.Length>=2&&args[0]=="--restore-encrypted"){var state=store.RestoreEncrypted(File.ReadAllBytes(args[1]));store.Apply(new("restore",JsonSerializer.SerializeToElement(state,Library.Json),store.Read().Revision));Console.WriteLine("Encrypted backup restored.");return;}
app.UseWhen(c=>c.Connection.LocalPort==settings.CatalogPort,branch=>{var forwarded=new ForwardedHeadersOptions{ForwardedHeaders=ForwardedHeaders.XForwardedFor,ForwardLimit=1};forwarded.KnownProxies.Add(IPAddress.Loopback);forwarded.KnownProxies.Add(IPAddress.IPv6Loopback);branch.UseForwardedHeaders(forwarded);});
app.UseRateLimiter();
var adminOrigin=$"http://localhost:{settings.AdminPort}";var allowedHosts=new HashSet<string>{"localhost", "127.0.0.1"};
app.Use(async(context,next)=>{
 context.Response.Headers.CacheControl="no-store";context.Response.Headers["X-Content-Type-Options"]="nosniff";context.Response.Headers["Referrer-Policy"]="no-referrer";
 try{
  if(context.Connection.LocalPort==settings.AdminPort){
   if(!allowedHosts.Contains(context.Request.Host.Host)||context.Request.Host.Port!=settings.AdminPort){context.Response.StatusCode=403;return;}
   if(context.Request.Headers["Sec-Fetch-Site"]=="cross-site"){context.Response.StatusCode=403;return;}
   if(context.Request.Method is not ("GET" or "HEAD")){
    var origin=context.Request.Headers.Origin.ToString();if(origin!=adminOrigin&&origin!=$"http://127.0.0.1:{settings.AdminPort}"){context.Response.StatusCode=403;return;}
    if(!context.Request.HasJsonContentType()&&!(BackupVerification.IsUpload(context.Request.Path)&&BackupVerification.Binary(context.Request))){context.Response.StatusCode=403;return;}
   }
   // Login assets expose only intentional public name/icon/palette branding, never administrative data.
   var path=context.Request.Path.Value??"/";
   var authenticationRoute=path is "/local-auth/health" or "/local-auth/status" or "/local-auth/login" or "/local-auth/enroll" or "/local-auth/recover" or "/local-auth/logout";
   var loginAsset=context.Request.Method is "GET" or "HEAD" && path is "/login" or "/local-login.js" or "/styles.css" or "/favicon.ico" or "/branding" or "/branding-icon.png" or "/theme-palette.js" or "/theme-branding.js";
   var companionToken=context.Request.Headers[LocalAdministrators.CompanionHeader].ToString();
   // Revalidate the existing paired-browser credential on the inner proxy request,
   // including reused connections and the interval after outer proxy authorization.
   var paired=path.StartsWith("/api/",StringComparison.Ordinal)&&companionToken!=""&&app.Services.GetRequiredService<BrowserAdmins>().Resolve(companionToken)!=null;
   var signedIn=localAdministrators.Authorized(context.Request.Cookies[LocalAdminEndpoints.Cookie(settings)]);
   context.Items["Carribrary.AdminAuthorized"]=signedIn||paired;
   if(!authenticationRoute&&!loginAsset&&!signedIn&&!paired){
    if(path.StartsWith("/api/",StringComparison.Ordinal)){context.Response.StatusCode=401;await context.Response.WriteAsJsonAsync(new{error="Sign in as the local administrator."});}
    else context.Response.Redirect("/login"+(context.Request.Query["stop"]=="1"?"?stop=1":""));
    return;
   }
  }else if(context.Connection.LocalPort==settings.CatalogPort){
   context.Response.Headers["X-Robots-Tag"]="noindex, nofollow, noarchive";
   if(context.Request.Method is not ("GET" or "HEAD")){
    if(context.Request.Method!="POST"||!System.Text.RegularExpressions.Regex.IsMatch(context.Request.Path.Value??"",@"^(/s/[^/]+/(requests|suggestions|insights)|/reading/(action|requests|history/preview)|/account/(suggestions|registration-options|register|login|logout|forgot-password|reset|vault|action|requests|history/preview|deletion-preview|delete|insights|push|push/remove|push/test)|/companion/(enroll|logout|push|push/remove|push/test|api/[a-zA-Z0-9/_-]+))$")){context.Response.StatusCode=405;return;}
    if(!context.Request.HasJsonContentType()&&!(BackupVerification.IsUpload(context.Request.Path)&&BackupVerification.Binary(context.Request))){context.Response.StatusCode=415;return;}
    if(context.Request.Headers.Origin.ToString()!=settings.PublicCatalogOrigin.TrimEnd('/')){context.Response.StatusCode=403;return;}
    var bodyLimit=context.Features.Get<IHttpMaxRequestBodySizeFeature>();if(bodyLimit!=null&&!bodyLimit.IsReadOnly)bodyLimit.MaxRequestBodySize=BackupVerification.IsUpload(context.Request.Path)?BackupVerification.MaxRequestBytes:context.Request.Path.StartsWithSegments("/companion/api")?10*1024*1024:context.Request.Path=="/account/vault"?6*1024*1024:8192;
   }
   if(!context.Request.Path.StartsWithSegments("/s")&&!context.Request.Path.StartsWithSegments("/reading")&&!context.Request.Path.StartsWithSegments("/companion")&&!context.Request.Path.StartsWithSegments("/account")&&context.Request.Path!="/robots.txt"&&context.Request.Path!="/"&&context.Request.Path!="/welcome.css"&&context.Request.Path!="/welcome.js"){context.Response.StatusCode=404;return;}
  }else if(context.Connection.LocalPort==settings.PhonePort){
   if(!context.Request.IsHttps){context.Response.StatusCode=403;return;}
   if(context.Request.Path!="/mobile/pair"){
    var cert=await context.Connection.GetClientCertificateAsync();if(identityHolder.Approved(cert)==null){context.Response.StatusCode=401;await context.Response.WriteAsJsonAsync(new{error="This phone is not paired or has been revoked."});return;}
   }
   if(!context.Request.Path.StartsWithSegments("/mobile")){context.Response.StatusCode=404;return;}
  }else{context.Response.StatusCode=403;return;}
  await next();
 }catch(LibraryError error){context.Response.StatusCode=error.Status;await context.Response.WriteAsJsonAsync(new{error=error.Message});}
 catch(Exception error) when(error is JsonException or InvalidOperationException or KeyNotFoundException){context.Response.StatusCode=400;await context.Response.WriteAsJsonAsync(new{error="Invalid JSON or backup."});}
});
// The companion proxy calls the local API; only its inner request takes a lease.
// Snapshot endpoints acquire the exclusive lease after normal authorization.
app.Use(async(context,next)=>{
 var path=context.Request.Path;
 if(BackupVerification.IsUpload(path)||path.StartsWithSegments("/companion/api")||path=="/api/backup/complete"||path=="/mobile/backup/complete"||path=="/api/backup/portable/run"||path=="/mobile/backup/portable/run"){await next();return;}
 using var lease=await app.Services.GetRequiredService<SnapshotGate>().Shared(context.RequestAborted);
 // All current route mutations finish before headers. Do not let a slow download
 // hold the snapshot gate after an operation has already completed.
 context.Response.OnStarting(()=>{lease.Dispose();return Task.CompletedTask;});await next();
});
bool Admin(HttpContext c)=>c.Connection.LocalPort==settings.AdminPort&&c.Items["Carribrary.AdminAuthorized"] is true;
bool Mobile(HttpContext c)=>c.Connection.LocalPort==settings.PhonePort;
app.MapGet("/api/backup/portable",(HttpContext c,PortableBackups backups)=>Admin(c)?Results.Json(backups.Status(),Library.Json):Results.NotFound());
app.MapPost("/api/backup/portable",(PortableBackupSetup input,HttpContext c,PortableBackups backups)=>{if(!Admin(c))return Results.NotFound();backups.Configure(input);return Results.Json(backups.Status(),Library.Json);});
app.MapPost("/api/backup/portable/run",async(PortableRunInput input,HttpContext c,PortableBackups backups)=>{if(!Admin(c))return Results.NotFound();var created=await backups.Run(true,c.RequestAborted,input.RequestId);return Results.Json(new{created,status=backups.Status()},Library.Json);});
app.MapGet("/api/backup/portable/latest",(HttpContext c,PortableBackups backups)=>Admin(c)?backups.Latest():Results.NotFound());
app.MapGet("/mobile/backup/portable",(HttpContext c,PortableBackups backups)=>Mobile(c)?Results.Json(backups.Status(),Library.Json):Results.NotFound());
app.MapPost("/mobile/backup/portable",(PortableBackupSetup input,HttpContext c,PortableBackups backups)=>{if(!Mobile(c))return Results.NotFound();backups.Configure(input);return Results.Json(backups.Status(),Library.Json);});
app.MapPost("/mobile/backup/portable/run",async(PortableRunInput input,HttpContext c,PortableBackups backups)=>{if(!Mobile(c))return Results.NotFound();var created=await backups.Run(true,c.RequestAborted,input.RequestId);return Results.Json(new{created,status=backups.Status()},Library.Json);});
app.MapGet("/mobile/backup/portable/latest",(HttpContext c,PortableBackups backups)=>Mobile(c)?backups.Latest():Results.NotFound());
app.MapPost("/api/backup/complete",async(CompleteBackupInput input,HttpContext c,CompleteBackup backups)=>Admin(c)?await backups.Create(input.Password,c.RequestAborted):Results.NotFound());
app.MapPost("/mobile/backup/complete",async(CompleteBackupInput input,HttpContext c,CompleteBackup backups)=>Mobile(c)?await backups.Create(input.Password,c.RequestAborted):Results.NotFound());
foreach(var prefix in new[]{"/api","/mobile"})app.MapPost(prefix+"/backup/verify",async(HttpContext c,BackupVerification verification)=>(prefix=="/api"?Admin(c):Mobile(c))?await verification.Upload(c):Results.NotFound());
app.MapPost("/api/recovery-review/complete",(RecoveryWizard.RecoveryConfirmation input,HttpContext c)=>{
 if(!Admin(c)||!localAdministrators.Authorized(c.Request.Cookies[LocalAdminEndpoints.Cookie(settings)]))return Results.NotFound();
 if(!settings.RecoveryReviewRequired||input.Confirmation!="RESUME REMOTE ACCESS"||!input.Checked)throw new LibraryError("Review restored borrower accounts and paired devices, confirm the check and type RESUME REMOTE ACCESS exactly.");
 File.Delete(Path.Combine(data,CompleteRecovery.ReviewMarker));return Results.Ok(new{reviewRecorded=true,restartRequired=true});
});
foreach(var prefix in new[]{"/api","/mobile"}){
 app.MapGet(prefix+"/purchase-suggestions",(HttpContext c,PurchaseSuggestions suggestions)=>(prefix=="/api"?Admin(c):Mobile(c))?Results.Json(new{items=suggestions.List()},Library.Json):Results.NotFound());
 app.MapPost(prefix+"/purchase-suggestions",(PurchaseSuggestionDecision input,HttpContext c,PurchaseSuggestions suggestions)=>{if(!(prefix=="/api"?Admin(c):Mobile(c)))return Results.NotFound();suggestions.Decide(input);return Results.Ok(new{saved=true});});
}
foreach(var prefix in new[]{"/api","/mobile"})app.MapGet(prefix+"/notifications",(HttpContext c,BorrowerAccounts accounts,PortableBackups backups,Reminders reminders,PushNotifications push,PurchaseSuggestions suggestions)=>(prefix=="/api"?Admin(c):Mobile(c))?Results.Json(AdminNotifications.Read(store,accounts,settings,backups,reminders,push,suggestions),Library.Json):Results.NotFound());
app.MapGet("/api/state",(HttpContext c)=>Admin(c)?Results.Json(store.AdminRead(),Library.Json):Results.NotFound());
app.MapPost("/api/action",(ActionRequest req,HttpContext c)=>Admin(c)?(req.Action=="requestBook"?Results.BadRequest(new{error="Use the shared catalog to submit requests."}):Results.Json(store.AdminApply(req),Library.Json)):Results.NotFound());
app.MapGet("/api/receipts/{copyId}",(string copyId,HttpContext c)=>Admin(c)?Results.Json(BorrowingReceipt.Create(store.Read().State,copyId)):Results.NotFound());
app.MapGet("/mobile/receipts/{copyId}",(string copyId,HttpContext c)=>Mobile(c)?Results.Json(BorrowingReceipt.Create(store.Read().State,copyId)):Results.NotFound());
app.MapGet("/api/sharing",(Sharing sharing,HttpContext c)=>Admin(c)?Results.Json(sharing.Status()):Results.NotFound());
app.MapPost("/api/sharing",(JsonElement req,Sharing sharing,HttpContext c)=>Admin(c)?Results.Json(sharing.Change(req.GetProperty("action").GetString()??"")):Results.NotFound());
app.MapGet("/api/devices",(HttpContext c)=>Admin(c)?Results.Json(identityHolder.Devices().Select(d=>new{d.Id,d.Name,d.Issued,d.Revoked})):Results.NotFound());
app.MapPost("/api/devices/pair",(JsonElement req,HttpContext c)=>Admin(c)?Results.Json(identityHolder.StartPair(req.GetProperty("serverUrl").GetString()??"")):Results.NotFound());
app.MapPost("/api/devices/revoke",(JsonElement req,HttpContext c)=>{if(!Admin(c))return Results.NotFound();identityHolder.Revoke(req.GetProperty("deviceId").GetString()??"");return Results.Ok(new{revoked=true});});
app.MapPost("/api/desktop/stop",(HttpContext c)=>{if(!Admin(c)||!DesktopSession.IsFlatpak)return Results.NotFound();_ = Task.Run(async()=>{await Task.Delay(300);app.Lifetime.StopApplication();});return Results.Ok(new{stopping=true});});
app.MapGet("/api/setup",(HttpContext c)=>Admin(c)?Results.Json(SetupStatus.Read(settings,data,store,identityHolder)):Results.NotFound());
app.MapPost("/api/setup/dns",async(HttpContext c)=>Admin(c)?Results.Json(await SetupStatus.Dns(settings,c.RequestAborted)):Results.NotFound());
app.MapGet("/mobile/setup",(HttpContext c)=>Mobile(c)?Results.Json(SetupStatus.Read(settings,data,store,identityHolder)):Results.NotFound());
app.MapPost("/mobile/setup/dns",async(HttpContext c)=>Mobile(c)?Results.Json(await SetupStatus.Dns(settings,c.RequestAborted)):Results.NotFound());
app.MapGet("/api/server",(HttpContext c)=>Admin(c)?Results.Json(new{phonePort=settings.PhonePort,serverId=identityHolder.ServerId,remoteAddress=settings.RemoteAddress,dataDirectory=data}):Results.NotFound());
app.MapGet("/api/cover-settings",(CoverSettings settings,HttpContext c)=>Admin(c)?Results.Json(settings.Status()):Results.NotFound());
app.MapPost("/api/cover-settings",(GoogleKeyInput input,CoverSettings settings,HttpContext c)=>{if(!Admin(c))return Results.NotFound();settings.Save(input.ApiKey??"");return Results.Json(settings.Status());});
app.MapPost("/api/cover-settings/test",async(CoverLookup lookup,HttpContext c)=>Admin(c)?Results.Json(await lookup.TestGoogle(c.RequestAborted),Library.Json):Results.NotFound());
app.MapGet("/api/covers",async(string isbn,string? title,string? author,CoverLookup lookup,HttpContext c)=>Admin(c)?Results.Json(await lookup.Find(isbn,title,author,c.RequestAborted),Library.Json):Results.NotFound());
app.MapGet("/mobile/covers",async(string isbn,string? title,string? author,CoverLookup lookup,HttpContext c)=>Mobile(c)?Results.Json(await lookup.Find(isbn,title,author,c.RequestAborted),Library.Json):Results.NotFound());
app.MapGet("/mobile/state",(HttpContext c)=>Mobile(c)?Results.Json(store.AdminRead(),Library.Json):Results.NotFound());
app.MapPost("/mobile/action",(ActionRequest req,HttpContext c)=>{if(!Mobile(c))return Results.NotFound();if(req.Action=="requestBook")throw new LibraryError("Use the shared catalog to submit requests.",403);return Results.Json(store.AdminApply(req),Library.Json);});
app.MapGet("/mobile/sharing",(Sharing sharing,HttpContext c)=>Mobile(c)?Results.Json(sharing.Status()):Results.NotFound());
app.MapPost("/mobile/sharing",(JsonElement req,Sharing sharing,HttpContext c)=>{if(!Mobile(c))return Results.NotFound();if(!req.TryGetProperty("action",out var action)||action.ValueKind!=JsonValueKind.String)throw new LibraryError("Select a sharing action.");return Results.Json(sharing.Change(action.GetString()??""));});
app.MapGet("/mobile/devices",(HttpContext c)=>Mobile(c)?Results.Json(new{devices=identityHolder.Devices().Select(d=>new{d.Id,d.Name,d.Issued,d.Revoked}),currentDeviceId=identityHolder.Approved(c.Connection.ClientCertificate)?.Id}):Results.NotFound());
app.MapPost("/mobile/devices/pair",(JsonElement req,HttpContext c)=>Mobile(c)?Results.Json(identityHolder.StartPair(req.GetProperty("serverUrl").GetString()??"")):Results.NotFound());
app.MapPost("/mobile/devices/revoke",(JsonElement req,HttpContext c)=>{if(!Mobile(c))return Results.NotFound();identityHolder.Revoke(req.GetProperty("deviceId").GetString()??"");return Results.Ok(new{revoked=true});});
app.MapPost("/mobile/pair",(EnrollRequest req,HttpContext c)=>Mobile(c)?Results.Json(identityHolder.Enroll(req),Library.Json):Results.NotFound()).RequireRateLimiting("pair");
foreach(var prefix in new[]{"/api","/mobile"})app.MapGet(prefix+"/books/{copyId}/about",async(string copyId,HttpContext c,BookAbout about)=>{if(!(prefix=="/api"?Admin(c):Mobile(c)))return Results.NotFound();var book=store.Read().State.Books.Find(b=>b.Id==copyId);return book==null?Results.NotFound():Results.Json(await about.Find(book,c.RequestAborted),Library.Json);});
// These management routes remain local-admin / paired-phone only.
foreach(var prefix in new[]{"/api","/mobile"}){
 app.MapGet(prefix+"/readers/{readerId}/sharing",(string readerId,HttpContext c,ReadingAccess access)=>{
  if(!(prefix=="/api"?Admin(c):Mobile(c)))return Results.NotFound();if(!store.AdminRead().State.Readers.Any(r=>r.Id==readerId))return Results.NotFound();return Results.Json(access.Status(readerId));
 });
 app.MapPost(prefix+"/readers/{readerId}/sharing",(string readerId,JsonElement req,HttpContext c,ReadingAccess access)=>{
  if(!(prefix=="/api"?Admin(c):Mobile(c)))return Results.NotFound();if(!store.AdminRead().State.Readers.Any(r=>r.Id==readerId))return Results.NotFound();return Results.Json(access.Change(readerId,req.GetProperty("action").GetString()??""));
 });
}
foreach(var prefix in new[]{"/api","/mobile"}){
 app.MapGet(prefix+"/reminders",(HttpContext c,Reminders reminders)=>(prefix=="/api"?Admin(c):Mobile(c))?Results.Json(reminders.Status()):Results.NotFound());
 app.MapPost(prefix+"/reminders",(ReminderConfiguration input,HttpContext c,Reminders reminders)=>{if(!(prefix=="/api"?Admin(c):Mobile(c)))return Results.NotFound();reminders.Save(input);return Results.Json(reminders.Status());});
 app.MapPost(prefix+"/reminders/test",async(HttpContext c,Reminders reminders)=>{if(!(prefix=="/api"?Admin(c):Mobile(c)))return Results.NotFound();await reminders.Test(c.RequestAborted);return Results.Json(new{sent=true});});
}
string PersonalReader(HttpContext c,ReadingAccess access){if(c.Connection.LocalPort!=settings.CatalogPort)throw new LibraryError("Not found.",404);var header=c.Request.Headers.Authorization.ToString();var id=header.StartsWith("Bearer ")?access.Resolve(header[7..]):null;if(id==null||!store.Read().State.Readers.Any(r=>r.Id==id&&r.BorrowerId==""))throw new LibraryError("This link is unavailable. Borrower personal lists now require a borrower account and private encryption setup.",401);return id;}
app.MapGet("/reading/about/{copyId}",async(string copyId,HttpContext c,ReadingAccess access,BookAbout about)=>{PersonalReader(c,access);var book=store.Read().State.Books.Find(b=>b.Id==copyId);return book==null?Results.NotFound():Results.Json(await about.Find(book,c.RequestAborted),Library.Json);}).RequireRateLimiting("personal-reading");
app.MapGet("/reading/state",(HttpContext c,ReadingAccess access)=>Results.Json(ReadingAccess.View(store.Read(),PersonalReader(c,access)),Library.Json)).RequireRateLimiting("personal-reading");
app.MapPost("/reading/history/preview",(HistorySelection input,HttpContext c,ReadingAccess access)=>Results.Json(store.PersonalHistory(PersonalReader(c,access),input),Library.Json)).RequireRateLimiting("personal-reading");
app.MapPost("/reading/action",(ActionRequest req,HttpContext c,ReadingAccess access)=>Results.Json(store.PersonalAction(PersonalReader(c,access),req),Library.Json)).RequireRateLimiting("personal-reading");
app.MapPost("/reading/requests",(PublicBookRequest req,HttpContext c,ReadingAccess access)=>Results.Json(store.PersonalRequest(PersonalReader(c,access),req),Library.Json)).RequireRateLimiting("book-requests");
app.MapGet("/reading/{**leaf}",(string? leaf,HttpContext c)=>{
 if(c.Connection.LocalPort!=settings.CatalogPort)return Results.NotFound();var file=leaf switch{null or ""=>"reading.html","reading.js"=>"reading.js","reading-features.js"=>"reading-features.js","reading-list.js"=>"reading-list.js","history-controls.js"=>"history-controls.js","private-vault.js"=>"private-vault.js","vault-crypto.js"=>"vault-crypto.js","book-about.js"=>"book-about.js","styles.css"=>"styles.css","cover-credit.js"=>"cover-credit.js","powered-by-google.png"=>"powered-by-google.png",_=>null};if(file==null)return Results.NotFound();
 c.Response.Headers["Content-Security-Policy"]="default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' https:; connect-src 'self'; object-src 'none'; base-uri 'none'; frame-ancestors 'none'";
 return Results.File(Path.Combine(app.Environment.ContentRootPath,"wwwroot",file),file.EndsWith(".html")?"text/html":file.EndsWith(".js")?"text/javascript":file.EndsWith(".png")?"image/png":"text/css");
});
DataControlsEndpoints.Map(app,settings,store,Admin,Mobile);
CatalogBatch.Map(app,store,Admin,Mobile);
LibraryInsights.Map(app,settings,store,Admin,Mobile);
AccountEndpoints.Map(app,settings,store,Admin,Mobile);VaultEndpoints.Map(app,settings,store);
LocalAdminEndpoints.Map(app,settings,Admin,Mobile);
CompanionEndpoints.Map(app,settings,store,Admin,Mobile);
PushEndpoints.Map(app,settings,Admin);
app.Use(async(c,next)=>{await next();if(c.Request.Method!="GET"||c.Response.StatusCode!=200||c.Connection.LocalPort!=settings.CatalogPort||c.Request.Headers["DNT"]=="1"||c.Request.Headers["Sec-GPC"]=="1")return;var path=c.Request.Path.Value??"";var insights=c.RequestServices.GetRequiredService<LibraryInsights>();try{var catalog=System.Text.RegularExpressions.Regex.IsMatch(path,@"^/s/[^/]+/$");if(path=="/"||path=="/account/"||catalog)insights.Record("visits");if(catalog)insights.Record("catalogViews");}catch(IOException){}catch(UnauthorizedAccessException){} });
app.MapGet("/robots.txt",(HttpContext c)=>c.Connection.LocalPort==settings.CatalogPort?Results.Text("User-agent: *\nDisallow: /\n"):Results.NotFound());
app.MapPost("/s/{token}/suggestions",(string token,PurchaseSuggestionInput input,HttpContext c,Sharing sharing,PurchaseSuggestions suggestions)=>c.Connection.LocalPort==settings.CatalogPort&&sharing.Allowed(token)?Results.Json(suggestions.Submit(input)):Results.NotFound()).RequireRateLimiting("book-requests");
app.MapPost("/s/{token}/requests",(string token,PublicBookRequest input,HttpContext c,Sharing sharing)=>{
 if(c.Connection.LocalPort!=settings.CatalogPort||!sharing.Allowed(token))return Results.NotFound();
 return Results.Json(store.SubmitRequest(input),Library.Json);
}).RequireRateLimiting("book-requests");
app.MapMethods("/s/{token}/{**leaf}",new[]{"GET","HEAD"},async(string token,string? leaf,HttpContext c,Sharing sharing)=>{
 if(c.Connection.LocalPort!=settings.CatalogPort||!sharing.Allowed(token))return Results.NotFound();
 if(string.IsNullOrEmpty(leaf)&&!c.Request.Path.Value!.EndsWith("/"))return Results.Redirect(c.Request.Path+"/"+c.Request.QueryString);
 if(leaf!=null&&leaf.StartsWith("about/")){var book=store.Read().State.Books.Find(b=>b.Id==leaf[6..]&&!b.Archived);return book==null?Results.NotFound():Results.Json(await c.RequestServices.GetRequiredService<BookAbout>().Find(book,c.RequestAborted),Library.Json);}
 if(leaf=="manifest.webmanifest")return BorrowerApp.Manifest(store);
 if(leaf=="books.json"){var state=store.Read().State;return Results.Json(new{libraryName=state.LibraryName,libraryTheme=LibraryAppearance.Theme(state),bannerSubtitle=state.BannerSubtitle,encouragingMessage=state.EncouragingMessage,collections=state.Collections,books=Sharing.Books(state)});}
 var filename=leaf switch{null or ""=>"catalog.html","catalog.js"=>"catalog.js","book-about.js"=>"book-about.js","catalog.css"=>"catalog.css","favicon.ico"=>"favicon.ico","powered-by-google.png"=>"powered-by-google.png","cover-credit.js"=>"cover-credit.js",_=>null};
 return filename==null?Results.NotFound():Results.File(Path.Combine(app.Environment.ContentRootPath,"wwwroot",filename),filename.EndsWith(".html")?"text/html":filename.EndsWith(".js")?"text/javascript":filename.EndsWith(".ico")?"image/x-icon":filename.EndsWith(".png")?"image/png":"text/css");
}).RequireRateLimiting("personal-reading");
var files=new HashSet<string>{"library-artwork.js","library-artwork.css","library-insights.js","appearance.js","portable-backups.js","backup-verification.js","catalog-import.js","renewals.js","theme-branding.js","theme-palette.js","theme-editor.js","sidebar-artwork.js","dashboard-accounts.js","admin-dashboard.js","admin-pages.js","admin-tool-inventory.js","settings-navigation.js","title-fit.js","admin-shell.css","reading-features.js","reading-list.js","library-branding.js","book-about.js","vendor/zxing-browser-0.2.1.min.js","quick-start.html","quick-start.css","quick-start.js","guide/catalog.png","guide/add-book.png","guide/checkout.png","help.html","help.js","help.json","index.html","local-dashboard.js","app.js","styles.css","library.js","copy-labels.js","camera.js","qr.js","qrcodegen.js","favicon.svg","favicon.ico","sw.js","devices.js","history.js","cover-credit.js","powered-by-google.png"};
app.MapGet("/{**path}",(string? path,HttpContext c)=>{
 if(c.Connection.LocalPort==settings.CatalogPort&&(string.IsNullOrEmpty(path)||path is "welcome.css" or "welcome.js")){
  c.Response.Headers["Content-Security-Policy"]="default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self'; object-src 'none'; base-uri 'none'; frame-ancestors 'none'; form-action 'self'";
  return string.IsNullOrEmpty(path)?WelcomePage.Render(store,app.Environment.ContentRootPath):Results.File(Path.Combine(app.Environment.ContentRootPath,"wwwroot",path!),path=="welcome.js"?"text/javascript":"text/css");
 }
 var localLogin=c.Connection.LocalPort==settings.AdminPort&&path is "login" or "local-login.js" or "styles.css" or "favicon.ico" or "branding" or "branding-icon.png" or "theme-palette.js" or "theme-branding.js";
 if(!Admin(c)&&!localLogin)return Results.NotFound();var file=string.IsNullOrEmpty(path)?"index.html":path;
 if(file=="branding")return Results.Json(new{libraryName=store.Read().State.LibraryName,libraryTheme=LibraryAppearance.Theme(store.Read().State),palette=CustomThemes.Selected(store.Read().State)});
 if(file=="branding-icon.png")return LibraryAppearance.IconFile(store,192,app.Environment.ContentRootPath);
 if(file=="login")file="local-login.html";
 if(file=="local-login.html"||file=="local-login.js"){
  c.Response.Headers["Content-Security-Policy"]="default-src 'self'; script-src 'self'; style-src 'self'; object-src 'none'; base-uri 'none'; frame-ancestors 'none'; form-action 'self'";
  return settings.RecoveryRehearsal&&file.EndsWith(".html")?Results.Content(RecoveryPlatform.Banner(File.ReadAllText(Path.Combine(app.Environment.ContentRootPath,"wwwroot",file))),"text/html"):Results.File(Path.Combine(app.Environment.ContentRootPath,"wwwroot",file),file.EndsWith(".html")?"text/html":"text/javascript");
 }
 if(!files.Contains(file))return Results.NotFound();c.Response.Headers["Content-Security-Policy"]="default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https: http:; connect-src 'self' https://openlibrary.org; media-src 'self' blob:; object-src 'none'; base-uri 'self'; frame-ancestors 'none'";
 if(file=="index.html")return Results.Content(RecoveryPlatform.ReviewBanner(RecoveryPlatform.Banner(File.ReadAllText(Path.Combine(app.Environment.ContentRootPath,"wwwroot",file)),settings.RecoveryRehearsal),settings.RecoveryReviewRequired).Replace("</head>","<script src=\"local-dashboard.js\" defer></script></head>"),"text/html");
 return Results.File(Path.Combine(app.Environment.ContentRootPath,"wwwroot",file),file.EndsWith(".html")?"text/html":file.EndsWith(".js")?"text/javascript":file.EndsWith(".css")?"text/css":file.EndsWith(".ico")?"image/x-icon":file.EndsWith(".svg")?"image/svg+xml":file.EndsWith(".png")?"image/png":"application/octet-stream");
});
Console.WriteLine($"Admin: {adminOrigin} | Phone HTTPS: {settings.PhonePort} | Data: {data}");
if(args.Contains("--desktop")&&!args.Contains("--no-browser"))app.Lifetime.ApplicationStarted.Register(()=>{_ = Task.Run(()=>DesktopSession.Open(adminOrigin));});
app.Run();
public partial class Program{static Identity? identityHolder;}
public sealed class Settings{public bool RecoveryRehearsal{get;set;}public bool RecoveryReviewRequired{get;set;}
public string DataDirectory{get;set;}="";public int AdminPort{get;set;}=4173;public int PhonePort{get;set;}=7443;public int CatalogPort{get;set;}=4174;public int DiscoveryPort{get;set;}=7445;public bool EnableDiscovery{get;set;}=true;public string RemoteAddress{get;set;}="";public string PublicCatalogOrigin{get;set;}="";}

public sealed record GoogleKeyInput(string? ApiKey);
