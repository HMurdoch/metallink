using System;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MetalLink.Desktop.Views;

public class TicketStatusColorConverter : IValueConverter
{
    public static readonly TicketStatusColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is null)
            return Brushes.Gray;
        
        if (value is not char status)
            return Brushes.Gray;

        return status switch
        {
            'H' => new SolidColorBrush(Color.Parse("#F44336")), // Header - Red
            'M' => new SolidColorBrush(Color.Parse("#FF9800")), // Multi-weight - Orange
            'C' => new SolidColorBrush(Color.Parse("#4CAF50")), // Complete - Green
            _ => Brushes.Gray
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}
