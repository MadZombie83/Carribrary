import Foundation

struct Book: Codable, Identifiable {
    var copyUid: String? = nil
    var room: String? = nil; var bookcase: String? = nil; var shelf: String? = nil
    var description: String? = nil; var genre: String? = nil
    var id: String; var isbn: String; var title: String; var author: String
    var publisher: String; var coverUrl: String; var series: String?; var archived: Bool?; var collectionId: String? = nil
}
struct LibraryCollection: Codable, Identifiable { var id: String; var name: String }
struct Borrower: Codable, Identifiable { var id: String; var name: String; var contact: String; var archived: Bool?; var reminderConsent: Bool? = nil }
struct Loan: Codable, Identifiable {
    var id: String; var copyId: String; var borrowerId: String
    var checkoutDate: String; var dueDate: String; var returnedDate: String?
}
struct BookRequest: Codable, Identifiable {
    var id: String; var copyId: String; var name: String; var contact: String
    var message: String; var submittedAt: String; var status: String; var loanId: String
}
struct CopyHold: Codable, Identifiable { var id: String; var copyId: String; var requestId: String; var borrowerId: String; var stage: String; var createdAt: String }
struct Acquisition: Codable, Identifiable { var series: String? = nil; var description: String? = nil; var id: String; var isbn: String; var title: String; var author: String; var publisher: String; var coverUrl: String; var notes: String; var addedDate: String }
struct LibraryState: Codable {
    var booksToBuy: [Acquisition]? = nil
    var holds: [CopyHold]? = nil; var circulationVersion: Int? = nil; var maxBatchCheckout: Int? = nil; var maxActiveLoans: Int? = nil
    var libraryTheme: String? = nil; var libraryIcon192: String? = nil; var libraryIcon512: String? = nil
    var libraryName: String; var bannerSubtitle: String?; var encouragingMessage: String?
    var copyNumberPolicy: String? = nil
    var version: Int; var nextCopy: Int; var books: [Book]; var borrowers: [Borrower]
    var loans: [Loan]; var requests: [BookRequest]?; var collections: [LibraryCollection]? = nil
    var readers: [ReaderProfile]? = nil; var readingEntries: [ReadingEntry]? = nil
    var recycleBin: [DeletedLibraryRecord]? = nil
    var availableCollections: [LibraryCollection] { collections ?? [LibraryCollection(id: "general", name: "General")] }
    static let empty = LibraryState(libraryName: "Carribrary", bannerSubtitle: "A shelf worth sharing", encouragingMessage: "A place for every book.", version: 1, nextCopy: 1, books: [], borrowers: [], loans: [], requests: [])
    func active(_ id: String) -> Loan? { loans.first { $0.copyId == id && $0.returnedDate == nil } }
}
struct Snapshot: Codable { var revision: Int64; var state: LibraryState }
struct PairCode: Codable {
    var code: String; var expires: String; var serverUrl: String; var serverKey: String; var serverId: String; var version: Int
    static func parse(_ text: String) throws -> PairCode {
        guard text.utf8.count < 8192, text.hasPrefix("hl-pair:"), let data = Data(base64URL: String(text.dropFirst(8))) else { throw AppError.message("Scan the private Add Phone QR code from your library server.") }
        let pair = try JSONDecoder().decode(Self.self, from: data)
        guard pair.version == 1, let url = URL(string: pair.serverUrl), url.scheme == "https", url.host != nil,
              url.user == nil, url.password == nil, url.query == nil, url.fragment == nil, url.path.isEmpty || url.path == "/",
              let pin = Data(base64Encoded: pair.serverKey), pin.count == 32,
              pin.map({ String(format: "%02x", $0) }).joined() == pair.serverId else { throw AppError.message("Invalid pairing address or server identity.") }
        let formatter = ISO8601DateFormatter(); formatter.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
        guard let expires = formatter.date(from: pair.expires), expires > Date() else { throw AppError.message("This pairing code has expired. Create a new code on the server.") }
        return pair
    }
}
struct Pairing: Codable { var serverUrl: String; var serverKey: String; var deviceId: String; var certificate: Data; var caCertificate: Data; var keyTag: String }
struct Enrollment: Decodable { var deviceId: String; var certificate: String; var caCertificate: String; var serverId: String }
struct Receipt: Decodable { var copyId: String; var borrowerName: String; var medium: String; var recipient: String; var subject: String; var body: String; var dueDate: String }
struct Sharing: Decodable { var configured: Bool; var enabled: Bool; var url: String? }
struct Device: Decodable, Identifiable { var id: String; var name: String; var issued: String; var revoked: Bool }
struct Devices: Decodable { var devices: [Device]; var currentDeviceId: String }
enum AppError: LocalizedError {
    case message(String), response(Int, String)
    var errorDescription: String? { switch self { case .message(let text): return text; case .response(_, let text): return text } }
}
extension Data {
    init?(base64URL: String) {
        var value = base64URL.replacingOccurrences(of: "-", with: "+").replacingOccurrences(of: "_", with: "/")
        value += String(repeating: "=", count: (4 - value.count % 4) % 4); self.init(base64Encoded: value)
    }
    var base64URL: String { base64EncodedString().replacingOccurrences(of: "+", with: "-").replacingOccurrences(of: "/", with: "_").replacingOccurrences(of: "=", with: "") }
}

