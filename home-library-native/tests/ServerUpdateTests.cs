using HomeLibrary;using System.Text.Json;using System.Net;
public static class ServerUpdateTests {
 public static async Task Run(){
  void Check(bool ok,string why){if(!ok)throw new Exception(why);Console.WriteLine("PASS "+why);}
  string releases="""[{"tag_name":"v2.0.59-preview","html_url":"https://github.com/MadZombie83/Carribrary/releases/tag/v2.0.59-preview","draft":false,"prerelease":true},{"tag_name":"v2.0.54","html_url":"https://github.com/MadZombie83/Carribrary/releases/tag/v2.0.54","draft":false,"prerelease":false},{"tag_name":"v99.0.0","html_url":"https://evil.example/release","draft":false,"prerelease":false},{"tag_name":"v100.0.0","html_url":"https://github.com/MadZombie83/Carribrary/releases/tag/v100.0.0","draft":true,"prerelease":false}]""";
  var current=Version.Parse(SetupStatus.Version.Split('-')[0]);var future=$"{current.Major}.{current.Minor}.{current.Build+1}";releases=releases.Replace("2.0.59",future);
  using var list=JsonDocument.Parse(releases);
  Check(ServerUpdates.Select(list.RootElement,"2.0.53")?.Version=="2.0.54","Stable servers exclude previews, drafts and external links");
  Check(ServerUpdates.Select(list.RootElement,"2.0.54-preview")?.Version==future+"-preview","Preview servers find newer official previews");
  Check(ServerUpdates.Compare("2.0.100","2.0.99")>0&&ServerUpdates.Compare("2.0.54","2.0.54-preview")>0,"Numeric versions and same-version promotion compare correctly");
  var handler=new Fake(releases);using var http=new HttpClient(handler);var updates=new ServerUpdates(http);
  var first=await updates.Check();await updates.Check();await updates.Check(true);
  Check(first.Available&&handler.Calls==1,"Automatic and repeated manual checks share cached results");
  Check(handler.Last=="https://api.github.com/repos/MadZombie83/Carribrary/releases?per_page=100","Update request has fixed public endpoint without library data");
  var failure=new Fake("not json");using var bad=new HttpClient(failure);var unavailable=await new ServerUpdates(bad).Check();
  Check(unavailable.Stale&&!unavailable.Available&&unavailable.Error.Contains("library still works"),"Invalid response fails without disrupting the library");
 }
 sealed class Fake(string body):HttpMessageHandler {public int Calls;public string Last="";protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken stop){Calls++;Last=request.RequestUri!.AbsoluteUri;return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(body)});}}
}
