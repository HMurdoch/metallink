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
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Avalonia;
using LiveChartsCore.Measure;
using MetalLink.Desktop;
using MetalLink.Desktop.Auth;
using MetalLink.Desktop.Hardware;
using MetalLink.Desktop.Services;
using MetalLink.Shared.Customers;
using MetalLink.Shared.Buyers;
using MetalLink.Shared.Tickets;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Threading;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject, INotifyPropertyChanged
{
    private Task AddLineRoutedAsync() => CurrentSection == EnumMainSection.TicketsSending ? AddSendingLineAsync() : AddReceivingLineAsync();

    private Task CreateTicketHeaderRoutedAsync() => CurrentSection == EnumMainSection.TicketsSending ? CreateSendingTicketHeaderAsync() : CreateTicketHeaderAsync();

    private Task FinalizeTicketRoutedAsync() => CurrentSection == EnumMainSection.TicketsSending ? FinalizeSendingTicketAsync() : FinalizeTicketAsync();

    private Task ClearTicketRoutedAsync() => CurrentSection == EnumMainSection.TicketsSending ? ClearSendingTicketAsync() : ClearTicketAsync();
    // --- Services / dependencies ---

    private readonly App _app;
    private readonly AuthState _authState;
    private readonly ApiClient _apiClient;
    private readonly CustomerService _customerService;
    private readonly BuyerService _buyerService;
    private readonly TicketService _ticketService;
    private readonly TicketReceivingService _ticketReceivingService;
    private readonly TicketSendingService _ticketSendingService;
    private readonly ProvinceService _provinceService;
    private readonly IScaleService _scaleService;
    private readonly DocumentService _documentService;
    private readonly ICameraService _cameraService;
    private readonly TicketReportService _ticketReportService;
    private readonly ISignaturePadService _signaturePadService;
    public new event PropertyChangedEventHandler? PropertyChanged;

    // Commands
    public ICommand CheckDbCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand SearchCustomerCommand { get; }
    public ICommand CreateCustomerCommand { get; }
    public ICommand CreateBuyerCommand { get; }
    public ICommand CreateTicketCommand { get; }
    public ICommand FinalizeTicketCommand { get; }
    public ICommand AddReceivingLineCommand { get; }
    public ICommand RemoveReceivingLineCommand { get; }
    public ICommand RemoveSendingLineCommand { get; }
    public ICommand ReadWeighbridgeCommand { get; }
    public ICommand ReadWeighbridgeSecondCommand { get; }
    public ICommand ReadPlatformCommand { get; }
    public ICommand ResetWeighbridgeWeightsCommand { get; }
    public ICommand ResetPlatformWeightCommand { get; }
    public ICommand LoadCustomerDocumentsCommand { get; }
    public ICommand UploadCustomerDocumentCommand { get; }

    // Section navigation
    public ICommand ShowDashboardCommand { get; }
    public ICommand ShowCustomersCommand { get; }
    public ICommand ShowBuyersCommand { get; }
    public ICommand ShowCompanyAndSitesCommand { get; }
    public ICommand ShowProductsAndPricesCommand { get; }
    public ICommand ShowTicketsCommand { get; }
    public ICommand ShowTicketsReceivingCommand { get; }
    public ICommand ShowTicketsSendingCommand { get; }
    public ICommand ShowDocumentsCommand { get; }
    public ICommand ShowCameraCommand { get; }
    public ICommand ShowReportsCommand { get; }   // ✅ ADDED
    public ICommand ShowStockLevelsCommand { get; }
    public ICommand ShowStockMovementCommand { get; }
    public ICommand ShowSettingsCommand { get; }  // ✅ ADDED

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

    // Ticket search commands
    public ICommand SearchTicketsCommand { get; }
    public ICommand ClearTicketSearchCommand { get; }
    public ICommand DeleteTicketCommand { get; }
    public ICommand EditTicketCommand { get; }
    public ICommand CancelEditTicketCommand { get; }
    
    // Receiving ticket search commands
    public ICommand SearchReceivingTicketsCommand { get; }
    public ICommand ClearReceivingTicketSearchCommand { get; }
    public ICommand DeleteReceivingTicketCommand { get; }
    public ICommand PrintReceivingTicketCommand { get; }
    public ICommand ShowLineNotesCommand { get; }
    public ICommand CloseLineNotesCommand { get; }
    
    // Sending ticket search commands
    public ICommand SearchSendingTicketsCommand { get; }
    public ICommand ClearSendingTicketSearchCommand { get; }
    public ICommand DeleteSendingTicketCommand { get; }
    public ICommand PrintSendingTicketCommand { get; }
    
    // Ticket line commands
    public ICommand EditTicketLineCommand { get; }
    public ICommand DeleteTicketLineCommand { get; }

    // Buyers
    public ICommand EditBuyerCommand { get; }
    public ICommand DeleteBuyerCommand { get; }
    public ICommand LogBuyerTicketCommand { get; }

    // Optional tab navigation commands
    public ICommand GoDashboardCommand { get; }
    public ICommand GoCustomerCommand { get; }
    public ICommand GoTicketsCommand { get; }
    public ICommand GoDocumentsCommand { get; }
    public ICommand GoCameraCommand { get; }

    // Signature command
    public ICommand CaptureSignatureCommand { get; }

    // Ticket receiving commands
    public ICommand AddLineCommand { get; }
    public ICommand RemoveLineCommand { get; }
    public ICommand SaveTicketCommand { get; }
    public ICommand ClearTicketCommand { get; }
    public ICommand CaptureWeightCommand { get; }
    public ICommand CapturePlatePhotoCommand { get; }
    public ICommand CaptureLoadPhotoCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public ICommand PrintTicketCommand { get; }
    public ICommand ScrollToAddLinesCommand { get; }
    public ICommand CreateTicketHeaderCommand { get; }
    public ICommand SaveEditedTicketLineCommand { get; }
    public ICommand CancelEditTicketLineCommand { get; }

    // Customer image capture commands
    public ICommand CaptureIdCardCommand { get; }
    public ICommand CaptureDriverLicenseCommand { get; }
    public ICommand CapturePhotoCommand { get; }
    public ICommand CaptureFingerprintCommand { get; }

    public ICommand EditCustomerCommand { get; }
    public ICommand DeleteCustomerCommand { get; }
    public ICommand LogTicketCommand { get; }
    public ICommand ClearNewCustomerCommand { get; }
    public ICommand ClearNewBuyerCommand { get; }
    public ICommand ClearCustomerSearchCommand { get; }
    public ICommand ClearBuyerSearchCommand { get; }
    public ICommand UpdateCustomerCommand { get; }
    public ICommand UpdateBuyerCommand { get; }
    public ICommand SearchCustomersCommand { get; }
    public ICommand SearchBuyersCommand { get; }

    // BuyersView.axaml binds to this name
    public ICommand SearchBuyerCommand { get; }

    // Ticket state from selected ticket
    private char _currentTicketState = 'C';
    public char CurrentTicketState
    {
        get => _currentTicketState;
        set
        {
            _currentTicketState = value;
            Console.WriteLine($"[DEBUG] CurrentTicketState changed to: {value}");
            Console.WriteLine($"[DEBUG] CreateHeaderButtonVisible: {CreateHeaderButtonVisible}");
            Console.WriteLine($"[DEBUG] SaveResetButtonVisible: {SaveResetButtonVisible}");
            Console.WriteLine($"[DEBUG] AddLineButtonEnabled: {AddLineButtonEnabled}");
            Console.WriteLine($"[DEBUG] IsFinalizeTicketEnabled: {IsFinalizeTicketEnabled}");
            Console.WriteLine($"[DEBUG] FirstWeightReadResetEnabled: {FirstWeightReadResetEnabled}");
            Console.WriteLine($"[DEBUG] SecondWeightReadResetEnabled: {SecondWeightReadResetEnabled}");
            OnPropertyChanged();
            OnPropertyChanged(nameof(CreateHeaderButtonVisible));
            OnPropertyChanged(nameof(SaveResetButtonVisible));
            OnPropertyChanged(nameof(AddLineButtonEnabled));
            OnPropertyChanged(nameof(IsFinalizeTicketEnabled));
            OnPropertyChanged(nameof(FirstWeightReadResetEnabled));
            OnPropertyChanged(nameof(SecondWeightReadResetEnabled));
            OnPropertyChanged(nameof(AreFirstWeightButtonsEnabled));
            OnPropertyChanged(nameof(AreSecondWeightButtonsEnabled));
        }
    }

    // Button visibility/enabled based on ticket state
    public bool CreateHeaderButtonVisible
    {
        get
        {
            var visible = CurrentTicketState == 'C';
            Console.WriteLine($"[DEBUG GETTER] CreateHeaderButtonVisible: CurrentTicketState='{CurrentTicketState}', ('{CurrentTicketState}' == 'C') = {visible}");
            return visible;
        }
    }

    public bool SaveResetButtonVisible
    {
        get
        {
            var visible = CurrentTicketState == 'H' || CurrentTicketState == 'M';
            Console.WriteLine($"[DEBUG GETTER] SaveResetButtonVisible: CurrentTicketState='{CurrentTicketState}', ('{CurrentTicketState}' == 'H' || '{CurrentTicketState}' == 'M') = {visible}");
            return visible;
        }
    }

    public bool AddLineButtonEnabled
    {
        get
        {
            var enabled = CurrentTicketState != 'C';
            Console.WriteLine($"[DEBUG] AddLineButtonEnabled getter called: state={CurrentTicketState}, enabled={enabled}");
            return enabled;
        }
    }

    // First Weight button enabled - only when ticket state is 'C' (not yet created)
    public bool FirstWeightReadResetEnabled
    {
        get
        {
            var enabled = CurrentTicketState == 'C';
            Console.WriteLine($"[DEBUG] FirstWeightReadResetEnabled getter called: state={CurrentTicketState}, enabled={enabled}");
            return enabled;
        }
    }

    // Second Weight button enabled - only when ticket state is 'H' or 'M' (ticket created)
    public bool SecondWeightReadResetEnabled
    {
        get
        {
            var enabled = CurrentTicketState == 'H' || CurrentTicketState == 'M';
            Console.WriteLine($"[DEBUG] SecondWeightReadResetEnabled getter called: state={CurrentTicketState}, enabled={enabled}");
            return enabled;
        }
    }

    // Aliases for XAML bindings - First Weight buttons enabled when state is 'C'
    public bool AreFirstWeightButtonsEnabled => FirstWeightReadResetEnabled;

    // Aliases for XAML bindings - Second Weight buttons enabled when state is 'H' or 'M'
    public bool AreSecondWeightButtonsEnabled => SecondWeightReadResetEnabled;

    public bool IsFinalizeTicketEnabled
    {
        get
        {
            var enabled = CurrentTicketState == 'H' || CurrentTicketState == 'M';
            Console.WriteLine($"[DEBUG] IsFinalizeTicketEnabled getter called: state={CurrentTicketState}, enabled={enabled}");
            return enabled;
        }
    }

    private string? _ticketTrailerRegistration;
    public string? TicketTrailerRegistration
    {
        get => _ticketTrailerRegistration;
        set
        {
            if (_ticketTrailerRegistration != value)
            {
                _ticketTrailerRegistration = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _ticketDriverName;
    public string? TicketDriverName
    {
        get => _ticketDriverName;
        set
        {
            if (_ticketDriverName != value)
            {
                _ticketDriverName = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _ticketDeliveryNumber;
    public string? TicketDeliveryNumber
    {
        get => _ticketDeliveryNumber;
        set
        {
            if (_ticketDeliveryNumber != value)
            {
                _ticketDeliveryNumber = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _ticketPlatformWeightText;
    public string? TicketPlatformWeightText
    {
        get => _ticketPlatformWeightText;
        set
        {
            if (_ticketPlatformWeightText != value)
            {
                _ticketPlatformWeightText = value;
                OnPropertyChanged();
            }
        }
    }
    // Show ticket details only if NOT searching for "new" entities AND a ticket is selected
    // This is shared by both Receiving and Sending views so the UI can bind identically.
    public bool ShouldShowTicketDetails
    {
        get
        {
            var show = CurrentSection switch
            {
                EnumMainSection.TicketsReceiving => !SearchReceivingNewCustomersCheckbox && HasSelectedReceivingTicket,
                EnumMainSection.TicketsSending => !SearchSendingNewBuyersCheckbox && HasSelectedSendingTicket,
                _ => false
            };

            Console.WriteLine(
                $"[DEBUG] ShouldShowTicketDetails: Section={CurrentSection}, " +
                $"SearchReceivingNewCustomersCheckbox={SearchReceivingNewCustomersCheckbox}, HasSelectedReceivingTicket={HasSelectedReceivingTicket}, " +
                $"SearchSendingNewBuyersCheckbox={SearchSendingNewBuyersCheckbox}, HasSelectedSendingTicket={HasSelectedSendingTicket}, " +
                $"show={show}");

            return show;
        }
    }

    /// <summary>
    /// Calculated Net Weight as SUM(WeightKg) - SUM(Tare) from the currently selected ticket's line items.
    /// Shared by Receiving + Sending so both UIs can bind identically.
    /// Falls back to the selected ticket's NetWeightKg if no lines are loaded.
    /// </summary>
    public decimal CalculatedNetWeightKg
    {
        get
        {
            return CurrentSection switch
            {
                EnumMainSection.TicketsReceiving =>
                    SelectedReceivingTicketLines.Count > 0
                        ? SelectedReceivingTicketLines.Sum(l => l.WeightKg) - SelectedReceivingTicketLines.Sum(l => l.Tare)
                        : (SelectedReceivingTicketDetails?.NetWeightKg ?? 0m),

                EnumMainSection.TicketsSending =>
                    SelectedSendingTicketLines.Count > 0
                        ? SelectedSendingTicketLines.Sum(l => l.WeightKg) - SelectedSendingTicketLines.Sum(l => l.Tare)
                        : (SelectedSendingTicketDetails?.NetWeightKg ?? 0m),

                _ => 0m
            };
        }
    }

    /// <summary>
    /// Total weight from line items being created in the Create/Edit section.
    /// Shared by Receiving + Sending so both UIs can bind identically.
    /// </summary>
    public decimal CreatingTicketTotalWeight
    {
        get
        {
            return CurrentSection switch
            {
                EnumMainSection.TicketsReceiving => ReceivingLinesTotalWeight,
                EnumMainSection.TicketsSending => SendingLinesTotalWeight,
                _ => 0m
            };
        }
    }

    public MainWindowViewModel(App app)
    {
        _app = app;
        _authState = app.AuthState;
        _apiClient = app.ApiClient;
        _customerService = app.CustomerService;
        _buyerService = app.BuyerService;
        _ticketService = app.TicketService;
        _ticketReceivingService = app.TicketReceivingService;
        _ticketSendingService = app.TicketSendingService;
        _provinceService = app.ProvinceService;
        _scaleService = app.ScaleService;
        _documentService = app.DocumentService;
        _cameraService = app.CameraService;
        _ticketReportService = app.TicketReportService;
        _signaturePadService = app.SignaturePadService;

        // Initialize ticket type options for search view
        InitializeTicketTypeOptions();

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

        SelectedTabIndex = 0;
        IsSearchSiteEnabled = false;

        // Core commands
        CheckDbCommand = new AsyncCommand(CheckDbAsync);
        LogoutCommand = new AsyncCommand(LogoutAsync);
        SearchCustomerCommand = new AsyncCommand(SearchCustomerAsync);
        CreateCustomerCommand = new AsyncCommand(CreateCustomerAsync);
        CreateBuyerCommand = new AsyncCommand(CreateBuyerAsync);
        CreateTicketCommand = new AsyncCommand(CreateTicketAsync);
        FinalizeTicketCommand = new AsyncCommand(FinalizeTicketRoutedAsync);
        AddReceivingLineCommand = new AsyncCommand(AddReceivingLineAsync);
        RemoveReceivingLineCommand = new AsyncRelayCommand<ReceivingLineItem?>(RemoveReceivingLineAsync);
        RemoveSendingLineCommand = new AsyncRelayCommand<SendingLineItem?>(RemoveSendingLineAsync);
        ReadWeighbridgeCommand = new AsyncCommand(ReadWeighbridgeAsync);
        ReadWeighbridgeSecondCommand = new AsyncCommand(ReadWeighbridgeSecondAsync);
        ReadPlatformCommand = new AsyncCommand(ReadPlatformAsync);
        ResetWeighbridgeWeightsCommand = new RelayCommand(ResetWeighbridgeWeights);
        ResetPlatformWeightCommand = new RelayCommand(ResetPlatformWeight);
        LoadCustomerDocumentsCommand = new AsyncCommand(LoadCustomerDocumentsAsync);
        UploadCustomerDocumentCommand = new AsyncCommand(UploadCustomerDocumentAsync);

        ShowCompanyAndSitesCommand = ReactiveUI.ReactiveCommand.Create(() =>
        {
            CurrentSection = EnumMainSection.CompanyAndSites;
            
            // Trigger company data loading
            _ = CompanyLetterFilters; // Lazy load trigger
        });
        ShowProductsAndPricesCommand = ReactiveUI.ReactiveCommand.Create(() => CurrentSection = EnumMainSection.ProductsAndPrices);
        // Section navigation (used by menu)
        ShowDashboardCommand = ReactiveUI.ReactiveCommand.Create(() => CurrentSection = EnumMainSection.Dashboard);
        ShowCustomersCommand = ReactiveUI.ReactiveCommand.CreateFromTask(async () =>
        {
            CurrentSection = EnumMainSection.Customers;

            // Trigger company data loading for dropdowns
            _ = CompanyLetterFilters; // Lazy load trigger

            await ClearNewCustomerFormAsync();
        });
        
        ShowBuyersCommand = ReactiveUI.ReactiveCommand.CreateFromTask(async () =>
        {
            CurrentSection = EnumMainSection.Buyers;

            // Trigger company data loading for dropdowns
            _ = CompanyLetterFilters; // Lazy load trigger

            await ClearNewBuyerFormAsync();
        });
        ShowTicketsCommand = ReactiveUI.ReactiveCommand.Create(() => CurrentSection = EnumMainSection.TicketsReceiving);
        ShowTicketsReceivingCommand = ReactiveUI.ReactiveCommand.Create(() => CurrentSection = EnumMainSection.TicketsReceiving);
        ShowTicketsSendingCommand = ReactiveUI.ReactiveCommand.CreateFromTask(async () =>
        {
            CurrentSection = EnumMainSection.TicketsSending;
            // Avoid state bleed from Receiving: sending uses its own Buyer field.
            TicketBuyerIdText = string.Empty;
            await OnEnterTicketsSendingAsync();
        });
        ShowDocumentsCommand = ReactiveUI.ReactiveCommand.Create(() => CurrentSection = EnumMainSection.Documents);
        ShowCameraCommand = ReactiveUI.ReactiveCommand.Create(() => CurrentSection = EnumMainSection.Camera);

        // ✅ ADDED: Reports + Settings behave like other nav items
        ShowReportsCommand = ReactiveUI.ReactiveCommand.Create(() => CurrentSection = EnumMainSection.Reports);
        ShowStockLevelsCommand = ReactiveUI.ReactiveCommand.Create(() => CurrentSection = EnumMainSection.StockLevels);
        ShowStockMovementCommand = ReactiveUI.ReactiveCommand.Create(() => CurrentSection = EnumMainSection.StockMovement);
        ShowSettingsCommand = ReactiveUI.ReactiveCommand.Create(() => CurrentSection = EnumMainSection.Settings);

        EditCustomerCommand = new RelayCommand<CustomerDto>(OnEditCustomer);
        EditBuyerCommand = new RelayCommand<BuyerDto>(OnEditBuyer);
        DeleteCustomerCommand = new AsyncRelayCommand<CustomerDto>(execute: OnDeleteCustomerAsync);
        LogTicketCommand = new RelayCommand<CustomerDto>(OnLogTicket);

        DeleteBuyerCommand = new AsyncRelayCommand<BuyerDto>(execute: OnDeleteBuyerAsync);
        LogBuyerTicketCommand = new RelayCommand<BuyerDto>(OnLogBuyerTicket);
        ClearNewCustomerCommand = new AsyncRelayCommand(ClearNewCustomerFormAsync);
        ClearNewBuyerCommand = new AsyncRelayCommand(ClearNewBuyerFormAsync);
        ClearCustomerSearchCommand = new RelayCommand(ClearCustomerSearch);
        ClearBuyerSearchCommand = new RelayCommand(ClearBuyerSearch);

        Console.WriteLine($"Next account number = {NewAccountNumber}");
        OnPropertyChanged(nameof(NewAccountNumberDisplay));

        UpdateCustomerCommand = new AsyncRelayCommand(OnUpdateCustomerAsync, () => CanUpdateCustomer);
        UpdateBuyerCommand = new AsyncRelayCommand(OnUpdateBuyerAsync, () => CanUpdateBuyer);
        SearchCustomersCommand = new AsyncRelayCommand(SearchCustomerAsync);
        SearchBuyersCommand = new AsyncRelayCommand(SearchBuyerAsync);
        SearchBuyerCommand = SearchBuyersCommand;
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

        // Ticket Report Command
        DownloadTicketReportCommand = new AsyncCommand(DownloadTicketReportAsync);

        // Ticket search commands
        SearchTicketsCommand = new AsyncCommand(SearchTicketsAsync);
        ClearTicketSearchCommand = new RelayCommand(ClearTicketSearch);
        DeleteTicketCommand = new AsyncRelayCommand<TicketSearchResultDto?>(DeleteTicketAsync);
        EditTicketCommand = new RelayCommand<TicketSearchResultDto>(OnEditTicket);
        CancelEditTicketCommand = new RelayCommand(OnCancelEditTicket);
        
        // Receiving ticket search commands
        SearchReceivingTicketsCommand = new AsyncCommand(SearchReceivingTicketsAsync);
        ClearReceivingTicketSearchCommand = new RelayCommand(ClearReceivingTicketSearch);
        DeleteReceivingTicketCommand = new AsyncRelayCommand<TicketSearchResultDto?>(DeleteReceivingTicketAsync);
        PrintReceivingTicketCommand = new AsyncCommand(PrintReceivingTicketAsync);
        ShowLineNotesCommand = new RelayCommand<string>(ShowLineNotes);
        CloseLineNotesCommand = new RelayCommand(CloseLineNotes);
        
        // Sending ticket search commands
        SearchSendingTicketsCommand = new AsyncCommand(SearchSendingTicketsAsync);
        ClearSendingTicketSearchCommand = new RelayCommand(ClearSendingTicketSearch);
        DeleteSendingTicketCommand = new AsyncRelayCommand<TicketSearchResultDto?>(DeleteSendingTicketAsync);
        PrintSendingTicketCommand = new AsyncCommand(PrintSendingTicketAsync);
        
        // Ticket line commands
        EditTicketLineCommand = new RelayCommand<TicketLineDto>(OnEditTicketLine);
        DeleteTicketLineCommand = new AsyncRelayCommand<TicketLineDto?>(DeleteTicketLineAsync);

        // Signature
        CaptureSignatureCommand = new AsyncCommand(CaptureSignatureAsync);

        // Ticket create/edit commands (route based on current section)
        AddLineCommand = new AsyncCommand(AddLineRoutedAsync);
        RemoveLineCommand = new AsyncRelayCommand<ReceivingLineItem?>(RemoveReceivingLineAsync);
        SaveTicketCommand = new AsyncCommand(SaveTicketAsync);
        ClearTicketCommand = new AsyncCommand(ClearTicketRoutedAsync);
        CaptureWeightCommand = new AsyncCommand(CaptureWeightAsync);
        CapturePlatePhotoCommand = new AsyncCommand(CapturePlatePhotoAsync);
        CaptureLoadPhotoCommand = new AsyncCommand(CaptureLoadPhotoAsync);
        
        // Ticket search commands (additional)
        ClearSearchCommand = new RelayCommand(ClearTicketSearch);
        PrintTicketCommand = new AsyncCommand(PrintTicketAsync);
        ScrollToAddLinesCommand = new RelayCommand(ScrollToAddLines);
        CreateTicketHeaderCommand = new AsyncCommand(CreateTicketHeaderRoutedAsync);
        SaveEditedTicketLineCommand = new AsyncCommand(SaveEditedTicketLineAsync);
        CancelEditTicketLineCommand = new RelayCommand(CancelEditTicketLine);

        // Customer image capture commands
        CaptureIdCardCommand = new AsyncCommand(CaptureIdCardAsync);
        CaptureDriverLicenseCommand = new AsyncCommand(CaptureDriverLicenseAsync);
        CapturePhotoCommand = new AsyncCommand(CapturePhotoAsync);
        CaptureFingerprintCommand = new AsyncCommand(CaptureFingerprintAsync);

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

        InitializeCountries();
        InitializeCompanyAndSiteCommands();
        InitializeProductsAndPricesCommands();
        _ = LoadProvincesAsync();
    }

    // --- Core helpers / section switching ---

    public async Task InitializeLookupsAsync()
    {
        InitializeCountries();
        await LoadProvincesAsync();
        await ClearNewCustomerFormAsync();
    }

    private Task SwitchSectionAsync(EnumMainSection section)
    {
        CurrentSection = section;
        StatusMessage = $"Section switched to: {section}.";
        return Task.CompletedTask;
    }

    private async Task CreateTicketAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Creating ticket...";

        try
        {
            // --- Basic validation ---
            if (!long.TryParse(TicketCustomerIdText, out var customerId) || customerId <= 0)
            {
                StatusMessage = "Customer ID must be a valid positive number.";
                return;
            }

            if (string.IsNullOrWhiteSpace(TicketNumber))
            {
                StatusMessage = "Ticket Number is required.";
                return;
            }

            if (!decimal.TryParse(NormalizeDecimalText(TicketUnitPriceText),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var unitPrice) || unitPrice < 0)
            {
                StatusMessage = "Unit price must be a valid non-negative number.";
                return;
            }

            decimal? ParseWeight(string text)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return null;

                if (decimal.TryParse(NormalizeDecimalText(text),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var value))
                {
                    return value;
                }

                throw new FormatException($"Invalid weight value: '{text}'.");
            }

            var firstWeight = ParseWeight(TicketFirstWeightText);
            var secondWeight = ParseWeight(TicketSecondWeightText);

            // --- Call API ---
            var dto = await _ticketService.CreateTicketAsync(
                customerId: customerId,
                ticketType: string.IsNullOrWhiteSpace(TicketType) ? "weighbridge" : TicketType.Trim(),
                ticketNumber: TicketNumber.Trim(),
                firstWeightKg: firstWeight,
                secondWeightKg: secondWeight,
                unitPricePerKg: unitPrice,
                currencyCode: string.IsNullOrWhiteSpace(TicketCurrencyCode) ? "ZAR" : TicketCurrencyCode.Trim(),
                productDescription: string.IsNullOrWhiteSpace(TicketProductDescription) ? null : TicketProductDescription.Trim(),
                notes: string.IsNullOrWhiteSpace(TicketNotes) ? null : TicketNotes.Trim(),
                vehicleRegistration: string.IsNullOrWhiteSpace(TicketVehicleRegistration) ? null : TicketVehicleRegistration.Trim(),
                ofmWeighbridgeTicket: string.IsNullOrWhiteSpace(TicketOfmWeighbridgeTicket) ? null : TicketOfmWeighbridgeTicket.Trim(),
                foreignTicket: string.IsNullOrWhiteSpace(TicketForeignTicket) ? null : TicketForeignTicket.Trim(),
                ckNumber: string.IsNullOrWhiteSpace(TicketCkNumber) ? null : TicketCkNumber.Trim()
            );

            if (dto == null)
            {
                StatusMessage = "Ticket create failed - API returned no result.";
                return;
            }

            LastCreatedTicket = dto;
            StatusMessage =
                $"Ticket {dto.TicketNumber} created. Net {dto.NetWeightKg} kg, Total {dto.TotalAmount:0.00} {dto.CurrencyCode}.";

            // Prepare for next ticket
            TicketNumber = GenerateNextTicketNumber();
            TicketFirstWeightText = string.Empty;
            TicketSecondWeightText = string.Empty;
            TicketUnitPriceText = string.Empty;
            TicketProductDescription = string.Empty;
            TicketNotes = string.Empty;
            TicketVehicleRegistration = string.Empty;
            TicketOfmWeighbridgeTicket = string.Empty;
            TicketForeignTicket = string.Empty;
            TicketCkNumber = string.Empty;
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            StatusMessage = $"Error calling API: {ex.Message}";
        }
        catch (FormatException ex)
        {
            StatusMessage = ex.Message;
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Error creating ticket: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string NormalizeDecimalText(string text)
    {
        // Accept both comma and dot as decimal separators and normalize to invariant culture
        return text.Replace(',', '.').Trim();
    }

    private string GenerateNextTicketNumber()
    {
        // Simple client-side ticket number pattern:
        // WB-<SiteId>-YYYYMMDD-HHMMSS
        var siteId = _authState.SiteId > 0 ? _authState.SiteId : 1;
        return $"WB-{siteId}-{DateTime.Now:yyyyMMdd-HHmmss}";
    }

    private void ScrollToAddLines()
    {
        // TODO: Implement actual scrolling - requires view interaction
        StatusMessage = "Please scroll down to the 'Add Product Lines' section to add line items";
    }

    private async Task CreateTicketHeaderAsync()
    {
        // If ticket status is 'H', this button is "Save & Reset" - clear and reset
        if (CurrentTicketState == 'H')
        {
            await SaveAndResetTicketAsync();
            return;
        }

        // Otherwise, create a new ticket header
        // Validate that customer ID is set
        if (string.IsNullOrWhiteSpace(TicketCustomerIdText))
        {
            StatusMessage = "Please select a customer before creating the ticket header.";
            return;
        }

        // For Weighbridge tickets, First Weight is required
        if (SelectedTicketTypeOption?.Key == "weighbridge")
        {
            if (string.IsNullOrWhiteSpace(TicketFirstWeightText) || TicketFirstWeightText == "0")
            {
                StatusMessage = "For Weighbridge tickets, First Weight must be set before creating the header.";
                return;
            }
        }

        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Creating ticket header...";

        try
        {
            // Create a NEW ticket with state 'H' (Header only)
            // This is an INSERT operation, not an UPDATE
            int ticketTypeId = SelectedTicketTypeOption?.Key == "weighbridge" ? 1 : 2;
            
            // Generate ticket number
            string prefix = ticketTypeId == 1 ? "RWB" : "RPL";
            string ticketNumber = await _ticketReceivingService.GenerateTicketNumberAsync(prefix);
            
            // Parse initialize weight for weighbridge
            decimal? initializeWeight = null;
            if (SelectedTicketTypeOption?.Key == "weighbridge")
            {
                var normalizedFw = NormalizeDecimalText(TicketFirstWeightText);
                if (decimal.TryParse(normalizedFw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var firstWeight))
                {
                    initializeWeight = firstWeight;
                    Console.WriteLine($"[DEBUG] CreateTicketHeaderAsync: Weighbridge ticket, initializeWeight={initializeWeight}, TicketFirstWeightText={TicketFirstWeightText}");
                }
                else
                {
                    Console.WriteLine($"[DEBUG] CreateTicketHeaderAsync: Failed to parse TicketFirstWeightText='{TicketFirstWeightText}' (normalized='{normalizedFw}')");
                }
            }
            else
            {
                Console.WriteLine($"[DEBUG] CreateTicketHeaderAsync: Platform ticket, initializeWeight=null");
            }

            // Extract customer ID
            long customerId = ExtractCustomerIdFromText(TicketCustomerIdText);
            if (customerId <= 0)
            {
                StatusMessage = "Invalid customer ID.";
                return;
            }

            // Create a NEW receiving ticket with state 'H' in a single INSERT
            bool isWeighbridgeTicket = ticketTypeId == 1;
            var createTicketDto = new CreateTicketReceivingDto
            {
                TicketTypeId = ticketTypeId,
                TicketNumber = ticketNumber,
                CustomerId = (int)customerId,
                CreatedByOperatorId = 1,  // TODO: Get from authenticated user context
                TicketState = 'H',  // Create directly with state 'H' (Header only)
                InitializeWeightKg = initializeWeight,
                NetWeightKg = 0m,  // No weight yet, will be calculated from lines
                VehicleRegistration = isWeighbridgeTicket ? TicketVehicleRegistration : null,
                TrailerRegistration = isWeighbridgeTicket ? TicketTrailerRegistration : null,
                DriverName = isWeighbridgeTicket ? TicketDriverName : null,
                OfmWeighbridgeTicket = isWeighbridgeTicket ? TicketOfmWeighbridgeTicket : null,
                CkNumber = isWeighbridgeTicket ? TicketCkNumber : null,
                DeliveryNumber = isWeighbridgeTicket ? TicketDeliveryNumber : null,
                ForeignTicket = isWeighbridgeTicket ? TicketForeignTicket : null,
                Notes = TicketNotes,
                InvoiceNumber = 0  // Will be set later if needed
            };
            
            Console.WriteLine($"[DEBUG] CreateTicketHeaderAsync: About to call API with TicketState='{createTicketDto.TicketState}', InitializeWeightKg={createTicketDto.InitializeWeightKg}");

            // Call API to create the ticket with state 'H' in a single INSERT
            var response = await _ticketReceivingService.CreateTicketAsync(createTicketDto);
            
            if (response == null || response.TicketReceivingId <= 0)
            {
                StatusMessage = "Failed to create ticket header.";
                return;
            }

            // Store the created ticket
            LastCreatedTicket = new TicketDto
            {
                TicketId = response.TicketReceivingId,
                CustomerId = response.CustomerId,
                TicketNumber = response.TicketNumber,
                TicketType = response.TicketTypeName,
                TicketTypeId = response.TicketTypeId,
                NetWeightKg = response.NetWeightKg,
                TicketState = response.TicketState,
                InitializeWeightKg = response.InitializeWeightKg
            };

            CurrentTicketState = 'H';
            
            // Untick "New Customer?" checkbox
            SearchReceivingNewCustomersCheckbox = false;
            Console.WriteLine($"[DEBUG] CreateTicketHeaderAsync: Unticked SearchReceivingNewCustomersCheckbox");
            
            // Load the newly created ticket details
            await LoadSelectedReceivingTicketDetailsAsync(response.TicketReceivingId);
            Console.WriteLine($"[DEBUG] CreateTicketHeaderAsync: Loaded ticket details for new ticket {response.TicketReceivingId}");
            
            StatusMessage = "Ticket header created successfully! You can now add line items.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating ticket header: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveAndResetTicketAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        
        try
        {
            // If we have a ticket to save, update it in the database
            if (LastCreatedTicket?.TicketId > 0)
            {
                StatusMessage = "Saving ticket changes...";
                
                // Build the update DTO from current form fields
                var updateDto = new CreateTicketReceivingDto
                {
                    CustomerId = (int)LastCreatedTicket.CustomerId,
                    TicketTypeId = (int)LastCreatedTicket.TicketTypeId,
                    TicketNumber = TicketNumber ?? string.Empty,
                    TicketState = LastCreatedTicket.TicketState,
                    InitializeWeightKg = SelectedTicketTypeOption?.Key == "weighbridge" && decimal.TryParse(NormalizeDecimalText(TicketFirstWeightText ?? ""), out var initW)
                        ? initW
                        : LastCreatedTicket.InitializeWeightKg,
                    VehicleRegistration = TicketVehicleRegistration,
                    TrailerRegistration = TicketTrailerRegistration,
                    DriverName = TicketDriverName,
                    OfmWeighbridgeTicket = TicketOfmWeighbridgeTicket,
                    ForeignTicket = TicketForeignTicket,
                    CkNumber = TicketCkNumber,
                    DeliveryNumber = TicketDeliveryNumber,
                    Notes = TicketNotes,
                    NetWeightKg = LastCreatedTicket.NetWeightKg
                };
                
                // Update the ticket in the database
                var result = await _ticketReceivingService.UpdateTicketReceivingAsync(LastCreatedTicket.TicketId, updateDto);
                
                if (result != null)
                {
                    StatusMessage = "✓ Ticket saved successfully.";
                }
                else
                {
                    StatusMessage = "Warning: Could not save ticket to database, but clearing form anyway.";
                }
            }
            
            // Clear Details and Create sections but keep Search and Results
            await ClearTicketAsync();
            
            // Reset button state to 'C' so next ticket creation shows "Create Header"
            CurrentTicketState = 'C';
            
            StatusMessage = "✓ Ticket cleared. Ready for new ticket.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving ticket: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<bool> ConfirmAsync(string message)
    {
        var owner = (Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner == null) return false;

        var dlg = new MetalLink.Desktop.Views.ConfirmDialog(message);
        return await dlg.ShowDialog<bool>(owner);
    }

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

        if (Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime desktop)
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

    // --- Customer search / create ---

    private async Task SearchCustomerAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Searching customers...";

        try
        {
            long? customerId = null;
            if (long.TryParse(SearchCustomerIdText, out var cid))
                customerId = cid;

            long? siteId = null;
            if (long.TryParse(SearchSiteIdText, out var sid))
                siteId = sid;

            // 🔹 Province / Country filters: null if "ALL"
            long? provinceId = null;
            if (SearchProvince != null &&
                !string.Equals(SearchProvince.ProvinceName, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                provinceId = SearchProvince.ProvinceId;
            }

            long? countryId = null;
            if (SearchCountry != null &&
                !string.Equals(SearchCountry.CountryName, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                countryId = SearchCountry.CountryId;
            }

            // Build a request object with all filters (null / empty = ignore)
            var request = new CustomerSearchRequestDto
            {
                CustomerId = customerId,
                SiteId = siteId ?? (SelectedSearchSite?.SiteId),
                FirstName = string.IsNullOrWhiteSpace(SearchFirstNameText) ? null : SearchFirstNameText.Trim(),
                LastName = string.IsNullOrWhiteSpace(SearchLastNameText) ? null : SearchLastNameText.Trim(),
                CompanyName = SelectedSearchCompany?.CompanyName ?? (string.IsNullOrWhiteSpace(SearchCompanyNameText) ? null : SearchCompanyNameText.Trim()),
                IdNumber = string.IsNullOrWhiteSpace(SearchIdNumberText) ? null : SearchIdNumberText.Trim(),
                AccountNumber = ParseAccountNumberOrNull(SearchAccountNumberText),
                PriceCode = string.IsNullOrEmpty(SearchPriceCode?.Code) ? null : SearchPriceCode.Code.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(SearchPhoneNumberText) ? null : SearchPhoneNumberText,
                MobileNumber = string.IsNullOrWhiteSpace(SearchMobileNumberText) ? null : SearchMobileNumberText,
                Email = string.IsNullOrWhiteSpace(SearchEmailText) ? null : SearchEmailText,

                ProvinceId = provinceId,
                CountryId = countryId
                // Taxable filter removed - returns all records regardless of is_taxable status
            };

            var results = await _customerService.SearchCustomersAsync(request);

            CustomerSearchResults.Clear();
            if (results != null)
            {
                foreach (var c in results)
                    CustomerSearchResults.Add(c);
            }

            // Update pagination with total records
            PaginationViewModel.SetTotalRecords(CustomerSearchResults.Count);
            PaginationViewModel.PageChanged -= OnPaginationPageChanged;
            PaginationViewModel.PageChanged += OnPaginationPageChanged;
            UpdatePagedResults();
            
            // Also populate paged results for initial display
            PagedCustomerSearchResults.Clear();
            foreach (var customer in CustomerSearchResults)
            {
                PagedCustomerSearchResults.Add(customer);
            }

            if (CustomerSearchResults.Count == 0)
            {
                StatusMessage = "No customers found.";
                FoundCustomer = null;
            }
            else
            {
                StatusMessage = $"Found {CustomerSearchResults.Count} customer(s).";
                FoundCustomer = PagedCustomerSearchResults.Count > 0 ? PagedCustomerSearchResults[0] : CustomerSearchResults[0];
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
            var errors = new List<string>();

            // 1) REQUIRED FIELDS
            if (string.IsNullOrWhiteSpace(NewFirstName))
                errors.Add("First Name is required.");

            if (string.IsNullOrWhiteSpace(NewLastName))
                errors.Add("Last Name is required.");

            if (string.IsNullOrWhiteSpace(NewIdNumber))
                errors.Add("ID Number is required.");

            if (string.IsNullOrWhiteSpace(NewEmail))
                errors.Add("Email is required.");

            if (NewProvince == null)
                errors.Add("Province is required.");

            if (NewCountry == null)
                errors.Add("Country is required.");

            // 2) COMPANY + SITE RULE
            if (NewIsCompany)
            {
                if (SelectedNewCompany == null || SelectedNewSite == null)
                {
                    StatusMessage = "Company and Site are required when Is Company is checked.";
                    return;
                }

                // Guard: stop “Elementech + Orange Farms” mismatch
                if (SelectedNewSite.CompanyId != SelectedNewCompany.CompanyId)
                {
                    StatusMessage = "Selected Site does not belong to the selected Company. Please pick a matching Site.";
                    return;
                }
            }

            if (errors.Count > 0)
            {
                StatusMessage = string.Join(Environment.NewLine, errors);
                return;
            }

            // 3) UNIQUENESS CHECK (ID NUMBER, ACCOUNT NUMBER, EMAIL)
            var uniqueCheckRequest = new CustomerSearchRequestDto
            {
                // only send the fields we care about for uniqueness
                IdNumber = NewIdNumber,
                Email = NewEmail
            };

            var duplicates = await _customerService.SearchCustomersAsync(uniqueCheckRequest);

            if (duplicates != null && duplicates.Any())
            {
                if (!string.IsNullOrWhiteSpace(NewIdNumber) &&
                    duplicates.Any(c => string.Equals(c.IdNumber, NewIdNumber, StringComparison.OrdinalIgnoreCase)))
                {
                    errors.Add("A customer with this ID Number already exists.");
                }

                if (!string.IsNullOrWhiteSpace(NewEmail) &&
                    duplicates.Any(c => string.Equals(c.Email, NewEmail, StringComparison.OrdinalIgnoreCase)))
                {
                    errors.Add("A customer with this Email already exists.");
                }

                if (errors.Count > 0)
                {
                    StatusMessage = string.Join(Environment.NewLine, errors);
                    return;
                }
            }

            if (errors.Count > 0)
            {
                StatusMessage = string.Join(" ", errors);
                return;
            }

            // ----- build DTO for the API (no AccountNumber) -----
            // Allocate account number at time of create (not on Clear)
            if (NewAccountNumber == null)
                NewAccountNumber = await _customerService.GetNextAccountNumberAsync();

            var dto = new CustomerDto
            {
                AccountNumber = NewAccountNumber,
                FirstName = NewFirstName!,
                LastName = NewLastName!,
                IdNumber = NewIdNumber!,
                Email = NewEmail!,
                PhoneNumber = string.IsNullOrWhiteSpace(NewPhoneNumber) ? null : NewPhoneNumber,
                MobileNumber = string.IsNullOrWhiteSpace(NewMobileNumber) ? null : NewMobileNumber,
                PriceCode = string.IsNullOrEmpty(SelectedPriceCodeChar?.Code) ? null : SelectedPriceCodeChar.Code.Trim(),
                IsCompany = NewIsCompany,
                Taxable = NewTaxable,

                CompanyId = NewIsCompany && SelectedNewCompany != null
                                    ? (int?)SelectedNewCompany.CompanyId
                                    : null,
                SiteId = NewIsCompany && SelectedNewSite != null
                                    ? (int?)SelectedNewSite.SiteId
                                    : null
            };

            Console.WriteLine($"CreateCustomer: IsCompany={NewIsCompany}, CompanyId={SelectedNewCompany?.CompanyId}, SiteId={SelectedNewSite?.SiteId}");

            // API will allocate AccountNumber from the DB identity
            var created = await _customerService.CreateCustomerAsync(dto);
            
            if (created == null)
            {
                StatusMessage = "Customer create failed - API returned no result.";
                return;
            }

            // Upload images if captured
            await UploadCustomerImagesAsync(created.CustomerId);

            // store raw number and refresh displayed padded text
            _newAccountNumber = created.AccountNumber;
            OnPropertyChanged(nameof(NewAccountNumber));

            StatusMessage = $"Customer {created.FirstName} {created.LastName} created successfully (Account {NewAccountNumber}).";
    
            var refreshed = await _customerService.GetCustomerByIdAsync(created.CustomerId);
            refreshed ??= dto;
        
            var existing = CustomerSearchResults.FirstOrDefault(c => c.CustomerId == dto.CustomerId);
            if (existing != null)
            {
                var index = CustomerSearchResults.IndexOf(existing);
                if (index >= 0)
                    CustomerSearchResults[index] = refreshed; // replace item (forces UI refresh)
            }
            else
            {
                CustomerSearchResults.Add(refreshed);
            }
            
            // if you want the form cleared except the new account number, you can adjust here;
            // right now you probably still call ClearNewCustomerForm();
            await ClearNewCustomerFormAsync();
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

    public bool CanCreateCustomer =>
        !string.IsNullOrWhiteSpace(NewFirstName)
        && !string.IsNullOrWhiteSpace(NewLastName)
        && (!NewIsCompany || (SelectedNewCompany != null && SelectedNewSite != null));

    public bool CanUpdateCustomer =>
        IsEditMode
        && EditingCustomerId.HasValue
        && !string.IsNullOrWhiteSpace(NewFirstName)
        && !string.IsNullOrWhiteSpace(NewLastName);

    public bool CanCreateBuyer =>
        !string.IsNullOrWhiteSpace(NewFirstName)
        && !string.IsNullOrWhiteSpace(NewLastName)
        && SelectedNewCompany != null
        && SelectedNewSite != null;

    public bool CanUpdateBuyer =>
        IsEditMode
        && EditingBuyerId.HasValue
        && !string.IsNullOrWhiteSpace(NewFirstName)
        && !string.IsNullOrWhiteSpace(NewLastName)
        && SelectedNewCompany != null
        && SelectedNewSite != null;

    // --- Scale reading ---

    private async Task ReadWeighbridgeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Reading weighbridge first weight...";

        try
        {
            var reading = await _scaleService.ReadOnceAsync(ScaleDeviceType.Weighbridge);
            if (reading == null)
            {
                StatusMessage = "No reading from weighbridge.";
                return;
            }

            TicketFirstWeightText = reading.WeightKg.ToString("0.0");
            StatusMessage = $"Weighbridge first weight: {reading.WeightKg:0.0} kg.";
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

    private async Task ReadWeighbridgeSecondAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Reading weighbridge second weight...";

        try
        {
            var reading = await _scaleService.ReadOnceAsync(ScaleDeviceType.Weighbridge);
            if (reading == null)
            {
                StatusMessage = "No reading from weighbridge.";
                return;
            }

            // Parse first weight to calculate net weight
            if (decimal.TryParse(NormalizeDecimalText(TicketFirstWeightText),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var firstWeight))
            {
                // When using MockScaleService, make second weight deterministic relative to first weight
                // (matches requested behaviour)
                var isMock = _scaleService is MetalLink.Desktop.Hardware.MockScaleService;
                if (isMock)
                {
                    var rnd = new Random();
                    var delta = rnd.Next(500, 4001); // inclusive-ish upper bound

                    var mockSecond = CurrentSection == EnumMainSection.TicketsSending
                        ? firstWeight + delta
                        : firstWeight - delta;

                    reading = new ScaleReading(ScaleDeviceType.Weighbridge, decimal.Round(mockSecond, 1));
                }

                // Receiving vs Sending behaviour differs
                if (CurrentSection == EnumMainSection.TicketsSending)
                {
                    // VALIDATION (Sending): Second weight must be GREATER than First Weight
                    if (reading.WeightKg <= firstWeight)
                    {
                        StatusMessage = $"Error: Second Weight ({reading.WeightKg:0.0} kg) must be GREATER than First Weight ({firstWeight:0.0} kg). No load was added!";
                        return;
                    }

                    TicketSecondWeightText = reading.WeightKg.ToString("0.0");

                    // Sending net weight: Second (gross) - First (tare) = Net
                    var netWeight = reading.WeightKg - firstWeight;

                    // Populate Add Product Line weight field so Add Line works immediately
                    SendingWeightText = netWeight.ToString("0.00");

                    StatusMessage = $"Weighbridge second weight: {reading.WeightKg:0.0} kg. Net weight: {netWeight:0.00} kg.";
                }
                else
                {
                    // VALIDATION (Receiving): Second weight must be LESS than First Weight
                    if (reading.WeightKg >= firstWeight)
                    {
                        StatusMessage = $"Error: Second Weight ({reading.WeightKg:0.0} kg) must be LESS than First Weight ({firstWeight:0.0} kg). No scrap was offloaded!";
                        return;
                    }

                    TicketSecondWeightText = reading.WeightKg.ToString("0.0");

                    // Receiving net weight: First (gross) - Second (tare) = Net
                    var netWeight = firstWeight - reading.WeightKg;
                    ReceivingWeightText = netWeight.ToString("0.00");

                    StatusMessage = $"Weighbridge second weight: {reading.WeightKg:0.0} kg. Net weight: {netWeight:0.00} kg.";
                }
            }
            else
            {
                StatusMessage = "Please read First Weight before reading Second Weight.";
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

            // For platform, generate random weight between 10 and 250
            var random = new Random();
            var randomWeight = random.Next(10, 251);
            TicketPlatformWeightText = randomWeight.ToString("0.0");
            
            // Populate the receiving weight textbox with the platform weight
            ReceivingWeightText = randomWeight.ToString("0.00");
            
            StatusMessage = $"Platform weight: {randomWeight:0.0} kg.";
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

    private void ResetWeighbridgeWeights()
    {
        TicketFirstWeightText = "0";
        TicketSecondWeightText = "0";
        ReceivingWeightText = string.Empty;
        StatusMessage = "Weighbridge weights reset to 0.";
    }

    private void ResetPlatformWeight()
    {
        TicketPlatformWeightText = "0";
        ReceivingWeightText = string.Empty;
        StatusMessage = "Platform weight reset to 0.";
    }

    // --- Documents ---

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

    // --- Camera / capture ---

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

    // --- Ticket report ---

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

    // --- Signature ---

    private async Task CaptureSignatureAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            // In Customers/Buyers create/edit section, just capture into the form and upload immediately
            if (CurrentSection == EnumMainSection.Customers || CurrentSection == EnumMainSection.Buyers)
            {
                StatusMessage = "Capturing signature...";

                var capture = await _signaturePadService.CaptureAsync("signature");

                if (capture != null && capture.ImageData != null)
                {
                    SignatureImage = LoadBitmapFromBytes(capture.ImageData);

                    if (CurrentSection == EnumMainSection.Buyers)
                    {
                        if (FoundBuyer == null)
                        {
                            StatusMessage = "Select a buyer before capturing signature.";
                            return;
                        }

                        await _buyerService.UploadBuyerImageAsync(FoundBuyer.BuyerId, "signature", capture.ImageData, "image/png");
                        SelectedSignatureImage = SignatureImage;
                        StatusMessage = "✓ Buyer signature captured and uploaded";
                    }
                    else
                    {
                        StatusMessage = "✓ Signature captured successfully";
                    }
                }
                else
                {
                    StatusMessage = "Failed to capture signature";
                }
                return;
            }

            // Otherwise, we're in the Documents section - need a customer ID
            StatusMessage = "Capturing signature and uploading...";

            // Use the same Customer ID as the Customer Documents section
            if (!long.TryParse(DocumentsCustomerIdText, out var customerId))
            {
                StatusMessage =
                    "Please enter a valid numeric Customer ID in the Customer Documents section before capturing signature.";
                return;
            }

            const string documentType = "signature";

            // Simulate pad capture (currently using MockSignaturePadService)
            var capture2 = await _signaturePadService.CaptureAsync(documentType);
            LastSignatureCaptureSummary = capture2.ToString();

            // Upload as a normal customer document
            var doc = await _documentService.UploadDocumentAsync(
                customerId,
                capture2.DocumentType,
                capture2.FilePath);

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

    // --- Dashboard stats / animation ---

    private async Task LoadDashboardStatsAsync()
    {
        var health = await _apiClient.GetAsync<HealthResponse>("api/health/db");

        if (health != null)
        {
            TotalCustomersInDb = health.customersCount;
            TotalTicketsInDb = health.ticketsCount;
            TotalCompaniesInDb = health.companiesCount;
            TotalSitesInDb = health.sitesCount;
            TotalProductsInDb = health.productsCount;

            _ = AnimateCounterAsync(TotalCustomersInDb, v => AnimatedTotalCustomersInDb = v);
            _ = AnimateCounterAsync(TotalTicketsInDb, v => AnimatedTotalTicketsInDb = v);
            _ = AnimateCounterAsync(TotalCompaniesInDb, v => AnimatedTotalCompaniesInDb = v);
            _ = AnimateCounterAsync(TotalSitesInDb, v => AnimatedTotalSitesInDb = v);
            _ = AnimateCounterAsync(TotalProductsInDb, v => AnimatedTotalProductsInDb = v);
        }
    }

    private async Task AnimateCounterAsync(
        int target,
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

    // --- OnPropertyChanged override ---

    protected new void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // --- Image upload helpers ---

    private async Task UploadCustomerImagesAsync(long customerId)
    {
        Console.WriteLine($"[DEBUG] UploadCustomerImagesAsync called for customer {customerId}");
        Console.WriteLine($"[DEBUG] IdCardImage: {(IdCardImage != null ? "present" : "null")}");
        Console.WriteLine($"[DEBUG] DriverLicenseImage: {(DriverLicenseImage != null ? "present" : "null")}");
        Console.WriteLine($"[DEBUG] PhotoImage: {(PhotoImage != null ? "present" : "null")}");
        Console.WriteLine($"[DEBUG] SignatureImage: {(SignatureImage != null ? "present" : "null")}");
        Console.WriteLine($"[DEBUG] FingerprintImage: {(FingerprintImage != null ? "present" : "null")}");
        
        try
        {
            // Upload ID card if captured
            if (IdCardImage != null)
            {
                Console.WriteLine($"[DEBUG] Converting IdCardImage to bytes...");
                var imageData = BitmapToBytes(IdCardImage);
                Console.WriteLine($"[DEBUG] IdCard imageData size: {imageData?.Length ?? 0} bytes");
                if (imageData != null)
                {
                    Console.WriteLine($"[DEBUG] Uploading ID card...");
                    await _customerService.UploadCustomerImageAsync(
                        customerId, "idcard", imageData, "image/png");
                    Console.WriteLine($"[DEBUG] ID card uploaded successfully");
                }
            }

            // Upload driver license if captured
            if (DriverLicenseImage != null)
            {
                Console.WriteLine($"[DEBUG] Uploading driver license...");
                var imageData = BitmapToBytes(DriverLicenseImage);
                if (imageData != null)
                {
                    await _customerService.UploadCustomerImageAsync(
                        customerId, "driverlicense", imageData, "image/png");
                    Console.WriteLine($"[DEBUG] Driver license uploaded successfully");
                }
            }

            // Upload photo if captured
            if (PhotoImage != null)
            {
                Console.WriteLine($"[DEBUG] Uploading photo...");
                var imageData = BitmapToBytes(PhotoImage);
                if (imageData != null)
                {
                    await _customerService.UploadCustomerImageAsync(
                        customerId, "photo", imageData, "image/png");
                    Console.WriteLine($"[DEBUG] Photo uploaded successfully");
                }
            }

            // Upload signature if captured
            if (SignatureImage != null)
            {
                Console.WriteLine($"[DEBUG] Uploading signature...");
                var imageData = BitmapToBytes(SignatureImage);
                if (imageData != null)
                {
                    await _customerService.UploadCustomerImageAsync(
                        customerId, "signature", imageData, "image/png");
                    Console.WriteLine($"[DEBUG] Signature uploaded successfully");
                }
            }

            // Upload fingerprint if captured
            if (FingerprintImage != null)
            {
                Console.WriteLine($"[DEBUG] Uploading fingerprint...");
                var imageData = BitmapToBytes(FingerprintImage);
                if (imageData != null)
                {
                    await _customerService.UploadCustomerImageAsync(
                        customerId, "fingerprint", imageData, "image/png");
                    Console.WriteLine($"[DEBUG] Fingerprint uploaded successfully");
                }
            }
            
            Console.WriteLine($"[DEBUG] UploadCustomerImagesAsync completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Error uploading images: {ex.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            // Don't throw - images are optional
        }
    }

    private async Task UploadBuyerImagesAsync(long buyerId)
    {
        // Buyers upload some images immediately (idcard/license/signature), but photo/fingerprint
        // may still only be captured locally, so we support uploading any present images here.
        try
        {
            if (IdCardImage != null)
            {
                var data = BitmapToBytes(IdCardImage);
                if (data != null)
                    await _buyerService.UploadBuyerImageAsync(buyerId, "idcard", data, "image/png");
            }

            if (DriverLicenseImage != null)
            {
                var data = BitmapToBytes(DriverLicenseImage);
                if (data != null)
                    await _buyerService.UploadBuyerImageAsync(buyerId, "driverlicense", data, "image/png");
            }

            if (PhotoImage != null)
            {
                var data = BitmapToBytes(PhotoImage);
                if (data != null)
                    await _buyerService.UploadBuyerImageAsync(buyerId, "photo", data, "image/png");
            }

            if (SignatureImage != null)
            {
                var data = BitmapToBytes(SignatureImage);
                if (data != null)
                    await _buyerService.UploadBuyerImageAsync(buyerId, "signature", data, "image/png");
            }

            if (FingerprintImage != null)
            {
                var data = BitmapToBytes(FingerprintImage);
                if (data != null)
                    await _buyerService.UploadBuyerImageAsync(buyerId, "fingerprint", data, "image/png");
            }
        }
        catch
        {
            // optional
        }
    }

    private byte[]? BitmapToBytes(Avalonia.Media.Imaging.Bitmap bitmap)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream);
            return memoryStream.ToArray();
        }
        catch
        {
            return null;
        }
    }

    // --- Nested helpers ---

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
        public int companiesCount { get; set; }
        public int sitesCount { get; set; }
        public int productsCount { get; set; }
    }

    // --- Pagination ---
    public PaginationViewModel PaginationViewModel { get; } = new();

    private ObservableCollection<CustomerDto> _pagedCustomerSearchResults = new();

    private ObservableCollection<BuyerDto> _pagedBuyerSearchResults = new();
    public ObservableCollection<BuyerDto> PagedBuyerSearchResults
    {
        get => _pagedBuyerSearchResults;
        private set
        {
            if (_pagedBuyerSearchResults == value) return;
            _pagedBuyerSearchResults = value;
            OnPropertyChanged();
        }
    }
    public ObservableCollection<CustomerDto> PagedCustomerSearchResults
    {
        get => _pagedCustomerSearchResults;
        private set
        {
            if (_pagedCustomerSearchResults == value) return;
            _pagedCustomerSearchResults = value;
            OnPropertyChanged();
        }
    }

    private void InitializePagination()
    {
        PaginationViewModel.PageChanged += OnPaginationPageChanged;
    }

    private void UpdatePagedResults()
    {
        var skip = PaginationViewModel.GetSkip();
        var take = PaginationViewModel.GetTake();

        if (CurrentSection == EnumMainSection.Buyers)
        {
            PagedBuyerSearchResults.Clear();
            var paged = BuyerSearchResults.Skip(skip).Take(take).ToList();
            foreach (var item in paged)
                PagedBuyerSearchResults.Add(item);
        }
        else
        {
            PagedCustomerSearchResults.Clear();
            var paged = CustomerSearchResults.Skip(skip).Take(take).ToList();
            foreach (var item in paged)
                PagedCustomerSearchResults.Add(item);
        }
    }

    private void OnPaginationPageChanged(object? sender, EventArgs e)
    {
        UpdatePagedResults();
    }

    private async Task SearchBuyerAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Searching buyers...";

        try
        {
            long? buyerId = null;
            if (long.TryParse(SearchCustomerIdText, out var bid))
                buyerId = bid;

            long? siteId = null;
            if (long.TryParse(SearchSiteIdText, out var sid))
                siteId = sid;

            long? provinceId = null;
            if (SearchProvince != null &&
                !string.Equals(SearchProvince.ProvinceName, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                provinceId = SearchProvince.ProvinceId;
            }

            long? countryId = null;
            if (SearchCountry != null &&
                !string.Equals(SearchCountry.CountryName, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                countryId = SearchCountry.CountryId;
            }

            var request = new BuyerSearchRequestDto
            {
                BuyerId = buyerId,
                SiteId = siteId ?? (SelectedSearchSite?.SiteId),
                FirstName = string.IsNullOrWhiteSpace(SearchFirstNameText) ? null : SearchFirstNameText.Trim(),
                LastName = string.IsNullOrWhiteSpace(SearchLastNameText) ? null : SearchLastNameText.Trim(),
                CompanyName = SelectedSearchCompany?.CompanyName ?? (string.IsNullOrWhiteSpace(SearchCompanyNameText) ? null : SearchCompanyNameText.Trim()),
                IdNumber = string.IsNullOrWhiteSpace(SearchIdNumberText) ? null : SearchIdNumberText.Trim(),
                AccountNumber = ParseAccountNumberOrNull(SearchAccountNumberText),
                PriceCode = string.IsNullOrEmpty(SearchPriceCode?.Code) ? null : SearchPriceCode.Code.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(SearchPhoneNumberText) ? null : SearchPhoneNumberText,
                MobileNumber = string.IsNullOrWhiteSpace(SearchMobileNumberText) ? null : SearchMobileNumberText,
                Email = string.IsNullOrWhiteSpace(SearchEmailText) ? null : SearchEmailText,
                ProvinceId = provinceId,
                CountryId = countryId
            };

            var results = await _buyerService.SearchBuyersAsync(request);

            BuyerSearchResults.Clear();
            if (results != null)
            {
                foreach (var b in results)
                    BuyerSearchResults.Add(b);
            }

            PaginationViewModel.SetTotalRecords(BuyerSearchResults.Count);
            PaginationViewModel.PageChanged -= OnPaginationPageChanged;
            PaginationViewModel.PageChanged += OnPaginationPageChanged;
            UpdatePagedResults();

            if (BuyerSearchResults.Count == 0)
            {
                StatusMessage = "No buyers found.";
                FoundBuyer = null;
            }
            else
            {
                StatusMessage = $"Found {BuyerSearchResults.Count} buyer(s).";
                FoundBuyer = PagedBuyerSearchResults.Count > 0 ? PagedBuyerSearchResults[0] : BuyerSearchResults[0];
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

    private async Task CreateBuyerAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Creating buyer...";

        try
        {
            if (SelectedNewCompany == null || SelectedNewSite == null)
            {
                StatusMessage = "Company and Site are required for buyers.";
                return;
            }

            // Guard: ensure site belongs to company
            if (SelectedNewSite.CompanyId != SelectedNewCompany.CompanyId)
            {
                StatusMessage = "Selected Site does not belong to the selected Company.";
                return;
            }

            // Allocate account number at time of create (not on Clear)
            if (NewAccountNumber == null)
                NewAccountNumber = await _buyerService.GetNextAccountNumberAsync();

            var dto = new BuyerDto
            {
                BuyerId = 0,
                FirstName = NewFirstName,
                LastName = NewLastName,
                IdNumber = NewIdNumber,
                AccountNumber = NewAccountNumber,
                IsCompany = true, // buyers are always company-based
                CompanyId = (int)SelectedNewCompany.CompanyId,
                SiteId = (int)SelectedNewSite.SiteId,
                IsTaxable = NewTaxable,
                Taxable = NewTaxable,
                PriceCode = string.IsNullOrEmpty(SelectedPriceCodeChar?.Code) ? null : SelectedPriceCodeChar.Code.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(NewPhoneNumber) ? null : NewPhoneNumber,
                MobileNumber = string.IsNullOrWhiteSpace(NewMobileNumber) ? null : NewMobileNumber,
                Email = string.IsNullOrWhiteSpace(NewEmail) ? null : NewEmail
            };

            var created = await _buyerService.CreateBuyerAsync(dto);
            if (created == null)
            {
                StatusMessage = "Buyer create failed.";
                return;
            }

            StatusMessage = $"Buyer {created.FirstName} {created.LastName} created (Account {created.AccountNumberDisplay}).";

            // Refresh list and reset form
            await SearchBuyerAsync();
            await ClearNewBuyerFormAsync();
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

    private void OnEditBuyer(BuyerDto? buyer)
    {
        if (buyer == null) return;
        FoundBuyer = buyer;
    }

    private static long? ParseAccountNumberOrNull(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        text = text.Trim();
        return long.TryParse(text, out var value) ? value : null;
    }

    private bool _isEditMode;
    private long? _editingCustomerId;
    private int? _editingBuyerId;

    public bool IsEditMode
    {
        get => _isEditMode;
        set
        {
            if (_isEditMode == value) return;
            _isEditMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsCreateMode));
            (UpdateCustomerCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
            (UpdateBuyerCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    // Used by BuyersView/Create button visibility
    public bool IsCreateMode => !IsEditMode;

    public long? EditingCustomerId
    {
        get => _editingCustomerId;
        set
        {
            if (_editingCustomerId == value) return;
            _editingCustomerId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanUpdateCustomer));
            (UpdateCustomerCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    public int? EditingBuyerId
    {
        get => _editingBuyerId;
        set
        {
            if (_editingBuyerId == value) return;
            _editingBuyerId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanUpdateBuyer));
            (UpdateBuyerCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private void OnEditCustomer(CustomerDto? customer)
    {
        if (customer == null) return;
        FoundCustomer = customer;
        IsEditMode = true;
        EditingCustomerId = customer.CustomerId;
        (UpdateCustomerCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
    }

    private async Task OnDeleteCustomerAsync(CustomerDto? customer)
    {
        if (customer == null) return;
        await _customerService.SoftDeleteCustomerAsync(customer.CustomerId);
        await SearchCustomerAsync();
    }

    private void OnLogTicket(CustomerDto? customer)
    {
        if (customer == null) return;
        // Placeholder: existing ticket logic lives in other partials.
        StatusMessage = $"Selected customer {customer.CustomerId} for ticketing.";
    }

    private async Task OnUpdateCustomerAsync()
    {
        if (IsBusy) return;
        if (!EditingCustomerId.HasValue)
            return;

        IsBusy = true;
        StatusMessage = "Updating customer...";

        try
        {
            // Build DTO from form fields (Create/Edit panel)
            var dto = new CustomerDto
            {
                CustomerId = (int)EditingCustomerId.Value,
                FirstName = NewFirstName,
                LastName = NewLastName,
                IdNumber = NewIdNumber,
                Email = NewEmail,
                PhoneNumber = NewPhoneNumber,
                MobileNumber = NewMobileNumber,
                PriceCode = string.IsNullOrEmpty(SelectedPriceCodeChar?.Code) ? null : SelectedPriceCodeChar.Code.Trim(),
                Taxable = NewTaxable,
                AccountNumber = NewAccountNumber,
                IsCompany = NewIsCompany,
                CompanyId = NewIsCompany && SelectedNewCompany != null ? (int?)SelectedNewCompany.CompanyId : null,
                SiteId = NewIsCompany && SelectedNewSite != null ? (int?)SelectedNewSite.SiteId : null
            };

            // Upload captured images (if any) before refreshing
            await UploadCustomerImagesAsync(dto.CustomerId);

            await _customerService.UpdateCustomerAsync(dto);

            // Refresh just this customer and keep selection
            var refreshed = await _customerService.GetCustomerByIdAsync(dto.CustomerId);
            if (refreshed == null)
            {
                StatusMessage = "Customer updated, but refresh failed.";
                return;
            }

            var existing = CustomerSearchResults.FirstOrDefault(c => c.CustomerId == refreshed.CustomerId);
            if (existing != null)
            {
                var index = CustomerSearchResults.IndexOf(existing);
                if (index >= 0)
                    CustomerSearchResults[index] = refreshed;
            }

            // Keep paged results in sync
            var existingPaged = PagedCustomerSearchResults.FirstOrDefault(c => c.CustomerId == refreshed.CustomerId);
            if (existingPaged != null)
            {
                var index = PagedCustomerSearchResults.IndexOf(existingPaged);
                if (index >= 0)
                    PagedCustomerSearchResults[index] = refreshed;
            }

            // Keep selection and refresh form + images
            FoundCustomer = refreshed;
            await LoadSelectedCustomerImagesAsync(refreshed);

            StatusMessage = "Customer updated.";
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = $"Error calling API: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error updating customer: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OnUpdateBuyerAsync()
    {
        if (IsBusy) return;
        if (!EditingBuyerId.HasValue)
        {
            StatusMessage = "Select a buyer to update.";
            return;
        }

        if (SelectedNewCompany == null || SelectedNewSite == null)
        {
            StatusMessage = "Company and Site are required.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Updating buyer...";

        try
        {
            var dto = new BuyerDto
            {
                BuyerId = EditingBuyerId.Value,
                FirstName = NewFirstName,
                LastName = NewLastName,
                IdNumber = NewIdNumber,
                AccountNumber = NewAccountNumber,
                IsCompany = true,
                CompanyId = (int)SelectedNewCompany.CompanyId,
                SiteId = (int)SelectedNewSite.SiteId,
                IsTaxable = NewTaxable,
                Taxable = NewTaxable,
                PriceCode = string.IsNullOrEmpty(SelectedPriceCodeChar?.Code) ? null : SelectedPriceCodeChar.Code.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(NewPhoneNumber) ? null : NewPhoneNumber,
                MobileNumber = string.IsNullOrWhiteSpace(NewMobileNumber) ? null : NewMobileNumber,
                Email = string.IsNullOrWhiteSpace(NewEmail) ? null : NewEmail
            };

            // Upload captured images (if any) before refreshing
            await UploadBuyerImagesAsync(dto.BuyerId);

            await _buyerService.UpdateBuyerAsync(dto);

            // Refresh just this buyer and keep selection
            var refreshed = await _buyerService.GetBuyerByIdAsync(dto.BuyerId);
            if (refreshed == null)
            {
                StatusMessage = "Buyer updated, but refresh failed.";
                return;
            }

            var existing = BuyerSearchResults.FirstOrDefault(b => b.BuyerId == refreshed.BuyerId);
            if (existing != null)
            {
                var index = BuyerSearchResults.IndexOf(existing);
                if (index >= 0)
                    BuyerSearchResults[index] = refreshed;
            }

            var existingPaged = PagedBuyerSearchResults.FirstOrDefault(b => b.BuyerId == refreshed.BuyerId);
            if (existingPaged != null)
            {
                var index = PagedBuyerSearchResults.IndexOf(existingPaged);
                if (index >= 0)
                    PagedBuyerSearchResults[index] = refreshed;
            }

            FoundBuyer = refreshed;
            await LoadSelectedBuyerImagesAsync(refreshed);

            StatusMessage = "Buyer updated.";
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = $"Error calling API: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error updating buyer: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OnDeleteBuyerAsync(BuyerDto? buyer)
    {
        if (buyer == null) return;
        await _buyerService.SoftDeleteBuyerAsync(buyer.BuyerId);
        await SearchBuyerAsync();
    }

    private void OnLogBuyerTicket(BuyerDto? buyer)
    {
        if (buyer == null) return;
        StatusMessage = $"Selected buyer {buyer.BuyerId} for ticketing.";
    }

    private void ClearCustomerSearch()
    {
        SearchCustomerIdText = string.Empty;
        SearchSiteIdText = string.Empty;
        SearchFirstNameText = string.Empty;
        SearchLastNameText = string.Empty;
        SearchCompanyNameText = string.Empty;
        SearchIdNumberText = string.Empty;
        SearchPhoneNumberText = string.Empty;
        SearchMobileNumberText = string.Empty;
        SearchEmailText = string.Empty;
        SearchAccountNumberText = string.Empty;
        CustomerSearchResults.Clear();
        PagedCustomerSearchResults.Clear();
        FoundCustomer = null;
        PaginationViewModel.SetTotalRecords(0);
    }

    private void ClearBuyerSearch()
    {
        BuyerSearchResults.Clear();
        FoundBuyer = null;
    }

    private async Task ClearNewCustomerFormAsync()
    {
        // Reset edit mode
        FoundCustomer = null;
        EditingCustomerId = null;
        IsEditMode = false;

        // Clear form fields
        NewFirstName = string.Empty;
        NewLastName = string.Empty;
        NewIdNumber = string.Empty;
        NewEmail = string.Empty;
        NewPhoneNumber = string.Empty;
        NewMobileNumber = string.Empty;
        NewPriceCode = string.Empty;
        NewIsCompany = false;
        NewCompanyName = null;
        NewTaxable = true;

        // Reset company/site + derived address
        SelectedNewCompanyLetter = "ALL";
        SelectedNewCompany = null;
        SelectedNewSite = null;
        NewSiteSuggestions.Clear();

        // Reset images
        IdCardImage = null;
        DriverLicenseImage = null;
        PhotoImage = null;
        SignatureImage = null;
        FingerprintImage = null;

        SelectedIdCardImage = null;
        SelectedDriverLicenseImage = null;
        SelectedPhotoImage = null;
        SelectedSignatureImage = null;
        SelectedFingerprintImage = null;

        NewCountry = Countries.FirstOrDefault();
        NewProvince = Provinces.FirstOrDefault();

        // Fetch a preview of the next globally-unique account number.
        // This uses the shared generator (max(customers,buyers)+1) and does not consume a sequence.
        NewAccountNumber = await _customerService.GetNextAccountNumberAsync();
        OnPropertyChanged(nameof(NewAccountNumberDisplay));

        OnPropertyChanged(nameof(CanCreateCustomer));
        (UpdateCustomerCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
    }

    private async Task ClearNewBuyerFormAsync()
    {
        // Reset edit mode
        FoundBuyer = null;
        EditingBuyerId = null;
        IsEditMode = false;

        // Clear form fields
        NewFirstName = string.Empty;
        NewLastName = string.Empty;
        NewIdNumber = string.Empty;
        NewEmail = string.Empty;
        NewPhoneNumber = string.Empty;
        NewMobileNumber = string.Empty;
        SelectedPriceCodeChar = PriceCodeOptions.FirstOrDefault();
        NewTaxable = false;

        // Reset company/site
        SelectedNewCompanyLetter = "ALL";
        SelectedNewCompany = null;
        SelectedNewSite = null;
        NewSiteSuggestions.Clear();

        // Reset images
        IdCardImage = null;
        DriverLicenseImage = null;
        PhotoImage = null;
        SignatureImage = null;
        FingerprintImage = null;

        SelectedIdCardImage = null;
        SelectedDriverLicenseImage = null;
        SelectedPhotoImage = null;
        SelectedSignatureImage = null;
        SelectedFingerprintImage = null;

        // Fetch a preview of the next globally-unique account number.
        // This uses the shared generator (max(customers,buyers)+1) and does not consume a sequence.
        NewAccountNumber = await _buyerService.GetNextAccountNumberAsync();

        OnPropertyChanged(nameof(NewAccountNumberDisplay));
        OnPropertyChanged(nameof(IsNewBuyerFullNameInvalid));
        OnPropertyChanged(nameof(CanCreateBuyer));
        (UpdateBuyerCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
    }

    private static Avalonia.Media.Imaging.Bitmap? LoadBitmapFromBytes(byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0) return null;
        try
        {
            using var ms = new MemoryStream(bytes);
            return new Avalonia.Media.Imaging.Bitmap(ms);
        }
        catch
        {
            return null;
        }
    }

    private async Task CaptureIdCardAsync()
    {
        var result = await _app.DocumentScanner.ScanDocumentAsync(DocumentType.IdCard);
        if (!result.IsSuccess || result.ImageData == null) return;

        IdCardImage = LoadBitmapFromBytes(result.ImageData);

        // Upload depending on context
        if (CurrentSection == EnumMainSection.Buyers)
        {
            if (FoundBuyer == null)
            {
                StatusMessage = "Select a buyer before scanning ID.";
                return;
            }

            await _buyerService.UploadBuyerImageAsync(FoundBuyer.BuyerId, "idcard", result.ImageData, "image/png");
            SelectedIdCardImage = IdCardImage;
            StatusMessage = "Buyer ID card uploaded.";
        }
        else
        {
            if (FoundCustomer == null)
                return;

            // For customers, upload when saving/creating; keep preview here.
            StatusMessage = "Customer ID card captured.";
        }
    }

    private async Task CaptureDriverLicenseAsync()
    {
        var result = await _app.DocumentScanner.ScanDocumentAsync(DocumentType.DriverLicense);
        if (!result.IsSuccess || result.ImageData == null) return;

        DriverLicenseImage = LoadBitmapFromBytes(result.ImageData);

        if (CurrentSection == EnumMainSection.Buyers)
        {
            if (FoundBuyer == null)
            {
                StatusMessage = "Select a buyer before scanning license.";
                return;
            }

            await _buyerService.UploadBuyerImageAsync(FoundBuyer.BuyerId, "driverlicense", result.ImageData, "image/png");
            SelectedDriverLicenseImage = DriverLicenseImage;
            StatusMessage = "Buyer driver license uploaded.";
        }
        else
        {
            if (FoundCustomer == null)
                return;

            StatusMessage = "Customer driver license captured.";
        }
    }

    private async Task CapturePhotoAsync()
    {
        var capture = await _cameraService.CaptureAsync(CameraDeviceType.CustomerPhoto, "photo");
        PhotoImage = LoadBitmapFromBytes(capture.ImageData);
    }

    private async Task CaptureFingerprintAsync()
    {
        var result = await _app.FingerprintScanner.CaptureAsync();
        if (result.IsSuccess && result.ImageData != null)
            FingerprintImage = LoadBitmapFromBytes(result.ImageData);
    }
}
