using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.DataProtection;
namespace HomeLibrary;
public sealed record PurchaseSuggestionInput(string RequestId,string Kind,string Title,string Author="",string Isbn="",string Notes="");
public sealed record PurchaseSuggestion(string Id,string Kind,string Title,string Author,string Isbn,string Notes,string SubmittedAt,string Status);
public sealed record PurchaseSuggestionDecision(string Id,string Status,string ExpectedStatus);
// Operational suggestions intentionally have no account/reader association or contact fields.
// Their content is private to administrators, not part of a borrower's personal vault.
public sealed class PurchaseSuggestions:IDisposable {
 readonly object gate=new();readonly SqliteConnection db;readonly IDataProtector protector;
 public PurchaseSuggestions(string directory,IDataProtectionProvider protection){protector=protection.CreateProtector("Carribrary.PurchaseSuggestions.v1");db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=Path.Combine(directory,"purchase-suggestions.sqlite")}.ToString());db.Open();using var c=db.CreateCommand();c.CommandText="PRAGMA journal_mode=WAL;PRAGMA secure_delete=ON;PRAGMA synchronous=FULL;CREATE TABLE IF NOT EXISTS suggestions(id TEXT PRIMARY KEY,status TEXT NOT NULL,data BLOB NOT NULL);CREATE TABLE IF NOT EXISTS receipts(id TEXT PRIMARY KEY,hash TEXT NOT NULL,created TEXT NOT NULL);";c.ExecuteNonQuery();}
 SqliteCommand Command(string sql,params(string,object)[] args){var c=db.CreateCommand();c.CommandText=sql;foreach(var(k,v)in args)c.Parameters.AddWithValue(k,v);return c;}
 static string Text(string? text,int max,bool required=false){var value=(text??"").Trim();if(value.Length>max||required&&value.Length==0||value.Any(c=>char.IsControl(c)&&c is not ('\n' or '\r' or '\t')))throw new LibraryError("Enter a title and plain-text details within the displayed limits.");return value;}
 public object Submit(PurchaseSuggestionInput input){
  if(!Guid.TryParseExact(input.RequestId,"D",out var id)||input.Kind is not ("book" or "series"))throw new LibraryError("Choose Book or Series and submit a valid request.");
  var title=Text(input.Title,300,true);var author=Text(input.Author,300);var notes=Text(input.Notes,1500);var isbn=Library.Code(input.Isbn??"");if(isbn!=""&&!Library.ValidCode(isbn))throw new LibraryError("Enter a valid ISBN, or leave it blank. Series suggestions do not need an ISBN.");
  var item=new PurchaseSuggestion(id.ToString("D"),input.Kind,title,author,isbn,notes,DateTimeOffset.UtcNow.ToString("O"),"pending");var hash=Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new{input.Kind,title,author,isbn,notes},Library.Json)));
  lock(gate){using var tx=db.BeginTransaction();using(var prior=Command("SELECT hash FROM receipts WHERE id=$id",("$id",item.Id))){var saved=prior.ExecuteScalar()?.ToString();if(saved!=null){if(saved!=hash)throw new LibraryError("This submission changed. Reopen the form and try again.",409);tx.Commit();return new{accepted=true};}}
   using(var count=Command("SELECT COUNT(*) FROM suggestions"))if(Convert.ToInt32(count.ExecuteScalar())>=500)throw new LibraryError("The suggestion box is full. Please ask the librarian to review it.",503);
   using(var insert=Command("INSERT INTO suggestions VALUES($id,'pending',$data)",("$id",item.Id),("$data",protector.Protect(JsonSerializer.SerializeToUtf8Bytes(item,Library.Json)))))insert.ExecuteNonQuery();
   using(var receipt=Command("INSERT INTO receipts VALUES($id,$hash,$created)",("$id",item.Id),("$hash",hash),("$created",item.SubmittedAt)))receipt.ExecuteNonQuery();
   using(var trim=Command("DELETE FROM receipts WHERE id NOT IN (SELECT id FROM receipts ORDER BY created DESC LIMIT 2000) AND id NOT IN (SELECT id FROM suggestions)"))trim.ExecuteNonQuery();tx.Commit();return new{accepted=true};
  }
 }
 public List<PurchaseSuggestion> List(){lock(gate){var list=new List<PurchaseSuggestion>();using var c=Command("SELECT status,data FROM suggestions ORDER BY rowid DESC");using var r=c.ExecuteReader();while(r.Read()){var item=JsonSerializer.Deserialize<PurchaseSuggestion>(protector.Unprotect((byte[])r[1]),Library.Json)??throw new LibraryError("A purchase suggestion could not be read.");list.Add(item with{Status=r.GetString(0)});}return list;}}
 public void Decide(PurchaseSuggestionDecision input){if(input.Status is not ("considering" or "declined" or "pending" or "delete"))throw new LibraryError("Choose a valid suggestion decision.");lock(gate){using var c=Command(input.Status=="delete"?"DELETE FROM suggestions WHERE id=$id AND status=$old":"UPDATE suggestions SET status=$new WHERE id=$id AND status=$old",("$id",input.Id),("$old",input.ExpectedStatus),("$new",input.Status));if(c.ExecuteNonQuery()!=1)throw new LibraryError("This suggestion changed or was removed. Refresh and review it again.",409);}}
 public void Dispose(){lock(gate)db.Dispose();}
}
