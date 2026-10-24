using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net;
namespace HomeLibrary;
public sealed record BookSummary(string Description,string Source,string SourceUrl);
public sealed class BookAbout(HttpClient http,CoverSettings settings){
 readonly SemaphoreSlim gate=new(1,1);readonly Dictionary<string,(DateTimeOffset At,BookSummary Summary)> cache=new();
 public async Task<BookSummary> Find(Book book,CancellationToken stop){
  if(book.Description!="")return new(book.Description,"Library description","");
  await gate.WaitAsync(stop);try{if(cache.TryGetValue(book.Isbn,out var saved)&&saved.At>DateTimeOffset.UtcNow)return saved.Summary;
   BookSummary result=new("No description is available yet. The librarian can add one in Edit details.","","");
   try{using var edition=await Get("https://openlibrary.org/isbn/"+Uri.EscapeDataString(book.Isbn)+".json",stop);if(edition!=null){var root=edition.RootElement;var description=Description(root);var source="https://openlibrary.org/isbn/"+book.Isbn;if(description==""&&root.TryGetProperty("works",out var works)&&works.ValueKind==JsonValueKind.Array&&works.GetArrayLength()>0&&works[0].TryGetProperty("key",out var key)&&Regex.IsMatch(key.GetString()??"",@"^/works/OL[0-9]+W$")){source="https://openlibrary.org"+key.GetString();using var work=await Get(source+".json",stop);if(work!=null)description=Description(work.RootElement);}if(description!="")result=new(description,"Open Library",source);}
    if(result.Source==""&&settings.Key!=""){using var google=await Get("https://www.googleapis.com/books/v1/volumes?q="+Uri.EscapeDataString("isbn:"+book.Isbn)+"&maxResults=3&key="+Uri.EscapeDataString(settings.Key),stop);if(google!=null&&google.RootElement.TryGetProperty("items",out var items))foreach(var item in items.EnumerateArray()){if(!item.TryGetProperty("volumeInfo",out var info)||!info.TryGetProperty("industryIdentifiers",out var ids)||!ids.EnumerateArray().Any(x=>x.TryGetProperty("identifier",out var id)&&Edition(id.GetString()??"")==Edition(book.Isbn)))continue;var desc=Description(info);if(desc!=""){var link=info.TryGetProperty("infoLink",out var l)?l.GetString()??"":"";if(!Uri.TryCreate(link,UriKind.Absolute,out var u)||u.Scheme is not ("https" or "http"))link="https://books.google.com";result=new(desc,"Google Books",link.Replace("http://","https://"));break;}}}
   }catch(Exception e)when(e is HttpRequestException or TaskCanceledException or JsonException){if(stop.IsCancellationRequested)throw;result=new("Description lookup is unavailable. Try again later, or ask the librarian to add a description.","","");}
   if(cache.Count>=300)cache.Clear();cache[book.Isbn]=(DateTimeOffset.UtcNow.AddMinutes(result.Source==""?2:360),result);return result;
  }finally{gate.Release();}
 }
 async Task<JsonDocument?> Get(string url,CancellationToken stop,int redirects=0){using var response=await http.GetAsync(url,stop);if((int)response.StatusCode is 301 or 302 or 307 or 308){var location=response.Headers.Location;if(redirects<3&&location!=null&&Uri.TryCreate(new Uri(url),location,out var next)&&next.Scheme=="https"&&next.Port==443&&next.UserInfo==""&&next.Host=="openlibrary.org"&&Regex.IsMatch(next.AbsolutePath,@"^/(books|works)/OL[0-9]+[MW]\.json$"))return await Get(next.AbsoluteUri,stop,redirects+1);return null;}if(!response.IsSuccessStatusCode)return null;var text=await response.Content.ReadAsStringAsync(stop);if(text.Length>2000000)return null;return JsonDocument.Parse(text);}
 static string Edition(string value){var code=Library.Code(value);if(code.Length!=10)return code;var prefix="978"+code[..9];var sum=prefix.Select((c,i)=>(c-'0')*(i%2==0?1:3)).Sum();return prefix+((10-sum%10)%10);}
 static string Description(JsonElement e){if(!e.TryGetProperty("description",out var d))return "";var text=d.ValueKind==JsonValueKind.String?d.GetString():d.ValueKind==JsonValueKind.Object&&d.TryGetProperty("value",out var v)&&v.ValueKind==JsonValueKind.String?v.GetString():"";text=Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(text??"","<[^>]*>"," ")),@"[ \t]+"," ").Trim();return text[..Math.Min(2000,text.Length)];}
}
