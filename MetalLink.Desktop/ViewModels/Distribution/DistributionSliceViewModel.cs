using Avalonia.Media;

namespace MetalLink.Desktop.ViewModels.Distribution;

public sealed class DistributionSliceViewModel
{
    public required string Label { get; init; }
    public required decimal Value { get; init; }
    public required decimal Percent { get; init; }
    public required IBrush Brush { get; init; }

    // Display helpers
    public string PercentText => $"{Percent:N1}%";
    public string KgText => $"{Value:N2} kg";
    // For cash, use a converter in XAML so it follows system culture.
    public decimal CashValue => Value;
}
