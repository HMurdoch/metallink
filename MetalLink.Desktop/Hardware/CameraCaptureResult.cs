using System;

namespace MetalLink.Desktop.Hardware;

public sealed class CameraCaptureResult
{
    public CameraDeviceType DeviceType { get; }
    public string DocumentType { get; }
    public string FilePath { get; }
    public DateTime Timestamp { get; }

    public CameraCaptureResult(
        CameraDeviceType deviceType,
        string documentType,
        string filePath)
    {
        DeviceType = deviceType;
        DocumentType = documentType;
        FilePath = filePath;
        Timestamp = DateTime.Now;
    }

    public override string ToString()
        => $"{DeviceType} -> {DocumentType} ({FilePath}) @ {Timestamp:yyyy-MM-dd HH:mm:ss}";
}
