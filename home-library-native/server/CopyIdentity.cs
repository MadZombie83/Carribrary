namespace HomeLibrary;
// Display catalog numbers may be reused. CopyUid identifies the physical record permanently.
public static class CopyIdentity {
 public static bool Migrate(LibraryState state){
  Library.Validate(state);
  if(state.Version is 3 or 4 or 5 or 6){Validate(state);return false;}
  if(state.Version is not (1 or 2))throw new LibraryError("Unsupported library format. Use a compatible Carribrary version.");
  foreach(var book in state.Books)if(book.CopyUid=="")book.CopyUid=Guid.NewGuid().ToString("N");
  foreach(var item in state.RecycleBin.Where(x=>x.Book!=null)){
   var book=item.Book!;
   var archived=state.Books.Find(x=>item.PreservedForHistory&&x.Archived&&x.Id==book.Id);
   if(book.CopyUid=="")book.CopyUid=archived?.CopyUid??Guid.NewGuid().ToString("N");
  }
  state.Version=3;Validate(state);return true;
 }
 public static void Validate(LibraryState state){
  if(state.Version==1)return; // Legacy input is migrated explicitly before saving.
  bool Valid(string value)=>Guid.TryParseExact(value,"N",out var id)&&id!=Guid.Empty&&value==id.ToString("N");
  if(state.Books.Any(b=>!Valid(b.CopyUid))||state.Books.Select(b=>b.CopyUid).Distinct().Count()!=state.Books.Count)throw new LibraryError("Invalid or duplicate permanent copy identities.");
  if(state.RecycleBin==null||state.RecycleBin.Any(x=>x==null))throw new LibraryError("Invalid recycle bin.");
  var used=state.Books.ToDictionary(b=>b.CopyUid);
  var deleted=new HashSet<string>();
  foreach(var item in state.RecycleBin.Where(x=>x.Book!=null)){
   var book=item.Book!;
   var archived=state.Books.Find(x=>item.PreservedForHistory&&x.Archived&&x.Id==book.Id);
   if(archived!=null&&archived.CopyUid!=book.CopyUid)throw new LibraryError("Deleted copy identity does not match its archived record.");
   if(!Valid(book.CopyUid)||!deleted.Add(book.CopyUid))throw new LibraryError("Invalid deleted copy identity.");
   if(used.TryGetValue(book.CopyUid,out var current)&&!(item.PreservedForHistory&&current.Archived&&current.Id==book.Id))throw new LibraryError("A deleted copy identity conflicts with a current copy.");
  }
 }
}
