using HomeLibrary;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using System.Text.Json;
public static class LendingPolicyTests {
 static JsonElement Json(object x)=>JsonSerializer.SerializeToElement(x,Library.Json);
 static void Check(bool ok,string message){if(!ok)throw new Exception("Lending: "+message);Console.WriteLine("PASS "+message);}
 static void Bad(Action work){try{work();throw new Exception("Expected lending rejection");}catch(LibraryError){}}
 public static async Task Run(string folder,IDataProtectionProvider protection){
  Directory.CreateDirectory(folder);
  using(var store=new Store(folder,protection)){
   JsonElement Change(string action,object input)=>Json(store.Apply(new(action,Json(input),store.Read().Revision,Guid.NewGuid().ToString()))).GetProperty("result");
   Change("addBorrower",new{name="Beth"});Change("addBorrower",new{name="Sam"});
   for(var n=0;n<7;n++)Change("addBook",new{isbn="9780140328721",title="Matilda",author="Roald Dahl",confirmedDuplicates=store.Read().State.Books.Select(b=>b.Id).ToArray()});
   PublicBookRequest Request(string copy,string who)=>new(copy,who,who+"@example.com","",Guid.NewGuid().ToString(),"9780140328721","Matilda");
   var outcomes=await Task.WhenAll(Enumerable.Range(0,2).Select(n=>Task.Run(()=>{try{store.SubmitRequest(Request("BK-0001","visitor"+n));return true;}catch(LibraryError){return false;}})));
   Check(outcomes.Count(x=>x)==1&&store.Read().State.Holds.Count==1,"simultaneous requests reserve exactly one physical copy");
   var hold=store.Read().State.Holds.Single();var requestId=hold.RequestId;
   Bad(()=>Change("checkout",new{copyId="BK-0001",borrowerId="BR-0001",checkoutDate=Library.Today}));
   Bad(()=>Change("removeBook",new{copyId="BK-0001"}));
   Check(Json(Sharing.Books(store.Read().State)).ToString().Contains("on-hold")&&!Json(Sharing.Books(store.Read().State)).ToString().Contains("visitor"),"public availability exposes no requester details");
   Change("approveRequest",new{requestId});Change("readyHold",new{holdId=hold.Id});Check(store.Read().State.Loans.Count==0,"approval and ready-for-pickup do not create loans");
   Bad(()=>Change("handoverHold",new{holdId=hold.Id,checkoutDate=Library.Today}));
   Change("handoverHold",new{holdId=hold.Id,checkoutDate=Library.Today,handoverConfirmed=true});Check(store.Read().State.Holds.Count==0&&store.Read().State.Requests.Single().Status=="fulfilled","confirmed handover converts reservation into one real loan");
   Bad(()=>Change("handoverHold",new{holdId=hold.Id,checkoutDate=Library.Today,handoverConfirmed=true}));
   Change("returnBook",new{copyId="BK-0001"});
   var manual=Change("createHold",new{copyId="BK-0001",borrowerId="BR-0001"}).GetProperty("id").GetString()!;
   Bad(()=>Change("checkout",new{copyId="BK-0001",borrowerId="BR-0002",holdId=manual,checkoutDate=Library.Today}));
   Bad(()=>Change("removeBorrower",new{borrowerId="BR-0001"}));Change("releaseHold",new{holdId=manual});
   var ids=new[]{"BK-0001","BK-0002","BK-0003","BK-0004","BK-0005"};
   var action=new ActionRequest("batchCheckout",Json(new{copyIds=ids,borrowerId="BR-0001",checkoutDate=Library.Today,dueDate="",handoverConfirmed=true}),store.Read().Revision,Guid.NewGuid().ToString());
   store.Apply(action);store.Apply(action);Check(store.Read().State.Loans.Count(l=>l.ReturnedDate==null)==5,"five-book batch and interrupted retry create five loans once");
   Bad(()=>Change("checkout",new{copyId="BK-0006",borrowerId="BR-0001",checkoutDate=Library.Today}));
   Bad(()=>Change("batchCheckout",new{copyIds=new[]{"BK-0006","BK-0007"},borrowerId="BR-0001",checkoutDate=Library.Today,handoverConfirmed=true}));
   Check(store.Read().State.Loans.Count(l=>l.ReturnedDate==null)==5,"active-loan limit cannot be bypassed with another batch or single checkout");
   var revision=store.Read().Revision;Bad(()=>Change("batchReturn",new{copyIds=new[]{"BK-0001","BK-0007"}}));Check(store.Read().Revision==revision&&Library.Active(store.Read().State,"BK-0001")!=null,"one invalid return rolls back the whole batch");
   Change("setLendingLimits",new{maxBatchCheckout=1,maxActiveLoans=1});Change("batchReturn",new{copyIds=ids});Check(store.Read().State.Loans.All(l=>l.ReturnedDate!=null),"all five books can be returned despite a one-book borrowing limit");
   Bad(()=>Change("batchCheckout",new{copyIds=new[]{"BK-0001","BK-0001"},borrowerId="BR-0001",checkoutDate=Library.Today,handoverConfirmed=true}));
   Bad(()=>Change("batchCheckout",new{copyIds=new[]{"BK-0001"},borrowerId="BR-0001",checkoutDate=Library.Today}));
   var personal=(ReaderProfile)JsonSerializer.Deserialize<ReaderProfile>(Change("addReader",new{name="Sam",borrowerId="BR-0002"}),Library.Json)!;
   store.PersonalRequest(personal.Id,Request("BK-0001","Sam"));var personalId=store.Read().State.Requests.Last().Id;
   var other=(ReaderProfile)JsonSerializer.Deserialize<ReaderProfile>(Change("addReader",new{name="Beth",borrowerId="BR-0001"}),Library.Json)!;
   Bad(()=>store.PersonalAction(other.Id,new("cancelRequest",Json(new{requestId=personalId}),store.Read().Revision)));
   store.PersonalAction(personal.Id,new("cancelRequest",Json(new{requestId=personalId}),store.Read().Revision));Check(store.Read().State.Holds.Count==0&&store.Read().State.Requests.Last().Status=="cancelled","only the requesting reader can cancel their request and release its hold");
   // Persist a realistic old backup with conflicting open requests, then restart.
   var legacy=store.Read().State;legacy.CirculationVersion=0;legacy.Requests=[];legacy.Holds=[];
   foreach(var status in new[]{"pending","approved","pending"})legacy.Requests.Add(new(){Id="RQ-"+Guid.NewGuid(),CopyId="BK-0001",Name=status,Contact="legacy@example.com",Status=status,SubmittedAt=DateTimeOffset.UtcNow.ToString("O")});
   using var db=new SqliteConnection($"Data Source={Path.Combine(folder,"library.sqlite")}");db.Open();using var cmd=db.CreateCommand();cmd.CommandText="UPDATE library SET data=$data WHERE id=1";cmd.Parameters.AddWithValue("$data",JsonSerializer.Serialize(legacy,Library.Json));cmd.ExecuteNonQuery();
  }
  using(var migrated=new Store(folder,protection)){
   var s=migrated.Read().State;Check(s.CirculationVersion==1&&s.Requests.Count==3&&s.Holds.Count==1&&s.Requests.Single(r=>r.Id==s.Holds[0].RequestId).Status=="approved","legacy migration preserves overlapping requests and reserves approved request first");
   Check(Directory.GetFiles(Path.Combine(folder,"backups"),"before-circulation-v1-*.hlbackup").Length==1,"circulation migration creates a protected safety backup");var revision=migrated.Read().Revision;
   using var again=new Store(folder,protection);Check(again.Read().Revision==revision,"migration is idempotent on restart");
   var stale=s.Requests.First(r=>r.Id!=s.Holds[0].RequestId);Bad(()=>migrated.Apply(new("approveRequest",Json(new{requestId=stale.Id}),revision)));
   migrated.Backup(s,"holds-test");Check(migrated.RestoreEncrypted(File.ReadAllBytes(Path.Combine(folder,"backups","holds-test.hlbackup"))).Holds.Count==1,"holds and policy survive protected backup");
  }
 }
}
