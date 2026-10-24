using Microsoft.AspNetCore.DataProtection;
using System.Text.RegularExpressions;
namespace HomeLibrary;
// Separate from the library/exports. Protected with the server's existing keyring.
public sealed class CoverSettings {
 readonly string file;readonly IDataProtector protector;readonly object gate=new();readonly string environmentKey;
 public CoverSettings(string directory,IDataProtectionProvider protection,string environmentKey="") {file=Path.Combine(directory,"google-books-key.protected");protector=protection.CreateProtector("HomeLibrary.GoogleBooksKey.v1");this.environmentKey=environmentKey;}
 public string Key {get {lock(gate){if(environmentKey!="")return environmentKey;try{return File.Exists(file)?protector.Unprotect(File.ReadAllText(file)):"";}catch(System.Security.Cryptography.CryptographicException){return "";}}}}
 public object Status()=>new{configured=Key!="",managedByEnvironment=environmentKey!=""};
 public void Save(string key) {lock(gate){
  if(environmentKey!="")throw new LibraryError("This key is managed by the server environment.");
  key=key.Trim();if(key!=""&&!Regex.IsMatch(key,@"^[A-Za-z0-9_-]{20,200}$"))throw new LibraryError("Enter a Google Books API key, or leave it blank to disable Google lookups.");
  if(key==""){if(File.Exists(file))File.Delete(file);return;}
  File.WriteAllText(file+".tmp",protector.Protect(key));
  if(OperatingSystem.IsLinux())File.SetUnixFileMode(file+".tmp",UnixFileMode.UserRead|UnixFileMode.UserWrite);
  File.Move(file+".tmp",file,true);
 }}
}
