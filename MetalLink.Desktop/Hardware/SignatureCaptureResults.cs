using System;

namespace MetalLink.Desktop.Hardware;

public sealed class SignatureCaptureResult
{
    public string DocumentType { get; }
    public string FilePath { get; }
    public DateTime Timestamp { get; }

    public SignatureCaptureResult(string documentType, string filePath)
    {
        DocumentType = documentType;
        FilePath = filePath;
        Timestamp = DateTime.Now;
    }

    public override string ToString()
        => $"{DocumentType} -> {FilePath} @ {Timestamp:yyyy-MM-dd HH:mm:ss}";
}
