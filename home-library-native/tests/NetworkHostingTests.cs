using HomeLibrary;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;
using System.Net;
static class NetworkHostingTests {
 public static async Task Run(string directory,IDataProtectionProvider protection){
  Directory.CreateDirectory(directory);void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
  void Reject(Action action,string name){try{action();throw new Exception("Expected rejection: "+name);}catch(LibraryError){Console.WriteLine("PASS "+name);}}
  var settings=new Settings{AdminPort=19301,CatalogPort=19302,PhonePort=19303,DiscoveryPort=19304,EnableDiscovery=false};
  foreach(var address in new[]{"127.0.0.1","8.8.8.8","169.254.1.2","::1","192.168.1.2.evil.test"})Check(!NetworkHosting.PrivateAddress(address),"reject non-private bridge address "+address);
  Check(NetworkHosting.PrivateAddress("192.168.40.68")&&NetworkHosting.PrivateAddress("172.16.1.2")&&NetworkHosting.PrivateAddress("10.2.3.4"),"private IPv4 bridge ranges");
  Reject(()=>NetworkHosting.Name("\n"),"empty library name rejected");
  var state=new HostingState();state.Libraries.Add(new(Guid.NewGuid().ToString("N"),"Test",settings.AdminPort,4201,4202,4203));Reject(()=>NetworkHosting.Validate(state,settings),"overlapping ports rejected");
  Check(LocalAdminEndpoints.Cookie(settings)==LocalAdministrators.Cookie&&LocalAdminEndpoints.Cookie(new Settings{InstanceId=Guid.NewGuid().ToString("N")})!=LocalAdminEndpoints.Cookie(settings),"main cookie preserved and child cookie isolated");
  var hosting=new NetworkHosting(directory,settings,protection);
  Reject(()=>new NetworkHosting(directory,new Settings{RecoveryRehearsal=true},protection).SaveJoin(new(Enabled:true)),"recovery review blocks hosting changes");
  Reject(()=>HostedProxyConfig.Render("books.example.org",4174,[new(Guid.NewGuid().ToString("N"),"evil.example.org","8.8.8.8",8080)],directory,false),"remote plaintext route rejected");
  var routes=HostedProxyConfig.Render("books.example.org",4174,[new(Guid.NewGuid().ToString("N"),"kids.example.org","127.0.0.1",4201)],directory,false);
  Check(routes.Contains("127.0.0.1:4174")&&routes.Contains("127.0.0.1:4201")&&routes.Contains("admin off"),"secondary route preserves main route and disables proxy admin");
  if(!OperatingSystem.IsLinux())return;
  using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(90));
  HostedLibrary? first=null,second=null;
  try{
   first=await hosting.Create(new("First test library"),timeout.Token);second=await hosting.Create(new("Second test library"),timeout.Token);
   Check(first.Initialized&&second.Initialized&&first.AdminPort!=second.AdminPort,"two real library processes ready with independent ports");
   var root=NetworkHosting.ManagedRoot(directory);Check(!root.StartsWith(directory+Path.DirectorySeparatorChar),"additional data outside main backup folder");
   Check(File.Exists(Path.Combine(root,first.Id,"data","library.sqlite"))&&File.Exists(Path.Combine(root,second.Id,"data","library.sqlite")),"each process created its own database");
   Check(hosting.SetupCode(first.Id)!=hosting.SetupCode(second.Id),"independent administrator setup credentials");
   using var http=new HttpClient(new HttpClientHandler{UseProxy=false}){Timeout=TimeSpan.FromSeconds(3)};
   using var sessions=new HttpClient(new HttpClientHandler{UseProxy=false,CookieContainer=new CookieContainer()}){Timeout=TimeSpan.FromSeconds(8)};
   async Task Enroll(HostedLibrary library){var origin=$"http://localhost:{library.AdminPort}";using var request=new HttpRequestMessage(HttpMethod.Post,origin+"/local-auth/enroll");request.Headers.Add("Origin",origin);request.Content=System.Net.Http.Json.JsonContent.Create(new{username="Test librarian",password="Disposable distinct library password 260",code=hosting.SetupCode(library.Id)});using var response=await sessions.SendAsync(request);Check(response.IsSuccessStatusCode,"real administrator enrollment "+library.Name);}
   await Enroll(first);await Enroll(second);
   async Task<bool> Authenticated(HostedLibrary library){using var response=await sessions.GetAsync($"http://localhost:{library.AdminPort}/local-auth/status");using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());return json.RootElement.GetProperty("authenticated").GetBoolean();}
   Check(await Authenticated(first)&&await Authenticated(second),"two localhost sign-ins coexist in one browser cookie jar");
   using(var response=await sessions.GetAsync($"http://localhost:{first.AdminPort}/api/state")){using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());Check(json.RootElement.GetProperty("state").GetProperty("libraryName").GetString()==first.Name,"child initial library name saved independently");}
   using var one=await http.GetAsync($"http://localhost:{first.AdminPort}/local-auth/status");Check(one.IsSuccessStatusCode,"first library available");
   await hosting.Change(new(second.Id,false),timeout.Token);
   using var alive=await http.GetAsync($"http://localhost:{first.AdminPort}/local-auth/health");Check(alive.IsSuccessStatusCode,"pausing second leaves first running");
   await hosting.Change(new(second.Id,true),timeout.Token);using(var response=await http.GetAsync($"http://localhost:{second.AdminPort}/local-auth/status")){using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());Check(json.RootElement.GetProperty("configured").GetBoolean(),"resume retains second library administrator credentials and data");}Check(await Authenticated(first),"restarting second does not sign first library out");
   await hosting.SetHostname(second.Id,"kids.example.org",timeout.Token);Check(hosting.Routes().Single().Hostname=="kids.example.org","hostname applies to own child and route");
   await NetworkGatewayTests.Run(Path.Combine(directory,"bridge"),first,protection);
   await hosting.Change(new(second.Id,false),timeout.Token);var database=Path.Combine(root,second.Id,"data","library.sqlite");File.Move(database,database+".held");try{try{await hosting.Change(new(second.Id,true),timeout.Token);throw new Exception("Missing database silently replaced");}catch(LibraryError){Check(!File.Exists(database),"missing initialized database never replaced by an empty library");}}finally{File.Move(database+".held",database);}
   Check(!JsonSerializer.Serialize(hosting.Status(),Library.Json).Contains("protectedToken",StringComparison.OrdinalIgnoreCase),"status never reveals protected credentials");
  }finally{if(second!=null)await hosting.Change(new(second.Id,false),CancellationToken.None);if(first!=null)await hosting.Change(new(first.Id,false),CancellationToken.None);}
 }
}
