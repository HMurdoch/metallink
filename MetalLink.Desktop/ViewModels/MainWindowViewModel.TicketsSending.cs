// Ticket Sending ViewModel
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using MetalLink.Desktop.Hardware;
using MetalLink.Shared.Products;
using MetalLink.Shared.Tickets;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    // Sending line item model used for the grid
    public sealed class SendingLineItem
    {
        public long TicketLineId { get; init; }
        public long TicketId { get; init; }
        public long ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public decimal WeightKg { get; init; }
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
            var lines = await _ticketService.GetTicketLinesAsync(ticketId);

            SendingLines.Clear();
            if (lines != null)
            {
                foreach (var dto in lines)
                {
                    SendingLines.Add(new SendingLineItem
                    {
                        TicketLineId = dto.TicketLineId,
                        TicketId = dto.TicketId,
                        ProductId = dto.ProductId,
                        ProductName = dto.ProductName,
                        WeightKg = dto.WeightKg
                    });
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
            var lines = new[] { ((long)SendingSelectedProduct.ProductId, weightKg) };

            var created = await _ticketService.AddTicketLinesAsync(
                LastCreatedTicket.TicketId,
                lines);

            if (created == null || created.Count == 0)
            {
                StatusMessage = "Ticket line create failed - API returned no result.";
                return;
            }

            foreach (var dto in created)
            {
                SendingLines.Add(new SendingLineItem
                {
                    TicketLineId = dto.TicketLineId,
                    TicketId = dto.TicketId,
                    ProductId = dto.ProductId,
                    ProductName = dto.ProductName,
                    WeightKg = dto.WeightKg
                });
            }

            RecalculateSendingTotals();

            StatusMessage = $"Added {created.Count} line(s) to ticket {LastCreatedTicket.TicketNumber}.";

            // Reset weight for next entry, keep product selection
            SendingWeightText = string.Empty;
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

    private void RecalculateSendingTotals()
    {
        var totalExcl = 0m;
        var totalVat = 0m;
        var totalIncl = 0m;

        foreach (var line in SendingLines)
        {
            // Financial calculations are handled at the ticket level, not line item level
        }

        SendingTotalExclVat = totalExcl;
        SendingTotalVat = totalVat;
        SendingTotalInclVat = totalIncl;
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
            await _ticketService.DeleteTicketLineAsync(line.TicketId, line.TicketLineId);

            SendingLines.Remove(line);
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

    private void ClearSendingTicket()
    {
        LastCreatedTicket = null;
        SendingLines.Clear();
        SendingWeightText = string.Empty;
        SendingSelectedProduct = null;
        SendingProductSearchText = string.Empty;
        RecalculateSendingTotals();
        
        // Initialize type options and set defaults
        InitializeTicketTypeOptions();
        SelectedTicketTypeOption = TicketTypeOptions.FirstOrDefault(t => t.Key == "weighbridge");
        
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

    public ObservableCollection<TicketSearchResultDto> SendingTicketSearchResults { get; } = new();

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

    private TicketSendingDto? _selectedSendingTicketDetails;
    public TicketSendingDto? SelectedSendingTicketDetails
    {
        get => _selectedSendingTicketDetails;
        set
        {
            _selectedSendingTicketDetails = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<TicketSendingLineDto> SelectedSendingTicketLines { get; } = new();

    public decimal SelectedSendingTicketLinesTotalExVat => 0m; // Financial calculations handled at ticket level only
    public decimal SelectedSendingTicketLinesTotalVat => 0m; // Financial calculations handled at ticket level only
    public decimal SelectedSendingTicketLinesTotalInclVat => 0m; // Financial calculations handled at ticket level only

    private async Task LoadSelectedSendingTicketDetailsAsync(long ticketId)
    {
        try
        {
            var details = await _ticketSendingService.GetTicketSendingByIdAsync(ticketId);
            SelectedSendingTicketDetails = details;

            // Populate form fields from loaded ticket
            if (details != null)
            {
                // Set ticket type
                InitializeTicketTypeOptions();
                var ticketTypeOption = TicketTypeOptions.FirstOrDefault(t => t.Key == details.TicketTypeName);
                if (ticketTypeOption != null)
                {
                    SelectedTicketTypeOption = ticketTypeOption;
                }
                
                // Set button text to Update
                CreateOrUpdateButtonText = "Update Ticket";

                // Load lines from the ticket details (already included in DTO)
                SelectedSendingTicketLines.Clear();
                if (details.Lines != null && details.Lines.Count > 0)
                {
                    foreach (var line in details.Lines)
                    {
                        SelectedSendingTicketLines.Add(line);
                    }
                }
            }
            
            OnPropertyChanged(nameof(SelectedSendingTicketLinesTotalExVat));
            OnPropertyChanged(nameof(SelectedSendingTicketLinesTotalVat));
            OnPropertyChanged(nameof(SelectedSendingTicketLinesTotalInclVat));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading ticket details: {ex.Message}";
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
            var ticketType = SelectedTicketTypeOption?.Key;
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

            var results = await _ticketSendingService.SearchTicketsSendingAsync(request);

            SendingTicketSearchResults.Clear();
            foreach (var t in results)
            {
                SendingTicketSearchResults.Add(t);
            }

            SelectedSendingTicket = SendingTicketSearchResults.FirstOrDefault();

            StatusMessage = $"Loaded {SendingTicketSearchResults.Count} sending ticket(s).";
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
