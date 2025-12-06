using System;
using System.Globalization;
using Avalonia.Data.Converters;
using MetalLink.Desktop.ViewModels;

namespace MetalLink.Desktop.Views;

public class SectionToViewConverter : IValueConverter
{
    public static readonly SectionToViewConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not EnumMainSection section)
            return null;

        return section switch
        {
            EnumMainSection.Dashboard => new DashboardView
            {
                DataContext = new DashboardView()
            },
            EnumMainSection.Customers => new CustomersView
            {
                DataContext = new CustomersView()
            },
            EnumMainSection.Tickets => new TicketsView
            {
                DataContext = new TicketsView()
            },
            EnumMainSection.Documents => new DocumentsView
            {
                DataContext = new DocumentsView()
            },
            EnumMainSection.Camera => new CameraView
            {
                DataContext = new CameraView()
            },
            _ => null
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
