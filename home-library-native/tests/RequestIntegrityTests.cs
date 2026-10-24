using HomeLibrary;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;
public static class RequestIntegrityTests {
 public static async Task Run(string directory,IDataProtectionProvider protection){
  void Check(bool ok,string message){if(!ok)throw new Exception("Request integrity: "+message);Console.WriteLine("PASS "+message);}
  JsonElement Json(object value)=>JsonSerializer.SerializeToElement(value,Library.Json);
  using var store=new Store(directory,protection);
  object Change(string action,object input,long? revision=null)=>store.Apply(new(action,Json(input),revision??store.Read().Revision));
  void Rejected(Action work,int? code=null){try{work();throw new Exception("Unsafe operation accepted");}catch(LibraryError e){if(code!=null)Check(e.Status==code,"rejected operation returns expected status");}}
  foreach(var title in new[]{"Blood Magick","Second physical copy","Unrelated loan","Unrelated hold"})Change("addBook",new{isbn="9780140328721",title,author="Nora Roberts",confirmedDuplicates=store.Read().State.Books.Select(b=>b.Id).ToArray()});
  Change("addBorrower",new{name="Beth"});Change("addBorrower",new{name="Other"});
  var own=(ReaderProfile)JsonSerializer.Deserialize<ReaderProfile>(Json(Change("addReader",new{name="Beth",borrowerId="BR-0001"})).GetProperty("result"),Library.Json)!;
  var other=(ReaderProfile)JsonSerializer.Deserialize<ReaderProfile>(Json(Change("addReader",new{name="Other",borrowerId="BR-0002"})).GetProperty("result"),Library.Json)!;
  string Request(string reader,string copy){var book=store.Read().State.Books.Single(b=>b.Id==copy);store.PersonalRequest(reader,new(copy,"Ignored client name","test@example.com","",Guid.NewGuid().ToString(),book.Isbn,book.Title));return store.Read().State.Requests.Last().Id;}
  void Cancel(string reader,string id,long? revision=null)=>store.PersonalAction(reader,new("cancelRequest",Json(new{requestId=id}),revision??store.Read().Revision));
  Change("checkout",new{copyId="BK-0003",borrowerId="BR-0002",checkoutDate=Library.Today});Change("createHold",new{copyId="BK-0004",borrowerId="BR-0002"});
  var unrelated=JsonSerializer.Serialize(store.Read().State.Holds.Single(),Library.Json);var loan=JsonSerializer.Serialize(store.Read().State.Loans.Single(),Library.Json);
  string first=Request(own.Id,"BK-0001"),second=Request(other.Id,"BK-0002");
  Check(store.Read().State.Holds.Count==3,"new requests reserve distinct physical copies independently");
  Rejected(()=>Request(other.Id,"BK-0001"),409);Rejected(()=>Cancel(other.Id,first),404);
  var stale=store.Read().Revision;Change("setLibraryName",new{libraryName="Unrelated admin edit"});Rejected(()=>Cancel(own.Id,first,stale),409);
  Check(store.Read().State.Requests.Single(r=>r.Id==first).Status=="pending","conflicting cancellation makes no partial state change");
  Cancel(own.Id,first);Check(store.Read().State.Requests.Single(r=>r.Id==first).Status=="cancelled"&&CirculationPolicy.Availability(store.Read().State,"BK-0001")=="available","cancel closes exact request and releases hold without history deletion");
  var revision=store.Read().Revision;Rejected(()=>Cancel(own.Id,first));Check(store.Read().Revision==revision,"repeat cancellation is safe without another mutation");
  Change("declineRequest",new{requestId=second});Check(CirculationPolicy.Availability(store.Read().State,"BK-0002")=="available"&&store.Read().State.Requests.Single(r=>r.Id==second).Status=="declined","decline closes exact request and releases hold without history deletion");
  var newer=Request(own.Id,"BK-0001");Rejected(()=>Cancel(own.Id,first));Check(CirculationPolicy.Hold(store.Read().State,"BK-0001")?.RequestId==newer,"old closed request cannot release a newer hold on the same copy");
  var raceRevision=store.Read().Revision;var races=await Task.WhenAll(Task.Run(()=>Attempt(()=>Change("approveRequest",new{requestId=newer},raceRevision))),Task.Run(()=>Attempt(()=>Cancel(own.Id,newer,raceRevision))));
  Check(races.Count(x=>x)==1,"approval/cancellation race has exactly one committed transition");Library.Validate(store.Read().State);
  if(store.Read().State.Requests.Single(r=>r.Id==newer).Status=="approved")Cancel(own.Id,newer);
  Check(CirculationPolicy.Hold(store.Read().State,"BK-0001")==null,"approved request can still be cancelled under existing business rules");
  var contested=Request(own.Id,"BK-0002");raceRevision=store.Read().Revision;races=await Task.WhenAll(Task.Run(()=>Attempt(()=>Change("declineRequest",new{requestId=contested},raceRevision))),Task.Run(()=>Attempt(()=>Cancel(own.Id,contested,raceRevision))));
  Check(races.Count(x=>x)==1&&!store.Read().State.Holds.Any(h=>h.RequestId==contested),"decline/cancellation race closes request and releases reservation atomically");
  var active=Request(own.Id,"BK-0001");var before=store.Read().State;var selection=new HistorySelection(AllReaders:true,Reading:false,Loans:true,Requests:true);var preview=store.PreviewHistory(selection);
  Check(preview.Requests==4&&preview.ProtectedLoans==1&&preview.ProtectedHolds==2,"history preview separates closed requests from valid active loans/holds");
  Change("clearHistory",selection with{PreviewToken=preview.PreviewToken,Confirmation="CLEAR HISTORY"});var state=store.Read().State;
  Check(state.Requests.Count==1&&state.Requests.Single().Id==active&&state.Holds.Count==2,"history clear preserves pending request and both active reservations");
  Check(JsonSerializer.Serialize(state.Holds.Single(h=>h.CopyId=="BK-0004"),Library.Json)==unrelated&&JsonSerializer.Serialize(state.Loans.Single(),Library.Json)==loan,"request transitions and history clear leave unrelated hold and loan unchanged");
  using(var reopened=new Store(directory,protection)){Check(reopened.Read().State.Requests.Single().Id==active&&reopened.Read().State.Holds.Count==2,"request/hold and history state persist after reopening database");Library.Validate(reopened.Read().State);}
  // Old overlapping requests stay associated by request ID, not title or visible copy code alone.
  var legacy=store.Read().State;legacy.CirculationVersion=0;legacy.Requests=[new(){Id="RQ-"+Guid.NewGuid(),CopyId="BK-0002",Name="Legacy A",Contact="a@example.com",Status="pending",SubmittedAt=DateTimeOffset.UtcNow.ToString("O")},new(){Id="RQ-"+Guid.NewGuid(),CopyId="BK-0002",Name="Legacy B",Contact="b@example.com",Status="pending",SubmittedAt=DateTimeOffset.UtcNow.ToString("O")}];legacy.Holds=[];CirculationPolicy.Migrate(legacy);var held=legacy.Holds.Single().RequestId;var unheld=legacy.Requests.Single(r=>r.Id!=held).Id;
  Library.Apply(legacy,"declineRequest",Json(new{requestId=unheld}));Check(legacy.Holds.Single().RequestId==held,"declining an unreserved legacy duplicate preserves the independently held request");Library.Validate(legacy);
  Library.Apply(legacy,"declineRequest",Json(new{requestId=held}));Check(legacy.Holds.Count==0&&legacy.Requests.All(r=>r.Status=="declined"),"declining remaining legacy request releases its exact hold");Library.Validate(legacy);
 }
 static bool Attempt(Action work){try{work();return true;}catch(LibraryError e)when(e.Status==409){return false;}}
}
