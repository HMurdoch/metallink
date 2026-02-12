using System;
using Avalonia.Data.Converters;

namespace MetalLink.Desktop.Views;

/// <summary>
/// Converter for Create Header button visibility based on ticket state.
/// CH = hidden for 'H' and 'M', visible for 'C'
/// </summary>
public class CreateHeaderButtonVisibilityConverter : IValueConverter
{
    public static readonly CreateHeaderButtonVisibilityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is not char state)
            return true; // Show by default if unknown

        // Hide for 'H' and 'M', show for 'C'
        return state == 'C';
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converter for Save & Reset button visibility based on ticket state.
/// SR = visible for 'H' and 'M', hidden for 'C'
/// </summary>
public class SaveResetButtonVisibilityConverter : IValueConverter
{
    public static readonly SaveResetButtonVisibilityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is not char state)
            return false; // Hide by default if unknown

        // Show for 'H' and 'M', hide for 'C'
        return state == 'H' || state == 'M';
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converter for Add Line button enabled state based on ticket state.
/// AL = enabled for 'H' and 'M', disabled for 'C'
/// </summary>
public class AddLineButtonEnabledConverter : IValueConverter
{
    public static readonly AddLineButtonEnabledConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is not char state)
            return true; // Enable by default if unknown

        // Enable for 'H' and 'M', disable for 'C'
        return state != 'C';
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}
