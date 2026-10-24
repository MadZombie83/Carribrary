using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
namespace HomeLibrary;

public sealed record LocalAdminInput(string Username,string Password,string Code="");
public sealed record LocalAdminAccount(string Id,string Username,string Salt,string PasswordHash,string RecoveryHash);
public sealed record LocalAdminCredentials(string Username,string Salt,string PasswordHash,string RecoveryHash,string SetupHash="",List<LocalAdminAccount>? Administrators=null,int Version=1);
public sealed record LocalAdminSession(string Hash,DateTimeOffset Expires,DateTimeOffset LastSeen,string AdministratorId="primary");
public sealed record LocalAdminSummary(string Id,string Username,bool Primary);
public sealed record LocalAdminAccountChange(string Id,string Password="");
public sealed record LocalAdminPasswordChange(string CurrentPassword,string Password);

// Separate dashboard identities; approved companion tokens remain independent.
public sealed class LocalAdministrators : IDisposable {
 public const string Cookie="carribrary-local-admin";
 public const string CompanionHeader="X-Carribrary-Companion";
 const int Iterations=600_000;
 readonly object gate=new();readonly string file,codeFile;readonly IDataProtector protector;readonly FileStream processLock;readonly TimeProvider clock;
 readonly Dictionary<string,LocalAdminSession> sessions=[];
 LocalAdminCredentials state;
 public LocalAdministrators(string directory,IDataProtectionProvider provider,TimeProvider? time=null){
  clock=time??TimeProvider.System;file=Path.Combine(directory,"local-admin.protected");codeFile=Path.Combine(directory,"local-admin-setup.txt");
  processLock=new FileStream(Path.Combine(directory,"local-admin.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
  protector=provider.CreateProtector("Carribrary.LocalAdministrator.v2");
  try{
   if(File.Exists(file)){
    var bytes=File.ReadAllBytes(file);byte[] plain;bool legacy=false;
    try{plain=protector.Unprotect(bytes);}catch(CryptographicException){plain=provider.CreateProtector("Carribrary.LocalAdministrator.v1").Unprotect(bytes);legacy=true;}
    state=JsonSerializer.Deserialize<LocalAdminCredentials>(plain,Library.Json)??throw new LibraryError("Local administrator credentials are unreadable. Use a verified complete backup; do not delete credentials.");
    if(state.Version is <1 or >2)throw new LibraryError("Unsupported administrator credential format. Use compatible server programs.");
    if(legacy||state.Version==1){state=state with{Administrators=state.Administrators??[],Version=2};Save(state);}
   }else{var code=Secret();state=new("",Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),"","",Hash(code),[],2);Save(state);WriteCode(code);}
  }catch{processLock.Dispose();throw;}
 }
 public bool Configured{get{lock(gate)return state.Username!="";}}
 public string SetupFile=>codeFile;
 IEnumerable<LocalAdminAccount> Accounts()=>new[]{new LocalAdminAccount("primary",state.Username,state.Salt,state.PasswordHash,state.RecoveryHash)}.Concat(state.Administrators??[]);
 public LocalAdminSummary[] List(){lock(gate)return Accounts().Where(a=>a.Username!="").Select(a=>new LocalAdminSummary(a.Id,a.Username,a.Id=="primary")).ToArray();}
 static string Secret()=>Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');
 static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
 static bool Equal(string a,string b)=>CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a),Encoding.UTF8.GetBytes(b));
 static string PasswordHash(string password,string salt)=>Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(password,Convert.FromBase64String(salt),Iterations,HashAlgorithmName.SHA256,32));
 void Save(LocalAdminCredentials value){var bytes=protector.Protect(JsonSerializer.SerializeToUtf8Bytes(value,Library.Json));File.WriteAllBytes(file+".tmp",bytes);if(!OperatingSystem.IsWindows())File.SetUnixFileMode(file+".tmp",UnixFileMode.UserRead|UnixFileMode.UserWrite);File.Move(file+".tmp",file,true);}
 void WriteCode(string code){File.WriteAllText(codeFile+".tmp",code+Environment.NewLine);if(!OperatingSystem.IsWindows())File.SetUnixFileMode(codeFile+".tmp",UnixFileMode.UserRead|UnixFileMode.UserWrite);File.Move(codeFile+".tmp",codeFile,true);}
 static void Validate(LocalAdminInput input){if(string.IsNullOrWhiteSpace(input.Username)||input.Username.Trim().Length>64||input.Username.Any(char.IsControl))throw new LibraryError("Choose an administrator username (1–64 characters).");if(input.Password is null||input.Password.Length is <12 or >128)throw new LibraryError("Use an administrator password of 12–128 characters.");}
 void UniqueName(string name,string except=""){if(Accounts().Any(a=>a.Id!=except&&a.Username.Equals(name.Trim(),StringComparison.OrdinalIgnoreCase)))throw new LibraryError("That administrator username is already used.",409);}
 public (string Token,string RecoveryCode) Enroll(LocalAdminInput input){lock(gate){if(Configured)throw new LibraryError("An administrator is already configured. Sign in or use account recovery.",409);return ReplacePrimary(input,state.SetupHash);}}
 public (string Token,string RecoveryCode) Recover(LocalAdminInput input){lock(gate){
  if(!Configured)throw new LibraryError("Complete first-run administrator setup first.",409);
  if(Proof(input.Code,state.RecoveryHash)||Proof(input.Code,state.SetupHash))return ReplacePrimary(input,state.RecoveryHash,state.SetupHash);
  var account=(state.Administrators??[]).Find(a=>a.Username.Equals(input.Username?.Trim(),StringComparison.OrdinalIgnoreCase));
  if(account==null||!Proof(input.Code,account.RecoveryHash))throw InvalidProof();Validate(input);var (updated,recovery)=NewCredentials(account.Id,account.Username,input.Password);
  CommitAccount(updated);Invalidate(account.Id);return(NewSession(account.Id),recovery);
 }}
 static bool Proof(string? value,string hash)=>value?.Length==43&&hash!=""&&Equal(hash,Hash(value));
 static LibraryError InvalidProof()=>new("The setup or recovery code is invalid or has already been used.",401);
 (string Token,string RecoveryCode) ReplacePrimary(LocalAdminInput input,params string[] proofs){
  if(!proofs.Any(p=>Proof(input.Code,p)))throw InvalidProof();Validate(input);UniqueName(input.Username,"primary");
  var (updated,recovery)=NewCredentials("primary",input.Username.Trim(),input.Password);
  var replacement=state with{Username=updated.Username,Salt=updated.Salt,PasswordHash=updated.PasswordHash,RecoveryHash=updated.RecoveryHash,SetupHash=""};Save(replacement);state=replacement;sessions.Clear();
  try{File.Delete(codeFile);}catch(IOException){}catch(UnauthorizedAccessException){}return(NewSession("primary"),recovery);
 }
 static (LocalAdminAccount Account,string RecoveryCode) NewCredentials(string id,string name,string password){var recovery=Secret();var salt=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));return(new(id,name,salt,PasswordHash(password,salt),Hash(recovery)),recovery);}
 void CommitAccount(LocalAdminAccount account){
  var list=new List<LocalAdminAccount>(state.Administrators??[]);list.RemoveAll(a=>a.Id==account.Id);list.Add(account);
  var next=state with{Administrators=list};Save(next);state=next;
 }
 public (LocalAdminSummary Administrator,string RecoveryCode) Add(LocalAdminInput input){lock(gate){if(!Configured)throw new LibraryError("Set up the first administrator first.",409);Validate(input);UniqueName(input.Username);if((state.Administrators?.Count??0)>=99)throw new LibraryError("This server supports up to 100 local administrator accounts.");var (account,recovery)=NewCredentials(Guid.NewGuid().ToString(),input.Username.Trim(),input.Password);CommitAccount(account);return(new(account.Id,account.Username,false),recovery);}}
 public string Reset(LocalAdminAccountChange input,string? current){lock(gate){var a=(state.Administrators??[]).Find(a=>a.Id==input.Id)??throw new LibraryError("Choose an additional administrator. The primary account uses its recovery procedure.",404);if(a.Id==current)throw new LibraryError("Use Change my password for your own account.");Validate(new(a.Username,input.Password));var (updated,recovery)=NewCredentials(a.Id,a.Username,input.Password);CommitAccount(updated);Invalidate(a.Id);return recovery;}}
 public void Remove(string id,string? current){lock(gate){if(id=="primary")throw new LibraryError("The primary administrator cannot be removed.");if(id==current)throw new LibraryError("You cannot remove the account you are using.");var list=new List<LocalAdminAccount>(state.Administrators??[]);if(list.RemoveAll(a=>a.Id==id)==0)throw new LibraryError("Administrator not found.",404);var next=state with{Administrators=list};Save(next);state=next;Invalidate(id);}}
 public (string Token,string RecoveryCode) ChangePassword(string? token,LocalAdminPasswordChange input){lock(gate){var current=Current(token)??throw new LibraryError("Sign in locally to change your own password.",401);var a=Accounts().First(a=>a.Id==current.Id);if(input.CurrentPassword==null||input.CurrentPassword.Length>128||!Equal(PasswordHash(input.CurrentPassword,a.Salt),a.PasswordHash))throw new LibraryError("Current password is incorrect.",401);Validate(new(a.Username,input.Password));var (updated,recovery)=NewCredentials(a.Id,a.Username,input.Password);
  if(a.Id=="primary"){var next=state with{Salt=updated.Salt,PasswordHash=updated.PasswordHash,RecoveryHash=updated.RecoveryHash,SetupHash=""};Save(next);state=next;try{File.Delete(codeFile);}catch(IOException){}catch(UnauthorizedAccessException){}}else CommitAccount(updated);
  Invalidate(a.Id);return(NewSession(a.Id),recovery);
 }}
 void Invalidate(string id){foreach(var key in sessions.Where(s=>s.Value.AdministratorId==id).Select(s=>s.Key).ToArray())sessions.Remove(key);}
 public string Login(LocalAdminInput input){lock(gate){
  if(input.Password is null||input.Password.Length>128||input.Username is null||input.Username.Length>64)throw new LibraryError("Administrator username or password is incorrect.",401);
  var a=Accounts().FirstOrDefault(a=>a.Username.Equals(input.Username.Trim(),StringComparison.OrdinalIgnoreCase));var computed=PasswordHash(input.Password,a?.Salt??state.Salt);
  if(!Configured||a==null||!Equal(computed,a.PasswordHash))throw new LibraryError("Administrator username or password is incorrect.",401);return NewSession(a.Id);
 }}
 string NewSession(string id){var now=clock.GetUtcNow();foreach(var key in sessions.Where(x=>x.Value.Expires<=now||x.Value.LastSeen.AddMinutes(30)<=now).Select(x=>x.Key).ToArray())sessions.Remove(key);if(sessions.Count>=32)sessions.Remove(sessions.MinBy(x=>x.Value.LastSeen).Key);var token=Secret();var hash=Hash(token);sessions[hash]=new(hash,now.AddHours(8),now,id);return token;}
 public LocalAdminSummary? Current(string? token){lock(gate){if(token?.Length!=43)return null;var hash=Hash(token);if(!sessions.TryGetValue(hash,out var session))return null;var now=clock.GetUtcNow();var account=Accounts().FirstOrDefault(a=>a.Id==session.AdministratorId);if(account==null||session.Expires<=now||session.LastSeen.AddMinutes(30)<=now){sessions.Remove(hash);return null;}sessions[hash]=session with{LastSeen=now};return new(account.Id,account.Username,account.Id=="primary");}}
 public bool Authorized(string? token)=>Current(token)!=null;
 public void Logout(string? token){lock(gate){if(token?.Length==43)sessions.Remove(Hash(token));}}
 // Stopped-server or existing approved admin proof recovers the primary identity.
 public void PrepareRecovery(){lock(gate){var code=Secret();var replacement=state with{SetupHash=Hash(code)};Save(replacement);state=replacement;WriteCode(code);}}
 public void Dispose()=>processLock.Dispose();
}
