using System.Xml;
using System.Xml.Linq;
using System.Security.Cryptography;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.Logging.Abstractions;
namespace HomeLibrary;
// Only the password-encrypted archive receives these key copies. Live key files
// remain untouched. No decryptor/deserializer named by an archive is instantiated.
public static class PortableKeyring {
 public const string Protection="Portable keyring inside password-encrypted archive; borrower vault secrets remain required";
 static readonly string DescriptorType=typeof(AuthenticatedEncryptorDescriptorDeserializer).AssemblyQualifiedName!;
 public static Dictionary<string,byte[]> Export(IKeyManager manager){
  var result=new Dictionary<string,byte[]>();var keys=manager.GetAllKeys();if(keys.Count==0)throw new LibraryError("Server encryption keys are not ready. Sign in and retry the portable backup.");
  foreach(var key in keys){
   var descriptor=key.Descriptor.ExportToXml();if(descriptor.DeserializerType!=typeof(AuthenticatedEncryptorDescriptorDeserializer))throw new LibraryError("This custom encryption provider needs supervised portable recovery. No portable archive was created.");
   var xml=new XElement("key",new XAttribute("id",key.KeyId),new XAttribute("version",1),new XElement("creationDate",key.CreationDate),new XElement("activationDate",key.ActivationDate),new XElement("expirationDate",key.ExpirationDate),new XElement("descriptor",new XAttribute("deserializerType",DescriptorType),new XElement(descriptor.SerializedDescriptorElement)));
   result.Add("key-"+key.KeyId+".xml",System.Text.Encoding.UTF8.GetBytes(xml.ToString(SaveOptions.DisableFormatting)));
   if(key.IsRevoked){var revoke=new XElement("revocation",new XAttribute("version",1),new XElement("revocationDate",DateTimeOffset.UtcNow),new XElement("key",new XAttribute("id",key.KeyId)),new XElement("reason","Preserved key revocation in portable backup"));result.Add("revocation-"+key.KeyId+".xml",System.Text.Encoding.UTF8.GetBytes(revoke.ToString(SaveOptions.DisableFormatting)));}
  }
  return result;
 }
 public static void PrepareRecovered(string data,CancellationToken stop=default){
  var directory=Path.Combine(data,"keyring");if(!Directory.Exists(directory))throw new LibraryError("Portable backup encryption keys are missing.");
  var files=Directory.GetFiles(directory);if(files.Length is <1 or >10000||Directory.GetDirectories(directory).Length!=0)throw new LibraryError("Invalid portable encryption-key inventory.");var keys=0;
  foreach(var file in files){
   stop.ThrowIfCancellationRequested();if(new FileInfo(file).Length>1024*1024||Path.GetExtension(file)!=".xml")throw new LibraryError("Unsupported portable encryption-key file.");
   XElement xml;try{using var reader=XmlReader.Create(file,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=1024*1024});xml=XElement.Load(reader);}catch(XmlException){throw new LibraryError("A portable encryption-key file is damaged.");}
   if(xml.Name=="revocation"){if(xml.Attribute("version")?.Value!="1"||!Guid.TryParse(xml.Element("key")?.Attribute("id")?.Value,out _))throw new LibraryError("Invalid portable key revocation.");continue;}
   if(xml.Name!="key"||xml.Attribute("version")?.Value!="1"||!Guid.TryParse(xml.Attribute("id")?.Value,out _)||xml.Descendants("encryptedSecret").Any())throw new LibraryError("Invalid or machine-bound portable encryption key.");
   var descriptor=xml.Element("descriptor");if(descriptor?.Attribute("deserializerType")?.Value.Split(',')[0]!=typeof(AuthenticatedEncryptorDescriptorDeserializer).FullName)throw new LibraryError("Unsupported portable key algorithm provider.");
   descriptor!.SetAttributeValue("deserializerType",DescriptorType);var secrets=xml.Descendants().Where(e=>e.Name.LocalName=="masterKey").ToArray();if(secrets.Length!=1)throw new LibraryError("Portable master key is missing or duplicated.");
   try{if(Convert.FromBase64String(secrets[0].Element("value")?.Value??"").Length!=64)throw new FormatException();}catch(FormatException){throw new LibraryError("Invalid portable master key.");}
   keys++;if(OperatingSystem.IsWindows())ProtectWindows(secrets[0]);
   File.WriteAllText(file,xml.ToString(SaveOptions.DisableFormatting));
  }
  if(keys==0)throw new LibraryError("Portable backup contains no encryption keys.");
 }
 [SupportedOSPlatform("windows")] static void ProtectWindows(XElement secret){var encrypted=new DpapiXmlEncryptor(true,NullLoggerFactory.Instance).Encrypt(new XElement(secret));secret.ReplaceWith(new XElement("encryptedSecret",new XAttribute("decryptorType",encrypted.DecryptorType.AssemblyQualifiedName!),encrypted.EncryptedElement));}
}
