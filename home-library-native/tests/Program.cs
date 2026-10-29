using HomeLibrary;using Microsoft.AspNetCore.DataProtection;using System.Text.Json;using System.Security.Cryptography;using System.Security.Cryptography.X509Certificates;using System.Text;
var directory=Path.Combine(Path.GetTempPath(),"hl-native-test-"+Guid.NewGuid());Directory.CreateDirectory(directory);
var protection=DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(directory,"keys")));
void Check(bool value,string name){if(!value)throw new Exception("FAILED: "+name);Console.WriteLine("PASS "+name);}
void Throws(Action action,string text){try{action();throw new Exception("Expected failure: "+text);}catch(LibraryError e){Check(e.Message.Contains(text,StringComparison.OrdinalIgnoreCase),text);}}
JsonElement Input(object value)=>JsonSerializer.SerializeToElement(value,Library.Json);
try{
 if(args.Contains("--public-install-only")){
  using var client=new HttpClient{Timeout=TimeSpan.FromMinutes(4)};var temp=Path.Combine(directory,"proxy-install");Directory.CreateDirectory(temp);var connection=new PublicConnection(temp,new Settings(),protection,client);await connection.Configure(new(true,"proxy-check.example.org",AllowDownload:true),default);var config=Path.Combine(temp,"public-https","Caddyfile");File.WriteAllText(config,PublicConnection.Render("proxy-check.example.org",4174));var info=new System.Diagnostics.ProcessStartInfo(Path.Combine(temp,"public-https","caddy")){UseShellExecute=false};info.Environment["XDG_DATA_HOME"]=Path.Combine(temp,"isolated-caddy-data");info.Environment["XDG_CONFIG_HOME"]=Path.Combine(temp,"isolated-caddy-config");foreach(var a in new[]{"validate","--config",config,"--adapter","caddyfile"})info.ArgumentList.Add(a);using var process=System.Diagnostics.Process.Start(info)!;await process.WaitForExitAsync();Check(process.ExitCode==0,"actual pinned Caddy install, archive extraction and generated configuration validation");return;
 }
 await PublicConnectionTests.Run(Path.Combine(directory,"public-connection-tests"),protection);
 if(args.Contains("--public-connection-only"))return;
 AdminNotificationTests.Run();
 PurchaseSuggestionTests.Run(Path.Combine(directory,"suggestions"),protection);
 if(args.Contains("--notifications-only"))return;
 if(args.Contains("--portable-only")){await PortableBackupTests.Run(Path.Combine(directory,"portable-backups-test"));return;}
 if(args.Contains("--complete-recovery-only")){await CompleteRecoveryTests.Run(Path.Combine(directory,"complete-recovery"));return;}
 if(args.Contains("--backup-verification-only")){await BackupVerificationTests.Run(Path.Combine(directory,"backup-verification"),protection);return;}
 if(args.Contains("--batch-renewals-only")){await BatchRenewalTests.Run(Path.Combine(directory,"batch-renewals"),protection);return;}
 ReadingPolishTests.Run(Path.Combine(directory,"reading-polish"),protection);
 if(args.Contains("--reading-polish-only"))return;
 CustomThemeTests.Run(Path.Combine(directory,"themes"),protection);
 if(args.Contains("--custom-themes-only"))return;
 SidebarArtworkTests.Run(Path.Combine(directory,"sidebar-artwork"),protection);
 if(args.Contains("--local-admin-only")){LocalAdministratorTests.Run(Path.Combine(directory,"local-admin"),protection);return;}
 using(var store=new Store(directory,protection)){
 Check(Library.ValidCode("9780141439518")&&Library.ValidCode("0306406152")&&!Library.ValidCode("9780141439519"),"ISBN checks");
 var add=new ActionRequest("addBook",Input(new{isbn="9780141439518",title="Pride and Prejudice",author="Jane Austen",series="Classics"}),0,Guid.NewGuid().ToString());store.Apply(add);store.Apply(add);Check(store.Read().State.Books.Count==1,"idempotent add");
 Throws(()=>store.Apply(add with{Input=Input(new{isbn="9780141439518",title="Different"})}),"different change");
 Throws(()=>store.Apply(new("addBook",add.Input,1)),"duplicate");
 store.Apply(new("addBook",Input(new{isbn="9780141439518",title="Pride and Prejudice",author="Jane Austen",confirmedDuplicates=new[]{"BK-0001"}}),1));Check(store.Read().State.Books[1].Id=="BK-0002","unique copy IDs");
 store.Apply(new("addBorrower",Input(new{name="Pat",contact="private@example.com"}),2));
 var checkout=new ActionRequest("checkout",Input(new{copyId="BK-0001",borrowerId="BR-0001",checkoutDate=Library.Today}),3,Guid.NewGuid().ToString());store.Apply(checkout);store.Apply(checkout);Check(store.Read().State.Loans.Count==1,"idempotent checkout");
 Throws(()=>store.Apply(new("checkout",checkout.Input,3)),"changed");
 Throws(()=>store.Apply(new("checkout",checkout.Input,4)),"already on loan");
 Throws(()=>store.Apply(new("removeBook",Input(new{copyId="BK-0001"}),4)),"Return");
 store.Apply(new("updateBook",Input(new{copyId="BK-0001",isbn="9780141439518",title="Pride and Prejudice",author="Jane Austen",series="Edited series"}),4));
 store.Apply(new("returnBook",Input(new{copyId="BK-0001"}),5));store.Apply(new("removeBook",Input(new{copyId="BK-0001"}),6));Check(store.Read().State.Loans.Count==1&&store.Read().State.Books[0].Archived,"archiving retains loan history");
 var json=JsonSerializer.Serialize(Sharing.Books(store.Read().State));Check(!json.Contains("private@example.com")&&!json.Contains("checkoutDate")&&!json.Contains("borrower"),"public projection excludes private records");
 var backup=Directory.GetFiles(Path.Combine(directory,"backups"),"*.hlbackup").First();Check(!Encoding.UTF8.GetString(File.ReadAllBytes(backup)).Contains("books"),"automatic backup encrypted");Library.Validate(store.RestoreEncrypted(File.ReadAllBytes(backup)));Console.WriteLine("PASS encrypted backup round trip");
 }
 using(var restart=new Store(directory,protection))Check(restart.Read().State.Books.Count==2&&restart.Read().State.Loans[0].ReturnedDate!=null,"database restart persistence");
 using(var names=new Store(directory,protection)){
  Throws(()=>names.Apply(new("setLibraryName",Input(new{libraryName=" "}),names.Read().Revision)),"library name");
  names.Apply(new("setLibraryName",Input(new{libraryName="  Beth’s Library  "}),names.Read().Revision));
  var named=names.Read().State;names.Backup(named,"named-test");Check(names.RestoreEncrypted(File.ReadAllBytes(Path.Combine(directory,"backups","named-test.hlbackup"))).LibraryName=="Beth’s Library","custom name included in encrypted backup");
 }
 using(var names=new Store(directory,protection))Check(names.Read().State.LibraryName=="Beth’s Library","custom library name persists after restart");
 Check(JsonSerializer.Deserialize<LibraryState>("{\"version\":1}",Library.Json)!.LibraryName=="Carribrary","nameless legacy backup uses Carribrary default");
 var legacy=JsonSerializer.Deserialize<LibraryState>("{\"version\":1}",Library.Json)!;Check(legacy.BannerSubtitle=="A shelf worth sharing"&&legacy.EncouragingMessage=="A place for every book.","legacy sayings default without migration");
 using(var sayings=new Store(directory,protection)){
  var before=sayings.Read().Revision;
  Throws(()=>sayings.Apply(new("setLibraryPresentation",Input(new{bannerSubtitle=new string('x',161),encouragingMessage="Good"}),before)),"Sayings");
  Check(sayings.Read().Revision==before,"invalid saying does not modify revision");
  sayings.Apply(new("setLibraryPresentation",Input(new{bannerSubtitle="  Share a little joy  ",encouragingMessage="You are enough. ♥"}),before));
  var saved=sayings.Read().State;sayings.Backup(saved,"sayings-test");var restored=sayings.RestoreEncrypted(File.ReadAllBytes(Path.Combine(directory,"backups","sayings-test.hlbackup")));Check(restored.EncouragingMessage=="You are enough. ♥"&&restored.BannerSubtitle=="Share a little joy","sayings survive encrypted backup");
 }
 using(var sayings=new Store(directory,protection)){Check(sayings.Read().State.EncouragingMessage=="You are enough. ♥","sayings survive server restart");sayings.Apply(new("setLibraryPresentation",Input(new{bannerSubtitle="",encouragingMessage=""}),sayings.Read().Revision));Check(sayings.Read().State.EncouragingMessage=="","empty saying hides message");}
 var identity=new Identity(directory,protection);var pair=identity.StartPair("https://example.duckdns.org:7443");
 using var phone=RSA.Create(2048);var pub=Convert.ToBase64String(phone.ExportSubjectPublicKeyInfo());var challenge=Encoding.UTF8.GetBytes("home-library-pair:v1:"+pair.Code+":"+pub);var proof=Convert.ToBase64String(phone.SignData(challenge,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1));
 Throws(()=>identity.Enroll(new(pair.Code,"Phone",pub,Convert.ToBase64String(new byte[256]))),"proof");
 var response=Input(identity.Enroll(new(pair.Code,"Test phone",pub,proof)));using var cert=X509CertificateLoader.LoadCertificate(Convert.FromBase64String(response.GetProperty("certificate").GetString()!));
 Check(identity.Approved(cert)!=null,"signed paired client certificate accepted");Throws(()=>identity.Enroll(new(pair.Code,"Phone",pub,proof)),"already used");
 var restartIdentity=new Identity(directory,protection);Check(restartIdentity.ServerKey==identity.ServerKey&&restartIdentity.Approved(cert)!=null,"server identity and device survive restart");
 var id=response.GetProperty("deviceId").GetString()!;restartIdentity.Revoke(id);Check(restartIdentity.Approved(cert)==null,"phone revocation");
 Check(new Identity(directory,protection).Approved(cert)==null,"revocation persists");
 var sharing=new Sharing(directory,"https://example.duckdns.org");var initial=Input(sharing.Change("enable"));var token=new Uri(initial.GetProperty("url").GetString()!).Segments[2].Trim('/');Check(sharing.Allowed(token),"share link enabled");sharing.Change("replace");Check(!sharing.Allowed(token),"old share QR revoked on replacement");sharing.Change("disable");Check(!Input(sharing.Status()).GetProperty("enabled").GetBoolean(),"sharing can be disabled");
 var receiptState=new LibraryState{LibraryName="Our Library",Books=[new(){Id="BK-0001",Title="A Book",Author="An Author"}],Borrowers=[new(){Id="BR-0001",Name="Pat",Contact="pat+books@example.com"}],Loans=[new(){Id="receipt",CopyId="BK-0001",BorrowerId="BR-0001",CheckoutDate="2026-10-01",DueDate="2026-10-15"}]};
 var emailReceipt=BorrowingReceipt.Create(receiptState,"BK-0001");Check(emailReceipt.Medium=="email"&&emailReceipt.Recipient=="pat+books@example.com"&&emailReceipt.Body.Contains("2026-10-15")&&emailReceipt.Body.Contains("A Book"),"email receipt includes book and due date");
 receiptState.Borrowers[0].Contact="+1 (555) 123-4567";Check(BorrowingReceipt.Create(receiptState,"BK0001").Recipient=="+15551234567","text receipt normalizes phone number");
 receiptState.Borrowers[0].Contact="pat@example.com\r\nBcc: someone@example.com";Check(BorrowingReceipt.Create(receiptState,"BK-0001").Medium=="copy","receipt rejects recipient header injection");
 receiptState.Borrowers[0].Contact="";receiptState.Loans[0].DueDate="";var noDate=BorrowingReceipt.Create(receiptState,"BK-0001");Check(noDate.Medium=="copy"&&noDate.Body.Contains("No date set"),"receipt supports missing contact and due date");
 receiptState.Loans[0].ReturnedDate="2026-10-02";Throws(()=>BorrowingReceipt.Create(receiptState,"BK-0001"),"No active loan");

 Check(JsonSerializer.Deserialize<LibraryState>("{}",Library.Json)!.Requests.Count==0,"legacy backups have empty request inbox");
 using(var requests=new Store(Path.Combine(directory,"requests"),protection)){
  void Change(string action,object input)=>requests.Apply(new(action,Input(input),requests.Read().Revision));
  Change("addBook",new{title="Requested book",author="Author",isbn="9780141439518"});
  var request=new PublicBookRequest("BK-0001","Visitor","visitor@example.com","Please",Guid.NewGuid().ToString(),"9780141439518","Requested book");
  var ack=Input(requests.SubmitRequest(request));requests.SubmitRequest(request);
  Check(ack.EnumerateObject().Count()==2&&requests.Read().State.Requests.Count==1,"public acknowledgement is private and retries are idempotent");
  Throws(()=>requests.SubmitRequest(request with{Name="Different"}),"different change");
  Throws(()=>requests.SubmitRequest(request with{RequestId=Guid.NewGuid().ToString(),Contact="invalid"}),"phone number");
  var requestIdentity=ack.GetProperty("id").GetString()!;
  Throws(()=>Change("fulfillRequest",new{requestId=requestIdentity,checkoutDate=Library.Today}),"Approve");
  Change("approveRequest",new{requestId=requestIdentity});
  Check(requests.Read().State.Loans.Count==0&&requests.Read().State.Borrowers.Count==0,"approval does not create a borrower or loan");
  Change("fulfillRequest",new{requestId=requestIdentity,checkoutDate=Library.Today,dueDate="2099-01-01"});
  var state=requests.Read().State;
  Check(state.Requests[0].Status=="fulfilled"&&state.Requests[0].LoanId==state.Loans[0].Id&&BorrowingReceipt.Create(state,"BK-0001").Recipient=="visitor@example.com","handover links request to loan and receipt");
  var second=request with{RequestId=Guid.NewGuid().ToString(),Name="Other",Contact="other@example.com"};
  Throws(()=>requests.SubmitRequest(second),"on loan");
  Change("returnBook",new{copyId="BK-0001"});
  var secondId=Input(requests.SubmitRequest(second)).GetProperty("id").GetString()!;
  Change("approveRequest",new{requestId=secondId});
  Throws(()=>Change("fulfillRequest",new{requestId=secondId,checkoutDate="not-a-date"}),"checkout date");
  Check(requests.Read().State.Borrowers.Count==1&&requests.Read().State.Requests[1].Status=="approved","failed handover rolls back borrower and request changes");
  Change("declineRequest",new{requestId=secondId});
  Throws(()=>Change("fulfillRequest",new{requestId=secondId,checkoutDate=Library.Today}),"Approve");
  var thirdId=Input(requests.SubmitRequest(request with{RequestId=Guid.NewGuid().ToString()})).GetProperty("id").GetString()!;
  Change("approveRequest",new{requestId=thirdId});Change("fulfillRequest",new{requestId=thirdId,checkoutDate=Library.Today});
  Check(requests.Read().State.Borrowers.Count==1,"handover reuses matching borrower");
  Change("returnBook",new{copyId="BK-0001"});Change("removeBook",new{copyId="BK-0001"});
  state=requests.Read().State;Library.Validate(state);requests.Backup(state,"requests-test");
  Check(state.Books[0].Archived&&requests.RestoreEncrypted(File.ReadAllBytes(Path.Combine(directory,"requests","backups","requests-test.hlbackup"))).Requests.Count==3,"request history retained in archives and encrypted backups");
 }
 using(var restartRequests=new Store(Path.Combine(directory,"requests"),protection))Check(restartRequests.Read().State.Requests.Count==3,"requests survive restart");
 PrivacyBoundaryTests.Run(Path.Combine(directory,"privacy-boundary"),protection);
 AcquisitionTests.Run(Path.Combine(directory,"acquisitions"),protection);
 HistoryToolsTests.Run(Path.Combine(directory,"history-tools"),protection);
 await LendingPolicyTests.Run(Path.Combine(directory,"lending-policy"),protection);
 await BatchRenewalTests.Run(Path.Combine(directory,"batch-renewals"),protection);
 await CompleteRecoveryTests.Run(Path.Combine(directory,"complete-recovery"));
 await BackupVerificationTests.Run(Path.Combine(directory,"backup-verification"),protection);
 await CompleteBackupTests.Run(Path.Combine(directory,"complete-backup"),protection);
await PortableBackupTests.Run(Path.Combine(directory,"portable-backups-test"));
 CopyIdentityTests.Run(Path.Combine(directory,"copy-identity"),protection);
 LocalAdministratorTests.Run(Path.Combine(directory,"local-admin"),protection);
 AccountSecurityAuditTests.Run(Path.Combine(directory,"account-audit"));
 await RegistrationProtectionTests.Run();
 await RequestIntegrityTests.Run(Path.Combine(directory,"request-integrity"),protection);
 CopyPolicyTests.Run(Path.Combine(directory,"copy-policy"),protection);
 await CatalogNumberTests.Run(Path.Combine(directory,"catalog-reuse"),protection);
 CollectionTests.Run(Path.Combine(directory,"collections"),protection);
 BorrowerAccountTests.Run(Path.Combine(directory,"accounts"),protection);
 ReadingTrackerTests.Run(Path.Combine(directory,"reading"),protection);
 await HouseholdToolsTests.Run(Path.Combine(directory,"household-tools"),protection);
 await AccountDeletionTests.Run(Path.Combine(directory,"account-erasure"),protection);
 await CoverLookupTests.Run();
 await CompanionFeaturesTests.Run(Path.Combine(directory,"companion"),protection);
 Console.WriteLine("ALL NATIVE SERVER TESTS PASSED");
}catch(Exception error){Console.Error.WriteLine(error);Environment.ExitCode=1;}finally{Directory.Delete(directory,true);}
