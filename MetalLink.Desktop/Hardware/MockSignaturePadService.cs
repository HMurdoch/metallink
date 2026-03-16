using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;

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

    public async Task<SignatureCaptureResult> CaptureAsync(
        string documentType,
        CancellationToken cancellationToken = default)
    {
        // Simulate capture delay
        await Task.Delay(500, cancellationToken);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"{documentType}_{timestamp}.png";
        var fullPath = Path.Combine(_baseFolder, fileName);

        // Create a detailed mock signature image
        byte[] mockImageData = CreateMockSignatureImage();
        await File.WriteAllBytesAsync(fullPath, mockImageData, cancellationToken);

        var result = new SignatureCaptureResult(documentType, fullPath, mockImageData);
        return result;
    }

    private static byte[] CreateMockSignatureImage()
    {
        var info = new SKImageInfo(400, 200);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;

        // Background
        canvas.Clear(SKColors.White);

        // Drawing area border
        using var borderPaint = new SKPaint
        {
            Color = SKColors.Gray,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };
        canvas.DrawRect(new SKRect(5, 5, 395, 195), borderPaint);

        // Baseline
        using var linePaint = new SKPaint
        {
            Color = SKColors.LightGray,
            StrokeWidth = 1
        };
        canvas.DrawLine(20, 150, 380, 150, linePaint);

        // Signature Text
        using var textPaint = new SKPaint
        {
            Color = SKColors.DarkBlue,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 3,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };
        
        // Draw a simulated "handwritten" signature path
        using var path = new SKPath();
        path.MoveTo(50, 140);
        path.CubicTo(70, 80, 100, 100, 120, 130);
        path.CubicTo(140, 160, 160, 120, 180, 110);
        path.CubicTo(200, 100, 220, 150, 250, 140);
        path.LineTo(350, 130);
        canvas.DrawPath(path, textPaint);

        // Text labels
        using var labelPaint = new SKPaint { Color = SKColors.DarkGray, IsAntialias = true };
        using var font = new SKFont(SKTypeface.Default, 12);
        canvas.DrawText("Sign here X_____________________", 20, 170, SKTextAlign.Left, font, labelPaint);
        canvas.DrawText($"MOCK SIGNATURE - {DateTime.Now:yyyy-MM-dd}", 20, 20, SKTextAlign.Left, font, labelPaint);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
