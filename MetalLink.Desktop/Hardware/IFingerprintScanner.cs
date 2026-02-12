using System.Threading.Tasks;

namespace MetalLink.Desktop.Hardware;

/// <summary>
/// Interface for fingerprint scanning devices
/// </summary>
public interface IFingerprintScanner
{
    /// <summary>
    /// Captures a fingerprint and returns the image data
    /// </summary>
    /// <returns>Result containing the fingerprint image data</returns>
    Task<FingerprintScanResult> CaptureAsync();
}

public class FingerprintScanResult
{
    public bool IsSuccess { get; set; }
    public byte[]? ImageData { get; set; }
    public string? ErrorMessage { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Quality { get; set; } // 0-100 quality score
}
