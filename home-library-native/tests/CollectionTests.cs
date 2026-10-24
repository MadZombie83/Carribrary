using HomeLibrary;using Microsoft.AspNetCore.DataProtection;using System.Text.Json;
public static class CollectionTests {
 public static void Run(string directory,IDataProtectionProvider protection){
  void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS "+message);}
  using var store=new Store(directory,protection);
  object Do(string action,object input)=>store.Apply(new(action,JsonSerializer.SerializeToElement(input,Library.Json),store.Read().Revision));
  Do("addBook",new{isbn="9780140328721",title="Matilda",author="Roald Dahl"});
  Check(store.Read().State.Books[0].CollectionId=="general","legacy/new books have default collection");
  var result=JsonSerializer.SerializeToElement(Do("addCollection",new{name="Kids"}),Library.Json);var kids=result.GetProperty("result").GetProperty("id").GetString()!;
  Do("updateBook",new{copyId="BK-0001",isbn="9780140328721",title="Matilda",author="Roald Dahl",collectionId=kids});
  Do("addBorrower",new{name="Shared borrower"});Do("checkout",new{copyId="BK-0001",borrowerId="BR-0001",checkoutDate=Library.Today});
  Do("updateBook",new{copyId="BK-0001",isbn="9780140328721",title="Matilda",author="Roald Dahl"});
  Check(store.Read().State.Books[0].CollectionId==kids&&store.Read().State.Loans.Count==1,"older clients preserve collections and shared loans");
  Do("renameCollection",new{collectionId=kids,name="Children"});
  try{Do("removeCollection",new{collectionId=kids});throw new Exception("Deleted populated collection");}catch(LibraryError){}
  var before=store.Read().Revision;try{Do("updateBook",new{copyId="BK-0001",isbn="9780140328721",title="Matilda",author="Roald Dahl",collectionId="missing"});throw new Exception("Accepted unknown collection");}catch(LibraryError){}Check(store.Read().Revision==before,"invalid collection move rolls back");
  store.Backup(store.Read().State,"collections");var restored=store.RestoreEncrypted(File.ReadAllBytes(Path.Combine(directory,"backups/collections.hlbackup")));Check(restored.Collections.Count==2&&restored.Books[0].CollectionId==kids,"collections retained in encrypted backups");
  for(var i=0;i<105;i++)Do("addCollection",new{name="Collection "+i});Check(store.Read().State.Collections.Count==107,"no small fixed collection limit");
  Do("updateBook",new{copyId="BK-0001",isbn="9780140328721",title="Matilda",author="Roald Dahl",collectionId="general"});Do("removeCollection",new{collectionId=kids});
  var keySettings=new CoverSettings(directory,protection);keySettings.Save("test-google-key-not-a-real-credential");
  Check(!File.ReadAllText(Path.Combine(directory,"google-books-key.protected")).Contains("test-google-key"),"Google key protected at rest");
  Check(new CoverSettings(directory,protection).Key==keySettings.Key&&!JsonSerializer.Serialize(keySettings.Status()).Contains(keySettings.Key),"Google settings survive restart without exposing key");
  keySettings.Save("");Check(keySettings.Key=="","Google lookup can be disabled");
 }
}
