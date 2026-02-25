using Avalonia.Controls;

namespace MetalLink.Desktop.Views;

public partial class StockMovementView : UserControl
{
    public StockMovementView()
    {
        InitializeComponent();

        // Bind VM if not already provided
        if (DataContext == null && Avalonia.Application.Current is MetalLink.Desktop.App app)
        {
            DataContext = new MetalLink.Desktop.ViewModels.StockMovementViewModel(app.ApiClient);
        }
    }
}
