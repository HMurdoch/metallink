using Avalonia.Controls;
using Avalonia.Input;
using MetalLink.Desktop.ViewModels;

namespace MetalLink.Desktop.Views;

public partial class PriceListsView : UserControl
{
    public PriceListsView()
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
    }

    private void ToggleSearchCriteria(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.PriceListsIsSearchCriteriaExpanded = !vm.PriceListsIsSearchCriteriaExpanded;
    }

    private void ToggleSearchResults(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.PriceListsIsSearchResultsExpanded = !vm.PriceListsIsSearchResultsExpanded;
    }

    private void ToggleCreateEdit(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.PriceListsIsCreateEditExpanded = !vm.PriceListsIsCreateEditExpanded;
    }
}
