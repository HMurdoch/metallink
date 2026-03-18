using System;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using MetalLink.Desktop.ViewModels;

namespace MetalLink.Desktop.Views;

public class SectionToViewConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (parameter?.ToString() == "arrow" && value is bool isExpanded)
        {
            return isExpanded ? "▼" : "▶";
        }

        Console.WriteLine($"[DEBUG] SectionToViewConverter: Convert called! Value='{value ?? "NULL"}', Type='{value?.GetType().AssemblyQualifiedName ?? "N/A"}', Parameter='{parameter ?? "NONE"}'");

        if (value is not EnumMainSection section)
        {
            Console.WriteLine($"[DEBUG] SectionToViewConverter: Value is NOT EnumMainSection. Actual type: {value?.GetType().FullName ?? "NULL"}. This is likely a namespace or assembly mismatch.");
            return null;
        }

        Console.WriteLine($"[DEBUG] SectionToViewConverter: Creating view for section '{section}' (Int value: {(int)section})");

        Control? view = null;
        try 
        {
            Console.WriteLine($"[DEBUG] SectionToViewConverter: Using switch logic for section '{section}'");
            view = section switch
            {
                EnumMainSection.Dashboard => new DashboardView(),
                EnumMainSection.Customers => new CustomersView(),
                EnumMainSection.Buyers => new BuyersView(),
                EnumMainSection.CompanyAndSites => new CompanyAndSitesView(),
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
            
            if (view == null)
            {
                Console.WriteLine($"[DEBUG] SectionToViewConverter: SWITCH RETURNED NULL for section '{section}' (Int: {(int)section})");
            }
            Console.WriteLine($"[DEBUG] SectionToViewConverter: Successfully created '{view?.GetType().Name ?? "NULL"}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] SectionToViewConverter: EXCEPTION creating view for {section}: {ex.Message}\n{ex.StackTrace}");
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
