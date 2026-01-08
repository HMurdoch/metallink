using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MetalLink.Desktop.Views;

public partial class CompanyAndSiteView : UserControl
{
    public CompanyAndSiteView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
