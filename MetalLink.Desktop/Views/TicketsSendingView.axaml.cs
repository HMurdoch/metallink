using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MetalLink.Desktop.ViewModels;
using MetalLink.Desktop.ViewModels.Sending;

namespace MetalLink.Desktop.Views;

public partial class TicketsSendingView : UserControl
{
    public TicketsSendingView()
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
            vm.Sending.SendingIsSearchCriteriaExpanded = !vm.Sending.SendingIsSearchCriteriaExpanded;
    }

    private void ToggleSearchResults(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.Sending.SendingIsSearchResultsExpanded = !vm.Sending.SendingIsSearchResultsExpanded;
    }

    private void ToggleDetails(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.Sending.SendingIsDetailsExpanded = !vm.Sending.SendingIsDetailsExpanded;
    }

    private void ToggleCreateEdit(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.Sending.SendingIsCreateEditExpanded = !vm.Sending.SendingIsCreateEditExpanded;
    }

    private void TogglePanel(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.Sending.SendingIsPanelExpanded = !vm.Sending.SendingIsPanelExpanded;
    }

    private void SendingTicketsGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e) { }
    public void UpdateLastLineTare() { }

    private void TareTextBox_GotFocus(object? sender, GotFocusEventArgs e) { }
    private void TareTextBox_LostFocus(object? sender, RoutedEventArgs e) { }
}
