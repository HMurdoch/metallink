using System.Threading.Tasks;

namespace MetalLink.Desktop.Hardware;

/// <summary>
/// Interface for scanning documents like ID cards and driver's licenses
/// </summary>
public interface IDocumentScanner
{
    /// <summary>
    /// Scans a document and returns the image data
    /// </summary>
    /// <param name="documentType">Type of document being scanned (ID, DriverLicense, etc.)</param>
    /// <returns>Result containing the scanned image data</returns>
    Task<DocumentScanResult> ScanDocumentAsync(DocumentType documentType);
}

public enum DocumentType
{
    IdCard,
    DriverLicense,
    Other
}

public class DocumentScanResult
{
    public bool IsSuccess { get; set; }
    public byte[]? ImageData { get; set; }
    public string? ErrorMessage { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
