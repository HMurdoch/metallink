using System;

namespace MetalLink.Desktop.Hardware;

public sealed class ScaleReading
{
    public ScaleDeviceType DeviceType { get; }
    public decimal WeightKg { get; }
    public DateTime Timestamp { get; }

    public ScaleReading(ScaleDeviceType deviceType, decimal weightKg)
    {
        DeviceType = deviceType;
        WeightKg = weightKg;
        Timestamp = DateTime.Now;
    }

    public override string ToString()
        => $"{DeviceType} {WeightKg} kg @ {Timestamp:HH:mm:ss}";
}
