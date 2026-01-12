using System;
using System.Threading.Tasks;
using SkiaSharp;

namespace MetalLink.Desktop.Hardware;

/// <summary>
/// Mock implementation of document scanner for testing
/// </summary>
public class MockDocumentScanner : IDocumentScanner
{
    public async Task<DocumentScanResult> ScanDocumentAsync(DocumentType documentType)
    {
        // Simulate scanning delay
        await Task.Delay(1500);

        // Create a mock scanned document image
        var width = 600;
        var height = 400;
        
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        
        // Background
        canvas.Clear(SKColors.White);
        
        // Draw document border
        using var borderPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.DarkGray,
            StrokeWidth = 2
        };
        canvas.DrawRect(10, 10, width - 20, height - 20, borderPaint);
        
        // Add text based on document type
        using var textPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 24,
            IsAntialias = true
        };
        
        var title = documentType switch
        {
            DocumentType.IdCard => "MOCK ID CARD",
            DocumentType.DriverLicense => "MOCK DRIVER LICENSE",
            _ => "MOCK DOCUMENT"
        };
        
        canvas.DrawText(title, 30, 50, textPaint);
        canvas.DrawText($"Document #: {Random.Shared.Next(100000, 999999)}", 30, 90, textPaint);
        canvas.DrawText($"Scanned: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", 30, 130, textPaint);
        
        // Add mock photo area
        using var photoPaint = new SKPaint
        {
            Color = SKColors.LightGray
        };
        canvas.DrawRect(400, 50, 150, 200, photoPaint);
        canvas.DrawText("PHOTO", 440, 160, textPaint);
        
        // Get image data
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        
        return new DocumentScanResult
        {
            IsSuccess = true,
            ImageData = data.ToArray(),
            Width = width,
            Height = height
        };
    }
}
