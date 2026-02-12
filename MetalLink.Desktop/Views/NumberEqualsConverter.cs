using System;
using Avalonia.Data.Converters;

namespace MetalLink.Desktop.Views;

public class NumberEqualsConverter : IValueConverter
{
    public static readonly NumberEqualsConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        if (!int.TryParse(value.ToString(), out var intValue))
            return false;

        if (!int.TryParse(parameter.ToString(), out var paramValue))
            return false;

        return intValue == paramValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}
