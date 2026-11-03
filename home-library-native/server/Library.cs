using System.Text.Json;
using System.Text.RegularExpressions;
namespace HomeLibrary;
public sealed class LibraryState {
 public string LibrarySidebarArtwork {get;set;}="";
 public BorrowerHeaderLayout LibraryBorrowerHeader {get;set;}=new();
 public string LibraryBorrowerArtwork {get;set;}="";
 public string LibraryIcon192 {get;set;}="";
 public string LibraryIcon512 {get;set;}="";
 public List<LibraryPalette> SavedThemes {get;set;}=[];
 public string LibraryTheme {get;set;}=""; // Empty means a legacy server has not saved its browser palette yet.
 public string LibraryName {get;set;}="Carribrary";
 public string BannerSubtitle {get;set;}="A shelf worth sharing";
 public string EncouragingMessage {get;set;}="A place for every book.";
 public string CopyNumberPolicy {get;set;}="fill-gaps";
 public int Version {get;set;}=1; public long NextCopy {get;set;}=1;
 public List<LibraryCollection> Collections {get;set;}=[new(){Id="general",Name="General"}];
 public List<ReaderProfile> Readers {get;set;}=[];
 public List<Acquisition> BooksToBuy {get;set;}=[];
 public List<ReadingEntry> ReadingEntries {get;set;}=[];
 public RenewalPolicy RenewalPolicy {get;set;}=new();
 public SelfServiceSettings SelfService{get;set;}=new();
 public int CirculationVersion {get;set;}=0;
 public int MaxBatchCheckout {get;set;}=5;
 public int MaxActiveLoans {get;set;}=5;
 public List<CopyHold> Holds {get;set;}=[];
 public List<BookRequest> Requests {get;set;}=[];
 public List<DeletedRecord> RecycleBin {get;set;}=[];
 public List<Book> Books {get;set;}=[]; public List<Borrower> Borrowers {get;set;}=[]; public List<Loan> Loans {get;set;}=[];
}
public sealed class LibraryCollection {public string Id{get;set;}="";public string Name{get;set;}="";}
public sealed class Book {public string Genre{get;set;}="";public string ImportNotes{get;set;}="";public string CopyUid{get;set;}="";public string Room{get;set;}="";public string Bookcase{get;set;}="";public string Shelf{get;set;}="";public string Description{get;set;}="";public string CollectionId{get;set;}="general";public string Id{get;set;}="";public string Isbn{get;set;}="";public string Title{get;set;}="";public string Author{get;set;}="";public string Publisher{get;set;}="";public string CoverUrl{get;set;}="";public string Series{get;set;}="";public bool Archived{get;set;}}
public sealed class Borrower {public string BorrowerUid{get;set;}="";public string Id{get;set;}="";public string Name{get;set;}="";public string Contact{get;set;}="";public bool Archived{get;set;}public bool ReminderConsent{get;set;}}
public sealed class Loan {public string Source{get;set;}="librarian";public string KioskId{get;set;}="";public string ReturnSource{get;set;}="";public string ReturnRequestedAt{get;set;}="";public string ReturnRequestedSource{get;set;}="";public string ReturnKioskId{get;set;}="";public List<LoanRenewal> Renewals{get;set;}=[];public string Id{get;set;}="";public string CopyId{get;set;}="";public string BorrowerId{get;set;}="";public string CheckoutDate{get;set;}="";public string DueDate{get;set;}="";public string? ReturnedDate{get;set;}}
public sealed class DeletedRecord {public bool PreservedForHistory{get;set;}public string Id{get;set;}="";public string DeletedAt{get;set;}="";public Book? Book{get;set;}public Borrower? Borrower{get;set;}}
public sealed record Snapshot(long Revision,LibraryState State);
public sealed record ActionRequest(string Action,JsonElement Input,long Revision,string? RequestId=null);
public sealed class LibraryError(string message,int status=400):Exception(message){public int Status{get;}=status;}
public static class Library {
 public static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web);
 public static int AvailableCopy(LibraryState state){
  var occupied=new HashSet<int>();
  foreach(var id in state.Books.Select(b=>b.Id).Concat(state.Loans.Select(l=>l.CopyId)).Concat(state.Requests.Select(r=>r.CopyId)).Concat(state.ReadingEntries.Select(r=>r.CopyId)))
   if(id.StartsWith("BK-")&&int.TryParse(id[3..],out var number)&&number>0)occupied.Add(number);
  var next=state.CopyNumberPolicy=="next-number"?Math.Max(state.NextCopy,(long)occupied.DefaultIfEmpty(0).Max()+1):1L;while(next<=int.MaxValue&&occupied.Contains((int)next))next++;if(next>int.MaxValue)throw new LibraryError("No catalog numbers are available.");return (int)next;
 }
 public static string Today=>DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd");
 public static string Code(string? value)=>Regex.Replace((value??"").Trim().ToUpperInvariant(),@"[\s-]","");
 public static string Copy(string? value){var c=Code(value);return Regex.IsMatch(c,@"^BK\d+$")?"BK-"+c[2..].PadLeft(4,'0'):(value??"").Trim().ToUpperInvariant();}
 public static bool ValidCode(string? value){var s=Code(value);if(Regex.IsMatch(s,@"^\d{9}[\dX]$"))return s.Select((c,i)=>(c=='X'?10:c-'0')*(10-i)).Sum()%11==0;if(Regex.IsMatch(s,@"^\d{13}$"))return s.Select((c,i)=>(c-'0')*(i%2==0?1:3)).Sum()%10==0;return false;}
 static string Text(string? v,int max=500)=>(v??"").Trim()[..Math.Min((v??"").Trim().Length,max)];
 static bool DateOk(string? value)=>DateOnly.TryParseExact(value,"yyyy-MM-dd",out _);
 public static Loan? Active(LibraryState s,string copy)=>s.Loans.Find(l=>l.CopyId==Copy(copy)&&l.ReturnedDate==null);
 static string Field(JsonElement e,string name)=>e.TryGetProperty(name,out var v)&&v.ValueKind==JsonValueKind.String?v.GetString()??"":"";
 static string Match(string s)=>Regex.Replace(s.Trim().Normalize().ToUpperInvariant(),@"\s+"," ");
 public static string Edition(string value){var s=Code(value);if(s.Length!=10)return s;var p="978"+s[..9];var sum=p.Select((c,i)=>(c-'0')*(i%2==0?1:3)).Sum();return p+((10-sum%10)%10);}
 static string Cover(string v){if(v=="")return "";if(!Uri.TryCreate(v,UriKind.Absolute,out var u)||u.Scheme is not ("http" or "https"))throw new LibraryError("Cover URL must start with https:// or http://.");return v;}
 static void ValidateSaying(string? value,int max){if(value==null||value.Length>max||value.Any(char.IsControl))throw new LibraryError($"Sayings must be plain text of at most {max} characters.");}
 public static void Validate(LibraryState s){
  if(string.IsNullOrWhiteSpace(s.LibraryName)||s.LibraryName.Length>80||s.LibraryName.Any(char.IsControl))throw new LibraryError("Enter a library name of 1–80 characters.");
  if(s.LibraryTheme==null||!LibraryAppearance.Valid(s.LibraryTheme))throw new LibraryError("Choose a supported library color scheme.");
  CustomThemes.Validate(s);
  LibraryAppearance.ValidateIcons(s.LibraryIcon192,s.LibraryIcon512);
  BorrowerHeaderLayout.Validate(s.LibraryBorrowerHeader);LibraryAppearance.ValidateSidebarArtwork(s.LibrarySidebarArtwork);LibraryAppearance.ValidateBorrowerArtwork(s.LibraryBorrowerArtwork);
  ValidateSaying(s.BannerSubtitle,160);ValidateSaying(s.EncouragingMessage,240);
  if(s.Version is not (1 or 2 or 3 or 4 or 5 or 6 or 7)||s.NextCopy<1||s.NextCopy>(long)int.MaxValue+1||s.Books==null||s.Borrowers==null||s.Loans==null)throw new LibraryError("Invalid library backup.");
  if(s.Books.Any(x=>x==null)||s.Borrowers.Any(x=>x==null)||s.Loans.Any(x=>x==null))throw new LibraryError("Invalid records in backup.");
  if(s.Books.Select(x=>x.Id).Distinct().Count()!=s.Books.Count||s.Borrowers.Select(x=>x.Id).Distinct().Count()!=s.Borrowers.Count||s.Loans.Select(x=>x.Id).Distinct().Count()!=s.Loans.Count)throw new LibraryError("Duplicate record IDs in backup.");
  if(s.Collections==null||!s.Collections.Any(x=>x!=null&&x.Id=="general")||s.Collections.Any(x=>x==null||!Regex.IsMatch(x.Id??"",@"^(general|[a-f0-9]{32})$")||string.IsNullOrWhiteSpace(x.Name)||x.Name.Length>80||x.Name.Any(char.IsControl))||s.Collections.Select(x=>x.Id).Distinct().Count()!=s.Collections.Count||s.Collections.Select(x=>Match(x.Name)).Distinct().Count()!=s.Collections.Count)throw new LibraryError("Invalid collections in backup.");
  foreach(var b in s.Books){if(!s.Collections.Any(x=>x.Id==b.CollectionId))throw new LibraryError("Unknown book collection in backup.");if(b==null||!Regex.IsMatch(b.Id??"",@"^BK-\d{4,}$")||(b.Isbn!=""||s.Version<4)&&!ValidCode(b.Isbn)||string.IsNullOrWhiteSpace(b.Title)||b.Author==null||b.Publisher==null||b.CoverUrl==null||b.Series==null)throw new LibraryError("Invalid book in backup.");if(new[]{b.Room,b.Bookcase,b.Shelf}.Any(v=>v==null||v.Length>120||v.Any(char.IsControl)))throw new LibraryError("Shelf locations must be plain text of at most 120 characters per field.");if(b.Genre==null||b.Genre.Length>120||b.Genre.Any(char.IsControl))throw new LibraryError("Genre must be plain text up to 120 characters.");if(b.Description==null||b.Description.Length>4000)throw new LibraryError("Keep the book description under 4,000 characters.");if(b.ImportNotes==null||b.ImportNotes.Length>8000)throw new LibraryError("Import notes must be at most 8000 characters.");Cover(b.CoverUrl);}
  foreach(var b in s.Borrowers)if(b==null||!Regex.IsMatch(b.Id??"",@"^BR-\d{4,}$")||string.IsNullOrWhiteSpace(b.Name)||b.Contact==null)throw new LibraryError("Invalid borrower in backup.");
  if(s.CopyNumberPolicy is not ("fill-gaps" or "next-number"))throw new LibraryError("Choose a supported copy-number policy.");
  CopyIdentity.Validate(s);Acquisitions.Validate(s);Recovery.Validate(s);ReadingTracker.Validate(s);BookRequests.Validate(s);CirculationPolicy.Validate(s);
  Renewals.Validate(s);SelfService.Validate(s);
  if(s.Version>=7&&(s.Borrowers.Any(b=>!Guid.TryParseExact(b.BorrowerUid,"N",out _))||s.Borrowers.Select(b=>b.BorrowerUid).Distinct().Count()!=s.Borrowers.Count))throw new LibraryError("Invalid permanent borrower identity.");
  var active=new HashSet<string>();
  foreach(var l in s.Loans){var book=s.Books.Find(b=>b.Id==l.CopyId);var person=s.Borrowers.Find(b=>b.Id==l.BorrowerId);
   if(string.IsNullOrEmpty(l.Id)||book==null||person==null||!DateOk(l.CheckoutDate)||string.CompareOrdinal(l.CheckoutDate,Today)>0||l.DueDate==null||(l.DueDate!=""&&(!DateOk(l.DueDate)||string.CompareOrdinal(l.DueDate,l.CheckoutDate)<0))||(l.ReturnedDate!=null&&(!DateOk(l.ReturnedDate)||string.CompareOrdinal(l.ReturnedDate,l.CheckoutDate)<0||string.CompareOrdinal(l.ReturnedDate,Today)>0)))throw new LibraryError("Invalid loan in backup.");
   if(l.ReturnedDate==null&&(!active.Add(l.CopyId)||book.Archived||person.Archived))throw new LibraryError("Invalid active loan in backup.");
  }
 }
 static Book Details(JsonElement e)=>new(){Genre=Text(Field(e,"genre"),120),ImportNotes=Text(Field(e,"importNotes"),8000),Room=Field(e,"room").Trim(),Bookcase=Field(e,"bookcase").Trim(),Shelf=Field(e,"shelf").Trim(),Description=Text(Field(e,"description"),4000),Isbn=Code(Field(e,"isbn")),Title=Text(Field(e,"title")),Author=Text(Field(e,"author")),Publisher=Text(Field(e,"publisher")),Series=Text(Field(e,"series")),CoverUrl=Cover(Text(Field(e,"coverUrl"),2000))};
 static void DuplicateCheck(LibraryState s,Book b,Book? old,JsonElement input){
  if(old!=null&&Edition(old.Isbn)==Edition(b.Isbn)&&Match(old.Title)==Match(b.Title)&&Match(old.Author)==Match(b.Author))return;
  var matches=s.Books.Where(x=>!x.Archived&&x.Id!=old?.Id&&((b.Isbn!=""&&Edition(x.Isbn)==Edition(b.Isbn))||(b.Title!=""&&b.Author!=""&&Match(x.Title)==Match(b.Title)&&Match(x.Author)==Match(b.Author)))).ToList();
  var confirmed=input.TryGetProperty("confirmedDuplicates",out var list)&&list.ValueKind==JsonValueKind.Array?list.EnumerateArray().Where(x=>x.ValueKind==JsonValueKind.String).Select(x=>x.GetString()).ToHashSet():[];
  if(matches.Any(x=>!confirmed.Contains(x.Id)))throw new LibraryError("Possible duplicate: "+string.Join(", ",matches.Select(x=>x.Id))+". Review and confirm before saving another copy.");
 }
 public static object Apply(LibraryState s,string action,JsonElement e){
  if(action is "startReread" or "setReadingGoal" or "saveReadingAnnotation" or "removeReadingAnnotation")return ReadingTracker.Apply(s,action,e);
  if(action=="addBooksBatch")return CatalogBatch.Apply(s,e);
  if(action is "setRenewalPolicy" or "requestRenewal" or "approveRenewal" or "declineRenewal")return Renewals.Apply(s,action,e);
  if(action=="clearHistory")return HistoryTools.Apply(s,e);
  if(action is "saveAcquisition" or "removeAcquisition" or "acquireBook")return Acquisitions.Apply(s,action,e);
  if(action=="setSelfService"){var v=e.Deserialize<SelfServiceSettings>(Json)??throw new LibraryError("Invalid self-service settings.");s.SelfService=v;SelfService.Validate(s);return v;}
  if(action is "createHold" or "releaseHold" or "readyHold" or "handoverHold" or "setLendingLimits" or "batchCheckout" or "batchReturn")return CirculationPolicy.Apply(s,action,e);
  if(action is "restoreBorrower" or "restoreDeleted" or "purgeDeleted" or "bulkEditBooks")return Recovery.Apply(s,action,e);
  if(action is "requestBook" or "approveRequest" or "declineRequest" or "cancelRequest" or "fulfillRequest")return BookRequests.Apply(s,action,e);
  if(action is "addReader" or "renameReader" or "removeReader" or "saveReading" or "removeReading")return ReadingTracker.Apply(s,action,e);
  var copy=Copy(Field(e,"copyId"));
  switch(action){
   case "addCollection":case "renameCollection":{
    var name=Field(e,"name").Trim();if(name.Length==0||name.Length>80||name.Any(char.IsControl))throw new LibraryError("Enter a collection name of 1–80 characters.");
    var id=action=="renameCollection"?Field(e,"collectionId"):"";if(s.Collections.Any(x=>x.Id!=id&&Match(x.Name)==Match(name)))throw new LibraryError("A collection with that name already exists.");
    if(action=="addCollection"){var item=new LibraryCollection{Id=Guid.NewGuid().ToString("N"),Name=name};s.Collections.Add(item);return item;}
    var existing=s.Collections.Find(x=>x.Id==id)??throw new LibraryError("Collection not found.");existing.Name=name;return existing;
   }
   case "removeCollection":{
    var id=Field(e,"collectionId");if(id=="general")throw new LibraryError("Keep the default collection; you can rename it.");
    var item=s.Collections.Find(x=>x.Id==id)??throw new LibraryError("Collection not found.");
    if(s.Books.Any(x=>x.CollectionId==id))throw new LibraryError("Move this collection's books first. Archived books also preserve their collection.");
    s.Collections.Remove(item);return new{id=item.Id};
   }
   case "setCopyNumberPolicy":{var policy=Field(e,"policy");if(policy is not ("fill-gaps" or "next-number"))throw new LibraryError("Choose a supported copy-number policy.");s.CopyNumberPolicy=policy;return new{policy};}
   case "setLibraryIdentity":{var name=Field(e,"libraryName").Trim();if(name.Length==0||name.Length>80||name.Any(char.IsControl))throw new LibraryError("Enter a library name of 1–80 characters.");var subtitle=Field(e,"bannerSubtitle").Trim();var message=Field(e,"encouragingMessage").Trim();ValidateSaying(subtitle,160);ValidateSaying(message,240);var small=Field(e,"icon192");var large=Field(e,"icon512");LibraryAppearance.ValidateIcons(small,large);s.LibraryName=name;s.BannerSubtitle=subtitle;s.EncouragingMessage=message;s.LibraryIcon192=small;s.LibraryIcon512=large;return new{saved=true};}
   case "setLibraryPresentation":{var subtitle=Field(e,"bannerSubtitle").Trim();var message=Field(e,"encouragingMessage").Trim();ValidateSaying(subtitle,160);ValidateSaying(message,240);s.BannerSubtitle=subtitle;s.EncouragingMessage=message;return new{bannerSubtitle=subtitle,encouragingMessage=message};}
   case "setLibraryBorrowerArtwork":{var artwork=Field(e,"artwork");LibraryAppearance.ValidateBorrowerArtwork(artwork);s.LibraryBorrowerArtwork=artwork;if(CustomThemes.Selected(s) is {} selected)selected.BorrowerArtwork=artwork;return new{saved=artwork!=""};}
   case "setLibrarySidebarArtwork":{var artwork=Field(e,"artwork");LibraryAppearance.ValidateSidebarArtwork(artwork);s.LibrarySidebarArtwork=artwork;if(CustomThemes.Selected(s) is {} selected)selected.SidebarArtwork=artwork;return new{saved=artwork!=""};}
   case "setLibraryIcon":{var small=Field(e,"icon192");var large=Field(e,"icon512");LibraryAppearance.ValidateIcons(small,large);s.LibraryIcon192=small;s.LibraryIcon512=large;return new{saved=large!=""};}
   case "saveCustomTheme":return CustomThemes.Save(s,e);
   case "setLibraryTheme":{var theme=Field(e,"libraryTheme");if(theme==""||!LibraryAppearance.Valid(theme))throw new LibraryError("Choose a supported library color scheme.");if(CustomThemes.IsCustom(theme)&&!s.SavedThemes.Any(t=>"custom-"+t.Id==theme))throw new LibraryError("Saved theme not found.");s.LibraryTheme=theme;s.LibraryBorrowerHeader=CustomThemes.Selected(s)?.BorrowerHeader??new BorrowerHeaderLayout();s.LibrarySidebarArtwork=CustomThemes.Selected(s)?.SidebarArtwork??"";s.LibraryBorrowerArtwork=CustomThemes.Selected(s)?.BorrowerArtwork??"";return new{libraryTheme=theme};}
   case "setLibraryName":{var name=Field(e,"libraryName").Trim();if(name.Length==0||name.Length>80||name.Any(char.IsControl))throw new LibraryError("Enter a library name of 1–80 characters.");s.LibraryName=name;return new{libraryName=name};}
   case "addBook":case "updateBook":{
    var b=Details(e);if((b.Isbn!=""||s.Version<4)&&!ValidCode(b.Isbn)||b.Title=="")throw new LibraryError("Enter a title and a valid ISBN, or leave ISBN blank for an unnumbered book.");
    var old=action=="updateBook"?s.Books.Find(x=>x.Id==copy&&!x.Archived):null;
    if(action=="updateBook"&&old==null)throw new LibraryError("Book copy not found.");DuplicateCheck(s,b,old,e);
    if(old!=null&&!e.TryGetProperty("collectionId",out _))b.CollectionId=old.CollectionId;
    else b.CollectionId=Field(e,"collectionId")==""?"general":Field(e,"collectionId");
    if(!s.Collections.Any(x=>x.Id==b.CollectionId))throw new LibraryError("Choose an existing collection.");
    if(old!=null){if(!e.TryGetProperty("genre",out _))b.Genre=old.Genre;if(!e.TryGetProperty("importNotes",out _))b.ImportNotes=old.ImportNotes;if(!e.TryGetProperty("room",out _))b.Room=old.Room;if(!e.TryGetProperty("bookcase",out _))b.Bookcase=old.Bookcase;if(!e.TryGetProperty("shelf",out _))b.Shelf=old.Shelf;if(!e.TryGetProperty("description",out _))b.Description=old.Description;b.CopyUid=old.CopyUid;b.Id=old.Id;s.Books[s.Books.IndexOf(old)]=b;}else{var next=AvailableCopy(s);b.CopyUid=Guid.NewGuid().ToString("N");b.Id="BK-"+next.ToString("D4");s.NextCopy=Math.Max(s.NextCopy,(long)next+1);s.Books.Add(b);}return b;
   }
   case "updateBorrower":{var b=s.Borrowers.Find(x=>x.Id==Field(e,"borrowerId")&&!x.Archived)??throw new LibraryError("Borrower not found.");var name=Text(Field(e,"name"));if(name=="")throw new LibraryError("Enter the borrower’s name.");b.Name=name;b.Contact=Text(Field(e,"contact"));b.ReminderConsent=e.TryGetProperty("reminderConsent",out var consent)&&consent.ValueKind==JsonValueKind.True;return b;}
   case "addBorrower":{var name=Text(Field(e,"name"));if(name=="")throw new LibraryError("Enter the borrower’s name.");var next=s.Borrowers.Select(x=>int.Parse(x.Id[3..])).DefaultIfEmpty(0).Max()+1;var b=new Borrower{BorrowerUid=Guid.NewGuid().ToString("N"),Id="BR-"+next.ToString("D4"),Name=name,Contact=Text(Field(e,"contact"))};b.ReminderConsent=e.TryGetProperty("reminderConsent",out var consent)&&consent.ValueKind==JsonValueKind.True;s.Borrowers.Add(b);return b;}
   case "checkout":{
    if(!s.Books.Any(b=>b.Id==copy&&!b.Archived))throw new LibraryError("Copy ID not found.");if(Active(s,copy)!=null)throw new LibraryError("This copy is already on loan.");
    var borrower=Field(e,"borrowerId");var date=Field(e,"checkoutDate");var due=Field(e,"dueDate");
    if(!s.Borrowers.Any(b=>b.Id==borrower&&!b.Archived))throw new LibraryError("Select a borrower.");
    if(!DateOk(date)||string.CompareOrdinal(date,Today)>0)throw new LibraryError("Choose a valid checkout date, today or earlier.");
    if(due!=""&&(!DateOk(due)||string.CompareOrdinal(due,date)<0))throw new LibraryError("Due date must be on or after checkout.");
    var hold=CirculationPolicy.CheckCheckout(s,copy,borrower,e);
    var loan=new Loan{Id=Guid.NewGuid().ToString(),CopyId=copy,BorrowerId=borrower,CheckoutDate=date,DueDate=due};s.Loans.Add(loan);CirculationPolicy.Complete(s,hold,loan);return loan;
   }
   case "returnBook":{var loan=Active(s,copy)??throw new LibraryError("No active loan found for that copy ID.");loan.ReturnedDate=Today;loan.ReturnSource="librarian";foreach(var renewal in loan.Renewals.Where(r=>r.Status=="pending")){renewal.Status="cancelled";renewal.DecidedAt=DateTimeOffset.UtcNow.ToString("O");}return loan;}
   case "removeBook":{var b=s.Books.Find(x=>x.Id==copy&&!x.Archived)??throw new LibraryError("Book copy not found.");if(Active(s,copy)!=null)throw new LibraryError("Return this copy before deleting it.");if(CirculationPolicy.Hold(s,copy)!=null)throw new LibraryError("Release this copy’s hold before deleting it.");s.RecycleBin.Add(new(){Id=Guid.NewGuid().ToString("N"),DeletedAt=DateTimeOffset.UtcNow.ToString("O"),PreservedForHistory=s.Loans.Any(x=>x.CopyId==copy)||s.Requests.Any(x=>x.CopyId==copy)||s.ReadingEntries.Any(x=>x.CopyId==copy),Book=JsonSerializer.Deserialize<Book>(JsonSerializer.Serialize(b,Json),Json)});if(s.Loans.Any(x=>x.CopyId==copy)||s.Requests.Any(x=>x.CopyId==copy)||s.ReadingEntries.Any(x=>x.CopyId==copy))b.Archived=true;else s.Books.Remove(b);return new{id=b.Id,name=b.Title,archived=b.Archived};}
   case "removeBorrower":{var id=Field(e,"borrowerId");var b=s.Borrowers.Find(x=>x.Id==id&&!x.Archived)??throw new LibraryError("Borrower not found.");if(s.Holds.Any(h=>h.BorrowerId==id))throw new LibraryError("Release this borrower’s holds before deleting them.");if(s.Loans.Any(x=>x.BorrowerId==id&&x.ReturnedDate==null))throw new LibraryError("Return this borrower’s books before deleting them.");s.RecycleBin.Add(new(){Id=Guid.NewGuid().ToString("N"),DeletedAt=DateTimeOffset.UtcNow.ToString("O"),PreservedForHistory=s.Loans.Any(x=>x.BorrowerId==id)||s.Readers.Any(x=>x.BorrowerId==id),Borrower=JsonSerializer.Deserialize<Borrower>(JsonSerializer.Serialize(b,Json),Json)});if(s.Loans.Any(x=>x.BorrowerId==id)||s.Readers.Any(x=>x.BorrowerId==id))b.Archived=true;else s.Borrowers.Remove(b);return new{id=b.Id,name=b.Name,archived=b.Archived};}
   default:throw new LibraryError("Unknown action.");
  }
 }
}
