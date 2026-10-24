using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;

namespace HomeLibrary;

// Setup imports once as administrator. The unprivileged service opens the existing
// machine key; it never attempts a PFX import into Windows machine key storage.
[SupportedOSPlatform("windows")]
internal static class WindowsServerCertificate {
 public static X509Certificate2 Load(X509Certificate2 expected,byte[] pfx,bool provision) {
  using var store=new X509Store(StoreName.My,StoreLocation.LocalMachine);
  store.Open(provision?OpenFlags.ReadWrite:OpenFlags.ReadOnly);
  var matches=store.Certificates.Find(X509FindType.FindByThumbprint,expected.Thumbprint,false);
  X509Certificate2? result=null;
  foreach(var candidate in matches) {
   if(result==null&&candidate.HasPrivateKey&&candidate.RawData.AsSpan().SequenceEqual(expected.RawData))result=candidate;
   else candidate.Dispose();
  }
  try {
   if(result==null) {
    if(!provision)throw new InvalidOperationException("The Windows HTTPS key has not been prepared. Run the Carribrary installer again to repair it.");
    result=X509CertificateLoader.LoadPkcs12(pfx,null,X509KeyStorageFlags.MachineKeySet|X509KeyStorageFlags.PersistKeySet);
    GrantServiceRead(result);
    store.Add(result);
   } else if(provision)GrantServiceRead(result);
   // Fail during setup/startup with a useful error if the key cannot actually be used.
   using var rsa=result.GetRSAPrivateKey()??throw new CryptographicException("The HTTPS certificate has no RSA private key.");
   var proof=RandomNumberGenerator.GetBytes(32);
   var signature=rsa.SignData(proof,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);
   using var publicKey=expected.GetRSAPublicKey()!;
   if(!publicKey.VerifyData(proof,signature,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1))throw new CryptographicException("The stored Windows HTTPS key does not match the library identity.");
   return result;
  } catch {result?.Dispose();throw;}
 }
 static void GrantServiceRead(X509Certificate2 certificate) {
  using var rsa=certificate.GetRSAPrivateKey()??throw new CryptographicException("Missing HTTPS private key.");
  string folder,name;
  if(rsa is RSACng cng) {
   if(!cng.Key.IsMachineKey||cng.Key.Provider!=CngProvider.MicrosoftSoftwareKeyStorageProvider)throw new CryptographicException("Expected a Windows software machine key.");
   folder="Microsoft/Crypto/Keys";name=cng.Key.UniqueName!;
  } else if(rsa is RSACryptoServiceProvider csp) {
   if(!csp.CspKeyContainerInfo.MachineKeyStore)throw new CryptographicException("Expected a Windows machine key.");
   folder="Microsoft/Crypto/RSA/MachineKeys";name=csp.CspKeyContainerInfo.UniqueKeyContainerName;
  } else throw new CryptographicException("Unsupported Windows HTTPS key provider.");
  if(string.IsNullOrWhiteSpace(name)||Path.GetFileName(name)!=name)throw new CryptographicException("Invalid Windows private key filename.");
  var file=new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),folder.Replace('/',Path.DirectorySeparatorChar),name));
  var acl=file.GetAccessControl();
  // Only this certificate's private key receives access, not the machine-key folder.
  acl.SetAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.LocalServiceSid,null),FileSystemRights.Read,AccessControlType.Allow));
  file.SetAccessControl(acl);
 }
}
