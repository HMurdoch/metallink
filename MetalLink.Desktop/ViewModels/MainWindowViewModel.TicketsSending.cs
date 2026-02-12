// Ticket Sending ViewModel
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Threading;
using MetalLink.Desktop.Hardware;
using MetalLink.Shared.Buyers;
using MetalLink.Shared.Products;
using MetalLink.Shared.Tickets;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    // Sending line item model used for the grid
    // Mirrors ReceivingLineItem so the UI and behaviours match exactly.
    public sealed class SendingLineItem : System.ComponentModel.INotifyPropertyChanged
    {
        private decimal _tare;

        public long TicketSendingLineId { get; init; }
        public long TicketSendingId { get; init; }
        public long ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;

        // For weighbridge columns in the UI. Sending lines don't currently store these per-line,
        // but we keep them for UI parity.
        public decimal FirstWeightKg { get; init; }
        public decimal SecondWeightKg { get; init; }

        // Stored net weight
        public decimal WeightKg { get; init; }

        public decimal UnitPricePerKg { get; init; }
        public decimal LineTotal { get; init; }
        public decimal VatAmount { get; init; }
        public decimal TotalInclVat { get; init; }

        public decimal Tare
        {
            get => _tare;
            set
            {
                if (_tare != value)
                {
                    _tare = value;
                    OnPropertyChanged(nameof(Tare));
                    OnPropertyChanged(nameof(DisplayFirstWeightKg));
                    OnPropertyChanged(nameof(DisplayWeightKg));
                }
            }
        }

        // Display properties that account for Tare deduction
        public decimal DisplayFirstWeightKg => FirstWeightKg - Tare;
        public decimal DisplayWeightKg => WeightKg - Tare;

        public string Notes { get; init; } = string.Empty;

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Collection with just the sending line items (no totals row - shown separately below grid)
    /// </summary>
    public ObservableCollection<SendingLineItem> SendingLinesWithTotals => SendingLines;

    private string _sendingLineNotes = string.Empty;
    public string SendingLineNotes
    {
        get => _sendingLineNotes;
        set
        {
            _sendingLineNotes = value;
            OnPropertyChanged();
        }
    }

    private ObservableCollection<SendingLineItem> _sendingLines = new();
    public ObservableCollection<SendingLineItem> SendingLines
    {
        get => _sendingLines;
        set
        {
            _sendingLines = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedTicketSending));
            RecalculateSendingTotals();
        }
    }

    public bool HasUnsavedTicketSending => 
        _sendingLines.Count > 0 || 
        !string.IsNullOrWhiteSpace(SendingWeightText) ||
        SendingSelectedProduct != null;

    private ObservableCollection<ProductLookupDto> _sendingProductSuggestions = new();
    public ObservableCollection<ProductLookupDto> SendingProductSuggestions
    {
        get => _sendingProductSuggestions;
        set
        {
            _sendingProductSuggestions = value;
            OnPropertyChanged();
        }
    }

    private ProductLookupDto? _sendingSelectedProduct;
    public ProductLookupDto? SendingSelectedProduct
    {
        get => _sendingSelectedProduct;
        set
        {
            _sendingSelectedProduct = value;
            OnPropertyChanged();
        }
    }

    private string _sendingProductSearchText = string.Empty;
    public string SendingProductSearchText
    {
        get => _sendingProductSearchText;
        set
        {
            _sendingProductSearchText = value;
            OnPropertyChanged();
            
            // When text search changes, clear letter filter to null/empty
            // This allows substring search instead of showing ALL products
            if (!string.IsNullOrWhiteSpace(value))
            {
                _selectedSendingProductLetter = string.Empty;
                OnPropertyChanged(nameof(SelectedSendingProductLetter));
            }
            
            _ = SearchSendingProductsAsync(value);
        }
    }

    private string _selectedSendingProductLetter = "ALL";
    public string SelectedSendingProductLetter
    {
        get => _selectedSendingProductLetter;
        set
        {
            _selectedSendingProductLetter = value;
            OnPropertyChanged();
            
            // When letter filter is selected, clear text search
            if (!string.IsNullOrWhiteSpace(value))
            {
                _sendingProductSearchText = string.Empty;
                OnPropertyChanged(nameof(SendingProductSearchText));
            }
            
            _ = ApplySendingProductFilterAsync();
        }
    }

    private string _sendingWeightText = string.Empty;
    public string SendingWeightText
    {
        get => _sendingWeightText;
        set
        {
            _sendingWeightText = value;
            OnPropertyChanged();
        }
    }

    private decimal _sendingTotalExclVat;
    public decimal SendingTotalExclVat
    {
        get => _sendingTotalExclVat;
        private set
        {
            _sendingTotalExclVat = value;
            OnPropertyChanged();
        }
    }

    private decimal _sendingTotalVat;
    public decimal SendingTotalVat
    {
        get => _sendingTotalVat;
        private set
        {
            _sendingTotalVat = value;
            OnPropertyChanged();
        }
    }

    private decimal _sendingTotalInclVat;
    public decimal SendingTotalInclVat
    {
        get => _sendingTotalInclVat;
        private set
        {
            _sendingTotalInclVat = value;
            OnPropertyChanged();
        }
    }

    private async Task SearchSendingProductsAsync(string? term)
    {
        try
        {
            var results = await _app.ProductsAndPricesService.LookupProductsAsync(
                string.IsNullOrWhiteSpace(term) ? string.Empty : term);

            SendingProductSuggestions.Clear();
            foreach (var p in results)
            {
                SendingProductSuggestions.Add(p);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error searching products: {ex.Message}";
        }
    }

    private async Task EnsureSendingProductsLoadedAsync()
    {
        // If dropdown already has values, don't spam the API.
        if (SendingProductSuggestions.Count > 0)
            return;

        // If user is actively searching, don't override.
        if (!string.IsNullOrWhiteSpace(SendingProductSearchText))
            return;

        // Force "ALL" letter filter to run and populate initial list.
        _selectedSendingProductLetter = "ALL";
        OnPropertyChanged(nameof(SelectedSendingProductLetter));
        await ApplySendingProductFilterAsync();
    }

    private async Task ApplySendingProductFilterAsync()
    {
        try
        {
            // If letter filter is empty/null, don't load anything
            // The search text will handle the filtering via SearchSendingProductsAsync
            if (string.IsNullOrWhiteSpace(SelectedSendingProductLetter))
            {
                return;
            }
            
            // Load all products from API
            var results = await _app.ProductsAndPricesService.LookupProductsAsync(string.Empty);
            
            SendingProductSuggestions.Clear();
            
            if (SelectedSendingProductLetter == "ALL")
            {
                // Show all products
                foreach (var p in results)
                {
                    SendingProductSuggestions.Add(p);
                }
            }
            else
            {
                // Filter by first letter
                var filtered = results
                    .Where(p => p.ProductName != null && 
                           p.ProductName.StartsWith(SelectedSendingProductLetter, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var p in filtered)
                {
                    SendingProductSuggestions.Add(p);
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error filtering products: {ex.Message}";
        }
    }

    private async Task LoadSendingLinesForTicketAsync(long ticketId)
    {
        if (ticketId <= 0)
        {
            SendingLines.Clear();
            RecalculateSendingTotals();
            return;
        }

        try
        {
            var lines = await _ticketSendingService.GetTicketSendingLinesAsync(ticketId);

            SendingLines.Clear();
            if (lines != null)
            {
                foreach (var dto in lines)
                {
                    var lineItem = new SendingLineItem
                    {
                        TicketSendingLineId = dto.TicketSendingLineId,
                        TicketSendingId = dto.TicketSendingId,
                        ProductId = dto.ProductId,
                        ProductName = dto.ProductName,
                        FirstWeightKg = dto.FirstWeightKg ?? 0m,
                        SecondWeightKg = dto.SecondWeightKg ?? 0m,
                        WeightKg = dto.NetWeightKg,
                        UnitPricePerKg = dto.UnitPricePerKg,
                        LineTotal = dto.LineTotal,
                        VatAmount = dto.VatAmount,
                        TotalInclVat = dto.TotalInclVat,
                        Notes = dto.Notes ?? string.Empty
                    };
                    lineItem.Tare = dto.Tare;
                    lineItem.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(SendingLineItem.Tare))
                        {
                            RecalculateSendingTotals();
                            _ = _ticketSendingService.UpdateLineTareAsync(dto.TicketSendingId, dto.TicketSendingLineId, lineItem.Tare);
                        }
                    };
                    SendingLines.Add(lineItem);
                }
            }

            RecalculateSendingTotals();

            StatusMessage = $"Loaded {SendingLines.Count} line(s) for ticket {ticketId}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading ticket lines: {ex.Message}";
        }
    }

    private async Task AddSendingLineAsync()
    {
        if (IsBusy) return;

        if (LastCreatedTicket == null || LastCreatedTicket.TicketId <= 0)
        {
            StatusMessage = "Please create a ticket header before adding lines.";
            return;
        }

        if (SendingSelectedProduct == null)
        {
            StatusMessage = "Please select a product for the line.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SendingWeightText))
        {
            StatusMessage = "Weight (kg) is required.";
            return;
        }

        // For Weighbridge sending tickets, Second Weight is required to create a line
        if (SelectedTicketTypeOption?.Key == "weighbridge")
        {
            if (string.IsNullOrWhiteSpace(TicketSecondWeightText) || TicketSecondWeightText == "0")
            {
                StatusMessage = "For Weighbridge tickets, Second Weight must be set before adding a line.";
                return;
            }
        }

        if (!decimal.TryParse(
                NormalizeDecimalText(SendingWeightText),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var weightKg) || weightKg <= 0)
        {
            StatusMessage = "Weight must be a valid number greater than zero.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Adding ticket line...";

        try
        {
            // For sending: unit price comes from buyer price code on the server when UnitPricePerKg = 0.
            var dto = new CreateTicketSendingLineDto
            {
                ProductId = SendingSelectedProduct.ProductId,
                NetWeightKg = weightKg,
                UnitPricePerKg = 0m,
                Notes = string.IsNullOrWhiteSpace(SendingLineNotes) ? null : SendingLineNotes,
                // For weighbridge tickets, the API requires SecondWeightKg to compute the line net weight.
                SecondWeightKg = SelectedTicketTypeOption?.Key == "weighbridge" && decimal.TryParse(NormalizeDecimalText(TicketSecondWeightText ?? ""), out var sw)
                    ? sw
                    : null,
                Tare = 0m
            };

            var updatedTicket = await _ticketSendingService.AddTicketSendingLineAsync(LastCreatedTicket.TicketId, dto);
            if (updatedTicket == null)
            {
                StatusMessage = "Ticket line create failed - API returned no result.";
                return;
            }

            // Reload lines from the updated ticket so the grid matches the server
            SendingLines.Clear();
            foreach (var lineDto in updatedTicket.Lines)
            {
                var lineItem = new SendingLineItem
                {
                    TicketSendingLineId = lineDto.TicketSendingLineId,
                    TicketSendingId = lineDto.TicketSendingId,
                    ProductId = lineDto.ProductId,
                    ProductName = lineDto.ProductName,
                    FirstWeightKg = lineDto.FirstWeightKg ?? 0m,
                    SecondWeightKg = lineDto.SecondWeightKg ?? 0m,
                    WeightKg = lineDto.NetWeightKg,
                    UnitPricePerKg = lineDto.UnitPricePerKg,
                    LineTotal = lineDto.LineTotal,
                    VatAmount = lineDto.VatAmount,
                    TotalInclVat = lineDto.TotalInclVat,
                    Notes = lineDto.Notes ?? string.Empty
                };
                lineItem.Tare = lineDto.Tare;
                lineItem.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(SendingLineItem.Tare))
                    {
                        RecalculateSendingTotals();
                        _ = _ticketSendingService.UpdateLineTareAsync(lineDto.TicketSendingId, lineDto.TicketSendingLineId, lineItem.Tare);
                    }
                };
                SendingLines.Add(lineItem);
            }

            // Set state to 'M' after first line is added (match Receiving behaviour)
            CurrentTicketState = 'M';

            RecalculateSendingTotals();

            StatusMessage = $"Added 1 line(s) to ticket {LastCreatedTicket.TicketNumber}.";

            // Reset weight and notes for next entry, keep product selection
            SendingWeightText = string.Empty;
            SendingLineNotes = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error adding ticket line: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private decimal _sendingLinesTotalWeight;
    public decimal SendingLinesTotalWeight
    {
        get => _sendingLinesTotalWeight;
        private set
        {
            _sendingLinesTotalWeight = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CreatingTicketTotalWeight));
        }
    }


    private void RecalculateSendingTotals()
    {
        var totalExcl = 0m;
        var totalVat = 0m;
        var totalIncl = 0m;

        decimal totalWeight = 0m;
        decimal totalTare = 0m;

        foreach (var line in SendingLines)
        {
            totalExcl += line.LineTotal;
            totalVat += line.VatAmount;
            totalIncl += line.TotalInclVat;
            totalWeight += line.WeightKg;
            totalTare += line.Tare;
        }

        SendingTotalExclVat = totalExcl;
        SendingTotalVat = totalVat;
        SendingTotalInclVat = totalIncl;

        // Match Receiving: show net total as SUM(weights) - SUM(tare)
        SendingLinesTotalWeight = totalWeight - totalTare;

        // Notify state-driven buttons (Finalize enabled when H/M)
        OnPropertyChanged(nameof(IsFinalizeTicketEnabled));
    }

    private async Task RemoveSendingLineAsync(SendingLineItem? line)
    {
        if (line == null)
        {
            StatusMessage = "No line item selected to remove.";
            return;
        }
        
        if (IsBusy) return;

        IsBusy = true;
        StatusMessage = "Removing ticket line...";

        try
        {
            await _ticketSendingService.DeleteTicketSendingLineAsync(line.TicketSendingId, line.TicketSendingLineId);

            SendingLines.Remove(line);

            // If no lines remain, return to Header state
            if (SendingLines.Count == 0)
            {
                CurrentTicketState = 'H';
            }

            RecalculateSendingTotals();

            StatusMessage = $"✓ Removed line for product {line.ProductName}";
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = $"Network error removing ticket line: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error removing ticket line: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Task SaveSendingTicketAsync()
    {
        if (IsBusy) return Task.CompletedTask;

        if (LastCreatedTicket == null || LastCreatedTicket.TicketId <= 0)
        {
            StatusMessage = "No ticket to save.";
            return Task.CompletedTask;
        }

        IsBusy = true;
        StatusMessage = "Saving ticket...";

        try
        {
            // Ticket is saved on the server side when lines are added
            // This command could trigger final validation or status update
            StatusMessage = $"Ticket {LastCreatedTicket.TicketNumber} saved successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving ticket: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
        
        return Task.CompletedTask;
    }

    private Task ClearSendingTicketAsync()
    {
        ClearSendingTicket();
        return Task.CompletedTask;
    }

    private void ClearSendingTicket()
    {
        LastCreatedTicket = null;
        SendingLines.Clear();
        SendingWeightText = string.Empty;
        SendingSelectedProduct = null;
        SendingProductSearchText = string.Empty;
        SendingLineNotes = string.Empty;

        // Reset all weights to 0
        TicketFirstWeightText = "0";
        TicketSecondWeightText = "0";
        TicketPlatformWeightText = "0";

        RecalculateSendingTotals();
        
        // Initialize type options and set defaults
        InitializeTicketTypeOptions();
        SelectedTicketTypeOption = TicketTypeOptions.FirstOrDefault(t => t.Key == "platform");
        
        // Initialize currency options and set default to ZAR
        InitializeCurrencyOptions();
        SelectedCurrency = "ZAR";
        
        CreateOrUpdateButtonText = "Create Ticket";
        
        StatusMessage = "Ticket cleared.";
    }

    private async Task CaptureSendingWeightAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        StatusMessage = "Capturing weight from scale...";

        try
        {
            var reading = await _app.ScaleService.ReadOnceAsync(ScaleDeviceType.Weighbridge);
            if (reading != null)
            {
                SendingWeightText = reading.WeightKg.ToString("F2");
                StatusMessage = $"Weight captured: {reading.WeightKg:F2} kg";
            }
            else
            {
                StatusMessage = "Failed to capture weight: No reading available";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error capturing weight: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CaptureSendingPlatePhotoAsync()
    {
        if (IsBusy) return;

        StatusMessage = "Capturing plate photo...";
        // TODO: Implement plate photo capture
        await Task.Delay(100);
        StatusMessage = "Plate photo capture not yet implemented.";
    }

    private async Task CaptureSendingLoadPhotoAsync()
    {
        if (IsBusy) return;

        StatusMessage = "Capturing load photo...";
        // TODO: Implement load photo capture
        await Task.Delay(100);
        StatusMessage = "Load photo capture not yet implemented.";
    }

    // --- Sending Ticket Search Properties ---

    private string _searchSendingTicketCustomerIdText = string.Empty;
    private string _searchSendingTicketIdNumberText = string.Empty;
    private string _searchSendingTicketFirstNameText = string.Empty;
    private string _searchSendingTicketLastNameText = string.Empty;
    private string _searchSendingTicketAccountNumberText = string.Empty;
    private string _searchSendingTicketNumberText = string.Empty;
    private string _searchSendingTicketTypeKey = "All";
    private string _searchSendingTicketCreatedFromText = string.Empty;
    private string _searchSendingTicketCreatedToText = string.Empty;

    public string SearchSendingTicketCustomerIdText
    {
        get => _searchSendingTicketCustomerIdText;
        set { _searchSendingTicketCustomerIdText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchSendingTicketIdNumberText
    {
        get => _searchSendingTicketIdNumberText;
        set { _searchSendingTicketIdNumberText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchSendingTicketFirstNameText
    {
        get => _searchSendingTicketFirstNameText;
        set { _searchSendingTicketFirstNameText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchSendingTicketLastNameText
    {
        get => _searchSendingTicketLastNameText;
        set { _searchSendingTicketLastNameText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchSendingTicketAccountNumberText
    {
        get => _searchSendingTicketAccountNumberText;
        set { _searchSendingTicketAccountNumberText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchSendingTicketNumberText
    {
        get => _searchSendingTicketNumberText;
        set { _searchSendingTicketNumberText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchSendingTicketTypeKey
    {
        get => _searchSendingTicketTypeKey;
        set { _searchSendingTicketTypeKey = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchSendingTicketCreatedFromText
    {
        get => _searchSendingTicketCreatedFromText;
        set { _searchSendingTicketCreatedFromText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchSendingTicketCreatedToText
    {
        get => _searchSendingTicketCreatedToText;
        set { _searchSendingTicketCreatedToText = value ?? string.Empty; OnPropertyChanged(); }
    }

    // --- Sending Search Results ---

    private bool _searchSendingNewBuyersCheckbox;
    public bool SearchSendingNewBuyersCheckbox
    {
        get => _searchSendingNewBuyersCheckbox;
        set
        {
            _searchSendingNewBuyersCheckbox = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedSendingTicket));
            OnPropertyChanged(nameof(ShouldShowTicketDetails));
        }
    }

    public ObservableCollection<TicketSearchResultDto> SendingTicketSearchResults { get; } = new();

    // Buyers without tickets (for "New Buyer?")
    public ObservableCollection<NewBuyerResultDto> SendingNewBuyerSearchResults { get; } = new();

    private NewBuyerResultDto? _selectedSendingNewBuyer;
    public NewBuyerResultDto? SelectedSendingNewBuyer
    {
        get => _selectedSendingNewBuyer;
        set
        {
            _selectedSendingNewBuyer = value;
            OnPropertyChanged();

            if (value != null)
            {
                // Format matches Receiving: "ID | First Last | Company | Site"
                var fullBuyerInfo = string.IsNullOrWhiteSpace(value.CompanyName)
                    ? $"{value.BuyerId} | {value.FirstName} {value.LastName}"
                    : $"{value.BuyerId} | {value.FirstName} {value.LastName} | {value.CompanyName} | {value.SiteName}";

                TicketCustomerIdText = fullBuyerInfo;
                // Also set the raw buyer id text box used by create header
                TicketCustomerIdText = fullBuyerInfo;

                // Default to Platform for new buyers
                InitializeTicketTypeOptions();
                SelectedTicketTypeOption = TicketTypeOptions.FirstOrDefault(t => t.Key == "platform");

                StatusMessage = $"Buyer {value.FirstName} {value.LastName} selected. You can now create a ticket.";
            }
        }
    }

    private TicketSearchResultDto? _selectedSendingTicket;
    public TicketSearchResultDto? SelectedSendingTicket
    {
        get => _selectedSendingTicket;
        set
        {
            _selectedSendingTicket = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedSendingTicketSummary));
            OnPropertyChanged(nameof(HasSelectedSendingTicket));
            OnPropertyChanged(nameof(ShouldShowTicketDetails));
            
            if (value != null)
            {
                _ = LoadSelectedSendingTicketDetailsAsync(value.TicketId);
            }
            else
            {
                SelectedSendingTicketLines.Clear();
                SelectedSendingTicketDetails = null;
            }
        }
    }

    public bool HasSelectedSendingTicket => SelectedSendingTicket != null;

    public string SelectedSendingTicketSummary
    {
        get
        {
            if (SelectedSendingTicket is null)
                return "No ticket selected.";

            return $"Ticket {SelectedSendingTicket.TicketNumber} ({SelectedSendingTicket.TicketType}) - " +
                   $"Customer {SelectedSendingTicket.CustomerId}, Net {SelectedSendingTicket.NetWeightKg:N2} kg, " +
                   $"Total {SelectedSendingTicketLinesTotalInclVat:N2}";
        }
    }

    private TicketDto? _selectedSendingTicketDetails;
    public TicketDto? SelectedSendingTicketDetails
    {
        get => _selectedSendingTicketDetails;
        set
        {
            _selectedSendingTicketDetails = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CalculatedNetWeightKg));
        }
    }

    public ObservableCollection<TicketLineDto> SelectedSendingTicketLines { get; } = new();

    public decimal SelectedSendingTicketLinesTotalExVat => SelectedSendingTicketLines.Sum(l => l.LineTotal);
    public decimal SelectedSendingTicketLinesTotalVat => SelectedSendingTicketLines.Sum(l => l.VatAmount);
    public decimal SelectedSendingTicketLinesTotalInclVat => SelectedSendingTicketLines.Sum(l => l.TotalInclVat);

    /// <summary>
    /// Collection with just the line items (no totals row - shown separately below grid)
    /// Kept to mirror Receiving so the AXAML layout can match exactly.
    /// </summary>
    public ObservableCollection<TicketLineDto> SelectedSendingTicketLinesWithTotals => SelectedSendingTicketLines;

    private async Task LoadSelectedSendingTicketDetailsAsync(long ticketId)
    {
        try
        {
            var details = await _ticketSendingService.GetTicketSendingByIdAsync(ticketId);

            if (details == null)
            {
                StatusMessage = "Failed to load ticket details.";
                SelectedSendingTicketLines.Clear();
                SelectedSendingTicketDetails = null;
                return;
            }

            // Map API DTO -> shared TicketDto (so the UI can match Receiving exactly)
            SelectedSendingTicketDetails = new TicketDto
            {
                TicketId = details.TicketSendingId,
                BuyerId = details.BuyerId,
                CustomerId = details.BuyerId, // NOTE: TicketDto uses CustomerId in the UI; keep for compatibility
                TicketNumber = details.TicketNumber,
                TicketTypeId = details.TicketTypeId,
                TicketType = details.TicketTypeName,
                FirstWeightKg = details.FirstWeightKg,
                SecondWeightKg = details.SecondWeightKg,
                NetWeightKg = details.NetWeightKg,
                VehicleRegistration = details.VehicleRegistration,
                TrailerRegistration = details.TrailerRegistration,
                DriverName = details.DriverName,
                OfmWeighbridgeTicket = details.OfmWeighbridgeTicket,
                ForeignTicket = details.ForeignTicket,
                CkNumber = details.CkNumber,
                DeliveryNumber = details.DeliveryNumber,
                Notes = details.Notes,
                CreatedTime = details.CreatedTime,
                UpdatedTime = details.UpdatedTime
            };

            // Populate form fields from loaded ticket
            // IMPORTANT: treat any selected ticket as "viewing" so changing TicketType does NOT regenerate numbers.
            // (Create section must show the selected ticket number for H/M.)
            IsViewingTicketOnly = true;

            // Populate common header fields
            // Format matches Receiving: "ID | First Last | Company | Site" where available.
            if (SelectedSendingTicket != null)
            {
                var fullBuyerInfo = string.IsNullOrWhiteSpace(SelectedSendingTicket.CompanyName)
                    ? $"{SelectedSendingTicket.CustomerId} | {SelectedSendingTicket.FirstName} {SelectedSendingTicket.LastName}"
                    : $"{SelectedSendingTicket.CustomerId} | {SelectedSendingTicket.FirstName} {SelectedSendingTicket.LastName} | {SelectedSendingTicket.CompanyName} | {SelectedSendingTicket.SiteName}";

                TicketCustomerIdText = fullBuyerInfo;
            }
            else
            {
                TicketCustomerIdText = details.BuyerId.ToString();
            }

            TicketNumber = details.TicketNumber;
            TicketNotes = details.Notes ?? string.Empty;

            // Weighbridge-only header fields
            TicketVehicleRegistration = details.VehicleRegistration ?? string.Empty;
            TicketTrailerRegistration = details.TrailerRegistration ?? string.Empty;
            TicketDriverName = details.DriverName ?? string.Empty;
            TicketOfmWeighbridgeTicket = details.OfmWeighbridgeTicket ?? string.Empty;
            TicketForeignTicket = details.ForeignTicket ?? string.Empty;
            TicketCkNumber = details.CkNumber ?? string.Empty;
            TicketDeliveryNumber = details.DeliveryNumber ?? string.Empty;

            // Set ticket type
            InitializeTicketTypeOptions();
            var ticketTypeKey = details.TicketTypeName?.ToLower() ?? string.Empty;
            var ticketTypeOption = TicketTypeOptions.FirstOrDefault(t => string.Equals(t.Key, ticketTypeKey, StringComparison.OrdinalIgnoreCase));
            if (ticketTypeOption != null)
                SelectedTicketTypeOption = ticketTypeOption;

            // Ensure product dropdown is pre-populated (ALL) when adding lines
            await EnsureSendingProductsLoadedAsync();

            // Weights: match Receiving behaviour
            // H: FirstWeightText comes from InitializeWeightKg
            // M: FirstWeightText comes from last line's SecondWeightKg
            // (C is view-only)
            if (details.TicketTypeName?.Equals("weighbridge", StringComparison.OrdinalIgnoreCase) == true)
            {
                TicketFirstWeightText = details.InitializeWeightKg?.ToString("0.00") ?? "0";
                TicketSecondWeightText = "0";

                if (details.TicketState == 'M' && details.Lines != null && details.Lines.Count > 0)
                {
                    // Only consider ACTIVE lines; if none remain active, fall back to InitializeWeightKg
                    var lastActiveLine = details.Lines
                        .Where(l => l.IsActive)
                        .OrderBy(l => l.CreatedTime)
                        .LastOrDefault();

                    if (lastActiveLine != null)
                    {
                        TicketFirstWeightText = (lastActiveLine.SecondWeightKg ?? 0m).ToString("0.00");
                    }
                }
            }
            else
            {
                TicketFirstWeightText = "0";
                TicketSecondWeightText = "0";
            }

            // Platform uses TicketPlatformWeightText (show net weight as the platform weight)
            TicketPlatformWeightText = details.NetWeightKg.ToString("0.00");

            // Load lines from the ticket details (already included in DTO) into shared TicketLineDto
            SelectedSendingTicketLines.Clear();
            if (details.Lines != null && details.Lines.Count > 0)
            {
                foreach (var line in details.Lines)
                {
                    SelectedSendingTicketLines.Add(new TicketLineDto
                    {
                        TicketLineId = line.TicketSendingLineId,
                        TicketId = line.TicketSendingId,
                        ProductId = line.ProductId,
                        ProductName = line.ProductName,
                        WeightKg = line.NetWeightKg,
                        UnitPricePerKg = line.UnitPricePerKg,
                        LineTotal = line.LineTotal,
                        VatAmount = line.VatAmount,
                        TotalInclVat = line.TotalInclVat,
                        Tare = line.Tare,
                        Notes = line.Notes ?? string.Empty,
                        CreatedTime = line.CreatedTime,
                        UpdatedTime = line.CreatedTime
                    });
                }
            }

            // Reflect actual state returned by API (H/M/C)
            CurrentTicketState = details.TicketState;
            CreateOrUpdateButtonText = details.TicketState == 'H' ? "Save & Reset" : "Create Header";

            if (details.TicketState == 'C')
            {
                // Complete tickets are view-only: clear create/edit line grid and prepare next ticket number
                LastCreatedTicket = null;
                SendingLines.Clear();
                RecalculateSendingTotals();

                var completeTicketTypeKey = details.TicketNumber?.StartsWith("SWB") == true ? "weighbridge" : "platform";
                var prefix = completeTicketTypeKey == "weighbridge" ? "SWB" : "SPL";
                await GenerateTicketNumberForSendingAsync(prefix);
            }
            else
            {
                // Incomplete tickets: load the create/edit grid lines (editable)
                LastCreatedTicket = new TicketDto
                {
                    TicketId = details.TicketSendingId,
                    TicketNumber = details.TicketNumber,
                    TicketState = details.TicketState,
                    InitializeWeightKg = details.InitializeWeightKg
                };

                await LoadSendingLinesForTicketAsync(details.TicketSendingId);
            }

            // Notify bindings
            OnPropertyChanged(nameof(AreWeighbridgeFieldsVisible));
            OnPropertyChanged(nameof(ArePlatformFieldsVisible));
            OnPropertyChanged(nameof(CalculatedNetWeightKg));
            OnPropertyChanged(nameof(SelectedSendingTicketLinesTotalExVat));
            OnPropertyChanged(nameof(SelectedSendingTicketLinesTotalVat));
            OnPropertyChanged(nameof(SelectedSendingTicketLinesTotalInclVat));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading ticket details: {ex.Message}";
        }
    }

    private long ExtractBuyerIdFromText(string buyerIdText)
    {
        // Format: "BuyerId | First Last" or "BuyerId | First Last | Company | Site"
        if (string.IsNullOrWhiteSpace(buyerIdText))
            return 0;

        var parts = buyerIdText.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return 0;

        return long.TryParse(parts[0], out var id) ? id : 0;
    }

    private async Task CreateSendingTicketHeaderAsync()
    {
        // Match Receiving: if currently 'H', treat as "Save & Reset"
        if (CurrentTicketState == 'H')
        {
            await SaveAndResetSendingTicketAsync();
            return;
        }

        if (string.IsNullOrWhiteSpace(TicketCustomerIdText))
        {
            StatusMessage = "Please select a buyer before creating the ticket header.";
            return;
        }

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
            int ticketTypeId = SelectedTicketTypeOption?.Key == "weighbridge" ? 1 : 2;

            // Generate a number for display (API will also generate its own and return it)
            var prefix = ticketTypeId == 1 ? "SWB" : "SPL";
            await GenerateTicketNumberForSendingAsync(prefix);

            long buyerId = ExtractBuyerIdFromText(TicketCustomerIdText);
            if (buyerId <= 0)
            {
                StatusMessage = "Invalid buyer ID.";
                return;
            }

            var isWeighbridgeTicket = ticketTypeId == 1;

            decimal? firstWeight = null;
            decimal? secondWeight = null;
            if (isWeighbridgeTicket)
            {
                var normalizedFw = NormalizeDecimalText(TicketFirstWeightText ?? "");
                if (decimal.TryParse(normalizedFw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var fw))
                    firstWeight = fw;

                if (!string.IsNullOrWhiteSpace(TicketSecondWeightText) && TicketSecondWeightText != "0" &&
                    decimal.TryParse(NormalizeDecimalText(TicketSecondWeightText), out var sw))
                    secondWeight = sw;
            }

            var createDto = new CreateTicketSendingDto
            {
                BuyerId = (int)buyerId,
                TicketTypeId = ticketTypeId,
                TicketNumber = TicketNumber ?? string.Empty,
                InvoiceNumber = 0,
                // Creating header-only ticket: state must be 'H'
                // and only the initial weight is required for weighbridge.
                TicketState = 'H',
                InitializeWeightKg = isWeighbridgeTicket ? firstWeight : null,
                FirstWeightKg = null,
                SecondWeightKg = null,
                NetWeightKg = 0m,
                VehicleRegistration = isWeighbridgeTicket ? TicketVehicleRegistration : null,
                TrailerRegistration = isWeighbridgeTicket ? TicketTrailerRegistration : null,
                DriverName = isWeighbridgeTicket ? TicketDriverName : null,
                OfmWeighbridgeTicket = isWeighbridgeTicket ? TicketOfmWeighbridgeTicket : null,
                ForeignTicket = isWeighbridgeTicket ? TicketForeignTicket : null,
                CkNumber = isWeighbridgeTicket ? TicketCkNumber : null,
                DeliveryNumber = isWeighbridgeTicket ? TicketDeliveryNumber : null,
                Notes = TicketNotes,
                CreatedByOperatorId = 1 // TODO: authenticated operator
            };

            var response = await _ticketSendingService.CreateTicketSendingAsync(createDto);
            if (response == null || response.TicketSendingId <= 0)
            {
                StatusMessage = "Failed to create ticket header.";
                return;
            }

            LastCreatedTicket = new TicketDto
            {
                TicketId = response.TicketSendingId,
                CustomerId = response.BuyerId,
                BuyerId = response.BuyerId,
                TicketNumber = response.TicketNumber,
                TicketTypeId = response.TicketTypeId,
                TicketType = response.TicketTypeName,
                NetWeightKg = response.NetWeightKg,
                TicketState = response.TicketState
            };

            CurrentTicketState = response.TicketState;
            SearchSendingNewBuyersCheckbox = false;

            await LoadSelectedSendingTicketDetailsAsync(response.TicketSendingId);
            await EnsureSendingProductsLoadedAsync();

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

    private async Task SaveAndResetSendingTicketAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            if (LastCreatedTicket?.TicketId > 0)
            {
                StatusMessage = "Saving ticket changes...";

                int ticketTypeId = SelectedTicketTypeOption?.Key == "weighbridge" ? 1 : 2;
                long buyerId = ExtractBuyerIdFromText(TicketCustomerIdText);

                var updateDto = new CreateTicketSendingDto
                {
                    BuyerId = (int)buyerId,
                    TicketTypeId = ticketTypeId,
                    TicketNumber = TicketNumber ?? string.Empty,
                    InvoiceNumber = 0,
                    InitializeWeightKg = ticketTypeId == 1 && decimal.TryParse(NormalizeDecimalText(TicketFirstWeightText ?? ""), out var initW) ? initW : LastCreatedTicket.InitializeWeightKg,
                    FirstWeightKg = null,
                    SecondWeightKg = null,
                    NetWeightKg = LastCreatedTicket.NetWeightKg,
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

                var result = await _ticketSendingService.UpdateTicketSendingAsync(LastCreatedTicket.TicketId, updateDto);
                StatusMessage = result != null ? "✓ Ticket saved successfully." : "Warning: Could not save ticket to database, but clearing form anyway.";
            }

            // Clear selected ticket so Details section resets (match Receiving expectation)
            SelectedSendingTicket = null;
            SelectedSendingTicketDetails = null;
            SelectedSendingTicketLines.Clear();
            OnPropertyChanged(nameof(HasSelectedSendingTicket));
            OnPropertyChanged(nameof(ShouldShowTicketDetails));

            await ClearSendingTicketAsync();
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

    private async Task FinalizeSendingTicketAsync()
    { 
        if (IsBusy) return;

        if (LastCreatedTicket == null || LastCreatedTicket.TicketId <= 0)
        {
            StatusMessage = "No ticket to finalize.";
            return;
        }

        if (CurrentTicketState != 'M')
        {
            StatusMessage = "Ticket can only be finalized if it has at least one line item.";
            return;
        }

        var confirmMessage = $"Are you sure you want to Finalize this ticket for Buyer: {TicketCustomerIdText}";
        var ok = await ConfirmAsync(confirmMessage);
        if (!ok) return;

        IsBusy = true;
        StatusMessage = "Finalizing ticket...";

        try
        {
            var success = await _ticketSendingService.UpdateTicketStateAsync(LastCreatedTicket.TicketId, 'C');
            if (!success)
            {
                StatusMessage = "Error updating ticket status to Complete. Please try again.";
                return;
            }

            StatusMessage = $"✓ Ticket {LastCreatedTicket.TicketNumber} finalized successfully.";
            await ClearSendingTicketAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error finalizing ticket: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SearchSendingTicketsAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        StatusMessage = "Searching sending tickets...";
        
        // Initialize ticket type options for search dropdown
        InitializeTicketTypeOptions();

        try
        {
            // Use the *search* ticket type dropdown (SelectedSearchTicketTypeOption)
            // not the create/edit ticket type (SelectedTicketTypeOption).
            var ticketTypeKey = SelectedSearchTicketTypeOption?.Key;
            var ticketType = string.IsNullOrWhiteSpace(ticketTypeKey) || ticketTypeKey.Equals("all", StringComparison.OrdinalIgnoreCase)
                ? null
                : ticketTypeKey;
            var request = new TicketSendingSearchRequestDto
            {
                CompanyId = SearchTicketSelectedCompany?.CompanyId,
                SiteId = SearchTicketSelectedSite?.SiteId,
                TicketType = ticketType,
                BuyerId = ParseIntOrNull(SearchSendingTicketCustomerIdText),
                FirstName = string.IsNullOrWhiteSpace(SearchSendingTicketFirstNameText) ? null : SearchSendingTicketFirstNameText.Trim(),
                LastName = string.IsNullOrWhiteSpace(SearchSendingTicketLastNameText) ? null : SearchSendingTicketLastNameText.Trim(),
                IdNumber = string.IsNullOrWhiteSpace(SearchSendingTicketIdNumberText) ? null : SearchSendingTicketIdNumberText.Trim(),
                AccountNumber = ParseLongOrNull(SearchSendingTicketAccountNumberText),
                SearchTerm = string.IsNullOrWhiteSpace(SearchSendingTicketNumberText) ? null : SearchSendingTicketNumberText.Trim(),
                StartDate = ParseDateOrNull(SearchSendingTicketCreatedFromText),
                EndDate = ParseDateOrNull(SearchSendingTicketCreatedToText)
            };

            if (SearchSendingNewBuyersCheckbox)
            {
                StatusMessage = "Searching for buyers without tickets...";

                var results = await _ticketSendingService.SearchNewBuyersWithoutTicketsAsync(new TicketSearchRequestDto
                {
                    BuyerId = ParseLongOrNull(SearchSendingTicketCustomerIdText),
                    FirstName = string.IsNullOrWhiteSpace(SearchSendingTicketFirstNameText) ? null : SearchSendingTicketFirstNameText.Trim(),
                    LastName = string.IsNullOrWhiteSpace(SearchSendingTicketLastNameText) ? null : SearchSendingTicketLastNameText.Trim(),
                    IdNumber = string.IsNullOrWhiteSpace(SearchSendingTicketIdNumberText) ? null : SearchSendingTicketIdNumberText.Trim(),
                    AccountNumber = ParseLongOrNull(SearchSendingTicketAccountNumberText),
                    CompanyId = SearchTicketSelectedCompany?.CompanyId,
                    SiteId = SearchTicketSelectedSite?.SiteId
                });

                SendingNewBuyerSearchResults.Clear();
                if (results != null)
                {
                    foreach (var b in results)
                        SendingNewBuyerSearchResults.Add(b);
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    SelectedSendingNewBuyer = SendingNewBuyerSearchResults.FirstOrDefault();
                });
                StatusMessage = $"Loaded {SendingNewBuyerSearchResults.Count} buyer(s) without tickets.";
            }
            else
            {
                var results = await _ticketSendingService.SearchTicketsSendingAsync(request);

                SendingTicketSearchResults.Clear();
                foreach (var t in results)
                {
                    SendingTicketSearchResults.Add(t);
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    SelectedSendingTicket = SendingTicketSearchResults.FirstOrDefault();
                });

                StatusMessage = $"Loaded {SendingTicketSearchResults.Count} sending ticket(s).";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Sending ticket search failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OnEnterTicketsSendingAsync()
    {
        // Initialize with default Platform type
        InitializeTicketTypeOptions();
        SelectedTicketTypeOption = TicketTypeOptions.FirstOrDefault(t => t.Key == "platform");
        
        // Generate ticket number for Sending Platform (prefix = "SPL")
        await GenerateTicketNumberForSendingAsync("SPL");
    }

    private async Task GenerateTicketNumberForSendingAsync(string prefix)
    {
        TicketNumber = await _ticketSendingService.GenerateTicketNumberAsync(prefix);
        OnPropertyChanged(nameof(TicketNumber));
    }

    // Called when ticket type changes - delegates to appropriate service based on section
    public async Task RegenerateTicketNumberAsync(string prefix)
    {
        if (CurrentSection == EnumMainSection.TicketsReceiving)
        {
            // For Receiving: use the ReceivingService
            TicketNumber = await _ticketReceivingService.GenerateTicketNumberAsync(prefix);
        }
        else
        {
            // For Sending: use the SendingService directly
            await GenerateTicketNumberForSendingAsync(prefix);
        }
        OnPropertyChanged(nameof(TicketNumber));
    }

    private void ClearSendingTicketSearch()
    {
        SearchSendingTicketCustomerIdText = string.Empty;
        SearchSendingTicketIdNumberText = string.Empty;
        SearchSendingTicketFirstNameText = string.Empty;
        SearchSendingTicketLastNameText = string.Empty;
        SearchSendingTicketAccountNumberText = string.Empty;
        SearchSendingTicketNumberText = string.Empty;
        SelectedSearchTicketTypeOption = null;
        SearchSendingNewBuyersCheckbox = false;

        SendingNewBuyerSearchResults.Clear();
        SelectedSendingNewBuyer = null;

        SelectedTicketTypeOption = null;
        SearchSendingTicketCreatedFromText = string.Empty;
        SearchSendingTicketCreatedToText = string.Empty;
        SearchTicketSelectedCompany = null;
        SearchTicketSelectedSite = null;

        SendingTicketSearchResults.Clear();
        SelectedSendingTicket = null;
    }

    private async Task DeleteSendingTicketAsync(TicketSearchResultDto? ticket)
    {
        var target = ticket ?? SelectedSendingTicket;
        if (target is null) return;
        if (IsBusy) return;

        var ok = await ConfirmAsync($"Are you sure you want to delete ticket {target.TicketNumber}?");
        if (!ok) return;

        IsBusy = true;
        try
        {
            await _ticketService.DeleteTicketAsync(target.TicketId);

            SendingTicketSearchResults.Remove(target);
            if (ReferenceEquals(SelectedSendingTicket, target))
            {
                SelectedSendingTicket = SendingTicketSearchResults.FirstOrDefault();
            }

            StatusMessage = $"Ticket {target.TicketNumber} deleted (soft).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Delete ticket failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task PrintSendingTicketAsync()
    {
        if (IsBusy) return;

        if (SelectedSendingTicket == null)
        {
            StatusMessage = "Please select a ticket to print.";
            return;
        }

        IsBusy = true;
        StatusMessage = $"Printing ticket {SelectedSendingTicket.TicketNumber}...";

        try
        {
            await Task.Delay(500);
            StatusMessage = $"Ticket {SelectedSendingTicket.TicketNumber} print not yet implemented.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error printing ticket: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
