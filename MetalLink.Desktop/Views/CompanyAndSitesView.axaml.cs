using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MetalLink.Desktop.ViewModels;

namespace MetalLink.Desktop.Views;

public partial class CompanyAndSiteView : UserControl
{
    public CompanyAndSiteView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ToggleSearchCriteria(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.CompanyIsSearchCriteriaExpanded = !vm.CompanyIsSearchCriteriaExpanded;
    }

    private void ToggleSearchResults(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.CompanyIsSearchResultsExpanded = !vm.CompanyIsSearchResultsExpanded;
    }

    private void ToggleDetails(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.CompanyIsDetailsExpanded = !vm.CompanyIsDetailsExpanded;
    }

    private void ToggleCreateEdit(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.CompanyIsCreateEditExpanded = !vm.CompanyIsCreateEditExpanded;
    }

    private void ToggleSites(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.CompanyIsPanelExpanded = !vm.CompanyIsPanelExpanded;
    }
}
