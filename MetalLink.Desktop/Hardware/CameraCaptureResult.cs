using System;

namespace MetalLink.Desktop.Hardware;

public sealed class CameraCaptureResult
{
    public CameraDeviceType DeviceType { get; }
    public string DocumentType { get; }
    public string FilePath { get; }
    public byte[]? ImageData { get; }
    public DateTime Timestamp { get; }

    public CameraCaptureResult(
        CameraDeviceType deviceType,
        string documentType,
        string filePath,
        byte[]? imageData = null)
    {
        DeviceType = deviceType;
        DocumentType = documentType;
        FilePath = filePath;
        ImageData = imageData;
        Timestamp = DateTime.Now;
    }

    public override string ToString()
        => $"{DeviceType} -> {DocumentType} ({FilePath}) @ {Timestamp:yyyy-MM-dd HH:mm:ss}";
}
