using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Controls.Documents;

namespace MetalLink.Desktop.Converters;

public class BoldSearchMatchConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2) return null;

        string? text = values[0]?.ToString();
        string? search = values[1]?.ToString();

        if (string.IsNullOrEmpty(text)) return null;

        var inlines = new InlineCollection();

        if (string.IsNullOrEmpty(search))
        {
            inlines.Add(new Run(text));
            return inlines;
        }

        int lastIndex = 0;
        int index = text.IndexOf(search, StringComparison.OrdinalIgnoreCase);

        while (index != -1)
        {
            // Add text before match
            if (index > lastIndex)
            {
                inlines.Add(new Run(text.Substring(lastIndex, index - lastIndex)));
            }

            // Add bold match
            inlines.Add(new Run(text.Substring(index, search.Length)) { FontWeight = FontWeight.Bold });

            lastIndex = index + search.Length;
            index = text.IndexOf(search, lastIndex, StringComparison.OrdinalIgnoreCase);
        }

        // Add remaining text
        if (lastIndex < text.Length)
        {
            inlines.Add(new Run(text.Substring(lastIndex)));
        }

        return inlines;
    }
}
