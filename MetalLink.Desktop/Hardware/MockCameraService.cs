using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;

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

    public async Task<CameraCaptureResult> CaptureAsync(
        CameraDeviceType deviceType,
        string documentType,
        CancellationToken cancellationToken = default)
    {
        // Simulate some latency
        await Task.Delay(300, cancellationToken);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"{documentType}_{timestamp}.jpg";
        var fullPath = Path.Combine(_baseFolder, fileName);

        // Create a detailed mock image
        byte[] mockImageData = CreateMockImage(documentType);
        await File.WriteAllBytesAsync(fullPath, mockImageData, cancellationToken);

        var result = new CameraCaptureResult(deviceType, documentType, fullPath, mockImageData);
        return result;
    }

    private static byte[] CreateMockImage(string documentType)
    {
        var info = new SKImageInfo(640, 480);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;

        // Background
        canvas.Clear(SKColors.White);

        // Border
        using var borderPaint = new SKPaint
        {
            Color = SKColors.Navy,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 10
        };
        canvas.DrawRect(new SKRect(5, 5, 635, 475), borderPaint);

        // Header Background
        using var headerPaint = new SKPaint { Color = SKColors.LightSkyBlue };
        canvas.DrawRect(new SKRect(10, 10, 630, 100), headerPaint);

        // Title
        using var textPaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true
        };
        using var font = new SKFont(SKTypeface.Default, 36);
        
        var title = documentType.ToUpper().Replace("IMAGE", "").Trim();
        canvas.DrawText($"MOCK {title}", 30, 65, SKTextAlign.Left, font, textPaint);

        // Mock Photo Area
        using var photoPaint = new SKPaint { Color = SKColors.Silver };
        canvas.DrawRect(new SKRect(400, 120, 600, 350), photoPaint);
        using var photoFont = new SKFont(SKTypeface.Default, 24);
        canvas.DrawText("PHOTO", 450, 240, SKTextAlign.Left, photoFont, textPaint);

        // Content
        using var contentFont = new SKFont(SKTypeface.Default, 24);
        canvas.DrawText($"Name: MOCK PERSON", 30, 150, SKTextAlign.Left, contentFont, textPaint);
        canvas.DrawText($"ID: {Random.Shared.Next(100000, 999999)}", 30, 200, SKTextAlign.Left, contentFont, textPaint);
        canvas.DrawText($"Date: {DateTime.Now:yyyy-MM-dd}", 30, 250, SKTextAlign.Left, contentFont, textPaint);
        canvas.DrawText($"Type: {documentType}", 30, 300, SKTextAlign.Left, contentFont, textPaint);

        // Bottom Footer
        using var footerFont = new SKFont(SKTypeface.Default, 14);
        canvas.DrawText($"MetalLink Secure Capture System - {DateTime.Now:HH:mm:ss}", 30, 450, SKTextAlign.Left, footerFont, textPaint);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 80);
        return data.ToArray();
    }
}
