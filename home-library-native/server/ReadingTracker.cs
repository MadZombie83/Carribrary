using System.Text.Json;
using System.Text.RegularExpressions;
namespace HomeLibrary;
public sealed class ReaderProfile {public string Id{get;set;}="";public string Name{get;set;}="";public string BorrowerId{get;set;}="";}
public sealed class ReadingEntry {
 public string Id{get;set;}="";public string ReaderId{get;set;}="";public string CopyId{get;set;}="";
 public string Status{get;set;}="want";public string StartedDate{get;set;}="";public string FinishedDate{get;set;}="";
 public int Rating{get;set;} public string Notes{get;set;}="";
}
// Private household profiles, not separate sign-ins. Never included in Sharing.Books.
public static class ReadingTracker {
 static string Field(JsonElement e,string key)=>e.TryGetProperty(key,out var x)&&x.ValueKind==JsonValueKind.String?x.GetString()??"":"";
 static string NameKey(string name)=>Regex.Replace(name.Trim().Normalize().ToUpperInvariant(),@"\s+"," ");
 static bool GoodName(string? name)=>!string.IsNullOrWhiteSpace(name)&&name.Length<=80&&!name.Any(char.IsControl);
 static bool Date(string value)=>value==""||DateOnly.TryParseExact(value,"yyyy-MM-dd",out _)&&string.CompareOrdinal(value,Library.Today)<=0;
 static void Entry(ReadingEntry x){
  if(x.Status is not ("want" or "reading" or "finished"))throw new LibraryError("Choose Want to read, Currently reading or Finished.");
  if(x.StartedDate==null||x.FinishedDate==null||!Date(x.StartedDate)||!Date(x.FinishedDate)||x.StartedDate!=""&&x.FinishedDate!=""&&string.CompareOrdinal(x.FinishedDate,x.StartedDate)<0)throw new LibraryError("Reading dates must be valid, today or earlier, with finish on or after start.");
  if(x.Status=="want"&&(x.StartedDate!=""||x.FinishedDate!="")||x.Status=="reading"&&x.FinishedDate!="")throw new LibraryError("Only finished books can have a finish date; Want to read has no reading dates.");
  if(x.Rating<0||x.Rating>5)throw new LibraryError("Choose a whole-star rating from 1 to 5, or Unrated.");
  if(x.Notes==null||x.Notes.Length>2000||x.Notes.Any(c=>char.IsControl(c)&&c is not ('\n' or '\r' or '\t')))throw new LibraryError("Reading notes must be plain text of at most 2000 characters.");
 }
 public static void Validate(LibraryState s){
  if(s.Readers==null||s.ReadingEntries==null||s.Readers.Any(x=>x==null||!Guid.TryParseExact(x.Id,"N",out _)||!GoodName(x.Name))||s.Readers.Select(x=>x.Id).Distinct().Count()!=s.Readers.Count||s.Readers.Where(x=>x.BorrowerId=="").Select(x=>NameKey(x.Name)).Distinct().Count()!=s.Readers.Count(x=>x.BorrowerId==""))throw new LibraryError("Invalid reading profiles in backup.");
  if(s.Readers.Any(r=>r.BorrowerId==null||r.BorrowerId!=""&&!s.Borrowers.Any(b=>b.Id==r.BorrowerId)))throw new LibraryError("Invalid borrower links in reading profiles.");
  var pairs=new HashSet<(string,string)>();var ids=new HashSet<string>();
  foreach(var x in s.ReadingEntries){
   if(x==null||!Guid.TryParseExact(x.Id,"N",out _)||!ids.Add(x.Id)||!s.Readers.Any(r=>r.Id==x.ReaderId)||!s.Books.Any(b=>b.Id==x.CopyId)||!pairs.Add((x.ReaderId,x.CopyId)))throw new LibraryError("Invalid reading entry references in backup.");
   Entry(x);
  }
 }
 public static object Apply(LibraryState s,string action,JsonElement e){
  var readerId=action=="addReader"?"":Field(e,"readerId");
  if(action is "addReader" or "renameReader"){
   var name=Field(e,"name").Trim();if(!GoodName(name))throw new LibraryError("Enter a reader name of 1–80 characters.");
   if(Field(e,"borrowerId")==""&&s.Readers.Any(r=>r.Id!=readerId&&r.BorrowerId==""&&NameKey(r.Name)==NameKey(name)))throw new LibraryError("A reader with that name already exists.");
   var linked=Field(e,"borrowerId");
   if(action=="renameReader"&&!e.TryGetProperty("borrowerId",out _))linked=s.Readers.Find(r=>r.Id==readerId)?.BorrowerId??"";
   if(linked!=""&&!s.Borrowers.Any(b=>b.Id==linked))throw new LibraryError("Choose an existing borrower.");

   if(linked!=""&&s.Readers.Any(r=>r.Id!=readerId&&r.BorrowerId==linked)&&(!e.TryGetProperty("allowPrivateReplacement",out var replacement)||replacement.ValueKind!=JsonValueKind.True))throw new LibraryError("This borrower already has a reading profile.");
   if(action=="addReader"){var reader=new ReaderProfile{Id=Guid.NewGuid().ToString("N"),Name=name,BorrowerId=linked};s.Readers.Add(reader);return reader;}
   var old=s.Readers.Find(r=>r.Id==readerId)??throw new LibraryError("Reader not found.");old.Name=name;old.BorrowerId=linked;return old;
  }
  var profile=s.Readers.Find(r=>r.Id==readerId)??throw new LibraryError("Choose an existing reader.");
  if(action=="removeReader"){
   if(s.ReadingEntries.Any(x=>x.ReaderId==readerId)||s.Requests.Any(x=>x.ReaderId==readerId))throw new LibraryError("This reader has reading entries or request history. Keep the profile to preserve history; you can rename it or revoke its personal link.");
   s.Readers.Remove(profile);return new{id=readerId};
  }
  var copy=Library.Copy(Field(e,"copyId"));
  var existing=s.ReadingEntries.Find(x=>x.ReaderId==readerId&&x.CopyId==copy);
  if(action=="removeReading"){
   if(existing==null)throw new LibraryError("Reading entry not found.");s.ReadingEntries.Remove(existing);return new{id=existing.Id};
  }
  if(!s.Books.Any(b=>b.Id==copy&&(!b.Archived||existing!=null)))throw new LibraryError("Choose an existing catalog book.");
  var rating=0;if(e.TryGetProperty("rating",out var star)&&(star.ValueKind!=JsonValueKind.Number||!star.TryGetInt32(out rating)))throw new LibraryError("Choose a whole-star rating from 1 to 5, or Unrated.");
  var entry=new ReadingEntry{Id=existing?.Id??Guid.NewGuid().ToString("N"),ReaderId=readerId,CopyId=copy,Status=Field(e,"status"),StartedDate=Field(e,"startedDate"),FinishedDate=Field(e,"finishedDate"),Rating=rating,Notes=Field(e,"notes").Trim()};Entry(entry);
  if(existing==null)s.ReadingEntries.Add(entry);else s.ReadingEntries[s.ReadingEntries.IndexOf(existing)]=entry;
  return entry;
 }
}
