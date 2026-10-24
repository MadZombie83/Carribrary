using System.Text.Json;using HomeLibrary;using Microsoft.AspNetCore.DataProtection;
static class AcquisitionTests {
 public static void Run(string directory,IDataProtectionProvider protection){using var store=new Store(directory,protection);JsonElement Input(object x)=>JsonSerializer.SerializeToElement(x,Library.Json);JsonElement Change(string action,object x)=>Input(store.Apply(new(action,Input(x),store.Read().Revision)));
  void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);}void Reject(string action,object value){var before=JsonSerializer.Serialize(store.Read(),Library.Json);try{Change(action,value);throw new Exception("Expected rejection");}catch(LibraryError){}Check(JsonSerializer.Serialize(store.Read(),Library.Json)==before,"failed acquisition is atomic");}
  var wish=new{isbn="9780140328721",title="Matilda",author="Roald Dahl",notes="PRIVATE ACQUISITION NOTE"};
  var result=Change("saveAcquisition",wish);var id=result.GetProperty("result").GetProperty("id").GetString()!;
  Check(store.Read().State.Books.Count==0&&store.Read().State.NextCopy==1,"wishlist does not allocate a physical copy or catalog number");
  Reject("saveAcquisition",wish);Reject("acquireBook",new{id});
  var copy=Change("acquireBook",new{id,purchaseConfirmed=true,room="Bedroom",bookcase="West",shelf="3"}).GetProperty("result");
  Check(copy.GetProperty("id").GetString()=="BK-0001"&&store.Read().State.BooksToBuy.Count==0,"confirmed acquisition adds copy and removes wishlist entry together");
  Check(store.Read().State.Books.Single().Shelf=="3","private room/bookcase/shelf saved on exact copy");
  Change("updateBook",new{copyId="BK-0001",isbn=wish.isbn,title="Matilda",author=wish.author});Check(store.Read().State.Books.Single().Room=="Bedroom","older clients preserve omitted shelf fields on edit");
  id=Change("saveAcquisition",wish).GetProperty("result").GetProperty("id").GetString()!;Reject("acquireBook",new{id,purchaseConfirmed=true});
  Check(store.Read().State.BooksToBuy.Count==1&&store.Read().State.Books.Count==1,"duplicate acquisition requires explicit physical-copy confirmation and keeps wishlist on failure");
  Change("acquireBook",new{id,purchaseConfirmed=true,confirmedDuplicates=new[]{"BK-0001"}});Check(store.Read().State.Books.Count==2,"confirmed duplicate acquisition creates a distinct physical copy");
  var publicJson=JsonSerializer.Serialize(Sharing.Books(store.Read().State),Library.Json);Check(!publicJson.Contains("Bedroom")&&!publicJson.Contains("room")&&!publicJson.Contains("bookcase")&&!publicJson.Contains("shelf")&&!publicJson.Contains("ACQUISITION"),"public catalog never exposes shelf locations or acquisition notes");
  Change("saveAcquisition",new{isbn="9780547928227",title="The Hobbit",author="J. R. R. Tolkien"});var backup=JsonSerializer.Serialize(store.Read().State,Library.Json);Change("restore",store.Read().State);Check(JsonSerializer.Serialize(store.Read().State,Library.Json)==backup,"backup restore preserves acquisitions and locations");
 }
}
