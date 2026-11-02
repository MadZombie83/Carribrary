using HomeLibrary;using Microsoft.AspNetCore.DataProtection;using System.Text.Json;
public static class BorrowerDashboardTests {
 public static void Run(string directory,IDataProtectionProvider protection){
  Directory.CreateDirectory(directory);using var store=new Store(directory,protection);using var accounts=new BorrowerAccounts(directory);
  JsonElement Input(object o)=>JsonSerializer.SerializeToElement(o,Library.Json);
  void Change(string action,object input)=>store.Apply(new(action,Input(input),store.Read().Revision));
  var a=accounts.Approve(new(accounts.Register(new("dashboard.reader","Disposable dashboard password!","Reader A","","")).Id),store);
  var b=accounts.Approve(new(accounts.Register(new("dashboard.other","Disposable other password!","Reader B","","")).Id),store);
  Change("addBook",new{title="Own held book",author="Example"});Change("addBook",new{title="Other held book",author="Example"});
  Change("createHold",new{copyId="BK-0001",borrowerId=a.BorrowerId});Change("createHold",new{copyId="BK-0002",borrowerId=b.BorrowerId});
  var view=Input(store.AccountView(a));var holds=view.GetProperty("holds");
  if(holds.GetArrayLength()!=1||holds[0].GetProperty("copyId").GetString()!="BK-0001"||holds[0].TryGetProperty("borrowerId",out _))throw new Exception("Borrower hold view must expose only their own circulation details");
  var legacy=Input(ReadingAccess.View(store.Read(),a.ReaderId));if(legacy.GetProperty("holds").GetArrayLength()!=0)throw new Exception("Personal capability links must not gain account circulation access");
  Console.WriteLine("PASS borrower dashboard exposes own manual holds only; personal links retain their existing boundary");
 }
}
