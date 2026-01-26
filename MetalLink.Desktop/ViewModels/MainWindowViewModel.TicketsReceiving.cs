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
    // Receiving line item model used for the grid
    public sealed class ReceivingLineItem
    {
        public long TicketLineId { get; init; }
        public long TicketId { get; init; }
        public long ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public decimal WeightKg { get; init; }
    }

    private ObservableCollection<ReceivingLineItem> _receivingLines = new();
    public ObservableCollection<ReceivingLineItem> ReceivingLines
    {
        get => _receivingLines;
        set
        {
            _receivingLines = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedTicketReceiving));
            RecalculateReceivingTotals();
        }
    }

    public bool HasUnsavedTicketReceiving => 
        _receivingLines.Count > 0 || 
        !string.IsNullOrWhiteSpace(ReceivingWeightText) ||
        ReceivingSelectedProduct != null;

    private ObservableCollection<ProductLookupDto> _receivingProductSuggestions = new();
    public ObservableCollection<ProductLookupDto> ReceivingProductSuggestions
    {
        get => _receivingProductSuggestions;
        set
        {
            _receivingProductSuggestions = value;
            OnPropertyChanged();
        }
    }

    private ProductLookupDto? _receivingSelectedProduct;
    public ProductLookupDto? ReceivingSelectedProduct
    {
        get => _receivingSelectedProduct;
        set
        {
            _receivingSelectedProduct = value;
            OnPropertyChanged();
        }
    }

    private string _receivingProductSearchText = string.Empty;
    public string ReceivingProductSearchText
    {
        get => _receivingProductSearchText;
        set
        {
            _receivingProductSearchText = value;
            OnPropertyChanged();
            
            // When text search changes, clear letter filter to null/empty
            // This allows substring search instead of showing ALL products
            if (!string.IsNullOrWhiteSpace(value))
            {
                _selectedReceivingProductLetter = string.Empty;
                OnPropertyChanged(nameof(SelectedReceivingProductLetter));
            }
            
            _ = SearchReceivingProductsAsync(value);
        }
    }

    private string _selectedReceivingProductLetter = "ALL";
    public string SelectedReceivingProductLetter
    {
        get => _selectedReceivingProductLetter;
        set
        {
            _selectedReceivingProductLetter = value;
            OnPropertyChanged();
            
            // When letter filter is selected, clear text search
            if (!string.IsNullOrWhiteSpace(value))
            {
                _receivingProductSearchText = string.Empty;
                OnPropertyChanged(nameof(ReceivingProductSearchText));
            }
            
            _ = ApplyReceivingProductFilterAsync();
        }
    }

    private string _receivingWeightText = string.Empty;
    public string ReceivingWeightText
    {
        get => _receivingWeightText;
        set
        {
            _receivingWeightText = value;
            OnPropertyChanged();
        }
    }

    private decimal _receivingTotalExclVat;
    public decimal ReceivingTotalExclVat
    {
        get => _receivingTotalExclVat;
        private set
        {
            _receivingTotalExclVat = value;
            OnPropertyChanged();
        }
    }

    private decimal _receivingTotalVat;
    public decimal ReceivingTotalVat
    {
        get => _receivingTotalVat;
        private set
        {
            _receivingTotalVat = value;
            OnPropertyChanged();
        }
    }

    private decimal _receivingTotalInclVat;
    public decimal ReceivingTotalInclVat
    {
        get => _receivingTotalInclVat;
        private set
        {
            _receivingTotalInclVat = value;
            OnPropertyChanged();
        }
    }

    private async Task SearchReceivingProductsAsync(string? term)
    {
        try
        {
            var results = await _app.ProductsAndPricesService.LookupProductsAsync(
                string.IsNullOrWhiteSpace(term) ? string.Empty : term);

            ReceivingProductSuggestions.Clear();
            foreach (var p in results)
            {
                ReceivingProductSuggestions.Add(p);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error searching products: {ex.Message}";
        }
    }

    private async Task ApplyReceivingProductFilterAsync()
    {
        try
        {
            // If letter filter is empty/null, don't load anything
            // The search text will handle the filtering via SearchReceivingProductsAsync
            if (string.IsNullOrWhiteSpace(SelectedReceivingProductLetter))
            {
                return;
            }
            
            // Load all products from API
            var results = await _app.ProductsAndPricesService.LookupProductsAsync(string.Empty);
            
            ReceivingProductSuggestions.Clear();
            
            if (SelectedReceivingProductLetter == "ALL")
            {
                // Show all products
                foreach (var p in results)
                {
                    ReceivingProductSuggestions.Add(p);
                }
            }
            else
            {
                // Filter by first letter
                var filtered = results
                    .Where(p => p.ProductName != null && 
                           p.ProductName.StartsWith(SelectedReceivingProductLetter, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var p in filtered)
                {
                    ReceivingProductSuggestions.Add(p);
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error filtering products: {ex.Message}";
        }
    }

    private async Task LoadReceivingLinesForTicketAsync(long ticketId)
    {
        if (ticketId <= 0)
        {
            ReceivingLines.Clear();
            RecalculateReceivingTotals();
            return;
        }

        try
        {
            var lines = await _ticketService.GetTicketLinesAsync(ticketId);

            ReceivingLines.Clear();
            if (lines != null)
            {
                foreach (var dto in lines)
                {
                    ReceivingLines.Add(new ReceivingLineItem
                    {
                        TicketLineId = dto.TicketLineId,
                        TicketId = dto.TicketId,
                        ProductId = dto.ProductId,
                        ProductName = dto.ProductName,
                        WeightKg = dto.WeightKg,
                    });
                }
            }

            RecalculateReceivingTotals();

            StatusMessage = $"Loaded {ReceivingLines.Count} line(s) for ticket {ticketId}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading ticket lines: {ex.Message}";
        }
    }

    private async Task AddReceivingLineAsync()
    {
        if (IsBusy) return;

        if (LastCreatedTicket == null || LastCreatedTicket.TicketId <= 0)
        {
            StatusMessage = "Please create a ticket header before adding lines.";
            return;
        }

        if (ReceivingSelectedProduct == null)
        {
            StatusMessage = "Please select a product for the line.";
            return;
        }

        if (string.IsNullOrWhiteSpace(ReceivingWeightText))
        {
            StatusMessage = "Weight (kg) is required.";
            return;
        }

        if (!decimal.TryParse(
                NormalizeDecimalText(ReceivingWeightText),
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
            var lines = new[] { ((long)ReceivingSelectedProduct.ProductId, weightKg) };

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
                ReceivingLines.Add(new ReceivingLineItem
                {
                    TicketLineId = dto.TicketLineId,
                    TicketId = dto.TicketId,
                    ProductId = dto.ProductId,
                    ProductName = dto.ProductName,
                    WeightKg = dto.WeightKg,
                });
            }

            RecalculateReceivingTotals();

            StatusMessage = $"Added {created.Count} line(s) to ticket {LastCreatedTicket.TicketNumber}.";

            // Reset weight for next entry, keep product selection
            ReceivingWeightText = string.Empty;
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

    private void RecalculateReceivingTotals()
    {
        var totalExcl = 0m;
        var totalVat = 0m;
        var totalIncl = 0m;

        foreach (var line in ReceivingLines)
        {
            // Financial calculations are handled at the ticket level, not line item level
        }

        ReceivingTotalExclVat = totalExcl;
        ReceivingTotalVat = totalVat;
        ReceivingTotalInclVat = totalIncl;
    }

    private async Task RemoveReceivingLineAsync(ReceivingLineItem? line)
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

            ReceivingLines.Remove(line);
            RecalculateReceivingTotals();

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

    private Task SaveTicketAsync()
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

    private async Task ClearTicketAsync()
    {
        LastCreatedTicket = null;
        ReceivingLines.Clear();
        ReceivingWeightText = string.Empty;
        ReceivingSelectedProduct = null;
        ReceivingProductSearchText = string.Empty;
        RecalculateReceivingTotals();
        
        // Initialize type options and set defaults
        InitializeTicketTypeOptions();
        SelectedTicketTypeOption = TicketTypeOptions.FirstOrDefault(t => t.Key == "platform");
        
        // Generate ticket number for Platform (ticketTypeId = 2)
        await GenerateTicketNumberAsync(2);
        TicketCustomerIdText = string.Empty;
        
        // Initialize currency options and set default to ZAR
        InitializeCurrencyOptions();
        SelectedCurrency = "ZAR";
        
        CreateOrUpdateButtonText = "Create Ticket";
        IsViewingTicketOnly = false;
        
        StatusMessage = "Ticket cleared.";
    }

    private async Task GenerateTicketNumberAsync(int ticketTypeId)
    {
        TicketNumber = await _ticketReceivingService.GetNextTicketNumberAsync(ticketTypeId);
        OnPropertyChanged(nameof(TicketNumber));
    }

    private async Task CaptureWeightAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        StatusMessage = "Capturing weight from scale...";

        try
        {
            var reading = await _app.ScaleService.ReadOnceAsync(ScaleDeviceType.Weighbridge);
            if (reading != null)
            {
                ReceivingWeightText = reading.WeightKg.ToString("F2");
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

    private async Task CapturePlatePhotoAsync()
    {
        if (IsBusy) return;

        StatusMessage = "Capturing plate photo...";
        // TODO: Implement plate photo capture
        await Task.Delay(100);
        StatusMessage = "Plate photo capture not yet implemented.";
    }

    private async Task CaptureLoadPhotoAsync()
    {
        if (IsBusy) return;

        StatusMessage = "Capturing load photo...";
        // TODO: Implement load photo capture
        await Task.Delay(100);
        StatusMessage = "Load photo capture not yet implemented.";
    }

    // --- Receiving Ticket Search Properties ---

    private string _searchReceivingTicketCustomerIdText = string.Empty;
    private string _searchReceivingTicketIdNumberText = string.Empty;
    private string _searchReceivingTicketFirstNameText = string.Empty;
    private string _searchReceivingTicketLastNameText = string.Empty;
    private string _searchReceivingTicketAccountNumberText = string.Empty;
    private string _searchReceivingTicketNumberText = string.Empty;
    private string _searchReceivingTicketTypeKey = "All";
    private string _searchReceivingTicketCreatedFromText = string.Empty;
    private string _searchReceivingTicketCreatedToText = string.Empty;

    public string SearchReceivingTicketCustomerIdText
    {
        get => _searchReceivingTicketCustomerIdText;
        set { _searchReceivingTicketCustomerIdText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchReceivingTicketIdNumberText
    {
        get => _searchReceivingTicketIdNumberText;
        set { _searchReceivingTicketIdNumberText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchReceivingTicketFirstNameText
    {
        get => _searchReceivingTicketFirstNameText;
        set { _searchReceivingTicketFirstNameText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchReceivingTicketLastNameText
    {
        get => _searchReceivingTicketLastNameText;
        set { _searchReceivingTicketLastNameText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchReceivingTicketAccountNumberText
    {
        get => _searchReceivingTicketAccountNumberText;
        set { _searchReceivingTicketAccountNumberText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchReceivingTicketNumberText
    {
        get => _searchReceivingTicketNumberText;
        set { _searchReceivingTicketNumberText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchReceivingTicketTypeKey
    {
        get => _searchReceivingTicketTypeKey;
        set { _searchReceivingTicketTypeKey = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchReceivingTicketCreatedFromText
    {
        get => _searchReceivingTicketCreatedFromText;
        set { _searchReceivingTicketCreatedFromText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchReceivingTicketCreatedToText
    {
        get => _searchReceivingTicketCreatedToText;
        set { _searchReceivingTicketCreatedToText = value ?? string.Empty; OnPropertyChanged(); }
    }

    // --- Receiving Search Results ---

    public ObservableCollection<TicketSearchResultDto> ReceivingTicketSearchResults { get; } = new();

    private TicketSearchResultDto? _selectedReceivingTicket;
    public TicketSearchResultDto? SelectedReceivingTicket
    {
        get => _selectedReceivingTicket;
        set
        {
            _selectedReceivingTicket = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedReceivingTicketSummary));
            OnPropertyChanged(nameof(HasSelectedReceivingTicket));
            
            if (value != null)
            {
                _ = LoadSelectedReceivingTicketDetailsAsync(value.TicketId);
            }
            else
            {
                SelectedReceivingTicketLines.Clear();
                SelectedReceivingTicketDetails = null;
            }
        }
    }

    public bool HasSelectedReceivingTicket => SelectedReceivingTicket != null;

    public string SelectedReceivingTicketSummary
    {
        get
        {
            if (SelectedReceivingTicket is null)
                return "No ticket selected.";

            return $"Ticket {SelectedReceivingTicket.TicketNumber} ({SelectedReceivingTicket.TicketType}) - " +
                   $"Customer {SelectedReceivingTicket.CustomerId}, Net {SelectedReceivingTicket.NetWeightKg:N2} kg, " +
                   $"Total {SelectedReceivingTicketLinesTotalInclVat:N2}";
        }
    }

    private TicketDto? _selectedReceivingTicketDetails;
    public TicketDto? SelectedReceivingTicketDetails
    {
        get => _selectedReceivingTicketDetails;
        set
        {
            _selectedReceivingTicketDetails = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<TicketLineDto> SelectedReceivingTicketLines { get; } = new();

    public decimal SelectedReceivingTicketLinesTotalExVat => 0m; // Financial calculations handled at ticket level only
    public decimal SelectedReceivingTicketLinesTotalVat
    {
        get
        {
            decimal totalVat = 0m;
            foreach (var line in SelectedReceivingTicketLines)
            {
                var lineTotal = line.WeightKg * line.UnitPricePerKg;
                var lineExcl = lineTotal / 1.15m;
                totalVat += lineTotal - lineExcl;
            }
            return totalVat;
        }
    }

    public decimal SelectedReceivingTicketLinesTotalInclVat => SelectedReceivingTicketLines.Sum(l => l.WeightKg * l.UnitPricePerKg);

    private async Task LoadSelectedReceivingTicketDetailsAsync(long ticketId)
    {
        try
        {
            var details = await _ticketService.GetTicketByIdAsync(ticketId);
            SelectedReceivingTicketDetails = details;

            // Populate form fields from loaded ticket
            if (details != null)
            {
                // Populate Customer ID
                TicketCustomerIdText = details.CustomerId.ToString();

                // Set ticket type
                InitializeTicketTypeOptions();
                var ticketTypeOption = TicketTypeOptions.FirstOrDefault(t => t.Key == details.TicketType);
                if (ticketTypeOption != null)
                {
                    SelectedTicketTypeOption = ticketTypeOption;
                }
                
                // Keep button text as Create (viewing only, not editing)
                CreateOrUpdateButtonText = "Create Ticket";
                IsViewingTicketOnly = true;

                // Load lines from the ticket details (already included in DTO)
                SelectedReceivingTicketLines.Clear();
                if (details.Lines != null && details.Lines.Count > 0)
                {
                    foreach (var line in details.Lines)
                    {
                        SelectedReceivingTicketLines.Add(line);
                    }
                }
            }
            
            OnPropertyChanged(nameof(SelectedReceivingTicketLinesTotalExVat));
            OnPropertyChanged(nameof(SelectedReceivingTicketLinesTotalVat));
            OnPropertyChanged(nameof(SelectedReceivingTicketLinesTotalInclVat));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading ticket details: {ex.Message}";
        }
    }

    private async Task SearchReceivingTicketsAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        StatusMessage = "Searching receiving tickets...";
        
        // Initialize ticket type options for search dropdown
        InitializeTicketTypeOptions();

        try
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] SearchTicketTypeKey={SearchTicketTypeKey}");
            
            var ticketType = string.IsNullOrWhiteSpace(SearchTicketTypeKey) || SearchTicketTypeKey == "All" ? null : SearchTicketTypeKey;
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Final TicketType={ticketType}");
            
            var request = new TicketReceivingSearchRequestDto
            {
                CompanyId = SelectedSearchCompany?.CompanyId,
                SiteId = SelectedSearchSite?.SiteId,
                TicketType = ticketType,
                CustomerId = ParseIntOrNull(SearchReceivingTicketCustomerIdText),
                FirstName = string.IsNullOrWhiteSpace(SearchReceivingTicketFirstNameText) ? null : SearchReceivingTicketFirstNameText.Trim(),
                LastName = string.IsNullOrWhiteSpace(SearchReceivingTicketLastNameText) ? null : SearchReceivingTicketLastNameText.Trim(),
                IdNumber = string.IsNullOrWhiteSpace(SearchReceivingTicketIdNumberText) ? null : SearchReceivingTicketIdNumberText.Trim(),
                AccountNumber = ParseLongOrNull(SearchReceivingTicketAccountNumberText),
                SearchTerm = string.IsNullOrWhiteSpace(SearchReceivingTicketNumberText) ? null : SearchReceivingTicketNumberText.Trim(),
                StartDate = ParseDateOrNull(SearchReceivingTicketCreatedFromText),
                EndDate = ParseDateOrNull(SearchReceivingTicketCreatedToText)
            };

            var results = await _ticketReceivingService.SearchTicketsReceivingAsync(request);

            System.Diagnostics.Debug.WriteLine($"[DEBUG] Search returned {results.Count} results");
            foreach (var r in results)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Ticket: {r.TicketNumber}");
            }

            ReceivingTicketSearchResults.Clear();
            foreach (var t in results)
            {
                ReceivingTicketSearchResults.Add(t);
            }

            SelectedReceivingTicket = ReceivingTicketSearchResults.FirstOrDefault();

            StatusMessage = $"Loaded {ReceivingTicketSearchResults.Count} receiving ticket(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Receiving ticket search failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ClearReceivingTicketSearch()
    {
        SearchReceivingTicketCustomerIdText = string.Empty;
        SearchReceivingTicketIdNumberText = string.Empty;
        SearchReceivingTicketFirstNameText = string.Empty;
        SearchReceivingTicketLastNameText = string.Empty;
        SearchReceivingTicketAccountNumberText = string.Empty;
        SearchReceivingTicketNumberText = string.Empty;
        SelectedSearchTicketTypeOption = null;
        SearchReceivingTicketCreatedFromText = string.Empty;
        SearchReceivingTicketCreatedToText = string.Empty;
        SelectedSearchCompany = null;
        SelectedSearchSite = null;

        ReceivingTicketSearchResults.Clear();
        SelectedReceivingTicket = null;
    }

    private async Task DeleteReceivingTicketAsync(TicketSearchResultDto? ticket)
    {
        var target = ticket ?? SelectedReceivingTicket;
        if (target is null) return;
        if (IsBusy) return;

        var ok = await ConfirmAsync($"Are you sure you want to delete ticket {target.TicketNumber}?");
        if (!ok) return;

        IsBusy = true;
        try
        {
            await _ticketService.DeleteTicketAsync(target.TicketId);

            ReceivingTicketSearchResults.Remove(target);
            if (ReferenceEquals(SelectedReceivingTicket, target))
            {
                SelectedReceivingTicket = ReceivingTicketSearchResults.FirstOrDefault();
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

    private async Task PrintReceivingTicketAsync()
    {
        if (IsBusy) return;

        if (SelectedReceivingTicket == null)
        {
            StatusMessage = "Please select a ticket to print.";
            return;
        }

        IsBusy = true;
        StatusMessage = $"Printing ticket {SelectedReceivingTicket.TicketNumber}...";

        try
        {
            await Task.Delay(500);
            StatusMessage = $"Ticket {SelectedReceivingTicket.TicketNumber} print not yet implemented.";
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
