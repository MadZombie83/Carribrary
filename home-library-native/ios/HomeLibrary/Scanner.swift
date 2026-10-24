import SwiftUI
import AVFoundation

struct Scanner: UIViewControllerRepresentable {
    var onCode: (String) -> Void
    func makeUIViewController(context: Context) -> CameraController { let controller = CameraController(); controller.onCode = onCode; return controller }
    func updateUIViewController(_ uiViewController: CameraController, context: Context) {}
    static func dismantleUIViewController(_ controller: CameraController, coordinator: ()) { controller.stop() }
}
final class CameraController: UIViewController, AVCaptureMetadataOutputObjectsDelegate {
    var onCode: ((String) -> Void)?
    private let session = AVCaptureSession(); private let queue = DispatchQueue(label: "HomeLibrary.camera")
    private var preview: AVCaptureVideoPreviewLayer?; private var emitted = false; private var stopped = false
    override func viewDidLoad() {
        super.viewDidLoad(); view.backgroundColor = .black
        switch AVCaptureDevice.authorizationStatus(for: .video) {
        case .authorized: configure()
        case .notDetermined: AVCaptureDevice.requestAccess(for: .video) { [weak self] allowed in DispatchQueue.main.async { if allowed { self?.configure() } else { self?.show("Allow camera access in Settings, or use manual entry.") } } }
        default: show("Allow camera access in Settings, or use manual entry.")
        }
    }
    private func configure() {
        let layer = AVCaptureVideoPreviewLayer(session: session); layer.videoGravity = .resizeAspectFill; view.layer.addSublayer(layer); preview = layer; layer.frame = view.bounds
        queue.async { [weak self] in
            guard let self, !self.stopped else { return }
            do {
                guard let camera = AVCaptureDevice.default(.builtInWideAngleCamera, for: .video, position: .back) else { throw AppError.message("Camera unavailable. Use manual entry.") }
                let input = try AVCaptureDeviceInput(device: camera)
                self.session.beginConfiguration(); self.session.sessionPreset = .high
                guard self.session.canAddInput(input) else { self.session.commitConfiguration(); throw AppError.message("Camera unavailable.") }
                self.session.addInput(input); let output = AVCaptureMetadataOutput()
                guard self.session.canAddOutput(output) else { self.session.commitConfiguration(); throw AppError.message("Scanner unavailable.") }
                self.session.addOutput(output); output.setMetadataObjectsDelegate(self, queue: .main)
                output.metadataObjectTypes = [.ean13, .ean8, .upce, .code128, .code39, .qr].filter { output.availableMetadataObjectTypes.contains($0) }
                self.session.commitConfiguration()
                try camera.lockForConfiguration(); if camera.isFocusModeSupported(.continuousAutoFocus) { camera.focusMode = .continuousAutoFocus }; camera.unlockForConfiguration()
                if !self.stopped { self.session.startRunning() }
            } catch { DispatchQueue.main.async { self.show(error.localizedDescription) } }
        }
    }
    override func viewDidLayoutSubviews() { super.viewDidLayoutSubviews(); preview?.frame = view.bounds }
    func metadataOutput(_ output: AVCaptureMetadataOutput, didOutput metadataObjects: [AVMetadataObject], from connection: AVCaptureConnection) {
        guard !emitted, let code = metadataObjects.compactMap({ ($0 as? AVMetadataMachineReadableCodeObject)?.stringValue }).first else { return }
        emitted = true; stop(); onCode?(code)
    }
    func stop() { queue.async { [weak self] in self?.stopped = true; self?.session.stopRunning() } }
    private func show(_ message: String) { let label = UILabel(); label.text = message; label.textColor = .white; label.numberOfLines = 0; label.textAlignment = .center; label.frame = view.bounds.insetBy(dx: 20, dy: 20); label.autoresizingMask = [.flexibleWidth, .flexibleHeight]; view.addSubview(label) }
}
