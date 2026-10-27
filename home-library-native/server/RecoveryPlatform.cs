using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
namespace HomeLibrary;
public sealed class RecoveryPlatform : IRecoveryHost {
 readonly Settings settings;readonly string data,mode;readonly string executable;Process? desktop;
 public string Mode=>mode;
 public RecoveryPlatform(string mode,string data,Settings settings){this.mode=mode;this.data=data;this.settings=settings;executable=Path.Combine(AppContext.BaseDirectory,OperatingSystem.IsWindows()?"HomeLibrary.Server.exe":"HomeLibrary.Server");
  if(mode=="windows"){if(!OperatingSystem.IsWindows())throw new LibraryError("Use the recovery launcher for this operating system.");WindowsAuthority();}
  else if(mode=="flatpak"){if(!DesktopSession.IsFlatpak)throw new LibraryError("Use the installed Flatpak recovery launcher.");}
  else if(mode=="pi"){if(!OperatingSystem.IsLinux()||AppContext.BaseDirectory.TrimEnd('/')!="/opt/home-library/server"||Environment.UserName!="root")throw new LibraryError("Open Raspberry Pi Complete Backup Recovery with its administrator launcher.");}
  else throw new LibraryError("Choose the installed Windows, Flatpak or Pi recovery launcher.");
  if(mode=="pi"){
   if(data!="/var/lib/home-library")throw new LibraryError("Custom Pi data paths need supervised recovery; this wizard supports the packaged service data folder.");
   var envfile="/etc/home-library/home-library.env";
   if(File.Exists(envfile))foreach(var raw in File.ReadLines(envfile)){var line=raw.Trim();if(line.StartsWith('#')||!line.Contains('='))continue;var pair=line.Split('=',2);var value=pair[1].Trim().Trim('"','\'');if(pair[0]=="HOME_LIBRARY_DATA"&&value!=data)throw new LibraryError("The Pi service has a custom data location. Use supervised recovery.");if(pair[0]=="Library__AdminPort"){if(!int.TryParse(value,out var port)||port is <1 or >65535)throw new LibraryError("The Pi service administrator port is invalid.");settings.AdminPort=port;}}
  }
 }
 [SupportedOSPlatform("windows")] void WindowsAuthority(){
  using var identity=WindowsIdentity.GetCurrent();if(!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))throw new LibraryError("Open Complete Backup Recovery from the Start menu and approve administrator access.");
  using var key=Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\HomeLibraryNative");var registered=(key?.GetValue("ImagePath") as string)?.Trim().Trim('"');if(!string.Equals(registered,executable,StringComparison.OrdinalIgnoreCase))throw new LibraryError("The Windows service does not match this installed program. No recovery was started.");
 }
 public async Task Stop(CancellationToken stop){
  if(mode=="windows")await Run(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"sc.exe"),["stop","HomeLibraryNative"],stop,true);
  else if(mode=="pi")await Run("/usr/bin/systemctl",["stop","home-library.service"],stop);
  // Flatpak exit must use the already-authenticated dashboard Stop confirmation.
  // Recovery does not create a local-administrator authentication bypass.
  var deadline=DateTime.UtcNow.AddSeconds(mode=="flatpak"?1:35);
  while(true){try{CompleteRecovery.AssertStopped(data);return;}catch(LibraryError)when(DateTime.UtcNow<deadline){await Task.Delay(300,stop);}}
 }
 public async Task Start(CancellationToken stop){
  if(mode=="windows")await Run(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"sc.exe"),["start","HomeLibraryNative"],stop,true);
  else if(mode=="pi")await Run("/usr/bin/systemctl",["start","home-library.service"],stop);
  else {if(desktop is {HasExited:false})return;var p=new ProcessStartInfo(executable){UseShellExecute=false};p.Environment["HOME_LIBRARY_DATA"]=data;p.ArgumentList.Add("--desktop");p.ArgumentList.Add("--no-browser");desktop=Process.Start(p)??throw new LibraryError("The Flatpak server could not be restarted.");}
 }
 public async Task<bool> Healthy(CancellationToken stop){
  using var client=new HttpClient(new HttpClientHandler{UseProxy=false,AllowAutoRedirect=false}){Timeout=TimeSpan.FromSeconds(2)};
  for(var i=0;i<30;i++){try{var result=await client.GetFromJsonAsync<Health>($"http://localhost:{settings.AdminPort}/local-auth/health",stop);if(result is{Ready:true}&&result.Version==SetupStatus.Version)return true;}catch(HttpRequestException){}catch(TaskCanceledException)when(!stop.IsCancellationRequested){}await Task.Delay(500,stop);}return false;
 }
 sealed record Health(bool Ready,string Version);
 public void PrepareData(string directory){CompleteRecovery.AssertTree(directory);if(mode=="windows"&&OperatingSystem.IsWindows()){var current=Path.Combine(AppContext.BaseDirectory,"appsettings.json");if(File.Exists(current))File.Copy(current,Path.Combine(directory,"installation-appsettings.json"),true);GrantService(directory);}else if(mode=="pi")Run("/usr/bin/chown",["-R","home-library:home-library",directory],CancellationToken.None).GetAwaiter().GetResult();}
 [SupportedOSPlatform("windows")] static void GrantService(string path){var info=new DirectoryInfo(path);var acl=info.GetAccessControl();acl.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.LocalServiceSid,null),FileSystemRights.Modify,InheritanceFlags.ContainerInherit|InheritanceFlags.ObjectInherit,PropagationFlags.None,AccessControlType.Allow));info.SetAccessControl(acl);}
 public static string ReviewBanner(string html,bool enabled){if(!enabled)return html;var body=html.IndexOf("<body",StringComparison.OrdinalIgnoreCase);if(body<0)return html;var end=html.IndexOf('>',body);return html.Insert(end+1,"<aside class=\"recovery-test-banner\" role=\"status\">RESTORED LIBRARY — remote access and automatic notifications are paused. Review borrower accounts and paired devices. <button id=\"complete-recovery-review\" type=\"button\">Record local review</button></aside>");}
 public static string Banner(string html,bool enabled=true){if(!enabled)return html;var body=html.IndexOf("<body",StringComparison.OrdinalIgnoreCase);if(body<0)return html;var end=html.IndexOf('>',body);return html.Insert(end+1,"<aside class=\"recovery-test-banner\" role=\"status\">RECOVERY TEST — this is a disposable copy. Your working library is unchanged.</aside>");}
 static async Task Run(string file,string[] args,CancellationToken stop,bool allowServiceState=false){using var timeout=CancellationTokenSource.CreateLinkedTokenSource(stop);timeout.CancelAfter(TimeSpan.FromSeconds(45));stop=timeout.Token;var p=new ProcessStartInfo(file){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};foreach(var arg in args)p.ArgumentList.Add(arg);using var process=Process.Start(p)??throw new LibraryError("The operating-system service helper did not start.");var output=process.StandardOutput.ReadToEndAsync(stop);var error=process.StandardError.ReadToEndAsync(stop);await process.WaitForExitAsync(stop);await output;await error;if(process.ExitCode!=0&&!(allowServiceState&&process.ExitCode is 1062 or 1056))throw new LibraryError("The operating-system service command failed. Check service permissions; no live data should be manually moved.",503);}
}
