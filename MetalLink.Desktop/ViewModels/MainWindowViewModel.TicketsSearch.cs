using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MetalLink.Shared.Products;
using MetalLink.Shared.Tickets;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    // --- Ticket search backing fields ---

    private string _searchTicketCustomerIdText = string.Empty;
    private string _searchTicketIdNumberText = string.Empty;
    private string _searchTicketFirstNameText = string.Empty;
    private string _searchTicketLastNameText = string.Empty;
    private string _searchTicketAccountNumberText = string.Empty;
    private string _searchTicketNumberText = string.Empty;
    private string _searchTicketTypeKey = string.Empty; // reuse TicketTypeOptions keys
    private string _searchTicketCreatedFromText = string.Empty;
    private string _searchTicketCreatedToText = string.Empty;

    public string SearchTicketCustomerIdText
    {
        get => _searchTicketCustomerIdText;
        set { _searchTicketCustomerIdText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchTicketIdNumberText
    {
        get => _searchTicketIdNumberText;
        set { _searchTicketIdNumberText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchTicketFirstNameText
    {
        get => _searchTicketFirstNameText;
        set { _searchTicketFirstNameText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchTicketLastNameText
    {
        get => _searchTicketLastNameText;
        set { _searchTicketLastNameText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchTicketAccountNumberText
    {
        get => _searchTicketAccountNumberText;
        set { _searchTicketAccountNumberText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchTicketNumberText
    {
        get => _searchTicketNumberText;
        set { _searchTicketNumberText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchTicketTypeKey
    {
        get => _searchTicketTypeKey;
        set { _searchTicketTypeKey = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchTicketCreatedFromText
    {
        get => _searchTicketCreatedFromText;
        set { _searchTicketCreatedFromText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchTicketCreatedToText
    {
        get => _searchTicketCreatedToText;
        set { _searchTicketCreatedToText = value ?? string.Empty; OnPropertyChanged(); }
    }

    // --- Search results ---

    public ObservableCollection<TicketSearchResultDto> TicketSearchResults { get; } = new();

    private TicketSearchResultDto? _selectedTicket;
    public TicketSearchResultDto? SelectedTicket
    {
        get => _selectedTicket;
        set
        {
            _selectedTicket = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedTicketSummary));
            OnPropertyChanged(nameof(HasSelectedTicket));
            
            // Load ticket lines when a ticket is selected
            if (value != null)
            {
                _ = LoadSelectedTicketDetailsAsync(value.TicketId);
            }
            else
            {
                SelectedTicketLines.Clear();
                SelectedTicketDetails = null;
            }
        }
    }

    public bool HasSelectedTicket => SelectedTicket != null;

    public string SelectedTicketSummary
    {
        get
        {
            if (SelectedTicket is null)
                return "No ticket selected.";

            return $"Ticket {SelectedTicket.TicketNumber} ({SelectedTicket.TicketType}) - " +
                   $"Customer {SelectedTicket.CustomerId}, Net {SelectedTicket.NetWeightKg:N2} kg, " +
                   $"Total {SelectedTicket.TotalInclVat:N2}";
        }
    }

    // --- Selected ticket details + lines ---

    private TicketDto? _selectedTicketDetails;
    public TicketDto? SelectedTicketDetails
    {
        get => _selectedTicketDetails;
        set
        {
            _selectedTicketDetails = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<TicketLineDto> SelectedTicketLines { get; } = new();

    // Calculated totals for selected ticket lines
    public decimal SelectedTicketLinesTotalExVat => SelectedTicketLines.Sum(l => l.LineTotal);
    public decimal SelectedTicketLinesTotalVat => SelectedTicketLines.Sum(l => l.VatAmount);
    public decimal SelectedTicketLinesTotalInclVat => SelectedTicketLines.Sum(l => l.TotalInclVat);

    private async Task LoadSelectedTicketDetailsAsync(long ticketId)
    {
        try
        {
            // Load full ticket details
            var details = await _ticketService.GetTicketByIdAsync(ticketId);
            SelectedTicketDetails = details;

            // Load ticket lines
            var lines = await _ticketService.GetTicketLinesAsync(ticketId);
            SelectedTicketLines.Clear();
            if (lines != null)
            {
                foreach (var line in lines)
                {
                    SelectedTicketLines.Add(line);
                }
            }
            
            // Notify totals changed
            OnPropertyChanged(nameof(SelectedTicketLinesTotalExVat));
            OnPropertyChanged(nameof(SelectedTicketLinesTotalVat));
            OnPropertyChanged(nameof(SelectedTicketLinesTotalInclVat));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading ticket details: {ex.Message}";
        }
    }

    private long? ParseLongOrNull(string text)
    {
        var t = (text ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(t)) return null;
        if (t.All(c => c == '0')) return null;
        return long.TryParse(t, out var v) ? v : null;
    }

    private DateTimeOffset? ParseDateOrNull(string text)
    {
        var t = (text ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(t)) return null;
        if (DateTimeOffset.TryParse(t, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var dt))
            return dt;
        if (DateTimeOffset.TryParse(t, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out dt))
            return dt;
        return null;
    }

    private TicketTypeOption? _selectedSearchTicketTypeOption;

    public TicketTypeOption? SelectedSearchTicketTypeOption
    {
        get => _selectedSearchTicketTypeOption;
        set
        {
            _selectedSearchTicketTypeOption = value;
            OnPropertyChanged();

            SearchTicketTypeKey = value?.Key ?? string.Empty;
        }
    }

    private async Task SearchTicketsAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        StatusMessage = "Searching tickets...";

        try
        {
            var request = new TicketSearchRequestDto
            {
                CustomerId = ParseLongOrNull(SearchTicketCustomerIdText),
                IdNumber = string.IsNullOrWhiteSpace(SearchTicketIdNumberText) ? null : SearchTicketIdNumberText.Trim(),
                FirstName = string.IsNullOrWhiteSpace(SearchTicketFirstNameText) ? null : SearchTicketFirstNameText.Trim(),
                LastName = string.IsNullOrWhiteSpace(SearchTicketLastNameText) ? null : SearchTicketLastNameText.Trim(),
                AccountNumber = ParseLongOrNull(SearchTicketAccountNumberText),
                TicketNumber = string.IsNullOrWhiteSpace(SearchTicketNumberText) ? null : SearchTicketNumberText.Trim(),
                TicketType = string.IsNullOrWhiteSpace(SearchTicketTypeKey) ? null : SearchTicketTypeKey.Trim(),
                CreatedFrom = ParseDateOrNull(SearchTicketCreatedFromText),
                CreatedTo = ParseDateOrNull(SearchTicketCreatedToText)
            };

            var results = await _ticketService.SearchTicketsAsync(request);

            TicketSearchResults.Clear();
            foreach (var t in results)
            {
                TicketSearchResults.Add(t);
            }

            SelectedTicket = TicketSearchResults.FirstOrDefault();

            StatusMessage = $"Loaded {TicketSearchResults.Count} ticket(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ticket search failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ClearTicketSearch()
    {
        SearchTicketCustomerIdText = string.Empty;
        SearchTicketIdNumberText = string.Empty;
        SearchTicketFirstNameText = string.Empty;
        SearchTicketLastNameText = string.Empty;
        SearchTicketAccountNumberText = string.Empty;
        SearchTicketNumberText = string.Empty;
        SearchTicketTypeKey = string.Empty;
        SearchTicketCreatedFromText = string.Empty;
        SearchTicketCreatedToText = string.Empty;
        SelectedSearchTicketTypeOption = null;

        TicketSearchResults.Clear();
        SelectedTicket = null;
    }

    private async Task DeleteTicketAsync(TicketSearchResultDto? ticket)
    {
        var target = ticket ?? SelectedTicket;
        if (target is null)
            return;

        if (IsBusy)
            return;

        var ok = await ConfirmAsync($"Are you sure you want to delete ticket {target.TicketNumber}?");
        if (!ok)
            return;

        IsBusy = true;
        try
        {
            await _ticketService.DeleteTicketAsync(target.TicketId);

            TicketSearchResults.Remove(target);
            if (ReferenceEquals(SelectedTicket, target))
            {
                SelectedTicket = TicketSearchResults.FirstOrDefault();
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

    // --- Edit Ticket Functionality ---

    private void OnEditTicket(TicketSearchResultDto? ticket)
    {
        if (ticket == null) return;

        EditingTicketId = ticket.TicketId;
        TicketCustomerIdText = ticket.CustomerId.ToString();
        TicketNumber = ticket.TicketNumber;
        
        var typeOption = TicketTypeOptions.FirstOrDefault(o => 
            o.Key.Equals(ticket.TicketType, StringComparison.OrdinalIgnoreCase));
        if (typeOption != null)
        {
            SelectedTicketTypeOption = typeOption;
        }

        _ = LoadTicketDetailsForEditAsync(ticket.TicketId);
        StatusMessage = $"Editing ticket {ticket.TicketNumber}";
    }

    private async Task LoadTicketDetailsForEditAsync(long ticketId)
    {
        try
        {
            var details = await _ticketService.GetTicketByIdAsync(ticketId);
            if (details == null) return;

            TicketFirstWeightText = details.FirstWeightKg?.ToString("0.00") ?? string.Empty;
            TicketSecondWeightText = details.SecondWeightKg?.ToString("0.00") ?? string.Empty;
            TicketUnitPriceText = details.UnitPricePerKg.ToString("0.00");
            TicketCurrencyCode = details.CurrencyCode;
            TicketProductDescription = details.ProductDescription ?? string.Empty;
            TicketNotes = details.Notes ?? string.Empty;
            TicketVehicleRegistration = details.VehicleRegistration ?? string.Empty;
            TicketOfmWeighbridgeTicket = details.OfmWeighbridgeTicket ?? string.Empty;
            TicketForeignTicket = details.ForeignTicket ?? string.Empty;
            TicketCkNumber = details.CkNumber ?? string.Empty;
            LastCreatedTicket = details;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading ticket for edit: {ex.Message}";
        }
    }

    private void OnCancelEditTicket()
    {
        EditingTicketId = null;
        TicketCustomerIdText = string.Empty;
        TicketNumber = string.Empty;
        TicketFirstWeightText = string.Empty;
        TicketSecondWeightText = string.Empty;
        TicketUnitPriceText = string.Empty;
        TicketProductDescription = string.Empty;
        TicketNotes = string.Empty;
        TicketVehicleRegistration = string.Empty;
        TicketOfmWeighbridgeTicket = string.Empty;
        TicketForeignTicket = string.Empty;
        TicketCkNumber = string.Empty;
        StatusMessage = "Edit cancelled";
    }

    // Line item editing state
    private TicketLineDto? _editingTicketLine;
    public TicketLineDto? EditingTicketLine
    {
        get => _editingTicketLine;
        set
        {
            _editingTicketLine = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEditingTicketLine));
        }
    }

    public bool IsEditingTicketLine => EditingTicketLine != null;

    private string _editLineProductSearchText = string.Empty;
    public string EditLineProductSearchText
    {
        get => _editLineProductSearchText;
        set
        {
            _editLineProductSearchText = value;
            OnPropertyChanged();
            
            // When text search changes, clear letter filter to null/empty
            // This allows substring search instead of showing ALL products
            if (!string.IsNullOrWhiteSpace(value))
            {
                _editLineSelectedProductLetter = string.Empty;
                OnPropertyChanged(nameof(EditLineSelectedProductLetter));
            }
            
            _ = SearchEditLineProductsAsync(value);
        }
    }

    private string _editLineSelectedProductLetter = "ALL";
    public string EditLineSelectedProductLetter
    {
        get => _editLineSelectedProductLetter;
        set
        {
            _editLineSelectedProductLetter = value;
            OnPropertyChanged();
            
            // When letter filter is selected, clear text search
            if (!string.IsNullOrWhiteSpace(value))
            {
                _editLineProductSearchText = string.Empty;
                OnPropertyChanged(nameof(EditLineProductSearchText));
            }
            
            _ = ApplyEditLineProductFilterAsync();
        }
    }

    public ObservableCollection<ProductLookupDto> EditLineProductSuggestions { get; } = new();

    private ProductLookupDto? _editLineSelectedProduct;
    public ProductLookupDto? EditLineSelectedProduct
    {
        get => _editLineSelectedProduct;
        set
        {
            _editLineSelectedProduct = value;
            OnPropertyChanged();
        }
    }

    private string _editLineWeightText = string.Empty;
    public string EditLineWeightText
    {
        get => _editLineWeightText;
        set
        {
            _editLineWeightText = value;
            OnPropertyChanged();
        }
    }

    private async Task SearchEditLineProductsAsync(string? term)
    {
        try
        {
            var results = await _app.ProductsAndPricesService.LookupProductsAsync(
                string.IsNullOrWhiteSpace(term) ? string.Empty : term);

            EditLineProductSuggestions.Clear();
            foreach (var p in results)
            {
                EditLineProductSuggestions.Add(p);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error searching products: {ex.Message}";
        }
    }

    private async Task ApplyEditLineProductFilterAsync()
    {
        try
        {
            // If letter filter is empty/null, don't load anything
            // The search text will handle the filtering via SearchEditLineProductsAsync
            if (string.IsNullOrWhiteSpace(EditLineSelectedProductLetter))
            {
                return;
            }
            
            // Load all products from API
            var results = await _app.ProductsAndPricesService.LookupProductsAsync(string.Empty);
            
            EditLineProductSuggestions.Clear();
            
            if (EditLineSelectedProductLetter == "ALL")
            {
                // Show all products
                foreach (var p in results)
                {
                    EditLineProductSuggestions.Add(p);
                }
            }
            else
            {
                // Filter by first letter
                var filtered = results
                    .Where(p => p.ProductName != null && 
                           p.ProductName.StartsWith(EditLineSelectedProductLetter, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var p in filtered)
                {
                    EditLineProductSuggestions.Add(p);
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error filtering products: {ex.Message}";
        }
    }

    private void OnEditTicketLine(TicketLineDto? line)
    {
        if (line == null) return;
        
        EditingTicketLine = line;
        EditLineWeightText = line.WeightKg.ToString("0.00");
        EditLineProductSearchText = string.Empty;
        // Set letter filter to the first letter of the product for quick filtering
        EditLineSelectedProductLetter = line.ProductName?.Substring(0, 1).ToUpper() ?? "ALL";
        
        // Load product based on the line's product
        _ = LoadEditLineProductAsync(line.ProductId);
        
        StatusMessage = $"Editing line item: {line.ProductName}";
    }

    private async Task LoadEditLineProductAsync(long productId)
    {
        try
        {
            var products = await _app.ProductsAndPricesService.LookupProductsAsync(string.Empty);
            var product = products.FirstOrDefault(p => p.ProductId == productId);
            
            if (product != null)
            {
                EditLineSelectedProduct = product;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading product: {ex.Message}";
        }
    }

    private async Task SaveEditedTicketLineAsync()
    {
        if (EditingTicketLine == null) return;
        if (IsBusy) return;

        // Validation
        var validationErrors = new List<string>();

        if (EditLineSelectedProduct == null)
        {
            validationErrors.Add("Please select a product.");
        }

        if (string.IsNullOrWhiteSpace(EditLineWeightText))
        {
            validationErrors.Add("Weight is required.");
        }
        else if (!decimal.TryParse(EditLineWeightText, out var weight) || weight <= 0)
        {
            validationErrors.Add("Weight must be a positive number.");
        }
        else if (weight > 100000)
        {
            validationErrors.Add("Weight cannot exceed 100,000 kg.");
        }

        if (validationErrors.Any())
        {
            StatusMessage = $"Validation errors: {string.Join("; ", validationErrors)}";
            return;
        }

        IsBusy = true;
        StatusMessage = "Updating line item...";

        try
        {
            var weight = decimal.Parse(EditLineWeightText);
            
            // Get the price for this product
            var price = await _app.ProductsAndPricesService.GetPriceForProductAsync(
                EditLineSelectedProduct!.ProductId);
            
            if (price == null)
            {
                StatusMessage = "Could not find price for selected product.";
                return;
            }

            // Use PriceA as the default price
            var unitPrice = price.PriceA;

            var success = await _app.TicketService.UpdateTicketLineAsync(
                EditingTicketLine.TicketId,
                EditingTicketLine.TicketLineId,
                EditLineSelectedProduct.ProductId,
                weight,
                unitPrice);

            if (success)
            {
                StatusMessage = $"✓ Updated line item: {EditLineSelectedProduct.ProductName} - {weight:N2} kg";
                
                // Reload the ticket details
                await LoadSelectedTicketDetailsAsync(EditingTicketLine.TicketId);
                
                CancelEditTicketLine();
            }
            else
            {
                StatusMessage = "Failed to update line item. Please try again.";
            }
        }
        catch (FormatException)
        {
            StatusMessage = "Invalid weight format. Please enter a valid number.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error updating line item: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void CancelEditTicketLine()
    {
        EditingTicketLine = null;
        EditLineProductSearchText = string.Empty;
        EditLineSelectedProductLetter = "ALL";
        EditLineSelectedProduct = null;
        EditLineWeightText = string.Empty;
        EditLineProductSuggestions.Clear();
        StatusMessage = "Edit cancelled";
    }

    private async Task DeleteTicketLineAsync(TicketLineDto? line)
    {
        if (line == null) return;
        if (IsBusy) return;

        var ok = await ConfirmAsync($"Delete line item '{line.ProductName}' ({line.WeightKg} kg)?");
        if (!ok) return;

        IsBusy = true;
        try
        {
            StatusMessage = "Deleting line item...";
            await _ticketService.DeleteTicketLineAsync(line.TicketId, line.TicketLineId);
            
            var item = SelectedTicketLines.FirstOrDefault(l => l.TicketLineId == line.TicketLineId);
            if (item != null)
            {
                SelectedTicketLines.Remove(item);
            }

            // Notify totals changed
            OnPropertyChanged(nameof(SelectedTicketLinesTotalExVat));
            OnPropertyChanged(nameof(SelectedTicketLinesTotalVat));
            OnPropertyChanged(nameof(SelectedTicketLinesTotalInclVat));

            StatusMessage = $"Line item deleted: {line.ProductName}";
            await LoadSelectedTicketDetailsAsync(line.TicketId);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting line item: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task PrintTicketAsync()
    {
        if (IsBusy) return;

        if (SelectedTicket == null)
        {
            StatusMessage = "Please select a ticket to print.";
            return;
        }

        IsBusy = true;
        StatusMessage = $"Printing ticket {SelectedTicket.TicketNumber}...";

        try
        {
            // TODO: Implement ticket printing functionality
            await Task.Delay(500);
            StatusMessage = $"Ticket {SelectedTicket.TicketNumber} print not yet implemented.";
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
