using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MetalLink.Desktop.ViewModels;

namespace MetalLink.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Only create a default VM if one was not provided (e.g. by LoginViewModel)
        if (DataContext == null && Application.Current is App app)
        {
            DataContext = new MainWindowViewModel(app);
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