struct CoverCandidate: Decodable, Identifiable { var url: String; var source: String; var sourceUrl: String; var title: String; var author: String; var exactEdition: Bool; var id: String { url } }
struct CoverResult: Decodable { var covers: [CoverCandidate]; var message: String }

// Manual ISBN-10 and printed ISBN-13 identify the same edition; copy IDs remain exact.
func circulationCode(_ raw: String) -> String { raw.uppercased().filter { !$0.isWhitespace && $0 != "-" } }
func validCirculationISBN(_ raw: String) -> Bool {
    let code = circulationCode(raw); let chars = Array(code)
    if code.count == 10, code.prefix(9).allSatisfy({ $0.isASCII && $0.isNumber }), let last = chars.last, last.isNumber || last == "X" {
        return chars.enumerated().reduce(0) { $0 + ($1.element == "X" ? 10 : ($1.element.wholeNumberValue ?? -100)) * (10 - $1.offset) } % 11 == 0
    }
    if code.count == 13, chars.allSatisfy({ $0.isASCII && $0.isNumber }) { return chars.enumerated().reduce(0) { $0 + ($1.element.wholeNumberValue ?? -100) * ($1.offset % 2 == 0 ? 1 : 3) } % 10 == 0 }
    return false
}
func circulationEdition(_ raw: String) -> String {
    let code = circulationCode(raw); guard code.count == 10 else { return code }
    let prefix = "978" + String(code.prefix(9)); let sum = prefix.enumerated().reduce(0) { $0 + ($1.element.wholeNumberValue ?? 0) * ($1.offset % 2 == 0 ? 1 : 3) }
    return prefix + String((10 - sum % 10) % 10)
}
extension LibraryState {
    func circulationMatches(_ raw: String, returning: Bool) -> [Book] {
        let code = circulationCode(raw); guard !code.isEmpty else { return [] }
        let suffix = String(code.dropFirst(2)); let isCopy = code.hasPrefix("BK") && !suffix.isEmpty && suffix.allSatisfy({ $0.isASCII && $0.isNumber })
        let copyID = "BK-" + String(repeating: "0", count: max(0, 4 - suffix.count)) + suffix
        return books.filter { book in
            guard book.archived != true, (active(book.id) != nil) == returning, returning || !(holds ?? []).contains(where: { $0.copyId == book.id }) else { return false }
            if raw.trimmingCharacters(in: .whitespacesAndNewlines).lowercased().hasPrefix("carribrary:") {
                let value = raw.trimmingCharacters(in: .whitespacesAndNewlines).lowercased()
                return value.range(of: "^carribrary:copy:v1:[a-f0-9]{32}$", options: .regularExpression) != nil && book.copyUid == String(value.suffix(32))
            }
            if isCopy { return book.id == copyID }
            if code.allSatisfy({ $0.isNumber || $0 == "X" }) { return validCirculationISBN(code) && circulationEdition(book.isbn) == circulationEdition(code) }
            return "\(book.title) \(book.author) \(book.series ?? "")".localizedCaseInsensitiveContains(raw.trimmingCharacters(in: .whitespacesAndNewlines))
        }
    }
}
