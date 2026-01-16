using MetalLink.Shared.Tickets;

namespace MetalLink.Desktop.ViewModels;

/// <summary>
/// LEGACY: Properties for the old unified ticket system.
/// These are kept for backward compatibility with the search functionality.
/// New code should use TicketsReceiving or TicketsSending ViewModels.
/// </summary>
public partial class MainWindowViewModel
{
    // Legacy ticket properties for search/edit functionality
    private string _ticketCustomerIdText = string.Empty;
    public string TicketCustomerIdText
    {
        get => _ticketCustomerIdText;
        set { _ticketCustomerIdText = value; OnPropertyChanged(); }
    }

    private string _ticketNumber = string.Empty;
    public string TicketNumber
    {
        get => _ticketNumber;
        set { _ticketNumber = value; OnPropertyChanged(); }
    }

    private string _ticketType = "weighbridge";
    public string TicketType
    {
        get => _ticketType;
        set { _ticketType = value; OnPropertyChanged(); }
    }

    private string _ticketFirstWeightText = string.Empty;
    public string TicketFirstWeightText
    {
        get => _ticketFirstWeightText;
        set { _ticketFirstWeightText = value; OnPropertyChanged(); }
    }

    private string _ticketSecondWeightText = string.Empty;
    public string TicketSecondWeightText
    {
        get => _ticketSecondWeightText;
        set { _ticketSecondWeightText = value; OnPropertyChanged(); }
    }

    private string _ticketUnitPriceText = string.Empty;
    public string TicketUnitPriceText
    {
        get => _ticketUnitPriceText;
        set { _ticketUnitPriceText = value; OnPropertyChanged(); }
    }

    private string _ticketCurrencyCode = "ZAR";
    public string TicketCurrencyCode
    {
        get => _ticketCurrencyCode;
        set { _ticketCurrencyCode = value; OnPropertyChanged(); }
    }

    private string _ticketProductDescription = string.Empty;
    public string TicketProductDescription
    {
        get => _ticketProductDescription;
        set { _ticketProductDescription = value; OnPropertyChanged(); }
    }

    private string _ticketNotes = string.Empty;
    public string TicketNotes
    {
        get => _ticketNotes;
        set { _ticketNotes = value; OnPropertyChanged(); }
    }

    private string _ticketVehicleRegistration = string.Empty;
    public string TicketVehicleRegistration
    {
        get => _ticketVehicleRegistration;
        set { _ticketVehicleRegistration = value; OnPropertyChanged(); }
    }

    private string _ticketOfmWeighbridgeTicket = string.Empty;
    public string TicketOfmWeighbridgeTicket
    {
        get => _ticketOfmWeighbridgeTicket;
        set { _ticketOfmWeighbridgeTicket = value; OnPropertyChanged(); }
    }

    private string _ticketForeignTicket = string.Empty;
    public string TicketForeignTicket
    {
        get => _ticketForeignTicket;
        set { _ticketForeignTicket = value; OnPropertyChanged(); }
    }

    private string _ticketCkNumber = string.Empty;
    public string TicketCkNumber
    {
        get => _ticketCkNumber;
        set { _ticketCkNumber = value; OnPropertyChanged(); }
    }

    private TicketDto? _lastCreatedTicket;
    public TicketDto? LastCreatedTicket
    {
        get => _lastCreatedTicket;
        set { _lastCreatedTicket = value; OnPropertyChanged(); }
    }

    // Ticket editing properties
    private long? _editingTicketId;
    public long? EditingTicketId
    {
        get => _editingTicketId;
        set { _editingTicketId = value; OnPropertyChanged(); }
    }

    // Ticket type selection
    private System.Collections.ObjectModel.ObservableCollection<Properties.TicketTypeOption> _ticketTypeOptions = new();
    public System.Collections.ObjectModel.ObservableCollection<Properties.TicketTypeOption> TicketTypeOptions
    {
        get => _ticketTypeOptions;
        set { _ticketTypeOptions = value; OnPropertyChanged(); }
    }

    private Properties.TicketTypeOption? _selectedTicketTypeOption;
    public Properties.TicketTypeOption? SelectedTicketTypeOption
    {
        get => _selectedTicketTypeOption;
        set { _selectedTicketTypeOption = value; OnPropertyChanged(); }
    }

    // Button text property
    private string _createOrUpdateButtonText = "Create Ticket";
    public string CreateOrUpdateButtonText
    {
        get => _createOrUpdateButtonText;
        set { _createOrUpdateButtonText = value; OnPropertyChanged(); }
    }

    // Currency options
    private System.Collections.ObjectModel.ObservableCollection<string> _currencyOptions = new();
    public System.Collections.ObjectModel.ObservableCollection<string> CurrencyOptions
    {
        get => _currencyOptions;
        set { _currencyOptions = value; OnPropertyChanged(); }
    }

    private string _selectedCurrency = "ZAR";
    public string SelectedCurrency
    {
        get => _selectedCurrency;
        set { _selectedCurrency = value; OnPropertyChanged(); }
    }

    // Initialize TicketTypeOptions if not already done (Receiving only: weighbridge and platform)
    public void InitializeTicketTypeOptions()
    {
        if (TicketTypeOptions.Count == 0)
        {
            TicketTypeOptions.Add(new Properties.TicketTypeOption { Key = "weighbridge", Display = "Weighbridge" });
            TicketTypeOptions.Add(new Properties.TicketTypeOption { Key = "platform", Display = "Platform" });
        }
    }

    // Initialize CurrencyOptions
    public void InitializeCurrencyOptions()
    {
        if (CurrencyOptions.Count == 0)
        {
            CurrencyOptions.Add("ZAR");
            // Add other currencies as needed in the future
        }
    }
}
