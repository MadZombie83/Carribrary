using System.Text.Json;
using System.Text.RegularExpressions;
namespace HomeLibrary;

public sealed class LibraryPalette {
 public string Id{get;set;}="";
 public string Name{get;set;}="";
 public string Ink{get;set;}="#493276";
 public string Accent{get;set;}="#c5dcff";
 public string Page{get;set;}="#f2f1fc";
 public string Surface{get;set;}="#ffffff";
 public string Soft{get;set;}="#e7eafb";
 public string Line{get;set;}="#d9d4ec";
 public string Text{get;set;}="#27273d";
 public string Muted{get;set;}="#55596a";
}
public static class CustomThemes {
 public static bool IsCustom(string? key)=>Regex.IsMatch(key??"",@"^custom-[a-f0-9]{32}\z");
 public static LibraryPalette? Selected(LibraryState s)=>s.SavedThemes.Find(t=>"custom-"+t.Id==s.LibraryTheme);
 static double Luminance(string hex){var rgb=Enumerable.Range(0,3).Select(i=>Convert.ToInt32(hex.Substring(1+i*2,2),16)/255d).Select(c=>c<=.04045?c/12.92:Math.Pow((c+.055)/1.055,2.4)).ToArray();return rgb[0]*.2126+rgb[1]*.7152+rgb[2]*.0722;}
 public static double Contrast(string a,string b){var x=Luminance(a);var y=Luminance(b);return (Math.Max(x,y)+.05)/(Math.Min(x,y)+.05);}
 public static void ValidatePalette(LibraryPalette p){
  if(p==null||!Regex.IsMatch(p.Id??"",@"^[a-f0-9]{32}\z")||string.IsNullOrWhiteSpace(p.Name)||p.Name.Length>60||p.Name.Any(char.IsControl))throw new LibraryError("Enter a theme name of 1–60 characters.");
  var colors=new[]{p.Ink,p.Accent,p.Page,p.Surface,p.Soft,p.Line,p.Text,p.Muted};
  if(colors.Any(c=>!Regex.IsMatch(c??"",@"^#[a-fA-F0-9]{6}\z")))throw new LibraryError("Use six-digit hex colors, such as #493276.");
  if(new[]{p.Page,p.Surface,p.Soft}.Any(bg=>Contrast(p.Text,bg)<4.5||Contrast(p.Muted,bg)<4.5||Contrast(p.Ink,bg)<4.5)||Contrast(p.Ink,"#ffffff")<4.5||Contrast(p.Ink,p.Accent)<4.5)throw new LibraryError("Increase contrast: banner/button color needs readable white and accent text; main, secondary and link text need 4.5:1 contrast on page, card and soft backgrounds.");
 }
 public static void Validate(LibraryState s){
  if(s.SavedThemes==null||s.SavedThemes.Any(t=>t==null))throw new LibraryError("Invalid saved themes.");
  foreach(var t in s.SavedThemes)ValidatePalette(t);
  if(s.SavedThemes.Select(t=>t.Id).Distinct().Count()!=s.SavedThemes.Count||s.SavedThemes.Select(t=>t.Name.Trim().ToUpperInvariant()).Distinct().Count()!=s.SavedThemes.Count)throw new LibraryError("Invalid saved themes.");
  if(IsCustom(s.LibraryTheme)&&Selected(s)==null)throw new LibraryError("Saved theme not found.");
 }
 public static object Save(LibraryState s,JsonElement input){
  LibraryPalette palette;try{palette=input.Deserialize<LibraryPalette>(Library.Json)??throw new JsonException();}catch(JsonException){throw new LibraryError("Enter a valid theme palette.");}
  palette.Name=(palette.Name??"").Trim();
  if(palette.Id=="")palette.Id=Guid.NewGuid().ToString("N");
  ValidatePalette(palette);
  var existing=s.SavedThemes.FindIndex(t=>t.Id==palette.Id);
  if(existing<0&&input.TryGetProperty("id",out var supplied)&&supplied.ValueKind==JsonValueKind.String&&supplied.GetString()!="")throw new LibraryError("Saved theme not found. Save it as a new theme.");
  if(s.SavedThemes.Any(t=>t.Id!=palette.Id&&t.Name.Equals(palette.Name,StringComparison.OrdinalIgnoreCase)))throw new LibraryError("That theme name is already saved. Choose it to edit, or enter another name.");
  if(existing<0)s.SavedThemes.Add(palette);else s.SavedThemes[existing]=palette;
  s.LibraryTheme="custom-"+palette.Id;return new{libraryTheme=s.LibraryTheme};
 }
}
