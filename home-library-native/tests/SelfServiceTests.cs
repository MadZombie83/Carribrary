using HomeLibrary;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;
public static class SelfServiceTests {
 sealed class Clock:TimeProvider {public DateTimeOffset Now=DateTimeOffset.UtcNow;public override DateTimeOffset GetUtcNow()=>Now;}
 static JsonElement J(object x)=>JsonSerializer.SerializeToElement(x,Library.Json);
 static void Check(bool yes,string name){if(!yes)throw new Exception(name);Console.WriteLine("PASS self-service "+name);}
 static void Reject(Action action,string name){try{action();}catch(LibraryError){Console.WriteLine("PASS self-service rejects "+name);return;}throw new Exception("Accepted "+name);}
 public static void Run(string directory,IDataProtectionProvider protection){
  Directory.CreateDirectory(directory);using var store=new Store(directory,protection);
  void Apply(string action,object input)=>store.Apply(new(action,J(input),store.Read().Revision));
  Apply("addBook",new{isbn="9780141439518",title="Pride and Prejudice",author="Jane Austen"});
  Apply("addBook",new{isbn="9780141439518",title="Pride and Prejudice",author="Jane Austen",confirmedDuplicates=new[]{"BK-0001"}});
  Apply("addBorrower",new{name="One",contact=""});Apply("addBorrower",new{name="Two",contact=""});
  var books=store.Read().State.Books;string a="BR-0001",b="BR-0002";
  for(int flags=0;flags<8;flags++){
   Apply("setSelfService",new{checkout=(flags&1)!=0,@return=(flags&2)!=0,kiosk=(flags&4)!=0,loanDays=14,idleSeconds=30,returnInstructions="Return at desk"});
   foreach(bool returning in new[]{false,true})foreach(string source in new[]{"borrower-pwa","kiosk"}){
    bool allowed=(flags&(returning?2:1))!=0&&(source!="kiosk"||(flags&4)!=0);
    if(allowed)store.SelfPreview(a,"9780141439518",returning,source);else Reject(()=>store.SelfPreview(a,"9780141439518",returning,source),$"disabled {flags}/{returning}/{source}");
   }
  }
  var clock=new Clock();using var cards=new PatronCards(directory,protection,false,clock);
  var card=J(cards.Issue(store,a,"123456"));string code=card.GetProperty("code").GetString()!,id=card.GetProperty("id").GetString()!;
  Check(J(cards.Resolve(store,code)).GetProperty("borrowerId").GetString()==a,"card without online account resolves existing borrower");
  var pair=J(cards.Pair(store,"Foyer"));string pairCode=pair.GetProperty("code").GetString()!,station=cards.Enroll(store,pairCode);
  Reject(()=>cards.Enroll(store,pairCode),"pair code reuse");
  Reject(()=>cards.Verify(store,"invalid-station",code,"123456"),"unauthorized station");
  for(int i=0;i<5;i++)Reject(()=>cards.Verify(store,station,code,"999999"),"wrong PIN");
  Reject(()=>cards.Verify(store,station,code,"123456"),"PIN lockout even with correct PIN");clock.Now=clock.Now.AddMinutes(16);
  var verified=cards.Verify(store,station,code,"123456");
  Check(J(cards.AsBorrower(store,station,verified.Token,(borrower,_)=>store.SelfPreview(borrower,"9780141439518",false,"kiosk"))).GetProperty("copies").GetArrayLength()==2,"ISBN offers both available copies");
  clock.Now=clock.Now.AddSeconds(31);Reject(()=>cards.AsBorrower(store,station,verified.Token,(_,_)=>new{}),"idle session");
  verified=cards.Verify(store,station,code,"123456");cards.ResetPin(store,a,"654321");Reject(()=>cards.AsBorrower(store,station,verified.Token,(_,_)=>new{}),"PIN reset invalidates live session");
  verified=cards.Verify(store,station,code,"654321");cards.ChangeCard(id,"revoke");Reject(()=>cards.AsBorrower(store,station,verified.Token,(_,_)=>new{}),"card revocation immediately invalidates session");Reject(()=>cards.Resolve(store,code),"revoked card resolution");
  var replacement=J(cards.Issue(store,a,"654321"));string replacementCode=replacement.GetProperty("code").GetString()!;
  Reject(()=>cards.Resolve(store,code),"old card after replacement");
  verified=cards.Verify(store,station,replacementCode,"654321");
  var stations=J(cards.List(store)).GetProperty("stations");cards.RevokeStation(stations[0].GetProperty("id").GetString()!);Reject(()=>cards.AsBorrower(store,station,verified.Token,(_,_)=>new{}),"station revocation");
  var payload=new SelfServiceInput("9780141439518",Guid.NewGuid().ToString(),books[0].CopyUid);
  var result=J(store.SelfCirculate(a,payload,false,"borrower-pwa"));store.SelfCirculate(a,payload,false,"borrower-pwa");Check(store.Read().State.Loans.Count==1,"transaction retry idempotent");
  Reject(()=>store.SelfCirculate(b,payload,false,"borrower-pwa"),"borrower substitution on request replay");
  Check(J(store.SelfPreview(b,"9780141439518",false,"borrower-pwa")).GetProperty("copies").GetArrayLength()==1,"checkout excludes on-loan copies");
  Reject(()=>store.SelfCirculate(b,new("BK-0001",Guid.NewGuid().ToString(),books[0].CopyUid),true,"borrower-pwa"),"return somebody else’s loan");
  var ret=new SelfServiceInput("carribrary:copy:v1:"+books[0].CopyUid,Guid.NewGuid().ToString(),books[0].CopyUid);store.SelfCirculate(a,ret,true,"borrower-pwa");store.SelfCirculate(a,ret,true,"borrower-pwa");
  Check(SelfService.Pending(store.Read().State.Loans[0])&&Library.Active(store.Read().State,books[0].Id)!=null,"reported return stays active until physical confirmation");
  Check(J(store.SelfPreview(a,"BK-0001",false,"borrower-pwa")).GetProperty("copies").GetArrayLength()==0,"pending return unavailable for checkout");
  Apply("returnBook",new{copyId="BK-0001"});Check(store.Read().State.Loans[0].ReturnedDate!=null&&store.Read().State.Loans[0].ReturnSource=="librarian","normal librarian return finalizes physical receipt");
  Apply("createHold",new{copyId="BK-0001",borrowerId=b});Check(J(store.SelfPreview(a,"BK-0001",false,"borrower-pwa")).GetProperty("copies").GetArrayLength()==0,"other borrower hold excluded");
  store.SelfCirculate(b,new("BK-0001",Guid.NewGuid().ToString(),books[0].CopyUid),false,"borrower-pwa");Check(store.Read().State.Holds.Count==0,"eligible borrower hold consumed through normal circulation");
  Apply("setLendingLimits",new{maxActiveLoans=1,maxBatchCheckout=5});Reject(()=>store.SelfCirculate(b,new("BK-0002",Guid.NewGuid().ToString(),books[1].CopyUid),false,"borrower-pwa"),"active loan limit");
  Apply("returnBook",new{copyId="BK-0001"});
  int won=0;Parallel.Invoke(()=>{try{store.SelfCirculate(a,new("BK-0002",Guid.NewGuid().ToString(),books[1].CopyUid),false,"borrower-pwa");Interlocked.Increment(ref won);}catch(LibraryError){}},()=>{try{store.SelfCirculate(b,new("BK-0002",Guid.NewGuid().ToString(),books[1].CopyUid),false,"borrower-pwa");Interlocked.Increment(ref won);}catch(LibraryError){}});Check(won==1,"two borrowers race for one copy: exactly one checkout");
  var state=store.Read().State;Library.Validate(state);Check(state.Version==7&&state.Borrowers.Select(x=>x.BorrowerUid).Distinct().Count()==2,"v7 permanent borrower identities");

  var life=Path.Combine(directory,"..","card-lifecycle-"+Guid.NewGuid());Directory.CreateDirectory(life);
  using(var identities=new Store(life,protection))using(var identityCards=new PatronCards(life,protection)){
   identities.Apply(new("addBorrower",J(new{name="Original borrower"}),identities.Read().Revision));var first=J(identityCards.Issue(identities,"BR-0001",""));
   var deleted=identities.Read().State;deleted.Borrowers.Clear();deleted.RecycleBin.Clear();identities.Apply(new("restore",J(deleted),identities.Read().Revision));
   identities.Apply(new("addBorrower",J(new{name="New person with reused display number"}),identities.Read().Revision));
   Reject(()=>identityCards.Resolve(identities,first.GetProperty("code").GetString()!),"reused BR number cannot inherit a deleted person’s card");
   identityCards.PruneDeleted(identities);Check(J(identityCards.List(identities)).GetProperty("cards").GetArrayLength()==0,"permanent personal-data deletion removes obsolete card registrations and PINs");
  }Directory.Delete(life,true);
  var capture=CompleteBackup.Capture(directory,new Settings(),default);var archive=Path.Combine(capture,"self-service.carribrary-backup");using(var source=File.OpenRead(Path.Combine(capture,"snapshot.zip")))using(var target=File.Create(archive))BackupCipher.Encrypt(source,target,"Self service test recovery 253!");
  var destination=Path.Combine(directory,"..","self-service-restored-"+Guid.NewGuid());CompleteBackupRecovery.Unpack(archive,destination,"Self service test recovery 253!");
  using(var recoveredStore=new Store(Path.Combine(destination,"data"),protection))using(var recoveredCards=new PatronCards(Path.Combine(destination,"data"),protection,true,clock)){
   var recoveredState=recoveredStore.Read().State;Check(recoveredState.SelfService.Kiosk&&recoveredState.Books[0].CopyUid==books[0].CopyUid&&recoveredState.Borrowers[0].BorrowerUid==state.Borrowers[0].BorrowerUid,"complete backup keeps policy, permanent book and borrower identities");
   var recoveredView=J(recoveredCards.List(recoveredStore));Check(recoveredView.GetProperty("cards").EnumerateArray().Any(x=>x.GetProperty("revoked").GetBoolean()),"complete recovery retains revoked card records");
   Check(recoveredView.GetProperty("stations").EnumerateArray().All(x=>x.GetProperty("revoked").GetBoolean()),"complete recovery never reauthorizes kiosk stations");
  }Directory.Delete(destination,true);Directory.Delete(capture,true);
  using var restored=new PatronCards(directory,protection,true,clock);Reject(()=>restored.Resolve(store,replacementCode),"restored card requires explicit review");
  var view=J(restored.List(store));Check(view.GetProperty("cards").EnumerateArray().Where(x=>!x.GetProperty("revoked").GetBoolean()).All(x=>x.GetProperty("reviewRequired").GetBoolean()),"backup cannot silently reactivate cards");
  Check(!view.ToString().Contains("654321")&&!view.ToString().Contains("tokenHash")&&!File.ReadAllText(Path.Combine(directory,"patron-cards.sqlite")).Contains("654321"),"registry status and disk do not expose PIN/token secrets");
 }
}
