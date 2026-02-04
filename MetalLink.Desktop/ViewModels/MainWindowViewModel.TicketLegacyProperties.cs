using System.Threading.Tasks;
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
        set
        {
            _selectedTicketTypeOption = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ArePlatformFieldsVisible)); // Update platform fields visibility
            // Update Weighbridge fields visibility based on ticket type
            // Only show Weighbridge-specific fields when type is "weighbridge"
            AreWeighbridgeFieldsVisible = value?.Key == "weighbridge";
            
            // Regenerate ticket number when type changes (only if not viewing an existing ticket)
            if (!IsViewingTicketOnly && value != null)
            {
                // ticketTypeId: 1 = Weighbridge, 2 = Platform
                int ticketTypeId = value.Key == "weighbridge" ? 1 : 2;
                _ = OnTicketTypeChangedAsync(ticketTypeId);
            }
        }
    }

    private async Task OnTicketTypeChangedAsync(int ticketTypeId)
    {
        // Generate ticket number based on ticket type
        // For Receiving: 1=Weighbridge (RWB), 2=Platform (RPL)
        // For Sending: 3=Weighbridge (SWB), 4=Platform (SPL)
        string prefix = ticketTypeId switch
        {
            1 => "RWB",  // Receiving Weighbridge
            2 => "RPL",  // Receiving Platform
            3 => "SWB",  // Sending Weighbridge
            4 => "SPL",  // Sending Platform
            _ => "RPL"   // Default to Platform
        };

        // Try to call RegenerateTicketNumberAsync on the derived class implementations
        // This will be implemented in TicketsReceiving and TicketsSending partial classes
        try
        {
            var method = this.GetType().GetMethod("RegenerateTicketNumberAsync", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                var task = method.Invoke(this, new object[] { prefix }) as Task;
                if (task != null)
                    await task;
            }
        }
        catch { }
    }

    private bool _areWeighbridgeFieldsVisible = false;
    public bool AreWeighbridgeFieldsVisible
    {
        get => _areWeighbridgeFieldsVisible;
        set { _areWeighbridgeFieldsVisible = value; OnPropertyChanged(); }
    }

    // Button text property
    private string _createOrUpdateButtonText = "Create Ticket";
    public string CreateOrUpdateButtonText
    {
        get => _createOrUpdateButtonText;
        set { _createOrUpdateButtonText = value; OnPropertyChanged(); }
    }

    private bool _isViewingTicketOnly = false;
    public bool IsViewingTicketOnly
    {
        get => _isViewingTicketOnly;
        set { _isViewingTicketOnly = value; OnPropertyChanged(); }
    }

    public bool IsViewingPlatformTicket => IsViewingTicketOnly && SelectedTicketTypeOption?.Key == "platform";
    public bool IsViewingWeighbridgeTicket => IsViewingTicketOnly && SelectedTicketTypeOption?.Key == "weighbridge";

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
