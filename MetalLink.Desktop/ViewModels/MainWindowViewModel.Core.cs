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
using MetalLink.Desktop.Auth;
using MetalLink.Desktop.Hardware;
using MetalLink.Desktop.Services;
using MetalLink.Shared.Customers;
using MetalLink.Shared.Buyers;
using MetalLink.Shared.Tickets.Receiving;
using MetalLink.Shared.Tickets.Sending;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Threading;
using System.Globalization;
using Avalonia.Media.Imaging;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject, INotifyPropertyChanged
{
    private readonly App _app;
    private readonly AuthState _authState;
    private readonly ApiClient _apiClient;
    private readonly CustomerService _customerService;
    private readonly BuyerService _buyerService;
    private readonly TicketReceivingService _ticketReceivingService;
    private readonly TicketSendingService _ticketSendingService;
    private readonly ProvinceService _provinceService;
    private readonly IScaleService _scaleService;
    private readonly DocumentService _documentService;
    private readonly ICameraService _cameraService;
    private readonly TicketReportService _ticketReportService;
    private readonly ISignaturePadService _signaturePadService;
    private readonly ThemeService _themeService;
    private readonly AppearanceService _appearanceService;

    // Navigation
    public ObservableCollection<NavItemViewModel> NavItems { get; } = new();
    public ICommand ToggleNavCommand { get; }
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
    public ICommand ShowReportsCommand { get; }
    public ICommand ShowStockLevelsCommand { get; }
    public ICommand ShowStockMovementCommand { get; }
    public ICommand ShowSettingsCommand { get; }

    // Logic ViewModels
    public MetalLink.Desktop.ViewModels.Receiving.TicketsReceivingViewModel Receiving { get; }
    public MetalLink.Desktop.ViewModels.Sending.TicketsSendingViewModel Sending { get; }

    // Core Commands
    public ICommand LogoutCommand { get; }
    public ICommand CheckDbCommand { get; }
    public ICommand SearchCustomerCommand { get; }
    public ICommand CreateCustomerCommand { get; }
    public ICommand ClearNewCustomerCommand { get; }
    public ICommand ClearCustomerSearchCommand { get; }
    public ICommand UpdateCustomerCommand { get; }
    public ICommand EditCustomerCommand { get; }
    public ICommand DeleteCustomerCommand { get; }
    public ICommand LogTicketCommand { get; }

    public ICommand SearchBuyerCommand { get; }
    public ICommand CreateBuyerCommand { get; }
    public ICommand ClearNewBuyerCommand { get; }
    public ICommand ClearBuyerSearchCommand { get; }
    public ICommand UpdateBuyerCommand { get; }
    public ICommand EditBuyerCommand { get; }
    public ICommand DeleteBuyerCommand { get; }
    public ICommand LogBuyerTicketCommand { get; }

    // Ticket Wrapper Commands
    public ICommand FinalizeReceivingTicketCommand { get; }
    public ICommand FinalizeSendingTicketCommand { get; }
    public ICommand AddReceivingLineCommand { get; }
    public ICommand RemoveReceivingLineCommand { get; }
    public ICommand RemoveSendingLineCommand { get; }
    public ICommand ReadWeighbridgeCommand { get; }
    public ICommand ReadWeighbridgeSecondCommand { get; }
    public ICommand ReadPlatformCommand { get; }
    public ICommand ResetWeighbridgeWeightsCommand { get; }
    public ICommand ResetPlatformWeightCommand { get; }
    public ICommand SaveTicketCommand { get; }
    public ICommand ClearReceivingTicketCommand { get; }
    public ICommand ClearSendingTicketCommand { get; }
    public ICommand CaptureWeightCommand { get; }
    public ICommand CapturePlatePhotoCommand { get; }
    public ICommand CaptureLoadPhotoCommand { get; }
    public ICommand PrintReceivingTicketCommand { get; }
    public ICommand PrintSendingTicketCommand { get; }
    public ICommand ShowLineNotesCommand { get; }
    public ICommand CloseLineNotesCommand { get; }
    public ICommand ScrollToAddLinesCommand { get; }
    public ICommand CreateReceivingTicketHeaderCommand { get; }
    public ICommand CreateSendingTicketHeaderCommand { get; }
    public ICommand SaveAndResetReceivingTicketCommand { get; }

    // Image/Document Commands
    public ICommand LoadCustomerDocumentsCommand { get; }
    public ICommand UploadCustomerDocumentCommand { get; }
    public ICommand CaptureSignatureCommand { get; }
    public ICommand CaptureIdCardCommand { get; }
    public ICommand CaptureDriverLicenseCommand { get; }
    public ICommand CapturePhotoCommand { get; }
    public ICommand CaptureFingerprintCommand { get; }

    public MainWindowViewModel(App app)
    {
        _app = app;
        _authState = app.AuthState;
        _apiClient = app.ApiClient;
        _customerService = app.CustomerService;
        _buyerService = app.BuyerService;
        _ticketReceivingService = app.TicketReceivingService;
        _ticketSendingService = app.TicketSendingService;
        _provinceService = app.ProvinceService;
        _scaleService = app.ScaleService;
        _documentService = app.DocumentService;
        _cameraService = app.CameraService;
        _ticketReportService = app.TicketReportService;
        _signaturePadService = app.SignaturePadService;
        _themeService = app.ThemeService;
        _appearanceService = app.AppearanceService;

        Receiving = new MetalLink.Desktop.ViewModels.Receiving.TicketsReceivingViewModel(_ticketReceivingService, new CompanyAndSiteService(_apiClient, _authState), _scaleService, new ProductsAndPricesService(_apiClient, _authState));
        Sending = new MetalLink.Desktop.ViewModels.Sending.TicketsSendingViewModel(_ticketSendingService, new CompanyAndSiteService(_apiClient, _authState), new ProductsAndPricesService(_apiClient, _authState));

        ToggleNavCommand = new RelayCommand(() => IsNavCollapsed = !IsNavCollapsed);
        ShowDashboardCommand = new RelayCommand(() => CurrentSection = EnumMainSection.Dashboard);
        ShowCustomersCommand = new AsyncRelayCommand(async () => {
            CurrentSection = EnumMainSection.Customers;
            ClearCustomerSearch();
            ClearNewCustomerForm();
            CustomerIsCreateEditExpanded = true;
            CustomerIsSearchCriteriaExpanded = true;
            CustomerIsSearchResultsExpanded = false;
            CustomerIsDetailsExpanded = false;
            await LoadAllPriceListsAsync();
        });
        ShowBuyersCommand = new AsyncRelayCommand(async () => {
            CurrentSection = EnumMainSection.Buyers;
            await ClearBuyerSearchAsync();
            await ClearNewBuyerFormAsync();
            BuyerIsCreateEditExpanded = true;
            BuyerIsSearchCriteriaExpanded = true;
            BuyerIsSearchResultsExpanded = false;
            BuyerIsDetailsExpanded = false;
            await LoadOperatorSettingsAsync();
            if (EnforceBuyerCompany) NewIsCompany = true;
            await LoadAllPriceListsAsync();
        });
        ShowCompanyAndSitesCommand = new RelayCommand(() => CurrentSection = EnumMainSection.CompanyAndSites);
        ShowProductsAndPricesCommand = new RelayCommand(() => CurrentSection = EnumMainSection.ProductsAndPrices);
        ShowTicketsCommand = new RelayCommand(() => CurrentSection = EnumMainSection.TicketsReceiving);
        ShowTicketsReceivingCommand = new RelayCommand(() => CurrentSection = EnumMainSection.TicketsReceiving);
        ShowTicketsSendingCommand = new AsyncRelayCommand(async () => {
            CurrentSection = EnumMainSection.TicketsSending;
            await Sending.OnEnterTicketsSendingAsync();
        });
        ShowDocumentsCommand = new RelayCommand(() => CurrentSection = EnumMainSection.Documents);
        ShowCameraCommand = new RelayCommand(() => CurrentSection = EnumMainSection.Camera);
        ShowReportsCommand = new RelayCommand(() => CurrentSection = EnumMainSection.Reports);
        ShowStockLevelsCommand = new RelayCommand(() => CurrentSection = EnumMainSection.StockLevels);
        ShowStockMovementCommand = new RelayCommand(() => CurrentSection = EnumMainSection.StockMovement);
        ShowSettingsCommand = new RelayCommand(() => CurrentSection = EnumMainSection.Settings);

        LogoutCommand = new AsyncRelayCommand(LogoutAsync);
        CheckDbCommand = new AsyncRelayCommand(CheckDbAsync);
        SearchCustomerCommand = new AsyncRelayCommand(SearchCustomerAsync);
        CreateCustomerCommand = new AsyncRelayCommand(CreateCustomerAsync);
        ClearNewCustomerCommand = new RelayCommand(ClearNewCustomerForm);
        ClearCustomerSearchCommand = new RelayCommand(ClearCustomerSearch);
        UpdateCustomerCommand = new AsyncRelayCommand(OnUpdateCustomerAsync, () => CanUpdateCustomer);
        EditCustomerCommand = new RelayCommand<CustomerDto?>(OnEditCustomer);
        DeleteCustomerCommand = new AsyncRelayCommand<CustomerDto?>(OnDeleteCustomerAsync);
        LogTicketCommand = new RelayCommand<CustomerDto?>(OnLogTicket);

        SearchBuyerCommand = new AsyncRelayCommand(SearchBuyerAsync);
        CreateBuyerCommand = new AsyncRelayCommand(CreateBuyerAsync);
        ClearNewBuyerCommand = new AsyncRelayCommand(ClearNewBuyerFormAsync);
        ClearBuyerSearchCommand = new AsyncRelayCommand(ClearBuyerSearchAsync);
        UpdateBuyerCommand = new AsyncRelayCommand(OnUpdateBuyerAsync, () => CanUpdateBuyer);
        EditBuyerCommand = new RelayCommand<BuyerDto?>(OnEditBuyer);
        DeleteBuyerCommand = new AsyncRelayCommand<BuyerDto?>(OnDeleteBuyerAsync);
        LogBuyerTicketCommand = new RelayCommand<BuyerDto?>(OnLogBuyerTicket);

        FinalizeReceivingTicketCommand = new AsyncRelayCommand(() => Receiving.FinalizeReceivingTicketAsync());
        FinalizeSendingTicketCommand = new AsyncRelayCommand(() => Sending.FinalizeSendingTicketAsync());
        AddReceivingLineCommand = new AsyncRelayCommand(() => Receiving.AddReceivingLineAsync());
        RemoveReceivingLineCommand = new RelayCommand(() => { }); // Placeholder
        RemoveSendingLineCommand = new RelayCommand(() => { }); // Placeholder
        
        ReadWeighbridgeCommand = new AsyncRelayCommand(async () => {
            if (CurrentSection == EnumMainSection.TicketsReceiving) await Receiving.ReadWeighbridgeAsync();
            else if (CurrentSection == EnumMainSection.TicketsSending) await Sending.ReadWeighbridgeAsync();
        });
        ReadWeighbridgeSecondCommand = new AsyncRelayCommand(async () => {
            if (CurrentSection == EnumMainSection.TicketsReceiving) await Receiving.ReadWeighbridgeSecondAsync();
            else if (CurrentSection == EnumMainSection.TicketsSending) await Sending.ReadWeighbridgeSecondAsync();
        });
        ReadPlatformCommand = new AsyncRelayCommand(async () => {
            if (CurrentSection == EnumMainSection.TicketsReceiving) await Receiving.ReadPlatformAsync();
            else if (CurrentSection == EnumMainSection.TicketsSending) await Sending.ReadPlatformAsync();
        });
        ResetWeighbridgeWeightsCommand = new RelayCommand(() => {
            if (CurrentSection == EnumMainSection.TicketsReceiving) Receiving.ResetWeighbridgeWeights();
            else if (CurrentSection == EnumMainSection.TicketsSending) Sending.ResetWeighbridgeWeights();
        });
        ResetPlatformWeightCommand = new RelayCommand(() => {
            if (CurrentSection == EnumMainSection.TicketsReceiving) Receiving.ResetPlatformWeight();
            else if (CurrentSection == EnumMainSection.TicketsSending) Sending.ResetPlatformWeight();
        });

        SaveTicketCommand = new AsyncRelayCommand(async () => {
            if (CurrentSection == EnumMainSection.TicketsReceiving) await Receiving.SaveTicketAsync();
            else if (CurrentSection == EnumMainSection.TicketsSending) await Sending.SaveTicketAsync();
        });
        ClearReceivingTicketCommand = new AsyncRelayCommand(() => Receiving.ClearTicketAsync());
        ClearSendingTicketCommand = new AsyncRelayCommand(() => Sending.ClearSendingTicketAsync());
        CaptureWeightCommand = new AsyncRelayCommand(async () => {
            if (CurrentSection == EnumMainSection.TicketsReceiving) await Receiving.CaptureWeightAsync();
            else if (CurrentSection == EnumMainSection.TicketsSending) await Sending.CaptureWeightAsync();
        });
        CapturePlatePhotoCommand = new AsyncRelayCommand(async () => {
            if (CurrentSection == EnumMainSection.TicketsReceiving) await Receiving.CapturePlatePhotoAsync();
            else if (CurrentSection == EnumMainSection.TicketsSending) await Sending.CapturePlatePhotoAsync();
        });
        CaptureLoadPhotoCommand = new AsyncRelayCommand(async () => {
            if (CurrentSection == EnumMainSection.TicketsReceiving) await Receiving.CaptureLoadPhotoAsync();
            else if (CurrentSection == EnumMainSection.TicketsSending) await Sending.CaptureLoadPhotoAsync();
        });
        PrintReceivingTicketCommand = new AsyncRelayCommand(() => Receiving.PrintReceivingTicketAsync());
        PrintSendingTicketCommand = new AsyncRelayCommand(() => Sending.PrintSendingTicketAsync());
        ShowLineNotesCommand = new RelayCommand<string?>(n => { if (n != null) Receiving.ShowLineNotes(n); });
        CloseLineNotesCommand = new RelayCommand(() => Receiving.CloseLineNotes());
        ScrollToAddLinesCommand = new RelayCommand(() => Receiving.ScrollToAddLines());
        CreateReceivingTicketHeaderCommand = new AsyncRelayCommand(() => Receiving.CreateTicketHeaderAsync());
        CreateSendingTicketHeaderCommand = new AsyncRelayCommand(() => Sending.CreateSendingTicketHeaderAsync());
        SaveAndResetReceivingTicketCommand = new AsyncRelayCommand(() => Receiving.SaveAndResetReceivingTicketAsync());

        LoadCustomerDocumentsCommand = new AsyncRelayCommand(LoadCustomerDocumentsAsync);
        UploadCustomerDocumentCommand = new AsyncRelayCommand(UploadCustomerDocumentAsync);
        CaptureSignatureCommand = new AsyncRelayCommand(CaptureSignatureAsync);
        CaptureIdCardCommand = new AsyncRelayCommand(CaptureIdCardAsync);
        CaptureDriverLicenseCommand = new AsyncRelayCommand(CaptureDriverLicenseAsync);
        CapturePhotoCommand = new AsyncRelayCommand(CapturePhotoAsync);
        CaptureFingerprintCommand = new AsyncRelayCommand(CaptureFingerprintAsync);

        BuildNavItems();
    }

    private void BuildNavItems() 
    {
        NavItems.Clear();
        NavItems.Add(new NavItemViewModel { Title = "Dashboard", IconKey = "Dashboard", Command = ShowDashboardCommand });
        NavItems.Add(new NavItemViewModel { Title = "Customers", IconKey = "People", Command = ShowCustomersCommand });
        NavItems.Add(new NavItemViewModel { Title = "Buyers", IconKey = "People", Command = ShowBuyersCommand });
        NavItems.Add(new NavItemViewModel { Title = "Companies & Sites", IconKey = "Business", Command = ShowCompanyAndSitesCommand });
        NavItems.Add(new NavItemViewModel { Title = "Products & Prices", IconKey = "Inventory", Command = ShowProductsAndPricesCommand });
        NavItems.Add(new NavItemViewModel { Title = "Ticket Receiving", IconKey = "Download", Command = ShowTicketsReceivingCommand });
        NavItems.Add(new NavItemViewModel { Title = "Ticket Sending", IconKey = "Upload", Command = ShowTicketsSendingCommand });
        NavItems.Add(new NavItemViewModel { Title = "Stock Levels", IconKey = "Assessment", Command = ShowStockLevelsCommand });
        NavItems.Add(new NavItemViewModel { Title = "Stock Movement", IconKey = "History", Command = ShowStockMovementCommand });
        NavItems.Add(new NavItemViewModel { Title = "Reports", IconKey = "Analytics", Command = ShowReportsCommand });
        NavItems.Add(new NavItemViewModel { Title = "Documents", IconKey = "DocumentScanner", Command = ShowDocumentsCommand });
        NavItems.Add(new NavItemViewModel { Title = "Camera", IconKey = "Camera", Command = ShowCameraCommand });
        NavItems.Add(new NavItemViewModel { Title = "Settings", IconKey = "Settings", Command = ShowSettingsCommand });
    }

    private async Task LogoutAsync() { await Task.CompletedTask; }
    private async Task CheckDbAsync() { await Task.CompletedTask; }
    private async Task LoadAllPriceListsAsync() { await Task.CompletedTask; }
    private async Task LoadOperatorSettingsAsync() { await Task.CompletedTask; }
    private async Task SearchCustomerAsync()
    {
        try
        {
            long? customerId = null;
            if (long.TryParse(SearchCustomerIdText, out var cid)) customerId = cid;

            var request = new CustomerSearchRequestDto
            {
                CustomerId = customerId,
                FirstName = string.IsNullOrWhiteSpace(SearchCustomerFirstNameText) ? null : SearchCustomerFirstNameText.Trim(),
                LastName = string.IsNullOrWhiteSpace(SearchCustomerLastNameText) ? null : SearchCustomerLastNameText.Trim(),
                CompanyName = string.IsNullOrWhiteSpace(SearchCustomerCompanyNameText) ? null : SearchCustomerCompanyNameText.Trim(),
                IdNumber = string.IsNullOrWhiteSpace(SearchCustomerIdNumberText) ? null : SearchCustomerIdNumberText.Trim(),
                AccountNumber = long.TryParse(SearchCustomerAccountNumberText, out var acc) ? acc : null,
                PhoneNumber = string.IsNullOrWhiteSpace(SearchCustomerPhoneNumberText) ? null : SearchCustomerPhoneNumberText.Trim(),
                MobileNumber = string.IsNullOrWhiteSpace(SearchCustomerMobileNumberText) ? null : SearchCustomerMobileNumberText.Trim(),
                Email = string.IsNullOrWhiteSpace(SearchCustomerEmailText) ? null : SearchCustomerEmailText.Trim()
            };

            var results = await _customerService.SearchCustomersAsync(request);
            CustomerSearchResults.Clear();
            PagedCustomerSearchResults.Clear();
            if (results != null)
            {
                foreach (var c in results) 
                {
                    CustomerSearchResults.Add(c);
                    PagedCustomerSearchResults.Add(c);
                }
            }

            CustomerIsSearchResultsExpanded = true;
            CustomerIsDetailsExpanded = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] SearchCustomerAsync: {ex.Message}");
        }
    }

    private async Task SearchBuyerAsync()
    {
        try
        {
            long? buyerId = null;
            if (long.TryParse(SearchBuyerIdText, out var bid)) buyerId = bid;

            var request = new BuyerSearchRequestDto
            {
                BuyerId = buyerId,
                FirstName = string.IsNullOrWhiteSpace(SearchBuyerFirstNameText) ? null : SearchBuyerFirstNameText.Trim(),
                LastName = string.IsNullOrWhiteSpace(SearchBuyerLastNameText) ? null : SearchBuyerLastNameText.Trim(),
                CompanyName = string.IsNullOrWhiteSpace(SearchBuyerCompanyNameText) ? null : SearchBuyerCompanyNameText.Trim(),
                IdNumber = string.IsNullOrWhiteSpace(SearchBuyerIdNumberText) ? null : SearchBuyerIdNumberText.Trim(),
                AccountNumber = long.TryParse(SearchBuyerAccountNumberText, out var acc) ? acc : null,
                PhoneNumber = string.IsNullOrWhiteSpace(SearchBuyerPhoneNumberText) ? null : SearchBuyerPhoneNumberText.Trim(),
                MobileNumber = string.IsNullOrWhiteSpace(SearchBuyerMobileNumberText) ? null : SearchBuyerMobileNumberText.Trim(),
                Email = string.IsNullOrWhiteSpace(SearchBuyerEmailText) ? null : SearchBuyerEmailText.Trim()
            };

            var results = await _buyerService.SearchBuyersAsync(request);
            BuyerSearchResults.Clear();
            PagedBuyerSearchResults.Clear();
            if (results != null)
            {
                foreach (var b in results)
                {
                    BuyerSearchResults.Add(b);
                    PagedBuyerSearchResults.Add(b);
                }
            }

            BuyerIsSearchResultsExpanded = true;
            BuyerIsDetailsExpanded = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] SearchBuyerAsync: {ex.Message}");
        }
    }

    private void ClearCustomerSearch()
    {
        SearchCustomerIdText = string.Empty;
        SearchCustomerFirstNameText = string.Empty;
        SearchCustomerLastNameText = string.Empty;
        SearchCustomerCompanyNameText = string.Empty;
        SearchCustomerIdNumberText = string.Empty;
        SearchCustomerAccountNumberText = string.Empty;
        SearchCustomerPhoneNumberText = string.Empty;
        SearchCustomerMobileNumberText = string.Empty;
        SearchCustomerEmailText = string.Empty;
        CustomerSearchResults.Clear();
        PagedCustomerSearchResults.Clear();
        FoundCustomer = null;
        CustomerIsSearchResultsExpanded = false;
        CustomerIsDetailsExpanded = false;
    }

    private async Task ClearBuyerSearchAsync()
    {
        SearchBuyerIdText = string.Empty;
        SearchBuyerFirstNameText = string.Empty;
        SearchBuyerLastNameText = string.Empty;
        SearchBuyerCompanyNameText = string.Empty;
        SearchBuyerIdNumberText = string.Empty;
        SearchBuyerAccountNumberText = string.Empty;
        SearchBuyerPhoneNumberText = string.Empty;
        SearchBuyerMobileNumberText = string.Empty;
        SearchBuyerEmailText = string.Empty;
        BuyerSearchResults.Clear();
        PagedBuyerSearchResults.Clear();
        FoundBuyer = null;
        BuyerIsSearchResultsExpanded = false;
        BuyerIsDetailsExpanded = false;
        await Task.CompletedTask;
    }

    private void ClearNewCustomerForm()
    {
        NewFirstName = string.Empty;
        NewLastName = string.Empty;
        NewCompanyName = null;
        NewIdNumber = null;
        NewEmail = null;
        NewPhoneNumber = null;
        NewMobileNumber = null;
        NewTaxable = true;
        NewAccountNumber = null;
        IsEditMode = false;
        EditingCustomerId = null;
    }

    private async Task ClearNewBuyerFormAsync()
    {
        NewFirstName = string.Empty;
        NewLastName = string.Empty;
        NewCompanyName = null;
        NewIdNumber = null;
        NewEmail = null;
        NewPhoneNumber = null;
        NewMobileNumber = null;
        NewTaxable = true;
        NewAccountNumber = null;
        IsEditMode = false;
        EditingBuyerId = null;
        await Task.CompletedTask;
    }

    private async Task CreateCustomerAsync() { await Task.CompletedTask; }
    private async Task CreateBuyerAsync() { await Task.CompletedTask; }
    private async Task OnUpdateCustomerAsync() { await Task.CompletedTask; }
    private async Task OnUpdateBuyerAsync() { await Task.CompletedTask; }
    private void OnEditCustomer(CustomerDto? c) { FoundCustomer = c; }
    private async Task OnDeleteCustomerAsync(CustomerDto? c) { await Task.CompletedTask; }
    private void OnLogTicket(CustomerDto? c) { }
    private void OnEditBuyer(BuyerDto? b) { FoundBuyer = b; }
    private async Task OnDeleteBuyerAsync(BuyerDto? b) { await Task.CompletedTask; }
    private void OnLogBuyerTicket(BuyerDto? b) { }
    private async Task LoadCustomerDocumentsAsync() { await Task.CompletedTask; }
    private async Task UploadCustomerDocumentAsync() { await Task.CompletedTask; }
    private async Task CaptureSignatureAsync() { await Task.CompletedTask; }
    private async Task CaptureIdCardAsync() { await Task.CompletedTask; }
    private async Task CaptureDriverLicenseAsync() { await Task.CompletedTask; }
    private async Task CapturePhotoAsync() { await Task.CompletedTask; }
    private async Task CaptureFingerprintAsync() { await Task.CompletedTask; }
    public async Task InitializeLookupsAsync() { await Task.CompletedTask; }
    public Bitmap? LoadBitmapFromBytes(byte[]? data)
    {
        if (data == null || data.Length == 0) return null;
        try
        {
            using var ms = new System.IO.MemoryStream(data);
            return new Bitmap(ms);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] LoadBitmapFromBytes: {ex.Message}");
            return null;
        }
    }
    public async Task<bool> ConfirmAsync(string message) => await Task.FromResult(true);

    public async Task SetEnforceBuyerCompanyAsync(bool enabled)
    {
        EnforceBuyerCompany = enabled;
        await Task.CompletedTask;
    }
}
