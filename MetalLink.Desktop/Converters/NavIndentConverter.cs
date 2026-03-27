using System;
using Avalonia;
using Avalonia.Data.Converters;

namespace MetalLink.Desktop.Converters;

public sealed class NavIndentConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        // value = IsNavCollapsed
        // When nav is collapsed, we must NOT indent, otherwise the icons get pushed off-screen.
        if (value is bool isCollapsed && !isCollapsed)
        {
            double indent = 18;
            if (parameter != null && double.TryParse(parameter.ToString(), out double multiplier))
            {
                indent += (multiplier * 18);
            }
            return new Thickness(indent, 0, 0, 0);
        }

        return new Thickness(0);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}
