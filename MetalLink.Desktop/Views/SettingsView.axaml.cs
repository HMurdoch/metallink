using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MetalLink.Desktop.ViewModels;

namespace MetalLink.Desktop.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void ToggleAppearance(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            vm.IsAppearanceExpanded = !vm.IsAppearanceExpanded;
    }
}
