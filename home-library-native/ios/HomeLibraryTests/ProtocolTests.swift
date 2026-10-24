import XCTest
@testable import HomeLibrary

final class ProtocolTests: XCTestCase {
    func testCirculationSelection() {
        var state = LibraryState.empty
        state.books = [
            Book(id: "BK-0001", isbn: "9780140328721", title: "Matilda", author: "Roald Dahl", publisher: "", coverUrl: "", series: nil, archived: false),
            Book(id: "BK-0002", isbn: "0140328726", title: "Matilda", author: "Roald Dahl", publisher: "", coverUrl: "", series: nil, archived: false),
            Book(id: "BK-0003", isbn: "9780140328721", title: "Matilda", author: "Roald Dahl", publisher: "", coverUrl: "", series: nil, archived: false),
            Book(id: "BK-0004", isbn: "9780140328721", title: "Matilda", author: "Roald Dahl", publisher: "", coverUrl: "", series: nil, archived: true),
            Book(id: "BK-0005", isbn: "9780547928227", title: "Other book", author: "Roald Dahl", publisher: "", coverUrl: "", series: nil, archived: false)
        ]
        state.loans = [
            Loan(id: "LN-0", copyId: "BK-0002", borrowerId: "BR-1", checkoutDate: "2026-10-01", dueDate: "", returnedDate: nil),
            Loan(id: "LN-1", copyId: "BK-0003", borrowerId: "BR-2", checkoutDate: "2026-10-02", dueDate: "", returnedDate: nil),
            Loan(id: "LN-2", copyId: "BK-0001", borrowerId: "BR-1", checkoutDate: "2026-09-01", dueDate: "", returnedDate: "2026-09-02")
        ]
        XCTAssertEqual(state.circulationMatches("9780140328721", returning: false).map(\.id), ["BK-0001"])
        XCTAssertEqual(state.circulationMatches("9780140328721", returning: true).map(\.id), ["BK-0002","BK-0003"])
        XCTAssertEqual(state.circulationMatches("0-14-032872-6", returning: true).map(\.id), ["BK-0002","BK-0003"])
        XCTAssertEqual(state.circulationMatches("BK-0002", returning: false).map(\.id), [])
        XCTAssertEqual(state.circulationMatches("bk2", returning: true).map(\.id), ["BK-0002"])
        XCTAssertEqual(state.circulationMatches("BK-0001", returning: true).map(\.id), [])
        XCTAssertEqual(state.circulationMatches("BK-0004", returning: false).map(\.id), [])
        XCTAssertEqual(state.circulationMatches("9780547928227", returning: true).map(\.id), [])
        XCTAssertEqual(state.circulationMatches("9780547928227", returning: false).map(\.id), ["BK-0005"])
        XCTAssertEqual(state.circulationMatches("9780140328722", returning: true).map(\.id), [])
        XCTAssertEqual(state.circulationMatches("", returning: false).map(\.id), [])
        XCTAssertEqual(state.circulationMatches("Matilda", returning: false).map(\.id), ["BK-0001"])
        XCTAssertEqual(state.circulationMatches("dahl", returning: true).map(\.id), ["BK-0002","BK-0003"])
        XCTAssertEqual(state.circulationMatches("BK-9999", returning: true).map(\.id), [])
        XCTAssertEqual(state.circulationMatches("9780000000002", returning: true).map(\.id), [])
    }
    func testBase64URLRoundTrip() {
        let bytes = Data([0, 255, 254, 1, 44]); XCTAssertEqual(Data(base64URL: bytes.base64URL), bytes)
        XCTAssertFalse(bytes.base64URL.contains("="))
    }
    func testSPKIUsesRSAAlgorithmAndBitString() {
        let pkcs1 = Data([0x30,0x06,0x02,0x01,0x03,0x02,0x01,0x03])
        let expected = Data([0x30,0x1a,0x30,0x0d,0x06,0x09,0x2a,0x86,0x48,0x86,0xf7,0x0d,0x01,0x01,0x01,0x05,0x00,0x03,0x09,0x00]) + pkcs1
        XCTAssertEqual(RSA.spki(pkcs1), expected)
        XCTAssertEqual(RSA.der(0x04, Data(repeating: 0, count: 256)).prefix(4), Data([0x04,0x82,0x01,0x00]))
    }
    func testPairingRejectsHTTPAndExpiredAndWrongPin() throws {
        let pin = Data(repeating: 3, count: 32)
        let format = ISO8601DateFormatter(); format.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
        var code = PairCode(code: "test", expires: format.string(from: Date().addingTimeInterval(300)), serverUrl: "https://library.example:7443", serverKey: pin.base64EncodedString(), serverId: pin.map { String(format: "%02x", $0) }.joined(), version: 1)
        func encoded() throws -> String { "hl-pair:" + (try JSONEncoder().encode(code)).base64URL }
        XCTAssertEqual(try PairCode.parse(encoded()).serverUrl, code.serverUrl)
        code.serverUrl = "http://library.example"; XCTAssertThrowsError(try PairCode.parse(encoded()))
        code.serverUrl = "https://library.example/path"; XCTAssertThrowsError(try PairCode.parse(encoded()))
        code.serverUrl = "https://library.example"; code.serverId = "wrong"; XCTAssertThrowsError(try PairCode.parse(encoded()))
        code.serverId = pin.map { String(format: "%02x", $0) }.joined(); code.expires = format.string(from: Date().addingTimeInterval(-10)); XCTAssertThrowsError(try PairCode.parse(encoded()))
    }
    func testOlderBackupAndReturnedLoans() throws {
        let json = #"{"libraryName":"Test","version":1,"nextCopy":2,"books":[{"id":"BK-0001","isbn":"9780140328721","title":"Matilda","author":"Roald Dahl","publisher":"Puffin","coverUrl":""}],"borrowers":[],"loans":[{"id":"old","copyId":"BK-0001","borrowerId":"BR-0001","checkoutDate":"2026-01-01","dueDate":"","returnedDate":"2026-01-02"}]}"#
        let state = try JSONDecoder().decode(LibraryState.self, from: Data(json.utf8))
        XCTAssertNil(state.active("BK-0001")); XCTAssertNil(state.books[0].series); XCTAssertNil(state.requests)
    }
}
