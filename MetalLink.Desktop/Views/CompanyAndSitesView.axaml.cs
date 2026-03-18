using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MetalLink.Desktop.ViewModels;

namespace MetalLink.Desktop.Views;

public partial class CompanyAndSitesView : UserControl
{
    public CompanyAndSitesView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void ToggleSearchCriteria(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.CompanyIsSearchCriteriaExpanded = !vm.CompanyIsSearchCriteriaExpanded;
    }

    public void ToggleSearchResults(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.CompanyIsSearchResultsExpanded = !vm.CompanyIsSearchResultsExpanded;
    }

    public void ToggleDetails(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.CompanyIsDetailsExpanded = !vm.CompanyIsDetailsExpanded;
    }

    public void ToggleCreateEdit(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.CompanyIsCreateEditExpanded = !vm.CompanyIsCreateEditExpanded;
    }

    public void ToggleSites(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.CompanyIsPanelExpanded = !vm.CompanyIsPanelExpanded;
    }

    public void ToggleSiteCreateEdit(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.IsSiteCreateEditExpanded = !vm.IsSiteCreateEditExpanded;
    }

    public void ToggleSiteDocumentation(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.IsSiteDocumentationExpanded = !vm.IsSiteDocumentationExpanded;
    }
}
