using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace MetalLink.Desktop.Hardware;

public sealed class MockScaleService : IScaleService
{
    private readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

    public Task<ScaleReading?> ReadOnceAsync(
        ScaleDeviceType deviceType,
        CancellationToken cancellationToken = default)
    {
        decimal weightKg = deviceType switch
        {
            ScaleDeviceType.Weighbridge => RandomDecimal(2000m, 35000m), // trucks
            ScaleDeviceType.Platform   => RandomDecimal(5m, 500m),      // small loads
            _                          => RandomDecimal(1m, 1000m)
        };

        var reading = new ScaleReading(deviceType, decimal.Round(weightKg, 1));
        return Task.FromResult<ScaleReading?>(reading);
    }

    private decimal RandomDecimal(decimal min, decimal max)
    {
        // Simple RNG-based decimal in [min, max)
        var bytes = new byte[4];
        _rng.GetBytes(bytes);
        var value = BitConverter.ToUInt32(bytes, 0) / (double)uint.MaxValue;
        var range = (double)(max - min);
        return min + (decimal)(value * range);
    }
}
