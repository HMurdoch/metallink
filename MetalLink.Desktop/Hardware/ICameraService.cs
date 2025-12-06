using System.Threading;
using System.Threading.Tasks;

namespace MetalLink.Desktop.Hardware;

public interface ICameraService
{
    /// <summary>
    /// Capture a single image from the specified camera device
    /// and save it to a local file. Returns metadata about the capture.
    /// </summary>
    Task<CameraCaptureResult> CaptureAsync(
        CameraDeviceType deviceType,
        string documentType,
        CancellationToken cancellationToken = default);
}
