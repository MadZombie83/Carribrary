using System.Diagnostics;
using System.Text.Json;
namespace HomeLibrary.Tray;
static class Program {
 [STAThread]static void Main(){ApplicationConfiguration.Initialize();using var mutex=new Mutex(true,"HomeLibraryTray",out var created);if(!created)return;Application.Run(new TrayContext());}
}
sealed class TrayContext:ApplicationContext {
 readonly NotifyIcon icon;readonly HttpClient client=new(){Timeout=TimeSpan.FromSeconds(3)};readonly System.Windows.Forms.Timer timer=new(){Interval=15000};readonly string origin;
 public TrayContext(){
 var settings=Path.Combine(AppContext.BaseDirectory,"..","server","appsettings.json");var port=4173;
 if(File.Exists(settings)){try{using var doc=JsonDocument.Parse(File.ReadAllText(settings));port=doc.RootElement.GetProperty("Library").GetProperty("AdminPort").GetInt32();}catch{}}
 origin=$"http://localhost:{port}";
 var menu=new ContextMenuStrip();menu.Items.Add("Open library admin",null,(_,_)=>Open(origin));menu.Items.Add("Pair or revoke phones",null,(_,_)=>Open(origin));menu.Items.Add("Setup & updates",null,(_,_)=>Open(origin+"/?setup=1"));menu.Items.Add("Remote connection setup",null,(_,_)=>{try{Process.Start(new ProcessStartInfo("powershell.exe","-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \""+Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","Connection-Setup.ps1"))+"\""){UseShellExecute=true,Verb="runas"});}catch(Exception e){MessageBox.Show(e.Message,"Connection setup");}});menu.Items.Add("Complete Backup Recovery",null,(_,_)=>{try{var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,".."));Process.Start(new ProcessStartInfo("powershell.exe","-NoProfile -STA -ExecutionPolicy Bypass -File \""+Path.Combine(root,"Complete-Backup-Recovery.ps1")+"\" -Root \""+root+"\""){UseShellExecute=true,Verb="runas"});}catch(Exception e){MessageBox.Show(e.Message,"Complete Backup Recovery");}});menu.Items.Add("Update & recovery",null,(_,_)=>{try{var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,".."));Process.Start(new ProcessStartInfo("powershell.exe","-NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File \""+Path.Combine(root,"Recover-Previous.ps1")+"\" -Root \""+root+"\""){UseShellExecute=true,Verb="runas"});}catch(Exception e){MessageBox.Show(e.Message,"Carribrary recovery");}});menu.Items.Add("Open setup guide",null,(_,_)=>Open(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","START-HERE.txt"))));menu.Items.Add("Open Windows Services",null,(_,_)=>Open("services.msc"));menu.Items.Add(new ToolStripSeparator());menu.Items.Add("Exit tray (server keeps running)",null,(_,_)=>ExitThread());
 icon=new NotifyIcon{Icon=new Icon(typeof(Program).Assembly.GetManifestResourceStream("HomeLibrary.Icon")!,new Size(32,32)),Text="Carribrary · checking server",ContextMenuStrip=menu,Visible=true};icon.DoubleClick+=(_,_)=>Open(origin);timer.Tick+=async(_,_)=>await Status();timer.Start();_ = Status();
 }
 static void Open(string target){try{Process.Start(new ProcessStartInfo(target){UseShellExecute=true});}catch(Exception e){MessageBox.Show(e.Message,"Carribrary");}}
 async Task Status(){try{var response=await client.GetAsync(origin+"/local-auth/health");icon.Text=response.IsSuccessStatusCode?"Carribrary · server running":"Carribrary · server needs attention";}catch{icon.Text="Carribrary · server stopped";}}
 protected override void ExitThreadCore(){timer.Stop();timer.Dispose();icon.Visible=false;icon.Dispose();client.Dispose();base.ExitThreadCore();}
}
