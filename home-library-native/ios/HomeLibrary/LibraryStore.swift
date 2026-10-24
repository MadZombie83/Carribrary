import Foundation
import CryptoKit
import SwiftUI
import UIKit

@MainActor final class LibraryStore: ObservableObject {
    @Published var snapshot = Snapshot(revision: 0, state: .empty)
    @Published var online = false; @Published var busy = false; @Published var error: String?
    @Published var pairing: Pairing?
    @Published var hasPending = false
    private var pending: Data?
    private let directory: URL
    private var cacheKey: SymmetricKey?
    init() {
        directory = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0].appendingPathComponent("HomeLibrary", isDirectory: true)
        do {
            try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
            var folder = directory; var values = URLResourceValues(); values.isExcludedFromBackup = true; try folder.setResourceValues(values)
            let keyBytes = Vault.read("cache-key") ?? SymmetricKey(size: .bits256).withUnsafeBytes { Data($0) }
            try Vault.write("cache-key", keyBytes); cacheKey = SymmetricKey(data: keyBytes)
            if let bytes = Vault.read("pairing") { pairing = try JSONDecoder().decode(Pairing.self, from: bytes) }
            if let data = try readFile("catalog") { snapshot = try JSONDecoder().decode(Snapshot.self, from: data); let hidden = Set((snapshot.state.readers ?? []).filter { !($0.borrowerId ?? "").isEmpty }.map(\.id)); snapshot.state.readers = (snapshot.state.readers ?? []).filter { !hidden.contains($0.id) }; snapshot.state.readingEntries = (snapshot.state.readingEntries ?? []).filter { !hidden.contains($0.readerId) }; try saveFile("catalog", data: JSONEncoder().encode(snapshot)) }
            pending = try readFile("pending"); hasPending = pending != nil
        } catch { self.error = error.localizedDescription }
    }
    private func readFile(_ name: String) throws -> Data? {
        let url = directory.appendingPathComponent(name)
        guard FileManager.default.fileExists(atPath: url.path), let cacheKey else { return nil }
        return try AES.GCM.open(AES.GCM.SealedBox(combined: Data(contentsOf: url)), using: cacheKey)
    }
    private func saveFile(_ name: String, data: Data) throws {
        guard let cacheKey, let sealed = try AES.GCM.seal(data, using: cacheKey).combined else { throw AppError.message("Secure cache unavailable.") }
        try sealed.write(to: directory.appendingPathComponent(name), options: [.atomic, .completeFileProtection])
    }
    func api(_ path: String, body: [String: Any]? = nil) async throws -> Data {
        guard let pairing else { throw AppError.message("Pair this phone with your library server first.") }
        let client = try SecureSession(url: pairing.serverUrl, pin: pairing.serverKey, pairing: pairing)
        return try await client.request(path, body: body.map { try JSONSerialization.data(withJSONObject: $0) })
    }
    func pair(_ text: String) async {
        guard !busy else { return }
        guard !hasPending else { error = "Retry the pending change before pairing with another server."; return }
        busy = true; defer { busy = false }
        do {
            let code = try PairCode.parse(text); let old = pairing
            pairing = try await PairingService.enroll(code, name: UIDevice.current.name)
            if let old { Vault.deleteIdentity(old) }
            snapshot = Snapshot(revision: 0, state: .empty)
            try saveFile("catalog", data: JSONEncoder().encode(snapshot))
            error = nil
        } catch { self.error = error.localizedDescription }
        busy = false
        await refresh()
    }
    func refresh() async {
        guard pairing != nil, !busy else { return }
        busy = true; defer { busy = false }
        do {
            if let pending { try await sendPending(pending) }
            snapshot = try JSONDecoder().decode(Snapshot.self, from: await api("/mobile/state"))
            try saveFile("catalog", data: JSONEncoder().encode(snapshot)); online = true; error = nil
        } catch { online = false; self.error = error.localizedDescription }
    }
    func clearLocalDownloads() throws {
        guard !hasPending, !busy else { throw AppError.message("Resolve pending changes first.") }
        let cache = directory.appendingPathComponent("catalog"); if FileManager.default.fileExists(atPath: cache.path) { try FileManager.default.removeItem(at: cache) }; snapshot = Snapshot(revision: 0, state: .empty); online = false
    }
    func acceptAccountDeletion(_ latest:Snapshot) throws {
        snapshot=latest
        let cache=directory.appendingPathComponent("catalog")
        if FileManager.default.fileExists(atPath:cache.path){try FileManager.default.removeItem(at:cache)}
        try saveFile("catalog",data:JSONEncoder().encode(latest))
    }
    func action(_ action: String, _ input: [String: Any]) async -> Bool {
        guard !busy, online, !hasPending else { error = "Connect and resolve any pending change first."; return false }
        busy = true; defer { busy = false }
        do {
            let body = try JSONSerialization.data(withJSONObject: ["action": action, "input": input, "revision": snapshot.revision, "requestId": UUID().uuidString.lowercased()])
            try saveFile("pending", data: body); pending = body; hasPending = true
            try await sendPending(body); error = nil; return true
        } catch { self.error = error.localizedDescription; busy = false; await refreshAfterConflict(error); return false }
    }
    private func refreshAfterConflict(_ error: Error) async {
        if let failure = error as? AppError, case .response(409, _) = failure { await refresh(); self.error = "Another device changed the library. The catalog was refreshed; review your change before trying again." }
    }
    private func sendPending(_ body: Data) async throws {
        guard let pairing else { throw AppError.message("No paired server.") }
        do {
            let client = try SecureSession(url: pairing.serverUrl, pin: pairing.serverKey, pairing: pairing)
            let data = try await client.request("/mobile/action", body: body)
            snapshot = try JSONDecoder().decode(Snapshot.self, from: data)
            try saveFile("catalog", data: JSONEncoder().encode(snapshot))
            try clearPending(); online = true
        } catch let error as AppError {
            if case .response(let status, _) = error, (400..<500).contains(status) { try clearPending() }
            throw error
        } catch { online = false; throw error }
    }
    private func clearPending() throws {
        let path = directory.appendingPathComponent("pending")
        if FileManager.default.fileExists(atPath: path.path) { try FileManager.default.removeItem(at: path) }
        pending = nil; hasPending = false
    }
    var state: LibraryState { snapshot.state }
    nonisolated static var today: String {
        let format = DateFormatter(); format.calendar = Calendar(identifier: .gregorian); format.locale = Locale(identifier: "en_US_POSIX"); format.dateFormat = "yyyy-MM-dd"; return format.string(from: Date())
    }
}
