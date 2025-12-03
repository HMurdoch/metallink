namespace MetalLink.Desktop.Configuration;

public sealed class ScalePortConfig
{
    /// <summary>
    /// Serial port name, e.g. "COM3" on Windows or "/dev/ttyUSB0" on Linux.
    /// </summary>
    public string PortName { get; set; } = string.Empty;

    /// <summary>
    /// Baud rate, e.g. 9600, 19200, 38400.
    /// </summary>
    public int BaudRate { get; set; } = 9600;

    /// <summary>
    /// Number of data bits, typically 7 or 8.
    /// </summary>
    public int DataBits { get; set; } = 8;

    /// <summary>
    /// Parity: None, Odd, Even, Mark, Space (we'll use string and map later).
    /// </summary>
    public string Parity { get; set; } = "None";

    /// <summary>
    /// Stop bits: "One", "Two", "OnePointFive".
    /// </summary>
    public string StopBits { get; set; } = "One";

    /// <summary>
    /// Optional command to send to the scale before reading, e.g. "W\r\n".
    /// Leave empty for passive continuous-output scales.
    /// </summary>
    public string RequestCommand { get; set; } = string.Empty;

    /// <summary>
    /// Read timeout in milliseconds.
    /// </summary>
    public int ReadTimeoutMs { get; set; } = 1500;
}
