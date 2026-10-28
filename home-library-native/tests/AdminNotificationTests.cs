using HomeLibrary;
public static class AdminNotificationTests {
 public static void Run(){
  void Check(bool ok,string label){if(!ok)throw new Exception(label);Console.WriteLine("PASS "+label);}
  var s=new LibraryState();s.Books.Add(new(){Id="BK-0001"});s.Books.Add(new(){Id="BK-0002"});
  s.Loans.Add(new(){Id="loan",CopyId="BK-0001",DueDate="2000-01-01",Renewals=[new(){Id="renewal",Status="pending"}]});
  s.Requests.Add(new(){Id="request",CopyId="BK-0002",Status="pending"});s.Holds.Add(new(){CopyId="BK-0002",Stage="pending"});
  var accounts=new[]{new BorrowerAccount("a","borrower","Private name","private@example.com","pending","",""),new BorrowerAccount("b","blocked","Blocked","","blocked","","")};
  var items=AdminNotifications.Build(s,accounts,"2026-10-28",true,true,true,true);
  Check(items.Select(i=>i.Id).SequenceEqual(new[]{"accounts","requests","renewals","overdue","recovery","backup","email","push"}),"all operational alert categories included; pending hold not double-counted");
  Check(items.Sum(i=>i.Count)==8,"notification badge totals actionable items");
  Check(!System.Text.Json.JsonSerializer.Serialize(items).Contains("private@example.com"),"notification summaries exclude contact and private reading data");
  Check(AdminNotifications.RequestEvents(s,accounts).Order().SequenceEqual(new[]{"account|a","renewal|renewal","request|request"}),"stable background identifiers include account/book/renewal requests");
  s.Requests[0].Status="approved";s.Holds[0].Stage="ready";s.Loans[0].ReturnedDate="2026-10-28";
  items=AdminNotifications.Build(s,[],"2026-10-28");Check(items.Count==1&&items[0].Id=="holds","resolved requests, renewals and overdue items clear; approved hold remains until handover");
 }
}
