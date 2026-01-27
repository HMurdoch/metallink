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

        // ✅ Create VM with App dependency (fixes AVLN3000)
        if (Application.Current is App app)
        {
            DataContext = new MainWindowViewModel(app);
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
