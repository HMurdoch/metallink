using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MetalLink.Desktop.Converters;

public sealed class EnumToBoolConverter : IValueConverter
{
    public static readonly EnumToBoolConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || parameter is null)
            return false;

        return string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || parameter is null)
            return Avalonia.Data.BindingOperations.DoNothing;

        if (value is bool b && b)
        {
            // parameter is the enum name
            return Enum.Parse(targetType, parameter.ToString()!, ignoreCase: true);
        }

        return Avalonia.Data.BindingOperations.DoNothing;
    }
}
