using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
namespace HomeLibrary;
public static class AccountRateLimits {
 public static void Configure(RateLimiterOptions options){
  options.RejectionStatusCode=429;
  options.OnRejected=async(context,stop)=>{
   if(context.Lease.TryGetMetadata(MetadataName.RetryAfter,out var after))context.HttpContext.Response.Headers.RetryAfter=Math.Max(1,(int)Math.Ceiling(after.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
   context.HttpContext.Response.Headers.CacheControl="no-store";
   await context.HttpContext.Response.WriteAsJsonAsync(new{error="Too many attempts. Wait before trying again."},stop);
  };
  void Add(string name,int count,TimeSpan window)=>options.AddPolicy(name,context=>RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString()??"unknown",_=>new(){PermitLimit=count,Window=window,QueueLimit=0}));
  // Separate signup/reset budgets prevent those endpoints consuming the login budget.
  Add("local-admin-login",20,TimeSpan.FromMinutes(15));Add("local-admin-enroll",6,TimeSpan.FromMinutes(15));Add("local-admin-recover",6,TimeSpan.FromMinutes(15));
  Add("account-register",5,TimeSpan.FromMinutes(15));Add("account-login",20,TimeSpan.FromMinutes(15));Add("account-reset",10,TimeSpan.FromMinutes(15));Add("account-reset-issue",6,TimeSpan.FromMinutes(1));
  Add("account-auth",20,TimeSpan.FromMinutes(15)); // Existing authenticated account-deletion routes.
  Add("personal-reading",120,TimeSpan.FromMinutes(1));Add("pair",10,TimeSpan.FromMinutes(1));Add("book-requests",10,TimeSpan.FromMinutes(15));
 }
}
