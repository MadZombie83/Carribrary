import SwiftUI
import UIKit
import UniformTypeIdentifiers
import CoreImage.CIFilterBuiltins

struct CopyLabelScreen: View {
 @EnvironmentObject var store: LibraryStore
 @State private var selection: Set<String> = []; @State private var query = ""; @State private var a4 = false; @State private var start = "1"
 @State private var document = LibraryDocument(data: Data()); @State private var exporting = false; @State private var note = ""
 var books: [Book] { store.state.books.filter { $0.archived != true } }
 var matches: [Book] { books.filter { query.isEmpty || "\($0.id) \($0.title) \($0.author)".localizedCaseInsensitiveContains(query) } }
 var body: some View {
  Form {
   Section("Copy numbering") {
    Text("Current: " + (store.state.copyNumberPolicy == "next-number" ? "Always use next number" : "Fill available gaps"))
    Text("Changes apply to future additions only. Existing numbers and history stay protected. Old BK-only labels may identify replacement copies after reuse; permanent QR labels cannot.")
    ForEach(["fill-gaps", "next-number"], id: \.self) { policy in Button(policy == "fill-gaps" ? "Fill available gaps" : "Always use next number") { Task { _ = await store.action("setCopyNumberPolicy", ["policy": policy]) } }.disabled(!store.online || store.busy || store.hasPending) }
   }
   Section("Optional labels") {
    Text("ISBN scanning works without labels. These QR codes identify physical copies permanently and contain no account or pairing secrets.")
    Toggle("A4 · 24 labels (63.5 × 33.9 mm)", isOn: $a4).onChange(of: a4) { _, _ in start = "1" }
    Text(a4 ? "A4 sheet" : "Letter · 30 labels (Avery 5160 geometry)")
    TextField("First unused position (across, then down)", text: $start).keyboardType(.numberPad)
    TextField("Find title, author or BK number", text: $query)
    Text("\(selection.count) selected · maximum 500 per batch")
    Button("Select matching books") { selection = Set(Array(selection.union(matches.map(\.id))).prefix(500)) }
    Button("Clear selection") { selection = [] }
    ForEach(matches) { book in Toggle(book.id + " · " + book.title, isOn: Binding(get: { selection.contains(book.id) }, set: { checked in if checked && selection.count < 500 { selection.insert(book.id) } else { selection.remove(book.id) } })) }
    Button("Save label PDF") { do { document = LibraryDocument(data: try labelPDF(books.filter { selection.contains($0.id) }, library: store.state.libraryName, a4: a4, start: Int(start) ?? 0)); exporting = true } catch { note = error.localizedDescription } }.disabled(selection.isEmpty)
    Text("Print at Actual size / 100%. Test on plain paper against your sheet first. No custom designer or print-history tracking yet.")
   }
   if !note.isEmpty { Text(note) }; if let error = store.error { Text(error) }
  }.navigationTitle("Copy labels & numbering").fileExporter(isPresented: $exporting, document: document, contentType: .pdf, defaultFilename: "Carribrary-copy-labels") { result in if case .failure(let error) = result { note = error.localizedDescription } }
 }
}
func labelPDF(_ books: [Book], library: String, a4: Bool, start: Int) throws -> Data {
 let rows = a4 ? 8 : 10
 guard (1...rows * 3).contains(start), (1...500).contains(books.count), books.allSatisfy({ ($0.copyUid ?? "").range(of: "^[a-f0-9]{32}$", options: .regularExpression) != nil }) else { throw AppError.message("Choose 1–500 current copies and a valid starting position.") }
 let mm = 72.0 / 25.4; let width = (a4 ? 63.5 : 66.675) * mm; let height = (a4 ? 33.9 : 25.4) * mm
 let left = (a4 ? 7.25 : 4.7625) * mm; let top = (a4 ? 12.9 : 12.7) * mm; let gap = (a4 ? 2.5 : 3.175) * mm
 let renderer = UIGraphicsPDFRenderer(bounds: CGRect(x: 0, y: 0, width: a4 ? 595.2756 : 612, height: a4 ? 841.8898 : 792))
 return renderer.pdfData { context in
  for page in 0..<((start - 1 + books.count + rows * 3 - 1) / (rows * 3)) {
   context.beginPage()
   for slot in 0..<rows * 3 {
    let index = page * rows * 3 + slot - (start - 1); guard books.indices.contains(index) else { continue }; let book = books[index]
    let x = left + Double(slot % 3) * (width + gap); let y = top + Double(slot / 3) * height
    let filter = CIFilter.qrCodeGenerator(); filter.message = Data(("carribrary:copy:v1:" + (book.copyUid ?? "")).utf8); filter.correctionLevel = "M"
    if let image = filter.outputImage, let cg = CIContext().createCGImage(image.transformed(by: CGAffineTransform(scaleX: 10, y: 10)), from: image.extent.applying(CGAffineTransform(scaleX: 10, y: 10))) {
     // Reserve a white quiet zone around the symbol within the 23 mm square.
     context.cgContext.interpolationQuality = .none
     UIImage(cgImage: cg).draw(in: CGRect(x: x + 4 * mm, y: y + (height - 17 * mm) / 2, width: 17 * mm, height: 17 * mm))
    }
    (book.id as NSString).draw(in: CGRect(x: x + 25 * mm, y: y + mm, width: width - 26 * mm, height: 5 * mm), withAttributes: [.font: UIFont.boldSystemFont(ofSize: 11), .foregroundColor: UIColor.black])
    (book.title as NSString).draw(in: CGRect(x: x + 25 * mm, y: y + 7 * mm, width: width - 26 * mm, height: 9 * mm), withAttributes: [.font: UIFont.systemFont(ofSize: 8), .foregroundColor: UIColor.black])
    (library as NSString).draw(in: CGRect(x: x + 25 * mm, y: y + 17 * mm, width: width - 26 * mm, height: 5 * mm), withAttributes: [.font: UIFont.systemFont(ofSize: 7), .foregroundColor: UIColor.black])
   }
  }
 }
}
