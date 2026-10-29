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
 readonly string file,folder;readonly Settings settings;readonly IDataProtector protector;readonly HttpClient http,privateHttp;
 readonly SemaphoreSlim gate=new(1);PublicConnectionSaved saved,active;Process? process;
 string proxy="Off",dns="Not checked",https="Not checked";bool stopped,restartNeeded;
 public PublicConnection(string data,Settings settings,IDataProtectionProvider protection,HttpClient http,HttpClient? privateHttp=null){this.settings=settings;file=Path.Combine(data,"public-connection.json");folder=Path.Combine(data,"public-https");protector=protection.CreateProtector("Carribrary.PublicConnection.v1");this.http=http;this.privateHttp=privateHttp??http;saved=Load(data);active=saved;}
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
 public static void ApplyOrigin(Settings settings,string data){if(!OperatingSystem.IsLinux()||settings.RecoveryRehearsal||settings.RecoveryReviewRequired)return;var c=Load(data);if(c.Enabled)settings.PublicCatalogOrigin="https://"+Hostname(c.Hostname);}
 public object Status()=>new{supported=OperatingSystem.IsLinux(),saved.Enabled,saved.Hostname,tokenConfigured=saved.ProtectedToken!="",pendingRestart=restartNeeded||saved.Enabled!=active.Enabled||saved.Hostname!=active.Hostname||saved.ProtectedToken!=active.ProtectedToken,publicOrigin=settings.PublicCatalogOrigin,httpPort=HttpPort,httpsPort=HttpsPort,proxyStatus=proxy,dnsStatus=dns,httpsStatus=https,addresses=Addresses(),caddyVersion=CaddyVersion,paused=settings.RecoveryRehearsal||settings.RecoveryReviewRequired};
 static string[] Addresses()=>NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.OperationalStatus==OperationalStatus.Up&&n.NetworkInterfaceType!=NetworkInterfaceType.Loopback).SelectMany(n=>n.GetIPProperties().UnicastAddresses).Where(a=>a.Address.AddressFamily==AddressFamily.InterNetwork&&!IPAddress.IsLoopback(a.Address)).Select(a=>a.Address.ToString()).Distinct().ToArray();
 string Executable=>Path.Combine(folder,"caddy");
 public async Task Configure(PublicConnectionInput input,CancellationToken stop){
  if(!OperatingSystem.IsLinux())throw new LibraryError("Use Windows Start → Carribrary → Connection setup on this computer.");
  if(settings.RecoveryRehearsal||settings.RecoveryReviewRequired)throw new LibraryError("Complete the local restored-library review before configuring public access.");
  await gate.WaitAsync(stop);try{
   if(!input.Enabled){saved=saved with{Enabled=false};Write();StopProxy();return;}
   var host=Hostname(input.Hostname);var token=input.ClearToken?"":saved.ProtectedToken;
   var tokenText=input.DuckDnsToken??"";
   if(tokenText.Length>200||tokenText.Any(char.IsControl))throw new LibraryError("The DuckDNS token is invalid.");
   if(tokenText.Trim()!="")token=protector.Protect(tokenText.Trim());
   if(token!=""&&!Regex.IsMatch(host,@"^[a-z0-9][a-z0-9-]{0,62}\.duckdns\.org$"))throw new LibraryError("Clear the saved DuckDNS token before using another DNS provider.");
   if(new[]{settings.AdminPort,settings.PhonePort,settings.CatalogPort}.Any(p=>p==HttpPort||p==HttpsPort))throw new LibraryError("Ports 8080/8443 overlap this library’s listeners. Use a separately managed proxy.");
   if(!File.Exists(Executable)||input.AllowDownload){
    if(!input.AllowDownload)throw new LibraryError("Approve the optional verified Caddy download to continue.");
    await Install(stop);
   }
   saved=new(true,host,token,saved.CheckId==""?Guid.NewGuid().ToString("N"):saved.CheckId);Write();restartNeeded=true;https="Not checked; restart Carribrary, then check HTTPS";
  }finally{gate.Release();}
 }
 void Write(){Directory.CreateDirectory(Path.GetDirectoryName(file)!);var temp=file+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(saved,Library.Json));if(OperatingSystem.IsLinux())File.SetUnixFileMode(temp,UnixFileMode.UserRead|UnixFileMode.UserWrite);File.Move(temp,file,true);}
 [System.Runtime.Versioning.SupportedOSPlatform("linux")]
 async Task Install(CancellationToken stop){
  var arch=RuntimeInformation.ProcessArchitecture;var (name,hash)=arch switch{Architecture.X64=>("amd64","727b91701a392de6ebc5027509f548bf39979e5216340d0faed8fa5e69c84f8b"),Architecture.Arm64=>("arm64","d8fc6d179a5d283028a472a5618564f6ad8a86fed513e64f032b3b0b7cc45e42"),_=>throw new LibraryError("This Caddy setup supports 64-bit Intel/AMD Linux and ARM64 Raspberry Pi.")};
  Directory.CreateDirectory(folder);File.SetUnixFileMode(folder,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
  // Fixed official release and independently pinned release-asset hash. Never accept a URL/binary from input.
  using var response=await http.GetAsync($"https://github.com/caddyserver/caddy/releases/download/v{CaddyVersion}/caddy_{CaddyVersion}_linux_{name}.tar.gz",HttpCompletionOption.ResponseHeadersRead,stop);response.EnsureSuccessStatusCode();
  using var archive=new MemoryStream();await using(var stream=await response.Content.ReadAsStreamAsync(stop)){var buffer=new byte[65536];int count;while((count=await stream.ReadAsync(buffer,stop))>0){if(archive.Length+count>64*1024*1024)throw new LibraryError("Caddy download exceeds its expected limit.");archive.Write(buffer,0,count);}}
  if(!Convert.ToHexString(SHA256.HashData(archive.ToArray())).Equals(hash,StringComparison.OrdinalIgnoreCase))throw new LibraryError("Caddy download verification failed. Nothing was enabled.");
  archive.Position=0;using var gzip=new GZipStream(archive,CompressionMode.Decompress);using var tar=new TarReader(gzip);byte[]? binary=null;string? license=null;TarEntry? entry;
  while((entry=tar.GetNextEntry())!=null){if(entry.Name is not ("caddy" or "LICENSE"))continue;if(entry.EntryType is not (TarEntryType.RegularFile or TarEntryType.V7RegularFile)||entry.Length>100*1024*1024||entry.DataStream==null)throw new LibraryError("Caddy archive is invalid.");using var output=new MemoryStream();await entry.DataStream.CopyToAsync(output,stop);if(entry.Name=="caddy"){if(binary!=null)throw new LibraryError("Caddy archive contains duplicate programs.");binary=output.ToArray();}else license=System.Text.Encoding.UTF8.GetString(output.ToArray());}
  if(binary==null||license==null)throw new LibraryError("Caddy archive is incomplete.");
  var tmp=Executable+".tmp";await File.WriteAllBytesAsync(tmp,binary,stop);File.SetUnixFileMode(tmp,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);File.Move(tmp,Executable,true);await File.WriteAllTextAsync(Path.Combine(folder,"LICENSE-Caddy.txt"),license,stop);await File.WriteAllTextAsync(Path.Combine(folder,"download-sha256.txt"),hash,stop);
 }
 public bool Matches(string? id)=>active.Enabled&&active.CheckId!=""&&id==active.CheckId;
 public string CheckId=>active.Enabled?active.CheckId:"";
 public async Task CheckHttps(CancellationToken stop){
  if(!active.Enabled||settings.RecoveryRehearsal||settings.RecoveryReviewRequired)throw new LibraryError("Enable setup and restart Carribrary first.");
  try{using var timeout=CancellationTokenSource.CreateLinkedTokenSource(stop);timeout.CancelAfter(TimeSpan.FromSeconds(12));using var response=await privateHttp.GetAsync("https://"+active.Hostname+"/",HttpCompletionOption.ResponseHeadersRead,timeout.Token);https=response.IsSuccessStatusCode&&response.Headers.TryGetValues("X-Carribrary-Public-Check",out var values)&&values.SequenceEqual(new[]{active.CheckId})?"HTTPS reached this library with a trusted certificate. Also test with phone Wi-Fi off.":"The address did not identify this library. Check DNS and router forwarding.";}catch(Exception e)when(e is HttpRequestException or OperationCanceledException){https="HTTPS could not be verified. Check DNS, router forwarding, firewall and internet access; then test using cellular data.";}
 }
 public async Task Run(CancellationToken stop){
  if(!OperatingSystem.IsLinux()||!active.Enabled||settings.RecoveryRehearsal||settings.RecoveryReviewRequired)return;
  try{
   if(!File.Exists(Executable)){proxy="Caddy missing; reopen setup to install it";return;}
   Directory.CreateDirectory(folder);File.SetUnixFileMode(folder,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
   var config=Path.Combine(folder,"Caddyfile");await File.WriteAllTextAsync(config,Render(active.Hostname,settings.CatalogPort),stop);
   var info=new ProcessStartInfo(Executable){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};foreach(var arg in new[]{"run","--config",config,"--adapter","caddyfile"})info.ArgumentList.Add(arg);
   info.Environment["XDG_DATA_HOME"]=folder;info.Environment["XDG_CONFIG_HOME"]=folder;
   process=Process.Start(info);if(process==null)throw new IOException();process.OutputDataReceived+=(_,_)=>{};process.ErrorDataReceived+=(_,_)=>{};process.BeginOutputReadLine();process.BeginErrorReadLine();proxy="Proxy started; certificate and outside access still need verification";
   while(!stop.IsCancellationRequested&&!process.HasExited&&!stopped){await UpdateDns(stop);await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(300),stop),process.WaitForExitAsync(stop));}
   if(process.HasExited&&!stopped)proxy="Proxy stopped. Check whether ports 8080/8443 are in use, then restart Carribrary.";
  }catch(OperationCanceledException)when(stop.IsCancellationRequested){}catch(Exception){proxy="Proxy could not start. Reopen setup; check executable permissions and ports 8080/8443.";}finally{StopProxy();}
 }
 async Task UpdateDns(CancellationToken stop){if(active.ProtectedToken==""){dns="Automatic DNS updates off; use your router or another updater";return;}try{var token=protector.Unprotect(active.ProtectedToken);using var timeout=CancellationTokenSource.CreateLinkedTokenSource(stop);timeout.CancelAfter(TimeSpan.FromSeconds(10));using var r=await privateHttp.GetAsync("https://www.duckdns.org/update?domains="+Uri.EscapeDataString(active.Hostname[..^12])+"&token="+Uri.EscapeDataString(token)+"&ip=",timeout.Token);dns=r.IsSuccessStatusCode&&(await r.Content.ReadAsStringAsync(timeout.Token)).Trim()=="OK"?"DuckDNS updated":"DuckDNS rejected the update; check the hostname/token in setup";}catch(Exception){dns="DuckDNS update failed; check internet and saved token";}}
 void StopProxy(){stopped=true;try{if(process is{HasExited:false}){process.Kill(true);process.WaitForExit(5000);}}catch(InvalidOperationException){}if(!saved.Enabled)proxy="Off";}
}
public sealed class PublicConnectionWorker(PublicConnection connection):BackgroundService{protected override Task ExecuteAsync(CancellationToken stoppingToken)=>connection.Run(stoppingToken);}
