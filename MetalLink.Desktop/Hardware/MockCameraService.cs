using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MetalLink.Desktop.Hardware;

public sealed class MockCameraService : ICameraService
{
    private readonly string _baseFolder;

    public MockCameraService()
    {
        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        _baseFolder = Path.Combine(pictures, "MetalLinkCameraMocks");

        if (!Directory.Exists(_baseFolder))
        {
            Directory.CreateDirectory(_baseFolder);
        }
    }

    public Task<CameraCaptureResult> CaptureAsync(
        CameraDeviceType deviceType,
        string documentType,
        CancellationToken cancellationToken = default)
    {
        // Simulate some latency
        Thread.Sleep(300);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"{documentType}_{timestamp}.jpg";
        var fullPath = Path.Combine(_baseFolder, fileName);

        // For now we write a tiny placeholder text file with .jpg extension.
        // In a real impl this would be an actual image captured from the camera.
        File.WriteAllText(fullPath,
            $"Mock image for {deviceType} as {documentType} at {DateTime.Now:O}");

        var result = new CameraCaptureResult(deviceType, documentType, fullPath);
        return Task.FromResult(result);
    }
}
