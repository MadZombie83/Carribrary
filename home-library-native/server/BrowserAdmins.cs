using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
namespace HomeLibrary;
public sealed record BrowserGrant(string Hash,DateTimeOffset Expires);
public sealed record BrowserAdmin(string Id,string Name,string Hash,DateTimeOffset Issued,DateTimeOffset Expires,bool Revoked=false);
public sealed class BrowserAdminState {public List<BrowserGrant> Grants{get;set;}=[];public List<BrowserAdmin> Devices{get;set;}=[];}
public sealed class BrowserAdmins {
 readonly object gate=new();readonly string file,origin;readonly IDataProtector protector;BrowserAdminState state;
 public const string Cookie="__Secure-hl-admin";
 static string Secret()=>Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');
 static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
 public BrowserAdmins(string directory,string origin,IDataProtectionProvider provider){file=Path.Combine(directory,"browser-admins.protected");this.origin=origin.TrimEnd('/');protector=provider.CreateProtector("HomeLibrary.BrowserAdmins.v1");state=File.Exists(file)?JsonSerializer.Deserialize<BrowserAdminState>(protector.Unprotect(File.ReadAllBytes(file)),Library.Json)??new():new();}
 void Save(){File.WriteAllBytes(file+".tmp",protector.Protect(JsonSerializer.SerializeToUtf8Bytes(state,Library.Json)));if(!OperatingSystem.IsWindows())File.SetUnixFileMode(file+".tmp",UnixFileMode.UserRead|UnixFileMode.UserWrite);File.Move(file+".tmp",file,true);}
 public object Pair(){lock(gate){if(origin=="")throw new LibraryError("Configure public HTTPS in Connection setup first.");state.Grants.RemoveAll(g=>g.Expires<DateTimeOffset.UtcNow);if(state.Grants.Count>=5)state.Grants.RemoveAt(0);var token=Secret();var expires=DateTimeOffset.UtcNow.AddMinutes(10);state.Grants.Add(new(Hash(token),expires));Save();return new{url=origin+"/companion/#pair="+token,code=token,expires};}}
 public (BrowserAdmin Device,string Token) Enroll(string code,string name){lock(gate){if(code.Length!=43)throw new LibraryError("Paste the pairing code or link from the library server.");var grant=state.Grants.Find(g=>g.Hash==Hash(code)&&g.Expires>DateTimeOffset.UtcNow)??throw new LibraryError("This pairing code expired or was already used. Create a new code on the server.",401);name=name.Trim();if(name.Length is <1 or >80||name.Any(char.IsControl))throw new LibraryError("Give this iPhone/browser a name (up to 80 characters).");if(state.Devices.Count(d=>!d.Revoked&&d.Expires>DateTimeOffset.UtcNow)>=50)throw new LibraryError("Revoke an unused browser first (50 active browsers maximum).");state.Grants.Remove(grant);var token=Secret();var d=new BrowserAdmin(Guid.NewGuid().ToString("N"),name,Hash(token),DateTimeOffset.UtcNow,DateTimeOffset.UtcNow.AddDays(30));state.Devices.Add(d);Save();return(d,d.Id+"."+token);}}
 public BrowserAdmin? Resolve(string token){lock(gate){if(token.Length!=76)return null;var parts=token.Split('.');if(parts.Length!=2)return null;return state.Devices.Find(d=>d.Id==parts[0]&&!d.Revoked&&d.Expires>DateTimeOffset.UtcNow&&CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(d.Hash),Encoding.ASCII.GetBytes(Hash(parts[1]))));}}
 public BrowserAdmin? Resolve(HttpContext c){var key=c.Request.Cookies.Keys.FirstOrDefault(k=>k.Equals(Cookie,StringComparison.Ordinal));return Resolve(key==null?"":c.Request.Cookies[key]??"");}
 public bool Active(string id){lock(gate)return state.Devices.Any(d=>d.Id==id&&!d.Revoked&&d.Expires>DateTimeOffset.UtcNow);}
 public object[] List(){lock(gate)return state.Devices.Select(d=>(object)new{d.Id,d.Name,d.Issued,d.Expires,revoked=d.Revoked||d.Expires<=DateTimeOffset.UtcNow}).ToArray();}
 public void Revoke(string id){lock(gate){var i=state.Devices.FindIndex(d=>d.Id==id);if(i<0)throw new LibraryError("Browser not found.",404);state.Devices[i]=state.Devices[i] with{Revoked=true};Save();}}
}
