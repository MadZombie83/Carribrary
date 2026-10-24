using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace HomeLibrary;
public sealed record RegistrationOptionsRequest(string ShareToken);
// Optional deployment configuration only. No shared developer key or client-side secret.
public sealed class RegistrationProtection(HttpClient client,string siteKey,string secretKey,string publicOrigin,TimeProvider? clock=null) {
 readonly TimeProvider time=clock??TimeProvider.System;
 public bool Enabled=>!string.IsNullOrWhiteSpace(siteKey)||!string.IsNullOrWhiteSpace(secretKey);
 void Configured(){if(!Regex.IsMatch(siteKey??"",@"^[A-Za-z0-9_-]{10,200}$")||!Regex.IsMatch(secretKey??"",@"^[A-Za-z0-9_-]{16,200}$")||!Uri.TryCreate(publicOrigin,UriKind.Absolute,out var u)||(u.Scheme!="https"||u.Host==""))throw new LibraryError("Registration protection needs server configuration. Contact the librarian.",503);}
 public object Options(){if(Enabled)Configured();return new{turnstileEnabled=Enabled,siteKey=Enabled?siteKey:""};}
 public async Task Verify(string? token,IPAddress? address,CancellationToken stop){
  if(!Enabled)return;Configured();
  if(string.IsNullOrWhiteSpace(token)||token.Length>2048)throw new LibraryError("Complete the registration verification, then try again.",400);
  using var deadline=CancellationTokenSource.CreateLinkedTokenSource(stop);deadline.CancelAfter(TimeSpan.FromSeconds(8));var verifyStop=deadline.Token;
  try{
   var fields=new Dictionary<string,string>{{"secret",secretKey},{"response",token}};if(address!=null)fields["remoteip"]=address.ToString();
   using var request=new HttpRequestMessage(HttpMethod.Post,"https://challenges.cloudflare.com/turnstile/v0/siteverify"){Content=new FormUrlEncodedContent(fields)};
   using var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,verifyStop);
   if(!response.IsSuccessStatusCode)throw new LibraryError("Registration verification is temporarily unavailable. Try later or contact the librarian.",503);
   using var stream=await response.Content.ReadAsStreamAsync(verifyStop);using var buffer=new MemoryStream();var chunk=new byte[4096];int count;
   while((count=await stream.ReadAsync(chunk,verifyStop))>0){if(buffer.Length+count>16384)throw new LibraryError("Registration verification returned an invalid response. Try later.",503);buffer.Write(chunk,0,count);}
   using var json=JsonDocument.Parse(buffer.ToArray());var data=json.RootElement;
   if(!data.TryGetProperty("success",out var success)||success.ValueKind!=JsonValueKind.True)throw new LibraryError("Registration verification expired or failed. Complete it again and retry.",400);
   var host=new Uri(publicOrigin).IdnHost;
   if(!data.TryGetProperty("hostname",out var hostname)||hostname.ValueKind!=JsonValueKind.String||!string.Equals(hostname.GetString(),host,StringComparison.OrdinalIgnoreCase)||!data.TryGetProperty("action",out var action)||action.GetString()!="register"||!data.TryGetProperty("challenge_ts",out var stamp)||!DateTimeOffset.TryParse(stamp.GetString(),out var issued)||issued<time.GetUtcNow().AddMinutes(-5)||issued>time.GetUtcNow().AddSeconds(30))throw new LibraryError("Registration verification does not match this library. Complete it again.",400);
  }catch(OperationCanceledException)when(!stop.IsCancellationRequested){throw new LibraryError("Registration verification timed out. Try again later.",503);}
  catch(HttpRequestException){throw new LibraryError("Registration verification is temporarily unavailable. Try later or contact the librarian.",503);}
  catch(IOException){throw new LibraryError("Registration verification is temporarily unavailable. Try later or contact the librarian.",503);}
  catch(JsonException){throw new LibraryError("Registration verification returned an invalid response. Try later.",503);}
  catch(InvalidOperationException){throw new LibraryError("Registration verification returned an invalid response. Try later.",503);}
 }
}
