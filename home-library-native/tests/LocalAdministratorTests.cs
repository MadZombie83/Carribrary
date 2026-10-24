using HomeLibrary;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
public static class LocalAdministratorTests {
 sealed class Clock:TimeProvider {public DateTimeOffset Now=DateTimeOffset.UtcNow;public override DateTimeOffset GetUtcNow()=>Now;}
 public static void Run(string directory,IDataProtectionProvider protection){
  Directory.CreateDirectory(directory);var clock=new Clock();
  var legacyDirectory=Path.Combine(directory,"v1-migration");Directory.CreateDirectory(legacyDirectory);
  var legacySalt=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));var legacyRecovery=new string('j',43);
  var legacyCredential=new{username="Existing Admin",salt=legacySalt,passwordHash=Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2("Existing administrator password",Convert.FromBase64String(legacySalt),600000,HashAlgorithmName.SHA256,32)),recoveryHash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(legacyRecovery))),setupHash=""};
  File.WriteAllBytes(Path.Combine(legacyDirectory,"local-admin.protected"),protection.CreateProtector("Carribrary.LocalAdministrator.v1").Protect(JsonSerializer.SerializeToUtf8Bytes(legacyCredential,Library.Json)));
  using(var migrated=new LocalAdministrators(legacyDirectory,protection)){
   if(!migrated.Authorized(migrated.Login(new("Existing Admin","Existing administrator password"))))throw new Exception("Legacy credentials lost");
   var added=migrated.Add(new("New Admin","New administrator password"));
   var primaryRecovery=migrated.Recover(new("Existing Admin","Updated existing password",legacyRecovery));if(!migrated.Authorized(primaryRecovery.Token)||migrated.List().Length!=2)throw new Exception("Legacy recovery/accounts lost");
   var retained=migrated.Login(new("New Admin","New administrator password"));migrated.PrepareRecovery();var prepared=File.ReadAllText(migrated.SetupFile).Trim();var primaryChange=migrated.ChangePassword(primaryRecovery.Token,new("Updated existing password","Changed primary password"));
   if(!migrated.Authorized(primaryChange.Token)||!migrated.Authorized(retained)||migrated.Authorized(primaryRecovery.Token)||File.Exists(migrated.SetupFile))throw new Exception("Primary password-change isolation/proof invalidation failed");
   try{migrated.Recover(new("Existing Admin","Another primary password",prepared));throw new Exception("Old prepared proof survived password change");}catch(LibraryError){Console.WriteLine("PASS primary password change renews own session, retains others and invalidates stale prepared proof");}
  }
  try{protection.CreateProtector("Carribrary.LocalAdministrator.v1").Unprotect(File.ReadAllBytes(Path.Combine(legacyDirectory,"local-admin.protected")));throw new Exception("Older single-account programs can read new credentials");}catch(CryptographicException){Console.WriteLine("PASS v1 credential migration preserves sign-in/recovery and fails closed to old programs");}
  using(var migratedRestart=new LocalAdministrators(legacyDirectory,protection)){if(!migratedRestart.Authorized(migratedRestart.Login(new("New Admin","New administrator password"))))throw new Exception("Added account did not persist");}

  void Check(bool ok,string name){if(!ok)throw new Exception("FAILED: "+name);Console.WriteLine("PASS "+name);}
  LibraryError Failed(Action action){try{action();throw new Exception("Expected rejection");}catch(LibraryError e){return e;}}
  // Seed a pre-login installation, including approved companions and a borrower.
  using var legacyStore=new Store(directory,protection);
  legacyStore.Apply(new("addBook",JsonSerializer.SerializeToElement(new{title="Legacy migration book",isbn="9780140328721"},Library.Json),legacyStore.Read().Revision));
  using var borrowers=new BorrowerAccounts(directory);var borrower=borrowers.Register(new("legacy.borrower","Borrower legacy password 33","Legacy Borrower","","test-invitation"));
  var browsers=new BrowserAdmins(directory,"https://library.example",protection);var grant=JsonSerializer.SerializeToElement(browsers.Pair(),Library.Json);var browser=browsers.Enroll(grant.GetProperty("code").GetString()!,"Existing iPhone");
  using var identity=new Identity(directory,protection);using var phoneKey=RSA.Create(2048);var pairing=identity.StartPair("https://library.example:7443");var spki=Convert.ToBase64String(phoneKey.ExportSubjectPublicKeyInfo());var signature=Convert.ToBase64String(phoneKey.SignData(Encoding.UTF8.GetBytes("home-library-pair:v1:"+pairing.Code+":"+spki),HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1));var enrolledPhone=JsonSerializer.SerializeToElement(identity.Enroll(new(pairing.Code,"Existing Android",spki,signature)),Library.Json);
  using var certificate=System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadCertificate(Convert.FromBase64String(enrolledPhone.GetProperty("certificate").GetString()!));
  var serverId=identity.ServerId;
  const string password="Local administrator test password 33";string savedRecovery,oldSession;
  using(var admins=new LocalAdministrators(directory,protection,clock)){
   Check(browsers.Resolve(browser.Token)!=null&&identity.Approved(certificate)!=null&&identity.ServerId==serverId&&legacyStore.Read().State.Books[0].Title=="Legacy migration book","migration preserves existing inventory, Android identity and paired browser");
   Check(!admins.Configured&&!admins.Authorized(null),"unconfigured localhost does not authorize administrator access");
   var setup=File.ReadAllText(admins.SetupFile).Trim();
   Check(Failed(()=>admins.Enroll(new("Admin",password,new string('x',43)))).Status==401,"first visitor cannot enroll without private filesystem proof");
   Failed(()=>admins.Enroll(new("Admin","short",setup)));
   var enrolled=admins.Enroll(new("Admin",password,setup));savedRecovery=enrolled.RecoveryCode;oldSession=enrolled.Token;
   Check(admins.Configured&&admins.Authorized(enrolled.Token)&&!File.Exists(admins.SetupFile),"enrollment creates local session and consumes private setup code");
   Check(Failed(()=>admins.Enroll(new("Other",password,setup))).Status==409,"setup cannot overwrite an enrolled administrator");
   var wrong=Failed(()=>admins.Login(new("Admin","wrong password")));var unknown=Failed(()=>admins.Login(new("Unknown",password)));
   Check(wrong.Status==401&&wrong.Message==unknown.Message,"unknown user and incorrect password have generic errors");
   var second=admins.Add(new("Second Admin",password+" second"));var secondId=second.Administrator.Id;var secondToken=admins.Login(new("second admin",password+" second"));
   Check(admins.Current(oldSession)?.Id=="primary"&&admins.Current(secondToken)?.Id==secondId,"separate usernames create distinct simultaneously authenticated identities");
   Check(Failed(()=>admins.Add(new(" second ADMIN ",password))).Status==409,"administrator usernames are unique ignoring case and whitespace");
   var resetSecond=admins.Reset(new(secondId,password+" reset"),"primary");Check(!admins.Authorized(secondToken)&&admins.Authorized(oldSession),"reset ends only target administrator sessions immediately");
   Check(Failed(()=>admins.Login(new("Second Admin",password+" second"))).Status==401,"reset invalidates the old password");
   secondToken=admins.Login(new("Second Admin",password+" reset"));Failed(()=>admins.ChangePassword(secondToken,new("wrong",password+" own")));
   var own=admins.ChangePassword(secondToken,new(password+" reset",password+" own"));Check(!admins.Authorized(secondToken)&&admins.Authorized(own.Token)&&admins.Authorized(oldSession),"password change renews current account without signing out others");
   Failed(()=>admins.Recover(new("Second Admin",password+" recovered",resetSecond)));
   var recoveredSecond=admins.Recover(new("Second Admin",password+" recovered",own.RecoveryCode));Check(!admins.Authorized(own.Token)&&admins.Authorized(recoveredSecond.Token)&&admins.Authorized(oldSession),"individual recovery preserves other administrator sessions");
   Failed(()=>admins.Remove("primary",secondId));Failed(()=>admins.Remove(secondId,secondId));
   admins.Remove(secondId,"primary");Check(!admins.Authorized(recoveredSecond.Token)&&admins.Authorized(oldSession)&&admins.List().Length==1,"removal invalidates only the removed identity and protects primary/current accounts");
   Check(Failed(()=>admins.Login(new("Second Admin",password+" recovered"))).Status==401,"removed administrator cannot sign in again");
   var independent=admins.Login(new("admin",password));admins.Logout(independent);
   Check(!admins.Authorized(independent)&&admins.Authorized(oldSession),"sign out revokes only the selected local session");
   clock.Now=clock.Now.AddMinutes(31);Check(!admins.Authorized(oldSession),"idle session expires after thirty minutes");
   var absolute=admins.Login(new("Admin",password));for(var i=0;i<47;i++){clock.Now=clock.Now.AddMinutes(10);Check(admins.Authorized(absolute),"active session remains authorized within absolute limit");}
   clock.Now=clock.Now.AddMinutes(10);Check(!admins.Authorized(absolute),"active session still expires at eight hours");
   var before=admins.Login(new("Admin",password));var reset=admins.Recover(new("NewAdmin",password+"changed",savedRecovery));savedRecovery=reset.RecoveryCode;
   Check(!admins.Authorized(before)&&admins.Authorized(reset.Token),"password recovery invalidates every old local session");
   Check(Failed(()=>admins.Recover(new("Admin",password,enrolled.RecoveryCode))).Status==401,"recovery code is single use and rotates");
   admins.PrepareRecovery();var repairCode=File.ReadAllText(admins.SetupFile).Trim();var offline=admins.Recover(new("Admin",password,repairCode));savedRecovery=offline.RecoveryCode;oldSession=offline.Token;
   Check(!File.Exists(admins.SetupFile)&&Failed(()=>admins.Recover(new("Admin",password,repairCode))).Status==401,"owner-prepared recovery proof is consumed without deleting library data");
   var bytes=File.ReadAllBytes(Path.Combine(directory,"local-admin.protected"));var decoded=protection.CreateProtector("Carribrary.LocalAdministrator.v2").Unprotect(bytes);var state=JsonSerializer.Deserialize<LocalAdminCredentials>(decoded,Library.Json)!;
   Check(!Encoding.UTF8.GetString(decoded).Contains(password)&&!Encoding.UTF8.GetString(decoded).Contains(savedRecovery)&&state.PasswordHash.Length>20,"protected credential state stores salted password hash and recovery digest, not plaintext secrets");
   Check(!Encoding.UTF8.GetString(bytes).Contains(state.Username),"credential file is data-protected");
   try{using var duplicate=new LocalAdministrators(directory,protection);throw new Exception("Concurrent credential writer accepted");}catch(IOException){Console.WriteLine("PASS offline credential recovery cannot race a running server");}
  }
  Check(borrowers.Login(new("legacy.borrower","Borrower legacy password 33")).Length==43&&browsers.Resolve(browser.Token)!=null&&identity.Approved(certificate)!=null,"local enrollment/recovery does not change borrower credentials or approved companions");
  using(var restarted=new LocalAdministrators(directory,protection,clock)){
   Check(restarted.Configured&&!restarted.Authorized(oldSession)&&restarted.Authorized(restarted.Login(new("Admin",password))),"credentials persist but local sessions expire on restart");
   Check(restarted.Authorized(restarted.Recover(new("Admin",password,savedRecovery)).Token),"saved recovery remains valid across restart");
  }
 }
}
