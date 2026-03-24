using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
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
using MetalLink.Desktop.Properties;
using MetalLink.Shared.Tickets.Sending;

namespace MetalLink.Desktop.ViewModels.Sending;

using MetalLink.Desktop.ViewModels.Distribution;

/// <summary>
/// Sending ticket system ViewModel. Must not reference Receiving.
/// </summary>
public sealed class TicketsSendingViewModel : ViewModelBase
{
    private readonly TicketSendingService _ticketSendingService;
    private readonly CompanyAndSiteService _companyAndSiteService;

    private readonly IScaleService _scaleService;
    private readonly ProductsAndPricesService _productsAndPricesService;

    public TicketsSendingViewModel(
        TicketSendingService ticketSendingService,
        CompanyAndSiteService companyAndSiteService,
        IScaleService scaleService,
        ProductsAndPricesService productsAndPricesService)
    {
        _ticketSendingService = ticketSendingService;
        _companyAndSiteService = companyAndSiteService;
        _scaleService = scaleService;
        _productsAndPricesService = productsAndPricesService;

        RefreshCompaniesCommand = new AsyncCommand(RefreshCompaniesAsync);
        RefreshSitesCommand = new AsyncCommand(RefreshSitesAsync);

        SearchSendingTicketsCommand = new AsyncCommand(SearchSendingTicketsAsync);
        ClearSendingTicketSearchCommand = new RelayCommand(ClearSendingTicketSearch);

        CreateSendingTicketHeaderCommand = new AsyncCommand(CreateSendingTicketHeaderAsync);
        AddSendingLineToTicketCommand = new AsyncCommand(AddSendingLineAsync);
        RemoveSendingLineCommand = new AsyncCommand<SendingLineRow?>(RemoveSendingLineAsync);
        FinalizeSendingTicketCommand = new AsyncCommand(FinalizeSendingTicketAsync);
        ReadWeighbridgeCommand = new AsyncCommand(ReadWeighbridgeFirstAsync);
        ReadWeighbridgeSecondCommand = new AsyncCommand(ReadWeighbridgeSecondAsync);
        ReadPlatformCommand = new AsyncCommand(ReadPlatformAsync);
        ResetWeighbridgeWeightsCommand = new RelayCommand(ResetWeighbridgeWeights);
        ResetPlatformWeightCommand = new RelayCommand(ResetPlatformWeight);

        // Line notes modal bindings (used by TicketsSendingView.axaml)
        ShowLineNotesCommand = new RelayCommand<string?>(notes =>
        {
            SelectedLineNotesContent = notes ?? string.Empty;
            IsNotesModalVisible = true;
        });
        CloseLineNotesCommand = new RelayCommand(() =>
        {
            IsNotesModalVisible = false;
            SelectedLineNotesContent = string.Empty;
        });

        OpenDistributionCommand = new RelayCommand(OpenDistribution);

        // Keep totals in sync when the selected ticket's line collection changes
        SelectedSendingTicketLines.CollectionChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(SelectedSendingTicketLinesTotalExVat));
            OnPropertyChanged(nameof(SelectedSendingTicketLinesTotalVat));
            OnPropertyChanged(nameof(SelectedSendingTicketLinesTotalInclVat));
            OnPropertyChanged(nameof(CalculatedNetWeightKg));
            OnPropertyChanged(nameof(SendingTotalExclVat));
            OnPropertyChanged(nameof(SendingTotalVat));
            OnPropertyChanged(nameof(SendingTotalInclVat));
            OnPropertyChanged(nameof(SendingLinesWithTotals));
            OnPropertyChanged(nameof(CreatingTicketTotalWeight));
        };

        // Basic letter filters to satisfy XAML bindings.
        CompanyLetterFilters.Add("ALL");
        for (var c = 'A'; c <= 'Z'; c++) CompanyLetterFilters.Add(c.ToString());

        ProductLetterFilters.Add("ALL");
        for (var c = 'A'; c <= 'Z'; c++) ProductLetterFilters.Add(c.ToString());
        SelectedSendingProductLetter = "ALL";
    }

    // ============================================================
    // Search (Sending-only)
    // ============================================================

    public ObservableCollection<TicketSendingSearchResultDto> SendingTicketSearchResults { get; } = new();

    private long _sendingDetailsLoadVersion;

    private TicketSendingSearchResultDto? _selectedSendingTicket;
    public TicketSendingSearchResultDto? SelectedSendingTicket
    {
        get => _selectedSendingTicket;
        set
        {
            _selectedSendingTicket = value;
            OnPropertyChanged();

            _sendingDetailsLoadVersion++;
            var version = _sendingDetailsLoadVersion;

            // Always populate Create client label from selected row
            TicketBuyerIdText = value == null ? string.Empty : FormatClientLabel(value.BuyerId, value.FirstName, value.LastName, value.CompanyName, value.SiteName);

            if (value != null && !IsNewBuyerOnly && value.TicketId > 0)
                _ = LoadSelectedSendingTicketDetailsAsync(value.TicketId, version);
            else
            {
                SelectedSendingTicketDetails = null;
                SelectedSendingTicketLines.Clear();

                // Notes must only be populated for WIP tickets under Create.
                TicketNotes = null;

                CurrentTicketState = 'C';

                // In "new ticket" flow, ticket type must be editable.
                IsTicketTypeEnabled = true;
                OnPropertyChanged(nameof(IsTicketTypeEnabled));

                // Defaults for new create in Sending: Weighbridge
                SelectedTicketTypeOption = TicketTypeOptions.FirstOrDefault(o => o.Key == "weighbridge");

                OnPropertyChanged(nameof(SendingLinesWithTotals));
                OnPropertyChanged(nameof(CreatingTicketTotalWeight));
            }
        }
    }

    private string _searchSendingTicketNumberText = string.Empty;
    public string SearchSendingTicketNumberText { get => _searchSendingTicketNumberText; set { _searchSendingTicketNumberText = value; OnPropertyChanged(); } }

    private string _searchSendingTicketBuyerIdText = string.Empty;
    public string SearchSendingTicketBuyerIdText { get => _searchSendingTicketBuyerIdText; set { _searchSendingTicketBuyerIdText = value; OnPropertyChanged(); } }

    private string _searchSendingTicketIdNumberText = string.Empty;
    public string SearchSendingTicketIdNumberText { get => _searchSendingTicketIdNumberText; set { _searchSendingTicketIdNumberText = value; OnPropertyChanged(); } }

    private string _searchSendingTicketFirstNameText = string.Empty;
    public string SearchSendingTicketFirstNameText { get => _searchSendingTicketFirstNameText; set { _searchSendingTicketFirstNameText = value; OnPropertyChanged(); } }

    private string _searchSendingTicketLastNameText = string.Empty;
    public string SearchSendingTicketLastNameText { get => _searchSendingTicketLastNameText; set { _searchSendingTicketLastNameText = value; OnPropertyChanged(); } }

    private string _searchSendingTicketAccountNumberText = string.Empty;
    public string SearchSendingTicketAccountNumberText { get => _searchSendingTicketAccountNumberText; set { _searchSendingTicketAccountNumberText = value; OnPropertyChanged(); } }

    // Aliases used by TicketsSendingView.axaml (kept to avoid XAML changes)
    public ObservableCollection<MetalLink.Shared.Companies.CompanyLookupDto> SearchCompanySuggestions => SendingCompanies;
    public ObservableCollection<MetalLink.Shared.Sites.SiteLookupDto> SearchTicketSiteSuggestions => SendingSites;

    public MetalLink.Shared.Companies.CompanyLookupDto? SelectedSearchCompany
    {
        get => SelectedSendingCompany;
        set { SelectedSendingCompany = value; OnPropertyChanged(); }
    }

    public MetalLink.Shared.Sites.SiteLookupDto? SelectedSearchSite
    {
        get => SelectedSendingSite;
        set { SelectedSendingSite = value; OnPropertyChanged(); }
    }

    public ObservableCollection<string> CompanyLetterFilters { get; } = new();

    private string _searchTicketCompanyLetter = "ALL";
    public string SearchTicketCompanyLetter { get => _searchTicketCompanyLetter; set { _searchTicketCompanyLetter = value; OnPropertyChanged(); } }

    public ICommand SearchSendingTicketsCommand { get; }
    public ICommand ClearSendingTicketSearchCommand { get; }

    private async Task SearchSendingTicketsAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            StatusMessage = "Searching sending tickets...";

            var req = new TicketSendingSearchRequestDto
            {
                NewBuyerOnly = IsNewBuyerOnly,
                CompanyId = SelectedSearchCompany?.CompanyId,
                SiteId = SelectedSearchSite?.SiteId,
                BuyerId = int.TryParse(SearchSendingTicketBuyerIdText, out var bid) ? bid : null,
                FirstName = string.IsNullOrWhiteSpace(SearchSendingTicketFirstNameText) ? null : SearchSendingTicketFirstNameText.Trim(),
                LastName = string.IsNullOrWhiteSpace(SearchSendingTicketLastNameText) ? null : SearchSendingTicketLastNameText.Trim(),
                IdNumber = string.IsNullOrWhiteSpace(SearchSendingTicketIdNumberText) ? null : SearchSendingTicketIdNumberText.Trim(),
                AccountNumber = long.TryParse(SearchSendingTicketAccountNumberText, out var acc) ? acc : null,
                SearchTerm = string.IsNullOrWhiteSpace(SearchSendingTicketNumberText) ? null : SearchSendingTicketNumberText.Trim(),
                PageNumber = 1,
                PageSize = 50
            };

            var results = await _ticketSendingService.SearchTicketsSendingAsync(req);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SendingTicketSearchResults.Clear();
                foreach (var r in results) SendingTicketSearchResults.Add(r);
                SelectedSendingTicket = SendingTicketSearchResults.FirstOrDefault();
                SelectedSendingTicketDetails = null;
                SelectedSendingTicketLines.Clear();
                OnPropertyChanged(nameof(ShouldShowTicketDetails));

                // Auto expand result panels and collapse search
                IsSearchExpanded = true;
                IsResultsExpanded = true;
                IsCreateExpanded = true;
                IsScaleExpanded = true;
                IsLinesExpanded = true;
                IsAddLinesExpanded = true;
            });

            StatusMessage = $"Loaded {SendingTicketSearchResults.Count} sending ticket(s).";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ClearSendingTicketSearch()
    {
        SearchSendingTicketNumberText = string.Empty;
        SearchSendingTicketBuyerIdText = string.Empty;
        SearchSendingTicketIdNumberText = string.Empty;
        SearchSendingTicketFirstNameText = string.Empty;
        SearchSendingTicketLastNameText = string.Empty;
        SearchSendingTicketAccountNumberText = string.Empty;
        SelectedSendingTicket = null;
        SendingTicketSearchResults.Clear();
        StatusMessage = "Sending search cleared.";
    }

    // ============================================================
    // Details panel (Sending-only)
    // ============================================================

    private TicketSendingDto? _selectedSendingTicketDetails;
    public TicketSendingDto? SelectedSendingTicketDetails
    {
        get => _selectedSendingTicketDetails;
        set { _selectedSendingTicketDetails = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShouldShowTicketDetails)); }
    }

    public ObservableCollection<TicketSendingLineDto> SelectedSendingTicketLines { get; } = new();
    public ObservableCollection<TicketSendingLineDto> SelectedSendingTicketLinesWithTotals => SelectedSendingTicketLines;

    // Create-panel line grid: show current in-progress ticket lines so the operator can see what's already added.
    // For state C (new/uncreated), keep it empty.
    public sealed record SendingLineRow(TicketSendingLineDto Line, bool IsLastLine)
    {
        public int TicketSendingLineId => Line.TicketSendingLineId;
        public string ProductName => Line.ProductName;
        public decimal? FirstWeightKg => Line.FirstWeightKg;
        public decimal? SecondWeightKg => Line.SecondWeightKg;
        public decimal NetWeightKg => Line.NetWeightKg;

        // UI-only: adjusted weight display when tare is used.
        // Sending: SW - FW - Tare (for weighbridge); for platform: Net - Tare.
        public decimal DisplayWeightKg
        {
            get
            {
                if (FirstWeightKg.HasValue && SecondWeightKg.HasValue)
                    return Math.Max(0m, SecondWeightKg.Value - FirstWeightKg.Value - Tare);

                return Math.Max(0m, Line.NetWeightKg - Tare);
            }
        }
        public decimal UnitPricePerKg => Line.UnitPricePerKg;
        public decimal LineTotal => Line.LineTotal;
        public decimal VatAmount => Line.VatAmount;
        public decimal TotalInclVat => Line.TotalInclVat;
        public decimal Tare { get => Line.Tare; set => Line.Tare = value; }
        public string? Notes => Line.Notes;

        public bool IsEditable => IsLastLine;
    }

    public System.Collections.IEnumerable SendingLinesWithTotals
        => (CurrentTicketState == 'H' || CurrentTicketState == 'M')
            ? SelectedSendingTicketLinesWithTotals
                .Where(l => l.IsActive)
                .OrderBy(l => l.CreatedTime)
                .Select((l, idx) => new SendingLineRow(l, idx == SelectedSendingTicketLinesWithTotals.Where(x => x.IsActive).Count() - 1))
                .ToList()
            : Array.Empty<SendingLineRow>();

    private bool _isNewBuyerOnly;
    public bool IsNewBuyerOnly
    {
        get => _isNewBuyerOnly;
        set
        {
            _isNewBuyerOnly = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShouldShowTicketDetails));
            OnPropertyChanged(nameof(ShouldShowTicketColumns));
            OnPropertyChanged(nameof(ResultsMainColumnWidth));
            OnPropertyChanged(nameof(ResultsSpacerColumnWidth));

            // Clear results when switching modes
            SelectedSendingTicket = null;
            SendingTicketSearchResults.Clear();

            ResetCreateSectionForMode();
        }
    }

    public bool ShouldShowTicketDetails => !IsNewBuyerOnly && SelectedSendingTicketDetails != null;

    public bool ShouldShowTicketColumns => !IsNewBuyerOnly;

    // Results width: when New Buyer? is active, show results at ~60% width.
    public GridLength ResultsMainColumnWidth => IsNewBuyerOnly ? new GridLength(3, GridUnitType.Star) : new GridLength(1, GridUnitType.Star);
    public GridLength ResultsSpacerColumnWidth => IsNewBuyerOnly ? new GridLength(2, GridUnitType.Star) : new GridLength(0, GridUnitType.Pixel);

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
        TicketNumber = string.Empty;
        TicketFirstWeightText = "0.00";
        TicketSecondWeightText = "0.00";
        TicketPlatformWeightText = "0.00";
        SendingWeightText = string.Empty;

        TicketVehicleRegistration = null;
        TicketTrailerRegistration = null;
        TicketDriverName = null;
        TicketOfmWeighbridgeTicket = null;
        TicketDeliveryNumber = null;
        TicketForeignTicket = null;
        TicketCkNumber = null;
        TicketNotes = null;

        SendingSelectedProduct = null;
        SendingProductSearchText = string.Empty;
        SendingLineNotes = string.Empty;

        CurrentTicketState = 'C';

        // In "new ticket" flow, ticket type must be editable.
        IsTicketTypeEnabled = true;
        OnPropertyChanged(nameof(IsTicketTypeEnabled));

        // Default ticket type for new create in Sending is Weighbridge
        SelectedTicketTypeOption = TicketTypeOptions.FirstOrDefault(o => o.Key == "weighbridge");

        if (SelectedSendingTicket != null)
            TicketBuyerIdText = FormatClientLabel(SelectedSendingTicket.BuyerId, SelectedSendingTicket.FirstName, SelectedSendingTicket.LastName, SelectedSendingTicket.CompanyName, SelectedSendingTicket.SiteName);

        _ = RegenerateTicketNumberForCreateAsync();
    }

    private async Task LoadSelectedSendingTicketDetailsAsync(long ticketId)
        => await LoadSelectedSendingTicketDetailsAsync(ticketId, _sendingDetailsLoadVersion);

    private async Task LoadSelectedSendingTicketDetailsAsync(long ticketId, long version)
    {
        var details = await _ticketSendingService.GetTicketSendingByIdAsync(ticketId);

        // Prevent stale async loads from overwriting newer selections
        if (version != _sendingDetailsLoadVersion)
            return;

        if (details == null)
        {
            SelectedSendingTicketDetails = null;
            SelectedSendingTicketLines.Clear();
            OnPropertyChanged(nameof(SendingLinesWithTotals));
            OnPropertyChanged(nameof(CreatingTicketTotalWeight));
            return;
        }

        SelectedSendingTicketDetails = details;
        OnPropertyChanged(nameof(CreatingTicketTotalWeight));
        SelectedSendingTicketLines.Clear();
        foreach (var l in details.Lines.Where(l => l.IsActive)) SelectedSendingTicketLines.Add(l);

        // Keep Create panel ticket type and header fields in sync with selected ticket.
        SetCreateTicketTypeFromTicketTypeId(details.TicketTypeId);

        if (details.TicketTypeId == 1 && (details.TicketState == 'H' || details.TicketState == 'M'))
        {
            TicketFirstWeightText = (details.InitializeWeightKg ?? 0m).ToString("0.00");
            TicketSecondWeightText = "0.00";
            SendingWeightText = string.Empty;
        }

        TicketVehicleRegistration = details.VehicleRegistration;
        TicketTrailerRegistration = details.TrailerRegistration;
        TicketDriverName = details.DriverName;
        TicketOfmWeighbridgeTicket = details.OfmWeighbridgeTicket;
        TicketDeliveryNumber = details.DeliveryNumber;
        TicketForeignTicket = details.ForeignTicket;
        TicketCkNumber = details.CkNumber;

        // Notes must only be populated for WIP tickets under Create.
        TicketNotes = (details.TicketState == 'H' || details.TicketState == 'M') ? details.Notes : null;

        // Populate Create panel depending on ticket state
        if (details.TicketState == 'H' || details.TicketState == 'M')
        {
            TicketNumber = details.TicketNumber;
            // Keep formatted display ("id | first last | company site") from SelectedSendingTicket
            CurrentTicketState = details.TicketState;
            IsTicketTypeEnabled = false;
        }
        else
        {
            // Keep formatted display ("id | first last | company site") from SelectedSendingTicket
            CurrentTicketState = 'C';
            TicketFirstWeightText = "0.00";
            TicketSecondWeightText = "0";
            TicketPlatformWeightText = "0";

            // Selecting a completed ticket means we are creating a NEW ticket based on it.
            // Use the non-consuming peek endpoint so the ticket number shown is always the next number.
            TicketNumber = await _ticketSendingService.GenerateTicketNumberAsync(details.TicketTypeId == 1 ? "SWB" : "SPL");

            IsTicketTypeEnabled = true;
        }

        OnPropertyChanged(nameof(CreateTicketModeText));
        OnPropertyChanged(nameof(IsCreateTicketModeVisible));
        OnPropertyChanged(nameof(CreateHeaderButtonVisible));
        OnPropertyChanged(nameof(SaveResetButtonVisible));
        OnPropertyChanged(nameof(IsFinalizeTicketEnabled));
        OnPropertyChanged(nameof(AddLineButtonEnabled));
        OnPropertyChanged(nameof(IsTicketTypeEnabled));
        OnPropertyChanged(nameof(SendingLinesWithTotals));

        // Ensure product dropdown is populated without requiring typing
        _ = RefreshSendingProductSuggestionsAsync();
    }

    public decimal SelectedSendingTicketLinesTotalVat => SelectedSendingTicketLines.Where(l => l.IsActive).Sum(l => l.VatAmount);
    public decimal SelectedSendingTicketLinesTotalInclVat => SelectedSendingTicketLines.Where(l => l.IsActive).Sum(l => l.TotalInclVat);
    public decimal SelectedSendingTicketLinesTotalExVat => SelectedSendingTicketLines.Where(l => l.IsActive).Sum(l => l.LineTotal);

    // ============================================================
    // Create/Edit panel (Sending-only)
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
            OnPropertyChanged(nameof(SendingLinesWithTotals));
            OnPropertyChanged(nameof(CreatingTicketTotalWeight));
        }
    }

    public string CreateTicketModeText =>
        CurrentTicketState is 'H' or 'M'
            ? $"Editing in-progress ticket: {TicketNumber}"
            : (SelectedSendingTicketDetails?.TicketState == 'C' ? "New ticket (based on selected completed ticket)" : "New ticket");

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

    private string _ticketBuyerIdText = string.Empty;
    public string TicketBuyerIdText { get => _ticketBuyerIdText; set { _ticketBuyerIdText = value; OnPropertyChanged(); } }

    private string _ticketNumber = string.Empty;
    public string TicketNumber { get => _ticketNumber; set { _ticketNumber = value; OnPropertyChanged(); } }

    public bool AreWeighbridgeFieldsVisible => SelectedTicketTypeOption?.Key == "weighbridge";
    public bool ArePlatformFieldsVisible => SelectedTicketTypeOption?.Key == "platform";

    public sealed record SendingCreateTicketTypeOption(string Key, string Display);

    private ObservableCollection<SendingCreateTicketTypeOption>? _ticketTypeOptions;
    public ObservableCollection<SendingCreateTicketTypeOption> TicketTypeOptions
        => _ticketTypeOptions ??= new ObservableCollection<SendingCreateTicketTypeOption>(
            new[]
            {
                new SendingCreateTicketTypeOption("weighbridge", "Weighbridge"),
                new SendingCreateTicketTypeOption("platform", "Platform")
            });

    private SendingCreateTicketTypeOption? _selectedTicketTypeOption;
    public SendingCreateTicketTypeOption? SelectedTicketTypeOption
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

    private void SetCreateTicketTypeFromTicketTypeId(int ticketTypeId)
    {
        var key = ticketTypeId == 1 ? "weighbridge" : "platform";
        SelectedTicketTypeOption = TicketTypeOptions.FirstOrDefault(o => o.Key == key) ?? TicketTypeOptions.FirstOrDefault();
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
            var prefix = SelectedTicketTypeOption.Key == "weighbridge" ? "SWB" : "SPL";
            var tn = await _ticketSendingService.GenerateTicketNumberAsync(prefix);

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

    public ICommand CreateSendingTicketHeaderCommand { get; }
    public ICommand AddSendingLineToTicketCommand { get; }
    public ICommand RemoveSendingLineCommand { get; }
    public ICommand FinalizeSendingTicketCommand { get; }

    public ICommand ReadWeighbridgeCommand { get; }
    public ICommand ReadWeighbridgeSecondCommand { get; }
    public ICommand ReadPlatformCommand { get; }
    public ICommand ResetWeighbridgeWeightsCommand { get; }
    public ICommand ResetPlatformWeightCommand { get; }

    private string _sendingProductSearchText = string.Empty;
    public string SendingProductSearchText
    {
        get => _sendingProductSearchText;
        set
        {
            _sendingProductSearchText = value;
            OnPropertyChanged();
            _ = RefreshSendingProductSuggestionsAsync();
        }
    }

    private string _sendingWeightText = string.Empty;
    public string SendingWeightText { get => _sendingWeightText; set { _sendingWeightText = value; OnPropertyChanged(); } }

    private string _sendingLineNotes = string.Empty;
    public string SendingLineNotes
    {
        get => _sendingLineNotes;
        set { _sendingLineNotes = value; OnPropertyChanged(); }
    }

    public ObservableCollection<ProductLookupDto> SendingProductSuggestions { get; } = new();

    private ProductLookupDto? _sendingSelectedProduct;
    public ProductLookupDto? SendingSelectedProduct { get => _sendingSelectedProduct; set { _sendingSelectedProduct = value; OnPropertyChanged(); } }

    private string _selectedSendingProductLetter = "ALL";
    public string SelectedSendingProductLetter
    {
        get => _selectedSendingProductLetter;
        set
        {
            _selectedSendingProductLetter = value;
            OnPropertyChanged();
            _ = RefreshSendingProductSuggestionsAsync();
        }
    }

    public ObservableCollection<string> ProductLetterFilters { get; } = new();

    public string EditLineProductSearchText { get; set; } = string.Empty;
    public ObservableCollection<string> EditLineProductSuggestions { get; } = new();
    public object? EditLineSelectedProduct { get; set; }
    public string EditLineSelectedProductLetter { get; set; } = string.Empty;
    public string EditLineWeightText { get; set; } = string.Empty;

    public bool IsEditingTicketLine { get; set; }
    public bool IsEditable { get; set; }
    public bool IsTicketTypeEnabled { get; set; } = true;

    public decimal CalculatedNetWeightKg =>
        SelectedSendingTicketLines.Count > 0
            ? SelectedSendingTicketLines.Where(l => l.IsActive).Sum(l =>
            {
                if (l.FirstWeightKg.HasValue && l.SecondWeightKg.HasValue)
                    return Math.Max(0m, l.SecondWeightKg.Value - l.FirstWeightKg.Value - l.Tare);

                return Math.Max(0m, l.NetWeightKg - l.Tare);
            })
            : (SelectedSendingTicketDetails?.NetWeightKg ?? 0m);

    // Create-panel totals section currently mirrors selected-ticket totals (until create-line migration is complete)
    public decimal SendingTotalExclVat => SelectedSendingTicketLinesTotalExVat;
    public decimal SendingTotalVat => SelectedSendingTicketLinesTotalVat;
    public decimal SendingTotalInclVat => SelectedSendingTicketLinesTotalInclVat;

    public decimal CreatingTicketTotalWeight =>
        (CurrentTicketState == 'H' || CurrentTicketState == 'M')
            ? SendingLinesWithTotals.Cast<SendingLineRow>().Sum(r => r.DisplayWeightKg)
            : 0m;

    // ============================================================
    // Sending-specific Company/Site selection (independent state)
    // ============================================================

    public ObservableCollection<MetalLink.Shared.Companies.CompanyLookupDto> SendingCompanies { get; } = new();
    public ObservableCollection<MetalLink.Shared.Sites.SiteLookupDto> SendingSites { get; } = new();

    private MetalLink.Shared.Companies.CompanyLookupDto? _selectedSendingCompany;
    public MetalLink.Shared.Companies.CompanyLookupDto? SelectedSendingCompany
    {
        get => _selectedSendingCompany;
        set
        {
            _selectedSendingCompany = value;
            OnPropertyChanged();
            _ = RefreshSitesAsync();
        }
    }

    private MetalLink.Shared.Sites.SiteLookupDto? _selectedSendingSite;
    public MetalLink.Shared.Sites.SiteLookupDto? SelectedSendingSite
    {
        get => _selectedSendingSite;
        set
        {
            _selectedSendingSite = value;
            OnPropertyChanged();
        }
    }

    private string _companyLookupTerm = string.Empty;
    public string SendingCompanyLookupTerm
    {
        get => _companyLookupTerm;
        set { _companyLookupTerm = value; OnPropertyChanged(); }
    }

    private string _siteLookupTerm = string.Empty;
    public string SendingSiteLookupTerm
    {
        get => _siteLookupTerm;
        set { _siteLookupTerm = value; OnPropertyChanged(); }
    }

    public ICommand RefreshCompaniesCommand { get; }
    public ICommand RefreshSitesCommand { get; }

    public ICommand ShowLineNotesCommand { get; }
    public ICommand CloseLineNotesCommand { get; }

    public ICommand OpenDistributionCommand { get; }

    private bool _isNotesModalVisible;
    public bool IsNotesModalVisible
    {
        get => _isNotesModalVisible;
        set { _isNotesModalVisible = value; OnPropertyChanged(); }
    }

    private string _selectedLineNotesContent = string.Empty;
    public string SelectedLineNotesContent
    {
        get => _selectedLineNotesContent;
        set { _selectedLineNotesContent = value; OnPropertyChanged(); }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }

    private bool _isSearchExpanded = true;
    public bool IsSearchExpanded { get => _isSearchExpanded; set { _isSearchExpanded = value; OnPropertyChanged(); } }

    private bool _isResultsExpanded = false;
    public bool IsResultsExpanded { get => _isResultsExpanded; set { _isResultsExpanded = value; OnPropertyChanged(); } }

    private bool _isCreateExpanded = false;
    public bool IsCreateExpanded { get => _isCreateExpanded; set { _isCreateExpanded = value; OnPropertyChanged(); } }

    private bool _isScaleExpanded = false;
    public bool IsScaleExpanded { get => _isScaleExpanded; set { _isScaleExpanded = value; OnPropertyChanged(); } }

    private bool _isLinesExpanded = false;
    public bool IsLinesExpanded { get => _isLinesExpanded; set { _isLinesExpanded = value; OnPropertyChanged(); } }

    private bool _isAddLinesExpanded = false;
    public bool IsAddLinesExpanded { get => _isAddLinesExpanded; set { _isAddLinesExpanded = value; OnPropertyChanged(); } }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public async Task InitializeAsync()
    {
        await RefreshCompaniesAsync();
    }

    public void ResetPlatformWeight()
    {
        TicketPlatformWeightText = "0";
        SendingWeightText = string.Empty;
        StatusMessage = "Platform weight reset.";
    }

    public async Task OnEnterTicketsSendingAsync()
    {
        await RefreshCompaniesAsync();
    }

    public async Task SaveTicketAsync()
    {
        StatusMessage = "Ticket saved.";
        await Task.CompletedTask;
    }

    public async Task ClearSendingTicketAsync()
    {
        StatusMessage = "Ticket cleared.";
        await Task.CompletedTask;
    }

    public async Task CaptureWeightAsync()
    {
        if (AreWeighbridgeFieldsVisible)
        {
            if (IsFirstWeightEnabled) await ReadWeighbridgeAsync();
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

    public async Task PrintSendingTicketAsync()
    {
        StatusMessage = "Printing sending ticket...";
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

    private async Task RefreshCompaniesAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            StatusMessage = "Loading companies (Sending)...";

            var items = await _companyAndSiteService.LookupCompaniesAsync(
                string.IsNullOrWhiteSpace(SendingCompanyLookupTerm) ? null : SendingCompanyLookupTerm.Trim());

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SendingCompanies.Clear();
                foreach (var c in items)
                    SendingCompanies.Add(c);

                // Do not auto-select a company on load; user must choose.
                SelectedSendingCompany = null;
                SendingSites.Clear();
                SelectedSendingSite = null;
            });

            StatusMessage = $"Loaded {SendingCompanies.Count} companies (Sending).";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshSitesAsync()
    {
        if (SelectedSendingCompany == null)
        {
            await Dispatcher.UIThread.InvokeAsync(() => SendingSites.Clear());
            SelectedSendingSite = null;
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Loading sites (Sending)...";

            var items = await _companyAndSiteService.LookupSitesForCompanyAsync(
                SelectedSendingCompany.CompanyId,
                term: string.IsNullOrWhiteSpace(SendingSiteLookupTerm) ? string.Empty : SendingSiteLookupTerm.Trim());

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SendingSites.Clear();
                foreach (var s in items ?? new())
                    SendingSites.Add(s);

                // Do not auto-select a site on load; user must choose.
                SelectedSendingSite = null;
            });

            StatusMessage = $"Loaded {SendingSites.Count} sites (Sending).";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshSendingProductSuggestionsAsync()
    {
        try
        {
            var term = string.IsNullOrWhiteSpace(SendingProductSearchText) ? null : SendingProductSearchText.Trim();
            var items = await _productsAndPricesService.LookupProductsAsync(term);

            if (!string.IsNullOrWhiteSpace(SelectedSendingProductLetter) && SelectedSendingProductLetter != "ALL")
            {
                items = items.Where(p => p.ProductName.StartsWith(SelectedSendingProductLetter, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SendingProductSuggestions.Clear();
                foreach (var p in items) SendingProductSuggestions.Add(p);
            });
        }
        catch
        {
            // optional
        }
    }

    private static string NormalizeDecimalText(string text)
        => (text ?? string.Empty).Replace(',', '.').Trim();

    public async Task ReadWeighbridgeAsync()
    {
        if (!IsFirstWeightEnabled) return;

        var reading = await _scaleService.ReadOnceAsync(ScaleDeviceType.Weighbridge);
        if (reading == null)
        {
            StatusMessage = "No reading from weighbridge.";
            return;
        }

        TicketFirstWeightText = reading.WeightKg.ToString("0.00");
        StatusMessage = $"Weighbridge first weight: {reading.WeightKg:0.00} kg.";
    }

    public async Task ReadWeighbridgeFirstAsync() => await ReadWeighbridgeAsync();

    public async Task ReadWeighbridgeSecondAsync()
    {
        if (!IsSecondWeightEnabled) return;

        if (!decimal.TryParse(NormalizeDecimalText(TicketFirstWeightText), out var firstWeight) || firstWeight <= 0m)
        {
            StatusMessage = "First weight is invalid.";
            return;
        }

        // Sending SW simulation: SW = FW + random(1500..4500)
        var delta = Random.Shared.Next(1500, 4501);
        var secondWeight = firstWeight + delta;

        TicketSecondWeightText = secondWeight.ToString("0.00");

        var net = secondWeight - firstWeight;
        SendingWeightText = net.ToString("0.00");
        StatusMessage = $"Weighbridge second weight: {secondWeight:0.00} kg. Net: {net:0.00} kg.";
        await Task.CompletedTask;
    }

    public async Task ReadPlatformAsync()
    {
        var reading = await _scaleService.ReadOnceAsync(ScaleDeviceType.Platform);
        if (reading == null)
        {
            StatusMessage = "No reading from platform scale.";
            return;
        }

        TicketPlatformWeightText = reading.WeightKg.ToString("0.00");
        SendingWeightText = TicketPlatformWeightText;
        StatusMessage = $"Platform weight: {reading.WeightKg:0.00} kg.";
    }

    public void ResetWeighbridgeWeights()
    {
        if (IsFirstWeightEnabled)
            TicketFirstWeightText = "0.00";
        if (IsSecondWeightEnabled)
            TicketSecondWeightText = "0.00";

        SendingWeightText = string.Empty;
        StatusMessage = "Weights reset.";
    }

    private string GetNextWeighbridgeFirstWeightText()
    {
        var last = SelectedSendingTicketLines
            .Where(l => l.IsActive)
            .OrderBy(l => l.CreatedTime)
            .LastOrDefault();

        if (last?.SecondWeightKg.HasValue == true)
            return last.SecondWeightKg.Value.ToString("0.00");

        return (SelectedSendingTicketDetails?.InitializeWeightKg ?? 0m).ToString("0.00");
    }

    public async Task CreateSendingTicketHeaderAsync()
    {
        if (IsBusy) return;

        // Remember if we are creating from "New Buyer?" mode so we can reset UI correctly afterwards.
        var wasNewBuyerMode = IsNewBuyerOnly;
        var selectedRowBeforeCreate = SelectedSendingTicket;

        if (!int.TryParse(TicketBuyerIdText.Split('|')[0].Trim(), out var buyerId) || buyerId <= 0)
        {
            StatusMessage = "Please enter/select a valid Buyer ID.";
            return;
        }

        int ticketTypeId = SelectedTicketTypeOption?.Key == "weighbridge" ? 1 : 2;

        decimal? initWeight = null;
        if (ticketTypeId == 1 && decimal.TryParse(NormalizeDecimalText(TicketFirstWeightText), out var fw))
            initWeight = fw;

        if (ticketTypeId == 1 && (!initWeight.HasValue || initWeight.Value <= 0))
        {
            StatusMessage = "First Weight must be captured before creating header.";
            return;
        }

        IsBusy = true;
        try
        {
            var prefix = ticketTypeId == 1 ? "SWB" : "SPL";
            TicketNumber = await _ticketSendingService.GenerateTicketNumberAsync(prefix);

            var dto = new CreateTicketSendingDto
            {
                BuyerId = buyerId,
                TicketTypeId = ticketTypeId,
                TicketNumber = TicketNumber,
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

            var created = await _ticketSendingService.CreateTicketSendingAsync(dto);
            if (created == null)
            {
                StatusMessage = "Failed to create ticket header.";
                return;
            }

            if (wasNewBuyerMode)
            {
                // Requirement: if New Buyer? was used, treat as creating a brand new ticket:
                // - untick the checkbox
                // - clear results
                // - add the newly created ticket row and select it
                IsNewBuyerOnly = false;

                SendingTicketSearchResults.Clear();

                var newRow = new TicketSendingSearchResultDto
                {
                    TicketId = created.TicketSendingId,
                    TicketNumber = created.TicketNumber,
                    TicketType = created.TicketTypeName,
                    TicketTypeId = created.TicketTypeId,
                    BuyerId = created.BuyerId,
                    FirstName = selectedRowBeforeCreate?.FirstName,
                    LastName = selectedRowBeforeCreate?.LastName,
                    CompanyName = selectedRowBeforeCreate?.CompanyName,
                    SiteName = selectedRowBeforeCreate?.SiteName,
                    AccountNumber = selectedRowBeforeCreate?.AccountNumber,
                    NetWeightKg = created.NetWeightKg,
                    TicketStatus = created.TicketState,
                    CreatedTime = created.CreatedTime
                };

                SendingTicketSearchResults.Add(newRow);
                SelectedSendingTicket = newRow;

                SelectedSendingTicketDetails = created;
                SelectedSendingTicketLines.Clear();
                foreach (var l in created.Lines.Where(l => l.IsActive)) SelectedSendingTicketLines.Add(l);

                CurrentTicketState = created.TicketState;
                StatusMessage = $"Header created: {created.TicketNumber}";
                return;
            }

            CurrentTicketState = created.TicketState;
            await SearchSendingTicketsAsync();

            SelectedSendingTicket = SendingTicketSearchResults.FirstOrDefault(r => r.TicketId == created.TicketSendingId)
                                   ?? SendingTicketSearchResults.FirstOrDefault();

            await LoadSelectedSendingTicketDetailsAsync(created.TicketSendingId);

            StatusMessage = $"Header created: {created.TicketNumber}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task AddSendingLineAsync()
    {
        if (SelectedSendingTicketDetails == null) return;

        if (SendingSelectedProduct == null)
        {
            StatusMessage = "Please select a product.";
            return;
        }

        if (SelectedTicketTypeOption?.Key == "weighbridge")
        {
            if (!decimal.TryParse(NormalizeDecimalText(TicketSecondWeightText), out var sw))
            {
                StatusMessage = "Invalid Second Weight.";
                return;
            }

            if (sw <= 0m)
            {
                StatusMessage = "Second Weight must be captured before adding a line.";
                return;
            }

            var lineDto = new CreateTicketSendingLineDto
            {
                ProductId = SendingSelectedProduct.ProductId,
                SecondWeightKg = sw,
                NetWeightKg = 0m,
                UnitPricePerKg = 0m,
                Tare = 0m,
                Notes = string.IsNullOrWhiteSpace(SendingLineNotes) ? null : SendingLineNotes
            };

            var updated = await _ticketSendingService.AddTicketSendingLineAsync(SelectedSendingTicketDetails.TicketSendingId, lineDto);
            if (updated == null)
            {
                StatusMessage = "Failed to add line.";
                return;
            }

            await LoadSelectedSendingTicketDetailsAsync(updated.TicketSendingId);

            TicketFirstWeightText = GetNextWeighbridgeFirstWeightText();
            TicketSecondWeightText = "0.00";
            SendingWeightText = string.Empty;
            CurrentTicketState = SelectedSendingTicketDetails?.TicketState ?? 'M';

            // Reset line-entry inputs
            SendingSelectedProduct = null;
            SendingProductSearchText = string.Empty;
            SendingLineNotes = string.Empty;
            SendingWeightText = string.Empty;

            StatusMessage = "Line added.";
            return;
        }

        if (!decimal.TryParse(NormalizeDecimalText(SendingWeightText), out var weight) || weight <= 0)
        {
            StatusMessage = "Weight must be captured before adding a line.";
            return;
        }

        var platformLine = new CreateTicketSendingLineDto
        {
            ProductId = SendingSelectedProduct.ProductId,
            NetWeightKg = weight,
            UnitPricePerKg = 0m,
            Tare = 0m,
            Notes = string.IsNullOrWhiteSpace(SendingLineNotes) ? null : SendingLineNotes
        };

        var updatedPlatform = await _ticketSendingService.AddTicketSendingLineAsync(SelectedSendingTicketDetails.TicketSendingId, platformLine);
        if (updatedPlatform != null)
            await LoadSelectedSendingTicketDetailsAsync(updatedPlatform.TicketSendingId);

        // Reset line-entry inputs
        SendingSelectedProduct = null;
        SendingProductSearchText = string.Empty;
        SendingLineNotes = string.Empty;
        SendingWeightText = string.Empty;
        TicketPlatformWeightText = "0.00";

        StatusMessage = "Line added.";
    }

    public async Task UpdateLastLineTareAsync(int ticketSendingLineId, decimal tare)
    {
        if (SelectedSendingTicketDetails == null) return;

        var ok = await _ticketSendingService.UpdateLineTareAsync(
            SelectedSendingTicketDetails.TicketSendingId,
            ticketSendingLineId,
            tare);

        if (!ok)
        {
            StatusMessage = "Failed to update tare.";
            return;
        }

        await LoadSelectedSendingTicketDetailsAsync(SelectedSendingTicketDetails.TicketSendingId);
        OnPropertyChanged(nameof(CreatingTicketTotalWeight));
        StatusMessage = "Tare updated.";
    }

    private void MoveSendingTicketToTop(long ticketId)
    {
        var item = SendingTicketSearchResults.FirstOrDefault(r => r.TicketId == ticketId);
        if (item == null) return;

        var idx = SendingTicketSearchResults.IndexOf(item);
        if (idx > 0)
            SendingTicketSearchResults.Move(idx, 0);
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

    public async Task FinalizeSendingTicketAsync()
    {
        if (SelectedSendingTicketDetails == null) return;

        var ok = await ConfirmAsync("Finalize this ticket?\n\nThis will mark it as complete.");
        if (!ok)
        {
            StatusMessage = "Finalize cancelled.";
            return;
        }

        var ticketId = SelectedSendingTicketDetails.TicketSendingId;
        await _ticketSendingService.UpdateTicketStateAsync(ticketId, 'C');

        // Reload details (NetWeightKg, state etc.)
        await LoadSelectedSendingTicketDetailsAsync(ticketId);

        // Requirement: update the existing row in the Results grid (Net Weight + Status)
        var idx = SendingTicketSearchResults.ToList().FindIndex(r => r.TicketId == ticketId);
        if (idx >= 0)
        {
            var existing = SendingTicketSearchResults[idx];
            var updatedRow = new TicketSendingSearchResultDto
            {
                TicketId = existing.TicketId,
                TicketNumber = existing.TicketNumber,
                TicketType = existing.TicketType,
                TicketTypeId = existing.TicketTypeId,
                BuyerId = existing.BuyerId,
                FirstName = existing.FirstName,
                LastName = existing.LastName,
                CompanyName = existing.CompanyName,
                SiteName = existing.SiteName,
                AccountNumber = existing.AccountNumber,
                NetWeightKg = SelectedSendingTicketDetails?.NetWeightKg ?? existing.NetWeightKg,
                TicketStatus = SelectedSendingTicketDetails?.TicketState ?? 'C',
                CreatedTime = existing.CreatedTime
            };

            SendingTicketSearchResults[idx] = updatedRow;
            SelectedSendingTicket = updatedRow;
        }

        StatusMessage = "Ticket finalized.";
    }

    private async Task RemoveSendingLineAsync(SendingLineRow? row)
    {
        if (SelectedSendingTicketDetails == null || row == null) return;

        var ok = await ConfirmAsync("Delete this line item?\n\nThis cannot be undone.");
        if (!ok)
        {
            StatusMessage = "Delete cancelled.";
            return;
        }

        await _ticketSendingService.DeleteTicketSendingLineAsync(SelectedSendingTicketDetails.TicketSendingId, row.TicketSendingLineId);
        await LoadSelectedSendingTicketDetailsAsync(SelectedSendingTicketDetails.TicketSendingId);

        if (SelectedSendingTicketDetails != null)
        {
            CurrentTicketState = SelectedSendingTicketDetails.TicketState;
            TicketFirstWeightText = GetNextWeighbridgeFirstWeightText();
            TicketSecondWeightText = "0.00";
            SendingWeightText = string.Empty;
        }

        StatusMessage = "Line deleted.";
    }

    // TODO: migrate sending ticket state/commands from MainWindowViewModel.TicketsSending

    private sealed class AsyncCommand<T> : ICommand
    {
        private readonly Func<T?, Task> _execute;
        public AsyncCommand(Func<T?, Task> execute) => _execute = execute;
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public async void Execute(object? parameter)
        {
            try { await _execute((T?)parameter); }
            catch (Exception ex) { Console.Error.WriteLine("[ERROR] Sending AsyncCommand<T> failed: " + ex); }
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
                Console.Error.WriteLine("[ERROR] Sending AsyncCommand failed: " + ex);
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
        if (SelectedSendingTicketDetails is null)
            return;

        var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        var owner = lifetime?.MainWindow;
        if (owner is null)
            return;

        var vm = TicketDistributionViewModel.FromSending(SelectedSendingTicketDetails);
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
