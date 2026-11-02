using System.Text.Json;
using System.Text.RegularExpressions;
namespace HomeLibrary;

// Read-only release discovery. Never downloads or runs an installer, accepts an input URL,
// or sends library configuration, credentials or borrower data to GitHub.
public sealed class ServerUpdates(HttpClient http) {
 public const string Releases="https://github.com/MadZombie83/Carribrary/releases";
 readonly SemaphoreSlim gate=new(1,1);
 UpdateStatus? cached;
 DateTimeOffset nextCheck;
 public async Task<UpdateStatus> Check(bool force=false,CancellationToken stop=default){
  await gate.WaitAsync(stop);
  try{
   if(cached!=null&&DateTimeOffset.UtcNow<nextCheck&&(!force||DateTimeOffset.UtcNow-cached.CheckedAt<TimeSpan.FromMinutes(1)))return cached;
   var previous=cached;
   try{
    using var request=new HttpRequestMessage(HttpMethod.Get,"https://api.github.com/repos/MadZombie83/Carribrary/releases?per_page=100");
    request.Headers.UserAgent.ParseAdd("Carribrary-Update-Check/1.0");request.Headers.Accept.ParseAdd("application/vnd.github+json");
    using var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,stop);
    response.EnsureSuccessStatusCode();
    if(response.Content.Headers.ContentLength>2*1024*1024)throw new HttpRequestException();
    await using var stream=await response.Content.ReadAsStreamAsync(stop);using var memory=new MemoryStream();
    var buffer=new byte[8192];int count;while((count=await stream.ReadAsync(buffer,stop))>0){if(memory.Length+count>2*1024*1024)throw new HttpRequestException();memory.Write(buffer,0,count);}
    using var json=JsonDocument.Parse(memory.ToArray());
    var latest=Select(json.RootElement,SetupStatus.Version);
    cached=new(SetupStatus.Version,latest?.Version??"",latest?.Url??Releases,latest!=null&&Compare(latest.Version,SetupStatus.Version)>0,DateTimeOffset.UtcNow,false,"");
    nextCheck=DateTimeOffset.UtcNow.AddHours(24);
   }catch(Exception e)when(e is HttpRequestException or JsonException or OperationCanceledException){
    cached=new(SetupStatus.Version,previous?.LatestVersion??"",previous?.ReleaseUrl??Releases,previous?.Available??false,DateTimeOffset.UtcNow,true,"Could not check GitHub. Your library still works; try again later.");
    nextCheck=DateTimeOffset.UtcNow.AddMinutes(15);
   }
   return cached;
  }finally{gate.Release();}
 }
 static readonly Regex VersionPattern=new(@"^v?(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-preview)?$",RegexOptions.CultureInvariant);
 static (Version Value,bool Preview)? Parse(string text){var m=VersionPattern.Match(text);return m.Success&&Version.TryParse(string.Join('.',m.Groups[1].Value,m.Groups[2].Value,m.Groups[3].Value),out var v)?(v,m.Groups[4].Success):null;}
 public static int Compare(string a,string b){var x=Parse(a)??throw new ArgumentException("Invalid release version");var y=Parse(b)??throw new ArgumentException("Invalid installed version");var result=x.Value.CompareTo(y.Value);return result!=0?result:x.Preview==y.Preview?0:x.Preview?-1:1;}
 public static UpdateRelease? Select(JsonElement list,string installed){
  var current=Parse(installed)??throw new ArgumentException("Invalid installed version");
  if(list.ValueKind!=JsonValueKind.Array)throw new JsonException();
  UpdateRelease? selected=null;
  foreach(var r in list.EnumerateArray()){
   if(r.ValueKind!=JsonValueKind.Object)continue;
   if(!r.TryGetProperty("draft",out var draft)||draft.ValueKind!=JsonValueKind.False||!r.TryGetProperty("prerelease",out var pre)||pre.ValueKind is not(JsonValueKind.True or JsonValueKind.False)||!r.TryGetProperty("tag_name",out var tag)||tag.ValueKind!=JsonValueKind.String)continue;
   var name=tag.GetString()!;var parsed=Parse(name);if(parsed==null||pre.GetBoolean()!=parsed.Value.Preview||(!current.Preview&&parsed.Value.Preview))continue;
   if(!r.TryGetProperty("html_url",out var url)||url.ValueKind!=JsonValueKind.String||url.GetString()!=Releases+"/tag/"+name)continue;
   var version=name.TrimStart('v');if(selected==null||Compare(version,selected.Version)>0)selected=new(version,Releases+"/tag/"+name);
  }
  return selected;
 }
}
public record UpdateRelease(string Version,string Url);
public record UpdateStatus(string CurrentVersion,string LatestVersion,string ReleaseUrl,bool Available,DateTimeOffset CheckedAt,bool Stale,string Error);
