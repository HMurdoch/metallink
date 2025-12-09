namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    // --- Documents backing fields ---

    private string _documentsCustomerIdText = string.Empty;
    private string _newDocumentType = "id_front";
    private string _newDocumentFilePath = string.Empty;
    private string _documentsSummary = "No documents loaded.";

    // --- Ticket Report backing fields ---

    private string _ticketReportTicketIdText = string.Empty;
    private string _lastTicketReportPath = "No ticket report downloaded yet.";

    // --- Validation ---

    public bool IsDocumentsCustomerIdInvalid  => !long.TryParse(DocumentsCustomerIdText, out _);
    public bool IsNewDocumentTypeInvalid      => string.IsNullOrWhiteSpace(NewDocumentType);
    public bool IsNewDocumentFilePathInvalid  => string.IsNullOrWhiteSpace(NewDocumentFilePath);

    // --- Document properties ---

    public string DocumentsCustomerIdText
    {
        get => _documentsCustomerIdText;
        set
        {
            _documentsCustomerIdText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDocumentsCustomerIdInvalid));
        }
    }

    public string NewDocumentType
    {
        get => _newDocumentType;
        set
        {
            _newDocumentType = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNewDocumentTypeInvalid));
        }
    }

    public string NewDocumentFilePath
    {
        get => _newDocumentFilePath;
        set
        {
            _newDocumentFilePath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNewDocumentFilePathInvalid));
        }
    }

    public string DocumentsSummary
    {
        get => _documentsSummary;
        set { _documentsSummary = value; OnPropertyChanged(); }
    }

    // --- Ticket report properties ---

    public string TicketReportTicketIdText
    {
        get => _ticketReportTicketIdText;
        set
        {
            _ticketReportTicketIdText = value;
            OnPropertyChanged();
        }
    }

    public string LastTicketReportPath
    {
        get => _lastTicketReportPath;
        set
        {
            _lastTicketReportPath = value;
            OnPropertyChanged();
        }
    }
}
