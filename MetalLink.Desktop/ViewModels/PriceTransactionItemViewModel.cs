using System;
using Avalonia.Media;

namespace MetalLink.Desktop.ViewModels;

/// <summary>
/// Represents a single BUY or SELL transaction line shown in the price row tooltip.
/// </summary>
public class PriceTransactionItemViewModel
{
    public bool IsBuy { get; init; }
    public DateTimeOffset Date { get; init; }
    public decimal QuantityKg { get; init; }

    public string Arrow    => IsBuy ? "↑" : "↓";
    public string TypeLabel => IsBuy ? "BUY " : "SELL";

    public IBrush ArrowBrush => IsBuy
        ? new SolidColorBrush(Color.Parse("#22c55e"))   // green
        : new SolidColorBrush(Color.Parse("#ef4444"));  // red

    public string QuantitySummary => $"{QuantityKg:N2} kg";
    public string DateLabel       => Date.LocalDateTime.ToString("dd MMM yyyy");
}
