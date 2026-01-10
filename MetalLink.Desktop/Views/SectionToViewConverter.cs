using System;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using MetalLink.Desktop.ViewModels;

namespace MetalLink.Desktop.Views;

public class SectionToViewConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is not EnumMainSection section)
            return null;

        return section switch
        {
            EnumMainSection.Dashboard => new DashboardView(),
            EnumMainSection.Customers => new CustomersView(),
            EnumMainSection.CompanyAndSites => new CompanyAndSiteView(),
            EnumMainSection.ProductsAndPrices => new ProductsAndPricesView(),
            EnumMainSection.Tickets => new TicketsView(),
            EnumMainSection.TicketsSending => new TicketsView(), // TODO: separate Sending view later
            EnumMainSection.Documents => new DocumentsView(),
            EnumMainSection.Camera => new CameraView(),
            EnumMainSection.Reports => new ReportsView(),
            EnumMainSection.Settings => new SettingsView(),
            _ => null
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}
