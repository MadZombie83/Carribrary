using System.Text.Json;
namespace HomeLibrary;
public sealed class Acquisition {public string Description{get;set;}="";public string Series{get;set;}="";public string Id{get;set;}="";public string Isbn{get;set;}="";public string Title{get;set;}="";public string Author{get;set;}="";public string Publisher{get;set;}="";public string CoverUrl{get;set;}="";public string Notes{get;set;}="";public string AddedDate{get;set;}="";}
// Household acquisition plans are admin-only; they are not owned copies or borrower TBR.
public static class Acquisitions {
 static string Field(JsonElement e,string k)=>e.TryGetProperty(k,out var v)&&v.ValueKind==JsonValueKind.String?v.GetString()??"":"";
 static bool Text(string? s,int length)=>s!=null&&s.Length<=length&&!s.Any(c=>char.IsControl(c)&&c is not ('\n' or '\r' or '\t'));
 public static void Validate(LibraryState s){if(s.BooksToBuy==null||s.BooksToBuy.Any(x=>x==null)||s.BooksToBuy.Select(x=>x.Id).Distinct().Count()!=s.BooksToBuy.Count)throw new LibraryError("Invalid acquisition wishlist.");foreach(var x in s.BooksToBuy){if(!Guid.TryParseExact(x.Id,"N",out _)||!Library.ValidCode(x.Isbn)||string.IsNullOrWhiteSpace(x.Title)||!Text(x.Title,500)||!Text(x.Author,500)||!Text(x.Publisher,500)||!Text(x.Series,500)||!Text(x.Description,4000)||!Text(x.Notes,2000)||!Text(x.CoverUrl,2000)||x.CoverUrl!=""&&(!Uri.TryCreate(x.CoverUrl,UriKind.Absolute,out var u)||u.Scheme is not ("http" or "https"))||!DateOnly.TryParseExact(x.AddedDate,"yyyy-MM-dd",out _))throw new LibraryError("Wishlist entries need a valid ISBN, title and plain-text details.");}}
 public static object Apply(LibraryState s,string action,JsonElement e){var id=Field(e,"id");var old=s.BooksToBuy.Find(x=>x.Id==id);
  if(action=="removeAcquisition"){if(old==null)throw new LibraryError("Wishlist entry not found.");s.BooksToBuy.Remove(old);return new{removed=true};}
  if(action=="acquireBook"){
   if(old==null)throw new LibraryError("Wishlist entry not found.");
   if(!e.TryGetProperty("purchaseConfirmed",out var confirm)||confirm.ValueKind!=JsonValueKind.True)throw new LibraryError("Confirm that you now own this physical book.");
   var fields=new Dictionary<string,object?>{{"isbn",old.Isbn},{"title",old.Title},{"author",old.Author},{"publisher",old.Publisher},{"coverUrl",old.CoverUrl},{"series",old.Series},{"description",old.Description},{"collectionId",Field(e,"collectionId")==""?"general":Field(e,"collectionId")}};
   foreach(var key in new[]{"confirmedDuplicates","room","bookcase","shelf"})if(e.TryGetProperty(key,out var v))fields[key]=v.Clone();
   var copy=Library.Apply(s,"addBook",JsonSerializer.SerializeToElement(fields,Library.Json));s.BooksToBuy.Remove(old);return copy;
  }
  if(id!=""&&old==null)throw new LibraryError("Wishlist entry not found.");
  var isbn=Library.Code(Field(e,"isbn"));if(s.BooksToBuy.Any(x=>x.Id!=id&&x.Isbn==isbn))throw new LibraryError("This ISBN is already on Books to buy. Edit its existing entry.");
  var item=new Acquisition{Series=e.TryGetProperty("series",out _)?Field(e,"series").Trim():old?.Series??"",Description=e.TryGetProperty("description",out _)?Field(e,"description").Trim():old?.Description??"",Id=old?.Id??Guid.NewGuid().ToString("N"),Isbn=isbn,Title=Field(e,"title").Trim(),Author=Field(e,"author").Trim(),Publisher=Field(e,"publisher").Trim(),CoverUrl=Field(e,"coverUrl").Trim(),Notes=Field(e,"notes").Trim(),AddedDate=old?.AddedDate??Library.Today};
  if(old==null)s.BooksToBuy.Add(item);else s.BooksToBuy[s.BooksToBuy.IndexOf(old)]=item;Validate(s);return item;
 }
}
