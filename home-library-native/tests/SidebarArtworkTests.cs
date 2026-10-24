using HomeLibrary;
using System.Text.Json;
using System.Buffers.Binary;
using Microsoft.AspNetCore.DataProtection;
public static class SidebarArtworkTests {
 public static void Run(string directory,IDataProtectionProvider protection){
  using var store=new Store(directory,protection);
  if(store.Read().State.LibrarySidebarArtwork!="")throw new Exception("Artwork must default to none");
  void Reject(string value){try{LibraryAppearance.ValidateSidebarArtwork(value);throw new Exception("Invalid artwork accepted");}catch(LibraryError){}}
  Reject("<svg onload=alert(1)>");Reject(new string('x',480001));
  byte[] header=new byte[33];new byte[]{137,80,78,71,13,10,26,10}.CopyTo(header,0);BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(16,4),512);BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(20,4),256);
  var art=Convert.ToBase64String(header);LibraryAppearance.ValidateSidebarArtwork(art);
  store.Apply(new("setLibrarySidebarArtwork",JsonSerializer.SerializeToElement(new{artwork=art}),store.Read().Revision));
  var state=store.Read().State;store.Backup(state,"artwork");var restored=store.RestoreEncrypted(File.ReadAllBytes(Path.Combine(directory,"backups/artwork.hlbackup")));if(restored.LibrarySidebarArtwork!=art)throw new Exception("Artwork backup lost");
  BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(16,4),1024);Reject(Convert.ToBase64String(header));
  store.Apply(new("setLibrarySidebarArtwork",JsonSerializer.SerializeToElement(new{artwork=""}),store.Read().Revision));if(store.Read().State.LibrarySidebarArtwork!="")throw new Exception("Artwork removal failed");
  Console.WriteLine("PASS optional artwork defaults empty, validates type/size/dimensions, round-trips backup and removes");
 }
}
