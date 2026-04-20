using System;
using Avalonia.Data.Converters;

namespace MetalLink.Desktop.Views;

public class ObjectConverters
{
    public static readonly IValueConverter IsNotNull =
        new FuncValueConverter<object?, bool>(x => x != null);

    public static readonly IValueConverter IsNull =
        new FuncValueConverter<object?, bool>(x => x == null);
}