using System.Text.Json;
using System.Text.RegularExpressions;
namespace HomeLibrary;
public sealed record CoverCandidate(string Url,string Source,string SourceUrl,string Title,string Author,bool ExactEdition);
public sealed record CoverResult(List<CoverCandidate> Covers,string Message);
public sealed record GoogleConnection(bool Ok,string Message);
// Read-only, authenticated, on-demand lookups. All outbound hosts are fixed;
// provider-supplied cover URLs are allowlisted before any image request.
public sealed class CoverLookup(HttpClient http,string googleApiKey="") {
 static string Text(JsonElement e,string key)=>e.ValueKind==JsonValueKind.Object&&e.TryGetProperty(key,out var v)&&v.ValueKind==JsonValueKind.String?v.GetString()??"":"";
 static IEnumerable<JsonElement> Array(JsonElement e,string key)=>e.ValueKind==JsonValueKind.Object&&e.TryGetProperty(key,out var a)&&a.ValueKind==JsonValueKind.Array?a.EnumerateArray():[];
 static string Names(JsonElement e,string key)=>string.Join(", ",Array(e,key).Select(a=>a.ValueKind==JsonValueKind.String?a.GetString():Text(a,"name")).Where(x=>!string.IsNullOrWhiteSpace(x)));
 static string Match(string s)=>Regex.Replace(s.ToUpperInvariant().Normalize(),@"[^\p{L}\p{N}]","");
 public static string Edition(string isbn){var s=Library.Code(isbn);if(s.Length!=10)return s;var prefix="978"+s[..9];var sum=prefix.Select((c,i)=>(c-'0')*(i%2==0?1:3)).Sum();return prefix+((10-sum%10)%10);}
 public static string SafeImage(string raw){
  if(!Uri.TryCreate(raw,UriKind.Absolute,out var u)||u.Scheme is not ("http" or "https")||!string.IsNullOrEmpty(u.UserInfo)||!u.IsDefaultPort)return "";
  if(u.Host is not ("covers.openlibrary.org" or "archive.org" or "books.google.com" or "books.googleusercontent.com")&&!Regex.IsMatch(u.Host,@"^ia[0-9]+\.(us|eu)\.archive\.org$"))return "";
  var builder=new UriBuilder(u){Scheme="https",Port=-1,Fragment=""};
  builder.Query=string.Join('&',u.Query.TrimStart('?').Split('&',StringSplitOptions.RemoveEmptyEntries).Where(x=>!Uri.UnescapeDataString(x.Split('=')[0]).Equals("key",StringComparison.OrdinalIgnoreCase)));
  return builder.Uri.AbsoluteUri;
 }
 static string WithoutKey(string raw){
  var u=new Uri(raw);var builder=new UriBuilder(u){Fragment=""};
  builder.Query=string.Join('&',u.Query.TrimStart('?').Split('&',StringSplitOptions.RemoveEmptyEntries).Where(x=>!Uri.UnescapeDataString(x.Split('=')[0]).Equals("key",StringComparison.OrdinalIgnoreCase)));
  return builder.Uri.AbsoluteUri;
 }
 string RequestUrl(string raw,bool image){
  var clean=WithoutKey(raw);var uri=new Uri(clean);
  if(!image&&googleApiKey!=""&&uri.Scheme=="https"&&uri.Host=="www.googleapis.com"&&uri.IsDefaultPort&&uri.UserInfo==""&&uri.AbsolutePath.StartsWith("/books/v1/",StringComparison.Ordinal))
   clean+=(uri.Query==""?"?":"&")+"key="+Uri.EscapeDataString(googleApiKey);
  return clean;
 }
 async Task<HttpResponseMessage> Fetch(string url,bool image,CancellationToken ct){
  for(var hop=0;hop<4;hop++){
   using var request=new HttpRequestMessage(HttpMethod.Get,RequestUrl(url,image));
   var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,ct);
   if((int)response.StatusCode is not (301 or 302 or 303 or 307 or 308)||response.Headers.Location==null)return response;
   var next=new Uri(new Uri(url),response.Headers.Location);var allowed=image?SafeImage(next.AbsoluteUri)!="":next.Scheme=="https"&&next.Host==new Uri(url).Host&&next.IsDefaultPort&&next.UserInfo=="";
   if(!allowed)return response;response.Dispose();url=image?SafeImage(next.AbsoluteUri):WithoutKey(next.AbsoluteUri);
  }
  throw new HttpRequestException("Provider redirected too many times.");
 }
 static async Task<JsonElement> ReadJson(HttpResponseMessage response,CancellationToken ct){
  using var stream=await response.Content.ReadAsStreamAsync(ct);using var buffer=new MemoryStream();var bytes=new byte[8192];int count;
  while((count=await stream.ReadAsync(bytes,ct))>0){if(buffer.Length+count>1024*1024)return default;buffer.Write(bytes,0,count);}
  using var doc=JsonDocument.Parse(buffer.ToArray());return doc.RootElement.Clone();
 }
 async Task<JsonElement> Json(string url,CancellationToken ct){
  try {using var response=await Fetch(url,false,ct);if(!response.IsSuccessStatusCode)return default;return await ReadJson(response,ct);}
  catch(Exception e)when(e is HttpRequestException or OperationCanceledException or JsonException){return default;}
 }
 // Provider errors are classified without echoing Google's raw response, keys,
 // account identifiers or URLs into the public catalog or client messages.
 static string GoogleError(int status,JsonElement data){
  var error=data.ValueKind==JsonValueKind.Object&&data.TryGetProperty("error",out var e)?e:default;
  var reasons=Array(error,"errors").Concat(Array(error,"details")).Select(x=>Text(x,"reason")).ToHashSet(StringComparer.OrdinalIgnoreCase);
  if(status==429||reasons.Overlaps(["rateLimitExceeded","dailyLimitExceeded","quotaExceeded","RESOURCE_EXHAUSTED"]))return "Google Books quota is exhausted. Check the project's Books API quota or try again after it resets.";
  if(reasons.Overlaps(["accessNotConfigured","SERVICE_DISABLED"]))return "Google Books API is not enabled for this key's project. Enable Books API in Google Cloud.";
  if(reasons.Contains("API_KEY_SERVICE_BLOCKED"))return "Google rejected this key's API restriction. Allow Books API in the key settings.";
  if(reasons.Overlaps(["API_KEY_IP_ADDRESS_BLOCKED","API_KEY_HTTP_REFERRER_BLOCKED","API_KEY_ANDROID_APP_BLOCKED","API_KEY_IOS_APP_BLOCKED","ipRefererBlocked"]))return "Google rejected this key's application restriction. Server lookups need IP addresses or None; an IP restriction must allow your server's public internet IP.";
  if(status is 400 or 401||reasons.Contains("API_KEY_INVALID"))return "Google rejected the API key. Check that the saved key is correct and belongs to a project with Books API enabled.";
  if(status==403)return "Google denied this request. Check Books API enablement, the key's API/application restrictions and the project's quota.";
  return "Google Books is temporarily unavailable. Try again later.";
 }
 static string Isbn10(string isbn){
  var edition=Edition(isbn);if(!edition.StartsWith("978")||edition.Length!=13)return "";
  var stem=edition.Substring(3,9);var sum=stem.Select((c,i)=>(c-'0')*(10-i)).Sum();var check=(11-sum%11)%11;
  return stem+(check==10?"X":check.ToString());
 }
 async Task<(JsonElement Data,string Error)> GoogleJson(string path,CancellationToken ct){
  try {
   for(var attempt=0;attempt<3;attempt++){
    using var response=await Fetch("https://www.googleapis.com/books/v1/"+path,false,ct);
    JsonElement data=default;try{data=await ReadJson(response,ct);}catch(JsonException){}
    if((int)response.StatusCode is 500 or 502 or 503 or 504&&attempt<2){await Task.Delay(1000*(1<<attempt),ct);continue;}
    if(!response.IsSuccessStatusCode)return (default,GoogleError((int)response.StatusCode,data));
    return data.ValueKind==JsonValueKind.Object?(data,""):(default,"Google Books returned an unreadable response. Try again later.");
   }
  }catch(Exception e)when(e is HttpRequestException or OperationCanceledException or JsonException){}
  return (default,"Could not reach Google Books. Check the server's internet connection and try again.");
 }
 Task<(JsonElement Data,string Error)> GoogleSearch(string query,CancellationToken ct)=>GoogleJson("volumes?q="+Uri.EscapeDataString(query)+"&maxResults=5",ct);
 public async Task<GoogleConnection> TestGoogle(CancellationToken requestToken){
  if(googleApiKey=="")return new(false,"No Google Books key is saved. Save a key first; no restart is required.");
  using var timeout=CancellationTokenSource.CreateLinkedTokenSource(requestToken);timeout.CancelAfter(TimeSpan.FromSeconds(6));
  var result=await GoogleSearch("isbn:9780140328721",timeout.Token);
  return result.Error!=""?new(false,result.Error):new(true,"Google Books connection succeeded with the saved key. No server restart is required. Individual books may still lack a cover.");
 }
 async Task<bool> ImageExists(string url,CancellationToken ct){
  if(SafeImage(url)=="")return false;
  try {using var response=await Fetch(url,true,ct);
   return response.IsSuccessStatusCode&&response.Content.Headers.ContentType?.MediaType?.StartsWith("image/",StringComparison.OrdinalIgnoreCase)==true;
  }catch(Exception e)when(e is HttpRequestException or OperationCanceledException){return false;}
 }
 public async Task<CoverResult> Find(string isbn,string? title,string? author,CancellationToken requestToken){
  isbn=Library.Code(isbn);title=(title??"").Trim();author=(author??"").Trim();
  if(!Library.ValidCode(isbn)||title.Length>500||author.Length>500||title.Any(char.IsControl)||author.Any(char.IsControl))throw new LibraryError("Enter a valid ISBN before looking for covers.");
  using var timeout=CancellationTokenSource.CreateLinkedTokenSource(requestToken);timeout.CancelAfter(TimeSpan.FromSeconds(18));var ct=timeout.Token;
  var covers=new List<CoverCandidate>();var tried=new HashSet<string>();var archiveIds=new HashSet<string>();
  async Task Add(string raw,string source,string sourceUrl,string bookTitle,string bookAuthor,bool exact){
   var url=SafeImage(raw);if(url==""||!tried.Add(url)||covers.Count>=8||ct.IsCancellationRequested)return;
   if(await ImageExists(url,ct))covers.Add(new(url,source,sourceUrl,bookTitle,bookAuthor,exact));
  }
  var isbnPage="https://openlibrary.org/isbn/"+Uri.EscapeDataString(isbn);
  await Add($"https://covers.openlibrary.org/b/isbn/{isbn}-M.jpg?default=false","Open Library",isbnPage,title,author,true);
  if(covers.Count>0)return new(covers,"Review the ISBN-linked cover, then choose it.");
  var edition=await Json(isbnPage+".json",ct);
  if(edition.ValueKind==JsonValueKind.Object){
   var editionTitle=Text(edition,"title");var olid=Text(edition,"key").Split('/').Last();
   foreach(var cover in Array(edition,"covers").Take(2))if(cover.TryGetInt64(out var id)&&id>0)await Add($"https://covers.openlibrary.org/b/id/{id}-M.jpg?default=false","Open Library",isbnPage,editionTitle,author,true);
   if(Regex.IsMatch(olid,@"^OL\d+M$"))await Add($"https://covers.openlibrary.org/b/olid/{olid}-M.jpg?default=false","Open Library",isbnPage,editionTitle,author,true);
   var ocaid=Text(edition,"ocaid");if(Regex.IsMatch(ocaid,@"^[A-Za-z0-9_.-]{1,100}$"))archiveIds.Add(ocaid);
  }
  if(covers.Count>0)return new(covers,"Review the edition-linked cover, then choose it.");
  // Broader matches stay suggestions, never silently become this edition's cover.
  if(covers.Count==0&&!string.IsNullOrWhiteSpace(title)&&!string.IsNullOrWhiteSpace(author)){
   var search=await Json("https://openlibrary.org/search.json?title="+Uri.EscapeDataString(title)+"&author="+Uri.EscapeDataString(author)+"&limit=3&fields=key,title,author_name,isbn,cover_i,ia",ct);
   foreach(var book in Array(search,"docs")){
    var bookTitle=Text(book,"title");var bookAuthor=Names(book,"author_name");
    if(Match(bookTitle)!=Match(title)||!Array(book,"author_name").Any(a=>a.ValueKind==JsonValueKind.String&&Match(a.GetString()??"")==Match(author)))continue;
    // A work-level image is not guaranteed to represent an ISBN in its edition list.
    if(book.TryGetProperty("cover_i",out var cover)&&cover.TryGetInt64(out var id)&&id>0)await Add($"https://covers.openlibrary.org/b/id/{id}-M.jpg?default=false","Open Library","https://openlibrary.org"+Text(book,"key"),bookTitle,bookAuthor,false);
    foreach(var ia in Array(book,"ia").Take(2))if(ia.ValueKind==JsonValueKind.String&&Regex.IsMatch(ia.GetString()??"",@"^[A-Za-z0-9_.-]{1,100}$"))archiveIds.Add(ia.GetString()!);
   }
  }
  foreach(var id in archiveIds.Take(3)){
   var item=await Json("https://archive.org/metadata/"+Uri.EscapeDataString(id),ct);
   if(item.ValueKind!=JsonValueKind.Object||!item.TryGetProperty("metadata",out var metadata))continue;
   var bookTitle=Text(metadata,"title");var creator=Text(metadata,"creator");if(creator=="")creator=Names(metadata,"creator");
   var codes=Array(metadata,"isbn").Where(x=>x.ValueKind==JsonValueKind.String).Select(x=>x.GetString()!).Append(Text(metadata,"isbn"));
   var exact=codes.Any(x=>Library.ValidCode(x)&&Edition(x)==Edition(isbn));
   if(!exact&&(Match(bookTitle)!=Match(title)||Match(creator)!=Match(author)))continue;
   // Only offer an actual listed item thumbnail; don't substitute a generic icon.
   if(Array(item,"files").Any(f=>Text(f,"name")=="__ia_thumb.jpg"))await Add($"https://archive.org/download/{Uri.EscapeDataString(id)}/__ia_thumb.jpg","Internet Archive","https://archive.org/details/"+Uri.EscapeDataString(id),bookTitle,creator,exact);
  }
  var googleError="";var googleMatched=false;
  if(covers.Count==0&&googleApiKey!=""&&!requestToken.IsCancellationRequested){
   using var googleTimeout=CancellationTokenSource.CreateLinkedTokenSource(requestToken);googleTimeout.CancelAfter(TimeSpan.FromSeconds(10));var googleCt=googleTimeout.Token;
   var seenVolumes=new HashSet<string>();var detailsCount=0;var suggestions=new List<CoverCandidate>();
   bool Exact(JsonElement info)=>Array(info,"industryIdentifiers").Any(x=>Library.ValidCode(Text(x,"identifier"))&&Edition(Text(x,"identifier"))==Edition(isbn));
   async Task<bool> TryVolume(JsonElement volume,bool allowDetails){
    if(volume.ValueKind!=JsonValueKind.Object||!volume.TryGetProperty("volumeInfo",out var info))return false;
    var id=Text(volume,"id");if(!Regex.IsMatch(id,@"^[A-Za-z0-9_-]{1,100}$"))return false;
    var exact=Exact(info);if(exact)googleMatched=true;
    var sameBook=title!=""&&author!=""&&Match(Text(info,"title"))==Match(title)&&Array(info,"authors").Any(x=>x.ValueKind==JsonValueKind.String&&Match(x.GetString()??"")==Match(author));
    var identified=Array(info,"industryIdentifiers").Any(x=>Library.ValidCode(Text(x,"identifier")));
    if((exact||sameBook&&identified)&&info.ValueKind==JsonValueKind.Object&&info.TryGetProperty("imageLinks",out var images)){
     foreach(var raw in new[]{Text(images,"thumbnail"),Text(images,"smallThumbnail")}.Distinct()){
      var url=SafeImage(raw);if(url==""||new Uri(url).Host!="books.google.com"||GoogleCover.Link(url)!="https://books.google.com/books?id="+id)continue;
      var validated=suggestions.FirstOrDefault(c=>c.Url==url);
      if(exact&&validated!=null){covers.Add(validated with {ExactEdition=true});return true;}
      if(!tried.Add(url))continue;
      if(await ImageExists(url,googleCt)){
       var candidate=new CoverCandidate(url,"Google Books","https://books.google.com/books?id="+id,Text(info,"title"),Names(info,"authors"),exact);
       if(exact){covers.Add(candidate);return true;}
       if(suggestions.Count<3&&!suggestions.Any(c=>c.SourceUrl==candidate.SourceUrl))suggestions.Add(candidate);break;
      }
     }
    }
    // Search fields can be incomplete. Details may verify the exact edition.
    // A different ISBN stays a review-only suggestion; never guess image IDs.
    if(allowDetails&&(exact||sameBook)&&detailsCount<3&&seenVolumes.Add(id)&&!googleCt.IsCancellationRequested){
     detailsCount++;var detail=await GoogleJson("volumes/"+Uri.EscapeDataString(id),googleCt);
     if(detail.Error!="")googleError=detail.Error;
     else if(Text(detail.Data,"id")==id)return await TryVolume(detail.Data,false);
    }
    return false;
   }
   var queries=new List<string>{"isbn:"+Edition(isbn)};var ten=Isbn10(isbn);if(ten!="")queries.Add("isbn:"+ten);
   if(title!=""&&author!=""){
    queries.Add(title+" "+author);
    queries.Add("intitle:\""+title.Replace("\"", " ")+"\" inauthor:\""+author.Replace("\"", " ")+"\"");
   }
   foreach(var query in queries){
    if(googleCt.IsCancellationRequested)break;
    var result=await GoogleSearch(query,googleCt);
    if(result.Error!=""){
     googleError=result.Error;
     // Alternate queries cannot repair key restrictions or exhausted quota.
     if(!result.Error.Contains("temporarily")&&!result.Error.Contains("Could not reach"))break;
     continue;
    }
    foreach(var volume in Array(result.Data,"items").Take(5))if(await TryVolume(volume,true))break;
    if(covers.Count>0)break;
   }
   if(covers.Count==0)covers.AddRange(suggestions);
  }
  if(covers.Count>0&&covers.All(c=>!c.ExactEdition))return new(covers,"Found covers for the same title and author with a different ISBN. These may show another edition or an ebook cover. Review the image, then choose it only if it suits your book.");
  if(covers.Count==0&&googleError!="")return new(covers,googleError);
  if(covers.Count==0&&googleApiKey!="")return new(covers,googleMatched?"Google Books lists this ISBN, but no usable cover image was returned. You can enter a cover URL manually.":"No cover found. Google Books returned no exact ISBN-matched image for this edition.");
  return new(covers,covers.Count==0?googleApiKey==""?"No cover found. You can enable optional Google Books lookup in Library settings, or enter a cover URL manually.":"No cover found from the configured sources. Keep the placeholder or enter a cover URL manually.":"Review a cover below, then choose it. Suggestions may show another edition or a scanned title page.");
 }
}
