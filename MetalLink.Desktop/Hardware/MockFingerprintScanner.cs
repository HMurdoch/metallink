using System;
using System.Threading.Tasks;
using SkiaSharp;

namespace MetalLink.Desktop.Hardware;

/// <summary>
/// Mock implementation of fingerprint scanner for testing
/// </summary>
public class MockFingerprintScanner : IFingerprintScanner
{
    public async Task<FingerprintScanResult> CaptureAsync()
    {
        // Simulate fingerprint capture delay
        await Task.Delay(2000);

        // Create a mock fingerprint image
        var width = 300;
        var height = 400;
        
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        
        // Background
        canvas.Clear(SKColors.White);
        
        // Draw fingerprint-like pattern
        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Black,
            StrokeWidth = 2,
            IsAntialias = true
        };
        
        var centerX = width / 2f;
        var centerY = height / 2f;
        
        // Draw concentric ovals to simulate fingerprint ridges
        for (int i = 1; i < 15; i++)
        {
            var radiusX = i * 10f;
            var radiusY = i * 13f;
            
            var rect = new SKRect(
                centerX - radiusX,
                centerY - radiusY,
                centerX + radiusX,
                centerY + radiusY
            );
            
            canvas.DrawOval(rect, paint);
        }
        
        // Add some random "breaks" in the pattern for realism
        using var whitePaint = new SKPaint
        {
            Color = SKColors.White,
            StrokeWidth = 3
        };
        
        for (int i = 0; i < 20; i++)
        {
            var angle = Random.Shared.NextDouble() * Math.PI * 2;
            var distance = Random.Shared.Next(50, 140);
            var x1 = centerX + (float)(Math.Cos(angle) * distance);
            var y1 = centerY + (float)(Math.Sin(angle) * distance * 1.3);
            var x2 = x1 + (float)(Math.Cos(angle) * 10);
            var y2 = y1 + (float)(Math.Sin(angle) * 10);
            
            canvas.DrawLine(x1, y1, x2, y2, whitePaint);
        }
        
        // Add timestamp
        using var textPaint = new SKPaint
        {
            Color = SKColors.DarkGray,
            TextSize = 12,
            IsAntialias = true
        };
        canvas.DrawText($"Captured: {DateTime.Now:HH:mm:ss}", 10, height - 10, textPaint);
        
        // Get image data
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        
        return new FingerprintScanResult
        {
            IsSuccess = true,
            ImageData = data.ToArray(),
            Width = width,
            Height = height,
            Quality = Random.Shared.Next(85, 100)
        };
    }
}
