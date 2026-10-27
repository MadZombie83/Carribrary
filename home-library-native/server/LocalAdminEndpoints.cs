namespace HomeLibrary;
public static class LocalAdminEndpoints {
 public static string Cookie(Settings settings)=>LocalAdministrators.Cookie+(settings.RecoveryRehearsal?"-rehearsal":"");
 public static void Map(WebApplication app,Settings settings,Func<HttpContext,bool> admin,Func<HttpContext,bool> mobile){
  bool Local(HttpContext c)=>c.Connection.LocalPort==settings.AdminPort;
  void SetCookie(HttpContext c,string token)=>c.Response.Cookies.Append(Cookie(settings),token,new CookieOptions{HttpOnly=true,Secure=false,SameSite=SameSiteMode.Strict,Path="/",MaxAge=TimeSpan.FromHours(8),IsEssential=true});
  app.MapGet("/local-auth/health",(HttpContext c)=>Local(c)?Results.Json(new{ready=true,version=SetupStatus.Version,recoveryRehearsal=settings.RecoveryRehearsal}):Results.NotFound());
  app.MapGet("/local-auth/status",(HttpContext c,LocalAdministrators accounts)=>Local(c)?Results.Json(new{configured=accounts.Configured,authenticated=accounts.Authorized(c.Request.Cookies[Cookie(settings)]),setupFile=accounts.Configured?null:accounts.SetupFile}):Results.NotFound());
  app.MapPost("/local-auth/login",(LocalAdminInput input,HttpContext c,LocalAdministrators accounts)=>{if(!Local(c))return Results.NotFound();SetCookie(c,accounts.Login(input));return Results.Ok(new{signedIn=true});}).RequireRateLimiting("local-admin-login");
  app.MapPost("/local-auth/enroll",(LocalAdminInput input,HttpContext c,LocalAdministrators accounts)=>{if(!Local(c))return Results.NotFound();var result=accounts.Enroll(input);SetCookie(c,result.Token);return Results.Ok(new{recoveryCode=result.RecoveryCode});}).RequireRateLimiting("local-admin-enroll");
  app.MapPost("/local-auth/recover",(LocalAdminInput input,HttpContext c,LocalAdministrators accounts)=>{if(!Local(c))return Results.NotFound();var result=accounts.Recover(input);SetCookie(c,result.Token);return Results.Ok(new{recoveryCode=result.RecoveryCode});}).RequireRateLimiting("local-admin-recover");
  app.MapPost("/local-auth/logout",(HttpContext c,LocalAdministrators accounts)=>{if(!Local(c))return Results.NotFound();accounts.Logout(c.Request.Cookies[Cookie(settings)]);c.Response.Cookies.Delete(Cookie(settings),new CookieOptions{HttpOnly=true,SameSite=SameSiteMode.Strict,Path="/"});return Results.Ok(new{signedOut=true});});
  foreach(var prefix in new[]{"/api","/mobile"}){
   bool Allowed(HttpContext c)=>prefix=="/api"?admin(c):mobile(c);
   app.MapGet(prefix+"/local-admin/users",(HttpContext c,LocalAdministrators accounts)=>Allowed(c)?Results.Json(new{administrators=accounts.List(),current=accounts.Current(c.Request.Cookies[Cookie(settings)])}):Results.NotFound());
   app.MapPost(prefix+"/local-admin/users/add",(LocalAdminInput input,HttpContext c,LocalAdministrators accounts)=>{if(!Allowed(c))return Results.NotFound();var result=accounts.Add(input);return Results.Json(new{administrator=result.Administrator,recoveryCode=result.RecoveryCode});}).RequireRateLimiting("local-admin-enroll");
   app.MapPost(prefix+"/local-admin/users/reset",(LocalAdminAccountChange input,HttpContext c,LocalAdministrators accounts)=>{if(!Allowed(c))return Results.NotFound();return Results.Json(new{recoveryCode=accounts.Reset(input,accounts.Current(c.Request.Cookies[Cookie(settings)])?.Id)});}).RequireRateLimiting("local-admin-recover");
   app.MapPost(prefix+"/local-admin/users/remove",(LocalAdminAccountChange input,HttpContext c,LocalAdministrators accounts)=>{if(!Allowed(c))return Results.NotFound();accounts.Remove(input.Id,accounts.Current(c.Request.Cookies[Cookie(settings)])?.Id);return Results.Ok(new{removed=true});});
  }
  app.MapPost("/api/local-admin/password",(LocalAdminPasswordChange input,HttpContext c,LocalAdministrators accounts)=>{if(!Local(c)||!admin(c))return Results.NotFound();var result=accounts.ChangePassword(c.Request.Cookies[Cookie(settings)],input);SetCookie(c,result.Token);return Results.Json(new{recoveryCode=result.RecoveryCode});}).RequireRateLimiting("local-admin-recover");
  // Existing privileged companions may assist enrollment, but do not bypass proof checks.
  foreach(var prefix in new[]{"/api","/mobile"})app.MapPost(prefix+"/local-admin/setup-code",(HttpContext c,LocalAdministrators accounts)=>{if(!(prefix=="/api"?admin(c):mobile(c)))return Results.NotFound();accounts.PrepareRecovery();return Results.Json(new{code=File.ReadAllText(accounts.SetupFile).Trim(),purpose=accounts.Configured?"recovery":"setup"});});
 }
}
