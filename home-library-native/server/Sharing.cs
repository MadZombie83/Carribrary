using System.Security.Cryptography;
using System.Text.Json;
namespace HomeLibrary;
public sealed class Sharing {
 readonly object gate=new();readonly string file;readonly string origin;ShareConfig config;
 sealed record ShareConfig(bool Enabled=false,string Token="");
 public Sharing(string directory,string origin){file=Path.Combine(directory,"sharing-native.json");this.origin=origin.TrimEnd('/');if(origin!=""&&(!Uri.TryCreate(origin,UriKind.Absolute,out var u)||u.Scheme!="https"||u.AbsolutePath!="/"||u.UserInfo!=""||u.Query!=""||u.Fragment!=""))throw new LibraryError("PublicCatalogOrigin must be an HTTPS origin.");config=File.Exists(file)?JsonSerializer.Deserialize<ShareConfig>(File.ReadAllText(file),Library.Json)??new():new();}
 public object Status(){lock(gate)return new{configured=origin!="",enabled=origin!=""&&config.Enabled,url=origin!=""&&config.Enabled?origin+"/s/"+config.Token+"/":""};}
 public object Change(string action){lock(gate){if(action is not ("enable" or "replace" or "disable"))throw new LibraryError("Unknown sharing action.");if(action!="disable"&&origin=="")throw new LibraryError("Configure the public catalog HTTPS hostname first; see START-HERE.txt.");var next=action=="disable"?new ShareConfig():action=="enable"&&config.Enabled?config:new ShareConfig(true,Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)).TrimEnd('=').Replace('+','-').Replace('/','_'));File.WriteAllText(file+".tmp",JsonSerializer.Serialize(next,Library.Json));File.Move(file+".tmp",file,true);config=next;return Status();}}
 public bool Allowed(string token){lock(gate)return origin!=""&&config.Enabled&&token==config.Token;}
 public static object[] Books(LibraryState s)=>s.Books.Where(b=>!b.Archived).Select(b=>(object)new{id=b.Id,isbn=b.Isbn,title=b.Title,description=b.Description,author=b.Author,series=b.Series,publisher=b.Publisher,coverUrl=b.CoverUrl,collectionId=b.CollectionId,availability=CirculationPolicy.Availability(s,b.Id)}).ToArray();
}
