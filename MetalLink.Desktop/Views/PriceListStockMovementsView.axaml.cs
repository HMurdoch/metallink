using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using MetalLink.Desktop.ViewModels;

namespace MetalLink.Desktop.Views;

public partial class PriceListStockMovementsView : UserControl
{
    public PriceListStockMovementsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ToggleProductFilter(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is PriceListStockMovementsViewModel vm)
            vm.IsProductFilterExpanded = !vm.IsProductFilterExpanded;
    }

    private void TogglePriceList(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is PriceListStockMovementsViewModel vm)
            vm.IsPriceListExpanded = !vm.IsPriceListExpanded;
    }

    private void ToggleDateFilter(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is PriceListStockMovementsViewModel vm)
            vm.IsDateFilterExpanded = !vm.IsDateFilterExpanded;
    }

    private void ToggleResults(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is PriceListStockMovementsViewModel vm)
            vm.IsResultsExpanded = !vm.IsResultsExpanded;
    }
}