using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MetalLink.Desktop.Converters;

public sealed class CurrencyConverter : IValueConverter
{
    public static readonly CurrencyConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return null;

        if (value is decimal d)
            return d.ToString("C", CultureInfo.CurrentCulture);

        if (value is double db)
            return db.ToString("C", CultureInfo.CurrentCulture);

        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Avalonia.Data.BindingOperations.DoNothing;
}
