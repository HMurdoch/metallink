using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using MetalLink.Desktop;              // <--- add this
using MetalLink.Desktop.ViewModels;
using MetalLink.Desktop.Auth;
using MetalLink.Desktop.Hardware;
using MetalLink.Desktop.Services;
using MetalLink.Shared.Customers;
using MetalLink.Shared.Tickets;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MetalLink.Desktop.ViewModels;

public class MainWindowViewModel : ObservableObject, INotifyPropertyChanged
{
    private readonly App _app;
    private readonly AuthState _authState;
    private readonly ApiClient _apiClient;
    private readonly CustomerService _customerService;
    private readonly TicketService _ticketService;
    private readonly IScaleService _scaleService;
    private readonly DocumentService _documentService;
    private readonly ICameraService _cameraService;

    private string _title = "Metal Link Desktop";
    private string _statusMessage = "Ready.";
    private bool _isBusy;

    // --- Menu / sections ---

    private EnumMainSection _currentSection = EnumMainSection.Dashboard;
    private EnumMainSection _previousSection = EnumMainSection.Dashboard;

    // true = slide from left (going "back"), false = from right (going "forward")
    private bool _isSlideFromLeft;

    public EnumMainSection CurrentSection
    {
        get => _currentSection;
        set
        {
            if (_currentSection == value) return;

            _previousSection = _currentSection;
            _currentSection = value;

            // if target index < previous index we treat as "back"
            IsSlideFromLeft = (int)_currentSection < (int)_previousSection;

            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDashboardSectionVisible));
            OnPropertyChanged(nameof(IsCustomerSectionVisible));
            OnPropertyChanged(nameof(IsTicketSectionVisible));
            OnPropertyChanged(nameof(IsDocumentSectionVisible));
            OnPropertyChanged(nameof(IsCameraSectionVisible));
        }
    }

    public bool IsSlideFromLeft
    {
        get => _isSlideFromLeft;
        set
        {
            if (_isSlideFromLeft == value) return;
            _isSlideFromLeft = value;
            OnPropertyChanged();
        }
    }

    // Section visibility, used by XAML
    public bool IsDashboardSectionVisible => CurrentSection == EnumMainSection.Dashboard;
    public bool IsCustomerSectionVisible  => CurrentSection == EnumMainSection.Customers;
    public bool IsTicketSectionVisible    => CurrentSection == EnumMainSection.Tickets;
    public bool IsDocumentSectionVisible  => CurrentSection == EnumMainSection.Documents;
    public bool IsCameraSectionVisible    => CurrentSection == EnumMainSection.Camera;

    // --- Validation flags (your originals) ---

    public bool IsNewCustomerFullNameInvalid => string.IsNullOrWhiteSpace(NewFullName);

    public bool IsTicketCustomerIdInvalid => !long.TryParse(TicketCustomerIdText, out _);
    public bool IsTicketNumberInvalid     => string.IsNullOrWhiteSpace(TicketNumber);
    public bool IsTicketUnitPriceInvalid  => !decimal.TryParse(TicketUnitPriceText, out _);

    public bool IsDocumentsCustomerIdInvalid  => !long.TryParse(DocumentsCustomerIdText, out _);
    public bool IsNewDocumentTypeInvalid      => string.IsNullOrWhiteSpace(NewDocumentType);
    public bool IsNewDocumentFilePathInvalid  => string.IsNullOrWhiteSpace(NewDocumentFilePath);

    // --- Unsaved state flags (your originals) ---

    public bool HasUnsavedNewCustomer =>
        !string.IsNullOrWhiteSpace(NewFullName)
        || NewIsCompany
        || !string.IsNullOrWhiteSpace(NewCompanyName)
        || !string.IsNullOrWhiteSpace(NewIdNumber)
        || !string.IsNullOrWhiteSpace(NewAccountNumber)
        || !string.IsNullOrWhiteSpace(NewPriceCode)
        || !string.IsNullOrWhiteSpace(NewPhoneNumber)
        || !string.IsNullOrWhiteSpace(NewMobileNumber)
        || !string.IsNullOrWhiteSpace(NewEmail);

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

    // Global “dirty” flag (used by header dot)
    public bool HasUnsavedChanges => HasUnsavedNewCustomer || HasUnsavedTicket;

    // --- (Optional) tab index, if you ever use a TabControl ---
    private int _selectedTabIndex;

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
    private string _ticketType = "weighbridge";
    private string _ticketNumber = string.Empty;
    private string _ticketFirstWeightText = string.Empty;
    private string _ticketSecondWeightText = string.Empty;
    private string _ticketUnitPriceText = string.Empty;
    private string _ticketCurrencyCode = "ZAR";
    private string _ticketProductDescription = string.Empty;
    private string _ticketNotes = string.Empty;
    private TicketDto? _lastCreatedTicket;

    // --- Documents ---
    private string _documentsCustomerIdText = string.Empty;
    private string _newDocumentType = "id_front";
    private string _newDocumentFilePath = string.Empty;
    private string _documentsSummary = "No documents loaded.";

    // --- Camera ---
    private string _lastCameraCaptureSummary = "No camera capture yet.";

    public new event PropertyChangedEventHandler? PropertyChanged;

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

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set { _selectedTabIndex = value; OnPropertyChanged(); }
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
        set
        {
            _foundCustomer = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FoundCustomerSummary));
        }
    }

    public string FoundCustomerSummary
    {
        get
        {
            if (FoundCustomer == null) return "No customer loaded.";
            return $"ID: {FoundCustomer.CustomerId}, Name: {FoundCustomer.FullName}, Account: {FoundCustomer.AccountNumber ?? "-"}";
        }
    }

    // --- New customer form ---

    public string NewFullName
    {
        get => _newFullName;
        set
        {
            _newFullName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNewCustomerFullNameInvalid));
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public bool NewIsCompany
    {
        get => _newIsCompany;
        set
        {
            _newIsCompany = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public string? NewCompanyName
    {
        get => _newCompanyName;
        set
        {
            _newCompanyName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public string? NewIdNumber
    {
        get => _newIdNumber;
        set
        {
            _newIdNumber = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public string? NewAccountNumber
    {
        get => _newAccountNumber;
        set
        {
            _newAccountNumber = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public string? NewPriceCode
    {
        get => _newPriceCode;
        set
        {
            _newPriceCode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public string? NewPhoneNumber
    {
        get => _newPhoneNumber;
        set
        {
            _newPhoneNumber = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public string? NewMobileNumber
    {
        get => _newMobileNumber;
        set
        {
            _newMobileNumber = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public string? NewEmail
    {
        get => _newEmail;
        set
        {
            _newEmail = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    // --- Ticket capture ---

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

    // --- Documents ---

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

    // --- Camera ---

    public string LastCameraCaptureSummary
    {
        get => _lastCameraCaptureSummary;
        set { _lastCameraCaptureSummary = value; OnPropertyChanged(); }
    }

    // Commands
    public ICommand CheckDbCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand SearchCustomerCommand { get; }
    public ICommand CreateCustomerCommand { get; }
    public ICommand CreateTicketCommand { get; }
    public ICommand ReadWeighbridgeCommand { get; }
    public ICommand ReadPlatformCommand { get; }
    public ICommand LoadCustomerDocumentsCommand { get; }
    public ICommand UploadCustomerDocumentCommand { get; }

    // Section navigation
    public ICommand ShowDashboardCommand { get; }
    public ICommand ShowCustomersCommand { get; }
    public ICommand ShowTicketsCommand { get; }
    public ICommand ShowDocumentsCommand { get; }
    public ICommand ShowCameraCommand { get; }

    // Camera commands
    public ICommand CaptureWbFrontBeforeCommand { get; }
    public ICommand CaptureWbTopBeforeCommand { get; }
    public ICommand CaptureWbFrontAfterCommand { get; }
    public ICommand CaptureWbTopAfterCommand { get; }
    public ICommand CapturePfFrontBeforeCommand { get; }
    public ICommand CapturePfTopBeforeCommand { get; }
    public ICommand CapturePfFrontAfterCommand { get; }
    public ICommand CapturePfTopAfterCommand { get; }

    // Optional tab navigation commands
    public ICommand GoDashboardCommand { get; }
    public ICommand GoCustomerCommand { get; }
    public ICommand GoTicketsCommand { get; }
    public ICommand GoDocumentsCommand { get; }
    public ICommand GoCameraCommand { get; }

    public MainWindowViewModel(App app)
    {
        _app = app;
        _authState = app.AuthState;
        _apiClient = app.ApiClient;
        _customerService = app.CustomerService;
        _ticketService = app.TicketService;
        _scaleService = app.ScaleService;
        _documentService = app.DocumentService;
        _cameraService = app.CameraService;

        _selectedTabIndex = 0;

        // Core commands
        CheckDbCommand = new AsyncCommand(CheckDbAsync);
        LogoutCommand = new AsyncCommand(LogoutAsync);
        SearchCustomerCommand = new AsyncCommand(SearchCustomerAsync);
        CreateCustomerCommand = new AsyncCommand(CreateCustomerAsync);
        CreateTicketCommand = new AsyncCommand(CreateTicketAsync);
        ReadWeighbridgeCommand = new AsyncCommand(ReadWeighbridgeAsync);
        ReadPlatformCommand = new AsyncCommand(ReadPlatformAsync);
        LoadCustomerDocumentsCommand = new AsyncCommand(LoadCustomerDocumentsAsync);
        UploadCustomerDocumentCommand = new AsyncCommand(UploadCustomerDocumentAsync);

        // Section navigation (used by menu)
        ShowDashboardCommand = ReactiveUI.ReactiveCommand.Create(() => CurrentSection = EnumMainSection.Dashboard);
        ShowCustomersCommand = ReactiveUI.ReactiveCommand.Create(() => CurrentSection = EnumMainSection.Customers);
        ShowTicketsCommand   = ReactiveUI.ReactiveCommand.Create(() => CurrentSection = EnumMainSection.Tickets);
        ShowDocumentsCommand = ReactiveUI.ReactiveCommand.Create(() => CurrentSection = EnumMainSection.Documents);
        ShowCameraCommand    = ReactiveUI.ReactiveCommand.Create(() => CurrentSection = EnumMainSection.Camera);
        // Camera commands
        CaptureWbFrontBeforeCommand = new AsyncCommand(() =>
            CaptureAndUploadAsync(CameraDeviceType.WeighbridgeFront, "wb_front_before"));
        CaptureWbTopBeforeCommand = new AsyncCommand(() =>
            CaptureAndUploadAsync(CameraDeviceType.WeighbridgeTop, "wb_top_before"));
        CaptureWbFrontAfterCommand = new AsyncCommand(() =>
            CaptureAndUploadAsync(CameraDeviceType.WeighbridgeFront, "wb_front_after"));
        CaptureWbTopAfterCommand = new AsyncCommand(() =>
            CaptureAndUploadAsync(CameraDeviceType.WeighbridgeTop, "wb_top_after"));

        CapturePfFrontBeforeCommand = new AsyncCommand(() =>
            CaptureAndUploadAsync(CameraDeviceType.PlatformFront, "pf_front_before"));
        CapturePfTopBeforeCommand = new AsyncCommand(() =>
            CaptureAndUploadAsync(CameraDeviceType.PlatformTop, "pf_top_before"));
        CapturePfFrontAfterCommand = new AsyncCommand(() =>
            CaptureAndUploadAsync(CameraDeviceType.PlatformFront, "pf_front_after"));
        CapturePfTopAfterCommand = new AsyncCommand(() =>
            CaptureAndUploadAsync(CameraDeviceType.PlatformTop, "pf_top_after"));

        // Optional tab navigation (unused in current XAML but kept for later)
        GoDashboardCommand = new AsyncCommand(() =>
        {
            SelectedTabIndex = 0;
            return Task.CompletedTask;
        });

        GoCustomerCommand = new AsyncCommand(() =>
        {
            SelectedTabIndex = 1;
            return Task.CompletedTask;
        });

        GoTicketsCommand = new AsyncCommand(() =>
        {
            SelectedTabIndex = 2;
            return Task.CompletedTask;
        });

        GoDocumentsCommand = new AsyncCommand(() =>
        {
            SelectedTabIndex = 3;
            return Task.CompletedTask;
        });

        GoCameraCommand = new AsyncCommand(() =>
        {
            SelectedTabIndex = 4;
            return Task.CompletedTask;
        });
    }

    // --- Command implementations (unchanged from yours except comments) ---

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

        if (Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var loginWindow = new MetalLink.Desktop.Views.LoginWindow
            {
                DataContext = new LoginViewModel(_app)
            };

            var current = desktop.MainWindow;
            desktop.MainWindow = loginWindow;
            current?.Close();
        }

        return Task.CompletedTask;
    }

    private Task SwitchSectionAsync(EnumMainSection section)
    {
        CurrentSection = section;
        StatusMessage = $"Section switched to: {section}.";
        return Task.CompletedTask;
    }

    // Search, CreateCustomerAsync, CreateTicketAsync, ReadWeighbridgeAsync,
    // ReadPlatformAsync, LoadCustomerDocumentsAsync, UploadCustomerDocumentAsync,
    // CaptureAndUploadAsync are identical to your latest versions, so I’ve left
    // them unchanged for brevity:

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

    private async Task LoadCustomerDocumentsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Loading customer documents...";

        try
        {
            if (!long.TryParse(DocumentsCustomerIdText, out var customerId))
            {
                StatusMessage = "Please enter a valid numeric Customer ID for documents.";
                DocumentsSummary = "No documents loaded.";
                return;
            }

            var docs = await _documentService.GetDocumentsAsync(customerId);

            if (docs == null || docs.Count == 0)
            {
                DocumentsSummary = "No documents found for this customer.";
                StatusMessage = "No documents found.";
                return;
            }

            var lines = docs
                .OrderBy(d => d.CreatedTime)
                .Select(d =>
                    $"ID: {d.CustomerDocumentId}, Type: {d.DocumentType}, File: {d.FileName}, Created: {d.CreatedTime:yyyy-MM-dd HH:mm}, Url: {d.Url}");

            DocumentsSummary = string.Join(Environment.NewLine, lines);
            StatusMessage = $"Loaded {docs.Count} document(s).";
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = $"Error calling API: {ex.Message}";
            DocumentsSummary = "Error loading documents.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UploadCustomerDocumentAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Uploading document...";

        try
        {
            if (!long.TryParse(DocumentsCustomerIdText, out var customerId))
            {
                StatusMessage = "Please enter a valid numeric Customer ID for documents.";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewDocumentType))
            {
                StatusMessage = "Document type is required (e.g. id_front, id_back, signature).";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewDocumentFilePath))
            {
                StatusMessage = "File path is required.";
                return;
            }

            var doc = await _documentService.UploadDocumentAsync(
                customerId,
                NewDocumentType,
                NewDocumentFilePath
            );

            if (doc == null)
            {
                StatusMessage = "Document upload failed (no response).";
                return;
            }

            StatusMessage = $"Uploaded document {doc.FileName} as {doc.DocumentType}.";
            await LoadCustomerDocumentsAsync();

            NewDocumentFilePath = string.Empty;
        }
        catch (FileNotFoundException ex)
        {
            StatusMessage = $"File not found: {ex.FileName}";
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = $"Error calling API: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error uploading document: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CaptureAndUploadAsync(CameraDeviceType deviceType, string documentType)
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Capturing image and uploading...";

        try
        {
            if (!long.TryParse(DocumentsCustomerIdText, out var customerId))
            {
                StatusMessage = "Please enter a valid numeric Customer ID (in Customer Documents section) before capturing.";
                return;
            }

            var capture = await _cameraService.CaptureAsync(deviceType, documentType);
            LastCameraCaptureSummary = capture.ToString();

            var doc = await _documentService.UploadDocumentAsync(
                customerId,
                capture.DocumentType,
                capture.FilePath
            );

            if (doc == null)
            {
                StatusMessage = "Camera capture upload failed (no response).";
                return;
            }

            StatusMessage = $"Captured and uploaded {capture.DocumentType} from {deviceType}.";
            await LoadCustomerDocumentsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error during capture/upload: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Helpers ---

    protected new void OnPropertyChanged([CallerMemberName] string? name = null)
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

    private sealed class HealthResponse
    {
        public string status { get; set; } = string.Empty;
        public int customersCount { get; set; }
    }
}
