using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MetalLink.Desktop.Views;

public partial class PriceListStockMovementsView : UserControl
{
    public PriceListStockMovementsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}