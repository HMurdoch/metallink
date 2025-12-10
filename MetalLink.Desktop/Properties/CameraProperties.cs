// MetalLink.Desktop/ViewModels/Properties/CameraProperties.cs
namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    // --- Camera ---

    private string _lastCameraCaptureSummary = "No camera capture yet.";

    public string LastCameraCaptureSummary
    {
        get => _lastCameraCaptureSummary;
        set { _lastCameraCaptureSummary = value; OnPropertyChanged(); }
    }
}
