using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using MetalLink.Shared.Tickets.Receiving;
using MetalLink.Shared.Tickets.Sending;

namespace MetalLink.Desktop.ViewModels.Distribution;

public sealed class TicketDistributionViewModel : ViewModelBase
{
    public string Title { get; }

    public ObservableCollection<DistributionSliceViewModel> Slices { get; } = new();

    // For the PieChartControl (derive from Value so switching modes always changes proportions)
    public IReadOnlyList<(double fraction, IBrush brush)> PieSlices
    {
        get
        {
            var total = Slices.Sum(s => s.Value);
            if (total <= 0)
                return Array.Empty<(double, IBrush)>();

            return Slices
                .Select(s => ((double)(s.Value / total), s.Brush))
                .ToList();
        }
    }

    private DistributionMode _mode;
    public DistributionMode Mode
    {
        get => _mode;
        set
        {
            if (_mode == value) return;
            _mode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsCashMode));
            Rebuild();
            OnPropertyChanged(nameof(PieSlices));
        }
    }

    public bool IsCashMode
    {
        get => Mode == DistributionMode.Cash;
        set => Mode = value ? DistributionMode.Cash : DistributionMode.Weight;
    }

    private readonly IReadOnlyList<(string productName, int productId, decimal netKg, decimal tareKg, decimal totalInclVat)> _lines;
    private readonly Dictionary<int, IBrush> _brushByProductId = new();

    public TicketDistributionViewModel(
        string title,
        IReadOnlyList<(string productName, int productId, decimal netKg, decimal tareKg, decimal totalInclVat)> lines)
    {
        Title = title;
        _lines = lines;
        _mode = DistributionMode.Weight;
        Rebuild();
    }

    public static TicketDistributionViewModel FromReceiving(TicketReceivingDto ticket)
    {
        var lines = ticket.Lines
            .Where(l => l.IsActive)
            .Select(l => (l.ProductName, l.ProductId, l.NetWeightKg, l.Tare, l.TotalInclVat))
            .ToList();

        return new TicketDistributionViewModel($"Receiving Ticket {ticket.TicketNumber} Distribution", lines);
    }

    public static TicketDistributionViewModel FromSending(TicketSendingDto ticket)
    {
        var lines = ticket.Lines
            .Where(l => l.IsActive)
            .Select(l => (l.ProductName, l.ProductId, l.NetWeightKg, l.Tare, l.TotalInclVat))
            .ToList();

        return new TicketDistributionViewModel($"Sending Ticket {ticket.TicketNumber} Distribution", lines);
    }

    private void Rebuild()
    {
        Slices.Clear();

        if (_lines.Count == 0)
            return;

        // Group by product
        var grouped = _lines
            .GroupBy(l => (l.productId, l.productName))
            .Select(g => new
            {
                g.Key.productId,
                g.Key.productName,
                WeightKg = g.Sum(x => Math.Max(0m, x.netKg - x.tareKg)),
                Cash = g.Sum(x => x.totalInclVat)
            })
            .OrderByDescending(x => Mode == DistributionMode.Weight ? x.WeightKg : x.Cash)
            .ToList();

        var total = grouped.Sum(x => Mode == DistributionMode.Weight ? x.WeightKg : x.Cash);
        if (total <= 0)
            return;

        foreach (var g in grouped)
        {
            var val = Mode == DistributionMode.Weight ? g.WeightKg : g.Cash;
            if (val <= 0) continue;

            var pct = val / total;
            Slices.Add(new DistributionSliceViewModel
            {
                Label = g.productName,
                Value = val,
                Percent = pct * 100m,
                Brush = GetBrushForProduct(g.productId, g.productName)
            });
        }

        OnPropertyChanged(nameof(PieSlices));
    }

    private IBrush GetBrushForProduct(int productId, string productName)
    {
        if (_brushByProductId.TryGetValue(productId, out var existing))
            return existing;

        // Create a distinct hue using a hash -> hue mapping.
        // This avoids collisions like the previous fixed palette did.
        var hash = productName.GetHashCode() ^ productId;
        var hue = (Math.Abs(hash) % 360);

        var c1 = HslToColor(hue, 0.78, 0.56);
        var c2 = HslToColor(hue, 0.85, 0.38);
        var brush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new GradientStop(c1, 0),
                new GradientStop(c2, 1)
            }
        };

        _brushByProductId[productId] = brush;
        return brush;
    }

    private static Color HslToColor(double h, double s, double l)
    {
        // h [0..360)
        h = h % 360;
        var c = (1 - Math.Abs(2 * l - 1)) * s;
        var x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        var m = l - c / 2;

        (double r, double g, double b) = h switch
        {
            < 60 => (c, x, 0d),
            < 120 => (x, c, 0d),
            < 180 => (0d, c, x),
            < 240 => (0d, x, c),
            < 300 => (x, 0d, c),
            _ => (c, 0d, x)
        };

        byte R = (byte)Math.Round((r + m) * 255);
        byte G = (byte)Math.Round((g + m) * 255);
        byte B = (byte)Math.Round((b + m) * 255);
        return Color.FromRgb(R, G, B);
    }
}
