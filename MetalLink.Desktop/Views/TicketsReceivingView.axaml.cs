using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MetalLink.Desktop.ViewModels;
using MetalLink.Desktop.ViewModels.Receiving;

namespace MetalLink.Desktop.Views;

public partial class TicketsReceivingView : UserControl
{
    public TicketsReceivingView()
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
            vm.Receiving.ReceivingIsSearchCriteriaExpanded = !vm.Receiving.ReceivingIsSearchCriteriaExpanded;
    }

    private void ToggleSearchResults(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.Receiving.ReceivingIsSearchResultsExpanded = !vm.Receiving.ReceivingIsSearchResultsExpanded;
    }

    private void ToggleDetails(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.Receiving.ReceivingIsDetailsExpanded = !vm.Receiving.ReceivingIsDetailsExpanded;
    }

    private void ToggleCreateEdit(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.Receiving.ReceivingIsCreateEditExpanded = !vm.Receiving.ReceivingIsCreateEditExpanded;
    }

    private void TogglePanel(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.Receiving.ReceivingIsPanelExpanded = !vm.Receiving.ReceivingIsPanelExpanded;
    }

    private void ReceivingTicketsGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e) { }
    public void UpdateLastLineTare() { }
}
