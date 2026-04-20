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
using Avalonia.Platform.Storage;
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

    partial void InitializePriceListsCommands();

    // Navigation
    public ObservableCollection<NavItemViewModel> NavItems { get; } = new();
    public ICommand ToggleNavCommand { get; }
    public ICommand ShowDashboardCommand { get; }
    public ICommand ShowCustomersCommand { get; }
    public ICommand ShowBuyersCommand { get; }
    public ICommand ShowCompanyAndSitesCommand { get; }
    public ICommand ShowProductsCommand { get; }
    public ICommand ShowPriceListsCommand { get; }
    public ICommand ShowPricesCommand { get; }
    public ICommand ShowTicketsCommand { get; }
    public ICommand ShowTicketsReceivingCommand { get; }
    public ICommand ShowTicketsSendingCommand { get; }
    public ICommand ShowDocumentsCommand { get; }
    public ICommand ShowCameraCommand { get; }
    public ICommand ShowReportsCommand { get; }
    public ICommand ShowStockLevelsCommand { get; }
    public ICommand ShowStockMovementCommand { get; }
    public ICommand ShowStockMovementForProductCommand { get; }
    public ICommand ShowSettingsCommand { get; }

    // Logic ViewModels
    public MetalLink.Desktop.ViewModels.Receiving.TicketsReceivingViewModel Receiving { get; }
    public MetalLink.Desktop.ViewModels.Sending.TicketsSendingViewModel Sending { get; }
    public StockLevelsViewModel StockLevels { get; }
    public StockMovementViewModel StockMovement { get; }
    public PaginationViewModel PaginationViewModel { get; } = new();

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

        // Initialize "Play Intro Video" from AuthState settings
        var introSetting = _authState.OperatorSettings.FirstOrDefault(s => s.SettingName.Equals("playintrovideo", StringComparison.OrdinalIgnoreCase));
        if (introSetting != null)
        {
            _playIntroVideo = introSetting.SettingOptionValue.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
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

        Receiving = new MetalLink.Desktop.ViewModels.Receiving.TicketsReceivingViewModel(_ticketReceivingService, new CompanyAndSiteService(_apiClient, _authState), _scaleService, new ProductsService(_apiClient, _authState));
        Sending = new MetalLink.Desktop.ViewModels.Sending.TicketsSendingViewModel(_ticketSendingService, new CompanyAndSiteService(_apiClient, _authState), _scaleService, new ProductsService(_apiClient, _authState));
        StockLevels = new StockLevelsViewModel(_apiClient);
        StockMovement = new StockMovementViewModel(_apiClient);

        ToggleNavCommand = new RelayCommand(() => IsNavCollapsed = !IsNavCollapsed);
        ShowDashboardCommand = new RelayCommand(() => { 
            Console.WriteLine("[DEBUG] MainWindowViewModel: ShowDashboardCommand triggered");
            CurrentSection = EnumMainSection.Dashboard; 
        });
        ShowCustomersCommand = new AsyncRelayCommand(async () => {
            CurrentSection = EnumMainSection.Customers;
            ClearCustomerSearch();
            ClearNewCustomerForm();
            CustomerIsCreateEditExpanded = true;
            CustomerIsSearchCriteriaExpanded = true;
            CustomerIsSearchResultsExpanded = false;
            CustomerIsDetailsExpanded = false;
            await LoadAllPriceListsAsync();
            NewAccountNumber = await _customerService.GetNextAccountNumberAsync();
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
            NewAccountNumber = await _buyerService.GetNextAccountNumberAsync();
        });
        ShowCompanyAndSitesCommand = new RelayCommand(async () => {
            CurrentSection = EnumMainSection.CompanyAndSites;
            
            // Populate provinces/countries for site forms
            if (Provinces.Count == 0) await LoadProvincesAsync();
            InitializeCountries();

            PaginationViewModel.Reset();
            PaginationViewModel.PageSize = 15;
            
            SitePaginationViewModel.Reset();
            SitePaginationViewModel.PageSize = 10;

            await SearchCompaniesAsync();
        });
        ShowProductsCommand = new RelayCommand(() => CurrentSection = EnumMainSection.Products);
        ShowPriceListsCommand = new RelayCommand(async () => {
            CurrentSection = EnumMainSection.PriceLists;
            SelectedPriceListEntityType = "Customer";
            await SearchPriceListsAsync();
        });
        ShowPricesCommand = new RelayCommand(() => CurrentSection = EnumMainSection.Prices);
        ShowTicketsCommand = new RelayCommand(() => CurrentSection = EnumMainSection.TicketsReceiving);
        ShowTicketsReceivingCommand = new RelayCommand(() =>
        {
            Console.WriteLine("[DEBUG] MainWindowViewModel: ShowTicketsReceivingCommand triggered");
            CurrentSection = EnumMainSection.TicketsReceiving;
            Console.WriteLine($"[DEBUG] MainWindowViewModel: CurrentSection is now {CurrentSection}");
        });
        ShowTicketsSendingCommand = new AsyncRelayCommand(async () => {
            Console.WriteLine("[DEBUG] MainWindowViewModel: ShowTicketsSendingCommand triggered");
            CurrentSection = EnumMainSection.TicketsSending;
            Console.WriteLine($"[DEBUG] MainWindowViewModel: CurrentSection is now {CurrentSection}");
            await Sending.OnEnterTicketsSendingAsync();
        });
        ShowDocumentsCommand = new RelayCommand(() => CurrentSection = EnumMainSection.Documents);
        ShowCameraCommand = new RelayCommand(() => CurrentSection = EnumMainSection.Camera);
        ShowReportsCommand = new RelayCommand(() => CurrentSection = EnumMainSection.Reports);
        ShowStockLevelsCommand = new AsyncRelayCommand(async () => {
            CurrentSection = EnumMainSection.StockLevels;
            await StockLevels.RefreshAsync();
        });
        ShowStockMovementCommand = new AsyncRelayCommand(async () => {
            CurrentSection = EnumMainSection.StockMovement;
            await StockMovement.RefreshAsync();
        });
        ShowStockMovementForProductCommand = new AsyncRelayCommand<int?>(async productId => {
            CurrentSection = EnumMainSection.StockMovement;
            // Clear filters to ensure only this product shows
            StockMovement.ProductSearchText = string.Empty;
            StockMovement.SelectedProductLetter = "ALL";
            StockMovement.SelectedProduct = null;
            StockMovement.SetInitialProductId(productId);
            await StockMovement.RefreshAsync();
        });
        ShowSettingsCommand = new RelayCommand(() => CurrentSection = EnumMainSection.Settings);

        InitializePriceListsCommands();

        LogoutCommand = new AsyncRelayCommand(LogoutAsync);
        CheckDbCommand = new AsyncRelayCommand(CheckDbAsync);
        SearchCustomerCommand = new AsyncRelayCommand(SearchCustomerAsync);
        CreateCustomerCommand = new AsyncRelayCommand(CreateCustomerAsync, () => CanCreateCustomer);
        ClearNewCustomerCommand = new RelayCommand(ClearNewCustomerForm);
        ClearCustomerSearchCommand = new RelayCommand(ClearCustomerSearch);
        UpdateCustomerCommand = new AsyncRelayCommand(OnUpdateCustomerAsync, () => CanUpdateCustomer);
        EditCustomerCommand = new RelayCommand<CustomerDto?>(OnEditCustomer);
        DeleteCustomerCommand = new AsyncRelayCommand<CustomerDto?>(OnDeleteCustomerAsync);
        LogTicketCommand = new RelayCommand<CustomerDto?>(OnLogTicket);

        SearchBuyerCommand = new AsyncRelayCommand(SearchBuyerAsync);
        CreateBuyerCommand = new AsyncRelayCommand(CreateBuyerAsync, () => CanCreateBuyer);
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
        ShowLineNotesCommand = new RelayCommand<string?>(n => { 
            if (n == null) return;
            if (CurrentSection == EnumMainSection.TicketsReceiving) Receiving.ShowLineNotes(n);
            else if (CurrentSection == EnumMainSection.TicketsSending) Sending.ShowLineNotes(n);
        });
        CloseLineNotesCommand = new RelayCommand(() => {
            if (CurrentSection == EnumMainSection.TicketsReceiving) Receiving.CloseLineNotes();
            else if (CurrentSection == EnumMainSection.TicketsSending) Sending.CloseLineNotes();
        });
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

        PaginationViewModel.PageChanged += async (s, e) => {
            if (CurrentSection == EnumMainSection.Customers) await SearchCustomerAsync();
            else if (CurrentSection == EnumMainSection.Buyers) await SearchBuyerAsync();
            else if (CurrentSection == EnumMainSection.CompanyAndSites) UpdatePagedCompanyResults();
        };

        InitializeCompanyAndSiteCommands();
        InitializeProductsCommands();
        BuildNavItems();
    }

    private void BuildNavItems() 
    {
        NavItems.Clear();
        NavItems.Add(new NavItemViewModel { Title = "Dashboard", IconKey = "Dashboard", Command = ShowDashboardCommand });
        NavItems.Add(new NavItemViewModel { Title = "Customers", IconKey = "People", Command = ShowCustomersCommand });
        NavItems.Add(new NavItemViewModel { Title = "Buyers", IconKey = "People", Command = ShowBuyersCommand });
        NavItems.Add(new NavItemViewModel { Title = "Companies & Sites", IconKey = "Business", Command = ShowCompanyAndSitesCommand });
        // Commodities Group
        NavItems.Add(new NavItemViewModel { Title = "Products", IconKey = "Inventory", Command = ShowProductsCommand });
        NavItems.Add(new NavItemViewModel { Title = "Price Lists", IconKey = "List", Command = ShowPriceListsCommand, IsIndented = true });
        NavItems.Add(new NavItemViewModel { Title = "Prices", IconKey = "Sell", Command = ShowPricesCommand, IsIndented = true });
        
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
    private async Task LoadAllPriceListsAsync()
    {
        try
        {
            var lists = await _app.ProductsService.GetPriceListsAsync();
            CustomerPriceLists.Clear();
            BuyerPriceLists.Clear();
            foreach (var list in lists)
            {
                if (list.EntityFlag == "C") CustomerPriceLists.Add(list);
                else if (list.EntityFlag == "B") BuyerPriceLists.Add(list);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] LoadAllPriceListsAsync: {ex.Message}");
        }
    }

    private async Task LoadOperatorSettingsAsync() { await Task.CompletedTask; }
    private async Task SearchCustomerAsync()
    {
        try
        {
            int? customerId = null;
            if (int.TryParse(SearchCustomerIdText, out var cid)) customerId = cid;

            var request = new CustomerSearchRequestDto
            {
                CustomerId = customerId,
                SiteId = CustomerSelectedSearchSite?.SiteId,
                ProductPriceListId = SearchCustomerPriceList?.ProductPriceListId,
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
            if (results != null)
            {
                foreach (var c in results.OrderByDescending(x => x.CustomerId)) CustomerSearchResults.Add(c);
            }

            PaginationViewModel.SetTotalRecords(CustomerSearchResults.Count);
            UpdatePagedCustomerResults();

            FoundCustomer = PagedCustomerSearchResults.FirstOrDefault();

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
            int? buyerId = null;
            if (int.TryParse(SearchBuyerIdText, out var bid)) buyerId = bid;

            var request = new BuyerSearchRequestDto
            {
                BuyerId = buyerId,
                SiteId = BuyerSelectedSearchSite?.SiteId,
                ProductPriceListId = SearchBuyerPriceList?.ProductPriceListId,
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
            if (results != null)
            {
                foreach (var b in results.OrderByDescending(x => x.BuyerId)) BuyerSearchResults.Add(b);
            }

            PaginationViewModel.SetTotalRecords(BuyerSearchResults.Count);
            UpdatePagedBuyerResults();

            FoundBuyer = PagedBuyerSearchResults.FirstOrDefault();

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
        PaginationViewModel.Reset();
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
        PaginationViewModel.Reset();
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
        
        SelectedNewCompany = null;
        SelectedNewSite = null;
        SelectedNewProductPriceList = null;

        IdCardImage = null;
        DriverLicenseImage = null;
        PhotoImage = null;
        SignatureImage = null;
        FingerprintImage = null;

        NotifyFormProperties();
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

        SelectedNewCompany = null;
        SelectedNewSite = null;
        SelectedNewProductPriceList = null;

        IdCardImage = null;
        DriverLicenseImage = null;
        PhotoImage = null;
        SignatureImage = null;
        FingerprintImage = null;

        NotifyFormProperties();
        await Task.CompletedTask;
    }

    private async Task CreateCustomerAsync()
    {
        try
        {
            Console.WriteLine($"[DEBUG] CreateCustomerAsync started. Name: {NewFirstName} {NewLastName}, Company: {NewCompanyName}");
            var dto = new CustomerDto
            {
                FirstName = NewFirstName,
                LastName = NewLastName,
                IdNumber = NewIdNumber,
                AccountNumber = NewAccountNumber,
                Email = NewEmail,
                PhoneNumber = NewPhoneNumber,
                MobileNumber = NewMobileNumber,
                Taxable = NewTaxable,
                IsCompany = NewIsCompany,
                CompanyId = (int?)(SelectedNewCompany?.CompanyId),
                SiteId = (int?)(SelectedNewSite?.SiteId),
                ProductPriceListId = SelectedNewProductPriceList?.ProductPriceListId
            };

            var result = await _customerService.CreateCustomerAsync(dto);
            if (result != null)
            {
                Console.WriteLine($"[DEBUG] CreateCustomerAsync successful. New ID: {result.CustomerId}");
                
                // Upload any images captured during creation
                var idCardData = GetBytesFromBitmap(IdCardImage);
                if (idCardData != null) await _customerService.UploadCustomerImageAsync(result.CustomerId, "idcard", idCardData);
                
                var licenseData = GetBytesFromBitmap(DriverLicenseImage);
                if (licenseData != null) await _customerService.UploadCustomerImageAsync(result.CustomerId, "driverlicense", licenseData);
                
                var photoData = GetBytesFromBitmap(PhotoImage);
                if (photoData != null) await _customerService.UploadCustomerImageAsync(result.CustomerId, "photo", photoData);
                
                var sigData = GetBytesFromBitmap(SignatureImage);
                if (sigData != null) await _customerService.UploadCustomerImageAsync(result.CustomerId, "signature", sigData);
                
                var fpData = GetBytesFromBitmap(FingerprintImage);
                if (fpData != null) await _customerService.UploadCustomerImageAsync(result.CustomerId, "fingerprint", fpData);

                // Refresh search results
                await SearchCustomerAsync();
                
                // Re-select the record to show details
                var updated = CustomerSearchResults.FirstOrDefault(x => x.CustomerId == result.CustomerId);
                if (updated != null) 
                {
                    FoundCustomer = updated;
                    _ = LoadSelectedCustomerImagesAsync(updated);
                }
                
                CustomerIsDetailsExpanded = true;
                CustomerIsSearchResultsExpanded = true;
                CustomerIsCreateEditExpanded = false;
            }

            ClearNewCustomerForm();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] CreateCustomerAsync: {ex.Message}");
        }
    }

    private async Task CreateBuyerAsync()
    {
        try
        {
            Console.WriteLine($"[DEBUG] CreateBuyerAsync started. Name: {NewFirstName} {NewLastName}, Company: {NewCompanyName}");
            var dto = new BuyerDto
            {
                FirstName = NewFirstName,
                LastName = NewLastName,
                IdNumber = NewIdNumber,
                AccountNumber = NewAccountNumber,
                Email = NewEmail,
                PhoneNumber = NewPhoneNumber,
                MobileNumber = NewMobileNumber,
                Taxable = NewTaxable,
                IsCompany = NewIsCompany,
                CompanyId = (int?)(SelectedNewCompany?.CompanyId),
                SiteId = (int?)(SelectedNewSite?.SiteId),
                ProductPriceListId = SelectedNewProductPriceList?.ProductPriceListId
            };

            var result = await _buyerService.CreateBuyerAsync(dto);
            if (result != null)
            {
                Console.WriteLine($"[DEBUG] CreateBuyerAsync successful. New ID: {result.BuyerId}");
                
                // Upload any images captured during creation
                var idCardData = GetBytesFromBitmap(IdCardImage);
                if (idCardData != null) await _buyerService.UploadBuyerImageAsync(result.BuyerId, "idcard", idCardData);
                
                var licenseData = GetBytesFromBitmap(DriverLicenseImage);
                if (licenseData != null) await _buyerService.UploadBuyerImageAsync(result.BuyerId, "driverlicense", licenseData);
                
                var photoData = GetBytesFromBitmap(PhotoImage);
                if (photoData != null) await _buyerService.UploadBuyerImageAsync(result.BuyerId, "photo", photoData);
                
                var sigData = GetBytesFromBitmap(SignatureImage);
                if (sigData != null) await _buyerService.UploadBuyerImageAsync(result.BuyerId, "signature", sigData);
                
                var fpData = GetBytesFromBitmap(FingerprintImage);
                if (fpData != null) await _buyerService.UploadBuyerImageAsync(result.BuyerId, "fingerprint", fpData);

                // Refresh search results
                await SearchBuyerAsync();

                // Re-select the record to show details
                var updated = BuyerSearchResults.FirstOrDefault(x => x.BuyerId == result.BuyerId);
                if (updated != null) 
                {
                    FoundBuyer = updated;
                    _ = LoadSelectedBuyerImagesAsync(updated);
                }

                BuyerIsDetailsExpanded = true;
                BuyerIsSearchResultsExpanded = true;
                BuyerIsCreateEditExpanded = false;
            }

            await ClearNewBuyerFormAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] CreateBuyerAsync: {ex.Message}");
        }
    }

    private async Task OnUpdateCustomerAsync()
    {
        if (EditingCustomerId == null) return;
        try
        {
            var dto = new CustomerDto
            {
                CustomerId = EditingCustomerId.Value,
                FirstName = NewFirstName,
                LastName = NewLastName,
                IdNumber = NewIdNumber,
                AccountNumber = NewAccountNumber,
                Email = NewEmail,
                PhoneNumber = NewPhoneNumber,
                MobileNumber = NewMobileNumber,
                Taxable = NewTaxable,
                IsCompany = NewIsCompany,
                CompanyId = (int?)(SelectedNewCompany?.CompanyId),
                SiteId = (int?)(SelectedNewSite?.SiteId),
                ProductPriceListId = SelectedNewProductPriceList?.ProductPriceListId
            };

            await _customerService.UpdateCustomerAsync(dto);

            // Upload any new images captured
            var idCardData = GetBytesFromBitmap(IdCardImage);
            if (idCardData != null) await _customerService.UploadCustomerImageAsync(dto.CustomerId, "idcard", idCardData);
            
            var licenseData = GetBytesFromBitmap(DriverLicenseImage);
            if (licenseData != null) await _customerService.UploadCustomerImageAsync(dto.CustomerId, "driverlicense", licenseData);
            
            var photoData = GetBytesFromBitmap(PhotoImage);
            if (photoData != null) await _customerService.UploadCustomerImageAsync(dto.CustomerId, "photo", photoData);
            
            var sigData = GetBytesFromBitmap(SignatureImage);
            if (sigData != null) await _customerService.UploadCustomerImageAsync(dto.CustomerId, "signature", sigData);
            
            var fpData = GetBytesFromBitmap(FingerprintImage);
            if (fpData != null) await _customerService.UploadCustomerImageAsync(dto.CustomerId, "fingerprint", fpData);
            
            // Refresh results
            await SearchCustomerAsync();
            
            // Re-select the record to show updated details
            var updated = CustomerSearchResults.FirstOrDefault(x => x.CustomerId == dto.CustomerId);
            if (updated != null) 
            {
                FoundCustomer = updated;
                // Specifically trigger image reload for details view
                _ = LoadSelectedCustomerImagesAsync(updated);
            }
            
            CustomerIsDetailsExpanded = true;
            ClearNewCustomerForm();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] OnUpdateCustomerAsync: {ex.Message}");
        }
    }

    private async Task OnUpdateBuyerAsync()
    {
        if (EditingBuyerId == null) return;
        try
        {
            var dto = new BuyerDto
            {
                BuyerId = EditingBuyerId.Value,
                FirstName = NewFirstName,
                LastName = NewLastName,
                IdNumber = NewIdNumber,
                AccountNumber = NewAccountNumber,
                Email = NewEmail,
                PhoneNumber = NewPhoneNumber,
                MobileNumber = NewMobileNumber,
                Taxable = NewTaxable,
                IsCompany = NewIsCompany,
                CompanyId = (int?)(SelectedNewCompany?.CompanyId),
                SiteId = (int?)(SelectedNewSite?.SiteId),
                ProductPriceListId = SelectedNewProductPriceList?.ProductPriceListId
            };

            await _buyerService.UpdateBuyerAsync(dto);

            // Upload any new images captured
            var idCardData = GetBytesFromBitmap(IdCardImage);
            if (idCardData != null) await _buyerService.UploadBuyerImageAsync(dto.BuyerId, "idcard", idCardData);
            
            var licenseData = GetBytesFromBitmap(DriverLicenseImage);
            if (licenseData != null) await _buyerService.UploadBuyerImageAsync(dto.BuyerId, "driverlicense", licenseData);
            
            var photoData = GetBytesFromBitmap(PhotoImage);
            if (photoData != null) await _buyerService.UploadBuyerImageAsync(dto.BuyerId, "photo", photoData);
            
            var sigData = GetBytesFromBitmap(SignatureImage);
            if (sigData != null) await _buyerService.UploadBuyerImageAsync(dto.BuyerId, "signature", sigData);
            
            var fpData = GetBytesFromBitmap(FingerprintImage);
            if (fpData != null) await _buyerService.UploadBuyerImageAsync(dto.BuyerId, "fingerprint", fpData);
            
            // Refresh results
            await SearchBuyerAsync();
            
            // Re-select the record to show updated details
            var updated = BuyerSearchResults.FirstOrDefault(x => x.BuyerId == dto.BuyerId);
            if (updated != null) 
            {
                FoundBuyer = updated;
                // Specifically trigger image reload for details view
                _ = LoadSelectedBuyerImagesAsync(updated);
            }
            
            BuyerIsDetailsExpanded = true;
            await ClearNewBuyerFormAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] OnUpdateBuyerAsync: {ex.Message}");
        }
    }

    private void OnEditCustomer(CustomerDto? c) { FoundCustomer = c; }
    private async Task OnDeleteCustomerAsync(CustomerDto? c) 
    { 
        if (c == null) return;
        if (await ConfirmAsync($"Are you sure you want to delete customer {c.FirstName} {c.LastName}?"))
        {
            await _customerService.DeleteCustomerAsync(c.CustomerId);
            await SearchCustomerAsync();
        }
    }
    private void OnLogTicket(CustomerDto? c) { }
    private void OnEditBuyer(BuyerDto? b) { FoundBuyer = b; }
    private async Task OnDeleteBuyerAsync(BuyerDto? b) 
    { 
        if (b == null) return;
        if (await ConfirmAsync($"Are you sure you want to delete buyer {b.FirstName} {b.LastName}?"))
        {
            await _buyerService.DeleteBuyerAsync(b.BuyerId);
            await SearchBuyerAsync();
        }
    }
    private void OnLogBuyerTicket(BuyerDto? b) { }
    private async Task LoadCustomerDocumentsAsync() { await Task.CompletedTask; }
    private async Task UploadCustomerDocumentAsync() { await Task.CompletedTask; }
    
    private async Task CaptureSignatureAsync() 
    { 
        Console.WriteLine("[DEBUG] CaptureSignatureAsync triggered");
        var result = await _signaturePadService.CaptureAsync("Signature");
        if (result.ImageData != null) SignatureImage = LoadBitmapFromBytes(result.ImageData);
        Console.WriteLine($"[DEBUG] CaptureSignatureAsync finished. Success: {result.ImageData != null}");
    }
    
    private async Task CaptureIdCardAsync() 
    { 
        Console.WriteLine("[DEBUG] CaptureIdCardAsync triggered");
        var result = await _cameraService.CaptureAsync(CameraDeviceType.Document, "IdCard");
        if (result.ImageData != null) IdCardImage = LoadBitmapFromBytes(result.ImageData);
        Console.WriteLine($"[DEBUG] CaptureIdCardAsync finished. Success: {result.ImageData != null}");
    }
    
    private async Task CaptureDriverLicenseAsync() 
    { 
        Console.WriteLine("[DEBUG] CaptureDriverLicenseAsync triggered");
        var result = await _cameraService.CaptureAsync(CameraDeviceType.Document, "DriverLicense");
        if (result.ImageData != null) DriverLicenseImage = LoadBitmapFromBytes(result.ImageData);
        Console.WriteLine($"[DEBUG] CaptureDriverLicenseAsync finished. Success: {result.ImageData != null}");
    }
    
    private async Task CapturePhotoAsync() 
    { 
        Console.WriteLine("[DEBUG] CapturePhotoAsync triggered");
        var result = await _cameraService.CaptureAsync(CameraDeviceType.Face, "Photo");
        if (result.ImageData != null) PhotoImage = LoadBitmapFromBytes(result.ImageData);
        Console.WriteLine($"[DEBUG] CapturePhotoAsync finished. Success: {result.ImageData != null}");
    }
    
    private async Task CaptureFingerprintAsync() 
    { 
        Console.WriteLine("[DEBUG] CaptureFingerprintAsync triggered");
        var result = await _app.FingerprintScanner.CaptureAsync();
        if (result.ImageData != null) FingerprintImage = LoadBitmapFromBytes(result.ImageData);
        Console.WriteLine($"[DEBUG] CaptureFingerprintAsync finished. Success: {result.ImageData != null}");
    }

    private byte[]? GetBytesFromBitmap(Bitmap? bitmap)
    {
        if (bitmap == null) return null;
        using var ms = new MemoryStream();
        bitmap.Save(ms);
        return ms.ToArray();
    }
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
    public async Task<bool> ConfirmAsync(string message)
    {
        var dialog = new MetalLink.Desktop.Views.ConfirmDialog(message);
        var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        if (lifetime?.MainWindow != null)
        {
            var result = await dialog.ShowDialog<bool>(lifetime.MainWindow);
            return result;
        }
        return false;
    }

    public async Task<string?> DoFilePickerAsync(string title, string[] filterNames, string[] filterExtensions)
    {
        var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        if (lifetime?.MainWindow == null) return null;

        var filters = new List<FilePickerFileType>();
        for (int i = 0; i < filterNames.Length; i++)
        {
            filters.Add(new FilePickerFileType(filterNames[i])
            {
                Patterns = new List<string> { filterExtensions[i] }
            });
        }

        var options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = filters
        };

        var results = await lifetime.MainWindow.StorageProvider.OpenFilePickerAsync(options);
        return results.FirstOrDefault()?.Path.LocalPath;
    }

    public async Task SetEnforceBuyerCompanyAsync(bool enabled)
    {
        EnforceBuyerCompany = enabled;
        OnPropertyChanged(nameof(EnforceBuyerCompany));
        OnPropertyChanged(nameof(IsNewBuyerOnlyEnabled));
        OnPropertyChanged(nameof(NewIsCompany)); // Trigger getter update
        await Task.CompletedTask;
    }

    private void UpdatePagedCustomerResults()
    {
        PagedCustomerSearchResults.Clear();
        var paged = CustomerSearchResults
            .Skip(PaginationViewModel.GetSkip())
            .Take(PaginationViewModel.GetTake());
        foreach (var c in paged) PagedCustomerSearchResults.Add(c);
    }

    private void UpdatePagedBuyerResults()
    {
        PagedBuyerSearchResults.Clear();
        var paged = BuyerSearchResults
            .Skip(PaginationViewModel.GetSkip())
            .Take(PaginationViewModel.GetTake());
        foreach (var b in paged) PagedBuyerSearchResults.Add(b);
    }
}
