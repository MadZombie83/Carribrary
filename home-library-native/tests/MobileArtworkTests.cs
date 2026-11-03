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
  var layout=new BorrowerHeaderLayout{Portrait=new(){Fit="contain",X=15,Y=20,Overlay=80},Landscape=new(){Fit="cover",X=85,Y=75,Overlay=60}};
  var themeId=state.SavedThemes[0].Id;
  Library.Apply(state,"saveCustomTheme",Input(new{id=themeId,name="Both artworks",sidebarArtwork=main,borrowerArtwork=mobile,borrowerHeader=layout}));
  Check(state.LibraryBorrowerHeader.Portrait.X==15&&state.LibraryBorrowerHeader.Landscape.X==85,"Independent portrait and landscape header settings saved");
  var backup=JsonSerializer.Deserialize<LibraryState>(JsonSerializer.Serialize(state,Library.Json),Library.Json)!;Library.Validate(backup);
  Check(backup.LibraryBorrowerHeader.Landscape.Fit=="cover","Header fitting survives backup serialization");
  Library.Apply(state,"saveCustomTheme",Input(new{id=themeId,name="Both artworks",sidebarArtwork=main}));
  Check(state.LibraryBorrowerHeader.Portrait.X==15,"Older theme clients preserve omitted header settings");
  foreach(var invalid in new[]{new HeaderArtworkPosition{Fit="stretch"},new HeaderArtworkPosition{X=101},new HeaderArtworkPosition{Y=-1},new HeaderArtworkPosition{Overlay=0}}){
   try{BorrowerHeaderLayout.Validate(new(){Portrait=invalid});throw new Exception("Invalid header accepted");}catch(LibraryError){}
  }
  var detailedIcon=new byte[600000];new byte[]{137,80,78,71,13,10,26,10}.CopyTo(detailedIcon,0);BinaryPrimitives.WriteInt32BigEndian(detailedIcon.AsSpan(16,4),512);BinaryPrimitives.WriteInt32BigEndian(detailedIcon.AsSpan(20,4),512);
  var smallIcon=new byte[40];new byte[]{137,80,78,71,13,10,26,10}.CopyTo(smallIcon,0);BinaryPrimitives.WriteInt32BigEndian(smallIcon.AsSpan(16,4),192);BinaryPrimitives.WriteInt32BigEndian(smallIcon.AsSpan(20,4),192);
  LibraryAppearance.ValidateIcons(Convert.ToBase64String(smallIcon),Convert.ToBase64String(detailedIcon));Check(true,"Detailed resized icons above the former 350 KB cap accepted");
  Array.Resize(ref detailedIcon,2097153);try{LibraryAppearance.ValidateIcons(Convert.ToBase64String(smallIcon),Convert.ToBase64String(detailedIcon));throw new Exception("Oversized icon accepted");}catch(LibraryError){}
  var id=state.SavedThemes[0].Id;Library.Apply(state,"saveCustomTheme",Input(new{id,name="Both artworks",sidebarArtwork=main}));Check(state.LibraryBorrowerArtwork==mobile,"Older theme clients do not erase omitted mobile artwork");
  Library.Apply(state,"setLibraryTheme",Input(new{libraryTheme="forest"}));Check(state.LibraryBorrowerArtwork==""&&state.LibrarySidebarArtwork=="","Built-in presets clear both artwork choices");
  Library.Apply(state,"setLibraryTheme",Input(new{libraryTheme=key}));Check(state.LibraryBorrowerArtwork==mobile&&state.LibraryBorrowerHeader.Landscape.X==85,"Selecting saved theme restores artwork and orientation settings");
  Library.Apply(state,"setLibraryBorrowerArtwork",Input(new{artwork=""}));Check(state.SavedThemes[0].BorrowerArtwork=="","Removing mobile override updates the selected saved theme");
  try{LibraryAppearance.ValidateBorrowerArtwork(main);throw new Exception("Wrong image dimensions accepted");}catch(LibraryError){Console.WriteLine("PASS mobile artwork dimensions enforced on the server");}
 }
}
