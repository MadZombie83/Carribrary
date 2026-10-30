using HomeLibrary;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;
public static class CopyIdentityTests {
 public static void Run(string directory,IDataProtectionProvider protection){
  JsonElement Input(object x)=>JsonSerializer.SerializeToElement(x,Library.Json);
  void Check(bool ok,string name){if(!ok)throw new Exception("Copy identity: "+name);Console.WriteLine("PASS "+name);}
  string uid;
  using(var store=new Store(directory,protection)){
   JsonElement Change(string action,object x)=>Input(store.Apply(new(action,Input(x),store.Read().Revision))).GetProperty("result");
   Book Book()=>store.Read().State.Books.Single();
   var result=Change("addBook",new{isbn="9780140328721",title="Original",author="Author",copyUid=new string('0',32)});uid=Book().CopyUid;
   Check(store.Read().State.Version==7&&Guid.TryParseExact(uid,"N",out _),"fresh libraries persist permanent identities in format 7");
   Check(uid!=new string('0',32),"clients cannot choose copy identities");
   Change("updateBook",new{copyId=Book().Id,isbn="9780140328721",title="Edited",author="Author",copyUid=Guid.NewGuid().ToString("N")});
   Check(Book().CopyUid==uid,"editing preserves immutable identity");
   Change("removeBook",new{copyId=Book().Id});var deleted=store.Read().State.RecycleBin.Single().Id;
   Change("addBook",new{isbn="9780140328721",title="Replacement",author="Author"});
   var replacement=Book();Check(replacement.Id=="BK-0001"&&replacement.CopyUid!=uid,"reused catalog number gets a distinct permanent identity");
   Change("restoreDeleted",new{deletedId=deleted});
   Check(store.Read().State.Books.Single(b=>b.Title=="Edited").CopyUid==uid&&store.Read().State.Books.Single(b=>b.Title=="Edited").Id!="BK-0001","Recycle Bin restoration preserves identity when display number changes");
   var backup=store.Read().State;Change("restore",backup);
   Check(store.Read().State.Books.Single(b=>b.Title=="Edited").CopyUid==uid,"current backup restore preserves permanent identities");
   backup.Books[1].CopyUid=backup.Books[0].CopyUid;
   try{Change("restore",backup);throw new Exception("Duplicate identity accepted");}catch(LibraryError){Check(store.Read().State.Books.Select(b=>b.CopyUid).Distinct().Count()==2,"invalid identity backup rolls back atomically");}
  }
  using(var reopened=new Store(directory,protection))Check(reopened.Read().State.Books.Single(b=>b.Title=="Edited").CopyUid==uid,"identities survive restart");
  var legacy=new LibraryState{Books=[new(){Id="BK-0001",Isbn="9780140328721",Title="Old",Archived=true}],RecycleBin=[new(){Id=Guid.NewGuid().ToString("N"),DeletedAt=DateTimeOffset.UtcNow.ToString("O"),PreservedForHistory=true,Book=new(){Id="BK-0001",Isbn="9780140328721",Title="Old"}}]};
  var oldDirectory=Path.Combine(directory,"legacy-server");Directory.CreateDirectory(oldDirectory);
  using(var connection=new SqliteConnection($"Data Source={Path.Combine(oldDirectory,"library.sqlite")}")){
   connection.Open();using var command=connection.CreateCommand();command.CommandText="CREATE TABLE library(id INTEGER PRIMARY KEY,revision INTEGER NOT NULL,data TEXT NOT NULL); INSERT INTO library VALUES(1,7,$data)";command.Parameters.AddWithValue("$data",JsonSerializer.Serialize(legacy,Library.Json));command.ExecuteNonQuery();
  }
  string migrated;
  using(var store=new Store(oldDirectory,protection)){
   var state=store.Read().State;migrated=state.Books[0].CopyUid;
   Check(state.Version==7&&state.Books[0].Id=="BK-0001"&&state.RecycleBin[0].Book!.CopyUid==migrated,"stored legacy server migration preserves display and recycle references");
   var backup=Directory.GetFiles(Path.Combine(oldDirectory,"backups"),"before-copy-identity-v2-*.hlbackup").Single();
   Check(store.RestoreEncrypted(File.ReadAllBytes(backup)).Version==1,"migration creates a private pre-change backup");
  }
  using(var store=new Store(oldDirectory,protection))Check(store.Read().State.Books[0].CopyUid==migrated,"migrated identity is persisted, not regenerated on read or restart");
  Check(CopyIdentity.Migrate(legacy),"legacy data migrates explicitly");var stable=legacy.Books[0].CopyUid;
  Check(legacy.RecycleBin[0].Book!.CopyUid==stable&&!CopyIdentity.Migrate(legacy),"legacy archived copies and recovery records share one stable identity");
 }
}
