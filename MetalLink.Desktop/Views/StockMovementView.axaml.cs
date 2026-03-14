using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MetalLink.Desktop.ViewModels;

namespace MetalLink.Desktop.Views;

public partial class StockMovementView : UserControl
{
    public StockMovementView()
    {
        InitializeComponent();
    }

    private void ToggleFilter(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is StockMovementViewModel vm)
            vm.IsFilterExpanded = !vm.IsFilterExpanded;
    }

    private void ToggleChart(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is StockMovementViewModel vm)
            vm.IsChartExpanded = !vm.IsChartExpanded;
    }

    private void ToggleResults(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is StockMovementViewModel vm)
            vm.IsResultsExpanded = !vm.IsResultsExpanded;
    }
}
