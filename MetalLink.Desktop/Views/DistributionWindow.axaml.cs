using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MetalLink.Desktop.Views;

public partial class DistributionWindow : Window
{
    public DistributionWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
