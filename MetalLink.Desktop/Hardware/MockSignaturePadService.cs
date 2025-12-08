using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MetalLink.Desktop.Hardware;

public sealed class MockSignaturePadService : ISignaturePadService
{
    private readonly string _baseFolder;

    public MockSignaturePadService()
    {
        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        _baseFolder = Path.Combine(pictures, "MetalLinkSignatures");

        if (!Directory.Exists(_baseFolder))
        {
            Directory.CreateDirectory(_baseFolder);
        }
    }

    public Task<SignatureCaptureResult> CaptureAsync(
        string documentType,
        CancellationToken cancellationToken = default)
    {
        // Simulate capture delay
        Thread.Sleep(500);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"{documentType}_{timestamp}.png";
        var fullPath = Path.Combine(_baseFolder, fileName);

        // Placeholder "image" content. Real implementation would write PNG bytes.
        File.WriteAllText(
            fullPath,
            $"Mock signature for {documentType} captured at {DateTime.Now:O}");

        var result = new SignatureCaptureResult(documentType, fullPath);
        return Task.FromResult(result);
    }
}
