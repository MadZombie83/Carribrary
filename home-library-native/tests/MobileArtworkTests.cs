using HomeLibrary;using System.Text.Json;using System.Buffers.Binary;
public static class MobileArtworkTests {
 public static void Run(){
  string Png(int height){var bytes=new byte[40];new byte[]{137,80,78,71,13,10,26,10}.CopyTo(bytes,0);BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(16,4),512);BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(20,4),height);return Convert.ToBase64String(bytes);}
  void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS "+message);}
  var state=new LibraryState();var main=Png(1536);var mobile=Png(1024);
  JsonElement Input(object o)=>JsonSerializer.SerializeToElement(o,Library.Json);
  Library.Apply(state,"saveCustomTheme",Input(new{name="Both artworks",sidebarArtwork=main,borrowerArtwork=mobile}));
  var key=state.LibraryTheme;Check(state.LibraryBorrowerArtwork==mobile&&state.LibrarySidebarArtwork==main,"Saved theme keeps independent main and mobile artwork");
  var restored=JsonSerializer.Deserialize<LibraryState>(JsonSerializer.Serialize(state,Library.Json),Library.Json)!;Library.Validate(restored);Check(restored.LibraryBorrowerArtwork==mobile,"Library serialization/restore preserves mobile branding");
  var id=state.SavedThemes[0].Id;Library.Apply(state,"saveCustomTheme",Input(new{id,name="Both artworks",sidebarArtwork=main}));Check(state.LibraryBorrowerArtwork==mobile,"Older theme clients do not erase omitted mobile artwork");
  Library.Apply(state,"setLibraryTheme",Input(new{libraryTheme="forest"}));Check(state.LibraryBorrowerArtwork==""&&state.LibrarySidebarArtwork=="","Built-in presets clear both artwork choices");
  Library.Apply(state,"setLibraryTheme",Input(new{libraryTheme=key}));Check(state.LibraryBorrowerArtwork==mobile,"Selecting saved theme restores mobile artwork");
  Library.Apply(state,"setLibraryBorrowerArtwork",Input(new{artwork=""}));Check(state.SavedThemes[0].BorrowerArtwork=="","Removing mobile override updates the selected saved theme");
  try{LibraryAppearance.ValidateBorrowerArtwork(main);throw new Exception("Wrong image dimensions accepted");}catch(LibraryError){Console.WriteLine("PASS mobile artwork dimensions enforced on the server");}
 }
}
