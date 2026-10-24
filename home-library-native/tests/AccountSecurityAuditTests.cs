using HomeLibrary;
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;
public static class AccountSecurityAuditTests {
 public static void Run(string directory){
  Directory.CreateDirectory(directory);using var accounts=new BorrowerAccounts(directory);
  void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS "+message);}
  LibraryError Failed(Action work){try{work();throw new Exception("Unsafe account operation accepted");}catch(LibraryError e){return e;}}
  const string password="Account audit test password 232";var a=accounts.Register(new("audit.reader",password,"Audit Reader","","test-invitation"));
  LibraryError unknown=Failed(()=>accounts.Login(new("not.registered",password))),wrong=Failed(()=>accounts.Login(new(a.Username,"Wrong password")));
  Check(unknown.Status==401&&unknown.Message==wrong.Message,"unknown username and wrong password use the same authentication error");
  var session=accounts.Login(new(a.Username,password));Check(session.Length==43&&accounts.Resolve(session)?.Id==a.Id,"cryptographic session token resolves only its own account");
  using var db=new SqliteConnection($"Data Source={Path.Combine(directory,"borrower-accounts.sqlite")}");db.Open();
  using(var cmd=db.CreateCommand()){cmd.CommandText="SELECT hash FROM sessions";var stored=(string)cmd.ExecuteScalar()!;Check(stored==Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(session)))&&!stored.Contains(session),"session database stores digest rather than bearer secret");}
  using(var cmd=db.CreateCommand()){cmd.CommandText="UPDATE sessions SET expires=1";cmd.ExecuteNonQuery();}
  Check(accounts.Resolve(session)==null,"expired browser session cannot authenticate");
  var reset=accounts.ResetLink(a.Id,"https://library.example").Split("#reset=")[1];
  using(var cmd=db.CreateCommand()){cmd.CommandText="UPDATE accounts SET reset_expires=1";cmd.ExecuteNonQuery();}
  var expired=Failed(()=>accounts.Reset(new(reset,"A replacement account password!")));var forged=Failed(()=>accounts.Reset(new(new string('x',43),"A replacement account password!")));
  Check(expired.Status==401&&expired.Message==forged.Message,"expired and unknown reset tokens have the same controlled failure");
  var active=accounts.Login(new(a.Username,password));var latest=accounts.ResetLink(a.Id,"https://library.example").Split("#reset=")[1];accounts.Reset(new(latest,"Replacement audit password 232"));
  Check(accounts.Resolve(active)==null&&Failed(()=>accounts.Reset(new(latest,password))).Status==401,"reset is single-use and invalidates prior sessions");
  var blocked=accounts.Login(new(a.Username,"Replacement audit password 232"));accounts.Block(a.Id);Check(accounts.Resolve(blocked)==null&&Failed(()=>accounts.Login(new(a.Username,"Replacement audit password 232"))).Message==unknown.Message,"blocked account revokes sessions without revealing block state through distinct sign-in errors");
 }
}
