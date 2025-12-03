using System;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using MetalLink.Desktop.Auth;
using MetalLink.Desktop.Hardware;
using MetalLink.Desktop.Services;
using MetalLink.Shared.Customers;
using MetalLink.Shared.Tickets;

namespace MetalLink.Desktop.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly App _app;
    private readonly AuthState _authState;
    private readonly ApiClient _apiClient;
    private readonly CustomerService _customerService;
    private readonly TicketService _ticketService;
    private readonly IScaleService _scaleService;

    private string _title = "Metal Link Desktop";
    private string _statusMessage = "Ready.";
    private bool _isBusy;

    // --- Section visibility flags (for menu) ---
    private bool _showSystemHealthSection = true;
    private bool _showCustomerSection = true;
    private bool _showTicketSection = true;

    // --- Customer search ---
    private string _searchCustomerIdText = string.Empty;
    private CustomerDto? _foundCustomer;

    // --- New customer form ---
    private string _newFullName = string.Empty;
    private bool _newIsCompany;
    private string? _newCompanyName;
    private string? _newIdNumber;
    private string? _newAccountNumber;
    private string? _newPriceCode;
    private string? _newPhoneNumber;
    private string? _newMobileNumber;
    private string? _newEmail;

    // --- Ticket capture ---
    private string _ticketCustomerIdText = string.Empty;
    private string _ticketType = "weighbridge"; // or "platform"
    private string _ticketNumber = string.Empty;
    private string _ticketFirstWeightText = string.Empty;
    private string _ticketSecondWeightText = string.Empty;
    private string _ticketUnitPriceText = string.Empty;
    private string _ticketCurrencyCode = "ZAR";
    private string _ticketProductDescription = string.Empty;
    private string _ticketNotes = string.Empty;
    private TicketDto? _lastCreatedTicket;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title
    {
        get => _title;
        set { _title = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }

    public string LoggedInUser => $"{_authState.DisplayName} ({_authState.Username})";

    // --- Section visibility properties ---

    public bool ShowSystemHealthSection
    {
        get => _showSystemHealthSection;
        set { _showSystemHealthSection = value; OnPropertyChanged(); }
    }

    public bool ShowCustomerSection
    {
        get => _showCustomerSection;
        set { _showCustomerSection = value; OnPropertyChanged(); }
    }

    public bool ShowTicketSection
    {
        get => _showTicketSection;
        set { _showTicketSection = value; OnPropertyChanged(); }
    }

    // --- Customer search properties ---

    public string SearchCustomerIdText
    {
        get => _searchCustomerIdText;
        set { _searchCustomerIdText = value; OnPropertyChanged(); }
    }

    public CustomerDto? FoundCustomer
    {
        get => _foundCustomer;
        set { _foundCustomer = value; OnPropertyChanged(); OnPropertyChanged(nameof(FoundCustomerSummary)); }
    }

    public string FoundCustomerSummary
    {
        get
        {
            if (FoundCustomer == null) return "No customer loaded.";
            return $"ID: {FoundCustomer.CustomerId}, Name: {FoundCustomer.FullName}, Account: {FoundCustomer.AccountNumber ?? "-"}";
        }
    }

    // --- New customer form properties ---

    public string NewFullName
    {
        get => _newFullName;
        set { _newFullName = value; OnPropertyChanged(); }
    }

    public bool NewIsCompany
    {
        get => _newIsCompany;
        set { _newIsCompany = value; OnPropertyChanged(); }
    }

    public string? NewCompanyName
    {
        get => _newCompanyName;
        set { _newCompanyName = value; OnPropertyChanged(); }
    }

    public string? NewIdNumber
    {
        get => _newIdNumber;
        set { _newIdNumber = value; OnPropertyChanged(); }
    }

    public string? NewAccountNumber
    {
        get => _newAccountNumber;
        set { _newAccountNumber = value; OnPropertyChanged(); }
    }

    public string? NewPriceCode
    {
        get => _newPriceCode;
        set { _newPriceCode = value; OnPropertyChanged(); }
    }

    public string? NewPhoneNumber
    {
        get => _newPhoneNumber;
        set { _newPhoneNumber = value; OnPropertyChanged(); }
    }

    public string? NewMobileNumber
    {
        get => _newMobileNumber;
        set { _newMobileNumber = value; OnPropertyChanged(); }
    }

    public string? NewEmail
    {
        get => _newEmail;
        set { _newEmail = value; OnPropertyChanged(); }
    }

    // --- Ticket capture properties ---

    public string TicketCustomerIdText
    {
        get => _ticketCustomerIdText;
        set { _ticketCustomerIdText = value; OnPropertyChanged(); }
    }

    public string TicketType
    {
        get => _ticketType;
        set { _ticketType = value; OnPropertyChanged(); }
    }

    public string TicketNumber
    {
        get => _ticketNumber;
        set { _ticketNumber = value; OnPropertyChanged(); }
    }

    public string TicketFirstWeightText
    {
        get => _ticketFirstWeightText;
        set { _ticketFirstWeightText = value; OnPropertyChanged(); }
    }

    public string TicketSecondWeightText
    {
        get => _ticketSecondWeightText;
        set { _ticketSecondWeightText = value; OnPropertyChanged(); }
    }

    public string TicketUnitPriceText
    {
        get => _ticketUnitPriceText;
        set { _ticketUnitPriceText = value; OnPropertyChanged(); }
    }

    public string TicketCurrencyCode
    {
        get => _ticketCurrencyCode;
        set { _ticketCurrencyCode = value; OnPropertyChanged(); }
    }

    public string TicketProductDescription
    {
        get => _ticketProductDescription;
        set { _ticketProductDescription = value; OnPropertyChanged(); }
    }

    public string TicketNotes
    {
        get => _ticketNotes;
        set { _ticketNotes = value; OnPropertyChanged(); }
    }

    public TicketDto? LastCreatedTicket
    {
        get => _lastCreatedTicket;
        set { _lastCreatedTicket = value; OnPropertyChanged(); OnPropertyChanged(nameof(LastCreatedTicketSummary)); }
    }

    public string LastCreatedTicketSummary
    {
        get
        {
            if (LastCreatedTicket == null) return "No ticket created yet.";
            return $"Ticket {LastCreatedTicket.TicketNumber}: Net {LastCreatedTicket.NetWeightKg} kg, Total {LastCreatedTicket.TotalAmount} {LastCreatedTicket.CurrencyCode}";
        }
    }

    // Commands
    public ICommand CheckDbCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand SearchCustomerCommand { get; }
    public ICommand CreateCustomerCommand { get; }
    public ICommand CreateTicketCommand { get; }
    public ICommand ReadWeighbridgeCommand { get; }
    public ICommand ReadPlatformCommand { get; }

    // Menu / section commands
    public ICommand ShowSystemHealthSectionCommand { get; }
    public ICommand ShowCustomerSectionCommand { get; }
    public ICommand ShowTicketSectionCommand { get; }
    public ICommand ShowAllSectionsCommand { get; }

    public MainWindowViewModel(App app)
    {
        _app = app;
        _authState = app.AuthState;
        _apiClient = app.ApiClient;
        _customerService = app.CustomerService;
        _ticketService = app.TicketService;
        _scaleService = app.ScaleService;

        CheckDbCommand = new AsyncCommand(CheckDbAsync);
        LogoutCommand = new AsyncCommand(LogoutAsync);
        SearchCustomerCommand = new AsyncCommand(SearchCustomerAsync);
        CreateCustomerCommand = new AsyncCommand(CreateCustomerAsync);
        CreateTicketCommand = new AsyncCommand(CreateTicketAsync);
        ReadWeighbridgeCommand = new AsyncCommand(ReadWeighbridgeAsync);
        ReadPlatformCommand = new AsyncCommand(ReadPlatformAsync);

        // Menu section commands
        ShowSystemHealthSectionCommand = new AsyncCommand(() => SetSectionsAsync(health: true, customers: false, tickets: false));
        ShowCustomerSectionCommand     = new AsyncCommand(() => SetSectionsAsync(health: false, customers: true, tickets: false));
        ShowTicketSectionCommand       = new AsyncCommand(() => SetSectionsAsync(health: false, customers: false, tickets: true));
        ShowAllSectionsCommand         = new AsyncCommand(() => SetSectionsAsync(health: true, customers: true, tickets: true));
    }

    private Task SetSectionsAsync(bool health, bool customers, bool tickets)
    {
        ShowSystemHealthSection = health;
        ShowCustomerSection = customers;
        ShowTicketSection = tickets;
        return Task.CompletedTask;
    }

    // --- Commands implementation ---

    private async Task CheckDbAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Checking database...";

        try
        {
            var result = await _apiClient.GetAsync<HealthResponse>("api/health/db");

            if (result is not null)
            {
                StatusMessage = $"DB OK. Customers count: {result.customersCount}";
            }
            else
            {
                StatusMessage = "DB check returned no data.";
            }
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = $"Error calling API: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Task LogoutAsync()
    {
        _app.AuthService.Logout();

        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var loginWindow = new Views.LoginWindow
            {
                DataContext = new LoginViewModel(_app)
            };

            var current = desktop.MainWindow;
            desktop.MainWindow = loginWindow;
            current?.Close();
        }

        return Task.CompletedTask;
    }

    private async Task SearchCustomerAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Searching customer...";

        try
        {
            if (!long.TryParse(SearchCustomerIdText, out var id))
            {
                StatusMessage = "Please enter a valid numeric Customer ID.";
                FoundCustomer = null;
                return;
            }

            var customer = await _customerService.GetCustomerByIdAsync(id);

            if (customer == null)
            {
                StatusMessage = $"Customer {id} not found.";
                FoundCustomer = null;
            }
            else
            {
                FoundCustomer = customer;
                StatusMessage = $"Loaded customer {customer.FullName}.";
            }
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = $"Error calling API: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateCustomerAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Creating customer...";

        try
        {
            if (string.IsNullOrWhiteSpace(NewFullName))
            {
                StatusMessage = "Full name is required.";
                return;
            }

            var customer = await _customerService.CreateCustomerAsync(
                fullName: NewFullName,
                isCompany: NewIsCompany,
                companyName: NewCompanyName,
                idNumber: NewIdNumber,
                accountNumber: NewAccountNumber,
                priceCode: NewPriceCode,
                addressLine1: null,
                addressLine2: null,
                suburb: null,
                city: null,
                postalCode: null,
                phoneNumber: NewPhoneNumber,
                mobileNumber: NewMobileNumber,
                email: NewEmail
            );

            if (customer == null)
            {
                StatusMessage = "Customer could not be created (no response).";
                return;
            }

            StatusMessage = $"Customer created: ID {customer.CustomerId}, {customer.FullName}.";
            FoundCustomer = customer;

            // Clear form
            NewFullName = string.Empty;
            NewIsCompany = false;
            NewCompanyName = null;
            NewIdNumber = null;
            NewAccountNumber = null;
            NewPriceCode = null;
            NewPhoneNumber = null;
            NewMobileNumber = null;
            NewEmail = null;
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = $"Error calling API: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateTicketAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Creating ticket...";

        try
        {
            if (!long.TryParse(TicketCustomerIdText, out var customerId))
            {
                StatusMessage = "Please enter a valid numeric Customer ID for the ticket.";
                return;
            }

            if (string.IsNullOrWhiteSpace(TicketNumber))
            {
                StatusMessage = "Ticket number is required.";
                return;
            }

            if (!decimal.TryParse(TicketUnitPriceText, out var unitPrice))
            {
                StatusMessage = "Please enter a valid unit price (per kg).";
                return;
            }

            decimal? firstWeight = null;
            decimal? secondWeight = null;

            if (!string.IsNullOrWhiteSpace(TicketFirstWeightText))
            {
                if (!decimal.TryParse(TicketFirstWeightText, out var fw))
                {
                    StatusMessage = "First weight is not a valid number.";
                    return;
                }
                firstWeight = fw;
            }

            if (!string.IsNullOrWhiteSpace(TicketSecondWeightText))
            {
                if (!decimal.TryParse(TicketSecondWeightText, out var sw))
                {
                    StatusMessage = "Second weight is not a valid number.";
                    return;
                }
                secondWeight = sw;
            }

            if (string.IsNullOrWhiteSpace(TicketType))
            {
                TicketType = "weighbridge";
            }

            var ticket = await _ticketService.CreateTicketAsync(
                customerId: customerId,
                ticketType: TicketType,
                ticketNumber: TicketNumber,
                firstWeightKg: firstWeight,
                secondWeightKg: secondWeight,
                unitPricePerKg: unitPrice,
                currencyCode: TicketCurrencyCode,
                productDescription: string.IsNullOrWhiteSpace(TicketProductDescription) ? null : TicketProductDescription,
                notes: string.IsNullOrWhiteSpace(TicketNotes) ? null : TicketNotes
            );

            if (ticket == null)
            {
                StatusMessage = "Ticket could not be created (no response).";
                return;
            }

            LastCreatedTicket = ticket;
            StatusMessage = $"Ticket {ticket.TicketNumber} created. Net {ticket.NetWeightKg} kg, Total {ticket.TotalAmount} {ticket.CurrencyCode}.";

            // Optional: keep customer and type but clear entry fields
            TicketNumber = string.Empty;
            TicketFirstWeightText = string.Empty;
            TicketSecondWeightText = string.Empty;
            TicketUnitPriceText = string.Empty;
            TicketProductDescription = string.Empty;
            TicketNotes = string.Empty;
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = $"Error calling API: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReadWeighbridgeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Reading weighbridge...";

        try
        {
            var reading = await _scaleService.ReadOnceAsync(ScaleDeviceType.Weighbridge);
            if (reading == null)
            {
                StatusMessage = "No reading from weighbridge.";
                return;
            }

            // If first empty, fill first; else fill second
            if (string.IsNullOrWhiteSpace(TicketFirstWeightText))
            {
                TicketFirstWeightText = reading.WeightKg.ToString("0.0");
                StatusMessage = $"Weighbridge first weight: {reading.WeightKg:0.0} kg.";
            }
            else
            {
                TicketSecondWeightText = reading.WeightKg.ToString("0.0");
                StatusMessage = $"Weighbridge second weight: {reading.WeightKg:0.0} kg.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error reading weighbridge: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReadPlatformAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Reading platform scale...";

        try
        {
            var reading = await _scaleService.ReadOnceAsync(ScaleDeviceType.Platform);
            if (reading == null)
            {
                StatusMessage = "No reading from platform scale.";
                return;
            }

            TicketFirstWeightText = reading.WeightKg.ToString("0.0");
            StatusMessage = $"Platform weight: {reading.WeightKg:0.0} kg.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error reading platform scale: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Helpers ---

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private sealed class AsyncCommand : ICommand
    {
        private readonly Func<Task> _execute;
        public AsyncCommand(Func<Task> execute) => _execute = execute;

        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => true;

        public async void Execute(object? parameter) => await _execute();
    }

    // Matches HealthController response shape
    private sealed class HealthResponse
    {
        public string status { get; set; } = string.Empty;
        public int customersCount { get; set; }
    }
}
