using System.Text.Json;
using System.Text.RegularExpressions;
namespace HomeLibrary;
public sealed class ReaderProfile {public string Id{get;set;}="";public string Name{get;set;}="";public string BorrowerId{get;set;}="";public Dictionary<int,int> AnnualGoals{get;set;}=[];}
public sealed class ReadingEntry {
 public string Id{get;set;}="";public string ReaderId{get;set;}="";public string CopyId{get;set;}="";
 public string Status{get;set;}="want";public string StartedDate{get;set;}="";public string FinishedDate{get;set;}="";
 public int Rating{get;set;} public string Notes{get;set;}="";
 public int CurrentPage{get;set;} public int TotalPages{get;set;}
 public List<ReadingSession> Sessions{get;set;}=[];public List<ReadingAnnotation> Annotations{get;set;}=[];
}
public sealed class ReadingSession {
 public string Id{get;set;}="";public string StartedDate{get;set;}="";public string FinishedDate{get;set;}="";
 public int CurrentPage{get;set;} public int TotalPages{get;set;}public int Rating{get;set;}public string Notes{get;set;}="";
}
public sealed class ReadingAnnotation {public string Id{get;set;}="";public string Kind{get;set;}="note";public string Text{get;set;}="";public int? Page{get;set;}}
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
  if(x.CurrentPage<0||x.TotalPages<0||x.TotalPages>100000||x.CurrentPage>100000||x.TotalPages>0&&x.CurrentPage>x.TotalPages)throw new LibraryError("Pages must be whole numbers from 0 to 100000, with current page no greater than total.");
  if(x.Sessions==null||x.Annotations==null||x.Sessions.Count>1000||x.Annotations.Count>1000)throw new LibraryError("Keep up to 1000 reading sessions and 1000 annotations per book.");
  if(x.Sessions.Any(y=>y==null||!Guid.TryParse(y.Id,out _)||y.StartedDate==null||y.FinishedDate==null)||x.Sessions.Select(y=>y.Id).Distinct().Count()!=x.Sessions.Count)throw new LibraryError("Invalid reading sessions.");
  foreach(var y in x.Sessions)Entry(new(){Status="finished",StartedDate=y.StartedDate,FinishedDate=y.FinishedDate,CurrentPage=y.CurrentPage,TotalPages=y.TotalPages,Rating=y.Rating,Notes=y.Notes});
  if(x.Annotations.Any(y=>y==null||!Guid.TryParse(y.Id,out _)||y.Kind is not ("quote" or "note")||string.IsNullOrWhiteSpace(y.Text)||y.Text.Length>4000||y.Text.Any(c=>char.IsControl(c)&&c is not ('\n' or '\r' or '\t'))||y.Page is <1 or >100000)||x.Annotations.Select(y=>y.Id).Distinct().Count()!=x.Annotations.Count)throw new LibraryError("Quotes and notes need text (up to 4000 characters) and an optional page from 1 to 100000.");
  if(x.Rating<0||x.Rating>5)throw new LibraryError("Choose a whole-star rating from 1 to 5, or Unrated.");
  if(x.Notes==null||x.Notes.Length>2000||x.Notes.Any(c=>char.IsControl(c)&&c is not ('\n' or '\r' or '\t')))throw new LibraryError("Reading notes must be plain text of at most 2000 characters.");
 }
 static int Number(JsonElement e,string key,int fallback){if(!e.TryGetProperty(key,out var n))return fallback;if(n.ValueKind!=JsonValueKind.Number||!n.TryGetInt32(out var result))throw new LibraryError("Pages must be whole numbers.");return result;}
 public static void Validate(LibraryState s){
  if(s.Readers==null||s.ReadingEntries==null||s.Readers.Any(x=>x==null||!Guid.TryParseExact(x.Id,"N",out _)||!GoodName(x.Name))||s.Readers.Select(x=>x.Id).Distinct().Count()!=s.Readers.Count||s.Readers.Where(x=>x.BorrowerId=="").Select(x=>NameKey(x.Name)).Distinct().Count()!=s.Readers.Count(x=>x.BorrowerId==""))throw new LibraryError("Invalid reading profiles in backup.");
  if(s.Readers.Any(r=>r.BorrowerId==null||r.BorrowerId!=""&&!s.Borrowers.Any(b=>b.Id==r.BorrowerId)))throw new LibraryError("Invalid borrower links in reading profiles.");
  if(s.Readers.Any(r=>r.AnnualGoals==null||r.AnnualGoals.Count>200||r.AnnualGoals.Any(g=>g.Key<1900||g.Key>2200||g.Value<0||g.Value>10000)))throw new LibraryError("Reading goals need a year from 1900 to 2200 and 0–10000 books.");
  var sessionIds=s.ReadingEntries.Where(x=>x?.Sessions!=null).SelectMany(x=>x.Sessions).Where(x=>x!=null).Select(x=>x.Id).ToArray();if(sessionIds.Distinct().Count()!=sessionIds.Length)throw new LibraryError("Duplicate reading session identities in backup.");
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
  if(action=="setReadingGoal"){
   if(!e.TryGetProperty("year",out var year)||year.ValueKind!=JsonValueKind.Number||!year.TryGetInt32(out var y)||y<1900||y>2200||!e.TryGetProperty("books",out var books)||books.ValueKind!=JsonValueKind.Number||!books.TryGetInt32(out var n)||n<0||n>10000)throw new LibraryError("Choose a year from 1900 to 2200 and a goal of 0–10000 books.");
   if(n==0)profile.AnnualGoals.Remove(y);else profile.AnnualGoals[y]=n;return profile;
  }
  var copy=Library.Copy(Field(e,"copyId"));
  var existing=s.ReadingEntries.Find(x=>x.ReaderId==readerId&&x.CopyId==copy);
  if(action=="removeReading"){
   if(existing==null)throw new LibraryError("Reading entry not found.");s.ReadingEntries.Remove(existing);return new{id=existing.Id};
  }
  if(action=="startReread"){
   if(existing==null||existing.Status!="finished")throw new LibraryError("Finish this reading session before starting a reread.");
   if(existing.Sessions.Count>=1000)throw new LibraryError("This book already has 1000 earlier reading sessions.");
   existing.Sessions.Add(new(){Id=Guid.NewGuid().ToString("N"),StartedDate=existing.StartedDate,FinishedDate=existing.FinishedDate,CurrentPage=existing.CurrentPage,TotalPages=existing.TotalPages,Rating=existing.Rating,Notes=existing.Notes});
   existing.Status="reading";existing.StartedDate=Library.Today;existing.FinishedDate="";existing.CurrentPage=0;existing.Rating=0;existing.Notes="";return existing;
  }
  if(action is "saveReadingAnnotation" or "removeReadingAnnotation"){
   if(existing==null)throw new LibraryError("Add this book to your reading list first.");
   var id=Field(e,"annotationId");var old=existing.Annotations.Find(a=>a.Id==id);
   if(action=="removeReadingAnnotation"){if(old==null)throw new LibraryError("Annotation not found.");existing.Annotations.Remove(old);return new{id};}
   if(id!=""&&old==null)throw new LibraryError("Annotation not found.");
   int? page=null;if(e.TryGetProperty("page",out var v)&&v.ValueKind!=JsonValueKind.Null){if(v.ValueKind!=JsonValueKind.Number||!v.TryGetInt32(out var number))throw new LibraryError("Enter a whole page number.");page=number;}
   var annotation=new ReadingAnnotation{Id=old?.Id??Guid.NewGuid().ToString("N"),Kind=Field(e,"kind"),Text=Field(e,"text").Trim(),Page=page};
   if(old!=null)existing.Annotations.Remove(old);existing.Annotations.Add(annotation);Entry(existing);return annotation;
  }
  if(!s.Books.Any(b=>b.Id==copy&&(!b.Archived||existing!=null)))throw new LibraryError("Choose an existing catalog book.");
  var rating=0;if(e.TryGetProperty("rating",out var star)&&(star.ValueKind!=JsonValueKind.Number||!star.TryGetInt32(out rating)))throw new LibraryError("Choose a whole-star rating from 1 to 5, or Unrated.");
  var entry=new ReadingEntry{Id=existing?.Id??Guid.NewGuid().ToString("N"),ReaderId=readerId,CopyId=copy,Status=Field(e,"status"),StartedDate=Field(e,"startedDate"),FinishedDate=Field(e,"finishedDate"),Rating=rating,Notes=Field(e,"notes").Trim(),CurrentPage=Number(e,"currentPage",existing?.CurrentPage??0),TotalPages=Number(e,"totalPages",existing?.TotalPages??0),Sessions=existing?.Sessions??[],Annotations=existing?.Annotations??[]};
  Entry(entry);if(entry.Status=="finished"&&entry.TotalPages>0)entry.CurrentPage=entry.TotalPages;
  if(existing==null)s.ReadingEntries.Add(entry);else s.ReadingEntries[s.ReadingEntries.IndexOf(existing)]=entry;
  return entry;
 }
}
