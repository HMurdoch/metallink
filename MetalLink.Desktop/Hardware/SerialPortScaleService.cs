using System;
using System.IO;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MetalLink.Desktop.Configuration;

namespace MetalLink.Desktop.Hardware;

public sealed class SerialPortScaleService : IScaleService
{
    public async Task<ScaleReading?> ReadOnceAsync(
        ScaleDeviceType deviceType,
        CancellationToken cancellationToken = default)
    {
        var cfg = deviceType switch
        {
            ScaleDeviceType.Weighbridge => ScaleConfig.Weighbridge,
            ScaleDeviceType.Platform    => ScaleConfig.Platform,
            _                           => ScaleConfig.Weighbridge
        };

        if (string.IsNullOrWhiteSpace(cfg.PortName))
        {
            throw new InvalidOperationException(
                $"No PortName configured for {deviceType}. Please set ScaleConfig.{deviceType}.");
        }

        using var port = CreateSerialPort(cfg);

        try
        {
            port.Open();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Could not open serial port '{cfg.PortName}' for {deviceType}: {ex.Message}", ex);
        }

        // Optional request command to trigger a reading
        if (!string.IsNullOrEmpty(cfg.RequestCommand))
        {
            try
            {
                port.Write(cfg.RequestCommand);
            }
            catch (Exception ex)
            {
                throw new IOException(
                    $"Failed to write request command to scale on '{cfg.PortName}': {ex.Message}", ex);
            }
        }

        // Wrap sync ReadLine in Task.Run so we can observe cancellation
        return await Task.Run(() =>
        {
            try
            {
                var line = port.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    return null;
                }

                var weightKg = ParseWeightFromLine(line);

                if (weightKg == null)
                {
                    throw new FormatException($"Could not parse weight from scale line: '{line.Trim()}'");
                }

                return new ScaleReading(deviceType, weightKg.Value);
            }
            catch (TimeoutException)
            {
                return null;
            }
        }, cancellationToken);
    }

    private static SerialPort CreateSerialPort(ScalePortConfig cfg)
    {
        var port = new SerialPort(cfg.PortName)
        {
            BaudRate = cfg.BaudRate,
            DataBits = cfg.DataBits,
            ReadTimeout = cfg.ReadTimeoutMs,
            NewLine = "\r\n"
        };

        port.Parity = cfg.Parity.ToLowerInvariant() switch
        {
            "none" => Parity.None,
            "even" => Parity.Even,
            "odd"  => Parity.Odd,
            "mark" => Parity.Mark,
            "space" => Parity.Space,
            _      => Parity.None
        };

        port.StopBits = cfg.StopBits.ToLowerInvariant() switch
        {
            "one" => StopBits.One,
            "two" => StopBits.Two,
            "onepointfive" => StopBits.OnePointFive,
            _ => StopBits.One
        };

        return port;
    }

    private static decimal? ParseWeightFromLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        // First integer/decimal number in the line
        var match = Regex.Match(line, @"-?\d+(\.\d+)?");
        if (!match.Success)
            return null;

        if (decimal.TryParse(match.Value, out var value))
        {
            return decimal.Round(value, 1);
        }

        return null;
    }
}
