import SwiftUI
import UniformTypeIdentifiers
import CoreImage.CIFilterBuiltins

struct LibraryDocument: FileDocument {
    static var readableContentTypes: [UTType] { [.json, .commaSeparatedText, .plainText, .data] }
    var data: Data
    init(data: Data) { self.data = data }
    init(configuration: ReadConfiguration) throws { data = configuration.file.regularFileContents ?? Data() }
    func fileWrapper(configuration: WriteConfiguration) throws -> FileWrapper { FileWrapper(regularFileWithContents: data) }
}
struct Administration: View {
    @EnvironmentObject var store: LibraryStore; @Environment(\.dismiss) var dismiss
    @AppStorage("palette") var palette = Palette.ocean.rawValue
    @State private var name = ""; @State private var subtitle = ""; @State private var saying = ""
    @State private var sharing: Sharing?; @State private var devices: Devices?
    @State private var qrText: String?; @State private var qrTitle = ""; @State private var note = ""
    @State private var document = LibraryDocument(data: Data()); @State private var exporting = false
    @State private var contentType: UTType = .json; @State private var filename = "HomeLibrary-backup"
    @State private var importing = false; @State private var restore: LibraryState?
    @State private var sharingAction: String?; @State private var revoke: Device?
    @State private var backupPassword = ""; @State private var backupConfirmation = ""; @State private var completeBusy = false
    @State private var loanHistory = false
    @State private var collectionName = ""; @State private var editingCollection = ""
    var body: some View {
        NavigationStack {
            Form {
                Section("People and help") {
                    NavigationLink("Data & privacy / clear history") { DataControls().environmentObject(store) }
                    NavigationLink("Lending & holds / batch lending") { LendingTools().environmentObject(store) }
                    NavigationLink("Borrower accounts & approvals") { BorrowerAccountAdministration().environmentObject(store) }
                    NavigationLink("Borrowers") { List(store.state.borrowers.filter { $0.archived != true }) { BorrowerRow(borrower: $0).environmentObject(store) } }
                    NavigationLink("Recycle Bin") { RecycleBinScreen().environmentObject(store) }
                    NavigationLink("Copy labels & numbering") { CopyLabelScreen().environmentObject(store) }
                    NavigationLink("Bulk edit books") { BulkBookEditor().environmentObject(store) }
                    NavigationLink("Email reminders") { ReminderSettingsScreen().environmentObject(store) }
                    NavigationLink("Setup & updates") { SetupChecklistScreen().environmentObject(store) }
                    NavigationLink("Help") { HelpScreen() }
                }
                Section("Acquisitions") { NavigationLink("Books to buy") { AcquisitionList().environmentObject(store) } }
                Section("Collections") {
                    Text("Named collections share borrowers and loans. There is no fixed limit.").font(.caption)
                    ForEach(store.state.availableCollections) { item in
                        HStack { Text(item.name); Spacer(); Button("Rename") { editingCollection = item.id; collectionName = item.name }; if item.id != "general" { Button("Delete empty") { Task { _ = await store.action("removeCollection", ["collectionId": item.id]) } } } }
                    }
                    TextField("Collection name", text: $collectionName)
                    Button(editingCollection.isEmpty ? "Add collection" : "Save name") { Task { if await store.action(editingCollection.isEmpty ? "addCollection" : "renameCollection", ["name": collectionName, "collectionId": editingCollection]) { collectionName = ""; editingCollection = "" } } }.disabled(store.busy || !store.online || collectionName.isEmpty)
                    if !editingCollection.isEmpty { Button("Add a new collection") { editingCollection = ""; collectionName = "" } }
                }
                Section("Appearance") {
                    Picker("Colors", selection: $palette) { ForEach(Palette.allCases, id: \.rawValue) { Text($0.rawValue).tag($0.rawValue) } }
                    TextField("Library name", text: $name)
                    Button("Save library name") { Task { _ = await store.action("setLibraryName", ["libraryName": name]) } }.disabled(!canChange)
                    TextField("Banner saying (up to 160 characters)", text: $subtitle)
                    TextField("Encouraging saying (up to 240 characters)", text: $saying, axis: .vertical)
                    Button("Save sayings") { Task { _ = await store.action("setLibraryPresentation", ["bannerSubtitle": subtitle, "encouragingMessage": saying]) } }.disabled(!canChange)
                    Text("The name and sayings also appear on the shared catalog. Colors are saved on this phone.").font(.caption)
                }
                Section("Library sharing") {
                    if let sharing {
                        if sharing.configured {
                            if sharing.enabled, let raw = sharing.url, let url = URL(string: raw) {
                                ShareLink("Share book list", item: url)
                                Button("Show library QR") { qrTitle = "Shared catalog"; qrText = raw }
                                Button("Replace sharing link") { sharingAction = "replace" }.disabled(!canChange)
                                Button("Disable sharing", role: .destructive) { sharingAction = "disable" }.disabled(!canChange)
                            } else { Button("Enable public catalog") { sharingAction = "enable" }.disabled(!canChange) }
                        } else { Text("Configure the public catalog address on the server first.") }
                    }
                    Button("Refresh sharing status") { Task { await loadSharing() } }
                }
                Section("Requests") {
                    ForEach((store.state.requests ?? []).filter { $0.status == "pending" || $0.status == "approved" }) { request in
                        RequestRow(request: request).environmentObject(store)
                    }
                    if !(store.state.requests ?? []).contains(where: { $0.status == "pending" || $0.status == "approved" }) { Text("No open requests") }
                }
                Section("Data") {
                    Button("Loan history") { loanHistory = true }
                    Button("Export CSV") { exportCSV() }
                    Text("Complete server data backup").font(.headline)
                    Text("Includes current accounts, encrypted borrower vaults, pairing keys, keyring and saved settings. Changes pause briefly for a consistent snapshot. Keep the backup password safely; it cannot be reset. Windows-protected keys/settings require the same computer.").font(.caption)
                    SecureField("Backup password (12–128 characters)", text: $backupPassword).disabled(completeBusy)
                    SecureField("Confirm password", text: $backupConfirmation).disabled(completeBusy)
                    Button(completeBusy ? "Preparing complete backup…" : "Create complete backup") {
                        completeBusy = true
                        Task {
                            do { let bytes = try await store.api("/mobile/backup/complete", body: ["password": backupPassword]); document = LibraryDocument(data: bytes); contentType = .data; filename = "Carribrary-complete.carribrary-backup"; exporting = true }
                            catch { note = error.localizedDescription }
                            backupPassword = ""; backupConfirmation = ""; completeBusy = false
                        }
                    }.disabled(!canChange || completeBusy || backupPassword.count < 12 || backupPassword.count > 128 || backupPassword != backupConfirmation)
                    Text("Current data only; earlier backups, external HTTPS/proxy settings, system services/tasks and environment-only secrets are separate. Offline recovery requires a stopped server. Borrowers still need their private-list passphrases. Use the server computer for large backups.").font(.caption)
                    Button("Export library JSON") {
                        do { document = LibraryDocument(data: try JSONEncoder().encode(store.state)); contentType = .json; filename = "HomeLibrary-backup"; exporting = true } catch { note = error.localizedDescription }
                    }
                    Button("Restore library export") { importing = true }.disabled(!canChange)
                    Text("Exported files include borrower contacts and loan history. Share backups only with people you trust.").font(.caption)
                }
                Section("Paired phones") {
                    Button("Add another phone") { Task { await addPhone() } }.disabled(!canChange)
                    if let devices {
                        ForEach(devices.devices) { device in
                            HStack { VStack(alignment: .leading) { Text(device.name); Text(device.revoked ? "Revoked" : device.id == devices.currentDeviceId ? "This phone" : "Paired").font(.caption) }; Spacer(); if !device.revoked { Button("Revoke", role: .destructive) { revoke = device }.disabled(!canChange) } }
                        }
                    }
                    Button("Refresh paired phones") { Task { await loadDevices() } }
                }
                if !note.isEmpty { Text(note) }; if let error = store.error { Text(error).foregroundStyle(.red) }
            }.navigationTitle("Library settings").toolbar { Button("Done") { dismiss() }.disabled(completeBusy) }
                .task { name = store.state.libraryName; subtitle = store.state.bannerSubtitle ?? ""; saying = store.state.encouragingMessage ?? ""; await loadSharing(); await loadDevices() }
                .sheet(isPresented: Binding(get: { qrText != nil }, set: { if !$0 { qrText = nil } })) { NavigationStack { if let qrText { QRView(text: qrText).navigationTitle(qrTitle).toolbar { Button("Done") { self.qrText = nil } } } } }
                .sheet(isPresented: $loanHistory) { NavigationStack { List(store.state.loans.reversed()) { loan in VStack(alignment: .leading) { Text(store.state.books.first { $0.id == loan.copyId }?.title ?? loan.copyId).font(.headline); Text("\(loan.copyId) · \(store.state.borrowers.first { $0.id == loan.borrowerId }?.name ?? loan.borrowerId)"); Text("Borrowed \(loan.checkoutDate) · Due \(loan.dueDate.isEmpty ? "not set" : loan.dueDate)"); Text(loan.returnedDate.map { "Returned \($0)" } ?? "On loan") }.font(.subheadline) }.navigationTitle("Loan history").toolbar { Button("Done") { loanHistory = false } } } }
                .fileExporter(isPresented: $exporting, document: document, contentType: contentType, defaultFilename: filename) { result in if case .failure(let error) = result { note = error.localizedDescription } }
                .fileImporter(isPresented: $importing, allowedContentTypes: [.json]) { result in
                    do {
                        let url = try result.get(); let access = url.startAccessingSecurityScopedResource(); defer { if access { url.stopAccessingSecurityScopedResource() } }
                        let size = try url.resourceValues(forKeys: [.fileSizeKey]).fileSize ?? 0
                        guard size < 20_000_000 else { throw AppError.message("Backup exceeds the 20 MB import limit.") }
                        restore = try JSONDecoder().decode(LibraryState.self, from: Data(contentsOf: url))
                    } catch { note = error.localizedDescription }
                }
                .confirmationDialog("Replace the current library with this backup?", isPresented: Binding(get: { restore != nil }, set: { if !$0 { restore = nil } }), titleVisibility: .visible) {
                    Button("Restore library", role: .destructive) { if let restore { Task { do { let json = try JSONSerialization.jsonObject(with: JSONEncoder().encode(restore)) as! [String: Any]; _ = await store.action("restore", json) } catch { note = error.localizedDescription }; self.restore = nil } } }
                } message: { Text("Export a backup of your current library first. The server validates imported records.") }
                .confirmationDialog("Change public sharing?", isPresented: Binding(get: { sharingAction != nil }, set: { if !$0 { sharingAction = nil } }), titleVisibility: .visible) {
                    Button("Continue") { if let action = sharingAction { Task { do { sharing = try JSONDecoder().decode(Sharing.self, from: await store.api("/mobile/sharing", body: ["action": action])) } catch { note = error.localizedDescription } } }; sharingAction = nil }
                } message: { Text(sharingAction == "enable" ? "Anyone with the link can view books and submit requests. Borrower contacts and loan history remain private." : "Existing shared links and QR codes will stop working.") }
                .confirmationDialog("Revoke \(revoke?.name ?? "phone")?", isPresented: Binding(get: { revoke != nil }, set: { if !$0 { revoke = nil } }), titleVisibility: .visible) {
                    Button("Revoke phone", role: .destructive) { if let device = revoke { Task { do { _ = try await store.api("/mobile/devices/revoke", body: ["deviceId": device.id]); await loadDevices(); if device.id == store.pairing?.deviceId { store.online = false; note = "This phone was revoked. Pair again to reconnect." } } catch { note = error.localizedDescription } } }; revoke = nil }
                }
        }
    }
    var canChange: Bool { store.online && !store.busy && !store.hasPending }
    func loadSharing() async { do { sharing = try JSONDecoder().decode(Sharing.self, from: await store.api("/mobile/sharing")) } catch { note = error.localizedDescription } }
    func loadDevices() async { do { devices = try JSONDecoder().decode(Devices.self, from: await store.api("/mobile/devices")) } catch { note = error.localizedDescription } }
    func addPhone() async {
        do {
            guard let pairing = store.pairing else { return }
            let data = try await store.api("/mobile/devices/pair", body: ["serverUrl": pairing.serverUrl])
            let code = try JSONDecoder().decode(PairCode.self, from: data)
            qrTitle = "Private phone pairing"; qrText = "hl-pair:" + (try JSONEncoder().encode(code)).base64URL
        } catch { note = error.localizedDescription }
    }
    func exportCSV() {
        func cell(_ value: String) -> String { let safe = ["=", "+", "-", "@", "\t", "\r"].contains(where: { value.hasPrefix($0) }) ? "'" + value : value; return "\"" + safe.replacingOccurrences(of: "\"", with: "\"\"") + "\"" }
        let rows = store.state.books.filter { $0.archived != true }.map { book in [book.id, book.isbn, book.title, book.author, book.publisher, book.series ?? "", book.coverUrl, store.state.availableCollections.first { $0.id == (book.collectionId ?? "general") }?.name ?? "General", store.state.active(book.id) == nil ? "Available" : "On loan"].map(cell).joined(separator: ",") }
        document = LibraryDocument(data: Data((["Copy ID,ISBN,Title,Author,Publisher,Series,Cover URL,Collection,Status"] + rows).joined(separator: "\r\n").utf8)); contentType = .commaSeparatedText; filename = "HomeLibrary-catalog"; exporting = true
    }
}
struct RequestRow: View {
    @EnvironmentObject var store: LibraryStore; let request: BookRequest
    @State private var due = ""; @State private var confirmDecline = false; @State private var receipt: Receipt?
    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text(store.state.books.first { $0.id == request.copyId }?.title ?? request.copyId).font(.headline)
            Text("\(request.name) · \(request.contact)"); Text(request.message); Text(request.status.capitalized).font(.caption)
            if request.status == "pending" || (request.status == "approved" && !(store.state.holds ?? []).contains(where: { $0.requestId == request.id })) { Button("Approve / reserve") { Task { _ = await store.action("approveRequest", ["requestId": request.id]) } } }
            if request.status == "approved" && (store.state.holds ?? []).contains(where: { $0.requestId == request.id }) {
                TextField("Due date YYYY-MM-DD (optional)", text: $due)
                Button("Hand over and check out") { Task { if await store.action("fulfillRequest", ["requestId": request.id, "checkoutDate": LibraryStore.today, "dueDate": due]) { receipt = try? JSONDecoder().decode(Receipt.self, from: await store.api("/mobile/receipts/" + request.copyId)) } } }
            }
            Button("Decline", role: .destructive) { confirmDecline = true }
        }.disabled(!store.online || store.busy || store.hasPending)
            .confirmationDialog("Decline this request?", isPresented: $confirmDecline, titleVisibility: .visible) { Button("Decline", role: .destructive) { Task { _ = await store.action("declineRequest", ["requestId": request.id]) } } }
            .sheet(isPresented: Binding(get: { receipt != nil }, set: { if !$0 { receipt = nil } })) { NavigationStack { if let receipt { ReceiptView(receipt: receipt).padding().toolbar { Button("Done") { self.receipt = nil } } } } }
    }
}
struct QRView: View {
    let text: String
    var image: UIImage? {
        let filter = CIFilter.qrCodeGenerator(); filter.message = Data(text.utf8); filter.correctionLevel = "M"
        guard let output = filter.outputImage, let image = CIContext().createCGImage(output.transformed(by: CGAffineTransform(scaleX: 8, y: 8)), from: output.extent.applying(CGAffineTransform(scaleX: 8, y: 8))) else { return nil }
        return UIImage(cgImage: image)
    }
    var body: some View { VStack(spacing: 24) { if let image { Image(uiImage: image).interpolation(.none).resizable().scaledToFit().padding(24).background(.white).frame(maxWidth: 360) }; Text(text.hasPrefix("hl-pair:") ? "Private pairing code. Share only with a phone you approve. Expires in five minutes." : "Scan to browse the shared library and request a book.").padding(); if !text.hasPrefix("hl-pair:"), let url = URL(string: text) { ShareLink("Share link", item: url) } }.padding() }
}
