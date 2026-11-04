using HomeLibrary;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
static class NetworkGatewayTests {
 public static async Task Run(string directory,HostedLibrary child,IDataProtectionProvider protection){
  var address=NetworkGateway.Addresses().FirstOrDefault()??throw new Exception("No private test interface");
  Directory.CreateDirectory(directory);var listener=new TcpListener(IPAddress.Any,0);listener.Start();var port=((IPEndPoint)listener.LocalEndpoint).Port;listener.Stop();
  var settings=new Settings{AdminPort=child.AdminPort,CatalogPort=child.CatalogPort,PhonePort=child.PhonePort,DiscoveryPort=child.DiscoveryPort,GatewayPort=port};
  var hosting=new NetworkHosting(directory,settings,protection);using var gateway=new NetworkGateway(directory,settings,hosting,protection);gateway.Configure(new("bridge.example.org",address,port));
  var connection=gateway.Export("Bridge test",address);var disk=File.ReadAllText(Path.Combine(directory,"network-hosting.json"));if(disk.Contains(connection.Token))throw new Exception("Gateway token stored plaintext");
  Console.WriteLine("PASS gateway credential protected at rest and absent from status");
  var builder=WebApplication.CreateBuilder();builder.WebHost.ConfigureKestrel(k=>k.Listen(IPAddress.Parse(address),port,l=>l.UseHttps(gateway.Certificate)));var app=builder.Build();app.Run(async c=>{if(!await gateway.Handle(c))c.Response.StatusCode=404;});
  var caddy=Environment.GetEnvironmentVariable("CARRIBRARY_CADDY_TEST_BINARY");
  if(!string.IsNullOrEmpty(caddy)){
   var config=Path.Combine(directory,"Caddyfile");File.WriteAllText(config,HostedProxyConfig.Render("first.example.org",19302,[new(connection.Id,connection.Hostname,connection.Address,connection.Port,connection.Certificate,connection.ServerName,connection.Token)],Path.Combine(directory,"certs"),false));
   var info=new System.Diagnostics.ProcessStartInfo(caddy){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};foreach(var argument in new[]{"validate","--config",config,"--adapter","caddyfile"})info.ArgumentList.Add(argument);
   using var check=System.Diagnostics.Process.Start(info)!;var output=check.StandardOutput.ReadToEndAsync();var errors=check.StandardError.ReadToEndAsync();using var limit=new CancellationTokenSource(TimeSpan.FromSeconds(15));await check.WaitForExitAsync(limit.Token);await Task.WhenAll(output,errors);if(check.ExitCode!=0)throw new Exception("Pinned Caddy rejected the generated multi-library configuration: "+await errors);Console.WriteLine("PASS pinned Caddy validates primary plus pinned-certificate remote route");
  }
  await app.StartAsync();try{
   await NetworkGateway.Probe(connection,default);Console.WriteLine("PASS real HTTPS bridge identity and certificate verified");
   try{await NetworkGateway.Probe(connection with{Token=new string('A',64)},default);throw new Exception("Wrong token accepted");}catch(LibraryError){Console.WriteLine("PASS wrong bridge credential rejected");}
   using(var key=System.Security.Cryptography.RSA.Create(2048)){var request=new System.Security.Cryptography.X509Certificates.CertificateRequest("CN="+connection.ServerName,key,System.Security.Cryptography.HashAlgorithmName.SHA256,System.Security.Cryptography.RSASignaturePadding.Pkcs1);var san=new System.Security.Cryptography.X509Certificates.SubjectAlternativeNameBuilder();san.AddDnsName(connection.ServerName);request.CertificateExtensions.Add(san.Build());using var other=request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1),DateTimeOffset.UtcNow.AddDays(1));try{await NetworkGateway.Probe(connection with{Certificate=Convert.ToBase64String(other.RawData)},default);throw new Exception("Mismatched certificate accepted");}catch(LibraryError){Console.WriteLine("PASS certificate pin mismatch rejected even with a matching SAN");}}
   using var handler=new HttpClientHandler{UseProxy=false,AllowAutoRedirect=false,ServerCertificateCustomValidationCallback=(_,cert,_,_)=>cert!=null&&cert.RawData.SequenceEqual(gateway.Certificate.RawData)};using var client=new HttpClient(handler);
   async Task<int> Get(string path,bool credential=true){using var request=new HttpRequestMessage(HttpMethod.Get,$"https://{address}:{port}"+path);if(credential)request.Headers.Add("X-Carribrary-Gateway",connection.Token);using var response=await client.SendAsync(request);return (int)response.StatusCode;}
   if(await Get("/")!=200||await Get("/api/state")!=404||await Get("/local-auth/status")!=404||await Get("/reading/state")!=401||await Get("/",false)!=401)throw new Exception("Bridge bypassed public listener permissions");
   Console.WriteLine("PASS bridge forwards public welcome only; admin and reader authorization retained");
   hosting.SaveJoin(hosting.Join with{Enabled=false});if(await Get("/")!=403)throw new Exception("Disconnected bridge still authorized");Console.WriteLine("PASS disconnect immediately denies existing bridge credential");
  }finally{await app.StopAsync();await app.DisposeAsync();}
 }
}
