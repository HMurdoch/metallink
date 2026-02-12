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

        // Create a simple mock image (1x1 pixel PNG)
        byte[] mockImageData = CreateMockImage();
        File.WriteAllBytes(fullPath, mockImageData);

        var result = new CameraCaptureResult(deviceType, documentType, fullPath, mockImageData);
        return Task.FromResult(result);
    }

    private static byte[] CreateMockImage()
    {
        // Create a simple 1x1 pixel PNG image
        // PNG signature + IHDR + IDAT + IEND chunks for a 1x1 white pixel
        return new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR chunk
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, // 1x1 dimensions
            0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53,
            0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, // IDAT chunk
            0x54, 0x08, 0xD7, 0x63, 0xF8, 0xFF, 0xFF, 0x3F,
            0x00, 0x05, 0xFE, 0x02, 0xFE, 0xDC, 0xCC, 0x59,
            0xE7, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, // IEND chunk
            0x44, 0xAE, 0x42, 0x60, 0x82
        };
    }
}
