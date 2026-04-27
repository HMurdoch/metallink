using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MetalLink.Desktop.Views;

public partial class PriceListStockLevelsView : UserControl
{
    public PriceListStockLevelsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}