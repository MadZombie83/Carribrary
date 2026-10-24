using HomeLibrary;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
public static class CoverLookupTests {
 sealed class Fake(Func<string,HttpResponseMessage> route):HttpMessageHandler {
  public List<string> Calls=[];public List<(string Host,string Key)> Headers=[];
  protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req,CancellationToken ct){Calls.Add(req.RequestUri!.AbsoluteUri);Headers.Add((req.RequestUri.Host,req.Headers.TryGetValues("X-Goog-Api-Key",out var values)?values.Single():""));return Task.FromResult(route(req.RequestUri.AbsoluteUri));}
 }
 static HttpResponseMessage Json(object value)=>new(HttpStatusCode.OK){Content=new StringContent(JsonSerializer.Serialize(value),System.Text.Encoding.UTF8,"application/json")};
 static HttpResponseMessage Image(){var response=new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent([1,2,3])};response.Content.Headers.ContentType=new MediaTypeHeaderValue("image/jpeg");return response;}
 static HttpResponseMessage Missing()=>new(HttpStatusCode.NotFound);
 static CoverLookup Lookup(Fake fake)=>new(new HttpClient(fake));
 static void Check(bool value,string reason){if(!value)throw new Exception("Cover test failed: "+reason);}
 public static async Task Run(){
  Check(CoverLookup.Edition("0140328726")=="9780140328721","ISBN-10 edition equivalence");
  foreach(var bad in new[]{"file:///etc/passwd","https://127.0.0.1/","https://archive.org:444/","https://user@archive.org/","https://archive.org.evil.example/","https://books.google.com.evil.example/"})Check(CoverLookup.SafeImage(bad)=="","unsafe provider image host");
  Check(!CoverLookup.SafeImage("http://books.google.com/books/content?id=x&key=SECRET").Contains("SECRET"),"key removed and HTTPS used");
  var direct=new Fake(url=>url.Contains("/b/isbn/")?Image():throw new Exception("Fallback called unnecessarily"));
  var result=await Lookup(direct).Find("9780140328721","Matilda","Roald Dahl",default);Check(result.Covers.Count==1&&result.Covers[0].ExactEdition&&direct.Calls.Count==1,"direct match short circuits");
  var archive=new Fake(url=>{
   if(url=="https://openlibrary.org/isbn/9780140328721.json"){var r=new HttpResponseMessage(HttpStatusCode.Found);r.Headers.Location=new Uri("/books/OL123M.json",UriKind.Relative);return r;}
   if(url.EndsWith("/books/OL123M.json"))return Json(new{title="Matilda",key="/books/OL123M",ocaid="matilda-test"});
   if(url.EndsWith("/metadata/matilda-test"))return Json(new{metadata=new{title="Matilda",creator="Roald Dahl",isbn=new[]{"0140328726"}},files=new[]{new{name="__ia_thumb.jpg"}}});
   if(url.EndsWith("/__ia_thumb.jpg"))return Image();return Missing();
  });
  result=await Lookup(archive).Find("9780140328721","Matilda","Roald Dahl",default);Check(result.Covers.Count==1&&result.Covers[0].Source=="Internet Archive"&&result.Covers[0].ExactEdition,"linked Archive fallback and relative redirects");
  var broader=new Fake(url=>{
   if(url.Contains("/search.json?"))return Json(new{docs=new[]{new{title="Matilda",author_name=new[]{"Roald Dahl"},key="/works/OL1W",cover_i=42,isbn=new[]{"9780140328721"}}}});
   if(url.Contains("/b/id/42-"))return Image();return Missing();
  });
  result=await Lookup(broader).Find("9780140328721","Matilda","Roald Dahl",default);Check(result.Covers.Count==1&&!result.Covers[0].ExactEdition,"work image requires edition confirmation even when ISBN listed");
  var blocked=new Fake(url=>{var r=new HttpResponseMessage(HttpStatusCode.Found);r.Headers.Location=new Uri("https://127.0.0.1/private");return r;});
  result=await Lookup(blocked).Find("9780140328721","","",default);Check(result.Covers.Count==0&&blocked.Calls.All(x=>!x.Contains("127.0.0.1")),"redirects cannot fetch private addresses");
  Check(!archive.Calls.Any(x=>x.Contains("googleapis")),"Google not called without configured key");
  const string testKey="test-google-key-not-a-real-credential";
  var google=new Fake(url=>{
   if(url.Contains("www.googleapis.com"))return Json(new{items=new[]{new{id="Volume_1",volumeInfo=new{title="Matilda",authors=new[]{"Roald Dahl"},industryIdentifiers=new[]{new{type="ISBN_10",identifier="0140328726"}},imageLinks=new{thumbnail="http://books.google.com/books/content?id=Volume_1&key=DO_NOT_PERSIST"}}}}});
   if(url.Contains("books.google.com/books/content"))return Image();return Missing();
  });
  result=await new CoverLookup(new HttpClient(google),testKey).Find("9780140328721","Matilda","Roald Dahl",default);
  Check(result.Covers.Count==1&&result.Covers[0].Source=="Google Books"&&result.Covers[0].ExactEdition,"Google exact ISBN fallback including ISBN-10 equivalence");
  Check(result.Covers[0].SourceUrl=="https://books.google.com/books?id=Volume_1"&&!result.Covers[0].Url.Contains("key="),"Google provenance and image URL exclude credentials");
  Check(google.Headers.All(x=>x.Key==""),"no API key/authorization header is used");
  Check(google.Calls.Any(x=>new Uri(x).Host=="www.googleapis.com"&&x.Contains("&key="+testKey)),"documented key query parameter sent to Google Books API");
  Check(google.Calls.Where(x=>x.Contains(testKey)).All(x=>new Uri(x).Host=="www.googleapis.com"&&new Uri(x).AbsolutePath.StartsWith("/books/v1/")),"key never goes to image or other provider hosts");
  var redirected=new Fake(url=>{
   if(url.Contains("googleapis")&&!url.Contains("/other-api")){var response=new HttpResponseMessage(HttpStatusCode.Found);response.Headers.Location=new Uri("https://www.googleapis.com/other-api?key=DO_NOT_FORWARD");return response;}
   return Missing();
  });
  await new CoverLookup(new HttpClient(redirected),testKey).TestGoogle(default);
  Check(redirected.Calls.Where(x=>x.Contains("/other-api")).All(x=>!x.Contains("key=")),"redirects outside Books API receive no credentials");
  var primary=new Fake(url=>url.Contains("covers.openlibrary.org")?Image():Missing());
  await new CoverLookup(new HttpClient(primary),testKey).Find("9780140328721","Matilda","Roald Dahl",default);
  Check(!primary.Calls.Any(x=>x.Contains("googleapis")),"Google is not called when an earlier source supplies a cover");
  // Force earlier providers absent and return an unrelated Google edition.
  var mismatch=new Fake(url=>url.Contains("googleapis")?Json(new{items=new[]{new{id="wrong",volumeInfo=new{industryIdentifiers=new[]{new{identifier="9780547928227"}},imageLinks=new{thumbnail="https://books.google.com/books/content?id=wrong"}}}}}):Missing());
  result=await new CoverLookup(new HttpClient(mismatch),testKey).Find("9780140328721","Matilda","Roald Dahl",default);Check(result.Covers.Count==0,"unrelated Google ISBN is not offered");
  Check(GoogleCover.Link("https://books.google.com/books/content?id=Volume_1")=="https://books.google.com/books?id=Volume_1"&&GoogleCover.Link("https://books.google.com.evil.example/?id=Volume_1")=="","safe persisted Google attribution link");
  var missingFirst=new Fake(url=>{
   if(url.Contains("googleapis"))return Json(new{items=new object[]{
    new{id="wrong",volumeInfo=new{industryIdentifiers=new[]{new{identifier="9780547928227"}},imageLinks=new{thumbnail="https://books.google.com/books/content?id=wrong"}}},
    new{id="noimage",volumeInfo=new{industryIdentifiers=new[]{new{identifier="9780593647448"}}}},
    new{id="KIuZEAAAQBAJ",volumeInfo=new{title="Stranger Danger",authors=new[]{"Maren Stoffels"},industryIdentifiers=new[]{new{identifier="0593647440"}},imageLinks=new{thumbnail="https://books.google.com/books/content?id=KIuZEAAAQBAJ"}}}
   }});
   if(url.Contains("id=KIuZEAAAQBAJ"))return Image();return Missing();
  });
  result=await new CoverLookup(new HttpClient(missingFirst),testKey).Find("9780593647448","Stranger Danger","Maren Stoffels",default);
  Check(result.Covers.Count==1&&result.Covers[0].SourceUrl.EndsWith("KIuZEAAAQBAJ"),"skip unrelated/missing first results and find later ISBN-matched cover");
  Check(missingFirst.Calls.Any(x=>x.Contains("maxResults=5"))&&!missingFirst.Calls.Any(x=>x.Contains("id=wrong")),"bounded search preserves order without fetching mismatched images");
  var transientCalls=0;
  var alternate=new Fake(url=>{
   var decoded=Uri.UnescapeDataString(url);
   if(decoded.Contains("q=isbn:9780593647448")){transientCalls++;return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable){Content=new StringContent("{}")};}
   if(decoded.Contains("q=isbn:0593647440"))return Json(new{items=new[]{new{id="Alternate",volumeInfo=new{industryIdentifiers=new[]{new{identifier="9780593647448"}},imageLinks=new{thumbnail="https://books.google.com/books/content?id=Alternate"}}}}});
   if(url.Contains("id=Alternate"))return Image();return Missing();
  });
  result=await new CoverLookup(new HttpClient(alternate),testKey).Find("9780593647448","Stranger Danger","Maren Stoffels",default);
  Check(result.Covers.Count==1&&transientCalls==3,"retry transient failure once then find equivalent ISBN-10");
  var titleFallback=new Fake(url=>{
   var decoded=Uri.UnescapeDataString(url);
   if(decoded.Contains("q=isbn:"))return Json(new{totalItems=0});
   if(decoded.Contains("q=intitle:"))return Json(new{items=new object[]{
    new{id="WrongEdition",volumeInfo=new{title="Stranger Danger",authors=new[]{"Different Author"},industryIdentifiers=new[]{new{identifier="9780140328721"}},imageLinks=new{thumbnail="https://books.google.com/books/content?id=WrongEdition"}}},
    new{id="KIuZEAAAQBAJ",volumeInfo=new{title="Stranger Danger",authors=new[]{"Maren Stoffels"}}}
   }});
   if(url.Contains("/volumes/KIuZEAAAQBAJ?"))return Json(new{id="KIuZEAAAQBAJ",volumeInfo=new{title="Stranger Danger",authors=new[]{"Maren Stoffels"},industryIdentifiers=new[]{new{identifier="0593647440"}},imageLinks=new{thumbnail="http://books.google.com/books/content?id=KIuZEAAAQBAJ"}}});
   if(url.Contains("/volumes/WrongEdition?"))return Json(new{id="WrongEdition",volumeInfo=new{industryIdentifiers=new[]{new{identifier="9780140328721"}}}});
   if(url.Contains("id=KIuZEAAAQBAJ"))return Image();return Missing();
  });
  result=await new CoverLookup(new HttpClient(titleFallback),testKey).Find("9780593647448","Stranger Danger","Maren Stoffels",default);
  Check(result.Covers.Count==1&&result.Covers[0].ExactEdition&&result.Covers[0].SourceUrl.EndsWith("KIuZEAAAQBAJ"),"title search then details verifies ISBN before offering cover");
  Check(!titleFallback.Calls.Any(x=>x.Contains("id=WrongEdition")),"matching title from different edition cannot supply image");
  var noIsbn=new Fake(url=>url.Contains("googleapis")?Json(new{items=new[]{new{id="Unverified",volumeInfo=new{title="Stranger Danger",authors=new[]{"Maren Stoffels"},imageLinks=new{thumbnail="https://books.google.com/books/content?id=Unverified"}}}}}):Missing());
  result=await new CoverLookup(new HttpClient(noIsbn),testKey).Find("9780593647448","Stranger Danger","Maren Stoffels",default);
  Check(result.Covers.Count==0&&!noIsbn.Calls.Any(x=>x.Contains("id=Unverified")),"unverified title never supplies cover");
  var plain=new Fake(url=>{
   var decoded=Uri.UnescapeDataString(url);
   if(decoded.Contains("q=isbn:")||decoded.Contains("q=intitle:"))return Json(new{totalItems=0});
   if(decoded.Contains("q=Stranger Danger Maren Stoffels"))return Json(new{items=new[]{new{id="EbookEdition",volumeInfo=new{title="Stranger Danger",authors=new[]{"Maren Stoffels"},industryIdentifiers=new[]{new{identifier="9780593647455"}},imageLinks=new{thumbnail="https://books.google.com/books/content?id=EbookEdition"}}}}});
   if(url.Contains("/volumes/EbookEdition?"))return Json(new{id="EbookEdition",volumeInfo=new{title="Stranger Danger",authors=new[]{"Maren Stoffels"},industryIdentifiers=new[]{new{identifier="9780593647455"}},imageLinks=new{thumbnail="https://books.google.com/books/publisher/content?id=EbookEdition"}}});
   if(url.Contains("id=EbookEdition"))return Image();return Missing();
  });
  result=await new CoverLookup(new HttpClient(plain),testKey).Find("9780593647448","Stranger Danger","Maren Stoffels",default);
  Check(result.Covers.Count==1&&!result.Covers[0].ExactEdition&&result.Message.Contains("different ISBN"),"plain title/author returns one marked alternate edition despite duplicate volume images");
  var preferExact=new Fake(url=>{
   if(Uri.UnescapeDataString(url).Contains("q=intitle:"))return Json(new{items=new[]{new{id="ExactEdition",volumeInfo=new{industryIdentifiers=new[]{new{identifier="9780593647448"}},imageLinks=new{thumbnail="https://books.google.com/books/content?id=ExactEdition"}}}}});
   if(url.Contains("id=ExactEdition"))return Image();
   // Reuse the plain fixture's provider response to verify an exact later
   // query wins over an earlier title-only edition suggestion.
   var method=new HttpClient(plain);return method.GetAsync(url).GetAwaiter().GetResult();
  });
  result=await new CoverLookup(new HttpClient(preferExact),testKey).Find("9780593647448","Stranger Danger","Maren Stoffels",default);
  Check(result.Covers.Count==1&&result.Covers[0].ExactEdition&&result.Covers[0].SourceUrl.EndsWith("ExactEdition"),"later exact edition takes priority over plain-search suggestions");
  foreach(var failure in new[]{(429,"rateLimitExceeded","quota"),(403,"SERVICE_DISABLED","not enabled"),(403,"API_KEY_HTTP_REFERRER_BLOCKED","application restriction"),(403,"API_KEY_SERVICE_BLOCKED","API restriction"),(400,"API_KEY_INVALID","API key")}){
   var denied=new Fake(url=>url.Contains("googleapis")?new HttpResponseMessage((HttpStatusCode)failure.Item1){Content=new StringContent(JsonSerializer.Serialize(new{error=new{message="PRIVATE PROVIDER BODY",details=new[]{new{reason=failure.Item2}}}}))}:Missing());
   var lookup=new CoverLookup(new HttpClient(denied),testKey);var connection=await lookup.TestGoogle(default);
   Check(!connection.Ok&&connection.Message.Contains(failure.Item3)&&!connection.Message.Contains("PRIVATE"),"safe key-test diagnostic for "+failure.Item2);
   var failed=await lookup.Find("9780593647448","Stranger Danger","Maren Stoffels",default);Check(failed.Covers.Count==0&&failed.Message.Contains(failure.Item3),"cover lookup surfaces provider error "+failure.Item2);
  }
  var working=new CoverLookup(new HttpClient(new Fake(_=>Json(new{totalItems=0}))),testKey);
  Check((await working.TestGoogle(default)).Ok,"valid connection succeeds even if sample search has no volumes");
  Check(!(await Lookup(new Fake(_=>throw new Exception("Unexpected network call"))).TestGoogle(default)).Ok,"test without key makes no request");
  try{await Lookup(archive).Find("123","","",default);throw new Exception("Invalid ISBN accepted");}catch(LibraryError){}
  Console.WriteLine("PASS cover source fallback, edition matching, confirmation flags, safe redirects, provider isolation and missing images");
 }
}
