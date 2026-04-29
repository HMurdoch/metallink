using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using MetalLink.Desktop.ViewModels;

namespace MetalLink.Desktop.Views;

public partial class PriceListStockLevelsView : UserControl
{
    public PriceListStockLevelsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ToggleFilter(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is PriceListStockLevelsViewModel vm)
            vm.IsFilterExpanded = !vm.IsFilterExpanded;
    }

    private void TogglePriceList(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is PriceListStockLevelsViewModel vm)
            vm.IsPriceListExpanded = !vm.IsPriceListExpanded;
    }

    private void ToggleResults(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is PriceListStockLevelsViewModel vm)
            vm.IsResultsExpanded = !vm.IsResultsExpanded;
    }
}