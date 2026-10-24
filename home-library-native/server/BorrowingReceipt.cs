using System.Net.Mail;
using System.Text.RegularExpressions;
namespace HomeLibrary;
public sealed record ReceiptDraft(string CopyId,string BorrowerName,string Medium,string Recipient,string Subject,string Body,string DueDate);
public static class BorrowingReceipt {
 public static ReceiptDraft Create(LibraryState state,string copyId){
  var loan=Library.Active(state,copyId)??throw new LibraryError("No active loan found for that copy ID.");
  var book=state.Books.Find(b=>b.Id==loan.CopyId)??throw new LibraryError("Book copy not found.");
  var borrower=state.Borrowers.Find(b=>b.Id==loan.BorrowerId)??throw new LibraryError("Borrower not found.");
  var contact=borrower.Contact.Trim();var medium="copy";var recipient="";
  // Only a single recipient is accepted. Unknown/mixed contacts stay copy-only.
  if(!contact.Any(char.IsControl)&&MailAddress.TryCreate(contact,out var email)&&email.Address.Equals(contact,StringComparison.OrdinalIgnoreCase)&&contact.Contains('@')){medium="email";recipient=email.Address;}
  else if(Regex.IsMatch(contact,@"^\+?[0-9 ().-]+$")){
   var digits=Regex.Replace(contact,"[^0-9]","");if(digits.Length is >=7 and <=15){medium="text";recipient=(contact.StartsWith('+')?"+":"")+digits;}
  }
  string Line(string value)=>Regex.Replace(value,@"[\r\n]+"," ").Trim();
  var name=Line(state.LibraryName);var title=Line(book.Title);var person=Line(borrower.Name);
  var body=$"{name} — borrowing receipt\n\nHi {person},\n\nBook: {title}"+(book.Author==""?"":"\nAuthor: "+Line(book.Author))+$"\nCopy: {book.Id}\nBorrowed: {loan.CheckoutDate}\n"+(loan.DueDate==""?"Return date: No date set. Please contact the library to arrange a return.":$"Please return by: {loan.DueDate}")+$"\n\nThank you!\n{name}";
  return new(book.Id,person,medium,recipient,$"{name}: borrowing receipt for {title}",body,loan.DueDate);
 }
}
