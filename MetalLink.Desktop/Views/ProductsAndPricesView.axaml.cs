using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MetalLink.Desktop.ViewModels;

namespace MetalLink.Desktop.Views;

public partial class ProductsAndPricesView : UserControl
{
    public ProductsAndPricesView()
    {
        InitializeComponent();
    }

    private void ToggleSearchCriteria(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.ProductsIsSearchCriteriaExpanded = !vm.ProductsIsSearchCriteriaExpanded;
    }

    private void ToggleSearchResults(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.ProductsIsSearchResultsExpanded = !vm.ProductsIsSearchResultsExpanded;
    }

    private void ToggleCreateEdit(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.ProductsIsCreateEditExpanded = !vm.ProductsIsCreateEditExpanded;
    }

    private void ToggleEditPrice(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.ProductsIsDetailsExpanded = !vm.ProductsIsDetailsExpanded;
    }
}
