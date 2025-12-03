namespace MetalLink.Desktop.Configuration;

public static class ScaleConfig
{
    /// <summary>
    /// True = use mock/random readings.
    /// False = use SerialPortScaleService.
    /// For now this is a static property; later you can load it from JSON/env.
    /// </summary>
    public static bool UseMockScales { get; set; } = true;

    /// <summary>
    /// Serial configuration for the weighbridge (truck scale).
    /// </summary>
    public static ScalePortConfig Weighbridge { get; } = new()
    {
        // TODO: set these for the real device
        PortName = "COM3",        // Windows
        BaudRate = 9600,
        DataBits = 8,
        Parity = "None",
        StopBits = "One",
        RequestCommand = string.Empty,   // or e.g. "W\r\n"
        ReadTimeoutMs = 1500
    };

    /// <summary>
    /// Serial configuration for the platform scale (small loads).
    /// </summary>
    public static ScalePortConfig Platform { get; } = new()
    {
        // TODO: set these for the real device
        PortName = "COM4",        // Windows, or "/dev/ttyUSB0" on Linux
        BaudRate = 9600,
        DataBits = 8,
        Parity = "None",
        StopBits = "One",
        RequestCommand = string.Empty,
        ReadTimeoutMs = 1500
    };
}
