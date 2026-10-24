import SwiftUI
import MessageUI

struct ReceiptView: View {
    let receipt: Receipt
    @State private var composing = false
    var available: Bool { receipt.medium == "email" ? MFMailComposeViewController.canSendMail() : receipt.medium == "text" && MFMessageComposeViewController.canSendText() }
    var body: some View {
        VStack(alignment: .leading, spacing: 16) {
            Text("Borrowing receipt").font(.headline); Text(receipt.body).textSelection(.enabled)
            if available { Button(receipt.medium == "email" ? "Prepare email" : "Prepare text") { composing = true } }
            else { Text("Messaging isn’t configured on this phone. Copy or share the receipt.").font(.caption) }
            Button("Copy receipt") { UIPasteboard.general.string = receipt.body }
            ShareLink("Share receipt", item: receipt.body)
            Text("Review the prepared message and tap Send yourself.").font(.caption)
        }.sheet(isPresented: $composing) { MessageComposer(receipt: receipt) { composing = false } }
    }
}
struct MessageComposer: UIViewControllerRepresentable {
    let receipt: Receipt; let done: () -> Void
    func makeCoordinator() -> Coordinator { Coordinator(done: done) }
    func makeUIViewController(context: Context) -> UIViewController {
        if receipt.medium == "email" {
            let controller = MFMailComposeViewController(); controller.mailComposeDelegate = context.coordinator
            controller.setToRecipients([receipt.recipient]); controller.setSubject(receipt.subject); controller.setMessageBody(receipt.body, isHTML: false); return controller
        }
        let controller = MFMessageComposeViewController(); controller.messageComposeDelegate = context.coordinator; controller.recipients = [receipt.recipient]; controller.body = receipt.body; return controller
    }
    func updateUIViewController(_ uiViewController: UIViewController, context: Context) {}
    final class Coordinator: NSObject, MFMailComposeViewControllerDelegate, MFMessageComposeViewControllerDelegate {
        let done: () -> Void; init(done: @escaping () -> Void) { self.done = done }
        func mailComposeController(_ controller: MFMailComposeViewController, didFinishWith result: MFMailComposeResult, error: Error?) { done() }
        func messageComposeViewController(_ controller: MFMessageComposeViewController, didFinishWith result: MessageComposeResult) { done() }
    }
}
