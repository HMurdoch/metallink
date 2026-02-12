using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MetalLink.Desktop.Views;

public partial class PaginationControl : UserControl
{
    public PaginationControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
