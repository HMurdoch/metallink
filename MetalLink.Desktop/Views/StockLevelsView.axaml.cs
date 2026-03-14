using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MetalLink.Desktop.ViewModels;

namespace MetalLink.Desktop.Views;

public partial class StockLevelsView : UserControl
{
    public StockLevelsView()
    {
        InitializeComponent();
    }

    private void ToggleFilter(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is StockLevelsViewModel vm)
            vm.IsFilterExpanded = !vm.IsFilterExpanded;
    }

    private void ToggleChart(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is StockLevelsViewModel vm)
            vm.IsChartExpanded = !vm.IsChartExpanded;
    }

    private void ToggleResults(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is StockLevelsViewModel vm)
            vm.IsResultsExpanded = !vm.IsResultsExpanded;
    }

    private void OnMovementClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Placeholder for movement navigation logic
    }
}
