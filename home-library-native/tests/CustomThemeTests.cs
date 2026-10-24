using HomeLibrary;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
public static class CustomThemeTests {
 public static void Run(string directory,IDataProtectionProvider protection){
  void Check(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("PASS "+text);}
  void Reject(Action operation){try{operation();throw new Exception("Invalid theme accepted");}catch(LibraryError){}}
  using(var store=new Store(directory,protection)){
   var initial=store.Read();Check(initial.State.SavedThemes.Count==0,"existing library starts without custom themes");
   var p=new LibraryPalette{Name="Meadow"};
   store.Apply(new("saveCustomTheme",JsonSerializer.SerializeToElement(p,Library.Json),initial.Revision));
   var s=store.Read();var saved=s.State.SavedThemes.Single();Check(CustomThemes.IsCustom(s.State.LibraryTheme)&&CustomThemes.Selected(s.State)?.Name=="Meadow","custom theme saves and activates");
   p.Id=saved.Id;p.Name="Meadow revised";p.Ink="#174b73";
   store.Apply(new("saveCustomTheme",JsonSerializer.SerializeToElement(p,Library.Json),s.Revision));Check(store.Read().State.SavedThemes.Count==1,"editing a named theme updates rather than duplicates");
   var before=store.Read().Revision;
   void Bad(LibraryPalette value)=>Reject(()=>store.Apply(new("saveCustomTheme",JsonSerializer.SerializeToElement(value,Library.Json),store.Read().Revision)));
   Bad(new(){Name="Bad",Ink="red;display:none"});Bad(new(){Name="Invisible",Text="#ffffff"});Bad(new(){Name="Bad",Id="../../data"});Bad(new(){Name="Meadow revised"});Bad(new(){Name=" ",Ink="#174b73"});
   Reject(()=>store.Apply(new("setLibraryTheme",JsonSerializer.SerializeToElement(new{libraryTheme="custom-"+new string('a',32)}),store.Read().Revision)));
   Check(store.Read().Revision==before,"invalid palette, duplicate name, low contrast and unknown selection do not change revision");
   var state=store.Read().State;store.Backup(state,"themes");var restored=store.RestoreEncrypted(File.ReadAllBytes(Path.Combine(directory,"backups/themes.hlbackup")));Check(restored.SavedThemes.Single().Name=="Meadow revised"&&LibraryAppearance.Ink(restored)=="#174b73","custom palette survives encrypted backup and restore");
   store.Apply(new("setLibraryTheme",JsonSerializer.SerializeToElement(new{libraryTheme="forest"}),store.Read().Revision));Check(store.Read().State.SavedThemes.Count==1&&LibraryAppearance.Ink(store.Read().State)=="#244d36","builtin selection preserves named themes");
  }
  using(var restart=new Store(directory,protection))Check(restart.Read().State.SavedThemes.Single().Name=="Meadow revised","saved palettes survive restart");
 }
}
