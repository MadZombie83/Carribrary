using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace HomeLibrary;

public sealed record HostedLibrary(string Id,string Name,int AdminPort,int CatalogPort,int PhonePort,int DiscoveryPort,bool Enabled=false,bool Initialized=false,string Hostname="",string ProtectedDnsToken="");
public sealed record HostedLibraryInput(string Name);
public sealed record HostedLibraryChange(string Id,bool Enabled);
public sealed record NetworkPeer(string Id,string Name,string Hostname,string Address,int Port,string Certificate,string ServerName,string ProtectedToken,bool Enabled=true,int PhonePort=0,int ExternalPhonePort=0);
public sealed record NetworkJoin(string Id="",string Hostname="",int Port=0,string ProtectedToken="",bool Enabled=false,string ProtectedDnsToken="",int ExternalPhonePort=0);
public sealed class HostingState {
 public int Version{get;set;}=1;
 public List<HostedLibrary> Libraries{get;set;}=[];
 public List<NetworkPeer> Peers{get;set;}=[];
 public NetworkJoin Join{get;set;}=new();
}
public sealed record HostedProxyRoute(string Id,string Hostname,string Address,int Port,string Certificate="",string ServerName="",string Token="");

// The host controls processes, not the content or sign-ins of its child libraries.
// Child data is a sibling of the main data folder: each library needs its own backup.
public sealed class NetworkHosting {
 readonly string data,file,root;
 readonly Settings settings;
 readonly IDataProtector protector;
 readonly object stateGate=new();
 readonly SemaphoreSlim processGate=new(1);
 readonly Dictionary<string,(Process Process,string Token)> running=[];
 readonly Dictionary<string,string> messages=[];
 readonly Dictionary<string,string> dns=[];DateTimeOffset nextDns=DateTimeOffset.MinValue;
 readonly HttpClient dnsHttp=new(new HttpClientHandler{AllowAutoRedirect=false,UseCookies=false}){Timeout=TimeSpan.FromSeconds(10)};
 HostingState state;
 public NetworkHosting(string data,Settings settings,IDataProtectionProvider protection){
  this.data=Path.GetFullPath(data);this.settings=settings;
  file=Path.Combine(this.data,"network-hosting.json");
  root=ManagedRoot(this.data);
  protector=protection.CreateProtector("Carribrary.NetworkHosting.v1");
  state=Load(this.data);
  Validate(state,settings);
 }
 public static string ManagedRoot(string data)=>Path.Combine(Path.GetDirectoryName(Path.GetFullPath(data))!,Path.GetFileName(Path.GetFullPath(data))+"-libraries");
 public static HostingState Load(string data)=>File.Exists(Path.Combine(data,"network-hosting.json"))?JsonSerializer.Deserialize<HostingState>(File.ReadAllText(Path.Combine(data,"network-hosting.json")),Library.Json)??throw new LibraryError("Library hosting settings are unreadable. Keep the existing folders and ask for help."):new();
 public static string Name(string value){var name=(value??"").Trim();if(name.Length is <1 or >80||name.Any(char.IsControl))throw new LibraryError("Give the library a name of 1–80 characters.");return name;}
 public static bool PrivateAddress(string value)=>IPAddress.TryParse(value,out var ip)&&ip.AddressFamily==AddressFamily.InterNetwork&&PrivateAddress(ip);
 static bool PrivateAddress(IPAddress ip){var b=ip.GetAddressBytes();return b[0]==10||b[0]==172&&b[1] is >=16 and <=31||b[0]==192&&b[1]==168;}
 public static void Validate(HostingState state,Settings settings){
  if(state.Version!=1||state.Libraries==null||state.Peers==null||state.Join==null)throw new LibraryError("Unsupported library hosting settings.");
  var phones=new HashSet<int>{settings.PhonePort};var ids=new HashSet<string>();var ports=new HashSet<int>{settings.AdminPort,settings.CatalogPort,settings.PhonePort,settings.DiscoveryPort};var hosts=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  if(Uri.TryCreate(settings.PublicCatalogOrigin,UriKind.Absolute,out var origin))hosts.Add(origin.Host);
  foreach(var item in state.Libraries){
   if(!Guid.TryParseExact(item.Id,"N",out _)||!ids.Add(item.Id)||Name(item.Name)!=item.Name)throw new LibraryError("Invalid hosted library identity.");
   phones.Add(item.PhonePort);
   foreach(var port in new[]{item.AdminPort,item.CatalogPort,item.PhonePort,item.DiscoveryPort})if(port is <1024 or >65535||!ports.Add(port))throw new LibraryError("Hosted library ports overlap. Keep existing settings and review the hosting plan.");
   if(item.Hostname!=""&&!hosts.Add(PublicConnection.Hostname(item.Hostname)))throw new LibraryError("Each public library needs its own hostname.");
  }
  foreach(var peer in state.Peers){
   if(!Guid.TryParseExact(peer.Id,"N",out _)||!ids.Add(peer.Id)||!PrivateAddress(peer.Address)||peer.Port is <1024 or >65535||Name(peer.Name)!=peer.Name||!hosts.Add(PublicConnection.Hostname(peer.Hostname)))throw new LibraryError("Invalid or duplicate connected library.");
   if(peer.PhonePort!=0&&(peer.PhonePort is <1024 or >65535||peer.ExternalPhonePort is <1024 or >65535||!phones.Add(peer.ExternalPhonePort)))throw new LibraryError("Choose a different outside Android phone port for each library. Update the second library and export its private file again.");
   NetworkGateway.ValidateCertificate(peer.Certificate,peer.ServerName);
  }
  if(state.Join.Enabled){if(!Guid.TryParseExact(state.Join.Id,"N",out _)||state.Join.Port is <1024 or >65535||ports.Contains(state.Join.Port))throw new LibraryError("The private connection port overlaps this library.");PublicConnection.Hostname(state.Join.Hostname);}
 }
 public static int[] FindPorts(Settings settings,IEnumerable<HostedLibrary> libraries,int reservedPort=0){
  var used=new HashSet<int>(new[]{reservedPort,settings.AdminPort,settings.CatalogPort,settings.PhonePort,settings.DiscoveryPort,settings.GatewayPort});
  foreach(var l in libraries)used.UnionWith(new[]{l.AdminPort,l.CatalogPort,l.PhonePort,l.DiscoveryPort});
  used.UnionWith(IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Select(x=>x.Port));
  used.UnionWith(IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners().Select(x=>x.Port));
  var result=new List<int>();for(var p=4200;p<=65000&&result.Count<4;p++)if(!used.Contains(p)){result.Add(p);used.Add(p);}
  if(result.Count!=4)throw new LibraryError("No free ports were found. Close an unused test server and try again.");return result.ToArray();
 }
 public object Status(){lock(stateGate)return new{canManage=!settings.ManagedInstance,paused=settings.RecoveryRehearsal||settings.RecoveryReviewRequired,managedDirectory=root,libraries=state.Libraries.Select(x=>new{x.Id,x.Name,x.Enabled,x.Initialized,x.Hostname,x.AdminPort,x.CatalogPort,x.PhonePort,dnsConfigured=x.ProtectedDnsToken!="",dnsStatus=dns.GetValueOrDefault(x.Id,"Not checked"),adminUrl=$"http://localhost:{x.AdminPort}/",status=messages.GetValueOrDefault(x.Id,!x.Initialized&&OperatingSystem.IsWindows()?"Windows assistant needed":x.Enabled?"Starting":"Paused"),dataPresent=Directory.Exists(ChildData(x.Id))}).ToArray(),peers=state.Peers.Select(x=>new{x.Id,x.Name,x.Hostname,x.Address,x.Port,x.Enabled,x.PhonePort,x.ExternalPhonePort}).ToArray(),join=new{state.Join.Enabled,state.Join.Hostname,state.Join.Port,state.Join.ExternalPhonePort,dnsConfigured=state.Join.ProtectedDnsToken!="",dnsStatus=dns.GetValueOrDefault(state.Join.Id,"Not checked")},windows=OperatingSystem.IsWindows()};}
 void Allowed(){if(settings.ManagedInstance)throw new LibraryError("Open Libraries & network on the main server to manage libraries on this computer.");if(settings.RecoveryRehearsal||settings.RecoveryReviewRequired)throw new LibraryError("Finish the local recovery review before starting or connecting libraries.");}
 string ChildData(string id){if(!Guid.TryParseExact(id,"N",out _))throw new LibraryError("Invalid library identity.");return Path.Combine(root,id,"data");}
 static void DirectorySafe(string path){Directory.CreateDirectory(path);if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new LibraryError("A library folder is a link. Keep it unchanged and ask for help.");if(OperatingSystem.IsLinux())File.SetUnixFileMode(path,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);}
 void Save(){Validate(state,settings);var tmp=file+".tmp";File.WriteAllText(tmp,JsonSerializer.Serialize(state,Library.Json));if(OperatingSystem.IsLinux())File.SetUnixFileMode(tmp,UnixFileMode.UserRead|UnixFileMode.UserWrite);File.Move(tmp,file,true);}
 public async Task<HostedLibrary> Create(HostedLibraryInput input,CancellationToken stop){
  Allowed();var name=Name(input.Name);await processGate.WaitAsync(stop);try{
   HostedLibrary item;lock(stateGate){if(state.Libraries.Any(x=>x.Name.Equals(name,StringComparison.OrdinalIgnoreCase)))throw new LibraryError("A hosted library already uses that name.");var ports=FindPorts(settings,state.Libraries,state.Join.Port);item=new(Guid.NewGuid().ToString("N"),name,ports[0],ports[1],ports[2],ports[3]);
    try{DirectorySafe(root);DirectorySafe(Path.GetDirectoryName(ChildData(item.Id))!);DirectorySafe(ChildData(item.Id));}catch(UnauthorizedAccessException){throw new LibraryError("The hosting folder is not writable yet. Run the current installer or hosting assistant, then try again. Existing libraries were not changed.");}
    state.Libraries.Add(item);Save();messages[item.Id]="Starting a new empty library…";
   }
   if(OperatingSystem.IsWindows()){lock(stateGate)messages[item.Id]="Windows assistant needed — prepare this library's HTTPS key";return item;}
   try{await Start(item,stop);lock(stateGate){item=item with{Initialized=true,Enabled=true};state.Libraries[state.Libraries.FindIndex(x=>x.Id==item.Id)]=item;Save();messages[item.Id]="Running";}return item;}
   catch{await Stop(item.Id);lock(stateGate)messages[item.Id]="Could not start. Your new library folder was kept; choose Start to retry.";throw;}
  }finally{processGate.Release();}
 }
 public async Task Change(HostedLibraryChange change,CancellationToken stop){Allowed();await processGate.WaitAsync(stop);try{HostedLibrary item;lock(stateGate)item=state.Libraries.Find(x=>x.Id==change.Id)??throw new LibraryError("Library not found.");if(change.Enabled){if(OperatingSystem.IsWindows()&&!item.Initialized)throw new LibraryError("Open the Windows hosting assistant to prepare this library first.");if(!Directory.Exists(ChildData(item.Id))||item.Initialized&&!File.Exists(Path.Combine(ChildData(item.Id),"library.sqlite")))throw new LibraryError("This library's data is missing. Restore its own complete backup before starting it. No empty replacement was created.");await Start(item,stop);}else await Stop(item.Id);
   lock(stateGate){state.Libraries[state.Libraries.FindIndex(x=>x.Id==item.Id)]=item with{Enabled=change.Enabled,Initialized=item.Initialized||change.Enabled};Save();messages[item.Id]=change.Enabled?"Running":"Paused — data kept";}
  }finally{processGate.Release();}}
 public string SetupCode(string id){Allowed();HostedLibrary item;lock(stateGate)item=state.Libraries.Find(x=>x.Id==id)??throw new LibraryError("Library not found.");var file=Path.Combine(ChildData(item.Id),"local-admin-setup.txt");if(!File.Exists(file))throw new LibraryError("Open that library's dashboard and use its normal administrator recovery if needed.");return File.ReadAllText(file).Trim();}
 public async Task SetHostname(string id,string hostname,CancellationToken stop,string duckDnsToken="",bool clearToken=false){Allowed();hostname=hostname==""?"":PublicConnection.Hostname(hostname);await processGate.WaitAsync(stop);try{HostedLibrary previous,changed;lock(stateGate){previous=state.Libraries.Find(x=>x.Id==id)??throw new LibraryError("Library not found.");changed=previous with{Hostname=hostname,ProtectedDnsToken=DnsToken(hostname,previous.ProtectedDnsToken,duckDnsToken,clearToken)};var at=state.Libraries.IndexOf(previous);state.Libraries[at]=changed;try{Save();}catch{state.Libraries[at]=previous;throw;}}if(changed.Enabled){await Stop(id);try{await Start(changed,stop);}catch{lock(stateGate){state.Libraries[state.Libraries.FindIndex(x=>x.Id==id)]=previous;Save();}try{await Start(previous,CancellationToken.None);}catch{lock(stateGate)messages[id]="Previous hostname restored; choose Start to retry.";}throw;}}}finally{processGate.Release();}}
 public async Task Import(GatewayConnection input,CancellationToken stop){
  Allowed();NetworkGateway.ValidateConnection(input);
  void CheckUnique(){if(state.Libraries.Any(x=>x.Hostname==input.Hostname)||state.Peers.Any(x=>x.Id!=input.Id&&x.Hostname==input.Hostname)||new Uri(settings.PublicCatalogOrigin==""?"https://unused.invalid":settings.PublicCatalogOrigin).Host==input.Hostname)throw new LibraryError("Another library uses this hostname. Give each library a different hostname.");}
  lock(stateGate)CheckUnique();await NetworkGateway.Probe(input,stop);
  lock(stateGate){CheckUnique();var item=new NetworkPeer(input.Id,Name(input.Name),PublicConnection.Hostname(input.Hostname),input.Address,input.Port,input.Certificate,input.ServerName,Protect(input.Token),true,input.PhonePort,input.ExternalPhonePort);var at=state.Peers.FindIndex(x=>x.Id==input.Id);var previous=at<0?null:state.Peers[at];if(at<0)state.Peers.Add(item);else state.Peers[at]=item;try{Save();}catch{if(at<0)state.Peers.Remove(item);else state.Peers[at]=previous!;throw;}}
 }
 public void SetPeer(string id,bool enabled){Allowed();lock(stateGate){var at=state.Peers.FindIndex(x=>x.Id==id);if(at<0)throw new LibraryError("Connected library not found.");var previous=state.Peers[at];state.Peers[at]=previous with{Enabled=enabled};try{Save();}catch{state.Peers[at]=previous;throw;}}}
 async Task Start(HostedLibrary item,CancellationToken stop){
  foreach(var directory in new[]{root,Path.GetDirectoryName(ChildData(item.Id))!,ChildData(item.Id)})if(Directory.Exists(directory)&&(File.GetAttributes(directory)&FileAttributes.ReparsePoint)!=0)throw new LibraryError("A library folder is a link. Restore to a private local folder before starting it.");
  if(running.TryGetValue(item.Id,out var old)&&!old.Process.HasExited)return;
  if(running.Remove(item.Id,out old))old.Process.Dispose();
  var token=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));var path=Environment.ProcessPath??throw new LibraryError("The server program could not be found.");var p=new ProcessStartInfo(path){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=AppContext.BaseDirectory,RedirectStandardOutput=true,RedirectStandardError=true};
  if(Path.GetFileNameWithoutExtension(path).Equals("dotnet",StringComparison.OrdinalIgnoreCase))p.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory,"HomeLibrary.Server.dll"));
  foreach(var name in p.Environment.Keys.Where(x=>x.StartsWith("Library__",StringComparison.OrdinalIgnoreCase)||x.StartsWith("BorrowerSecurity__",StringComparison.OrdinalIgnoreCase)||x.StartsWith("GoogleBooks__",StringComparison.OrdinalIgnoreCase)||x=="CARRIBRARY_MANAGED_STOP_TOKEN").ToArray())p.Environment.Remove(name);
  p.Environment["HOME_LIBRARY_DATA"]=ChildData(item.Id);p.Environment["CARRIBRARY_MANAGED_STOP_TOKEN"]=token;p.Environment["GoogleBooks__ApiKey"]="";p.Environment["BorrowerSecurity__TurnstileSecretKey"]="";p.Environment["BorrowerSecurity__TurnstileSiteKey"]="";
  foreach(var arg in new[]{"--Library:ManagedInstance=true","--Library:InstanceId="+item.Id,"--Library:InitialLibraryName="+item.Name,"--Library:AdminPort="+item.AdminPort,"--Library:CatalogPort="+item.CatalogPort,"--Library:PhonePort="+item.PhonePort,"--Library:DiscoveryPort="+item.DiscoveryPort,"--Library:EnableDiscovery=false","--Library:PublicCatalogOrigin="+(item.Hostname==""?"":"https://"+item.Hostname),"--Library:RemoteAddress="+(item.Hostname==""?"":$"https://{item.Hostname}:{item.PhonePort}"),"--Library:RecoveryRehearsal=false","--Library:RecoveryReviewRequired=false"})p.ArgumentList.Add(arg);
  var child=Process.Start(p)??throw new LibraryError("The new library could not start.");child.OutputDataReceived+=(_,_)=>{};child.ErrorDataReceived+=(_,_)=>{};child.BeginOutputReadLine();child.BeginErrorReadLine();running[item.Id]=(child,token);
  using var http=new HttpClient(new HttpClientHandler{UseProxy=false,AllowAutoRedirect=false}){Timeout=TimeSpan.FromSeconds(2)};
  try{for(var i=0;i<100;i++){stop.ThrowIfCancellationRequested();if(child.HasExited)break;try{using var r=await http.GetAsync($"http://localhost:{item.AdminPort}/local-auth/health",stop);if(r.IsSuccessStatusCode){using var health=JsonDocument.Parse(await r.Content.ReadAsStringAsync(stop));if(health.RootElement.TryGetProperty("instanceId",out var id)&&id.GetString()==item.Id)return;}}catch(HttpRequestException){}catch(TaskCanceledException)when(!stop.IsCancellationRequested){}await Task.Delay(200,stop);}}catch{await Stop(item.Id);throw;}
  await Stop(item.Id);throw new LibraryError("The new library did not become ready. Its folders were kept. Check free ports and available memory, then try again.");
 }
 async Task Stop(string id){if(!running.Remove(id,out var child))return;try{if(!child.Process.HasExited){var port=state.Libraries.Find(x=>x.Id==id)?.AdminPort;if(port!=null){using var http=new HttpClient(new HttpClientHandler{UseProxy=false}){Timeout=TimeSpan.FromSeconds(3)};using var req=new HttpRequestMessage(HttpMethod.Post,$"http://localhost:{port}/internal-managed-stop");req.Headers.Add("X-Carribrary-Managed-Stop",child.Token);try{using var response=await http.SendAsync(req);}catch(HttpRequestException){}catch(TaskCanceledException){}}
    using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(10));try{await child.Process.WaitForExitAsync(timeout.Token);}catch(OperationCanceledException){child.Process.Kill(true);await child.Process.WaitForExitAsync();}
   }}finally{child.Process.Dispose();}}
 public async Task Run(CancellationToken stop){if(settings.ManagedInstance||settings.RecoveryRehearsal||settings.RecoveryReviewRequired)return;try{while(!stop.IsCancellationRequested){await processGate.WaitAsync(stop);try{HostedLibrary[] items;lock(stateGate)items=state.Libraries.Where(x=>x.Enabled&&x.Initialized).ToArray();foreach(var item in items){if(!Directory.Exists(ChildData(item.Id))||!File.Exists(Path.Combine(ChildData(item.Id),"library.sqlite"))){lock(stateGate)messages[item.Id]="Data missing — restore this library's own backup";continue;}try{await Start(item,stop);lock(stateGate)messages[item.Id]="Running";}catch(LibraryError e){lock(stateGate)messages[item.Id]=e.Message;}}}finally{processGate.Release();}await UpdateDns(stop);await Task.Delay(3000,stop);}}catch(OperationCanceledException)when(stop.IsCancellationRequested){}finally{await processGate.WaitAsync();try{foreach(var id in running.Keys.ToArray())await Stop(id);}finally{processGate.Release();}}}
 public string DnsToken(string hostname,string previous,string token,bool clear){token??="";if(token.Length>200||token.Any(char.IsControl))throw new LibraryError("Check the DuckDNS token.");var result=clear?"":previous;if(token.Trim()!="")result=Protect(token.Trim());if(result!=""&&!System.Text.RegularExpressions.Regex.IsMatch(hostname,@"^[a-z0-9][a-z0-9-]{0,62}\.duckdns\.org$"))throw new LibraryError("Remove the DuckDNS token before using a different DNS provider or no hostname.");return result;}
 async Task UpdateDns(CancellationToken stop){if(DateTimeOffset.UtcNow<nextDns)return;nextDns=DateTimeOffset.UtcNow.AddMinutes(5);(string Id,string Host,string Token)[] entries;lock(stateGate)entries=state.Libraries.Where(x=>x.Enabled&&x.ProtectedDnsToken!="").Select(x=>(x.Id,x.Hostname,x.ProtectedDnsToken)).Concat(state.Join.Enabled&&state.Join.ProtectedDnsToken!=""?new[]{(state.Join.Id,state.Join.Hostname,state.Join.ProtectedDnsToken)}:Array.Empty<(string,string,string)>()).ToArray();foreach(var entry in entries){string status;try{using var response=await dnsHttp.GetAsync("https://www.duckdns.org/update?domains="+Uri.EscapeDataString(entry.Host[..^12])+"&token="+Uri.EscapeDataString(Reveal(entry.Token))+"&ip=",stop);status=response.IsSuccessStatusCode&&(await response.Content.ReadAsStringAsync(stop)).Trim()=="OK"?"DuckDNS updated":"DuckDNS rejected the update; check hostname and token";}catch(Exception e)when(e is HttpRequestException or TaskCanceledException or CryptographicException){status="DNS update failed; check internet and saved token";}lock(stateGate)dns[entry.Id]=status;}}
 public NetworkJoin Join{get{lock(stateGate)return state.Join;}}
 public string Reveal(string value)=>protector.Unprotect(value);
 public void SaveJoin(NetworkJoin value){if(settings.RecoveryRehearsal||settings.RecoveryReviewRequired)throw new LibraryError("Finish the restored-library review first.");lock(stateGate){var previous=state.Join;state.Join=value;try{Save();}catch{state.Join=previous;throw;}}}
 public string Protect(string value)=>protector.Protect(value);
 public IReadOnlyList<HostedProxyRoute> Routes(){lock(stateGate)return state.Libraries.Where(x=>x.Enabled&&x.Hostname!="").Select(x=>new HostedProxyRoute(x.Id,x.Hostname,"127.0.0.1",x.CatalogPort)).Concat(state.Peers.Where(x=>x.Enabled).Select(x=>new HostedProxyRoute(x.Id,x.Hostname,x.Address,x.Port,x.Certificate,x.ServerName,Reveal(x.ProtectedToken)))).ToArray();}
}
public sealed class NetworkHostingWorker(NetworkHosting hosting):BackgroundService {protected override Task ExecuteAsync(CancellationToken stoppingToken)=>hosting.Run(stoppingToken);}
