using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MetalLink.Desktop.ViewModels;

namespace MetalLink.Desktop.Views;

public partial class ReportsView : UserControl
{
    public ReportsView()
    {
        InitializeComponent();
    }

    private void ToggleCriteria(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is ReportsViewModel vm)
            vm.IsCriteriaExpanded = !vm.IsCriteriaExpanded;
    }

    private void ToggleResults(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is ReportsViewModel vm)
            vm.IsResultsExpanded = !vm.IsResultsExpanded;
    }
}
