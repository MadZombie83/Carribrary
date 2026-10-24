using System.Text.Json;
namespace HomeLibrary;
public static class Recovery {
 public static void Validate(LibraryState s){
  if(s.RecycleBin==null||s.RecycleBin.Any(x=>x==null)||s.RecycleBin.Select(x=>x.Id).Distinct().Count()!=s.RecycleBin.Count)throw new LibraryError("Invalid recycle bin.");
  foreach(var item in s.RecycleBin){
   if(!Guid.TryParse(item.Id,out _)||!DateTimeOffset.TryParse(item.DeletedAt,out _)||(item.Book==null)==(item.Borrower==null))throw new LibraryError("Invalid deleted record.");
   var probe=new LibraryState{Collections=s.Collections};if(item.Book!=null){var b=JsonSerializer.Deserialize<Book>(JsonSerializer.Serialize(item.Book,Library.Json),Library.Json)!;b.CollectionId="general";probe.Books.Add(b);}else probe.Borrowers.Add(item.Borrower!);
   Library.Validate(probe);
  }
 }
 static string Field(JsonElement e,string key)=>e.TryGetProperty(key,out var x)&&x.ValueKind==JsonValueKind.String?x.GetString()??"":"";
 public static object Apply(LibraryState s,string action,JsonElement input){
  if(action=="restoreBorrower"){
   var borrower=s.Borrowers.Find(b=>b.Id==Field(input,"borrowerId")&&b.Archived)??throw new LibraryError("Deleted borrower not found.");borrower.Archived=false;borrower.ReminderConsent=false;s.RecycleBin.RemoveAll(x=>x.PreservedForHistory&&x.Borrower?.Id==borrower.Id);return borrower;
  }
  if(action=="bulkEditBooks"){
   if(!input.TryGetProperty("copyIds",out var ids)||ids.ValueKind!=JsonValueKind.Array)throw new LibraryError("Select books first.");
   var selected=ids.EnumerateArray().Select(x=>x.ValueKind==JsonValueKind.String?x.GetString():null).ToArray();
   if(selected.Length is <1 or >500||selected.Any(x=>x==null)||selected.Distinct().Count()!=selected.Length)throw new LibraryError("Select between 1 and 500 distinct copies.");
   var books=selected.Select(id=>s.Books.Find(b=>b.Id==id&&!b.Archived)??throw new LibraryError("A selected book is no longer available. Refresh and review your selection.")).ToList();
   if(!input.TryGetProperty("changes",out var changes)||changes.ValueKind!=JsonValueKind.Object||!changes.EnumerateObject().Any())throw new LibraryError("Choose at least one field to change.");
   var fields=new Dictionary<string,string>();foreach(var field in changes.EnumerateObject()){
    if(field.Name is not ("author" or "publisher" or "series" or "collectionId")||field.Value.ValueKind!=JsonValueKind.String||fields.ContainsKey(field.Name))throw new LibraryError("Choose author, publisher, series or collection only.");
    var value=field.Value.GetString()!.Trim();if(value.Length>500||value.Any(char.IsControl))throw new LibraryError("Fields must be plain text of at most 500 characters.");fields.Add(field.Name,value);
   }
   if(fields.TryGetValue("collectionId",out var collection)&&!s.Collections.Any(c=>c.Id==collection))throw new LibraryError("Choose an existing collection.");
   foreach(var book in books)foreach(var (field,value) in fields)switch(field){case "author":book.Author=value;break;case "publisher":book.Publisher=value;break;case "series":book.Series=value;break;case "collectionId":book.CollectionId=value;break;}
   return new{updated=books.Count,copyIds=selected};
  }
  var record=s.RecycleBin.Find(x=>x.Id==Field(input,"deletedId"))??throw new LibraryError("This record is no longer in the Recycle Bin.");
  if(action=="purgeDeleted"){s.RecycleBin.Remove(record);return new{purged=true};} // Referenced archive records remain for history.
  string previousId,newId;
  if(record.Book is {} original){
   previousId=original.Id;var archived=s.Books.Find(b=>record.PreservedForHistory&&b.Id==original.Id&&b.Archived);
   if(archived!=null){archived.Archived=false;newId=archived.Id;}
   else {var book=JsonSerializer.Deserialize<Book>(JsonSerializer.Serialize(original,Library.Json),Library.Json)!;book.Archived=false;if(s.Books.Any(b=>b.Id==book.Id))book.Id="BK-"+Library.AvailableCopy(s).ToString("D4");if(!s.Collections.Any(c=>c.Id==book.CollectionId))book.CollectionId="general";s.Books.Add(book);if(int.TryParse(book.Id[3..],out var allocated))s.NextCopy=Math.Max(s.NextCopy,(long)allocated+1);newId=book.Id;}
  }else{
   var originalBorrower=record.Borrower!;previousId=originalBorrower.Id;var archived=s.Borrowers.Find(b=>record.PreservedForHistory&&b.Id==previousId&&b.Archived);
   if(archived!=null){archived.Archived=false;archived.ReminderConsent=false;newId=archived.Id;}
   else {var borrower=JsonSerializer.Deserialize<Borrower>(JsonSerializer.Serialize(originalBorrower,Library.Json),Library.Json)!;borrower.Archived=false;borrower.ReminderConsent=false;if(s.Borrowers.Any(b=>b.Id==borrower.Id))borrower.Id="BR-"+(s.Borrowers.Select(b=>int.Parse(b.Id[3..])).DefaultIfEmpty(0).Max()+1).ToString("D4");s.Borrowers.Add(borrower);newId=borrower.Id;}
  }
  s.RecycleBin.Remove(record);return new{restored=true,previousId,id=newId};
 }
}
