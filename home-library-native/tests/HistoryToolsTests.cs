using HomeLibrary;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using System.Text.Json;
public static class HistoryToolsTests {
 static JsonElement Json(object x)=>JsonSerializer.SerializeToElement(x,Library.Json);
 static void Check(bool ok,string why){if(!ok)throw new Exception("History: "+why);Console.WriteLine("PASS "+why);}
 static void Bad(Action work){try{work();throw new Exception("Expected history rejection");}catch(LibraryError){}}
 public static void Run(string directory,IDataProtectionProvider protection){
  using var store=new Store(directory,protection);
  JsonElement Change(string action,object input)=>Json(store.Apply(new(action,Json(input),store.Read().Revision,Guid.NewGuid().ToString()))).GetProperty("result");
  Change("addBorrower",new{name="Sam",contact="sam@example.com"});Change("addBorrower",new{name="Beth",contact="beth@example.com"});
  var sam=Change("addReader",new{name="Sam",borrowerId="BR-0001"}).GetProperty("id").GetString()!;var beth=Change("addReader",new{name="Beth",borrowerId="BR-0002"}).GetProperty("id").GetString()!;
  for(var i=0;i<3;i++)Change("addBook",new{isbn="9780140328721",title="Matilda",author="Roald Dahl",confirmedDuplicates=store.Read().State.Books.Select(b=>b.Id).ToArray()});
  var req=new PublicBookRequest("BK-0001","Sam","sam@example.com","",Guid.NewGuid().ToString(),"9780140328721","Matilda");store.PersonalRequest(sam,req);var id=store.Read().State.Requests.Single().Id;Change("approveRequest",new{requestId=id});Change("fulfillRequest",new{requestId=id,checkoutDate=Library.Today});Change("returnBook",new{copyId="BK-0001"});
  Change("saveReading",new{readerId=sam,copyId="BK-0001",status="finished",notes="ERASE FINISHED NOTE",rating=5});Change("saveReading",new{readerId=sam,copyId="BK-0002",status="reading",notes="KEEP UNFINISHED"});Change("saveReading",new{readerId=beth,copyId="BK-0001",status="finished",notes="KEEP OTHER PERSON"});
  Change("checkout",new{copyId="BK-0002",borrowerId="BR-0001",checkoutDate=Library.Today});store.PersonalRequest(sam,req with{CopyId="BK-0003",RequestId=Guid.NewGuid().ToString()});
  var choice=new HistorySelection(sam,false,true,true,true);var preview=store.PreviewHistory(choice);
  Check(preview.Reading==1&&preview.Loans==1&&preview.Requests==1&&preview.ProtectedLoans==1&&preview.ProtectedHolds==1&&preview.UnfinishedReading==1,"history preview counts completed removal and protected active records");
  var spoof=store.PreviewHistory(choice with{ReaderId=beth});Bad(()=>store.PersonalAction(sam,new("clearHistory",Json(choice with{ReaderId=beth,PreviewToken=spoof.PreviewToken,Confirmation="CLEAR HISTORY"}),preview.Revision)));
  Bad(()=>Change("clearHistory",choice with{PreviewToken=preview.PreviewToken,Confirmation="yes"}));
  Change("setLibraryName",new{libraryName="Changed"});Bad(()=>Change("clearHistory",choice with{PreviewToken=preview.PreviewToken,Confirmation="CLEAR HISTORY"}));
  preview=store.PersonalHistory(sam,choice with{AllReaders=true});Check(preview.Reading==1,"personal preview cannot expand to every reader");
  var backups=Directory.GetFiles(Path.Combine(directory,"backups")).Length;
  store.PersonalAction(sam,new("clearHistory",Json(choice with{AllReaders=true,PreviewToken=preview.PreviewToken,Confirmation="CLEAR HISTORY"}),preview.Revision));
  var s=store.Read().State;Check(s.ReadingEntries.Count==2&&s.ReadingEntries.Any(e=>e.Notes=="KEEP OTHER PERSON")&&s.ReadingEntries.Any(e=>e.Notes=="KEEP UNFINISHED"),"personal clear preserves other readers and TBR/in-progress entries");
  Check(s.Loans.Count==1&&s.Loans[0].ReturnedDate==null&&s.Requests.Count==1&&s.Requests[0].Status=="pending"&&s.Holds.Count==1,"completed history clear preserves active loans, requests and holds");
  Check(Directory.GetFiles(Path.Combine(directory,"backups")).Length==backups,"clearing history creates no new copy of erased records");Library.Validate(s);
  using(var db=new SqliteConnection($"Data Source={Path.Combine(directory,"library.sqlite")}")){db.Open();using var cmd=db.CreateCommand();cmd.CommandText="SELECT group_concat(result) FROM receipts";Check(!Convert.ToString(cmd.ExecuteScalar())!.Contains("ERASE FINISHED NOTE"),"history erasure also removes private idempotency receipt copies");}
  var all=new HistorySelection(AllReaders:true,Reading:true);var last=store.PreviewHistory(all);Change("clearHistory",all with{PreviewToken=last.PreviewToken,Confirmation="CLEAR HISTORY"});Check(store.Read().State.ReadingEntries.Single().Status=="reading","admin can clear completed history across profiles without clearing unfinished entries");
 }
}
