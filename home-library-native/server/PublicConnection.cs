using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
namespace HomeLibrary;
public sealed record PublicConnectionInput(bool Enabled,string Hostname,string DuckDnsToken="",bool ClearToken=false,bool AllowDownload=false);
public sealed record PublicConnectionSaved(bool Enabled=false,string Hostname="",string ProtectedToken="",string CheckId="");
// This is a separate optional process, running inside the existing Flatpak/network sandbox.
// Unprivileged listener ports avoid host package installation and privilege escalation.
public sealed class PublicConnection {
 public const int HttpPort=8080,HttpsPort=8443;
 public const string CaddyVersion="2.11.7";
 readonly string file,folder;readonly Settings settings;readonly IDataProtector protector;readonly HttpClient http,privateHttp;readonly Func<IReadOnlyList<HostedProxyRoute>> routes;
 readonly SemaphoreSlim gate=new(1);PublicConnectionSaved saved,active;Process? process;
 string proxy="Off",dns="Not checked",https="Not checked";bool stopped,restartNeeded;
 public PublicConnection(string data,Settings settings,IDataProtectionProvider protection,HttpClient http,HttpClient? privateHttp=null,Func<IReadOnlyList<HostedProxyRoute>>? routes=null){this.settings=settings;file=Path.Combine(data,"public-connection.json");folder=Path.Combine(data,"public-https");protector=protection.CreateProtector("Carribrary.PublicConnection.v1");this.http=http;this.privateHttp=privateHttp??http;this.routes=routes??(()=>Array.Empty<HostedProxyRoute>());saved=Load(data);active=saved;}
 public static PublicConnectionSaved Load(string data)=>File.Exists(Path.Combine(data,"public-connection.json"))?JsonSerializer.Deserialize<PublicConnectionSaved>(File.ReadAllText(Path.Combine(data,"public-connection.json")),Library.Json)??new():new();
 public static string Hostname(string value){var host=(value??"").Trim().ToLowerInvariant();if(host.Length>253||!Regex.IsMatch(host,@"^(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z]{2,63}$")||host.EndsWith(".localhost")||host.EndsWith(".local"))throw new LibraryError("Enter a public DNS hostname only, without https://, a path or port.");return host;}
 public static string Render(string hostname,int catalogPort){hostname=Hostname(hostname);if(catalogPort is <1 or >65535)throw new LibraryError("Invalid catalog port.");return $$"""
{
    admin off
    http_port 8080
    https_port 8443
    auto_https disable_redirects
}
{{hostname}} {
    reverse_proxy 127.0.0.1:{{catalogPort}}
}
http://{{hostname}} {
    redir https://{{hostname}}{uri} 308
}
""";}
 public static void ApplyOrigin(Settings settings,string data){if(settings.RecoveryRehearsal||settings.RecoveryReviewRequired)return;var c=Load(data);if(c.Enabled)settings.PublicCatalogOrigin="https://"+Hostname(c.Hostname);}
 public object Status()=>new{supported=OperatingSystem.IsLinux()||OperatingSystem.IsWindows(),saved.Enabled,saved.Hostname,tokenConfigured=saved.ProtectedToken!="",pendingRestart=restartNeeded||saved.Enabled!=active.Enabled||saved.Hostname!=active.Hostname||saved.ProtectedToken!=active.ProtectedToken,publicOrigin=settings.PublicCatalogOrigin,httpPort=OperatingSystem.IsWindows()?80:HttpPort,httpsPort=OperatingSystem.IsWindows()?443:HttpsPort,proxyStatus=proxy,dnsStatus=dns,httpsStatus=https,addresses=Addresses(),caddyVersion=CaddyVersion,windows=OperatingSystem.IsWindows(),windowsAssistantNeeded=OperatingSystem.IsWindows()&&!WindowsHostingReadiness.Ready(Path.Combine(Path.GetDirectoryName(file)!,"network-windows-ready")),paused=settings.RecoveryRehearsal||settings.RecoveryReviewRequired};
 static string[] Addresses()=>NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.OperationalStatus==OperationalStatus.Up&&n.NetworkInterfaceType!=NetworkInterfaceType.Loopback).SelectMany(n=>n.GetIPProperties().UnicastAddresses).Where(a=>a.Address.AddressFamily==AddressFamily.InterNetwork&&!IPAddress.IsLoopback(a.Address)).Select(a=>a.Address.ToString()).Distinct().ToArray();
 string Executable=>Path.Combine(folder,OperatingSystem.IsWindows()?"caddy.exe":"caddy");
 public async Task Configure(PublicConnectionInput input,CancellationToken stop){
  if(!OperatingSystem.IsLinux()&&!OperatingSystem.IsWindows())throw new LibraryError("Managed public sharing supports Windows, Linux and Raspberry Pi.");
  if(settings.RecoveryRehearsal||settings.RecoveryReviewRequired)throw new LibraryError("Complete the local restored-library review before configuring public access.");
  await gate.WaitAsync(stop);try{
   if(!input.Enabled){saved=saved with{Enabled=false};Write();StopProxy();return;}
   var host=Hostname(input.Hostname);if(routes().Any(x=>x.Hostname==host))throw new LibraryError("Another connected library uses this hostname. Give each library its own hostname.");var token=input.ClearToken?"":saved.ProtectedToken;
   var tokenText=input.DuckDnsToken??"";
   if(tokenText.Length>200||tokenText.Any(char.IsControl))throw new LibraryError("The DuckDNS token is invalid.");
   if(tokenText.Trim()!="")token=protector.Protect(tokenText.Trim());
   if(token!=""&&!Regex.IsMatch(host,@"^[a-z0-9][a-z0-9-]{0,62}\.duckdns\.org$"))throw new LibraryError("Clear the saved DuckDNS token before using another DNS provider.");
   if(new[]{settings.AdminPort,settings.PhonePort,settings.CatalogPort}.Any(p=>p==HttpPort||p==HttpsPort))throw new LibraryError("Ports 8080/8443 overlap this library’s listeners. Use a separately managed proxy.");
   if(!File.Exists(Executable)||input.AllowDownload){
    if(OperatingSystem.IsWindows()&&process is{HasExited:false})throw new LibraryError("Stop managed public HTTPS before repairing its program, then enable it again. Existing settings were kept.");
    if(!input.AllowDownload)throw new LibraryError("Approve the optional verified Caddy download to continue.");
    await Install(stop);
   }
   saved=new(true,host,token,saved.CheckId==""?Guid.NewGuid().ToString("N"):saved.CheckId);Write();restartNeeded=true;https="Not checked; restart Carribrary, then check HTTPS";
  }finally{gate.Release();}
 }
 void Write(){Directory.CreateDirectory(Path.GetDirectoryName(file)!);var temp=file+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(saved,Library.Json));if(OperatingSystem.IsLinux())File.SetUnixFileMode(temp,UnixFileMode.UserRead|UnixFileMode.UserWrite);File.Move(temp,file,true);}
 async Task Install(CancellationToken stop){
  var windows=OperatingSystem.IsWindows();var arch=RuntimeInformation.ProcessArchitecture;var (name,hash)=windows&&arch==Architecture.X64?("amd64","0a1edc0b799512051c57071ce0e798d3f2cf67dc98171366f1d2b326072e3b06"):!windows?arch switch{Architecture.X64=>("amd64","727b91701a392de6ebc5027509f548bf39979e5216340d0faed8fa5e69c84f8b"),Architecture.Arm64=>("arm64","d8fc6d179a5d283028a472a5618564f6ad8a86fed513e64f032b3b0b7cc45e42"),_=>throw new LibraryError("This Caddy setup supports 64-bit Intel/AMD Linux and ARM64 Raspberry Pi.")}:throw new LibraryError("The Windows hosting assistant supports Windows x64.");
  Directory.CreateDirectory(folder);if(!windows)File.SetUnixFileMode(folder,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
  // Fixed official release and independently pinned release-asset hash. Never accept a URL/binary from input.
  using var response=await http.GetAsync($"https://github.com/caddyserver/caddy/releases/download/v{CaddyVersion}/caddy_{CaddyVersion}_{(windows?"windows":"linux")}_{name}.{(windows?"zip":"tar.gz")}",HttpCompletionOption.ResponseHeadersRead,stop);response.EnsureSuccessStatusCode();
  using var archive=new MemoryStream();await using(var stream=await response.Content.ReadAsStreamAsync(stop)){var buffer=new byte[65536];int count;while((count=await stream.ReadAsync(buffer,stop))>0){if(archive.Length+count>64*1024*1024)throw new LibraryError("Caddy download exceeds its expected limit.");archive.Write(buffer,0,count);}}
  if(!Convert.ToHexString(SHA256.HashData(archive.ToArray())).Equals(hash,StringComparison.OrdinalIgnoreCase))throw new LibraryError("Caddy download verification failed. Nothing was enabled.");
  archive.Position=0;byte[]? binary=null;string? license=null;
  if(windows){using var zip=new ZipArchive(archive,ZipArchiveMode.Read,true);foreach(var entry in zip.Entries){if(entry.FullName is not ("caddy.exe" or "LICENSE"))continue;if(entry.Length>100*1024*1024)throw new LibraryError("Caddy archive is invalid.");using var output=new MemoryStream();await using var input=entry.Open();await input.CopyToAsync(output,stop);if(entry.FullName=="caddy.exe"){if(binary!=null)throw new LibraryError("Caddy archive contains duplicate programs.");binary=output.ToArray();}else license=System.Text.Encoding.UTF8.GetString(output.ToArray());}}
  else{using var gzip=new GZipStream(archive,CompressionMode.Decompress,true);using var tar=new TarReader(gzip);TarEntry? entry;while((entry=tar.GetNextEntry())!=null){if(entry.Name is not ("caddy" or "LICENSE"))continue;if(entry.EntryType is not (TarEntryType.RegularFile or TarEntryType.V7RegularFile)||entry.Length>100*1024*1024||entry.DataStream==null)throw new LibraryError("Caddy archive is invalid.");using var output=new MemoryStream();await entry.DataStream.CopyToAsync(output,stop);if(entry.Name=="caddy"){if(binary!=null)throw new LibraryError("Caddy archive contains duplicate programs.");binary=output.ToArray();}else license=System.Text.Encoding.UTF8.GetString(output.ToArray());}}
  if(binary==null||license==null)throw new LibraryError("Caddy archive is incomplete.");
  var tmp=Executable+".tmp";await File.WriteAllBytesAsync(tmp,binary,stop);if(!windows)File.SetUnixFileMode(tmp,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);File.Move(tmp,Executable,true);await File.WriteAllTextAsync(Path.Combine(folder,"LICENSE-Caddy.txt"),license,stop);await File.WriteAllTextAsync(Path.Combine(folder,"download-sha256.txt"),hash,stop);
 }
 public bool Matches(string? id)=>active.Enabled&&active.CheckId!=""&&id==active.CheckId;
 public string CheckId=>active.Enabled?active.CheckId:"";
 public async Task CheckHttps(CancellationToken stop){
  if(!active.Enabled||settings.RecoveryRehearsal||settings.RecoveryReviewRequired)throw new LibraryError("Enable setup and restart Carribrary first.");
  try{using var timeout=CancellationTokenSource.CreateLinkedTokenSource(stop);timeout.CancelAfter(TimeSpan.FromSeconds(12));using var response=await privateHttp.GetAsync("https://"+active.Hostname+"/",HttpCompletionOption.ResponseHeadersRead,timeout.Token);https=response.IsSuccessStatusCode&&response.Headers.TryGetValues("X-Carribrary-Public-Check",out var values)&&values.SequenceEqual(new[]{active.CheckId})?"HTTPS reached this library with a trusted certificate. Also test with phone Wi-Fi off.":"The address did not identify this library. Check DNS and router forwarding.";}catch(Exception e)when(e is HttpRequestException or OperationCanceledException){https="HTTPS could not be verified. Check DNS, router forwarding, firewall and internet access; then test using cellular data.";}
 }
 public async Task Run(CancellationToken stop){
  if(!active.Enabled||settings.RecoveryRehearsal||settings.RecoveryReviewRequired)return;
  if(OperatingSystem.IsWindows()&&!WindowsHostingReadiness.Ready(Path.Combine(Path.GetDirectoryName(file)!,"network-windows-ready"))){proxy="Windows assistant needed before public HTTPS can start";return;}
  try{
   if(!File.Exists(Executable)){proxy="Caddy missing; reopen setup to install it";return;}
   Directory.CreateDirectory(folder);if(OperatingSystem.IsLinux())File.SetUnixFileMode(folder,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
   var config=Path.Combine(folder,"Caddyfile");string last="",rejected="";var nextDns=DateTimeOffset.MinValue;
   while(!stop.IsCancellationRequested&&!stopped){
    string content;try{content=HostedProxyConfig.Render(active.Hostname,settings.CatalogPort,routes(),Path.Combine(folder,"peers"),OperatingSystem.IsWindows());}catch(Exception e)when(e is LibraryError or CryptographicException or IOException){proxy="A connected library needs attention. The previous public entry was kept; review the connection file or certificate.";await Task.Delay(2000,stop);continue;}
    if(content!=last&&content!=rejected){
     var candidate=Path.Combine(folder,"Caddyfile.pending");await File.WriteAllTextAsync(candidate,content,stop);if(OperatingSystem.IsLinux())File.SetUnixFileMode(candidate,UnixFileMode.UserRead|UnixFileMode.UserWrite);
     if(await ValidateConfig(candidate,stop)){
      var previous=last;StopProcess();File.Move(candidate,config,true);try{StartProcess(config);}catch{if(previous!=""){await File.WriteAllTextAsync(config,previous,stop);StartProcess(config);}throw;}await Task.Delay(400,stop);
      if(process is{HasExited:true}){StopProcess();if(previous!=""){await File.WriteAllTextAsync(config,previous,stop);StartProcess(config);}rejected=content;proxy="The new public entry did not start. Previous routing was restored where available; check ports and the Windows assistant.";}
      else{last=content;rejected="";proxy="Public entry running; check HTTPS and test from phone mobile data";}
     }else{rejected=content;File.Delete(candidate);proxy="A hosting change failed validation. The previous public entry was kept. Review hostnames and connections, then retry.";}
    }
    if(last!=""&&(process==null||process.HasExited)){proxy="Public entry stopped. Check its ports/firewall and restart Carribrary.";return;}
    if(DateTimeOffset.UtcNow>=nextDns){await UpdateDns(stop);nextDns=DateTimeOffset.UtcNow.AddMinutes(5);}
    await Task.Delay(1000,stop);
   }
  }catch(OperationCanceledException)when(stop.IsCancellationRequested){}catch(Exception){proxy="Proxy could not start. Reopen setup; check executable permissions and ports 8080/8443.";}finally{StopProxy();}
 }
 async Task<bool> ValidateConfig(string path,CancellationToken stop){
  var info=ProcessInfo();foreach(var arg in new[]{"validate","--config",path,"--adapter","caddyfile"})info.ArgumentList.Add(arg);
  using var check=Process.Start(info);if(check==null)return false;var output=check.StandardOutput.ReadToEndAsync(stop);var error=check.StandardError.ReadToEndAsync(stop);using var timeout=CancellationTokenSource.CreateLinkedTokenSource(stop);timeout.CancelAfter(TimeSpan.FromSeconds(15));try{await check.WaitForExitAsync(timeout.Token);await Task.WhenAll(output,error);return check.ExitCode==0;}catch(OperationCanceledException){try{check.Kill(true);}catch(InvalidOperationException){}if(stop.IsCancellationRequested)throw;return false;}
 }
 ProcessStartInfo ProcessInfo(){var info=new ProcessStartInfo(Executable){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};info.Environment["XDG_DATA_HOME"]=folder;info.Environment["XDG_CONFIG_HOME"]=folder;if(OperatingSystem.IsWindows()){info.Environment["APPDATA"]=folder;info.Environment["LOCALAPPDATA"]=folder;}return info;}
 void StartProcess(string config){var info=ProcessInfo();foreach(var arg in new[]{"run","--config",config,"--adapter","caddyfile"})info.ArgumentList.Add(arg);process=Process.Start(info)??throw new IOException();process.OutputDataReceived+=(_,_)=>{};process.ErrorDataReceived+=(_,_)=>{};process.BeginOutputReadLine();process.BeginErrorReadLine();}
 void StopProcess(){try{if(process is{HasExited:false}){process.Kill(true);process.WaitForExit(5000);}}catch(InvalidOperationException){}finally{process?.Dispose();process=null;}}
 async Task UpdateDns(CancellationToken stop){if(active.ProtectedToken==""){dns="Automatic DNS updates off; use your router or another updater";return;}try{var token=protector.Unprotect(active.ProtectedToken);using var timeout=CancellationTokenSource.CreateLinkedTokenSource(stop);timeout.CancelAfter(TimeSpan.FromSeconds(10));using var r=await privateHttp.GetAsync("https://www.duckdns.org/update?domains="+Uri.EscapeDataString(active.Hostname[..^12])+"&token="+Uri.EscapeDataString(token)+"&ip=",timeout.Token);dns=r.IsSuccessStatusCode&&(await r.Content.ReadAsStringAsync(timeout.Token)).Trim()=="OK"?"DuckDNS updated":"DuckDNS rejected the update; check the hostname/token in setup";}catch(Exception){dns="DuckDNS update failed; check internet and saved token";}}
 void StopProxy(){stopped=true;StopProcess();if(!saved.Enabled)proxy="Off";}
}
public sealed class PublicConnectionWorker(PublicConnection connection):BackgroundService{protected override Task ExecuteAsync(CancellationToken stoppingToken)=>connection.Run(stoppingToken);}
