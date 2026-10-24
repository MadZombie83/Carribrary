using HomeLibrary;using Microsoft.AspNetCore.DataProtection;using System.Text.Json;
public static class ReadingTrackerTests {
 static JsonElement Input(object x)=>JsonSerializer.SerializeToElement(x,Library.Json);
 static void Check(bool ok,string why){if(!ok)throw new Exception("Reading tracker: "+why);}
 static void Bad(Action a){try{a();throw new Exception("Expected reading validation error");}catch(LibraryError){}}
 public static void Run(string path,IDataProtectionProvider protection){
  using var store=new Store(path,protection);
  JsonElement Change(string action,object value)=>JsonSerializer.SerializeToElement(store.Apply(new(action,Input(value),store.Read().Revision)),Library.Json).GetProperty("result");
  Change("addBook",new{isbn="9780140328721",title="Matilda",author="Roald Dahl"});Change("addBorrower",new{name="Visitor",contact="visitor@example.com"});
  var a=Change("addReader",new{name="Beth"}).GetProperty("id").GetString()!;var b=Change("addReader",new{name="Visitor",borrowerId="BR-0001"}).GetProperty("id").GetString()!;
  Bad(()=>Change("addReader",new{name=" beth "}));Bad(()=>Change("addReader",new{name="Other",borrowerId="BR-0001"}));
  Change("saveReading",new{readerId=a,copyId="BK-0001",status="finished",startedDate="2026-01-01",finishedDate="2026-01-02",rating=5,notes="PRIVATE HOUSEHOLD NOTES"});
  Change("saveReading",new{readerId=b,copyId="BK-0001",status="want",rating=0,notes="Visitor's own note"});
  Check(store.Read().State.ReadingEntries.Count==2&&store.Read().State.Loans.Count==0,"two independent readers, loans unaffected");
  Bad(()=>Change("saveReading",new{readerId=b,copyId="BK-0001",status="finished",startedDate="2026-01-02",finishedDate="2026-01-01"}));
  Bad(()=>Change("saveReading",new{readerId=b,copyId="BK-0001",status="want",rating=1.5}));Bad(()=>Change("saveReading",new{readerId=b,copyId="BK-0001",status="unknown"}));Bad(()=>Change("saveReading",new{readerId=b,copyId="BK-0001",status="want",notes=new string('x',2001)}));
  var before=store.Read().Revision;Bad(()=>Change("saveReading",new{readerId=b,copyId="BK-9999",status="want"}));Check(store.Read().Revision==before,"invalid entry rolls back");
  var own=store.PersonalAction(b,new("saveReading",Input(new{readerId=a,copyId="BK-0001",status="reading",startedDate="2026-01-01",rating=3}),store.Read().Revision));
  var ownJson=JsonSerializer.Serialize(own,Library.Json);Check(!ownJson.Contains("PRIVATE HOUSEHOLD")&&!ownJson.Contains("Beth")&&!ownJson.Contains("borrowerId"),"personal projection hides other readers and contacts");Check(store.Read().State.ReadingEntries.Single(e=>e.ReaderId==a).Status=="finished","spoofed readerId cannot overwrite another profile");
  Bad(()=>store.PersonalAction(b,new("removeBook",Input(new{copyId="BK-0001"}),store.Read().Revision)));
  var request=new PublicBookRequest("BK-0001","Pretend Beth","visitor@example.com","Please",Guid.NewGuid().ToString(),"9780140328721","Matilda");store.PersonalRequest(b,request);store.PersonalRequest(b,request);Check(store.Read().State.Requests.Count==1&&store.Read().State.Requests[0].ReaderId==b&&store.Read().State.Requests[0].Name=="Visitor","personal request scoped, named and idempotent");
  Check(!JsonSerializer.Serialize(ReadingAccess.View(store.Read(),a),Library.Json).Contains("pending"),"another reader's requests hidden");
  var access=new ReadingAccess(path,"https://books.example",protection);var status=Input(access.Change(b,"enable"));var url=status.GetProperty("url").GetString()!;var token=new Uri(url).Fragment[1..];Check(access.Resolve(token)==b,"personal fragment token resolves");Check(!File.ReadAllText(Path.Combine(path,"reading-access.protected")).Contains(token),"personal token protected on disk");
  access.Change(b,"replace");Check(access.Resolve(token)==null,"old personal QR revoked on replacement");var replacement=new Uri(Input(access.Status(b)).GetProperty("url").GetString()!).Fragment[1..];Check(new ReadingAccess(path,"https://books.example",protection).Resolve(replacement)==b,"personal access survives restart");access.Change(b,"disable");Check(access.Resolve(replacement)==null,"disabled link rejected");
  Change("cancelRequest",new{requestId=store.Read().State.Requests.Single().Id});
  Change("removeBook",new{copyId="BK-0001"});Check(store.Read().State.Books[0].Archived&&store.Read().State.ReadingEntries.Count==2,"deleting catalog copy preserves reading history");
  Check(Library.AvailableCopy(store.Read().State)==2,"reading copy number cannot be reused");Bad(()=>Change("removeReader",new{readerId=a}));
  Change("removeBorrower",new{borrowerId="BR-0001"});Check(store.Read().State.Borrowers[0].Archived,"borrower link preserved after deletion");
  var current=store.Read().State;store.Backup(current,"reading-test");var restored=store.RestoreEncrypted(File.ReadAllBytes(Path.Combine(path,"backups","reading-test.hlbackup")));Check(restored.Readers.Count==1&&restored.Readers[0].Id==a&&restored.ReadingEntries.Count==1&&restored.ReadingEntries[0].ReaderId==a,"library snapshots preserve household reading and omit borrower personal records");
  Check(!JsonSerializer.Serialize(Sharing.Books(current),Library.Json).Contains("PRIVATE HOUSEHOLD"),"public catalog excludes reading notes");
  var legacy=JsonSerializer.Deserialize<LibraryState>("{\"version\":1}",Library.Json)!;Library.Validate(legacy);Check(legacy.Readers.Count==0&&legacy.ReadingEntries.Count==0,"legacy library defaults to empty tracker");
  Console.WriteLine("PASS reading profiles, dates/ratings, isolation, borrower requests, personal link revocation, archives and backups");
 }
}
