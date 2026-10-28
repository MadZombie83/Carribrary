using HomeLibrary;
using Microsoft.AspNetCore.DataProtection;
using System.IO.Compression;
public static class PurchaseSuggestionTests {
 public static void Run(string directory,IDataProtectionProvider protection){
  Directory.CreateDirectory(directory);void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);}
  void Reject(Action action,string name){try{action();throw new Exception("Not rejected: "+name);}catch(LibraryError){Console.WriteLine("PASS "+name);}}
  using var store=new Store(directory,protection);var id=Guid.NewGuid().ToString();var input=new PurchaseSuggestionInput(id,"series","Secret test series","Test Author","","Please consider these books.");
  using(var box=new PurchaseSuggestions(directory,protection)){
   box.Submit(input);box.Submit(input);Check(box.List().Count==1&&box.List()[0].Isbn=="","series without ISBN accepted; retry does not duplicate");
   Reject(()=>box.Submit(input with{Title="Changed retry"}),"request ID cannot be reused for changed data");
   Reject(()=>box.Submit(input with{RequestId=Guid.NewGuid().ToString(),Isbn="123"}),"invalid optional ISBN rejected");
   Reject(()=>box.Submit(input with{RequestId=Guid.NewGuid().ToString(),Kind="other"}),"invalid suggestion type rejected");
   Reject(()=>box.Submit(input with{RequestId=Guid.NewGuid().ToString(),Notes=new string('x',1501)}),"oversized public notes rejected");
   box.Decide(new(id,"considering","pending"));Reject(()=>box.Decide(new(id,"declined","pending")),"stale decision conflicts instead of overriding another admin");
   Check(box.List()[0].Status=="considering"&&store.Read().State.Books.Count==0&&store.Read().State.BooksToBuy.Count==0,"considering does not buy, reserve, create copies or change acquisition wishlist");
  }
  using(var box=new PurchaseSuggestions(directory,protection)){
   Check(box.List().Single().Title==input.Title,"protected purchase suggestion survives restart");
   var stage=CompleteBackup.Capture(directory,new Settings(),default);using(var zip=ZipFile.OpenRead(Path.Combine(stage,"snapshot.zip")))Check(zip.GetEntry("data/purchase-suggestions.sqlite")!=null,"complete backup includes consistent suggestion database");Directory.Delete(stage,true);
   box.Decide(new(id,"delete","considering"));box.Submit(input);Check(box.List().Count==0,"deleted suggestion is not recreated by a delayed identical retry");
  }
  Check(!System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(directory,"purchase-suggestions.sqlite"))).Contains(input.Title),"suggestion text is encrypted at rest");
 }
}
