using System.Text.Json;
namespace HomeLibrary;
public sealed record SelfDeletion(bool DeleteInformation=true,string Password="",string Confirmation="",string PreviewToken="");
public static class DataControlsEndpoints {
 public static void Map(WebApplication app,Settings settings,Store store,Func<HttpContext,bool> admin,Func<HttpContext,bool> mobile){
  bool Public(HttpContext c)=>c.Connection.LocalPort==settings.CatalogPort&&settings.PublicCatalogOrigin!="";
  foreach(var prefix in new[]{"/api","/mobile"})app.MapPost(prefix+"/history/preview",(HistorySelection input,HttpContext c)=>(prefix=="/api"?admin(c):mobile(c))?Results.Json(store.AdminHistoryPreview(input),Library.Json):Results.NotFound());
  app.MapPost("/account/history/preview",(HistorySelection input,HttpContext c,BorrowerAccounts accounts)=>{if(!Public(c))return Results.NotFound();var a=AccountEndpoints.Account(c,accounts);var reader=BorrowerAccounts.ApprovedReader(a,store);return Results.Json(store.PersonalHistory(reader,input),Library.Json);}).RequireRateLimiting("personal-reading");
  app.MapPost("/account/deletion-preview",(SelfDeletion input,HttpContext c,BorrowerAccounts accounts)=>{
   if(!Public(c))return Results.NotFound();var a=AccountEndpoints.Account(c,accounts);var preview=store.PreviewAccountDeletion(a,new(a.Id,input.DeleteInformation),accounts.List());
   // Do not return guest-request candidates: matching contacts do not prove ownership.
   return Results.Json(new{preview.Username,preview.PreviewToken,preview.ReadingEntries,preview.Requests,preview.ReturnedLoans,preview.ActiveLoans,preview.Holds},Library.Json);
  }).RequireRateLimiting("account-auth");
  app.MapPost("/account/delete",async(SelfDeletion input,HttpContext c,BorrowerAccounts accounts,ReadingAccess access,Reminders reminders,PushNotifications push,BorrowerVault vault)=>{
   if(!Public(c))return Results.NotFound();var a=AccountEndpoints.Account(c,accounts);accounts.VerifyPassword(a.Id,input.Password);
   using var paused=await reminders.Pause(c.RequestAborted);
   var result=JsonSerializer.SerializeToElement(vault.AfterDeletion(accounts.Delete(new(a.Id,input.DeleteInformation,Confirmation:input.Confirmation,PreviewToken:input.PreviewToken),store,access,reminders),a.Id,accounts),Library.Json);
   push.RemoveOwner("borrower:"+a.Id);c.Response.Cookies.Delete("__Host-hl-reader",new CookieOptions{Path="/",Secure=true,HttpOnly=true,SameSite=SameSiteMode.Strict});
   // The admin deletion result includes a private snapshot. Never expose it here.
   return Results.Json(new{deleted=true,informationDeleted=input.DeleteInformation,warnings=result.GetProperty("warnings")},Library.Json);
  }).RequireRateLimiting("account-auth");
  app.MapGet("/account/delete-help",(HttpContext c)=>Public(c)?Results.File(Path.Combine(app.Environment.ContentRootPath,"wwwroot","delete-help.html"),"text/html"):Results.NotFound());
 }
}
