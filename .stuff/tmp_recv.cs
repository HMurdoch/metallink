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
    private readonly ProductsService _productsAndPricesService;

    // --- UI Toggles ---
    private bool _receivingIsSearchCriteriaExpanded = true;
    public bool ReceivingIsSearchCriteriaExpanded { get => _receivingIsSearchCriteriaExpanded; set { _receivingIsSearchCriteriaExpanded = value; OnPropertyChanged(); } }
    private bool _receivingIsSearchResultsExpanded;
    public bool ReceivingIsSearchResultsExpanded { get => _receivingIsSearchResultsExpanded; set { _receivingIsSearchResultsExpanded = value; OnPropertyChanged(); } }
    private bool _receivingIsDetailsExpanded;
    public bool ReceivingIsDetailsExpanded { get => _receivingIsDetailsExpanded; set { _receivingIsDetailsExpanded = value; OnPropertyChanged(); } }
    private bool _receivingIsCreateEditExpanded = true;
    public bool ReceivingIsCreateEditExpanded { get => _receivingIsCreateEditExpanded; set { _receivingIsCreateEditExpanded = value; OnPropertyChanged(); } }
    private bool _receivingIsPanelExpanded = true;
    public bool ReceivingIsPanelExpanded { get => _receivingIsPanelExpanded; set { _receivingIsPanelExpanded = value; OnPropertyChanged(); } }

    public TicketsReceivingViewModel(
        TicketReceivingService ticketReceivingService,
        CompanyAndSiteService companyAndSiteService,
        IScaleService scaleService,
        ProductsService productsAndPricesService)
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
        ResetPlatformWeightCommand = new RelayCommand(() => { TicketPlatformWeightText = "0"; ReceivingWeightText = string.Empty; });
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

    public async Task RefreshCompaniesAsync()
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

    public async Task RefreshSitesAsync()
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

    public async Task SearchReceivingTicketsAsync()
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

    public void ClearReceivingTicketSearch()
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

    public async Task SaveTicketAsync()
    {
        if (SelectedReceivingTicketDetails == null) return;
        IsBusy = true;
        try
        {
            var dto = new UpdateTicketReceivingDto
            {
                VehicleRegistration = TicketVehicleRegistration,
                TrailerRegistration = TicketTrailerRegistration,
                DriverName = TicketDriverName,
                OfmWeighbridgeTicket = TicketOfmWeighbridgeTicket,
                ForeignTicket = TicketForeignTicket,
                CkNumber = TicketCkNumber,
                DeliveryNumber = TicketDeliveryNumber,
                Notes = TicketNotes
            };

            var updated = await _ticketReceivingService.UpdateTicketReceivingAsync(SelectedReceivingTicketDetails.TicketReceivingId, dto);
            if (updated != null)
            {
                await LoadSelectedReceivingTicketDetailsAsync(updated.TicketReceivingId);
                StatusMessage = "Ticket saved.";
            }
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

    public async Task ClearTicketAsync()
    {
        ResetCreateSectionForMode();
        StatusMessage = "Create form cleared.";
        await Task.CompletedTask;
    }

    public async Task CaptureWeightAsync()
    {
        if (SelectedTicketTypeOption?.Key == "weighbridge")
        {
            if (CurrentTicketState == 'C') await ReadWeighbridgeAsync();
            else await ReadWeighbridgeSecondAsync();
        }
        else
        {
            await ReadPlatformAsync();
        }
    }

    public async Task CapturePlatePhotoAsync()
    {
        StatusMessage = "Plate photo capture not implemented yet.";
        await Task.CompletedTask;
    }

    public async Task CaptureLoadPhotoAsync()
    {
        StatusMessage = "Load photo capture not implemented yet.";
        await Task.CompletedTask;
    }

    public void ScrollToAddLines()
    {
        StatusMessage = "Please scroll to the line items section.";
    }

    public void ResetPlatformWeight()
    {
        TicketPlatformWeightText = "0.00";
        ReceivingWeightText = string.Empty;
        StatusMessage = "Platform weight reset.";
    }

    public async Task CreateTicketHeaderAsync()
    {
        await CreateReceivingTicketHeaderAsync();
    }

    public async Task ReadWeighbridgeAsync()
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

    public async Task ReadWeighbridgeFirstAsync()
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

    public void ResetCreateSectionForMode()
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

    public void SetCreateTicketTypeFromTicketTypeId(int ticketTypeId)
    {
        // Defensive mapping: 1 = weighbridge, 2 = platform
        var key = ticketTypeId == 1 ? "weighbridge" : "platform";
        SelectedTicketTypeOption = TicketTypeOptions.FirstOrDefault(o => o.Key == key) ?? TicketTypeOptions.FirstOrDefault();
    }

    public async Task LoadSelectedReceivingTicketDetailsAsync(long ticketId)
        => await LoadSelectedReceivingTicketDetailsAsync(ticketId, _receivingDetailsLoadVersion);

    public async Task LoadSelectedReceivingTicketDetailsAsync(long ticketId, long? version = null)
    {
        if (version.HasValue && version.Value != _receivingDetailsLoadVersion)
            return;

        var details = await _ticketReceivingService.GetTicketReceivingByIdAsync(ticketId);

        // Prevent stale async loads from overwriting newer selections
        if (version.HasValue && version.Value != _receivingDetailsLoadVersion)
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
