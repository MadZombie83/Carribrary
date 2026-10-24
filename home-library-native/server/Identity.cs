using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;
using System.Text;
namespace HomeLibrary;
public sealed record Device(string Id,string Name,string Certificate,string KeyHash,string Issued,bool Revoked=false);
public sealed record PairSession(string Code,string Expires,string ServerUrl,string ServerKey,string ServerId,int Version=1);
public sealed record EnrollRequest(string Code,string Name,string PublicKey,string Signature);
public sealed class Identity : IDisposable {
 readonly bool prepareWindowsKeys;readonly object gate=new();readonly string directory;readonly IDataProtector protect;
 readonly X509Certificate2 ca;public X509Certificate2 Server{get;}public string ServerId{get;}
 readonly List<Device> devices;readonly Dictionary<string,DateTimeOffset> pairing=[];
 public static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
 public string ServerKey=>Convert.ToBase64String(SHA256.HashData(Server.PublicKey.ExportSubjectPublicKeyInfo()));
 public Identity(string directory,IDataProtectionProvider protection,bool prepareWindowsKeys=false){
 this.prepareWindowsKeys=prepareWindowsKeys;this.directory=directory;protect=protection.CreateProtector("HomeLibrary.Certificates.v1");
 ca=LoadOrCreate("device-ca",()=>{using var key=RSA.Create(3072);var r=new CertificateRequest("CN=Home Library Device Authority",key,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);r.CertificateExtensions.Add(new X509BasicConstraintsExtension(true,false,0,true));r.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign|X509KeyUsageFlags.CrlSign,true));return r.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5),DateTimeOffset.UtcNow.AddYears(10));});
 Server=LoadOrCreate("server",()=>{using var key=RSA.Create(3072);var r=new CertificateRequest("CN=Home Library",key,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);r.CertificateExtensions.Add(new X509BasicConstraintsExtension(false,false,0,true));r.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature|X509KeyUsageFlags.KeyEncipherment,true));r.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new System.Security.Cryptography.OidCollection{new("1.3.6.1.5.5.7.3.1")},true));return r.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5),DateTimeOffset.UtcNow.AddYears(5));});
 ServerId=Hash(Server.PublicKey.ExportSubjectPublicKeyInfo());
 var path=Path.Combine(directory,"devices.json");devices=File.Exists(path)?JsonSerializer.Deserialize<List<Device>>(File.ReadAllText(path),Library.Json)??[]:[];
 }
 X509Certificate2 LoadOrCreate(string name,Func<X509Certificate2> create){
  var path=Path.Combine(directory,name+".protected");
  if(!File.Exists(path)){using var cert=create();File.WriteAllBytes(path,protect.Protect(cert.Export(X509ContentType.Pfx)));}
  var pfx=protect.Unprotect(File.ReadAllBytes(path));
  try {
   var certificate=X509CertificateLoader.LoadPkcs12(pfx,null,X509KeyStorageFlags.EphemeralKeySet);
   if(name!="server"||!OperatingSystem.IsWindows())return certificate;
   using(certificate)return WindowsServerCertificate.Load(certificate,pfx,prepareWindowsKeys);
  } finally {CryptographicOperations.ZeroMemory(pfx);}
 }

 void Save(){var file=Path.Combine(directory,"devices.json");File.WriteAllText(file+".tmp",JsonSerializer.Serialize(devices,Library.Json));File.Move(file+".tmp",file,true);}
 public Device[] Devices(){lock(gate)return devices.ToArray();}
 public PairSession StartPair(string url){lock(gate){
 if(!Uri.TryCreate(url,UriKind.Absolute,out var u)||u.Scheme!="https"||!string.IsNullOrEmpty(u.UserInfo)||u.AbsolutePath!="/"||!string.IsNullOrEmpty(u.Query)||!string.IsNullOrEmpty(u.Fragment))throw new LibraryError("Enter an HTTPS server address, including port 7443, without a path.");
 foreach(var old in pairing.Where(x=>x.Value<DateTimeOffset.UtcNow).Select(x=>x.Key).ToArray())pairing.Remove(old);
 var code=Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)).TrimEnd('=').Replace('+','-').Replace('/','_');var expires=DateTimeOffset.UtcNow.AddMinutes(5);pairing[Hash(Encoding.UTF8.GetBytes(code))]=expires;
 return new(code,expires.ToString("O"),u.GetLeftPart(UriPartial.Authority),ServerKey,ServerId);
 }}
 public object Enroll(EnrollRequest input){lock(gate){
 var codeHash=Hash(Encoding.UTF8.GetBytes(input.Code??""));if(!pairing.TryGetValue(codeHash,out var expiry)||expiry<DateTimeOffset.UtcNow)throw new LibraryError("Pairing code expired or already used.",401);
 if(string.IsNullOrWhiteSpace(input.Name)||input.Name.Length>100)throw new LibraryError("Enter a device name.");
 byte[] spki,signature;try{spki=Convert.FromBase64String(input.PublicKey);signature=Convert.FromBase64String(input.Signature);}catch{throw new LibraryError("Invalid device proof.");}
 using var key=RSA.Create();try{key.ImportSubjectPublicKeyInfo(spki,out var consumed);if(consumed!=spki.Length||key.KeySize<2048||key.KeySize>4096)throw new Exception();}catch{throw new LibraryError("Invalid device public key.");}
 var challenge=Encoding.UTF8.GetBytes("home-library-pair:v1:"+input.Code+":"+input.PublicKey);
 if(!key.VerifyData(challenge,signature,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1))throw new LibraryError("Invalid device proof.",401);
 var id=Guid.NewGuid().ToString();var request=new CertificateRequest("CN="+id,key,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);
 request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false,false,0,true));request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature,true));request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new System.Security.Cryptography.OidCollection{new("1.3.6.1.5.5.7.3.2")},true));
 using var cert=request.Create(ca,DateTimeOffset.UtcNow.AddMinutes(-5),DateTimeOffset.UtcNow.AddYears(2),RandomNumberGenerator.GetBytes(16));
 var device=new Device(id,input.Name.Trim(),Convert.ToBase64String(cert.RawData),Hash(spki),DateTimeOffset.UtcNow.ToString("O"));devices.Add(device);try{Save();}catch{devices.Remove(device);throw;}pairing.Remove(codeHash);
 return new{deviceId=id,certificate=device.Certificate,caCertificate=Convert.ToBase64String(ca.RawData),serverId=ServerId};
 }}
 public Device? Approved(X509Certificate2? cert){if(cert==null)return null;lock(gate){if(DateTime.UtcNow<cert.NotBefore.ToUniversalTime()||DateTime.UtcNow>cert.NotAfter.ToUniversalTime())return null;var encoded=Convert.ToBase64String(cert.RawData);var device=devices.Find(x=>!x.Revoked&&x.Certificate==encoded);if(device==null)return null;using var chain=new X509Chain();chain.ChainPolicy.TrustMode=X509ChainTrustMode.CustomRootTrust;chain.ChainPolicy.CustomTrustStore.Add(ca);chain.ChainPolicy.RevocationMode=X509RevocationMode.NoCheck;chain.ChainPolicy.ApplicationPolicy.Add(new("1.3.6.1.5.5.7.3.2"));return chain.Build(cert)?device:null;}}
 public void Revoke(string id){lock(gate){var index=devices.FindIndex(x=>x.Id==id);if(index<0)throw new LibraryError("Device not found.");var old=devices[index];devices[index]=old with{Revoked=true};try{Save();}catch{devices[index]=old;throw;}}}
 public void Dispose(){Server.Dispose();ca.Dispose();}
}
