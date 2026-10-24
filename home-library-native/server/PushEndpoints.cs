namespace HomeLibrary;
public sealed record PushEndpointInput(string Endpoint);
public static class PushEndpoints {
 public static void Map(WebApplication app,Settings settings,Func<HttpContext,bool> admin){
  string Owner(HttpContext c,BorrowerAccounts accounts,BrowserAdmins browsers){if(c.Connection.LocalPort!=settings.CatalogPort||settings.PublicCatalogOrigin=="")throw new LibraryError("Not found.",404);if(c.Request.Path.StartsWithSegments("/companion")){var device=browsers.Resolve(c)??throw new LibraryError("Pair this companion first.",401);return "admin:"+device.Id;}var account=AccountEndpoints.Account(c,accounts);if(account.Status!="active")throw new LibraryError("Your account needs approval before enabling reminders.",403);return "borrower:"+account.Id;}
  app.MapGet("/api/push-status",(HttpContext c,PushNotifications push)=>admin(c)?Results.Json(push.Status()):Results.NotFound());
  foreach(var prefix in new[]{"/account","/companion"}){
   app.MapGet(prefix+"/push",(HttpContext c,PushNotifications push,BorrowerAccounts accounts,BrowserAdmins browsers)=>Results.Json(push.Options(Owner(c,accounts,browsers)))).RequireRateLimiting("personal-reading");
   app.MapPost(prefix+"/push",(WebSubscription sub,HttpContext c,PushNotifications push,BorrowerAccounts accounts,BrowserAdmins browsers)=>{push.Subscribe(Owner(c,accounts,browsers),sub);return Results.Ok(new{subscribed=true});}).RequireRateLimiting("personal-reading");
   app.MapPost(prefix+"/push/remove",(PushEndpointInput input,HttpContext c,PushNotifications push,BorrowerAccounts accounts,BrowserAdmins browsers)=>{push.Remove(Owner(c,accounts,browsers),input.Endpoint);return Results.Ok(new{removed=true});}).RequireRateLimiting("personal-reading");
   app.MapPost(prefix+"/push/test",async(PushEndpointInput input,HttpContext c,PushNotifications push,BorrowerAccounts accounts,BrowserAdmins browsers)=>{await push.Test(Owner(c,accounts,browsers),input.Endpoint,c.RequestAborted);return Results.Ok(new{accepted=true});}).RequireRateLimiting("book-requests");
  }
 }
}
