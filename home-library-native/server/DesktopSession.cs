using System.Diagnostics;
using System.Net.Http.Json;
namespace HomeLibrary;
public static class DesktopSession {
 public static bool IsFlatpak=>Environment.GetEnvironmentVariable("FLATPAK_ID")=="com.household.HomeLibrary";
 public static FileStream? Acquire(string directory){
  try{return new FileStream(Path.Combine(directory,"desktop.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}
  catch(IOException){return null;}
 }
 public static void Open(string url){
  try{
   var info=new ProcessStartInfo{FileName=IsFlatpak?"/usr/bin/gdbus":"xdg-open",UseShellExecute=false};
   if(IsFlatpak){foreach(var arg in new[]{"call","--session","--dest","org.freedesktop.portal.Desktop","--object-path","/org/freedesktop/portal/desktop","--method","org.freedesktop.portal.OpenURI.OpenURI","",url,"{}"})info.ArgumentList.Add(arg);}
   else info.ArgumentList.Add(url);
   using var process=Process.Start(info);if(process!=null&&process.WaitForExit(10000)&&process.ExitCode!=0)Console.Error.WriteLine("Open the library in your browser: "+url);
  }catch(Exception error){Console.Error.WriteLine("Open "+url+" in your browser. "+error.Message);}
 }
 // Shutdown now opens an authenticated local confirmation; never bypass local login.
 public static Task Stop(int port){Open($"http://localhost:{port}/?stop=1");return Task.CompletedTask;}
}
