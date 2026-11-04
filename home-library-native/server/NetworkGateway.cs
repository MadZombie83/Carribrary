using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;

namespace HomeLibrary;
public sealed record GatewaySetup(string Hostname,string Address,int Port=7446,string DuckDnsToken="",bool ClearToken=false,int ExternalPhonePort=7444);
public sealed record GatewayConnection(int Format,string Id,string Name,string Hostname,string Address,int Port,string Certificate,string ServerName,string Token,int PhonePort=0,int ExternalPhonePort=0);

// A narrow, authenticated TLS bridge to the existing public listener. It never
// exposes the loopback administrator or changes borrower/companion authorization.
public sealed class NetworkGateway:IDisposable {
 readonly string data;readonly Settings settings;readonly NetworkHosting hosting;readonly IDataProtector protection;readonly bool prepareWindows;
 readonly HttpClient proxy=new(new HttpClientHandler{AllowAutoRedirect=false,UseProxy=false,UseCookies=false}){Timeout=TimeSpan.FromMinutes(10)};
 X509Certificate2? certificate;
 public NetworkGateway(string data,Settings settings,NetworkHosting hosting,IDataProtectionProvider provider,bool prepareWindows=false){this.data=data;this.settings=settings;this.hosting=hosting;this.prepareWindows=prepareWindows;protection=provider.CreateProtector("Carribrary.NetworkGateway.Certificate.v1");}
 public static string[] Addresses()=>NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.OperationalStatus==OperationalStatus.Up).SelectMany(n=>n.GetIPProperties().UnicastAddresses).Select(a=>a.Address.ToString()).Where(NetworkHosting.PrivateAddress).Distinct().ToArray();
 public static void ApplySettings(Settings settings,string data){if(settings.RecoveryRehearsal||settings.RecoveryReviewRequired)return;var join=NetworkHosting.Load(data).Join;if(join.Enabled){settings.GatewayPort=WindowsHostingReadiness.Ready(Path.Combine(data,"network-gateway-ready-"+join.Id))?join.Port:0;settings.PublicCatalogOrigin="https://"+PublicConnection.Hostname(join.Hostname);if(join.ExternalPhonePort>0)settings.RemoteAddress=$"https://{join.Hostname}:{join.ExternalPhonePort}";}}
 public X509Certificate2 Certificate {
  get{if(certificate!=null)return certificate;var id=hosting.Join.Id;if(!Guid.TryParseExact(id,"N",out _))throw new LibraryError("Configure the private library connection first.");var path=Path.Combine(data,"network-gateway-"+id+".protected");
   if(!File.Exists(path)){using var key=RSA.Create(3072);var name="gateway-"+id+".carribrary.invalid";var request=new CertificateRequest("CN="+name,key,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);var san=new SubjectAlternativeNameBuilder();san.AddDnsName(name);request.CertificateExtensions.Add(san.Build());request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false,false,0,true));request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature|X509KeyUsageFlags.KeyEncipherment,true));request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection{new("1.3.6.1.5.5.7.3.1")},true));using var cert=request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5),DateTimeOffset.UtcNow.AddYears(5));File.WriteAllBytes(path,protection.Protect(cert.Export(X509ContentType.Pfx)));if(OperatingSystem.IsLinux())File.SetUnixFileMode(path,UnixFileMode.UserRead|UnixFileMode.UserWrite);}
   var bytes=protection.Unprotect(File.ReadAllBytes(path));try{var expected=X509CertificateLoader.LoadPkcs12(bytes,null,X509KeyStorageFlags.EphemeralKeySet);if(!OperatingSystem.IsWindows())return certificate=expected;using(expected)return certificate=WindowsServerCertificate.Load(expected,bytes,prepareWindows);}finally{CryptographicOperations.ZeroMemory(bytes);}
  }
 }
 public static void ValidateCertificate(string value,string name){
  try{if(string.IsNullOrEmpty(value)||value.Length>24000||!System.Text.RegularExpressions.Regex.IsMatch(name,@"^gateway-[a-f0-9]{32}\.carribrary\.invalid$"))throw new FormatException();using var cert=X509CertificateLoader.LoadCertificate(Convert.FromBase64String(value));if(cert.NotAfter.ToUniversalTime()<=DateTime.UtcNow||cert.NotBefore.ToUniversalTime()>DateTime.UtcNow||cert.GetNameInfo(X509NameType.DnsName,false)!=name||!cert.Extensions.Cast<X509Extension>().Any(x=>x.Oid?.Value=="2.5.29.17"))throw new FormatException();}
  catch(Exception e)when(e is FormatException or CryptographicException or ArgumentException){throw new LibraryError("The private connection certificate is invalid or expired. Export a new connection file from the other library.");}
 }
 public void Configure(GatewaySetup input){
  if(settings.ManagedInstance)throw new LibraryError("Use the main server's library card to set the hostname for a library on this computer.");
  var hostname=PublicConnection.Hostname(input.Hostname);if(!Addresses().Contains(input.Address))throw new LibraryError("Choose this computer's home-network address from the list.");if(input.Port is <1024 or >65535||new[]{settings.AdminPort,settings.CatalogPort,settings.PhonePort,settings.DiscoveryPort}.Contains(input.Port))throw new LibraryError("Choose a different private connection port; this one overlaps a library port.");
  if(input.ExternalPhonePort is <1024 or >65535||input.ExternalPhonePort is 8080 or 8443)throw new LibraryError("Choose a separate external phone port, such as 7444.");var previous=hosting.Join;var token=!previous.Enabled||previous.ProtectedToken==""?hosting.Protect(Convert.ToHexString(RandomNumberGenerator.GetBytes(32))):previous.ProtectedToken;
  hosting.SaveJoin(new(previous.Id==""?Guid.NewGuid().ToString("N"):previous.Id,hostname,input.Port,token,true,hosting.DnsToken(hostname,previous.ProtectedDnsToken,input.DuckDnsToken,input.ClearToken),input.ExternalPhonePort));
 }
 public GatewayConnection Export(string name,string address){
  if(settings.RecoveryRehearsal||settings.RecoveryReviewRequired)throw new LibraryError("Complete the restored-library review first.");var join=hosting.Join;if(!join.Enabled||!Addresses().Contains(address))throw new LibraryError("Save a private connection and choose this computer's network address first.");
  return new(1,join.Id,NetworkHosting.Name(name),join.Hostname,address,join.Port,Convert.ToBase64String(Certificate.RawData),"gateway-"+join.Id+".carribrary.invalid",hosting.Reveal(join.ProtectedToken),settings.PhonePort,join.ExternalPhonePort);
 }
 public static void ValidateConnection(GatewayConnection input){
  if(input.Format!=1||!Guid.TryParseExact(input.Id,"N",out _)||!NetworkHosting.PrivateAddress(input.Address)||input.Port is <1024 or >65535||!System.Text.RegularExpressions.Regex.IsMatch(input.Token??"",@"^[A-F0-9]{64}$")||input.ServerName!="gateway-"+input.Id+".carribrary.invalid")throw new LibraryError("Choose a current Carribrary private connection file. Its address must be on the home network.");NetworkHosting.Name(input.Name);PublicConnection.Hostname(input.Hostname);ValidateCertificate(input.Certificate,input.ServerName);
 }
 public static async Task Probe(GatewayConnection input,CancellationToken stop){
  ValidateConnection(input);using var deadline=CancellationTokenSource.CreateLinkedTokenSource(stop);deadline.CancelAfter(TimeSpan.FromSeconds(8));var probeStop=deadline.Token;var expected=SHA256.HashData(Convert.FromBase64String(input.Certificate));
  using var handler=new HttpClientHandler{AllowAutoRedirect=false,UseCookies=false,UseProxy=false,ServerCertificateCustomValidationCallback=(_,cert,_,_)=>cert!=null&&cert.NotAfter.ToUniversalTime()>DateTime.UtcNow&&cert.NotBefore.ToUniversalTime()<=DateTime.UtcNow&&CryptographicOperations.FixedTimeEquals(SHA256.HashData(cert.RawData),expected)};
  using var client=new HttpClient(handler){Timeout=TimeSpan.FromSeconds(8)};using var request=new HttpRequestMessage(HttpMethod.Get,$"https://{input.Address}:{input.Port}/hosting-check");request.Headers.Add("X-Carribrary-Gateway",input.Token);
  try{using var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,probeStop);if(!response.IsSuccessStatusCode||response.Content.Headers.ContentLength>4096)throw new LibraryError("The connection did not identify the expected library. Restart the other library and check its firewall.");using var stream=await response.Content.ReadAsStreamAsync(probeStop);using var buffer=new MemoryStream();var bytes=new byte[4096];int count;while((count=await stream.ReadAsync(bytes,probeStop))>0){if(buffer.Length+count>4096)throw new LibraryError("Unexpected connection response.");buffer.Write(bytes,0,count);}using var json=JsonDocument.Parse(buffer.ToArray());if(json.RootElement.GetProperty("serverId").GetString()!=input.Id||json.RootElement.GetProperty("hostname").GetString()!=input.Hostname)throw new LibraryError("This address belongs to a different library. Export its connection file again.");}
  catch(Exception e)when(e is HttpRequestException or OperationCanceledException or JsonException or KeyNotFoundException){throw new LibraryError("The other library could not be verified. Check that it is on, restarted after setup, and allows its private connection port on the home network. No route was added.");}
 }
 static readonly HashSet<string> Hop=new(StringComparer.OrdinalIgnoreCase){"Connection","Keep-Alive","Proxy-Authenticate","Proxy-Authorization","TE","Trailer","Transfer-Encoding","Upgrade","Host","X-Carribrary-Gateway"};
 public async Task<bool> Handle(HttpContext context){
  if(settings.GatewayPort==0||context.Connection.LocalPort!=settings.GatewayPort)return false;
  var join=hosting.Join;context.Response.Headers.CacheControl="no-store";
  if(!join.Enabled||settings.RecoveryRehearsal||settings.RecoveryReviewRequired||!context.Request.IsHttps){context.Response.StatusCode=403;return true;}
  var given=context.Request.Headers["X-Carribrary-Gateway"].ToString();var token=hosting.Reveal(join.ProtectedToken);if(given.Length!=token.Length||!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(given),Encoding.UTF8.GetBytes(token))){context.Response.StatusCode=401;return true;}
  if(context.Request.Path=="/hosting-check"&&context.Request.Method=="GET"){await context.Response.WriteAsJsonAsync(new{serverId=join.Id,hostname=join.Hostname});return true;}
  if(context.Request.Method is not ("GET" or "HEAD" or "POST")){context.Response.StatusCode=405;return true;}
  var limit=context.Features.Get<IHttpMaxRequestBodySizeFeature>();if(limit is{IsReadOnly:false})limit.MaxRequestBodySize=BackupVerification.IsUpload(context.Request.Path)?BackupVerification.MaxRequestBytes:10*1024*1024;
  using var req=new HttpRequestMessage(new HttpMethod(context.Request.Method),$"http://127.0.0.1:{settings.CatalogPort}"+context.Request.Path+context.Request.QueryString);
  if(context.Request.Method=="POST")req.Content=new StreamContent(context.Request.Body);
  foreach(var h in context.Request.Headers){if(Hop.Contains(h.Key))continue;if(!req.Headers.TryAddWithoutValidation(h.Key,h.Value.ToArray())&&req.Content!=null)req.Content.Headers.TryAddWithoutValidation(h.Key,h.Value.ToArray());}
  try{using var response=await proxy.SendAsync(req,HttpCompletionOption.ResponseHeadersRead,context.RequestAborted);context.Response.StatusCode=(int)response.StatusCode;foreach(var h in response.Headers.Concat(response.Content.Headers))if(!Hop.Contains(h.Key))context.Response.Headers[h.Key]=h.Value.ToArray();await response.Content.CopyToAsync(context.Response.Body,context.RequestAborted);}
  catch(HttpRequestException){if(!context.Response.HasStarted){context.Response.StatusCode=502;await context.Response.WriteAsJsonAsync(new{error="The library is restarting. Try again shortly."});}}
  return true;
 }
 public void Dispose(){proxy.Dispose();certificate?.Dispose();}
}
