using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using MetalLink.Desktop.Services;
using MetalLink.Desktop.Views;
using MetalLink.Desktop.ViewModels;
using MetalLink.Desktop.Hardware;
using MetalLink.Shared.Products;
using MetalLink.Shared.Tickets.Receiving;

namespace MetalLink.Desktop.ViewModels.Receiving;

using MetalLink.Desktop.ViewModels.Distribution;

/// <summary>
/// Receiving ticket system ViewModel. Must not reference Sending.
/// </summary>
public sealed class TicketsReceivingViewModel : ViewModelBase
{
    private readonly TicketReceivingService _ticketReceivingService;
    private readonly CompanyAndSiteService _companyAndSiteService;

    private readonly IScaleService _scaleService;
    private readonly ProductsAndPricesService _productsAndPricesService;

    public TicketsReceivingViewModel(
        TicketReceivingService ticketReceivingService,
        CompanyAndSiteService companyAndSiteService,
        IScaleService scaleService,
        ProductsAndPricesService productsAndPricesService)
    {
        _ticketReceivingService = ticketReceivingService;
        _companyAndSiteService = companyAndSiteService;
        _scaleService = scaleService;
        _productsAndPricesService = productsAndPricesService;

        RefreshCompaniesCommand = new AsyncCommand(RefreshCompaniesAsync);
        RefreshSitesCommand = new AsyncCommand(RefreshSitesAsync);

        SearchReceivingTicketsCommand = new AsyncCommand(SearchReceivingTicketsAsync);
        ClearReceivingTicketSearchCommand = new RelayCommand(ClearReceivingTicketSearch);
        CreateReceivingTicketHeaderCommand = new AsyncCommand(CreateReceivingTicketHeaderAsync);
        SaveAndResetReceivingTicketCommand = new AsyncCommand(SaveAndResetReceivingTicketAsync);
        FinalizeReceivingTicketCommand = new AsyncCommand(FinalizeReceivingTicketAsync);

        ProductLetterFilters.Add("ALL");
        for (var c = 'A'; c <= 'Z'; c++) ProductLetterFilters.Add(c.ToString());
        SelectedReceivingProductLetter = "ALL";

        // Keep totals in sync when the selected ticket's line collection changes
        SelectedReceivingTicketLines.CollectionChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(SelectedReceivingTicketLinesTotalExVat));
            OnPropertyChanged(nameof(SelectedReceivingTicketLinesTotalVat));
            OnPropertyChanged(nameof(SelectedReceivingTicketLinesTotalInclVat));
            OnPropertyChanged(nameof(CalculatedNetWeightKg));
            OnPropertyChanged(nameof(ReceivingTotalExclVat));
            OnPropertyChanged(nameof(ReceivingTotalVat));
            OnPropertyChanged(nameof(ReceivingTotalInclVat));
            OnPropertyChanged(nameof(ReceivingLinesWithTotals));
            OnPropertyChanged(nameof(CreatingTicketTotalWeight));
        };

        AddReceivingLineToTicketCommand = new AsyncCommand(AddReceivingLineAsync);
        RemoveReceivingLineCommand = new AsyncCommand<ReceivingLineRow?>(RemoveReceivingLineAsync);
        ReadWeighbridgeCommand = new AsyncCommand(ReadWeighbridgeFirstAsync);
        ReadWeighbridgeSecondCommand = new AsyncCommand(ReadWeighbridgeSecondAsync);
        ReadPlatformCommand = new AsyncCommand(ReadPlatformAsync);
        ResetWeighbridgeWeightsCommand = new RelayCommand(ResetWeighbridgeWeights);
        ResetPlatformWeightCommand = new RelayCommand(ResetPlatformWeight);
        ShowLineNotesCommand = new RelayCommand<string>(notes =>
        {
            SelectedLineNotesContent = notes ?? string.Empty;
            IsNotesModalVisible = true;
        });
        CloseLineNotesCommand = new RelayCommand(() => { IsNotesModalVisible = false; SelectedLineNotesContent = string.Empty; });

        OpenDistributionCommand = new RelayCommand(OpenDistribution);
        DownloadTicketReportCommand = new AsyncCommand(() => Task.CompletedTask);
    }

    // ============================================================
    // Status / Busy
    // ============================================================

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    // ============================================================
    // Company/Site selectors (Receiving-only)
    // ============================================================

    public ObservableCollection<MetalLink.Shared.Companies.CompanyLookupDto> SearchCompanySuggestions { get; } = new();
    public ObservableCollection<MetalLink.Shared.Sites.SiteLookupDto> SearchSiteSuggestions { get; } = new();

    private MetalLink.Shared.Companies.CompanyLookupDto? _selectedSearchCompany;
    public MetalLink.Shared.Companies.CompanyLookupDto? SelectedSearchCompany
    {
        get => _selectedSearchCompany;
        set
        {
            _selectedSearchCompany = value;
            OnPropertyChanged();
            _ = RefreshSitesAsync();
        }
    }

    private MetalLink.Shared.Sites.SiteLookupDto? _selectedSearchSite;
    public MetalLink.Shared.Sites.SiteLookupDto? SelectedSearchSite
    {
        get => _selectedSearchSite;
        set { _selectedSearchSite = value; OnPropertyChanged(); }
    }

    public ICommand RefreshCompaniesCommand { get; }
    public ICommand RefreshSitesCommand { get; }

    private async Task RefreshCompaniesAsync()
    {
        var items = await _companyAndSiteService.LookupCompaniesAsync(null);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            SearchCompanySuggestions.Clear();
            foreach (var c in items) SearchCompanySuggestions.Add(c);

            // Do not auto-select a company on load; user must choose.
            SelectedSearchCompany = null;
            SearchSiteSuggestions.Clear();
            SelectedSearchSite = null;
        });
    }

    private async Task RefreshSitesAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            SearchSiteSuggestions.Clear();
        });

        if (SelectedSearchCompany == null)
        {
            SelectedSearchSite = null;
            return;
        }

        var sites = await _companyAndSiteService.LookupSitesForCompanyAsync(SelectedSearchCompany.CompanyId, term: "");

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            foreach (var s in sites ?? new()) SearchSiteSuggestions.Add(s);

            // Do not auto-select a site on load; user must choose.
            SelectedSearchSite = null;
        });
    }

    public async Task InitializeAsync()
    {
        await RefreshCompaniesAsync();
    }

    public void ResetPlatformWeight()
    {
        TicketPlatformWeightText = "0";
        ReceivingWeightText = string.Empty;
        StatusMessage = "Platform weight reset.";
    }

    public async Task SaveTicketAsync()
    {
        StatusMessage = "Ticket saved.";
        await Task.CompletedTask;
    }

    public async Task ClearTicketAsync()
    {
        ResetCreateSectionForMode();
        StatusMessage = "Ticket cleared.";
        await Task.CompletedTask;
    }

    public async Task CaptureWeightAsync()
    {
        if (AreWeighbridgeFieldsVisible)
        {
            if (IsFirstWeightEnabled) await ReadWeighbridgeFirstAsync();
            else if (IsSecondWeightEnabled) await ReadWeighbridgeSecondAsync();
        }
        else if (ArePlatformFieldsVisible)
        {
            await ReadPlatformAsync();
        }
    }

    public async Task CapturePlatePhotoAsync()
    {
        StatusMessage = "Plate photo captured (simulated).";
        await Task.CompletedTask;
    }

    public async Task CaptureLoadPhotoAsync()
    {
        StatusMessage = "Load photo captured (simulated).";
        await Task.CompletedTask;
    }

    public async Task PrintReceivingTicketAsync()
    {
        StatusMessage = "Printing receiving ticket...";
        await Task.CompletedTask;
    }

    public void ShowLineNotes(string notes)
    {
        SelectedLineNotesContent = notes ?? string.Empty;
        IsNotesModalVisible = true;
        OnPropertyChanged(nameof(IsNotesModalVisible));
        OnPropertyChanged(nameof(SelectedLineNotesContent));
    }

    public void CloseLineNotes()
    {
        IsNotesModalVisible = false;
        SelectedLineNotesContent = string.Empty;
        OnPropertyChanged(nameof(IsNotesModalVisible));
        OnPropertyChanged(nameof(SelectedLineNotesContent));
    }

    public void ScrollToAddLines()
    {
        // UI-only action stub
    }

    public async Task CreateTicketHeaderAsync() => await CreateReceivingTicketHeaderAsync();

    public async Task ReadWeighbridgeAsync() => await ReadWeighbridgeFirstAsync();

    // ============================================================
    // Search (Receiving-only)
    // ============================================================

    public ObservableCollection<TicketReceivingSearchResultDto> ReceivingTicketSearchResults { get; } = new();

    private long _receivingDetailsLoadVersion;

    private TicketReceivingSearchResultDto? _selectedReceivingTicket;
    public TicketReceivingSearchResultDto? SelectedReceivingTicket
    {
        get => _selectedReceivingTicket;
        set
        {
            _selectedReceivingTicket = value;
            OnPropertyChanged();

            _receivingDetailsLoadVersion++;
            var version = _receivingDetailsLoadVersion;

            // Always populate Create client label from selected row
            TicketCustomerIdText = value == null ? string.Empty : FormatClientLabel(value.CustomerId, value.FirstName, value.LastName, value.CompanyName, value.SiteName);

            if (value != null && !IsNewCustomerOnly && value.TicketId > 0)
                _ = LoadSelectedReceivingTicketDetailsAsync(value.TicketId, version);
            else
            {
                // New-customer mode (or no selection): no ticket details
                SelectedReceivingTicketDetails = null;
                SelectedReceivingTicketLines.Clear();

                // Notes must only be populated for WIP tickets under Create.
                TicketNotes = null;

                CurrentTicketState = 'C';

                // In "new ticket" flow, ticket type must be editable.
                IsTicketTypeEnabled = true;
                OnPropertyChanged(nameof(IsTicketTypeEnabled));

                // Defaults for new create in Receiving: Platform
                SelectedTicketTypeOption = TicketTypeOptions.FirstOrDefault(o => o.Key == "platform");

                OnPropertyChanged(nameof(ReceivingLinesWithTotals));
                OnPropertyChanged(nameof(CreatingTicketTotalWeight));
            }
        }
    }

    private string _searchReceivingTicketNumberText = string.Empty;
    public string SearchReceivingTicketNumberText { get => _searchReceivingTicketNumberText; set { _searchReceivingTicketNumberText = value; OnPropertyChanged(); } }

    private string _searchReceivingTicketCustomerIdText = string.Empty;
    public string SearchReceivingTicketCustomerIdText { get => _searchReceivingTicketCustomerIdText; set { _searchReceivingTicketCustomerIdText = value; OnPropertyChanged(); } }

    private string _searchReceivingTicketIdNumberText = string.Empty;
    public string SearchReceivingTicketIdNumberText { get => _searchReceivingTicketIdNumberText; set { _searchReceivingTicketIdNumberText = value; OnPropertyChanged(); } }

    private string _searchReceivingTicketFirstNameText = string.Empty;
    public string SearchReceivingTicketFirstNameText { get => _searchReceivingTicketFirstNameText; set { _searchReceivingTicketFirstNameText = value; OnPropertyChanged(); } }

    private string _searchReceivingTicketLastNameText = string.Empty;
    public string SearchReceivingTicketLastNameText { get => _searchReceivingTicketLastNameText; set { _searchReceivingTicketLastNameText = value; OnPropertyChanged(); } }

    private string _searchReceivingTicketAccountNumberText = string.Empty;
    public string SearchReceivingTicketAccountNumberText { get => _searchReceivingTicketAccountNumberText; set { _searchReceivingTicketAccountNumberText = value; OnPropertyChanged(); } }

    // Ticket type filter (Receiving-only)
    public sealed record ReceivingTicketTypeOption(string Key, string Display);

    public ObservableCollection<ReceivingTicketTypeOption> SearchTicketTypeOptions { get; } = new()
    {
        new ReceivingTicketTypeOption("all", "All"),
        new ReceivingTicketTypeOption("weighbridge", "Weighbridge"),
        new ReceivingTicketTypeOption("platform", "Platform")
    };

    private ReceivingTicketTypeOption? _selectedSearchReceivingTicketTypeOption;
    public ReceivingTicketTypeOption? SelectedSearchReceivingTicketTypeOption
    {
        get => _selectedSearchReceivingTicketTypeOption;
        set { _selectedSearchReceivingTicketTypeOption = value; OnPropertyChanged(); }
    }

    public ICommand SearchReceivingTicketsCommand { get; }
    public ICommand ClearReceivingTicketSearchCommand { get; }

    private async Task SearchReceivingTicketsAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            StatusMessage = "Searching receiving tickets...";

            var ticketTypeKey = SelectedSearchReceivingTicketTypeOption?.Key;
            var ticketType = string.IsNullOrWhiteSpace(ticketTypeKey) || ticketTypeKey == "all" ? null : ticketTypeKey;

            var req = new TicketReceivingSearchRequestDto
            {
                NewCustomerOnly = IsNewCustomerOnly,
                TicketType = ticketType,
                CompanyId = SelectedSearchCompany?.CompanyId,
                SiteId = SelectedSearchSite?.SiteId,
                CustomerId = int.TryParse(SearchReceivingTicketCustomerIdText, out var cid) ? cid : null,
                FirstName = string.IsNullOrWhiteSpace(SearchReceivingTicketFirstNameText) ? null : SearchReceivingTicketFirstNameText.Trim(),
                LastName = string.IsNullOrWhiteSpace(SearchReceivingTicketLastNameText) ? null : SearchReceivingTicketLastNameText.Trim(),
                IdNumber = string.IsNullOrWhiteSpace(SearchReceivingTicketIdNumberText) ? null : SearchReceivingTicketIdNumberText.Trim(),
                AccountNumber = long.TryParse(SearchReceivingTicketAccountNumberText, out var acc) ? acc : null,
                SearchTerm = string.IsNullOrWhiteSpace(SearchReceivingTicketNumberText) ? null : SearchReceivingTicketNumberText.Trim(),
                PageNumber = 1,
                PageSize = 50
            };

            var results = await _ticketReceivingService.SearchTicketsReceivingAsync(req);

            ReceivingTicketSearchResults.Clear();
            foreach (var r in results) ReceivingTicketSearchResults.Add(r);

            SelectedReceivingTicket = ReceivingTicketSearchResults.FirstOrDefault();
            SelectedReceivingTicketDetails = null;
            SelectedReceivingTicketLines.Clear();
            OnPropertyChanged(nameof(ShouldShowTicketDetails));
            StatusMessage = $"Loaded {ReceivingTicketSearchResults.Count} receiving ticket(s).";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ClearReceivingTicketSearch()
    {
        SearchReceivingTicketNumberText = string.Empty;
        SearchReceivingTicketCustomerIdText = string.Empty;
        SearchReceivingTicketIdNumberText = string.Empty;
        SearchReceivingTicketFirstNameText = string.Empty;
        SearchReceivingTicketLastNameText = string.Empty;
        SearchReceivingTicketAccountNumberText = string.Empty;
        SelectedReceivingTicket = null;
        ReceivingTicketSearchResults.Clear();
        SelectedSearchReceivingTicketTypeOption = SearchTicketTypeOptions.FirstOrDefault();
        StatusMessage = "Receiving search cleared.";
    }

    // ============================================================
    // Details panel (Receiving-only)
    // ============================================================

    private TicketReceivingDto? _selectedReceivingTicketDetails;
    public TicketReceivingDto? SelectedReceivingTicketDetails
    {
        get => _selectedReceivingTicketDetails;
        set { _selectedReceivingTicketDetails = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShouldShowTicketDetails)); }
    }

    public ObservableCollection<TicketReceivingLineDto> SelectedReceivingTicketLines { get; } = new();
    public ObservableCollection<TicketReceivingLineDto> SelectedReceivingTicketLinesWithTotals => SelectedReceivingTicketLines;

    private bool _isNewCustomerOnly;
    public bool IsNewCustomerOnly
    {
        get => _isNewCustomerOnly;
        set
        {
            _isNewCustomerOnly = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShouldShowTicketDetails));
            OnPropertyChanged(nameof(ShouldShowTicketColumns));
            OnPropertyChanged(nameof(ResultsMainColumnWidth));
            OnPropertyChanged(nameof(ResultsSpacerColumnWidth));

            // Clear results when switching modes
            SelectedReceivingTicket = null;
            ReceivingTicketSearchResults.Clear();

            ResetCreateSectionForMode();
        }
    }

    public bool ShouldShowTicketDetails => !IsNewCustomerOnly && SelectedReceivingTicketDetails != null;

    public bool ShouldShowTicketColumns => !IsNewCustomerOnly;

    // Results width: when New Customer? is active, show results at ~60% width.
    public GridLength ResultsMainColumnWidth => IsNewCustomerOnly ? new GridLength(3, GridUnitType.Star) : new GridLength(1, GridUnitType.Star);
    public GridLength ResultsSpacerColumnWidth => IsNewCustomerOnly ? new GridLength(2, GridUnitType.Star) : new GridLength(0, GridUnitType.Pixel);

    private static string FormatClientLabel(long id, string? first, string? last, string? company, string? site)
    {
        var name = $"{first} {last}".Trim();
        var basePart = $"{id} | {name}";

        if (!string.IsNullOrWhiteSpace(company) || !string.IsNullOrWhiteSpace(site))
            basePart += $" | {company} {site}".Trim();

        return basePart.Trim().TrimEnd('|').Trim();
    }

    private void ResetCreateSectionForMode()
    {
        // Clear ticket-specific create state
        TicketNumber = string.Empty;
        TicketFirstWeightText = "0.00";
        TicketSecondWeightText = "0.00";
        TicketPlatformWeightText = "0.00";
        ReceivingWeightText = string.Empty;

        TicketVehicleRegistration = null;
        TicketTrailerRegistration = null;
        TicketDriverName = null;
        TicketOfmWeighbridgeTicket = null;
        TicketDeliveryNumber = null;
        TicketForeignTicket = null;
        TicketCkNumber = null;
        TicketNotes = null;

        ReceivingSelectedProduct = null;
        ReceivingProductSearchText = string.Empty;
        ReceivingLineNotes = string.Empty;

        CurrentTicketState = 'C';

        // In "new ticket" flow, ticket type must be editable.
        IsTicketTypeEnabled = true;
        OnPropertyChanged(nameof(IsTicketTypeEnabled));

        // Default ticket type for new create in Receiving is Platform
        SelectedTicketTypeOption = TicketTypeOptions.FirstOrDefault(o => o.Key == "platform");

        // Re-populate client label from selected row if available
        if (SelectedReceivingTicket != null)
            TicketCustomerIdText = FormatClientLabel(SelectedReceivingTicket.CustomerId, SelectedReceivingTicket.FirstName, SelectedReceivingTicket.LastName, SelectedReceivingTicket.CompanyName, SelectedReceivingTicket.SiteName);

        _ = RegenerateTicketNumberForCreateAsync();
    }

    private void SetCreateTicketTypeFromTicketTypeId(int ticketTypeId)
    {
        // Defensive mapping: 1 = weighbridge, 2 = platform
        var key = ticketTypeId == 1 ? "weighbridge" : "platform";
        SelectedTicketTypeOption = TicketTypeOptions.FirstOrDefault(o => o.Key == key) ?? TicketTypeOptions.FirstOrDefault();
    }

    public async Task LoadSelectedReceivingTicketDetailsAsync(long ticketId)
        => await LoadSelectedReceivingTicketDetailsAsync(ticketId, _receivingDetailsLoadVersion);

    private async Task LoadSelectedReceivingTicketDetailsAsync(long ticketId, long version)
    {
        var details = await _ticketReceivingService.GetTicketReceivingByIdAsync(ticketId);

        // Prevent stale async loads from overwriting newer selections
        if (version != _receivingDetailsLoadVersion)
            return;

        if (details == null)
        {
            SelectedReceivingTicketDetails = null;
            SelectedReceivingTicketLines.Clear();
            OnPropertyChanged(nameof(ReceivingLinesWithTotals));
            OnPropertyChanged(nameof(CreatingTicketTotalWeight));
            return;
        }

        SelectedReceivingTicketDetails = details;
        OnPropertyChanged(nameof(CreatingTicketTotalWeight));
        SelectedReceivingTicketLines.Clear();
        foreach (var l in details.Lines.Where(l => l.IsActive)) SelectedReceivingTicketLines.Add(l);

        // Ensure Create panel ticket type matches the selected ticket.
        // This is especially important when editing an in-progress ticket.
        SetCreateTicketTypeFromTicketTypeId(details.TicketTypeId);

        // Populate Create panel header fields from the selected ticket.
        // (For completed tickets, this acts as a "copy-from" convenience.)
        TicketVehicleRegistration = details.VehicleRegistration;
        TicketTrailerRegistration = details.TrailerRegistration;
        TicketDriverName = details.DriverName;
        TicketOfmWeighbridgeTicket = details.OfmWeighbridgeTicket;
        TicketDeliveryNumber = details.DeliveryNumber;
        TicketForeignTicket = details.ForeignTicket;
        TicketCkNumber = details.CkNumber;

        // Notes must only be populated for WIP tickets under Create.
        TicketNotes = (details.TicketState == 'H' || details.TicketState == 'M') ? details.Notes : null;

        // Populate Create panel depending on state
        if (details.TicketState == 'H' || details.TicketState == 'M')
        {
            // edit in-progress
            TicketNumber = details.TicketNumber;
            // Keep formatted display ("id | first last | company site") from SelectedReceivingTicket
            CurrentTicketState = details.TicketState;

            // Weighbridge workflow values
            if (details.TicketTypeId == 1)
            {
                TicketFirstWeightText = (details.InitializeWeightKg ?? 0m).ToString("0.00", CultureInfo.InvariantCulture);
                TicketSecondWeightText = "0.00";
                ReceivingWeightText = string.Empty;
            }

            // Ticket type should not change while editing an in-progress ticket.
            IsTicketTypeEnabled = false;
        }
        else
        {
            // complete selected: clone header fields but new ticket number/weights reset
            // Keep formatted display ("id | first last | company site") from SelectedReceivingTicket
            CurrentTicketState = 'C';
            TicketFirstWeightText = "0.00";
            TicketSecondWeightText = "0";
            TicketPlatformWeightText = "0";

            // Selecting a completed ticket means we are creating a NEW ticket based on it.
            // Use the non-consuming peek endpoint so the ticket number shown is always the next number.
            TicketNumber = await _ticketReceivingService.GenerateTicketNumberAsync(details.TicketTypeId == 1 ? "RWB" : "RPL");

            // When creating a new ticket (even if based on a completed ticket), allow choosing ticket type.
            IsTicketTypeEnabled = true;
        }

        OnPropertyChanged(nameof(CreateTicketModeText));
        OnPropertyChanged(nameof(IsCreateTicketModeVisible));
        OnPropertyChanged(nameof(CreateHeaderButtonVisible));
        OnPropertyChanged(nameof(SaveResetButtonVisible));
        OnPropertyChanged(nameof(IsFinalizeTicketEnabled));
        OnPropertyChanged(nameof(AddLineButtonEnabled));
        OnPropertyChanged(nameof(IsTicketTypeEnabled));
        OnPropertyChanged(nameof(ReceivingLinesWithTotals));

        // Ensure product dropdown is populated without requiring typing
        _ = RefreshReceivingProductSuggestionsAsync();
    }

    // Totals
    public decimal SelectedReceivingTicketLinesTotalVat => SelectedReceivingTicketLines.Where(l => l.IsActive).Sum(l => l.VatAmount);
    public decimal SelectedReceivingTicketLinesTotalInclVat => SelectedReceivingTicketLines.Where(l => l.IsActive).Sum(l => l.TotalInclVat);
    public decimal SelectedReceivingTicketLinesTotalExVat => SelectedReceivingTicketLines.Where(l => l.IsActive).Sum(l => l.LineTotal);

    // ============================================================
    // Create/Edit panel (Receiving-only)
    // ============================================================

    private char _currentTicketState = 'C';
    public char CurrentTicketState
    {
        get => _currentTicketState;
        set
        {
            _currentTicketState = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CreateTicketModeText));
            OnPropertyChanged(nameof(IsCreateTicketModeVisible));

            // Button/section enablement depends on state
            OnPropertyChanged(nameof(CreateHeaderButtonVisible));
            OnPropertyChanged(nameof(SaveResetButtonVisible));
            OnPropertyChanged(nameof(AddLineButtonEnabled));
            OnPropertyChanged(nameof(IsFinalizeTicketEnabled));

            // Weight capture controls depend on state
            OnPropertyChanged(nameof(IsFirstWeightEnabled));
            OnPropertyChanged(nameof(IsSecondWeightEnabled));
            OnPropertyChanged(nameof(AreFirstWeightButtonsEnabled));
            OnPropertyChanged(nameof(AreSecondWeightButtonsEnabled));

            // Totals/grids depend on state
            OnPropertyChanged(nameof(ReceivingLinesWithTotals));
            OnPropertyChanged(nameof(CreatingTicketTotalWeight));
        }
    }

    public string CreateTicketModeText =>
        CurrentTicketState is 'H' or 'M'
            ? $"Editing in-progress ticket: {TicketNumber}"
            : (SelectedReceivingTicketDetails?.TicketState == 'C' ? "New ticket (based on selected completed ticket)" : "New ticket");

    public bool IsCreateTicketModeVisible => !string.IsNullOrWhiteSpace(CreateTicketModeText);

    public bool CreateHeaderButtonVisible => CurrentTicketState == 'C';
    public bool SaveResetButtonVisible => CurrentTicketState == 'H' || CurrentTicketState == 'M';
    public bool AddLineButtonEnabled => CurrentTicketState != 'C';
    public bool IsFinalizeTicketEnabled => CurrentTicketState == 'H' || CurrentTicketState == 'M';

    private bool IsWeighbridgeMode => SelectedTicketTypeOption?.Key == "weighbridge";

    // Strict UI enforcement for weighbridge weights
    public bool IsFirstWeightEnabled => IsWeighbridgeMode && CurrentTicketState == 'C';
    public bool IsSecondWeightEnabled => IsWeighbridgeMode && (CurrentTicketState == 'H' || CurrentTicketState == 'M');

    // Backwards-compatible aliases used by existing XAML button bindings
    public bool AreFirstWeightButtonsEnabled => IsFirstWeightEnabled;
    public bool AreSecondWeightButtonsEnabled => IsSecondWeightEnabled;

    private string _ticketCustomerIdText = string.Empty;
    public string TicketCustomerIdText { get => _ticketCustomerIdText; set { _ticketCustomerIdText = value; OnPropertyChanged(); } }

    private string _ticketNumber = string.Empty;
    public string TicketNumber { get => _ticketNumber; set { _ticketNumber = value; OnPropertyChanged(); } }

    public bool AreWeighbridgeFieldsVisible => SelectedTicketTypeOption?.Key == "weighbridge";
    public bool ArePlatformFieldsVisible => SelectedTicketTypeOption?.Key == "platform";

    public sealed record ReceivingCreateTicketTypeOption(string Key, string Display);

    private ObservableCollection<ReceivingCreateTicketTypeOption>? _ticketTypeOptions;
    public ObservableCollection<ReceivingCreateTicketTypeOption> TicketTypeOptions
        => _ticketTypeOptions ??= new ObservableCollection<ReceivingCreateTicketTypeOption>(
            new[]
            {
                new ReceivingCreateTicketTypeOption("weighbridge", "Weighbridge"),
                new ReceivingCreateTicketTypeOption("platform", "Platform")
            });

    private ReceivingCreateTicketTypeOption? _selectedTicketTypeOption;
    public ReceivingCreateTicketTypeOption? SelectedTicketTypeOption
    {
        get => _selectedTicketTypeOption;
        set
        {
            _selectedTicketTypeOption = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AreWeighbridgeFieldsVisible));
            OnPropertyChanged(nameof(ArePlatformFieldsVisible));
            OnPropertyChanged(nameof(IsFirstWeightEnabled));
            OnPropertyChanged(nameof(IsSecondWeightEnabled));
            OnPropertyChanged(nameof(AreFirstWeightButtonsEnabled));
            OnPropertyChanged(nameof(AreSecondWeightButtonsEnabled));

            if (CurrentTicketState == 'C')
                _ = RegenerateTicketNumberForCreateAsync();
        }
    }

    private bool _isRegeneratingTicketNumber;

    private async Task RegenerateTicketNumberForCreateAsync()
    {
        if (_isRegeneratingTicketNumber) return;
        if (CurrentTicketState != 'C') return;
        if (SelectedTicketTypeOption == null) return;

        try
        {
            _isRegeneratingTicketNumber = true;
            var prefix = SelectedTicketTypeOption.Key == "weighbridge" ? "RWB" : "RPL";
            var tn = await _ticketReceivingService.GenerateTicketNumberAsync(prefix);

            // If state changed while awaiting (e.g. we started loading an in-progress ticket), don't overwrite.
            if (CurrentTicketState != 'C')
                return;

            TicketNumber = tn;
        }
        finally
        {
            _isRegeneratingTicketNumber = false;
        }
    }

    private string _ticketFirstWeightText = "0";
    public string TicketFirstWeightText { get => _ticketFirstWeightText; set { _ticketFirstWeightText = value; OnPropertyChanged(); } }

    private string _ticketSecondWeightText = "0";
    public string TicketSecondWeightText { get => _ticketSecondWeightText; set { _ticketSecondWeightText = value; OnPropertyChanged(); } }

    private string _ticketPlatformWeightText = "0";
    public string TicketPlatformWeightText { get => _ticketPlatformWeightText; set { _ticketPlatformWeightText = value; OnPropertyChanged(); } }

    private string _receivingWeightText = string.Empty;
    public string ReceivingWeightText { get => _receivingWeightText; set { _receivingWeightText = value; OnPropertyChanged(); } }

    private string? _ticketVehicleRegistration;
    public string? TicketVehicleRegistration { get => _ticketVehicleRegistration; set { _ticketVehicleRegistration = value; OnPropertyChanged(); } }

    private string? _ticketTrailerRegistration;
    public string? TicketTrailerRegistration { get => _ticketTrailerRegistration; set { _ticketTrailerRegistration = value; OnPropertyChanged(); } }

    private string? _ticketDriverName;
    public string? TicketDriverName { get => _ticketDriverName; set { _ticketDriverName = value; OnPropertyChanged(); } }

    private string? _ticketOfmWeighbridgeTicket;
    public string? TicketOfmWeighbridgeTicket { get => _ticketOfmWeighbridgeTicket; set { _ticketOfmWeighbridgeTicket = value; OnPropertyChanged(); } }

    private string? _ticketForeignTicket;
    public string? TicketForeignTicket { get => _ticketForeignTicket; set { _ticketForeignTicket = value; OnPropertyChanged(); } }

    private string? _ticketCkNumber;
    public string? TicketCkNumber { get => _ticketCkNumber; set { _ticketCkNumber = value; OnPropertyChanged(); } }

    private string? _ticketDeliveryNumber;
    public string? TicketDeliveryNumber { get => _ticketDeliveryNumber; set { _ticketDeliveryNumber = value; OnPropertyChanged(); } }

    private string? _ticketNotes;
    public string? TicketNotes { get => _ticketNotes; set { _ticketNotes = value; OnPropertyChanged(); } }

    public ICommand CreateReceivingTicketHeaderCommand { get; }
    public ICommand SaveAndResetReceivingTicketCommand { get; }
    public ICommand FinalizeReceivingTicketCommand { get; }

    private long ExtractCustomerIdFromText(string text)
        => long.TryParse(text.Split('|')[0].Trim(), out var v) ? v : 0;

    private static string NormalizeDecimalText(string text)
        => (text ?? string.Empty).Replace(',', '.').Trim();

    private async Task CreateReceivingTicketHeaderAsync()
    {
        if (IsBusy) return;

        // Remember if we are creating from "New Customer?" mode so we can reset UI correctly afterwards.
        var wasNewCustomerMode = IsNewCustomerOnly;
        var selectedRowBeforeCreate = SelectedReceivingTicket;

        var customerId = ExtractCustomerIdFromText(TicketCustomerIdText);
        if (customerId <= 0)
        {
            StatusMessage = "Please enter/select a valid Customer ID.";
            return;
        }

        int ticketTypeId = SelectedTicketTypeOption?.Key == "weighbridge" ? 1 : 2;
        var prefix = ticketTypeId == 1 ? "RWB" : "RPL";

        IsBusy = true;
        try
        {
            decimal? initWeight = null;
            if (ticketTypeId == 1 && decimal.TryParse(NormalizeDecimalText(TicketFirstWeightText), NumberStyles.Any, CultureInfo.InvariantCulture, out var fw))
                initWeight = fw;

            if (ticketTypeId == 1 && (!initWeight.HasValue || initWeight.Value <= 0m))
            {
                StatusMessage = "First Weight must be captured before creating header.";
                return;
            }

            var ticketNumber = await _ticketReceivingService.GenerateTicketNumberAsync(prefix);
            TicketNumber = ticketNumber;

            var dto = new CreateTicketReceivingDto
            { 
                CustomerId = (int)customerId,
                TicketTypeId = ticketTypeId,
                TicketNumber = ticketNumber,
                TicketState = 'H',
                InitializeWeightKg = initWeight,
                NetWeightKg = 0m,
                VehicleRegistration = TicketVehicleRegistration,
                TrailerRegistration = TicketTrailerRegistration,
                DriverName = TicketDriverName,
                OfmWeighbridgeTicket = TicketOfmWeighbridgeTicket,
                ForeignTicket = TicketForeignTicket,
                CkNumber = TicketCkNumber,
                DeliveryNumber = TicketDeliveryNumber,
                Notes = TicketNotes,
                CreatedByOperatorId = 1
            };

            var created = await _ticketReceivingService.CreateTicketReceivingAsync(dto);
            if (created == null)
            {
                StatusMessage = "Failed to create ticket header.";
                return;
            }

            if (wasNewCustomerMode)
            {
                // Requirement: if New Customer? was used, treat as creating a brand new ticket:
                // - untick the checkbox
                // - clear results
                // - add the newly created ticket row and select it
                IsNewCustomerOnly = false;

                ReceivingTicketSearchResults.Clear();

                var newRow = new TicketReceivingSearchResultDto
                {
                    TicketId = created.TicketReceivingId,
                    TicketNumber = created.TicketNumber,
                    TicketType = created.TicketTypeName,
                    TicketTypeId = created.TicketTypeId,
                    CustomerId = created.CustomerId,
                    FirstName = selectedRowBeforeCreate?.FirstName,
                    LastName = selectedRowBeforeCreate?.LastName,
                    CompanyName = selectedRowBeforeCreate?.CompanyName,
                    SiteName = selectedRowBeforeCreate?.SiteName,
                    AccountNumber = selectedRowBeforeCreate?.AccountNumber,
                    NetWeightKg = created.NetWeightKg,
                    TicketStatus = created.TicketState,
                    CreatedTime = created.CreatedTime
                };

                ReceivingTicketSearchResults.Add(newRow);
                SelectedReceivingTicket = newRow;

                // Keep details pane and create pane in sync
                SelectedReceivingTicketDetails = created;
                SelectedReceivingTicketLines.Clear();
                foreach (var l in created.Lines.Where(l => l.IsActive)) SelectedReceivingTicketLines.Add(l);

                CurrentTicketState = created.TicketState;
                StatusMessage = $"Header created: {created.TicketNumber}";
                return;
            }

            // Normal mode: refresh results from server.
            CurrentTicketState = created.TicketState;
            await SearchReceivingTicketsAsync();

            // Ensure the newly created ticket is selected.
            SelectedReceivingTicket = ReceivingTicketSearchResults.FirstOrDefault(r => r.TicketId == created.TicketReceivingId)
                                     ?? ReceivingTicketSearchResults.FirstOrDefault();

            await LoadSelectedReceivingTicketDetailsAsync(created.TicketReceivingId);
            StatusMessage = $"Header created: {created.TicketNumber}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task SaveAndResetReceivingTicketAsync()
    {
        // For now: just reset create panel
        TicketNumber = string.Empty;
        TicketFirstWeightText = "0.00";
        TicketSecondWeightText = "0";
        TicketPlatformWeightText = "0";
        CurrentTicketState = 'C';

        // Reset ticket type selection back to a sensible default and re-enable changing it.
        SelectedTicketTypeOption ??= TicketTypeOptions.FirstOrDefault();
        IsTicketTypeEnabled = true;
        OnPropertyChanged(nameof(IsTicketTypeEnabled));

        StatusMessage = "Ticket reset.";
        await Task.CompletedTask;
    }

    private void MoveReceivingTicketToTop(long ticketId)
    {
        var item = ReceivingTicketSearchResults.FirstOrDefault(r => r.TicketId == ticketId);
        if (item == null) return;

        var idx = ReceivingTicketSearchResults.IndexOf(item);
        if (idx > 0)
            ReceivingTicketSearchResults.Move(idx, 0);
    }

    private async Task<bool> ConfirmAsync(string message)
    {
        var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        var owner = lifetime?.MainWindow;
        if (owner == null)
            return true;

        var dlg = new ConfirmDialog(message);
        var result = await dlg.ShowDialog<bool>(owner);
        return result;
    }

    public async Task FinalizeReceivingTicketAsync()
    {
        if (SelectedReceivingTicketDetails == null) return;

        var ok = await ConfirmAsync("Finalize this ticket?\n\nThis will mark it as complete.");
        if (!ok)
        {
            StatusMessage = "Finalize cancelled.";
            return;
        }

        var ticketId = SelectedReceivingTicketDetails.TicketReceivingId;
        await _ticketReceivingService.UpdateTicketStateAsync(ticketId, 'C');

        // Reload details (NetWeightKg, state etc.)
        await LoadSelectedReceivingTicketDetailsAsync(ticketId);

        // Requirement: update the existing row in the Results grid (Net Weight + Status)
        var idx = ReceivingTicketSearchResults.ToList().FindIndex(r => r.TicketId == ticketId);
        if (idx >= 0)
        {
            var existing = ReceivingTicketSearchResults[idx];
            var updatedRow = new TicketReceivingSearchResultDto
            {
                TicketId = existing.TicketId,
                TicketNumber = existing.TicketNumber,
                TicketType = existing.TicketType,
                TicketTypeId = existing.TicketTypeId,
                CustomerId = existing.CustomerId,
                FirstName = existing.FirstName,
                LastName = existing.LastName,
                CompanyName = existing.CompanyName,
                SiteName = existing.SiteName,
                AccountNumber = existing.AccountNumber,
                NetWeightKg = SelectedReceivingTicketDetails?.NetWeightKg ?? existing.NetWeightKg,
                TicketStatus = SelectedReceivingTicketDetails?.TicketState ?? 'C',
                CreatedTime = existing.CreatedTime
            };

            ReceivingTicketSearchResults[idx] = updatedRow;
            SelectedReceivingTicket = updatedRow;
        }

        StatusMessage = "Ticket finalized.";
    }

    // ============================================================
    // Misc UI bindings (stubs to be implemented later)
    // ============================================================

    public ICommand AddReceivingLineToTicketCommand { get; }
    public ICommand RemoveReceivingLineCommand { get; }
    public ICommand ReadWeighbridgeCommand { get; }
    public ICommand ReadWeighbridgeSecondCommand { get; }
    public ICommand ReadPlatformCommand { get; }
    public ICommand ResetWeighbridgeWeightsCommand { get; }
    public ICommand ResetPlatformWeightCommand { get; }

    public ICommand ShowLineNotesCommand { get; }
    public ICommand CloseLineNotesCommand { get; }

    public ICommand OpenDistributionCommand { get; }
    public bool IsNotesModalVisible { get; set; }
    public string SelectedLineNotesContent { get; set; } = string.Empty;
    public string ReceivingLineNotes { get; set; } = string.Empty;

    public bool IsEditingTicketLine { get; set; }
    public bool IsEditable { get; set; }
    public bool IsTicketTypeEnabled { get; set; } = true;

    public string TicketReportTicketIdText { get; set; } = string.Empty;
    public string LastTicketReportPath { get; set; } = string.Empty;
    public ICommand DownloadTicketReportCommand { get; }

    // These are referenced in XAML but will be implemented during line editing migration
    public string EditLineProductSearchText { get; set; } = string.Empty;
    public ObservableCollection<string> EditLineProductSuggestions { get; } = new();
    public object? EditLineSelectedProduct { get; set; }
    public string EditLineSelectedProductLetter { get; set; } = string.Empty;
    public string EditLineWeightText { get; set; } = string.Empty;

    private string _receivingProductSearchText = string.Empty;
    public string ReceivingProductSearchText
    {
        get => _receivingProductSearchText;
        set
        {
            _receivingProductSearchText = value;
            OnPropertyChanged();
            _ = RefreshReceivingProductSuggestionsAsync();
        }
    }

    public ObservableCollection<ProductLookupDto> ReceivingProductSuggestions { get; } = new();

    private ProductLookupDto? _receivingSelectedProduct;
    public ProductLookupDto? ReceivingSelectedProduct { get => _receivingSelectedProduct; set { _receivingSelectedProduct = value; OnPropertyChanged(); } }
    private string _selectedReceivingProductLetter = "ALL";
    public string SelectedReceivingProductLetter
    {
        get => _selectedReceivingProductLetter;
        set
        {
            _selectedReceivingProductLetter = value;
            OnPropertyChanged();
            _ = RefreshReceivingProductSuggestionsAsync();
        }
    }

    public ObservableCollection<string> ProductLetterFilters { get; } = new();
    public ObservableCollection<object> CompanyLetterFilters { get; } = new();

    // Create-panel line grid: show current in-progress ticket lines so the operator can see what's already added.
    // For state C (new/uncreated), keep it empty.
    public sealed record ReceivingLineRow(TicketReceivingLineDto Line, bool IsLastLine)
    {
        public int ReceivingTicketLineId => Line.ReceivingTicketLineId;
        public string ProductName => Line.ProductName;
        public decimal? FirstWeightKg => Line.FirstWeightKg;
        public decimal? SecondWeightKg => Line.SecondWeightKg;
        public decimal NetWeightKg => Line.NetWeightKg;

        // UI-only: adjusted weight display when tare is used.
        // Receiving: FW - SW - Tare (for weighbridge); for platform: Net - Tare.
        public decimal DisplayWeightKg
        {
            get
            {
                if (FirstWeightKg.HasValue && SecondWeightKg.HasValue)
                    return Math.Max(0m, FirstWeightKg.Value - SecondWeightKg.Value - Tare);

                return Math.Max(0m, Line.NetWeightKg - Tare);
            }
        }
        public decimal UnitPricePerKg => Line.UnitPricePerKg;
        public decimal LineTotal => Line.LineTotal;
        public decimal VatAmount => Line.VatAmount;
        public decimal TotalInclVat => Line.TotalInclVat;
        public decimal Tare { get => Line.Tare; set => Line.Tare = value; }
        public string? Notes => Line.Notes;

        // Only last line can be deleted/edited for tare
        public bool IsEditable => IsLastLine;
    }

    public System.Collections.IEnumerable ReceivingLinesWithTotals
        => (CurrentTicketState == 'H' || CurrentTicketState == 'M')
            ? SelectedReceivingTicketLinesWithTotals
                .Where(l => l.IsActive)
                .OrderBy(l => l.CreatedTime)
                .Select((l, idx) => new ReceivingLineRow(l, idx == SelectedReceivingTicketLinesWithTotals.Where(x => x.IsActive).Count() - 1))
                .ToList()
            : Array.Empty<ReceivingLineRow>();
    public ObservableCollection<object> ReceivingLines { get; } = new();
    public decimal CreatingTicketTotalWeight =>
        (CurrentTicketState == 'H' || CurrentTicketState == 'M')
            ? ReceivingLinesWithTotals.Cast<ReceivingLineRow>().Sum(r => r.DisplayWeightKg)
            : 0m;
    public decimal CalculatedNetWeightKg =>
        SelectedReceivingTicketLines.Count > 0
            ? SelectedReceivingTicketLines.Where(l => l.IsActive).Sum(l =>
            {
                if (l.FirstWeightKg.HasValue && l.SecondWeightKg.HasValue)
                    return Math.Max(0m, l.FirstWeightKg.Value - l.SecondWeightKg.Value - l.Tare);

                return Math.Max(0m, l.NetWeightKg - l.Tare);
            })
            : (SelectedReceivingTicketDetails?.NetWeightKg ?? 0m);

    // Create-panel totals section currently mirrors selected-ticket totals (until create-line migration is complete)
    public decimal ReceivingTotalExclVat => SelectedReceivingTicketLinesTotalExVat;
    public decimal ReceivingTotalVat => SelectedReceivingTicketLinesTotalVat;
    public decimal ReceivingTotalInclVat => SelectedReceivingTicketLinesTotalInclVat;

    public decimal SelectedReceivingTicketLinesTotalVatFormatted => SelectedReceivingTicketLinesTotalVat;

    private async Task RefreshReceivingProductSuggestionsAsync()
    {
        try
        {
            var term = string.IsNullOrWhiteSpace(ReceivingProductSearchText) ? null : ReceivingProductSearchText.Trim();
            var items = await _productsAndPricesService.LookupProductsAsync(term);

            if (!string.IsNullOrWhiteSpace(SelectedReceivingProductLetter) && SelectedReceivingProductLetter != "ALL")
            {
                items = items.Where(p => p.ProductName.StartsWith(SelectedReceivingProductLetter, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ReceivingProductSuggestions.Clear();
                foreach (var p in items) ReceivingProductSuggestions.Add(p);
            });
        }
        catch
        {
            // optional
        }
    }

    private async Task ReadWeighbridgeFirstAsync()
    {
        if (!IsFirstWeightEnabled) return;

        var reading = await _scaleService.ReadOnceAsync(ScaleDeviceType.Weighbridge);
        if (reading == null)
        {
            StatusMessage = "No reading from weighbridge.";
            return;
        }

        TicketFirstWeightText = reading.WeightKg.ToString("0.00", CultureInfo.InvariantCulture);
        StatusMessage = $"Weighbridge first weight: {reading.WeightKg:0.00} kg.";
    }

    public async Task ReadWeighbridgeSecondAsync()
    {
        if (!IsSecondWeightEnabled) return;

        if (!decimal.TryParse(NormalizeDecimalText(TicketFirstWeightText), NumberStyles.Any, CultureInfo.InvariantCulture, out var firstWeight) || firstWeight <= 0m)
        {
            StatusMessage = "First weight is invalid.";
            return;
        }

        // Receiving SW simulation: SW = FW - random(1500..4500)
        var delta = Random.Shared.Next(1500, 4501);
        var secondWeight = firstWeight - delta;
        if (secondWeight <= 0m) secondWeight = 1m;

        TicketSecondWeightText = secondWeight.ToString("0.00", CultureInfo.InvariantCulture);

        var net = firstWeight - secondWeight;
        ReceivingWeightText = net.ToString("0.00", CultureInfo.InvariantCulture);
        StatusMessage = $"Weighbridge second weight: {secondWeight:0.00} kg. Net: {net:0.00} kg.";
        await Task.CompletedTask;
    }

    private string GetNextWeighbridgeFirstWeightText()
    {
        // UI-only rule: do NOT mutate initialize_weight_kg.
        // - If there are active lines: next FW should be last active line's SW
        // - Else: next FW should be initialize_weight_kg
        var last = SelectedReceivingTicketLines
            .Where(l => l.IsActive)
            .OrderBy(l => l.CreatedTime)
            .LastOrDefault();

        if (last?.SecondWeightKg.HasValue == true)
            return last.SecondWeightKg.Value.ToString("0.00", CultureInfo.InvariantCulture);

        return (SelectedReceivingTicketDetails?.InitializeWeightKg ?? 0m).ToString("0.00", CultureInfo.InvariantCulture);
    }

    public async Task ReadPlatformAsync()
    {
        var reading = await _scaleService.ReadOnceAsync(ScaleDeviceType.Platform);
        if (reading == null)
        {
            StatusMessage = "No reading from platform scale.";
            return;
        }

        TicketPlatformWeightText = reading.WeightKg.ToString("0.00", CultureInfo.InvariantCulture);
        ReceivingWeightText = TicketPlatformWeightText;
        StatusMessage = $"Platform weight: {reading.WeightKg:0.00} kg.";
    }

    public void ResetWeighbridgeWeights()
    {
        if (IsFirstWeightEnabled)
        {
            TicketFirstWeightText = "0.00";
            ReceivingWeightText = string.Empty;
        }

        if (IsSecondWeightEnabled)
        {
            TicketSecondWeightText = "0.00";
            ReceivingWeightText = string.Empty;
        }

        StatusMessage = "Weights reset.";
    }

    public async Task AddReceivingLineAsync()
    {
        if (SelectedReceivingTicketDetails == null) return;
        if (SelectedTicketTypeOption?.Key == "weighbridge")
        {
            if (!decimal.TryParse(NormalizeDecimalText(TicketFirstWeightText), NumberStyles.Any, CultureInfo.InvariantCulture, out var fw))
            {
                StatusMessage = "Invalid First Weight.";
                return;
            }

            if (!decimal.TryParse(NormalizeDecimalText(TicketSecondWeightText), NumberStyles.Any, CultureInfo.InvariantCulture, out var sw))
            {
                StatusMessage = "Invalid Second Weight.";
                return;
            }

            if (sw <= 0m)
            {
                StatusMessage = "Second Weight must be captured before adding a line.";
                return;
            }

            if (sw >= fw)
            {
                StatusMessage = "Second Weight must be less than First Weight.";
                return;
            }

            if (ReceivingSelectedProduct == null)
            {
                StatusMessage = "Please select a product.";
                return;
            }

            var net = fw - sw;

            var lineDto = new CreateTicketReceivingLineDto
            {
                ProductId = ReceivingSelectedProduct.ProductId,
                FirstWeightKg = fw,
                SecondWeightKg = sw,
                NetWeightKg = net,
                UnitPricePerKg = 0m,
                Tare = 0m,
                Notes = string.IsNullOrWhiteSpace(ReceivingLineNotes) ? null : ReceivingLineNotes
            };

            var updated = await _ticketReceivingService.AddTicketReceivingLineAsync(SelectedReceivingTicketDetails.TicketReceivingId, lineDto);
            if (updated == null)
            {
                StatusMessage = "Failed to add line.";
                return;
            }

            await LoadSelectedReceivingTicketDetailsAsync(updated.TicketReceivingId);

            // After add: FW is derived from last active line's SW (state M) or initialize_weight_kg (state H).
            TicketFirstWeightText = GetNextWeighbridgeFirstWeightText();
            TicketSecondWeightText = "0.00";
            ReceivingWeightText = string.Empty;
            CurrentTicketState = SelectedReceivingTicketDetails?.TicketState ?? 'M';
            // Reset line-entry inputs
            ReceivingSelectedProduct = null;
            ReceivingProductSearchText = string.Empty;
            ReceivingLineNotes = string.Empty;
            ReceivingWeightText = string.Empty;

            StatusMessage = "Line added.";

            return;
        }

        // Platform: use ReceivingWeightText
        if (ReceivingSelectedProduct == null)
        {
            StatusMessage = "Please select a product.";
            return;
        }

        if (!decimal.TryParse(NormalizeDecimalText(ReceivingWeightText), NumberStyles.Any, CultureInfo.InvariantCulture, out var weight) || weight <= 0m)
        {
            StatusMessage = "Weight must be captured before adding a line.";
            return;
        }

        var platformLineDto = new CreateTicketReceivingLineDto
        {
            ProductId = ReceivingSelectedProduct.ProductId,
            NetWeightKg = weight,
            UnitPricePerKg = 0m,
            Tare = 0m,
            Notes = string.IsNullOrWhiteSpace(ReceivingLineNotes) ? null : ReceivingLineNotes
        };

        var updatedPlatform = await _ticketReceivingService.AddTicketReceivingLineAsync(SelectedReceivingTicketDetails.TicketReceivingId, platformLineDto);
        if (updatedPlatform == null)
        {
            StatusMessage = "Failed to add line.";
            return;
        }

        await LoadSelectedReceivingTicketDetailsAsync(updatedPlatform.TicketReceivingId);

        // Reset line-entry inputs
        ReceivingSelectedProduct = null;
        ReceivingProductSearchText = string.Empty;
        ReceivingLineNotes = string.Empty;
        ReceivingWeightText = string.Empty;
        TicketPlatformWeightText = "0.00";

        StatusMessage = "Line added.";
    }

    public async Task UpdateLastLineTareAsync(int receivingTicketLineId, decimal tare)
    {
        if (SelectedReceivingTicketDetails == null) return;

        var ok = await _ticketReceivingService.UpdateLineTareAsync(
            SelectedReceivingTicketDetails.TicketReceivingId,
            receivingTicketLineId,
            tare);

        if (!ok)
        {
            StatusMessage = "Failed to update tare.";
            return;
        }

        await LoadSelectedReceivingTicketDetailsAsync(SelectedReceivingTicketDetails.TicketReceivingId);
        OnPropertyChanged(nameof(CreatingTicketTotalWeight));
        StatusMessage = "Tare updated.";
    }

    private async Task RemoveReceivingLineAsync(ReceivingLineRow? row)
    {
        if (SelectedReceivingTicketDetails == null || row == null) return;

        var ok = await ConfirmAsync("Delete this line item?\n\nThis cannot be undone.");
        if (!ok)
        {
            StatusMessage = "Delete cancelled.";
            return;
        }

        await _ticketReceivingService.DeleteTicketReceivingLineAsync(
            SelectedReceivingTicketDetails.TicketReceivingId,
            row.ReceivingTicketLineId);

        await LoadSelectedReceivingTicketDetailsAsync(SelectedReceivingTicketDetails.TicketReceivingId);

        if (SelectedReceivingTicketDetails != null)
        {
            CurrentTicketState = SelectedReceivingTicketDetails.TicketState;
            TicketFirstWeightText = GetNextWeighbridgeFirstWeightText();
            TicketSecondWeightText = "0.00";
            ReceivingWeightText = string.Empty;
        }

        StatusMessage = "Line deleted.";
    }

    private sealed class AsyncCommand<T> : ICommand
    {
        private readonly Func<T?, Task> _execute;
        public AsyncCommand(Func<T?, Task> execute) => _execute = execute;
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public async void Execute(object? parameter)
        {
            try { await _execute((T?)parameter); }
            catch (Exception ex) { Console.Error.WriteLine("[ERROR] Receiving AsyncCommand<T> failed: " + ex); }
        }
    }

    private sealed class AsyncCommand : ICommand
    {
        private readonly Func<Task> _execute;
        public AsyncCommand(Func<Task> execute) => _execute = execute;
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public async void Execute(object? parameter)
        {
            try
            {
                await _execute();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[ERROR] Receiving AsyncCommand failed: " + ex);
            }
        }
    }

    private sealed class RelayCommand : ICommand
    {
        private readonly Action _execute;
        public RelayCommand(Action execute) => _execute = execute;
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute();
    }

    private void OpenDistribution()
    {
        if (SelectedReceivingTicketDetails is null)
            return;

        var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        var owner = lifetime?.MainWindow;
        if (owner is null)
            return;

        var vm = TicketDistributionViewModel.FromReceiving(SelectedReceivingTicketDetails);
        var win = new MetalLink.Desktop.Views.DistributionWindow
        {
            DataContext = vm
        };

        win.ShowDialog(owner);
    }

    private sealed class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        public RelayCommand(Action<T?> execute) => _execute = execute;
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute((T?)parameter);
    }
}
