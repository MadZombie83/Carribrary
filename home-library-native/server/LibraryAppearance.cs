namespace HomeLibrary;
public static class LibraryAppearance {
 public static void ValidateSidebarArtwork(string value){
  if(value=="")return;
  try{if(value==null||value.Length>480000)throw new FormatException();var b=Convert.FromBase64String(value);if(b.Length<33||b.Length>350000||!b.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10})||System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(b.AsSpan(16,4))!=512||System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(b.AsSpan(20,4))!=256)throw new FormatException();}
  catch(FormatException){throw new LibraryError("Choose sidebar artwork using the PNG upload (512 × 256 pixels after fitting, maximum 350 KB).");}
 }
 public static void ValidateIcons(string small,string large){if(small==""&&large=="")return;if(!Icon(small,192)||!Icon(large,512))throw new LibraryError("Choose an image using Library icon. Both sized PNG icons are required (maximum 350 KB each).");}
 static bool Icon(string value,int size){try{if(value==null||value.Length>480000)return false;var b=Convert.FromBase64String(value);return b.Length>32&&b.Length<=350000&&b.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10})&&System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(b.AsSpan(16,4))==size&&System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(b.AsSpan(20,4))==size;}catch(FormatException){return false;}}
 public static IResult IconFile(Store store,int size,string root){var s=store.Read().State;var data=size==192?s.LibraryIcon192:s.LibraryIcon512;return data==""?Results.File(Path.Combine(root,"wwwroot",$"borrower-icon-{size}.png"),"image/png"):Results.Bytes(Convert.FromBase64String(data),"image/png");}
 public static bool Valid(string theme)=>CustomThemes.IsCustom(theme)||theme is "" or "classic" or "purple-blue" or "forest" or "rose" or "ocean";
 public static string Theme(LibraryState s)=>s.LibraryTheme==""?"purple-blue":s.LibraryTheme;
 public static string Ink(LibraryState s)=>CustomThemes.Selected(s)?.Ink??(Theme(s) switch{"classic"=>"#153e46","forest"=>"#244d36","rose"=>"#683a58","ocean"=>"#174b73",_=>"#493276"});
 public static string Page(LibraryState s)=>CustomThemes.Selected(s)?.Page??(Theme(s) switch{"classic"=>"#f3f5f5","forest"=>"#f0f5ef","rose"=>"#fcf2f6","ocean"=>"#eff6fb",_=>"#f2f1fc"});
}
