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
            EnumMainSection.Buyers => new BuyersView(),
            EnumMainSection.CompanyAndSites => new CompanyAndSiteView(),
            EnumMainSection.ProductsAndPrices => new ProductsAndPricesView(),
            EnumMainSection.TicketsReceiving => new TicketsReceivingView(),
            EnumMainSection.TicketsSending => new TicketsSendingView(),
            EnumMainSection.Documents => new DocumentsView(),
            EnumMainSection.Camera => new CameraView(),
            EnumMainSection.Reports => new ReportsView(),
            EnumMainSection.StockLevels => new StockLevelsView(),
            EnumMainSection.StockMovement => new StockMovementView(),
            EnumMainSection.Settings => new SettingsView(),
            _ => null
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}
