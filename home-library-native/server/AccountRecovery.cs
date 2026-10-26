using System.Threading.Channels;
namespace HomeLibrary;
public sealed record ForgotPassword(string Username);
// Bounded in-memory work queue: public responses never reveal account/email existence or SMTP outcomes.
public sealed class AccountRecovery(Reminders reminders,BorrowerAccounts accounts,Store store,Settings settings,SnapshotGate snapshots):BackgroundService {
 readonly Channel<string> queue=Channel.CreateBounded<string>(new BoundedChannelOptions(32){FullMode=BoundedChannelFullMode.Wait,SingleReader=true});
 public void Request(string? username){if(username is {} u&&u.Length<=40)queue.Writer.TryWrite(u);}
 protected override async Task ExecuteAsync(CancellationToken stop){await foreach(var username in queue.Reader.ReadAllAsync(stop))try{
  using var lease=await snapshots.Shared(stop);if(!reminders.RecoveryEnabled||settings.PublicCatalogOrigin=="")continue;
  var prepared=accounts.PrepareEmailReset(username,settings.PublicCatalogOrigin,store);if(prepared!=null)await reminders.SendRecovery(prepared.Value.Email,prepared.Value.Link,stop);
 }catch(OperationCanceledException)when(stop.IsCancellationRequested){return;}catch(Exception){/* No tokens, recipients or SMTP details in public responses/logs. Librarian fallback remains. */}}
}
