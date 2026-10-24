using System.Text.RegularExpressions;
namespace HomeLibrary;
public static class GoogleCover {
 public static string Link(string raw) {
  if(!Uri.TryCreate(raw,UriKind.Absolute,out var u)||u.Scheme is not ("https" or "http")||u.Host!="books.google.com"||u.UserInfo!=""||!u.IsDefaultPort)return "";
  foreach(var part in u.Query.TrimStart('?').Split('&')){var p=part.Split('=',2);if(p.Length==2&&p[0]=="id") {var id=Uri.UnescapeDataString(p[1]);if(Regex.IsMatch(id,@"^[A-Za-z0-9_-]{1,100}$"))return "https://books.google.com/books?id="+id;}}
  return "";
 }
}
