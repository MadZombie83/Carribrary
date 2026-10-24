using HomeLibrary;
using System.Net;
using System.Text;
using System.Text.Json;
public static class RegistrationProtectionTests {
 public static async Task Run(){
  void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS "+message);}
  async Task Bad(Func<Task> work,int code){try{await work();throw new Exception("Verification accepted unsafe input");}catch(LibraryError e){Check(e.Status==code,"verification rejects unsafe/unavailable input with controlled status");}}
  var now=DateTimeOffset.UtcNow;var handler=new Reply();using var client=new HttpClient(handler);
  var guard=new RegistrationProtection(client,"public-test-sitekey","private-test-secret-key-232","https://library.example",new FixedTime(now));
  var good=JsonSerializer.Serialize(new{success=true,hostname="library.example",action="register",challenge_ts=now});
  handler.Body=good;await new RegistrationProtection(client,"","","",new FixedTime(now)).Verify(null,null,default);Check(handler.Calls==0,"unconfigured self-hosted registration makes no external request");
  await Bad(()=>new RegistrationProtection(client,"public-test-sitekey","","https://library.example").Verify("proof",null,default),503);
  await Bad(()=>guard.Verify("",null,default),400);await Bad(()=>guard.Verify(new string('x',2049),null,default),400);Check(handler.Calls==0,"missing/oversized verification tokens never reach provider");
  await guard.Verify("one-time-proof",IPAddress.Parse("192.0.2.1"),default);
  Check(handler.LastUri=="https://challenges.cloudflare.com/turnstile/v0/siteverify"&&handler.LastMethod=="POST"&&!handler.LastUri.Contains("secret")&&handler.LastBody.Contains("secret=private-test-secret-key-232")&&handler.LastBody.Contains("response=one-time-proof")&&handler.LastBody.Contains("remoteip=192.0.2.1"),"provider verification uses fixed HTTPS endpoint and form body, never secret query string");
  foreach(var body in new[]{"{\"success\":false,\"error-codes\":[\"timeout-or-duplicate\"]}",JsonSerializer.Serialize(new{success=true,hostname="attacker.example",action="register",challenge_ts=now}),JsonSerializer.Serialize(new{success=true,hostname="library.example",action="login",challenge_ts=now}),JsonSerializer.Serialize(new{success=true,hostname="library.example",action="register",challenge_ts=now.AddMinutes(-6)}),JsonSerializer.Serialize(new{success=true,hostname="library.example",action="register",challenge_ts=now.AddMinutes(2)})}){handler.Body=body;await Bad(()=>guard.Verify("proof",null,default),400);}
  foreach(var body in new[]{"not-json","[]",new string('x',16385)}){handler.Body=body;await Bad(()=>guard.Verify("proof",null,default),503);}
  handler.Body=good;handler.Status=HttpStatusCode.ServiceUnavailable;await Bad(()=>guard.Verify("proof",null,default),503);handler.Status=HttpStatusCode.OK;handler.Timeout=true;await Bad(()=>guard.Verify("proof",null,default),503);handler.Timeout=false;
  handler.Body="{\"success\":false,\"error-codes\":[\"timeout-or-duplicate\"]}";await Bad(()=>guard.Verify("one-time-proof",null,default),400);Check(!JsonSerializer.Serialize(guard.Options(),Library.Json).Contains("private-test"),"public verification options never expose the server secret");
 }
 sealed class FixedTime(DateTimeOffset now):TimeProvider {public override DateTimeOffset GetUtcNow()=>now;}
 sealed class Reply:HttpMessageHandler {
  public int Calls;public string Body="",LastUri="",LastMethod="",LastBody="";public HttpStatusCode Status=HttpStatusCode.OK;public bool Timeout;
  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken stop){Calls++;LastUri=request.RequestUri!.AbsoluteUri;LastMethod=request.Method.Method;LastBody=await request.Content!.ReadAsStringAsync(stop);if(Timeout)throw new TaskCanceledException();return new(Status){Content=new StringContent(Body,Encoding.UTF8,"application/json")};}
 }
}
