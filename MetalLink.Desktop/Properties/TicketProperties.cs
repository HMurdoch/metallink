using System.Collections.Generic;
using System.Linq;
using MetalLink.Shared.Tickets;

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
}
