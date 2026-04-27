using System;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using MetalLink.Desktop.ViewModels;

namespace MetalLink.Desktop.Views;

public class SectionToViewConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        var paramStr = parameter?.ToString();
        if (paramStr == "arrow" && value is bool isExpanded)
        {
            return isExpanded ? "▼" : "▶";
        }
        
        if (paramStr == "entity")
        {
            // EntityFlag can arrive as char or as a single-char string depending on
            // the binding path / JSON deserialisation round-trip.
            char flag = value switch
            {
                char c => c,
                string s when s.Length > 0 => s[0],
                _ => '\0'
            };
            return flag == 'C' ? "Customer" : "Buyer";
        }

        if (value is not EnumMainSection section) return null;

        Control? view = null;
        Console.WriteLine($"[DEBUG] SectionToViewConverter: Converting section '{section}'");
        try 
        {
            view = section switch
            {
                EnumMainSection.Dashboard => new DashboardView(),
                EnumMainSection.Customers => new CustomersView(),
                EnumMainSection.Buyers => new BuyersView(),
                EnumMainSection.CompanyAndSites => new CompanyAndSitesView(),
                EnumMainSection.Products => new ProductsView(),
                EnumMainSection.PriceLists => new PriceListsView(),
                EnumMainSection.Prices => new PricesView(),
                EnumMainSection.TicketsReceiving => new ReceivingTicketsView(),
                EnumMainSection.TicketsSending => new SendingTicketsView(),
                EnumMainSection.Documents => new DocumentsView(),
                EnumMainSection.Camera => new CameraView(),
                EnumMainSection.Reports => new ReportsView(),
                EnumMainSection.StockLevels => new StockLevelsView(),
                EnumMainSection.PriceListStockLevels => new PriceListStockLevelsView(),
                EnumMainSection.StockMovement => new StockMovementView(),
                EnumMainSection.PriceListStockMovements => new PriceListStockMovementsView(),
                EnumMainSection.Settings => new SettingsView(),
                _ => null
            };
            Console.WriteLine($"[DEBUG] SectionToViewConverter: Successfully created view of type {(view?.GetType().Name ?? "NULL")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] SectionToViewConverter: EXCEPTION creating view for {section}: {ex}");
            return new Border { Background = Avalonia.Media.Brushes.Red, Child = new TextBlock { Text = $"Error: {ex.Message}" } };
        }

        return view ?? new Border
        {
            Padding = new Avalonia.Thickness(24),
            Child = new TextBlock
            {
                Text = $"No view mapped for section: {section}",
                FontSize = 18
            }
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}
