import Foundation
import Security
import CryptoKit

// The server protocol uses RSA SPKI; SecKey exports PKCS#1 on Apple platforms.
enum RSA {
    static func der(_ tag: UInt8, _ bytes: Data) -> Data {
        let length: [UInt8]
        if bytes.count < 128 { length = [UInt8(bytes.count)] }
        else {
            var number = bytes.count; var encoded: [UInt8] = []
            while number > 0 { encoded.insert(UInt8(number & 255), at: 0); number >>= 8 }
            length = [0x80 | UInt8(encoded.count)] + encoded
        }
        return Data([tag] + length) + bytes
    }
    static func spki(_ pkcs1: Data) -> Data {
        let algorithm = Data([0x30,0x0d,0x06,0x09,0x2a,0x86,0x48,0x86,0xf7,0x0d,0x01,0x01,0x01,0x05,0x00])
        return der(0x30, algorithm + der(0x03, Data([0]) + pkcs1))
    }
    static func external(_ key: SecKey) throws -> Data {
        var error: Unmanaged<CFError>?
        guard let bytes = SecKeyCopyExternalRepresentation(key, &error) else { throw AppError.message("Cannot export the public key.") }
        return bytes as Data
    }
}
enum Vault {
    static let service = "com.household.HomeLibrary.ios"
    static func read(_ name: String) -> Data? {
        var result: CFTypeRef?
        let status = SecItemCopyMatching([kSecClass: kSecClassGenericPassword, kSecAttrService: service, kSecAttrAccount: name, kSecReturnData: true] as CFDictionary, &result)
        return status == errSecSuccess ? result as? Data : nil
    }
    static func write(_ name: String, _ data: Data) throws {
        let query = [kSecClass: kSecClassGenericPassword, kSecAttrService: service, kSecAttrAccount: name] as CFDictionary
        let update = SecItemUpdate(query, [kSecValueData: data] as CFDictionary)
        if update == errSecItemNotFound {
            let status = SecItemAdd([kSecClass: kSecClassGenericPassword, kSecAttrService: service, kSecAttrAccount: name, kSecValueData: data, kSecAttrAccessible: kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly] as CFDictionary, nil)
            guard status == errSecSuccess else { throw AppError.message("Cannot save secure credentials (\(status)).") }
        } else if update != errSecSuccess { throw AppError.message("Cannot update secure credentials (\(update)).") }
    }
    static func identity(_ certificate: Data) throws -> SecIdentity {
        var result: CFTypeRef?
        let status = SecItemCopyMatching([kSecClass: kSecClassIdentity, kSecReturnRef: true, kSecMatchLimit: kSecMatchLimitAll] as CFDictionary, &result)
        if status == errSecSuccess, let identities = result as? [SecIdentity] {
            for identity in identities {
                var cert: SecCertificate?
                if SecIdentityCopyCertificate(identity, &cert) == errSecSuccess, let cert, SecCertificateCopyData(cert) as Data == certificate { return identity }
            }
        }
        throw AppError.message("The paired device identity is unavailable. Pair this phone again.")
    }
    static func deleteIdentity(_ pair: Pairing) {
        if let cert = SecCertificateCreateWithData(nil, pair.certificate as CFData) { SecItemDelete([kSecClass: kSecClassCertificate, kSecValueRef: cert] as CFDictionary) }
        deleteKey(pair.keyTag)
    }
    static func deleteKey(_ tag: String) { SecItemDelete([kSecClass: kSecClassKey, kSecAttrApplicationTag: Data(tag.utf8)] as CFDictionary) }
}
final class SecureSession: NSObject, URLSessionDelegate, URLSessionTaskDelegate {
    let endpoint: URL; let pin: Data; let pairing: Pairing?
    init(url: String, pin: String, pairing: Pairing? = nil) throws {
        guard let endpoint = URL(string: url), endpoint.scheme == "https", let bytes = Data(base64Encoded: pin) else { throw AppError.message("Invalid HTTPS connection.") }
        self.endpoint = endpoint; self.pin = bytes; self.pairing = pairing
    }
    func urlSession(_ session: URLSession, task: URLSessionTask, willPerformHTTPRedirection response: HTTPURLResponse, newRequest request: URLRequest, completionHandler: @escaping (URLRequest?) -> Void) { completionHandler(nil) }
    func urlSession(_ session: URLSession, didReceive challenge: URLAuthenticationChallenge, completionHandler: @escaping (URLSession.AuthChallengeDisposition, URLCredential?) -> Void) {
        guard challenge.protectionSpace.host.lowercased() == endpoint.host?.lowercased(), challenge.protectionSpace.port == (endpoint.port ?? 443) else { completionHandler(.cancelAuthenticationChallenge, nil); return }
        if challenge.protectionSpace.authenticationMethod == NSURLAuthenticationMethodServerTrust {
            guard let trust = challenge.protectionSpace.serverTrust, let chain = SecTrustCopyCertificateChain(trust) as? [SecCertificate], let cert = chain.first,
                  let key = SecCertificateCopyKey(cert), let pkcs1 = try? RSA.external(key), Data(SHA256.hash(data: RSA.spki(pkcs1))) == pin else { completionHandler(.cancelAuthenticationChallenge, nil); return }
            // QR pin binds the exact server and endpoint. Validate its self-signed
            // certificate as the sole trust anchor, including validity and SSL usage.
            SecTrustSetPolicies(trust, SecPolicyCreateSSL(true, nil))
            SecTrustSetAnchorCertificates(trust, [cert] as CFArray); SecTrustSetAnchorCertificatesOnly(trust, true)
            guard SecTrustEvaluateWithError(trust, nil) else { completionHandler(.cancelAuthenticationChallenge, nil); return }
            completionHandler(.useCredential, URLCredential(trust: trust))
        } else if challenge.protectionSpace.authenticationMethod == NSURLAuthenticationMethodClientCertificate, let pairing,
                  let identity = try? Vault.identity(pairing.certificate), let ca = SecCertificateCreateWithData(nil, pairing.caCertificate as CFData) {
            completionHandler(.useCredential, URLCredential(identity: identity, certificates: [ca], persistence: .forSession))
        } else { completionHandler(.cancelAuthenticationChallenge, nil) }
    }
    func request(_ path: String, body: Data? = nil) async throws -> Data {
        let config = URLSessionConfiguration.ephemeral; config.timeoutIntervalForRequest = path == "/mobile/backup/complete" ? 180 : path == "/mobile/accounts/delete" ? 45 : 30; config.urlCache = nil; config.tlsMinimumSupportedProtocolVersion = .TLSv12
        let session = URLSession(configuration: config, delegate: self, delegateQueue: nil)
        defer { session.finishTasksAndInvalidate() }
        guard let url = URL(string: path, relativeTo: endpoint)?.absoluteURL, url.host == endpoint.host, url.port == endpoint.port else { throw AppError.message("Invalid API path.") }
        var request = URLRequest(url: url); request.httpMethod = body == nil ? "GET" : "POST"; request.httpBody = body
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        let (data, response) = try await session.data(for: request)
        guard let http = response as? HTTPURLResponse else { throw AppError.message("Invalid server response.") }
        guard (200..<300).contains(http.statusCode) else {
            let object = (try? JSONSerialization.jsonObject(with: data)) as? [String: Any]
            throw AppError.response(http.statusCode, object?["error"] as? String ?? "Server returned \(http.statusCode).")
        }
        return data
    }
}
enum PairingService {
    static func enroll(_ code: PairCode, name: String) async throws -> Pairing {
        let tag = Vault.service + "." + UUID().uuidString
        var error: Unmanaged<CFError>?
        guard let key = SecKeyCreateRandomKey([kSecAttrKeyType: kSecAttrKeyTypeRSA, kSecAttrKeySizeInBits: 2048,
            kSecPrivateKeyAttrs: [kSecAttrIsPermanent: true, kSecAttrApplicationTag: Data(tag.utf8), kSecAttrAccessible: kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly]] as CFDictionary, &error), let publicKey = SecKeyCopyPublicKey(key) else { throw AppError.message("Cannot create this phone’s private key.") }
        var completed = false; var addedCertificate: SecCertificate?
        defer {
            if !completed { Vault.deleteKey(tag); if let cert = addedCertificate { SecItemDelete([kSecClass: kSecClassCertificate, kSecValueRef: cert] as CFDictionary) } }
        }
        let spki = RSA.spki(try RSA.external(publicKey)).base64EncodedString()
        let proof = Data("home-library-pair:v1:\(code.code):\(spki)".utf8)
        guard let signature = SecKeyCreateSignature(key, .rsaSignatureMessagePKCS1v15SHA256, proof as CFData, &error) else { throw AppError.message("Cannot sign the pairing proof.") }
        let body = try JSONSerialization.data(withJSONObject: ["code": code.code, "name": name, "publicKey": spki, "signature": (signature as Data).base64EncodedString()])
        let client = try SecureSession(url: code.serverUrl, pin: code.serverKey)
        let result = try JSONDecoder().decode(Enrollment.self, from: await client.request("/mobile/pair", body: body))
        guard result.serverId == code.serverId, let bytes = Data(base64Encoded: result.certificate), let ca = Data(base64Encoded: result.caCertificate),
              let cert = SecCertificateCreateWithData(nil, bytes as CFData), let certifiedKey = SecCertificateCopyKey(cert), try RSA.external(certifiedKey) == RSA.external(publicKey) else { throw AppError.message("The server returned an invalid device certificate.") }
        let status = SecItemAdd([kSecClass: kSecClassCertificate, kSecValueRef: cert] as CFDictionary, nil)
        guard status == errSecSuccess else { throw AppError.message("Cannot save the device certificate (\(status)).") }
        addedCertificate = cert; _ = try Vault.identity(bytes)
        let pair = Pairing(serverUrl: code.serverUrl, serverKey: code.serverKey, deviceId: result.deviceId, certificate: bytes, caCertificate: ca, keyTag: tag)
        try Vault.write("pairing", JSONEncoder().encode(pair)); completed = true; return pair
    }
}
