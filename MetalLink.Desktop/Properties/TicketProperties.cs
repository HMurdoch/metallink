using MetalLink.Shared.Tickets;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    // --- Ticket capture backing fields ---

    private string _ticketCustomerIdText = string.Empty;
    private string _ticketType = "weighbridge";
    private string _ticketNumber = string.Empty;
    private string _ticketFirstWeightText = string.Empty;
    private string _ticketSecondWeightText = string.Empty;
    private string _ticketUnitPriceText = string.Empty;
    private string _ticketCurrencyCode = "ZAR";
    private string _ticketProductDescription = string.Empty;
    private string _ticketNotes = string.Empty;
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
