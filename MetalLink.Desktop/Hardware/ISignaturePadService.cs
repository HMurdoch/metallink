using System.Threading;
using System.Threading.Tasks;

namespace MetalLink.Desktop.Hardware;

public interface ISignaturePadService
{
    /// <summary>
    /// Capture a signature and save it to a local file.
    /// Returns metadata about the capture.
    /// </summary>
    Task<SignatureCaptureResult> CaptureAsync(
        string documentType,
        CancellationToken cancellationToken = default);
}
