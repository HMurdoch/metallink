using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MetalLink.Shared.Tickets;
using MetalLink.Shared.Products;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    // Represents a selectable ticket type in the UI
    public sealed record TicketTypeOption(string Key, string Display);

    // Options shown in the ticket type ComboBox
    public IReadOnlyList<TicketTypeOption> TicketTypeOptions { get; } = new[]
    {
        new TicketTypeOption("platform", "Scale"),
        new TicketTypeOption("weighbridge", "Weighbridge")
    };

    // Wrapper around TicketType for binding to the ComboBox
    public TicketTypeOption? SelectedTicketTypeOption
    {
        get => TicketTypeOptions.FirstOrDefault(o => o.Key == TicketType);
        set
        {
            if (value is null) return;
            if (TicketType == value.Key) return;

            TicketType = value.Key;
            OnPropertyChanged();
        }
    }

    // --- Ticket capture backing fields ---

    private string _ticketCustomerIdText = string.Empty;
    private string _ticketType = "platform"; // default to Scale
    private string _ticketNumber = string.Empty;
    private string _ticketFirstWeightText = string.Empty;
    private string _ticketSecondWeightText = string.Empty;
    private string _ticketUnitPriceText = string.Empty;
    private string _ticketCurrencyCode = "ZAR";
    private string _ticketProductDescription = string.Empty;
    private string _ticketNotes = string.Empty;

    // Header / vehicle
    private string _ticketVehicleRegistration = string.Empty;
    private string _ticketOfmWeighbridgeTicket = string.Empty;
    private string _ticketForeignTicket = string.Empty;
    private string _ticketCkNumber = string.Empty;

    private TicketDto? _lastCreatedTicket;

    // --- Validation / unsaved state ---

    public bool IsTicketCustomerIdInvalid => !long.TryParse(TicketCustomerIdText, out _);
    public bool IsTicketNumberInvalid     => string.IsNullOrWhiteSpace(TicketNumber);
    public bool IsTicketUnitPriceInvalid  => !decimal.TryParse(TicketUnitPriceText, out _);

    public bool HasUnsavedTicket =>
        !string.IsNullOrWhiteSpace(TicketCustomerIdText)
        || !string.IsNullOrWhiteSpace(TicketType)
        || !string.IsNullOrWhiteSpace(TicketNumber)
        || !string.IsNullOrWhiteSpace(TicketFirstWeightText)
        || !string.IsNullOrWhiteSpace(TicketSecondWeightText)
        || !string.IsNullOrWhiteSpace(TicketUnitPriceText)
        || !string.IsNullOrWhiteSpace(TicketCurrencyCode)
        || !string.IsNullOrWhiteSpace(TicketProductDescription)
        || !string.IsNullOrWhiteSpace(TicketNotes);

    // --- Properties ---

    public string TicketCustomerIdText
    {
        get => _ticketCustomerIdText;
        set
        {
            _ticketCustomerIdText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsTicketCustomerIdInvalid));
            OnPropertyChanged(nameof(HasUnsavedTicket));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public string TicketType
    {
        get => _ticketType;
        set
        {
            _ticketType = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedTicket));
            OnPropertyChanged(nameof(HasUnsavedChanges));
            OnTicketTypeChanged(); // Trigger conditional logic
        }
    }

    public string TicketNumber
    {
        get => _ticketNumber;
        set
        {
            _ticketNumber = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsTicketNumberInvalid));
            OnPropertyChanged(nameof(HasUnsavedTicket));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public string TicketFirstWeightText
    {
        get => _ticketFirstWeightText;
        set
        {
            _ticketFirstWeightText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedTicket));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public string TicketSecondWeightText
    {
        get => _ticketSecondWeightText;
        set
        {
            _ticketSecondWeightText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedTicket));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public string TicketUnitPriceText
    {
        get => _ticketUnitPriceText;
        set
        {
            _ticketUnitPriceText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsTicketUnitPriceInvalid));
            OnPropertyChanged(nameof(HasUnsavedTicket));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public string TicketCurrencyCode
    {
        get => _ticketCurrencyCode;
        set
        {
            _ticketCurrencyCode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedTicket));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public string TicketProductDescription
    {
        get => _ticketProductDescription;
        set
        {
            _ticketProductDescription = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedTicket));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public string TicketNotes
    {
        get => _ticketNotes;
        set
        {
            _ticketNotes = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedTicket));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public string TicketVehicleRegistration
    {
        get => _ticketVehicleRegistration;
        set
        {
            _ticketVehicleRegistration = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedTicket));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public string TicketOfmWeighbridgeTicket
    {
        get => _ticketOfmWeighbridgeTicket;
        set
        {
            _ticketOfmWeighbridgeTicket = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedTicket));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public string TicketForeignTicket
    {
        get => _ticketForeignTicket;
        set
        {
            _ticketForeignTicket = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedTicket));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public string TicketCkNumber
    {
        get => _ticketCkNumber;
        set
        {
            _ticketCkNumber = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedTicket));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public TicketDto? LastCreatedTicket
    {
        get => _lastCreatedTicket;
        set
        {
            _lastCreatedTicket = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LastCreatedTicketSummary));

            if (value != null && value.TicketId > 0)
            {
                TicketReportTicketIdText = value.TicketId.ToString();
                _ = LoadReceivingLinesForTicketAsync(value.TicketId);
            }
            else
            {
                _ = LoadReceivingLinesForTicketAsync(0);
            }
        }
    }

    public string LastCreatedTicketSummary
    {
        get
        {
            if (LastCreatedTicket == null) return "No ticket created yet.";
            return $"Ticket {LastCreatedTicket.TicketNumber}: Net {LastCreatedTicket.NetWeightKg} kg, Total {LastCreatedTicket.TotalAmount} {LastCreatedTicket.CurrencyCode}";
        }
    }

    // --- Edit Mode Properties ---

    private long? _editingTicketId;
    public long? EditingTicketId
    {
        get => _editingTicketId;
        set
        {
            _editingTicketId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsTicketEditMode));
            OnPropertyChanged(nameof(IsTicketCreateMode));
            OnPropertyChanged(nameof(TicketSaveButtonText));
        }
    }

    public bool IsTicketEditMode => EditingTicketId.HasValue;
    public bool IsTicketCreateMode => !EditingTicketId.HasValue;
    public string TicketSaveButtonText => IsTicketEditMode ? "Update Ticket" : "Create Ticket";

    // --- Product Search Properties ---

    private string _ticketProductSearchText = string.Empty;
    public string TicketProductSearchText
    {
        get => _ticketProductSearchText;
        set
        {
            _ticketProductSearchText = value;
            OnPropertyChanged();
            
            // When text search changes, clear product letter filter to null/empty
            // This allows substring search instead of showing ALL products
            if (!string.IsNullOrWhiteSpace(value))
            {
                _selectedTicketProductLetter = string.Empty;
                OnPropertyChanged(nameof(SelectedTicketProductLetter));
            }
        }
    }

    private string _selectedTicketProductLetter = "ALL";
    public string SelectedTicketProductLetter
    {
        get => _selectedTicketProductLetter;
        set
        {
            _selectedTicketProductLetter = value;
            OnPropertyChanged();
            
            // When letter filter is selected, clear text search
            if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(TicketProductSearchText))
            {
                _ticketProductSearchText = string.Empty;
                OnPropertyChanged(nameof(TicketProductSearchText));
            }
            
            // Filter products by letter
            ApplyTicketProductFilter();
        }
    }

    private ProductLookupDto? _selectedTicketProduct;
    public ProductLookupDto? SelectedTicketProduct
    {
        get => _selectedTicketProduct;
        set
        {
            _selectedTicketProduct = value;
            OnPropertyChanged();
            
            // When product selected, update description
            if (value != null)
            {
                TicketProductDescription = value.ProductName;
            }
        }
    }

    public ObservableCollection<ProductLookupDto> TicketProductSuggestions { get; } = new();

    private void ApplyTicketProductFilter()
    {
        // This will be implemented in the ViewModel to filter products
        // based on SelectedTicketProductLetter
    }

    // --- Currency Properties ---

    private string _selectedCurrency = "ZAR";
    public string SelectedCurrency
    {
        get => _selectedCurrency;
        set
        {
            _selectedCurrency = value;
            OnPropertyChanged();
            TicketCurrencyCode = value;
        }
    }

    public ObservableCollection<string> CurrencyOptions { get; } = new() { "ZAR", "USD", "EUR", "GBP" };

    // --- Platform/Weighbridge Conditional Logic ---

    public bool IsWeighbridgeMode => SelectedTicketTypeOption?.Key == "weighbridge";
    public bool IsPlatformMode => SelectedTicketTypeOption?.Key == "platform";
    
    // Fields enabled based on ticket type
    public bool AreWeightsEnabled => IsWeighbridgeMode;
    public bool IsProductSelectionEnabled => IsWeighbridgeMode;
    public bool AreVehicleFieldsEnabled => IsWeighbridgeMode;

    // When ticket type changes, notify all conditional properties
    private void OnTicketTypeChanged()
    {
        OnPropertyChanged(nameof(IsWeighbridgeMode));
        OnPropertyChanged(nameof(IsPlatformMode));
        OnPropertyChanged(nameof(AreWeightsEnabled));
        OnPropertyChanged(nameof(IsProductSelectionEnabled));
        OnPropertyChanged(nameof(AreVehicleFieldsEnabled));
        
        // Clear weights if switching to Platform mode
        if (IsPlatformMode)
        {
            TicketFirstWeightText = string.Empty;
            TicketSecondWeightText = string.Empty;
            SelectedTicketProduct = null;
            TicketProductSearchText = string.Empty;
        }
    }
}
