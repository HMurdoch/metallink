using Avalonia.Controls;
using MetalLink.Desktop.ViewModels;
using MetalLink.Desktop.Services;

namespace MetalLink.Desktop.Views;

public partial class ReportsView : UserControl
{
    public ReportsView()
    {
        InitializeComponent();
        
        // Initialize ViewModel if not set by DI
        if (DataContext == null)
        {
            // Get ApiClient from DI or create instance
            // TODO: Update this based on your DI setup
            // DataContext = new ReportsViewModel(apiClient);
        }
    }
}
