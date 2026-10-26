using HomeLibrary;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using System.Text.Json;
public static class CopyPolicyTests {
 public static void Run(string directory,IDataProtectionProvider protection){
  void Check(bool ok,string title){if(!ok)throw new Exception(title);Console.WriteLine("PASS "+title);}
  JsonElement Input(object x)=>JsonSerializer.SerializeToElement(x,Library.Json);
  using(var store=new Store(directory,protection)){
   JsonElement Change(string action,object input)=>Input(store.Apply(new(action,Input(input),store.Read().Revision))).GetProperty("result");
   string Add(string title)=>Change("addBook",new{isbn="9780140328721",title,author="Author",confirmedDuplicates=store.Read().State.Books.Select(b=>b.Id).ToArray()}).GetProperty("id").GetString()!;
   Check(Add("One")=="BK-0001"&&Add("Two")=="BK-0002","default allocator preserves initial sequence");
   var uid=store.Read().State.Books[0].CopyUid;Change("removeBook",new{copyId="BK-0001"});
   Change("setCopyNumberPolicy",new{policy="next-number"});Check(Add("Three")=="BK-0003","next-number policy leaves deleted gaps");
   Change("setCopyNumberPolicy",new{policy="fill-gaps"});Check(Add("Replacement")=="BK-0001"&&store.Read().State.Books.Find(b=>b.Id=="BK-0001")!.CopyUid!=uid,"policy changes only future allocation and replacement identity");
   Change("removeBook",new{copyId="BK-0003"});Change("setCopyNumberPolicy",new{policy="next-number"});Check(Add("Four")=="BK-0004","switching back uses retained high-water mark");
   var retired=store.Read().State.RecycleBin.First(r=>r.Book?.Title=="One").Id;
   Change("restoreDeleted",new{deletedId=retired});var recovered=store.Read().State.Books.Single(b=>b.Title=="One");
   Check(recovered.Id=="BK-0005","conflicting recovery uses forward allocator");Change("removeBook",new{copyId=recovered.Id});
   Check(Add("Recovery successor")=="BK-0006","deleting a newly numbered restored copy cannot rewind forward allocator");
   var before=store.Read();try{Change("setCopyNumberPolicy",new{policy="bogus"});throw new Exception("invalid policy accepted");}catch(LibraryError){Check(store.Read().Revision==before.Revision,"invalid allocation policy rolls back atomically");}
   var backup=store.Read().State;Change("restore",backup);Check(store.Read().State.CopyNumberPolicy=="next-number"&&Add("Seven")=="BK-0007","allocation choice and high-water mark survive backup restore");
  }
  using(var store=new Store(directory,protection))Check(store.Read().State.CopyNumberPolicy=="next-number"&&Library.AvailableCopy(store.Read().State)==8,"allocation choice persists after restart");
  var dir=Path.Combine(directory,"format2");Directory.CreateDirectory(dir);var book=new Book{Id="BK-0007",CopyUid=Guid.NewGuid().ToString("N"),Isbn="9780140328721",Title="Existing"};
  using(var db=new SqliteConnection($"Data Source={Path.Combine(dir,"library.sqlite")}")){db.Open();using var cmd=db.CreateCommand();cmd.CommandText="CREATE TABLE library(id INTEGER PRIMARY KEY,revision INTEGER NOT NULL,data TEXT NOT NULL);INSERT INTO library VALUES(1,12,$data)";cmd.Parameters.AddWithValue("$data",JsonSerializer.Serialize(new LibraryState{Version=2,CirculationVersion=1,NextCopy=8,Books=[book]},Library.Json));cmd.ExecuteNonQuery();}
  using(var store=new Store(dir,protection)){var migrated=store.Read();Check(migrated.Revision==13&&migrated.State.Version==4&&migrated.State.CopyNumberPolicy=="fill-gaps"&&migrated.State.Books[0].CopyUid==book.CopyUid,"format-2 upgrade keeps existing identity and allocation preference");Check(Directory.GetFiles(Path.Combine(dir,"backups"),"before-copy-identity-v3-*.hlbackup").Length==1,"format upgrade saves a private pre-change backup");}
  var limit=new LibraryState{CopyNumberPolicy="next-number",NextCopy=int.MaxValue,Books=[]};
  Check(Library.AvailableCopy(limit)==int.MaxValue,"last supported catalog number can be allocated");limit.NextCopy=(long)int.MaxValue+1;
  try{Library.AvailableCopy(limit);throw new Exception("allocator overflow");}catch(LibraryError){Check(true,"exhausted high-water counter cannot wrap or reassign a retired number");}

 }
}
