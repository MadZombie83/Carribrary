using System.Text.Json;
using System.Net;
namespace HomeLibrary;
public sealed record BrowserEnrollment(string Code,string Name);
public static class CompanionEndpoints {
 public static void Map(WebApplication app,Settings settings,Store store,Func<HttpContext,bool> admin,Func<HttpContext,bool> mobile){
  bool Public(HttpContext c)=>c.Connection.LocalPort==settings.CatalogPort&&settings.PublicCatalogOrigin!="";
  foreach(var prefix in new[]{"/api","/mobile"}){
   app.MapGet(prefix+"/browser-admins",(HttpContext c,BrowserAdmins browsers)=>(prefix=="/api"?admin(c):mobile(c))?Results.Json(new{devices=browsers.List()}):Results.NotFound());
   app.MapPost(prefix+"/browser-admins/pair",(HttpContext c,BrowserAdmins browsers)=>(prefix=="/api"?admin(c):mobile(c))?Results.Json(browsers.Pair()):Results.NotFound());
   app.MapPost(prefix+"/browser-admins/revoke",(JsonElement input,HttpContext c,BrowserAdmins browsers,PushNotifications push)=>{if(!(prefix=="/api"?admin(c):mobile(c)))return Results.NotFound();var id=input.GetProperty("deviceId").GetString()??"";browsers.Revoke(id);push.RemoveOwner("admin:"+id);return Results.Ok(new{revoked=true});});
  }
  app.MapPost("/companion/enroll",(BrowserEnrollment input,HttpContext c,BrowserAdmins browsers)=>{if(!Public(c))return Results.NotFound();var enrolled=browsers.Enroll(input.Code,input.Name);c.Response.Cookies.Append(BrowserAdmins.Cookie,enrolled.Token,new CookieOptions{Path="/companion",Secure=true,HttpOnly=true,SameSite=SameSiteMode.Strict,MaxAge=TimeSpan.FromDays(30)});return Results.Ok(new{paired=true});}).RequireRateLimiting("pair");
  app.MapPost("/companion/logout",(HttpContext c,BrowserAdmins browsers,PushNotifications push)=>{if(!Public(c))return Results.NotFound();var d=browsers.Resolve(c);if(d!=null){browsers.Revoke(d.Id);push.RemoveOwner("admin:"+d.Id);}c.Response.Cookies.Delete(BrowserAdmins.Cookie,new CookieOptions{Path="/companion",Secure=true,HttpOnly=true,SameSite=SameSiteMode.Strict});return Results.Ok(new{signedOut=true});});
  app.MapMethods("/companion/api/{**leaf}",new[]{"GET","POST"},async(string? leaf,HttpContext c,BrowserAdmins browsers,IHttpClientFactory factory)=>{
   if(!Public(c))return Results.NotFound();if(browsers.Resolve(c)==null)throw new LibraryError("Pair this iPhone/browser with your library server.",401);
   // Authenticated, same-origin browser admin only. Never proxy arbitrary paths/hosts or redirects.
   if(leaf==null||!System.Text.RegularExpressions.Regex.IsMatch(leaf,@"^[a-zA-Z0-9/_-]+$")||leaf.Contains(".."))return Results.NotFound();
   if(leaf.StartsWith("desktop/")||leaf.StartsWith("browser-admins/")||leaf=="browser-admins")throw new LibraryError("Manage paired browsers directly on the library server or native Android admin.",403);
   using var req=new HttpRequestMessage(new HttpMethod(c.Request.Method),$"http://127.0.0.1:{settings.AdminPort}/api/"+leaf+c.Request.QueryString);
   req.Headers.TryAddWithoutValidation(LocalAdministrators.CompanionHeader,c.Request.Cookies[BrowserAdmins.Cookie]);
   req.Headers.TryAddWithoutValidation("Origin",$"http://127.0.0.1:{settings.AdminPort}");if(c.Request.Method=="POST"){req.Content=new StreamContent(c.Request.Body);req.Content.Headers.ContentType=new("application/json");}
   using var result=await factory.CreateClient("companion-proxy").SendAsync(req,HttpCompletionOption.ResponseHeadersRead,c.RequestAborted);c.Response.StatusCode=(int)result.StatusCode;c.Response.ContentType=result.Content.Headers.ContentType?.ToString()??"application/json";await result.Content.CopyToAsync(c.Response.Body,c.RequestAborted);return Results.Empty;
  }).RequireRateLimiting("personal-reading");
  app.MapGet("/companion/{**leaf}",(string? leaf,HttpContext c,BrowserAdmins browsers)=>{
   if(!Public(c))return Results.NotFound();var root=Path.Combine(app.Environment.ContentRootPath,"wwwroot");
   c.Response.Headers["Content-Security-Policy"]="default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https: http:; connect-src 'self' https://openlibrary.org; media-src 'self' blob:; object-src 'none'; base-uri 'self'; frame-ancestors 'none'";
   if(leaf=="manifest.webmanifest")return BorrowerApp.Manifest(store,true);
   if(leaf=="admin-sw.js")return Results.File(Path.Combine(root,"admin-sw.js"),"text/javascript");
   if(leaf=="companion-login.js")return Results.File(Path.Combine(root,leaf),"text/javascript");
   if(string.IsNullOrEmpty(leaf)){
    if(browsers.Resolve(c)==null)return Results.File(Path.Combine(root,"companion-login.html"),"text/html");
    var html=File.ReadAllText(Path.Combine(root,"index.html")).Replace("</head>","<link rel=\"manifest\" href=\"manifest.webmanifest\"><link rel=\"apple-touch-icon\" href=\"/account/library-icon-192.png\"><link rel=\"stylesheet\" href=\"companion.css\"><script src=\"companion-boot.js\"></script></head>").Replace("href=\"/help.html\"","href=\"help.html\"");return Results.Content(html,"text/html");
   }
   var files=new HashSet<string>{"sidebar-artwork.js","dashboard-accounts.js","admin-dashboard.js","admin-shell.css","reading-list.js","app.js","companion-boot.js","companion.css","library-branding.js","book-about.js","styles.css","library.js","copy-labels.js","camera.js","qr.js","qrcodegen.js","devices.js","history.js","quick-start.html","quick-start.css","quick-start.js","guide/catalog.png","guide/add-book.png","guide/checkout.png","help.html","help.js","help.json","favicon.svg","favicon.ico","cover-credit.js","powered-by-google.png","vendor/zxing-browser-0.2.1.min.js","push-client.js"};
   if(!files.Contains(leaf)||browsers.Resolve(c)==null)return Results.NotFound();var type=leaf.EndsWith(".js")?"text/javascript":leaf.EndsWith(".css")?"text/css":leaf.EndsWith(".html")?"text/html":leaf.EndsWith(".json")?"application/json":leaf.EndsWith(".png")?"image/png":leaf.EndsWith(".svg")?"image/svg+xml":"image/x-icon";
   return Results.File(Path.Combine(root,leaf),type);
  });
 }
}
