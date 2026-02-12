using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MetalLink.Desktop.ViewModels;

namespace MetalLink.Desktop.Views;

public partial class CustomersView : UserControl
{
    public CustomersView()
    {
        InitializeComponent();
        // NOTE: Selection is already handled via DataGrid SelectedItem binding.
        // Do NOT execute commands from PropertyChanged here; it causes recursion.
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
