using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MetalLink.Desktop.ViewModels;

namespace MetalLink.Desktop.Views;

public partial class ProductsView : UserControl
{
    public ProductsView()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        DataContextChanged += (s, e) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.RequestProductImagePopup += ShowProductImagePopup;
            }
        };
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.I && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (DataContext is MainWindowViewModel vm && !string.IsNullOrEmpty(vm.ProductIsriUrl))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = vm.ProductIsriUrl,
                        UseShellExecute = true
                    });
                    e.Handled = true;
                }
                catch { }
            }
        }
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

    public async void ShowProductImagePopup(string imageUrl)
    {
        var window = new ProductImageWindow();
        var owner = VisualRoot as Window;
        if (owner != null)
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show(owner);
        }
        else
        {
            window.Show();
        }
        await window.LoadImageAsync(imageUrl);
    }
}
