using System.Threading;
using System.Threading.Tasks;

namespace MetalLink.Desktop.Hardware;

public interface IScaleService
{
    /// <summary>
    /// Reads a single weight value from the specified scale.
    /// </summary>
    Task<ScaleReading?> ReadOnceAsync(
        ScaleDeviceType deviceType,
        CancellationToken cancellationToken = default);
}
