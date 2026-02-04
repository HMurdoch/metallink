using System;
using Avalonia.Data.Converters;

namespace MetalLink.Desktop.Views;

public class HasNotesConverter : IValueConverter
{
    public static readonly HasNotesConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is string notes)
            return !string.IsNullOrWhiteSpace(notes);

        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}
