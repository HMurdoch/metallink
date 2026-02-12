using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MetalLink.Desktop.ViewModels;

namespace MetalLink.Desktop.Views;

public partial class BuyersView : UserControl
{
    public BuyersView()
    {
        InitializeComponent();
        // NOTE: Selection is already bound via:
        // SelectedItem="{Binding FoundBuyer, Mode=TwoWay}"
        // Do NOT execute commands from PropertyChanged here; it causes recursion.
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
