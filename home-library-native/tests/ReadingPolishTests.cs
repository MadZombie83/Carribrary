using HomeLibrary;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;
public static class ReadingPolishTests {
 static JsonElement J(object x)=>JsonSerializer.SerializeToElement(x,Library.Json);
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);}
 static void Bad(Action action){try{action();throw new Exception("Expected reading validation failure");}catch(LibraryError){}}
 public static void Run(string directory,IDataProtectionProvider provider){
  using var store=new Store(directory,provider);
  JsonElement Act(string action,object input)=>J(store.Apply(new(action,J(input),store.Read().Revision))).GetProperty("result");
  Act("addBook",new{title="Reading history",isbn="9780140328721",genre="Fiction"});var reader=Act("addReader",new{name="Household reader"}).GetProperty("id").GetString()!;
  Act("saveReading",new{readerId=reader,copyId="BK-0001",status="reading",currentPage=12,totalPages=240,notes="Session one"});
  Check(store.Read().State.ReadingEntries.Single().CurrentPage==12,"current and total pages persist");
  var revision=store.Read().Revision;Bad(()=>Act("saveReading",new{readerId=reader,copyId="BK-0001",status="reading",currentPage=241,totalPages=240}));Bad(()=>Act("saveReading",new{readerId=reader,copyId="BK-0001",status="reading",currentPage=1.5}));Check(revision==store.Read().Revision,"invalid progress cannot change existing records");
  Act("saveReading",new{readerId=reader,copyId="BK-0001",status="finished",startedDate="2026-01-01",finishedDate="2026-01-02",rating=5,notes="Session one"});
  Check(store.Read().State.ReadingEntries.Single().CurrentPage==240,"finishing fills known total and old clients preserve page fields");
  Act("saveReadingAnnotation",new{readerId=reader,copyId="BK-0001",kind="quote",text="A favorite passage",page=20});
  Act("startReread",new{readerId=reader,copyId="BK-0001"});var entry=store.Read().State.ReadingEntries.Single();
  Check(entry.Status=="reading"&&entry.CurrentPage==0&&entry.Sessions.Single().Notes=="Session one"&&entry.Sessions.Single().Rating==5&&entry.Sessions.Single().CurrentPage==240&&entry.Annotations.Single().Page==20,"reread preserves session dates, notes, rating, progress and book annotations");
  revision=store.Read().Revision;Bad(()=>Act("startReread",new{readerId=reader,copyId="BK-0001"}));Bad(()=>Act("saveReadingAnnotation",new{readerId=reader,copyId="BK-0001",kind="quote",text="bad page",page=0}));Check(store.Read().Revision==revision,"invalid reread and annotation roll back");
  Act("setReadingGoal",new{readerId=reader,year=2026,books=25});Check(store.Read().State.Readers.Single().AnnualGoals[2026]==25,"annual reading goals stored per reader");
  Bad(()=>Act("setReadingGoal",new{readerId=reader,year=2026,books=-1}));
  var preview=store.PreviewHistory(new(ReaderId:reader));Check(preview.Reading==1,"clear history previews earlier completed sessions during active reread");
  Act("clearHistory",new HistorySelection(ReaderId:reader,Confirmation:"CLEAR HISTORY",PreviewToken:preview.PreviewToken));entry=store.Read().State.ReadingEntries.Single();Check(entry.Status=="reading"&&entry.Sessions.Count==0&&entry.Annotations.Count==1,"clear completed history preserves active read and book-level annotations");
  var legacy=store.Read().State;legacy.Version=5;store.Backup(legacy,"legacy-reading");Act("restore",store.RestoreEncrypted(File.ReadAllBytes(Path.Combine(directory,"backups","legacy-reading.hlbackup"))));Check(store.Read().State.Version==6&&store.Read().State.ReadingEntries.Count==1,"old reading records migrate without changing references");
  var analytics=new LibraryInsights(directory,provider);analytics.Record("visits");Check(J(analytics.Report("all",store.Read().State)).GetProperty("counts").EnumerateObject().Count()==0,"analytics opt-in is disabled by default");analytics.Change(new(true));analytics.Record("searches");analytics.Record("zeroResults");analytics.Record("bookViews",1,store.Read().State.Books.Single().CopyUid);analytics.Record("visits",1,date:DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-10));
  var week=J(analytics.Report("7",store.Read().State));Check(week.GetProperty("counts").GetProperty("searches").GetInt64()==1&&week.GetProperty("counts").GetProperty("visits").GetInt64()==0&&week.GetProperty("popular")[0].GetProperty("views").GetInt64()==1,"aggregate search, zero results, popularity and range filters");
  var before=store.Read().State;Act("addBorrower",new{name="Visitor",contact="not-in-analytics@example.com"});Act("checkout",new{copyId="BK-0001",borrowerId="BR-0001",checkoutDate=Library.Today});analytics.Observe(before,store.Read().State);before=store.Read().State;Act("returnBook",new{copyId="BK-0001"});analytics.Observe(before,store.Read().State);
  var paused=new LibraryInsights(directory,provider,false);paused.Record("visits");Check(J(paused.Report("all",store.Read().State)).GetProperty("collectionPausedForRecovery").GetBoolean(),"recovery review suppresses optional analytics collection");
  var all=J(new LibraryInsights(directory,provider).Report("all",store.Read().State));Check(all.GetProperty("counts").GetProperty("checkouts").GetInt64()==1&&all.GetProperty("counts").GetProperty("returns").GetInt64()==1&&!all.GetRawText().Contains("not-in-analytics")&&!all.GetRawText().Contains("Session one"),"analytics survives restart and excludes contacts and reading notes");
  Bad(()=>analytics.Change(new(true,true,"wrong")));analytics.Change(new(false));analytics.Record("visits");analytics.Change(new(false,true,"CLEAR ANALYTICS"));Check(J(analytics.Report("all",store.Read().State)).GetProperty("counts").EnumerateObject().Count()==0,"analytics pause and safeguarded aggregate deletion");
 }
}
