using System.Net;
using System.Text.RegularExpressions;
namespace HomeLibrary;
public static class WelcomePage {
 public static IResult Render(Store store,string root){
  var state=store.Read().State;
  // Explicit public presentation fields only. Never serialize library state or access tokens.
  var text=new Dictionary<string,string>{
   ["@@LIBRARY_NAME@@"]=WebUtility.HtmlEncode(state.LibraryName),
   ["@@TAGLINE@@"]=string.IsNullOrEmpty(state.BannerSubtitle)?"":"<p class=\"welcome-tagline\">"+WebUtility.HtmlEncode(state.BannerSubtitle)+"</p>",
   ["@@SAYING@@"]=string.IsNullOrEmpty(state.EncouragingMessage)?"":"<p class=\"welcome-saying\">"+WebUtility.HtmlEncode(state.EncouragingMessage)+"</p>",
   ["@@ARTWORK@@"]=state.LibrarySidebarArtwork==""?"":"<img class=\"welcome-artwork\" src=\"data:image/png;base64,"+WebUtility.HtmlEncode(state.LibrarySidebarArtwork)+"\" alt=\"\" aria-hidden=\"true\">"
  };
  var html=Regex.Replace(File.ReadAllText(Path.Combine(root,"wwwroot","welcome.html")),"@@(?:LIBRARY_NAME|TAGLINE|SAYING|ARTWORK)@@",m=>text[m.Value]);
  return Results.Content(html,"text/html; charset=utf-8");
 }
}
