namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    private string _lastSignatureCaptureSummary = "No signature captured yet.";

    public string LastSignatureCaptureSummary
    {
        get => _lastSignatureCaptureSummary;
        set { _lastSignatureCaptureSummary = value; OnPropertyChanged(); }
    }
}
