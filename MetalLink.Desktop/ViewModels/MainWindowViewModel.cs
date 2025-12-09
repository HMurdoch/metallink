using System;
using System.Collections.ObjectModel;
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
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Avalonia;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView.Painting;


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
    private readonly TicketReportService _ticketReportService;
    
    private readonly ISignaturePadService _signaturePadService;

    private string _title = "Metal Link Desktop";
    private string _statusMessage = "Ready.";
    private bool _isBusy;

    // --- Menu / sections ---

    private EnumMainSection _currentSection = EnumMainSection.Dashboard;
    private EnumMainSection _previousSection = EnumMainSection.Dashboard;

    // --- Dashboard counters ---
    private int _customersLoadedCount;
    private int _ticketsCreatedCount;

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

    // Pie chart: tickets by type
    public ISeries[] TicketsByTypeSeries { get; set; } = Array.Empty<ISeries>();

    // Line chart: tickets per day
    public ISeries[] TicketsPerDaySeries { get; set; } = Array.Empty<ISeries>();
    public Axis[] TicketsPerDayXAxis { get; set; } = Array.Empty<Axis>();


    // Section visibility, used by XAML
    public bool IsDashboardSectionVisible => CurrentSection == EnumMainSection.Dashboard;
    public bool IsCustomerSectionVisible  => CurrentSection == EnumMainSection.Customers;
    public bool IsTicketSectionVisible    => CurrentSection == EnumMainSection.Tickets;
    public bool IsDocumentSectionVisible  => CurrentSection == EnumMainSection.Documents;
    public bool IsCameraSectionVisible    => CurrentSection == EnumMainSection.Camera;

    // --- Validation flags (your originals) ---

    // Require at least a name (first/last) or a company name
    public bool IsNewCustomerFullNameInvalid =>
        string.IsNullOrWhiteSpace(NewFirstName)
        && string.IsNullOrWhiteSpace(NewLastName)
        && string.IsNullOrWhiteSpace(NewCompanyName);

    public bool HasUnsavedNewCustomer =>
        !string.IsNullOrWhiteSpace(NewFirstName)
        || !string.IsNullOrWhiteSpace(NewLastName)
        || NewIsCompany
        || !string.IsNullOrWhiteSpace(NewCompanyName)
        || !string.IsNullOrWhiteSpace(NewIdNumber)
        || !string.IsNullOrWhiteSpace(NewAccountNumber)
        || !string.IsNullOrWhiteSpace(NewPriceCode)
        || !string.IsNullOrWhiteSpace(NewAddressLine1)
        || !string.IsNullOrWhiteSpace(NewAddressLine2)
        || !string.IsNullOrWhiteSpace(NewSuburb)
        || !string.IsNullOrWhiteSpace(NewCity)
        || !string.IsNullOrWhiteSpace(NewPostalCode)
        || !string.IsNullOrWhiteSpace(NewPhoneNumber)
        || !string.IsNullOrWhiteSpace(NewMobileNumber)
        || !string.IsNullOrWhiteSpace(NewEmail);



    public bool IsTicketCustomerIdInvalid => !long.TryParse(TicketCustomerIdText, out _);
    public bool IsTicketNumberInvalid     => string.IsNullOrWhiteSpace(TicketNumber);
    public bool IsTicketUnitPriceInvalid  => !decimal.TryParse(TicketUnitPriceText, out _);

    public bool IsDocumentsCustomerIdInvalid  => !long.TryParse(DocumentsCustomerIdText, out _);
    public bool IsNewDocumentTypeInvalid      => string.IsNullOrWhiteSpace(NewDocumentType);
    public bool IsNewDocumentFilePathInvalid  => string.IsNullOrWhiteSpace(NewDocumentFilePath);

    // --- Unsaved state flags (your originals) ---

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
    private string _searchSiteIdText = string.Empty;
    private string _searchFirstNameText = string.Empty;
    private string _searchLastNameText = string.Empty;
    private string _searchCompanyNameText = string.Empty;
    private string _searchIdNumberText = string.Empty;
    private string _searchAccountNumberText = string.Empty;
    private string _searchPriceCodeText = string.Empty;
    private string _searchAddressLine1Text = string.Empty;
    private string _searchAddressLine2Text = string.Empty;
    private string _searchSuburbText = string.Empty;
    private string _searchCityText = string.Empty;
    private string _searchPostalCodeText = string.Empty;
    private string _searchPhoneNumberText = string.Empty;
    private string _searchMobileNumberText = string.Empty;
    private string _searchEmailText = string.Empty;

    private ObservableCollection<CustomerDto> _customerSearchResults = new();

    private CustomerDto? _foundCustomer;
    private int _totalCustomersInDb;
    private int _totalTicketsInDb;

    // --- New customer form ---
    private string _newFirstName = string.Empty;
    private string _newLastName = string.Empty;
    private bool _newIsCompany;
    private string? _newCompanyName;
    private string _newAddressLine1 = string.Empty;
    private string _newAddressLine2 = string.Empty;
    private string _newSuburb = string.Empty;
    private string _newCity = string.Empty;
    private string _newPostalCode = string.Empty;
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

    // --- Ticket report ---
    private string _ticketReportTicketIdText = string.Empty;
    private string _lastTicketReportPath = "No ticket report downloaded yet.";

    // --- Signature ---
    private string _lastSignatureCaptureSummary = "No signature captured yet.";

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

    public string SearchSiteIdText
    {
        get => _searchSiteIdText;
        set { _searchSiteIdText = value; OnPropertyChanged(); }
    }

    public string SearchFirstNameText
    {
        get => _searchFirstNameText;
        set { _searchFirstNameText = value; OnPropertyChanged(); }
    }

    public string SearchLastNameText
    {
        get => _searchLastNameText;
        set { _searchLastNameText = value; OnPropertyChanged(); }
    }

    public string SearchCompanyNameText
    {
        get => _searchCompanyNameText;
        set { _searchCompanyNameText = value; OnPropertyChanged(); }
    }

    public string SearchIdNumberText
    {
        get => _searchIdNumberText;
        set { _searchIdNumberText = value; OnPropertyChanged(); }
    }

    public string SearchAccountNumberText
    {
        get => _searchAccountNumberText;
        set { _searchAccountNumberText = value; OnPropertyChanged(); }
    }

    public string SearchPriceCodeText
    {
        get => _searchPriceCodeText;
        set { _searchPriceCodeText = value; OnPropertyChanged(); }
    }

    public string SearchAddressLine1Text
    {
        get => _searchAddressLine1Text;
        set { _searchAddressLine1Text = value; OnPropertyChanged(); }
    }

    public string SearchAddressLine2Text
    {
        get => _searchAddressLine2Text;
        set { _searchAddressLine2Text = value; OnPropertyChanged(); }
    }

    public string SearchSuburbText
    {
        get => _searchSuburbText;
        set { _searchSuburbText = value; OnPropertyChanged(); }
    }

    public string SearchCityText
    {
        get => _searchCityText;
        set { _searchCityText = value; OnPropertyChanged(); }
    }

    public string SearchPostalCodeText
    {
        get => _searchPostalCodeText;
        set { _searchPostalCodeText = value; OnPropertyChanged(); }
    }

    public string SearchPhoneNumberText
    {
        get => _searchPhoneNumberText;
        set { _searchPhoneNumberText = value; OnPropertyChanged(); }
    }

    public string SearchMobileNumberText
    {
        get => _searchMobileNumberText;
        set { _searchMobileNumberText = value; OnPropertyChanged(); }
    }

    public string SearchEmailText
    {
        get => _searchEmailText;
        set { _searchEmailText = value; OnPropertyChanged(); }
    }

    public ObservableCollection<CustomerDto> CustomerSearchResults
    {
        get => _customerSearchResults;
        set { _customerSearchResults = value; OnPropertyChanged(); }
    }

    public int TotalCustomersInDb
    {
        get => _totalCustomersInDb;
        set { _totalCustomersInDb = value; OnPropertyChanged(); }
    }

    public int TotalTicketsInDb
    {
        get => _totalTicketsInDb;
        set { _totalTicketsInDb = value; OnPropertyChanged(); }
    }

    public string FoundCustomerSummary
    {
        get
        {
            if (FoundCustomer == null) return "No customer loaded.";
            return $"ID: {FoundCustomer.CustomerId}, Name: {FoundCustomer.FullName}, Account: {FoundCustomer.AccountNumber ?? "-"}";
        }
    }

    // --- Customer search ---

    public CustomerDto? FoundCustomer
    {
        get => _foundCustomer;
        set
        {
            _foundCustomer = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FoundCustomerSummary));
            OnPropertyChanged(nameof(SelectedCustomerIdDisplay));
            OnPropertyChanged(nameof(SelectedFirstName));
            OnPropertyChanged(nameof(SelectedLastName));
            OnPropertyChanged(nameof(SelectedCompanyName));
            OnPropertyChanged(nameof(SelectedIdNumber));
            OnPropertyChanged(nameof(SelectedAccountNumber));
            OnPropertyChanged(nameof(SelectedPriceCode));
            OnPropertyChanged(nameof(SelectedAddressLine1));
            OnPropertyChanged(nameof(SelectedAddressLine2));
            OnPropertyChanged(nameof(SelectedSuburb));
            OnPropertyChanged(nameof(SelectedCity));
            OnPropertyChanged(nameof(SelectedPostalCode));
            OnPropertyChanged(nameof(SelectedPhoneNumber));
            OnPropertyChanged(nameof(SelectedMobileNumber));
            OnPropertyChanged(nameof(SelectedEmail));
        }
    }

    // 8-digit, zero-padded Customer ID
    public string SelectedCustomerIdDisplay =>
        FoundCustomer == null ? string.Empty : FoundCustomer.CustomerId.ToString("D8");

    // For now we split FullName into first & last name for display.
    // Later we can move to dedicated FirstName / LastName columns in the DB.
    private (string first, string last) SplitName()
    {
        if (FoundCustomer == null || string.IsNullOrWhiteSpace(FoundCustomer.FullName))
            return (string.Empty, string.Empty);

        var parts = FoundCustomer.FullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return (string.Empty, string.Empty);
        if (parts.Length == 1) return (parts[0], string.Empty);
        return (parts[0], parts[1]);
    }

    public string SelectedFirstName => SplitName().first;
    public string SelectedLastName  => SplitName().last;

    public string SelectedCompanyName => FoundCustomer?.CompanyName ?? string.Empty;
    public string SelectedIdNumber    => FoundCustomer?.IdNumber ?? string.Empty;
    public string SelectedAccountNumber => FoundCustomer?.AccountNumber ?? string.Empty;
    public string SelectedPriceCode   => FoundCustomer?.PriceCode ?? string.Empty;
    public string SelectedAddressLine1 => FoundCustomer?.AddressLine1 ?? string.Empty;
    public string SelectedAddressLine2 => FoundCustomer?.AddressLine2 ?? string.Empty;
    public string SelectedSuburb      => FoundCustomer?.Suburb ?? string.Empty;
    public string SelectedCity        => FoundCustomer?.City ?? string.Empty;
    public string SelectedPostalCode  => FoundCustomer?.PostalCode ?? string.Empty;
    public string SelectedPhoneNumber => FoundCustomer?.PhoneNumber ?? string.Empty;
    public string SelectedMobileNumber => FoundCustomer?.MobileNumber ?? string.Empty;
    public string SelectedEmail       => FoundCustomer?.Email ?? string.Empty;

    // --- New customer form ---

    // --- New customer form ---

    public string NewFirstName
    {
        get => _newFirstName;
        set
        {
            _newFirstName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNewCustomerFullNameInvalid));
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public string NewLastName
    {
        get => _newLastName;
        set
        {
            _newLastName = value;
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

    public string NewAddressLine1
    {
        get => _newAddressLine1;
        set
        {
            _newAddressLine1 = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public string NewAddressLine2
    {
        get => _newAddressLine2;
        set
        {
            _newAddressLine2 = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public string NewSuburb
    {
        get => _newSuburb;
        set
        {
            _newSuburb = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public string NewCity
    {
        get => _newCity;
        set
        {
            _newCity = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    public string NewPostalCode
    {
        get => _newPostalCode;
        set
        {
            _newPostalCode = value;
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

    // --- Signature properties ---

    public string LastSignatureCaptureSummary
    {
        get => _lastSignatureCaptureSummary;
        set { _lastSignatureCaptureSummary = value; OnPropertyChanged(); }
    }

    public int CustomersLoadedCount
    {
        get => _customersLoadedCount;
        set { _customersLoadedCount = value; OnPropertyChanged(); }
    }

    public int TicketsCreatedCount
    {
        get => _ticketsCreatedCount;
        set { _ticketsCreatedCount = value; OnPropertyChanged(); }
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

    // Ticket Report commands
    public ICommand DownloadTicketReportCommand { get; }


    // Optional tab navigation commands
    public ICommand GoDashboardCommand { get; }
    public ICommand GoCustomerCommand { get; }
    public ICommand GoTicketsCommand { get; }
    public ICommand GoDocumentsCommand { get; }
    public ICommand GoCameraCommand { get; }

    // Signature command
    public ICommand CaptureSignatureCommand { get; }


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
        _ticketReportService = app.TicketReportService;
        _signaturePadService = app.SignaturePadService;

        _ = LoadDashboardStatsAsync();
        
        // Demo – you can later wire these to API stats
        TicketsByTypeSeries = new ISeries[]
        {
            new PieSeries<int> { Values = new[] { 60 }, Name = "Weighbridge" },
            new PieSeries<int> { Values = new[] { 40 }, Name = "Platform" }
        };

        TicketsPerDaySeries = new ISeries[]
        {
            new LineSeries<int>
            {
                Name = "Tickets per day",
                Values = new[] { 4, 7, 3, 9, 5, 2, 8 }
            }
        };

        TicketsPerDayXAxis = new[]
        {
            new Axis
            {
                Labels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" }
            }
        };

        
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

        //Ticket Report Command
        DownloadTicketReportCommand = new AsyncCommand(DownloadTicketReportAsync);

        // Signature
        CaptureSignatureCommand = new AsyncCommand(CaptureSignatureAsync);

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
        StatusMessage = "Searching customers...";

        try
        {
            long? customerId = null;
            if (long.TryParse(SearchCustomerIdText, out var id))
                customerId = id;

            long? siteId = null;
            if (long.TryParse(SearchSiteIdText, out var sid))
                siteId = sid;

            // Build a request object with all filters (null / empty = ignore)
            var request = new CustomerSearchRequestDto
            {
                CustomerId    = customerId,
                SiteId        = siteId,
                FirstName     = string.IsNullOrWhiteSpace(SearchFirstNameText) ? null : SearchFirstNameText,
                LastName      = string.IsNullOrWhiteSpace(SearchLastNameText) ? null : SearchLastNameText,
                CompanyName   = string.IsNullOrWhiteSpace(SearchCompanyNameText) ? null : SearchCompanyNameText,
                IdNumber      = string.IsNullOrWhiteSpace(SearchIdNumberText) ? null : SearchIdNumberText,
                AccountNumber = string.IsNullOrWhiteSpace(SearchAccountNumberText) ? null : SearchAccountNumberText,
                PriceCode     = string.IsNullOrWhiteSpace(SearchPriceCodeText) ? null : SearchPriceCodeText,
                AddressLine1  = string.IsNullOrWhiteSpace(SearchAddressLine1Text) ? null : SearchAddressLine1Text,
                AddressLine2  = string.IsNullOrWhiteSpace(SearchAddressLine2Text) ? null : SearchAddressLine2Text,
                Suburb        = string.IsNullOrWhiteSpace(SearchSuburbText) ? null : SearchSuburbText,
                City          = string.IsNullOrWhiteSpace(SearchCityText) ? null : SearchCityText,
                PostalCode    = string.IsNullOrWhiteSpace(SearchPostalCodeText) ? null : SearchPostalCodeText,
                PhoneNumber   = string.IsNullOrWhiteSpace(SearchPhoneNumberText) ? null : SearchPhoneNumberText,
                MobileNumber  = string.IsNullOrWhiteSpace(SearchMobileNumberText) ? null : SearchMobileNumberText,
                Email         = string.IsNullOrWhiteSpace(SearchEmailText) ? null : SearchEmailText
            };

            var results = await _customerService.SearchCustomersAsync(request);

            CustomerSearchResults.Clear();
            if (results != null)
            {
                foreach (var c in results)
                    CustomerSearchResults.Add(c);
            }

            if (CustomerSearchResults.Count == 0)
            {
                StatusMessage = "No customers found.";
                FoundCustomer = null;
            }
            else
            {
                StatusMessage = $"Found {CustomerSearchResults.Count} customer(s).";
                FoundCustomer = CustomerSearchResults[0];
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
            // Build FullName from First + Last
            var fullName = string.Join(" ",
                new[] { NewFirstName, NewLastName }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));

            if (string.IsNullOrWhiteSpace(fullName) &&
                string.IsNullOrWhiteSpace(NewCompanyName))
            {
                StatusMessage = "Please capture a First/Last name or a Company Name.";
                return;
            }

            var customer = await _customerService.CreateCustomerAsync(
                fullName: fullName,
                isCompany: NewIsCompany,
                companyName: NewCompanyName,
                idNumber: NewIdNumber,
                accountNumber: NewAccountNumber,
                priceCode: NewPriceCode,
                addressLine1: string.IsNullOrWhiteSpace(NewAddressLine1) ? null : NewAddressLine1,
                addressLine2: string.IsNullOrWhiteSpace(NewAddressLine2) ? null : NewAddressLine2,
                suburb:      string.IsNullOrWhiteSpace(NewSuburb)      ? null : NewSuburb,
                city:        string.IsNullOrWhiteSpace(NewCity)        ? null : NewCity,
                postalCode:  string.IsNullOrWhiteSpace(NewPostalCode)  ? null : NewPostalCode,
                phoneNumber: NewPhoneNumber,
                mobileNumber: NewMobileNumber,
                email: NewEmail
            );

            if (customer == null)
            {
                StatusMessage = "Customer could not be created (no response).";
                return;
            }

            StatusMessage = $"Customer created: ID {customer.CustomerId:D8}, {customer.FullName}.";
            FoundCustomer = customer;   // this makes the summary panel show the new customer

            // Clear form
            NewFirstName = string.Empty;
            NewLastName  = string.Empty;
            NewIsCompany = false;
            NewCompanyName = null;
            NewIdNumber = null;
            NewAccountNumber = null;
            NewPriceCode = null;
            NewAddressLine1 = string.Empty;
            NewAddressLine2 = string.Empty;
            NewSuburb = string.Empty;
            NewCity = string.Empty;
            NewPostalCode = string.Empty;
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
            TicketsCreatedCount++;
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

    private async Task DownloadTicketReportAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        StatusMessage = "Downloading ticket PDF...";

        try
        {
            if (!long.TryParse(TicketReportTicketIdText, out var ticketId))
            {
                StatusMessage = "Please enter a valid numeric Ticket ID.";
                return;
            }

            var path = await _ticketReportService.DownloadTicketReportAsync(ticketId);
            LastTicketReportPath = path;
            StatusMessage = $"Ticket PDF saved to: {path}";
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = $"Error calling API: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error downloading ticket report: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CaptureSignatureAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Capturing signature and uploading...";

        try
        {
            // Use the same Customer ID as the Customer Documents section
            if (!long.TryParse(DocumentsCustomerIdText, out var customerId))
            {
                StatusMessage =
                    "Please enter a valid numeric Customer ID in the Customer Documents section before capturing signature.";
                return;
            }

            const string documentType = "signature";

            // Simulate pad capture (currently using MockSignaturePadService)
            var capture = await _signaturePadService.CaptureAsync(documentType);
            LastSignatureCaptureSummary = capture.ToString();

            // Upload as a normal customer document
            var doc = await _documentService.UploadDocumentAsync(
                customerId,
                capture.DocumentType,
                capture.FilePath);

            if (doc == null)
            {
                StatusMessage = "Signature upload failed (no response).";
                return;
            }

            StatusMessage = "Signature captured and uploaded.";

            // Refresh the documents list so the new signature appears
            await LoadCustomerDocumentsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error during signature capture/upload: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // animated display values
    private int _animatedTotalCustomersInDb;
    public int AnimatedTotalCustomersInDb
    {
        get => _animatedTotalCustomersInDb;
        set { _animatedTotalCustomersInDb = value; OnPropertyChanged(); }
    }

    private int _animatedTotalTicketsInDb;
    public int AnimatedTotalTicketsInDb
    {
        get => _animatedTotalTicketsInDb;
        set { _animatedTotalTicketsInDb = value; OnPropertyChanged(); }
    }


    private async Task LoadDashboardStatsAsync()
    {
        var health = await _apiClient.GetAsync<HealthResponse>("api/health/db");

        if (health != null)
        {
            TotalCustomersInDb = health.customersCount;
            TotalTicketsInDb   = health.ticketsCount;

            _ = AnimateCounterAsync(TotalCustomersInDb, v => AnimatedTotalCustomersInDb = v);
            _ = AnimateCounterAsync(TotalTicketsInDb,   v => AnimatedTotalTicketsInDb   = v);            
        }
    }

    // --- Helpers ---

    protected new void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private async Task AnimateCounterAsync(int target,
        Action<int> setValue,
        int durationMs = 600)
    {
        if (target < 0) target = 0;

        var frames = Math.Max(1, durationMs / 30); // ~30 fps
        var step = (double)target / frames;

        double current = 0;

        for (int i = 0; i < frames; i++)
        {
            current += step;
            setValue((int)Math.Round(current));
            await Task.Delay(30);
        }

        setValue(target);
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
        public int ticketsCount { get; set; }
    }
}
