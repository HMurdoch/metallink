using System;
using Avalonia;
using Avalonia.Data.Converters;

namespace MetalLink.Desktop.Converters;

public sealed class NavIndentConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool b && b)
            return new Thickness(18, 0, 0, 0);

        return new Thickness(0);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}
