using System.Net;
using System.Net.Mail;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
namespace HomeLibrary;
public sealed class ReminderConfiguration {
 public bool EmailPasswordRecovery{get;set;}
 public bool Enabled{get;set;}public string Host{get;set;}="";public int Port{get;set;}=587;public string Username{get;set;}="";public string? Password{get;set;}public string FromEmail{get;set;}="";public int DaysBefore{get;set;}=2;public int OverdueDays{get;set;}=7;public int SendHour{get;set;}=9;
}
public sealed record ReminderMail(string LoanId,string CopyId,string Stage,string DueDate,string Recipient,string Subject,string Body);
public interface IReminderSender {Task Send(ReminderConfiguration settings,ReminderMail mail,CancellationToken stop);}
public sealed class SmtpReminderSender:IReminderSender {
 public async Task Send(ReminderConfiguration settings,ReminderMail mail,CancellationToken stop){
  using var message=new MailMessage(settings.FromEmail,mail.Recipient){Subject=mail.Subject,Body=mail.Body,IsBodyHtml=false};
  using var client=new SmtpClient(settings.Host,settings.Port){EnableSsl=true,UseDefaultCredentials=false,Credentials=new NetworkCredential(settings.Username,settings.Password),Timeout=20000};
  using var timeout=CancellationTokenSource.CreateLinkedTokenSource(stop);timeout.CancelAfter(TimeSpan.FromSeconds(25));await client.SendMailAsync(message,timeout.Token);
 }
}
public sealed class Reminders:IDisposable {
 readonly object gate=new();readonly SemaphoreSlim sending=new(1,1);readonly string file;readonly IDataProtector protector;readonly SqliteConnection db;readonly Store store;readonly IReminderSender sender;
 public Reminders(string directory,IDataProtectionProvider protection,Store store,IReminderSender sender){this.store=store;this.sender=sender;file=Path.Combine(directory,"reminder-settings.protected");protector=protection.CreateProtector("HomeLibrary.Reminders.v1");db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=Path.Combine(directory,"reminders.sqlite")}.ToString());db.Open();using var cmd=db.CreateCommand();cmd.CommandText="PRAGMA journal_mode=WAL;PRAGMA secure_delete=ON;PRAGMA synchronous=FULL; CREATE TABLE IF NOT EXISTS deliveries(id TEXT PRIMARY KEY,loan TEXT NOT NULL,copy TEXT NOT NULL,stage TEXT NOT NULL,due TEXT NOT NULL,attempted TEXT NOT NULL,status TEXT NOT NULL);";cmd.ExecuteNonQuery();}
 ReminderConfiguration Read(){lock(gate){if(!File.Exists(file))return new();try{return JsonSerializer.Deserialize<ReminderConfiguration>(protector.Unprotect(File.ReadAllText(file)),Library.Json)??new();}catch(System.Security.Cryptography.CryptographicException){throw new LibraryError("Email settings cannot be decrypted. Save the sending account again.");}}}
 public object Status(){lock(gate){var c=Read();var log=new List<object>();using var cmd=db.CreateCommand();cmd.CommandText="SELECT copy,stage,due,attempted,status FROM deliveries ORDER BY attempted DESC LIMIT 50";using var rows=cmd.ExecuteReader();while(rows.Read())log.Add(new{copyId=rows.GetString(0),stage=rows.GetString(1),dueDate=rows.GetString(2),attemptedAt=rows.GetString(3),status=rows.GetString(4)});return new{emailPasswordRecovery=c.EmailPasswordRecovery,enabled=c.Enabled,host=c.Host,port=c.Port,username=c.Username,fromEmail=c.FromEmail,passwordSaved=!string.IsNullOrEmpty(c.Password),daysBefore=c.DaysBefore,overdueDays=c.OverdueDays,sendHour=c.SendHour,timeZone=TimeZoneInfo.Local.DisplayName,history=log};}}
 public void Save(ReminderConfiguration c){lock(gate){c.Host=c.Host.Trim();c.Username=c.Username.Trim();c.FromEmail=c.FromEmail.Trim();c.Password??=Read().Password;
  if(c.Host.Length>253||c.Host.Any(char.IsControl)||c.Username.Length>320||c.Username.Any(char.IsControl)||c.Password?.Length>1000||c.Port is <1 or >65535||c.Port==465||c.DaysBefore is <1 or >14||c.OverdueDays is <1 or >30||c.SendHour is <0 or >23)throw new LibraryError("Check SMTP settings. Use STARTTLS (usually port 587), 1–14 days before due, 1–30 days overdue, and hour 0–23. Port 465 is not supported.");
  if((c.Enabled||c.EmailPasswordRecovery)&&(Uri.CheckHostName(c.Host)==UriHostNameType.Unknown||!Email(c.FromEmail)||c.Username==""||string.IsNullOrEmpty(c.Password)))throw new LibraryError("Enter the SMTP host, sending email, username and app password before enabling reminders.");
  File.WriteAllText(file+".tmp",protector.Protect(JsonSerializer.Serialize(c,Library.Json)));if(OperatingSystem.IsLinux())File.SetUnixFileMode(file+".tmp",UnixFileMode.UserRead|UnixFileMode.UserWrite);File.Move(file+".tmp",file,true);
 }}
 public static bool Email(string value)=>!value.Any(char.IsControl)&&MailAddress.TryCreate(value,out var email)&&email.Address==value&&value.Contains('@');
 public static List<ReminderMail> Plan(LibraryState state,ReminderConfiguration settings,DateOnly today){
  var result=new List<ReminderMail>();foreach(var loan in state.Loans.Where(l=>l.ReturnedDate==null&&l.DueDate!="")){
   var borrower=state.Borrowers.Find(b=>b.Id==loan.BorrowerId&&!b.Archived&&b.ReminderConsent);var book=state.Books.Find(b=>b.Id==loan.CopyId&&!b.Archived);if(borrower==null||book==null||!Email(borrower.Contact.Trim())||!DateOnly.TryParseExact(loan.DueDate,"yyyy-MM-dd",out var due))continue;
   var days=due.DayNumber-today.DayNumber;var stage=days>0&&days<=settings.DaysBefore?"before":days==0?"due":days<=-settings.OverdueDays?"overdue":"";if(stage=="")continue;
   string Line(string text)=>text.Replace('\r',' ').Replace('\n',' ').Trim();var status=stage=="overdue"?"return reminder":stage=="due"?"due today":"due soon";var name=Line(state.LibraryName);
   result.Add(new(loan.Id,book.Id,stage,loan.DueDate,borrower.Contact.Trim(),$"{name}: {status} — {Line(book.Title)}",$"Hi {Line(borrower.Name)},\n\nA friendly reminder about your borrowed book:\n\n{Line(book.Title)}\n{Line(book.Author)}\nCopy: {book.Id}\nReturn date: {loan.DueDate}\n\nPlease contact the library if you need more time or no longer want email reminders.\n\nThank you!\n{name}"));
  }return result;
 }
 string Key(ReminderMail mail)=>mail.LoanId+"|"+mail.DueDate+"|"+mail.Stage;
 bool Claim(ReminderMail mail,DateTimeOffset now){lock(gate){using var cmd=db.CreateCommand();cmd.CommandText="INSERT OR IGNORE INTO deliveries VALUES($id,$loan,$copy,$stage,$due,$at,'check-delivery')";cmd.Parameters.AddWithValue("$id",Key(mail));cmd.Parameters.AddWithValue("$loan",mail.LoanId);cmd.Parameters.AddWithValue("$copy",mail.CopyId);cmd.Parameters.AddWithValue("$stage",mail.Stage);cmd.Parameters.AddWithValue("$due",mail.DueDate);cmd.Parameters.AddWithValue("$at",now.ToString("O"));return cmd.ExecuteNonQuery()==1;}}
 void Complete(ReminderMail mail,string status){lock(gate){using var cmd=db.CreateCommand();cmd.CommandText="UPDATE deliveries SET status=$status WHERE id=$id";cmd.Parameters.AddWithValue("$id",Key(mail));cmd.Parameters.AddWithValue("$status",status);cmd.ExecuteNonQuery();}}
 public async Task Run(DateTimeOffset now,CancellationToken stop){if(!await sending.WaitAsync(0,stop))return;try{
  var c=Read();if(!c.Enabled||now.LocalDateTime.Hour<c.SendHour)return;var today=DateOnly.FromDateTime(now.LocalDateTime);
  var attempts=0;foreach(var planned in Plan(store.Read().State,c,today)){
   stop.ThrowIfCancellationRequested();c=Read();if(!c.Enabled)break;
   var current=Plan(store.Read().State,c,today).Find(m=>Key(m)==Key(planned));if(attempts>=30)break;if(current==null||!Claim(current,now))continue;attempts++;
   try{await sender.Send(c,current,stop);Complete(current,"sent");}catch(Exception error) when(error is SmtpException or OperationCanceledException or InvalidOperationException){Complete(current,"check-delivery");break;}
  }
 }finally{sending.Release();}}
 public bool RecoveryEnabled=>Read().EmailPasswordRecovery;
 public async Task SendRecovery(string recipient,string link,CancellationToken stop){await sending.WaitAsync(stop);try{var c=Read();if(!c.EmailPasswordRecovery||!Email(recipient))return;await sender.Send(c,new("","","account-reset","",recipient,"Carribrary — reset your sign-in password","A password reset was requested for your Carribrary borrower account.\n\nOpen this private link within 30 minutes:\n"+link+"\n\nIf you did not request it, ignore this email. It does not change your password unless you confirm a new one. This does not reset or unlock your encrypted personal reading list."),stop);}finally{sending.Release();}}
 public async Task Test(CancellationToken stop){var c=Read();if(!Email(c.FromEmail)||c.Host==""||c.Username==""||string.IsNullOrEmpty(c.Password))throw new LibraryError("Save complete SMTP settings first. The test is sent only to the sending email address.");try{await sender.Send(c,new("test","","test","",c.FromEmail,"Carribrary — test email","Your Carribrary email settings are working. No borrower reminder was sent."),stop);}catch(Exception error)when(error is SmtpException or OperationCanceledException or InvalidOperationException){throw new LibraryError("The test could not be confirmed. Check your provider's STARTTLS host, username and app password, and check the sending mailbox. No credentials were returned.");}}
 public async Task<IDisposable> Pause(CancellationToken stop){await sending.WaitAsync(stop);return new Resume(sending);}
 sealed class Resume(SemaphoreSlim semaphore):IDisposable {public void Dispose()=>semaphore.Release();}
 public void EraseDeliveries(string[] loanIds){lock(gate){foreach(var id in loanIds){using var cmd=db.CreateCommand();cmd.CommandText="DELETE FROM deliveries WHERE loan=$id";cmd.Parameters.AddWithValue("$id",id);cmd.ExecuteNonQuery();}using var cleanup=db.CreateCommand();cleanup.CommandText="PRAGMA wal_checkpoint(TRUNCATE)";cleanup.ExecuteNonQuery();}}
 public void Dispose(){lock(gate)db.Dispose();sending.Dispose();}
}
public sealed class ReminderWorker(Reminders reminders,SnapshotGate snapshots,ILogger<ReminderWorker> logger):BackgroundService {
 protected override async Task ExecuteAsync(CancellationToken stop){using var timer=new PeriodicTimer(TimeSpan.FromMinutes(15));do{try{using var lease=await snapshots.Shared(stop);await reminders.Run(DateTimeOffset.Now,stop);}catch(OperationCanceledException)when(stop.IsCancellationRequested){return;}catch(Exception){logger.LogWarning("Email reminder check failed. Review Reminders settings in the library.");}}while(await timer.WaitForNextTickAsync(stop));}
}
