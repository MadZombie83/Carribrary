using Microsoft.Data.Sqlite;
using System.Text.Json;
namespace HomeLibrary;
// Ciphertext only. Carribrary never receives the private passphrase, data key or recovery code.
public sealed record VaultEnvelope(int Version,string AccountId,string Salt,string KeyIv,string WrappedKey,string RecoveryIv,string RecoveryWrappedKey,string DataIv,string Data);
public sealed record VaultWrite(long Revision,VaultEnvelope Envelope,bool MigrateLegacy=false,long LibraryRevision=-1,string LegacyToken="");
public sealed class BorrowerVault:IDisposable {
 readonly object gate=new();readonly SqliteConnection db;
 public BorrowerVault(string directory){db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=Path.Combine(directory,"borrower-vaults.sqlite")}.ToString());db.Open();using var c=db.CreateCommand();c.CommandText="PRAGMA journal_mode=WAL; PRAGMA secure_delete=ON; PRAGMA synchronous=FULL; CREATE TABLE IF NOT EXISTS vaults(account TEXT PRIMARY KEY,revision INTEGER NOT NULL,envelope TEXT NOT NULL);";c.ExecuteNonQuery();}
 public object Read(string account){lock(gate){using var c=db.CreateCommand();c.CommandText="SELECT revision,envelope FROM vaults WHERE account=$id";c.Parameters.AddWithValue("$id",account);using var r=c.ExecuteReader();return r.Read()?new{revision=r.GetInt64(0),envelope=JsonSerializer.Deserialize<VaultEnvelope>(r.GetString(1),Library.Json)}:new{revision=0L,envelope=(VaultEnvelope?)null};}}
 public bool Exists(string id){lock(gate){using var c=db.CreateCommand();c.CommandText="SELECT 1 FROM vaults WHERE account=$id";c.Parameters.AddWithValue("$id",id);return c.ExecuteScalar()!=null;}}
 static bool Bytes(string? text,int length,int? max=null){try{if(text==null||text.Length>(max??length)*2)return false;var b=Convert.FromBase64String(text);return max==null?b.Length==length:b.Length>=length&&b.Length<=max;}catch(FormatException){return false;}}
 public object Save(string account,VaultWrite input){lock(gate){var e=input.Envelope;if(e==null||e.Version!=1||e.AccountId!=account||!Bytes(e.Salt,16)||!Bytes(e.KeyIv,12)||!Bytes(e.WrappedKey,48)||!Bytes(e.RecoveryIv,12)||!Bytes(e.RecoveryWrappedKey,48)||!Bytes(e.DataIv,12)||!Bytes(e.Data,16,4*1024*1024)||input.Revision<0)throw new LibraryError("Invalid encrypted personal vault.");
  using var t=db.BeginTransaction();using var c=db.CreateCommand();c.Transaction=t;c.CommandText=input.Revision==0?"INSERT OR IGNORE INTO vaults VALUES($id,1,$data)":"UPDATE vaults SET revision=revision+1,envelope=$data WHERE account=$id AND revision=$revision";c.Parameters.AddWithValue("$id",account);c.Parameters.AddWithValue("$data",JsonSerializer.Serialize(e,Library.Json));if(input.Revision!=0)c.Parameters.AddWithValue("$revision",input.Revision);if(c.ExecuteNonQuery()!=1)throw new LibraryError("Your private list changed on another device. Reload and unlock it before saving again.",409);t.Commit();return Read(account);
 }}
 public void Delete(string account){lock(gate){using var c=db.CreateCommand();c.CommandText="DELETE FROM vaults WHERE account=$id";c.Parameters.AddWithValue("$id",account);c.ExecuteNonQuery();}}
 public object AfterDeletion(object result,string account,BorrowerAccounts accounts){var response=JsonSerializer.SerializeToElement(result,Library.Json).Deserialize<Dictionary<string,JsonElement>>(Library.Json)!;try{Delete(account);accounts.CompleteVaultDelete(account);}catch(SqliteException){var warnings=response["warnings"].Deserialize<string[]>(Library.Json)??[];response["warnings"]=JsonSerializer.SerializeToElement(warnings.Append("Sign-in was deleted, but encrypted-vault cleanup could not finish. Ask the server owner to restart the server and check storage permissions."),Library.Json);}return response;}
 public void Dispose()=>db.Dispose();
}
public static class VaultEndpoints {
 public static void Map(WebApplication app,Settings settings,Store store){
  bool Public(HttpContext c)=>c.Connection.LocalPort==settings.CatalogPort&&settings.PublicCatalogOrigin!="";
  app.MapGet("/account/vault",(HttpContext c,BorrowerAccounts accounts,BorrowerVault vault)=>{if(!Public(c))return Results.NotFound();var a=AccountEndpoints.Account(c,accounts);BorrowerAccounts.ApprovedReader(a,store);return Results.Json(vault.Read(a.Id),Library.Json);}).RequireRateLimiting("personal-reading");
  app.MapGet("/account/vault/legacy",(HttpContext c,BorrowerAccounts accounts,BorrowerVault vault)=>{if(!Public(c))return Results.NotFound();var a=AccountEndpoints.Account(c,accounts);var reader=BorrowerAccounts.ApprovedReader(a,store);return Results.Json(store.LegacyView(reader),Library.Json);}).RequireRateLimiting("personal-reading");
  app.MapPost("/account/vault",(VaultWrite input,HttpContext c,BorrowerAccounts accounts,BorrowerVault vault)=>{if(!Public(c))return Results.NotFound();var a=AccountEndpoints.Account(c,accounts);var reader=BorrowerAccounts.ApprovedReader(a,store);return Results.Json(accounts.WithActiveAccount(a.Id,store,approved=>store.SaveVault(vault,approved.Id,approved.ReaderId,input)),Library.Json);}).RequireRateLimiting("personal-reading");
 }
}
