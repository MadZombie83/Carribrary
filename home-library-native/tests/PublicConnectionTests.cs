using HomeLibrary;using Microsoft.AspNetCore.DataProtection;using System.Net;using System.Text;using System.Text.Json;
static class PublicConnectionTests {
 public static async Task Run(string root,IDataProtectionProvider protection){
  Directory.CreateDirectory(root);void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
  void Reject(Action action){try{action();throw new Exception("Expected hostname rejection");}catch(LibraryError){}}
  foreach(var host in new[]{"https://books.example.org","books.example.org:443","x.example.org\n{ admin :9999 }","127.0.0.1","localhost","a.local","a..org","-a.example.org"})Reject(()=>PublicConnection.Hostname(host));
  Check(PublicConnection.Hostname(" BOOKS.EXAMPLE.ORG ")=="books.example.org","public hostname normalized without config injection");
  var config=PublicConnection.Render("books.example.org",19875);Check(config.Contains("admin off")&&config.Contains("http_port 8080")&&config.Contains("https_port 8443")&&config.Contains("127.0.0.1:19875")&&!config.Contains("4173")&&config.Contains("https://books.example.org{uri}"),"unprivileged public proxy includes root and only catalog upstream; no Caddy admin API");
  var peer=new HostedProxyRoute(Guid.NewGuid().ToString("N"),"peer.duckdns.org","127.0.0.1",4214);
  foreach(var invalid in new[]{"books.duckdns.org","peer.duckdns.org","https://family.duckdns.org","example.org"})
   Reject(()=>SharedHttps.ValidateHostname(invalid,"books.duckdns.org",[peer]));
  var family=SharedHttps.ValidateHostname(" FAMILY.DUCKDNS.ORG ","books.duckdns.org",[peer]);
  var combined=HostedProxyConfig.Render("books.duckdns.org",4174,SharedHttps.WithFamilyRoute([peer],new(family)),root,true);
  Check(combined.Contains("127.0.0.1:4174")&&combined.Contains("127.0.0.1:4214")&&combined.Contains("127.0.0.1:8123")&&
   combined.Contains("X-Carribrary-Shared-Https 1")&&!combined.Contains(":4173"),"shared HTTPS keeps established library routes and adds only the FCC loopback backend");
  var existing=JsonSerializer.Deserialize<PublicConnectionSaved>("""{"enabled":true,"hostname":"books.duckdns.org"}""",Library.Json)!;
  Check(existing.FamilyCommandCenter==null&&existing.Enabled,"existing managed setup loads with no shared FCC route");
  var roundTrip=JsonSerializer.Deserialize<PublicConnectionSaved>(JsonSerializer.Serialize(existing with {FamilyCommandCenter=new(family,true)},Library.Json),Library.Json);
  Check(roundTrip?.FamilyCommandCenter==new SharedHttpsInput(family,true),"FCC hostname and DNS choice persist across restart");
  var generic=SharedHttps.ValidateSite(new("Recipe box"," recipes.example.org ",5821),"books.duckdns.org",[peer.Hostname,family],[]);
  Check(generic.Hostname=="recipes.example.org"&&SharedHttps.RenderSite(generic).Contains("127.0.0.1:5821"),"arbitrary web app uses a separate hostname and loopback only");
  foreach(var port in new[]{2019,4173,4174,7443,8124,5821})
   Reject(()=>SharedHttps.ValidateSite(new("Unsafe app","app.example.org",port),"books.duckdns.org",[],[5821]));
  Reject(()=>SharedHttps.ValidateSite(new("Duplicate",peer.Hostname,5821),"books.duckdns.org",[peer.Hostname],[]));
  var migrated=JsonSerializer.Deserialize<PublicConnectionSaved>(
   """{"enabled":true,"hostname":"books.duckdns.org","familyCommandCenter":{"hostname":"family.duckdns.org","useDuckDns":true}}""",Library.Json)!;
  Check(migrated.Enabled&&migrated.FamilyCommandCenter==new SharedHttpsInput(family,true)&&migrated.JoinedHostname=="",
   "an existing Carribrary-hosted FCC setup keeps its owner, route, and DNS choice");
  var joining=Path.Combine(root,"joining");Directory.CreateDirectory(joining);
  File.WriteAllText(Path.Combine(joining,"public-connection.json"),
   JsonSerializer.Serialize(existing with{Enabled=false,JoinedHostname="joined.duckdns.org"},Library.Json));
  var joinedSettings=new Settings();PublicConnection.ApplyOrigin(joinedSettings,joining);
  Check(joinedSettings.PublicCatalogOrigin=="https://joined.duckdns.org"&&!PublicConnection.Load(joining).Enabled,
   "joining another HTTPS host sets the public origin without starting Carribrary's Caddy");
  var reviewSettings=new Settings{RecoveryReviewRequired=true};PublicConnection.ApplyOrigin(reviewSettings,joining);
  Check(reviewSettings.PublicCatalogOrigin=="","recovery review blocks joined public origin");
  combined+=SharedHttps.RenderSite(generic);
  var caddy=Environment.GetEnvironmentVariable("CARRIBRARY_CADDY_TEST_BINARY");
  if(!string.IsNullOrEmpty(caddy)){
   var file=Path.Combine(root,"Caddyfile");
   File.WriteAllText(file,combined);
   var info=new System.Diagnostics.ProcessStartInfo(caddy){UseShellExecute=false};
   foreach(var arg in new[]{"validate","--config",file,"--adapter","caddyfile"})info.ArgumentList.Add(arg);
   using var proc=System.Diagnostics.Process.Start(info)!;
   await proc.WaitForExitAsync();
   Check(proc.ExitCode==0,"Caddy validates combined Windows HTTPS configuration");
  }
  if(!OperatingSystem.IsLinux())return;
  var settings=new Settings{AdminPort=19873,CatalogPort=19875,PhonePort=19874};var handler=new Handler();using var http=new HttpClient(handler);var c=new PublicConnection(root,settings,protection,http);
  try{await c.Configure(new(true,"books.example.org"),default);throw new Exception("Download should require permission");}catch(LibraryError e){Check(e.Message.Contains("Approve"),"Caddy download requires explicit GUI consent");}
  handler.Response="invalid archive";try{await c.Configure(new(true,"books.example.org",AllowDownload:true),default);throw new Exception("Wrong hash accepted");}catch(LibraryError e){Check(e.Message.Contains("verification failed"),"wrong Caddy archive hash rejected before execution/configuration");}
  Check(!File.Exists(Path.Combine(root,"public-connection.json")),"failed download leaves no enabled public configuration");
  var folder=Path.Combine(root,"public-https");Directory.CreateDirectory(folder);await File.WriteAllTextAsync(Path.Combine(folder,"caddy"),"#!/bin/sh\nexit 0\n");
  await c.Configure(new(true,"test.duckdns.org","private-test-token"),default);var disk=File.ReadAllText(Path.Combine(root,"public-connection.json"));Check(!disk.Contains("private-test-token")&&!JsonSerializer.Serialize(c.Status(),Library.Json).Contains("private-test-token"),"DuckDNS token encrypted at rest and absent from status");
  var loaded=PublicConnection.Load(root);Check(loaded.Enabled&&loaded.Hostname=="test.duckdns.org","guided setup persists outside Flatpak installation folder");
  Check(settings.PublicCatalogOrigin=="","save does not partially mutate live account or sharing origins");PublicConnection.ApplyOrigin(settings,root);Check(settings.PublicCatalogOrigin=="https://test.duckdns.org","new origin applied consistently on next start");
  var recovery=new Settings{RecoveryRehearsal=true};PublicConnection.ApplyOrigin(recovery,root);Check(recovery.PublicCatalogOrigin=="","recovery rehearsal never enables public hostname");
  var review=new Settings{RecoveryReviewRequired=true};PublicConnection.ApplyOrigin(review,root);Check(review.PublicCatalogOrigin=="","restored-library review blocks public setup activation");
  try{await c.Configure(new(true,"books.example.org"),default);throw new Exception("Cross-provider token reuse");}catch(LibraryError){Console.WriteLine("PASS saved DuckDNS token cannot move to another provider");}
  await c.Configure(new(true,"books.example.org",ClearToken:true),default);Check(PublicConnection.Load(root).ProtectedToken=="","clear token preserves no hidden DNS credentials");
  // Use a harmless local test process, never a real proxy or outside DNS request.
  var program=Path.Combine(folder,"caddy");await File.WriteAllTextAsync(program,"#!/bin/sh\nsleep 30\n");File.SetUnixFileMode(program,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
  var running=new PublicConnection(root,settings,protection,http);using var cancel=new CancellationTokenSource();var run=running.Run(cancel.Token);await Task.Delay(200);await running.Configure(new(false,""),default);cancel.Cancel();await run;Check(!PublicConnection.Load(root).Enabled,"disable stops the managed child without a server restart");
  await c.Configure(new(false,""),default);Check(!PublicConnection.Load(root).Enabled,"disable persists without deleting existing library or DNS settings");
 }
 sealed class Handler:HttpMessageHandler{public string Response="";protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken stop)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(Response,Encoding.UTF8)});}
}
