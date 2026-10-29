using HomeLibrary;using Microsoft.AspNetCore.DataProtection;using System.Net;using System.Text;using System.Text.Json;
static class PublicConnectionTests {
 public static async Task Run(string root,IDataProtectionProvider protection){
  Directory.CreateDirectory(root);void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
  void Reject(Action action){try{action();throw new Exception("Expected hostname rejection");}catch(LibraryError){}}
  foreach(var host in new[]{"https://books.example.org","books.example.org:443","x.example.org\n{ admin :9999 }","127.0.0.1","localhost","a.local","a..org","-a.example.org"})Reject(()=>PublicConnection.Hostname(host));
  Check(PublicConnection.Hostname(" BOOKS.EXAMPLE.ORG ")=="books.example.org","public hostname normalized without config injection");
  var config=PublicConnection.Render("books.example.org",19875);Check(config.Contains("admin off")&&config.Contains("http_port 8080")&&config.Contains("https_port 8443")&&config.Contains("127.0.0.1:19875")&&!config.Contains("4173")&&config.Contains("https://books.example.org{uri}"),"unprivileged public proxy includes root and only catalog upstream; no Caddy admin API");
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
