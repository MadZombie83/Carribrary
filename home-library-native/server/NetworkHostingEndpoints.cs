using System.Text.Json;
namespace HomeLibrary;
public sealed record HostedHostname(string Id,string Hostname,string DuckDnsToken="",bool ClearToken=false);
public sealed record GatewayExport(string Name,string Address);
public sealed record HostingId(string Id);
public static class NetworkHostingEndpoints {
 public static void Map(WebApplication app,Settings settings,Func<HttpContext,bool> allowed){
  app.MapGet("/api/network-hosting",(HttpContext c,NetworkHosting hosting)=>allowed(c)?Results.Json(new{hosting=hosting.Status(),addresses=NetworkGateway.Addresses(),gatewayActive=settings.GatewayPort>0,phonePort=settings.PhonePort,adminPort=settings.AdminPort,publicOrigin=settings.PublicCatalogOrigin,windowsAssistant="carribrary-network://setup"},Library.Json):Results.NotFound());
  app.MapPost("/api/network-hosting/create",async(HostedLibraryInput input,HttpContext c,NetworkHosting hosting)=>{if(!allowed(c))return Results.NotFound();return Results.Json(await hosting.Create(input,c.RequestAborted),Library.Json);});
  app.MapPost("/api/network-hosting/library",async(HostedLibraryChange input,HttpContext c,NetworkHosting hosting)=>{if(!allowed(c))return Results.NotFound();await hosting.Change(input,c.RequestAborted);return Results.Ok(new{saved=true});});
  app.MapPost("/api/network-hosting/hostname",async(HostedHostname input,HttpContext c,NetworkHosting hosting)=>{if(!allowed(c))return Results.NotFound();await hosting.SetHostname(input.Id,input.Hostname,c.RequestAborted,input.DuckDnsToken,input.ClearToken);return Results.Ok(new{saved=true});});
  app.MapPost("/api/network-hosting/setup-code",(HostingId input,HttpContext c,NetworkHosting hosting)=>allowed(c)?Results.Json(new{code=hosting.SetupCode(input.Id)}):Results.NotFound());
  app.MapPost("/api/network-hosting/join",async(GatewaySetup input,HttpContext c,NetworkGateway gateway,PublicConnection connection)=>{if(!allowed(c))return Results.NotFound();gateway.Configure(input);await connection.Configure(new(false,""),c.RequestAborted);return Results.Ok(new{saved=true,restartRequired=true});});
  app.MapPost("/api/network-hosting/export",(GatewayExport input,HttpContext c,NetworkGateway gateway)=>allowed(c)?Results.File(JsonSerializer.SerializeToUtf8Bytes(gateway.Export(input.Name,input.Address),Library.Json),"application/octet-stream","Carribrary-private-connection.json"):Results.NotFound());
  app.MapPost("/api/network-hosting/import",async(GatewayConnection input,HttpContext c,NetworkHosting hosting)=>{if(!allowed(c))return Results.NotFound();await hosting.Import(input,c.RequestAborted);return Results.Ok(new{connected=true});});
  app.MapPost("/api/network-hosting/peer",(HostedLibraryChange input,HttpContext c,NetworkHosting hosting)=>{if(!allowed(c))return Results.NotFound();hosting.SetPeer(input.Id,input.Enabled);return Results.Ok(new{saved=true});});
  app.MapPost("/api/network-hosting/disconnect",(HttpContext c,NetworkHosting hosting)=>{if(!allowed(c))return Results.NotFound();hosting.SaveJoin(hosting.Join with{Enabled=false});return Results.Ok(new{disabled=true});});
 }
}
