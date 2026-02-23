using Avalonia;
using Avalonia.Controls;

namespace MetalLink.Desktop.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();

        // Wire up VM here because views are created via SectionToViewConverter.
        if (Application.Current is App app)
        {
            DataContext = new MetalLink.Desktop.ViewModels.SettingsViewModel(app.ThemeService);
        }
    }
}
