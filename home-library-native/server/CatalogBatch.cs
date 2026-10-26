using System.Text.Json;
namespace HomeLibrary;
public sealed record CatalogBatchRow(Dictionary<string,JsonElement> Fields,bool KeepDuplicate=false);
public sealed record CatalogBatchInput(List<CatalogBatchRow> Rows,long Revision);
public sealed record CatalogBatchPreview(int Row,bool Valid,string Error,string[] Warnings,Book? Book);
public static class CatalogBatch {
 public const int Maximum=1000;
 static string Match(string value)=>System.Text.RegularExpressions.Regex.Replace(value.Trim().Normalize().ToUpperInvariant(),@"\s+"," ");
 static LibraryState Clone(LibraryState s)=>JsonSerializer.Deserialize<LibraryState>(JsonSerializer.Serialize(s,Library.Json),Library.Json)!;
 public static List<CatalogBatchPreview> Preview(LibraryState source,List<CatalogBatchRow>? rows){
  if(rows==null||rows.Count is <1 or >Maximum)throw new LibraryError($"Review 1–{Maximum} rows per batch. Split larger files into batches.");
  var state=Clone(source);state.Version=4;var results=new List<CatalogBatchPreview>();
  for(var i=0;i<rows.Count;i++){
   var row=rows[i];var warnings=new List<string>();try{
    if(row==null||row.Fields==null)throw new LibraryError("Invalid row.");
    var fields=new Dictionary<string,JsonElement>(row.Fields);string Field(string key)=>fields.TryGetValue(key,out var v)&&v.ValueKind==JsonValueKind.String?v.GetString()??"":"";
    var isbn=Library.Code(Field("isbn"));var title=Field("title");var author=Field("author");
    var duplicates=state.Books.Where(b=>!b.Archived&&((isbn!=""&&CoverLookup.Edition(b.Isbn)==CoverLookup.Edition(isbn))||(title.Trim()!=""&&author.Trim()!=""&&Match(b.Title)==Match(title)&&Match(b.Author)==Match(author)))).ToArray();
    if(duplicates.Length>0){warnings.Add("Possible duplicate: "+string.Join(", ",duplicates.Select(b=>b.Id)));if(!row.KeepDuplicate)throw new LibraryError("Choose Keep as additional copy, or remove this row.");fields["confirmedDuplicates"]=JsonSerializer.SerializeToElement(duplicates.Select(b=>b.Id));}
    if(isbn=="")warnings.Add("No ISBN: title/author and optional copy label identify this book.");
    if(Field("sourceCopyId")!="")warnings.Add("Source catalog number is retained in import notes; a new Carribrary number is assigned.");
    var note=Field("importNotes");if(Field("sourceCopyId")!="")note="Source catalog number: "+Field("sourceCopyId")+"\n"+note;
    fields["importNotes"]=JsonSerializer.SerializeToElement(note);
    if(note.Length>8000)throw new LibraryError("Import notes exceed 8000 characters; split or reduce this row before importing.");
    foreach(var key in new[]{"room","bookcase","shelf"})if(Field(key).Length>120||Field(key).Any(char.IsControl))throw new LibraryError("Shelf locations must be plain text of at most 120 characters.");
    var candidate=(Book)Library.Apply(state,"addBook",JsonSerializer.SerializeToElement(fields,Library.Json));
    results.Add(new(i+1,true,"",warnings.ToArray(),candidate));
   }catch(LibraryError e){results.Add(new(i+1,false,e.Message,warnings.ToArray(),null));}
  }
  Library.Validate(state);return results;
 }
 public static object Apply(LibraryState s,JsonElement input){
  var batch=input.Deserialize<CatalogBatchInput>(Library.Json)??throw new LibraryError("Invalid batch.");var preview=Preview(s,batch.Rows);
  if(preview.Any(x=>!x.Valid))throw new LibraryError("Batch not saved. Correct or remove invalid rows and review again. "+string.Join(" ",preview.Where(x=>!x.Valid).Take(5).Select(x=>$"Row {x.Row}: {x.Error}")));
  // Preview assigned every permanent identity and catalog number against this exact revision.
  foreach(var row in preview){s.Books.Add(row.Book!);if(long.TryParse(row.Book!.Id[3..],out var number))s.NextCopy=Math.Max(s.NextCopy,number+1);}
  return new{added=preview.Count,copyIds=preview.Select(x=>x.Book!.Id).ToArray()};
 }
 public static void Map(WebApplication app,Store store,Func<HttpContext,bool> admin,Func<HttpContext,bool> mobile){
  foreach(var prefix in new[]{"/api","/mobile"})app.MapPost(prefix+"/catalog-batch/csv",(CsvText input,HttpContext c)=>(prefix=="/api"?admin(c):mobile(c))?Results.Json(CatalogCsv.Parse(input.Text),Library.Json):Results.NotFound());
  foreach(var prefix in new[]{"/api","/mobile"})app.MapGet(prefix+"/catalog-batch/lookup",async(string isbn,HttpContext c,IHttpClientFactory factory)=>{
   if(!(prefix=="/api"?admin(c):mobile(c)))return Results.NotFound();isbn=Library.Code(isbn);if(!Library.ValidCode(isbn))throw new LibraryError("Enter a valid ISBN/EAN.");
   using var timeout=CancellationTokenSource.CreateLinkedTokenSource(c.RequestAborted);timeout.CancelAfter(TimeSpan.FromSeconds(10));try{using var response=await factory.CreateClient().GetAsync("https://openlibrary.org/api/books?bibkeys=ISBN:"+isbn+"&format=json&jscmd=data",timeout.Token);if(!response.IsSuccessStatusCode)throw new LibraryError("Metadata lookup unavailable. Enter details manually or retry.",503);using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));if(!json.RootElement.TryGetProperty("ISBN:"+isbn,out var book))return Results.Json(new{title=""});string Text(JsonElement e,string key)=>e.TryGetProperty(key,out var v)&&v.ValueKind==JsonValueKind.String?v.GetString()??"":"";string Names(string key)=>book.TryGetProperty(key,out var list)&&list.ValueKind==JsonValueKind.Array?string.Join(", ",list.EnumerateArray().Select(x=>Text(x,"name"))):"";return Results.Json(new{title=Text(book,"title"),author=Names("authors"),publisher=Names("publishers"),coverUrl=book.TryGetProperty("cover",out var cover)?CoverLookup.SafeImage(Text(cover,"medium")):""});}catch(HttpRequestException){throw new LibraryError("Metadata lookup unavailable. Enter details manually or retry.",503);}catch(OperationCanceledException){throw new LibraryError("Metadata lookup timed out. Enter details manually or retry.",503);}
  });
  foreach(var prefix in new[]{"/api","/mobile"})app.MapPost(prefix+"/catalog-batch/preview",(CatalogBatchInput input,HttpContext c)=>{
   if(!(prefix=="/api"?admin(c):mobile(c)))return Results.NotFound();var snapshot=store.Read();if(snapshot.Revision!=input.Revision)throw new LibraryError("Library changed. Refresh before reviewing this batch.",409);return Results.Json(new{revision=snapshot.Revision,rows=Preview(snapshot.State,input.Rows)},Library.Json);
  });
 }
}
