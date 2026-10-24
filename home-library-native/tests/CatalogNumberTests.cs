using HomeLibrary;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;
public static class CatalogNumberTests {
 public static Task Run(string directory,IDataProtectionProvider protection){
  JsonElement Input(object value)=>JsonSerializer.SerializeToElement(value,Library.Json);
  void Check(bool condition,string name){if(!condition)throw new Exception("Catalog reuse test failed: "+name);Console.WriteLine("PASS "+name);}
  using(var store=new Store(directory,protection)){
   JsonElement Change(string action,object input,string? requestId=null)=>Input(store.Apply(new(action,Input(input),store.Read().Revision,requestId)));
   string Add(string title,string? requestId=null)=>Change("addBook",new{isbn="9780140328721",title,author="Author",confirmedDuplicates=store.Read().State.Books.Where(b=>!b.Archived).Select(b=>b.Id).ToArray()},requestId).GetProperty("result").GetProperty("id").GetString()!;
   Check(Add("First")=="BK-0001"&&Add("Second")=="BK-0002"&&Add("Third")=="BK-0003","initial sequence");
   Change("removeBook",new{copyId="BK-0002"});Change("removeBook",new{copyId="BK-0001"});
   Check(Add("Replacement one")=="BK-0001"&&Add("Replacement two")=="BK-0002","lowest deleted numbers reused first");
   var request=new PublicBookRequest("BK-0001","Visitor","v@example.com","",Guid.NewGuid().ToString(),"9780140328721","First");
   try{store.SubmitRequest(request);throw new Exception("Stale request accepted");}catch(LibraryError error){Check(error.Status==409,"stale shared page cannot request a different replacement book");}
   store.SubmitRequest(request with{RequestId=Guid.NewGuid().ToString(),ExpectedTitle="Replacement one"});
   Change("declineRequest",new{requestId=store.Read().State.Requests.Single().Id});
   Change("removeBook",new{copyId="BK-0001"});
   Change("addBorrower",new{name="Borrower"});Change("checkout",new{copyId="BK-0002",borrowerId="BR-0001",checkoutDate=Library.Today});Change("returnBook",new{copyId="BK-0002"});Change("removeBook",new{copyId="BK-0002"});
   Check(Add("Fourth")=="BK-0004","request and loan history numbers stay retired");
   var state=store.Read().State;state.NextCopy=500;Change("restore",state);
   Change("removeBook",new{copyId="BK-0003"});
   var id=Guid.NewGuid().ToString();var input=new{isbn="9780140328721",title="Reuse after restore",author="Author",confirmedDuplicates=store.Read().State.Books.Where(b=>!b.Archived).Select(b=>b.Id).ToArray()};
   var action=new ActionRequest("addBook",Input(input),store.Read().Revision,id);
   store.Apply(action);store.Apply(action);
   Check(store.Read().State.Books.Single(b=>b.Title=="Reuse after restore").Id=="BK-0003","restored high counter does not block free number; retries allocate once");
   Check(store.Read().State.Loans.Single().CopyId=="BK-0002"&&store.Read().State.Requests.Single().CopyId=="BK-0001","historical references preserved");
  }
  using(var reopened=new Store(directory,protection)){Check(reopened.Read().State.Books.Count(b=>b.Id=="BK-0003")==1,"reused IDs persist after restart");}
  var guarded=new LibraryState {Books=[new Book{Id="BK-00001"}],Loans=[new Loan{CopyId="BK-0002"}],Requests=[new BookRequest{CopyId="BK-0003"}]};
  Check(Library.AvailableCopy(guarded)==4,"occupied numbers protected across records and alternate padding");
  return Task.CompletedTask;
 }
}
