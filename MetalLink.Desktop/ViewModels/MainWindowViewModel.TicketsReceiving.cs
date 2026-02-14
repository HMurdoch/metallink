using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    public sealed class ReceivingLineItem : INotifyPropertyChanged
    {
        private bool _isEditable;
        private decimal _tare;

        public long TicketLineId { get; init; }
        public long TicketId { get; init; }
        public long ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public decimal FirstWeightKg { get; init; }      // For weighbridge: first weight reading (stored value)
        public decimal SecondWeightKg { get; init; }     // For weighbridge: second weight reading (stored value)
        public decimal WeightKg { get; init; }           // Net weight = FirstWeightKg - SecondWeightKg (stored value)
        public decimal UnitPricePerKg { get; init; }
        public decimal LineTotal { get; init; }
        public decimal VatAmount { get; init; }
        public decimal TotalInclVat { get; init; }
        
        public bool IsEditable
        {
            get => _isEditable;
            set
            {
                if (_isEditable != value)
                {
                    _isEditable = value;
                    OnPropertyChanged(nameof(IsEditable));
                }
            }
        }

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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
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
    
    private ObservableCollection<ReceivingLineItem> _receivingLinesWithTotals = new();
    
    /// <summary>
    /// Collection with just the receiving line items (no totals row - shown separately below grid)
    /// </summary>
    public ObservableCollection<ReceivingLineItem> ReceivingLinesWithTotals => ReceivingLines;

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

    private string _receivingLineNotes = string.Empty;
    public string ReceivingLineNotes
    {
        get => _receivingLineNotes;
        set
        {
            _receivingLineNotes = value;
            OnPropertyChanged();
        }
    }

    private string _selectedLineNotesContent = string.Empty;
    public string SelectedLineNotesContent
    {
        get => _selectedLineNotesContent;
        set
        {
            _selectedLineNotesContent = value;
            OnPropertyChanged();
        }
    }

    private bool _isNotesModalVisible = false;
    public bool IsNotesModalVisible
    {
        get => _isNotesModalVisible;
        set
        {
            _isNotesModalVisible = value;
            OnPropertyChanged();
        }
    }

    // Platform Weight visibility - only visible if ticket type is "platform"
    public bool ArePlatformFieldsVisible
    {
        get
        {
            var visible = SelectedTicketTypeOption?.Key?.ToLower() == "platform";
            Console.WriteLine($"[DEBUG] ArePlatformFieldsVisible: {visible}, SelectedTicketTypeOption={SelectedTicketTypeOption?.Key}");
            return visible;
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

    private decimal _receivingLinesTotalWeight;
    public decimal ReceivingLinesTotalWeight
    {
        get => _receivingLinesTotalWeight;
        private set
        {
            _receivingLinesTotalWeight = value;
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
        Console.WriteLine($"[DEBUG CREATE] LoadReceivingLinesForTicketAsync START: ticketId={ticketId}");
        
        if (ticketId <= 0)
        {
            Console.WriteLine($"[DEBUG CREATE] LoadReceivingLinesForTicketAsync: ticketId <= 0, clearing ReceivingLines");
            ReceivingLines.Clear();
            RecalculateReceivingTotals();
            return;
        }

        try
        {
            Console.WriteLine($"[DEBUG CREATE] Calling _ticketReceivingService.GetTicketReceivingLinesAsync({ticketId})");
            var lines = await _ticketReceivingService.GetTicketReceivingLinesAsync(ticketId);
            Console.WriteLine($"[DEBUG CREATE] Got {lines?.Count ?? 0} lines from API");

            ReceivingLines.Clear();
            Console.WriteLine($"[DEBUG CREATE] Cleared ReceivingLines");
            
            if (lines != null && lines.Count > 0)
            {
                Console.WriteLine($"[DEBUG CREATE] Adding {lines.Count} lines to ReceivingLines");
                foreach (var dto in lines)
                {
                    Console.WriteLine($"[DEBUG CREATE]   Adding line: ProductId={dto.ProductId}, ProductName={dto.ProductName}, NetWeightKg={dto.NetWeightKg}, Notes='{dto.Notes}'");
                    ReceivingLines.Add(new ReceivingLineItem
                    {
                        TicketLineId = dto.ReceivingTicketLineId,
                        TicketId = dto.ReceivingTicketId,
                        ProductId = dto.ProductId,
                        ProductName = dto.ProductName,
                        WeightKg = dto.NetWeightKg,
                        UnitPricePerKg = dto.UnitPricePerKg,
                        LineTotal = dto.LineTotal,
                        VatAmount = dto.VatAmount,
                        TotalInclVat = dto.TotalInclVat,
                        Notes = dto.Notes ?? string.Empty,
                    });
                }
            }
            else
            {
                Console.WriteLine($"[DEBUG CREATE] No lines returned or null");
            }

            RecalculateReceivingTotals();
            Console.WriteLine($"[DEBUG CREATE] RecalculateReceivingTotals called. ReceivingLines.Count={ReceivingLines.Count}, ReceivingLinesTotalWeight={ReceivingLinesTotalWeight}");

            StatusMessage = $"Loaded {ReceivingLines.Count} line(s) for ticket {ticketId}.";
            
            Console.WriteLine($"[DEBUG CREATE] LoadReceivingLinesForTicketAsync END: ReceivingLines.Count={ReceivingLines.Count}, ReceivingLinesWithTotals.Count={ReceivingLinesWithTotals.Count}, CreatingTicketTotalWeight={CreatingTicketTotalWeight}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG CREATE] ERROR in LoadReceivingLinesForTicketAsync: {ex.Message}");
            StatusMessage = $"Error loading ticket lines: {ex.Message}";
        }
    }

    private async Task AddReceivingLineAsync()
    {
        if (IsBusy) return;

        // If ticket hasn't been created yet, create it now with the header data
        if (LastCreatedTicket == null || LastCreatedTicket.TicketId <= 0)
        {
            // Create the ticket first
            IsBusy = true;
            StatusMessage = "Creating ticket...";

            try
            {
                // Determine ticket type
                int ticketTypeId = SelectedTicketTypeOption?.Key == "weighbridge" ? 1 : 2;
                
                // For weighbridge tickets, get First Weight to initialize_weight_kg
                decimal? initializeWeight = null;
                if (SelectedTicketTypeOption?.Key == "weighbridge" && 
                    decimal.TryParse(NormalizeDecimalText(TicketFirstWeightText), out var firstWeight))
                {
                    initializeWeight = firstWeight;
                    Console.WriteLine($"[DEBUG] Setting initialize_weight_kg to {initializeWeight} for weighbridge ticket");
                }

                // Create a NEW receiving ticket with state 'H' (Header) in a single INSERT operation
                // Do NOT create with default state and then UPDATE
                // Weighbridge ticket type ID is 1, Platform is 2
                bool isWeighbridgeTicket = ticketTypeId == 1;
                var createTicketDto = new CreateTicketReceivingDto
                {
                    TicketTypeId = ticketTypeId,
                    TicketNumber = TicketNumber,
                    CustomerId = (int)ExtractCustomerIdFromText(TicketCustomerIdText),
                    CreatedByOperatorId = 1,  // TODO: Get from authenticated user context
                    TicketState = 'H',  // Create directly with state 'H' (Header only)
                    InitializeWeightKg = initializeWeight,
                    NetWeightKg = 0m,  // No weight yet, will be calculated from lines
                    VehicleRegistration = isWeighbridgeTicket ? TicketVehicleRegistration : null,
                    TrailerRegistration = isWeighbridgeTicket ? TicketTrailerRegistration : null,
                    DriverName = isWeighbridgeTicket ? TicketDriverName : null,
                    Notes = TicketNotes
                };

                // Call API to create the ticket with state 'H' in a single INSERT
                var response = await _ticketReceivingService.CreateTicketAsync(createTicketDto);
                
                if (response == null || response.TicketReceivingId <= 0)
                {
                    StatusMessage = "Failed to create ticket.";
                    IsBusy = false;
                    return;
                }

                LastCreatedTicket = response != null ? MapReceivingToTicketDto(response) : null;
                CurrentTicketState = 'H';
                
                // After ticket is created with state 'H', First Weight becomes readonly (loaded from initialize_weight_kg)
                // Second Weight is now enabled for user input
                Console.WriteLine($"[DEBUG] Ticket created with state='H': FW is now readonly, SW is enabled");
                TicketSecondWeightText = "0"; // Reset Second Weight
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error creating ticket: {ex.Message}";
                IsBusy = false;
                return;
            }
        }

        if (ReceivingSelectedProduct == null)
        {
            StatusMessage = "Please select a product for the line.";
            IsBusy = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(ReceivingWeightText))
        {
            StatusMessage = "Weight (kg) is required.";
            IsBusy = false;
            return;
        }

        if (!decimal.TryParse(
                NormalizeDecimalText(ReceivingWeightText),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var weightKg) || weightKg <= 0)
        {
            StatusMessage = "Weight must be a valid number greater than zero.";
            IsBusy = false;
            return;
        }

        // For weighbridge tickets, validate Second Weight <= First Weight
        if (SelectedTicketTypeOption?.Key == "weighbridge")
        {
            if (!decimal.TryParse(NormalizeDecimalText(TicketSecondWeightText), out var secondWeight))
            {
                secondWeight = 0;
            }

            if (decimal.TryParse(NormalizeDecimalText(TicketFirstWeightText), out var firstWeight))
            {
                if (secondWeight > firstWeight)
                {
                    StatusMessage = $"Error: Second Weight ({secondWeight}) cannot be greater than First Weight ({firstWeight}).";
                    IsBusy = false;
                    return;
                }
            }
        }

        IsBusy = true;
        StatusMessage = "Adding ticket line...";

        try
        {
            // Parse FW and SW from UI textboxes
            decimal firstWeightKg = 0m;
            decimal secondWeightKg = 0m;
            
            if (decimal.TryParse(NormalizeDecimalText(TicketFirstWeightText), out var fw))
            {
                firstWeightKg = fw;
            }
            if (decimal.TryParse(NormalizeDecimalText(TicketSecondWeightText), out var sw))
            {
                secondWeightKg = sw;
            }
            
            Console.WriteLine($"[DEBUG ADD LINE] Creating line: ProductId={ReceivingSelectedProduct.ProductId}, FW={firstWeightKg}, SW={secondWeightKg}, NetWeight={weightKg}");
            
            // Create the line item DTO with FW and SW values
            var lineDto = new CreateTicketReceivingLineDto
            {
                ProductId = (int)ReceivingSelectedProduct.ProductId,
                FirstWeightKg = firstWeightKg,
                SecondWeightKg = secondWeightKg,
                NetWeightKg = weightKg,
                UnitPricePerKg = 0m,  // Will be looked up by API based on customer's price code
                Tare = 0m,  // Default tare to 0, user can update it later
                Notes = string.IsNullOrWhiteSpace(ReceivingLineNotes) ? null : ReceivingLineNotes
            };

            // Call the API to add the line and get back the updated ticket
            var response = await _apiClient.PostAsync<CreateTicketReceivingLineDto, TicketReceivingDto>(
                $"api/tickets-receiving/{LastCreatedTicket.TicketId}/lines",
                lineDto
            );

            if (response == null || response.Lines == null || response.Lines.Count == 0)
            {
                StatusMessage = "Ticket line create failed - API returned no result.";
                return;
            }

            // Clear and repopulate the receiving lines from the response
            ReceivingLines.Clear();
            // Determine the last ACTIVE line deterministically.
            // CreatedTime may be missing/zero in some responses; IDs are monotonic.
            var lastActiveLineId = response.Lines
                .Where(l => l.IsActive)
                .OrderBy(l => l.ReceivingTicketLineId)
                .LastOrDefault()?
                .ReceivingTicketLineId;
            foreach (var dto in response.Lines)
            { 
                Console.WriteLine($"[DEBUG LINE ADD RESPONSE] ProductId={dto.ProductId}, ProductName={dto.ProductName}, Notes='{dto.Notes}'");
                var lineItem = new ReceivingLineItem
                {
                    TicketLineId = dto.ReceivingTicketLineId,
                    TicketId = dto.ReceivingTicketId,
                    ProductId = dto.ProductId,
                    ProductName = dto.ProductName,
                    FirstWeightKg = dto.FirstWeightKg ?? 0m,
                    SecondWeightKg = dto.SecondWeightKg ?? 0m,
                    WeightKg = dto.NetWeightKg,
                    UnitPricePerKg = dto.UnitPricePerKg,
                    LineTotal = dto.LineTotal,
                    VatAmount = dto.VatAmount,
                    TotalInclVat = dto.TotalInclVat,
                    Notes = dto.Notes ?? string.Empty,
                };
                lineItem.Tare = dto.Tare;
                lineItem.PropertyChanged += (s, e) => 
                {
                    if (e.PropertyName == nameof(ReceivingLineItem.Tare))
                    {
                        RecalculateReceivingTotals();
                        if (lineItem.IsEditable)
                            _ = SaveTareToApiAsync(LastCreatedTicket.TicketId, lineItem.TicketLineId, lineItem.Tare);
                    }
                };
                ReceivingLines.Add(lineItem);
            }

            // Only last/current line editable/deletable
            foreach (var li in ReceivingLines)
                li.IsEditable = lastActiveLineId.HasValue && li.TicketLineId == lastActiveLineId.Value;

            RecalculateReceivingTotals();
            
            // Calculate FW from last line's SW and verify Total Net Weight
            CalculateReceivingWeights();

            StatusMessage = $"Added {response.Lines.Count} line(s) to ticket {LastCreatedTicket.TicketNumber}.";

            // DO NOT reload ticket details here - it causes confusion with old ticket data
            // The newly added lines are already displayed in the ReceivingLines collection above
            
            // Refresh the search results to show updated net weight
            await RefreshCurrentTicketInSearchResultsAsync();
            
            // Set state to 'M' (Multi-weight/Multi-item) after first line is added
            if (ReceivingLines.Count == 1)
            {
                // Update ticket state to 'M'
                await _ticketReceivingService.UpdateTicketStateAsync(LastCreatedTicket.TicketId, 'M');
                CurrentTicketState = 'M';
                Console.WriteLine($"[DEBUG] Set ticket state to 'M' after first line added");
            }
            
            // Notify that Finalize button may now be enabled
            OnPropertyChanged(nameof(IsFinalizeTicketEnabled));

            // Reset based on ticket type and state
            if (SelectedTicketTypeOption?.Key == "platform")
            {
                // For Platform: Reset Platform Weight to 0
                TicketPlatformWeightText = "0";
            }
            else if (SelectedTicketTypeOption?.Key == "weighbridge")
            {
                // For Weighbridge: 
                // After line is added, FW value becomes the SW value (cycling weights)
                // Then SW is reset to 0 for the next entry
                // Note: FW is readonly but we update it from the last line's SW value in the DB
                if (CurrentTicketState == 'M')
                {
                    // State 'M': Set First Weight to current Second Weight value (which was just saved to DB as second_weight_kg)
                    TicketFirstWeightText = TicketSecondWeightText;
                    Console.WriteLine($"[DEBUG] After line added: Set FW={TicketFirstWeightText} (from SW), reset SW to 0");
                }
                TicketSecondWeightText = "0";
            }

            // Reset weight, notes for next entry and clear product selection
            ReceivingWeightText = string.Empty;
            ReceivingLineNotes = string.Empty;
            ReceivingSelectedProduct = null;
            ReceivingProductSearchText = string.Empty;
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
        Console.WriteLine($"[TASK 2] RecalculateReceivingTotals START: ReceivingLines.Count={ReceivingLines.Count}");
        
        var totalWeight = 0m;
        var totalTare = 0m;
        var totalExcl = 0m;
        var totalVat = 0m;
        var totalIncl = 0m;

        foreach (var line in ReceivingLines)
        {
            // Calculate net weight after tare deduction: (NetWeightKg - Tare)
            var adjustedNetWeight = line.WeightKg - line.Tare;
            totalWeight += adjustedNetWeight;
            totalTare += line.Tare;
            totalExcl += line.LineTotal;
            totalVat += line.VatAmount;
            totalIncl += line.TotalInclVat;
            Console.WriteLine($"[TASK 2]   Line: ProductId={line.ProductId}, ProductName={line.ProductName}, WeightKg={line.WeightKg}, Tare={line.Tare}, AdjustedWeight={adjustedNetWeight}, FW={line.FirstWeightKg}, SW={line.SecondWeightKg}");
        }

        ReceivingLinesTotalWeight = totalWeight;
        ReceivingTotalExclVat = totalExcl;
        ReceivingTotalVat = totalVat;
        ReceivingTotalInclVat = totalIncl;
        
        // Notify UI that CreatingTicketTotalWeight has changed
        OnPropertyChanged(nameof(CreatingTicketTotalWeight));
        
        Console.WriteLine($"[TASK 2] RecalculateReceivingTotals END: ReceivingLinesTotalWeight={ReceivingLinesTotalWeight} (after deducting totalTare={totalTare}), CreatingTicketTotalWeight={CreatingTicketTotalWeight}");
        Console.WriteLine($"[TASK 2] ReceivingLinesWithTotals.Count={ReceivingLinesWithTotals.Count}");
    }

    private async Task RemoveReceivingLineAsync(ReceivingLineItem? line)
    {
        if (line == null)
        {
            StatusMessage = "No line item selected to remove.";
            return;
        }

        if (!line.IsEditable)
        {
            StatusMessage = "Only the last/current line item can be deleted.";
            return;
        }
        
        if (IsBusy) return;

        // Show confirmation dialog
        var confirmMessage = $"Are you sure you want to delete the line item for {line.ProductName}? (Weight: {line.WeightKg:N2} kg)";
        var confirmed = await ConfirmAsync(confirmMessage);
        if (!confirmed)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = "Deleting ticket line...";

        try
        {
            // Soft delete - sets is_active to false
            await _ticketService.DeleteTicketLineAsync(line.TicketId, line.TicketLineId);

            // Reload ticket details to refresh all grids and recalculate net weight
            await LoadSelectedReceivingTicketDetailsAsync(line.TicketId);
            
            // If all line items are deleted, set ticket status back to 'H' (Header only)
            if (SelectedReceivingTicketLines.Count == 0)
            {
                await _ticketReceivingService.UpdateTicketStateAsync(line.TicketId, 'H');
                CurrentTicketState = 'H';
                StatusMessage = $"✓ Deleted line for product {line.ProductName}. All lines removed. Ticket status reset to 'H'.";
            }
            else
            {
                StatusMessage = $"✓ Deleted line for product {line.ProductName}. Weight deducted from ticket.";
            }
            
            // Refresh search results
            await RefreshCurrentTicketInSearchResultsAsync();
            
            // Notify that Finalize button state may have changed
            OnPropertyChanged(nameof(IsFinalizeTicketEnabled));
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = $"Network error deleting ticket line: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting ticket line: {ex.Message}";
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

    private async Task FinalizeTicketAsync()
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

        // Build the confirmation message
        string confirmMessage = $"Are you sure you want to Finalize this ticket for Customer: {SelectedReceivingTicket?.CustomerId} | {SelectedReceivingTicket?.FirstName} {SelectedReceivingTicket?.LastName}";
        
        if (!string.IsNullOrWhiteSpace(SelectedReceivingTicket?.CompanyName))
        {
            confirmMessage += $" | {SelectedReceivingTicket.CompanyName} | {SelectedReceivingTicket.SiteName}";
        }

        var ok = await ConfirmAsync(confirmMessage);
        if (!ok) return;

        IsBusy = true;
        StatusMessage = "Finalizing ticket...";

        try
        {
            // Update ticket state to 'C' (Complete)
            var success = await _ticketReceivingService.UpdateTicketStateAsync(LastCreatedTicket.TicketId, 'C');
            
            if (!success)
            {
                StatusMessage = "Error updating ticket status to Complete. Please try again.";
                IsBusy = false;
                return;
            }
            
            StatusMessage = $"✓ Ticket {LastCreatedTicket.TicketNumber} finalized successfully.";
            
            // Clear the Create section and reset for new ticket
            await ClearTicketAsync();
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

    private async Task OnEnterTicketsReceivingAsync()
    {
        Console.WriteLine("[DEBUG] OnEnterTicketsReceivingAsync called");
        await ClearTicketAsync();
    }

    private async Task ClearTicketAsync()
    {
        Console.WriteLine("[DEBUG] ClearTicketAsync called");
        LastCreatedTicket = null;
        ReceivingLines.Clear();
        ReceivingWeightText = string.Empty;
        ReceivingSelectedProduct = null;
        ReceivingProductSearchText = string.Empty;
        
        // Reset all weights to 0
        TicketFirstWeightText = "0";
        TicketSecondWeightText = "0";
        TicketPlatformWeightText = "0";
        
        RecalculateReceivingTotals();
        
        // Initialize type options and set defaults
        Console.WriteLine("[DEBUG] Initializing ticket type options");
        InitializeTicketTypeOptions();
        SelectedTicketTypeOption = TicketTypeOptions.FirstOrDefault(t => t.Key == "platform");
        Console.WriteLine($"[DEBUG] SelectedTicketTypeOption set to {SelectedTicketTypeOption?.Key}");
        
        // Generate ticket number for Platform (prefix = "RPL")
        Console.WriteLine("[DEBUG] Calling GenerateTicketNumberForReceivingAsync with RPL");
        await GenerateTicketNumberForReceivingAsync("RPL");
        Console.WriteLine($"[DEBUG] After GenerateTicketNumberForReceivingAsync, TicketNumber={TicketNumber}");
        TicketCustomerIdText = string.Empty;
        
        // Initialize currency options and set default to ZAR
        InitializeCurrencyOptions();
        SelectedCurrency = "ZAR";
        
        // Reset ticket state to 'C' (Complete) to show Create Header button
        CurrentTicketState = 'C';
        CreateOrUpdateButtonText = "Create Header";
        IsViewingTicketOnly = false;
        
        StatusMessage = "Ticket cleared.";
    }

    private long ExtractCustomerIdFromText(string customerIdText)
    {
        // Format: "Customer ID | First Name Last Name" or "Customer ID | First Name Last Name | Company | Site"
        // Extract the first number before the pipe
        if (string.IsNullOrWhiteSpace(customerIdText))
            return 0;

        var parts = customerIdText.Split('|');
        if (parts.Length > 0 && long.TryParse(parts[0].Trim(), out var customerId))
        {
            return customerId;
        }

        return 0;
    }

    private TicketDto MapReceivingToTicketDto(TicketReceivingDto receivingDto)
    {
        return new TicketDto
        {
            TicketId = receivingDto.TicketReceivingId,
            CustomerId = receivingDto.CustomerId,
            TicketNumber = receivingDto.TicketNumber,
            TicketType = receivingDto.TicketTypeName,
            TicketTypeId = receivingDto.TicketTypeId,
            NetWeightKg = receivingDto.NetWeightKg,
            TicketState = receivingDto.TicketState,
            InitializeWeightKg = receivingDto.InitializeWeightKg
        };
    }

    private async Task GenerateTicketNumberForReceivingAsync(string prefix)
    {
        Console.WriteLine($"[DEBUG] GenerateTicketNumberForReceivingAsync called with prefix={prefix}");
        Console.WriteLine($"[DEBUG] Before generation - TicketNumber={TicketNumber}, IsViewingTicketOnly={IsViewingTicketOnly}");
        TicketNumber = await _ticketReceivingService.GenerateTicketNumberAsync(prefix);
        Console.WriteLine($"[DEBUG] Generated TicketNumber={TicketNumber}");
        Console.WriteLine($"[DEBUG] After generation - TicketNumber={TicketNumber}");
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

    private void ShowLineNotes(string? notes)
    {
        Console.WriteLine($"[DEBUG NOTES] ShowLineNotes called with notes='{notes}'");
        if (string.IsNullOrWhiteSpace(notes))
        {
            Console.WriteLine($"[DEBUG NOTES] Notes are empty/null, showing message");
            StatusMessage = "No notes available for this line item.";
            return;
        }

        Console.WriteLine($"[DEBUG NOTES] Setting SelectedLineNotesContent='{notes}', opening modal");
        SelectedLineNotesContent = notes;
        IsNotesModalVisible = true;
    }

    private void CloseLineNotes()
    {
        IsNotesModalVisible = false;
        SelectedLineNotesContent = string.Empty;
    }

    private async Task CaptureLoadPhotoAsync()
    {
        if (IsBusy) return;

        StatusMessage = "Capturing load photo...";
        // TODO: Implement load photo capture
        await Task.Delay(100);
        StatusMessage = "Load photo capture not yet implemented.";
    }

    /// <summary>
    /// Generate a random weight between 50 and 2000 kg
    /// Used for filling missing weight values in old data
    /// </summary>
    private decimal GenerateRandomWeight()
    {
        var random = new Random();
        // Generate weight between 50 and 2000 kg
        return (decimal)(random.NextDouble() * 1950 + 50);
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

    private bool _searchReceivingNewCustomersCheckbox;
    public bool SearchReceivingNewCustomersCheckbox
    {
        get => _searchReceivingNewCustomersCheckbox;
        set 
        { 
            _searchReceivingNewCustomersCheckbox = value; 
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShouldShowTicketDetails));
        }
    }

    public ObservableCollection<TicketSearchResultDto> ReceivingTicketSearchResults { get; } = new();
    
    public ObservableCollection<NewCustomerResultDto> ReceivingNewCustomerSearchResults { get; } = new();
    
    private NewCustomerResultDto? _selectedReceivingNewCustomer;
    public NewCustomerResultDto? SelectedReceivingNewCustomer
    {
        get => _selectedReceivingNewCustomer;
        set
        {
            _selectedReceivingNewCustomer = value;
            OnPropertyChanged();
            
            // Auto-populate the Customer ID in Create Ticket section when a new customer is selected
            if (value != null)
            {
                // Format: "Customer ID | First Name Last Name" or "Customer ID | First Name Last Name | Company | Site"
                string fullCustomerInfo;
                if (string.IsNullOrWhiteSpace(value.CompanyName))
                {
                    // No company, just show customer ID and name
                    fullCustomerInfo = $"{value.CustomerId} | {value.FirstName} {value.LastName}";
                }
                else
                {
                    // Include company and site
                    fullCustomerInfo = $"{value.CustomerId} | {value.FirstName} {value.LastName} | {value.CompanyName} | {value.SiteName}";
                }
                TicketCustomerIdText = fullCustomerInfo;
                
                // Reset all weighbridge fields for new customer
                TicketVehicleRegistration = string.Empty;
                TicketTrailerRegistration = string.Empty;
                TicketDriverName = string.Empty;
                TicketOfmWeighbridgeTicket = string.Empty;
                TicketForeignTicket = string.Empty;
                TicketCkNumber = string.Empty;
                TicketNotes = string.Empty;
                
                // Set ticket type to Platform as default for new customers
                InitializeTicketTypeOptions();
                SelectedTicketTypeOption = TicketTypeOptions.FirstOrDefault(t => t.Key == "platform");
                
                StatusMessage = $"Customer {value.FirstName} {value.LastName} selected. You can now create a ticket.";
            }
        }
    }


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
            OnPropertyChanged(nameof(ShouldShowTicketDetails));
            
            // Only load ticket details if NOT searching for new customers
            if (value != null && !SearchReceivingNewCustomersCheckbox)
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
    
    /// <summary>
    /// Returns true if the currently selected ticket type is weighbridge
    /// </summary>
    public bool IsWeighbridgeTicket
    {
        get
        {
            var result = SelectedTicketTypeOption?.Key == "weighbridge";
            return result;
        }
    }
    
    /// <summary>
    /// Calculates total net weight from all receiving lines and sets FW from last line's SW
    /// </summary>
    private void CalculateReceivingWeights()
    {
        Console.WriteLine($"[CALC WEIGHTS] START: ReceivingLines.Count={ReceivingLines.Count}");
        
        decimal totalNetWeightKg = 0m;
        decimal firstWeightFromLastLine = 0m;
        
        foreach (var line in ReceivingLines)
        {
            totalNetWeightKg += line.WeightKg;
            firstWeightFromLastLine = line.SecondWeightKg;  // Will end up with last line's SW
            Console.WriteLine($"[CALC WEIGHTS]   Line: ProductId={line.ProductId}, WeightKg={line.WeightKg}, SecondWeightKg={line.SecondWeightKg}");
            Console.WriteLine($"[CALC WEIGHTS]     Running total: totalNetWeightKg={totalNetWeightKg}, firstWeightFromLastLine={firstWeightFromLastLine}");
        }
        
        // Bind to UI
        TicketFirstWeightText = firstWeightFromLastLine.ToString("0.00");
        Console.WriteLine($"[CALC WEIGHTS] SET TicketFirstWeightText={TicketFirstWeightText}");
        Console.WriteLine($"[CALC WEIGHTS] SET Total Net Weight will be displayed via ReceivingLinesTotalWeight={ReceivingLinesTotalWeight}");
        Console.WriteLine($"[CALC WEIGHTS] END: IsWeighbridgeTicket={IsWeighbridgeTicket}");
    }

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

    public decimal SelectedReceivingTicketLinesTotalExVat => SelectedReceivingTicketLines.Sum(l => l.LineTotal);
    public decimal SelectedReceivingTicketLinesTotalVat => SelectedReceivingTicketLines.Sum(l => l.VatAmount);

    public decimal SelectedReceivingTicketLinesTotalInclVat => SelectedReceivingTicketLines.Sum(l => l.TotalInclVat);
    
    public decimal SelectedReceivingTicketLinesWeightTotal => SelectedReceivingTicketLines.Sum(l => l.WeightKg);
    
    /// <summary>
    /// Collection with just the line items (no totals row - shown separately below grid)
    /// </summary>
    public ObservableCollection<TicketLineDto> SelectedReceivingTicketLinesWithTotals => SelectedReceivingTicketLines;


    private async Task LoadSelectedReceivingTicketDetailsAsync(long ticketId)
    {
        try
        {
            // API Call
            Console.WriteLine($"[DEBUG] GET: http://localhost:5066/api/tickets-receiving/{ticketId}");
            var receivingDetails = await _ticketReceivingService.GetTicketReceivingByIdAsync(ticketId);
            
            if (receivingDetails == null)
            {
                StatusMessage = "Failed to load ticket details.";
                return;
            }

            // Results for Ticket Number
            Console.WriteLine($"[DEBUG] Results for Ticket Number [{receivingDetails.TicketNumber}]:");
            Console.WriteLine($"[DEBUG]   TicketReceivingId: {receivingDetails.TicketReceivingId}");
            Console.WriteLine($"[DEBUG]   TicketNumber: {receivingDetails.TicketNumber}");
            Console.WriteLine($"[DEBUG]   TicketState: {receivingDetails.TicketState}");
            Console.WriteLine($"[DEBUG]   CustomerId: {receivingDetails.CustomerId}");
            Console.WriteLine($"[DEBUG]   VehicleRegistration: {receivingDetails.VehicleRegistration}");
            Console.WriteLine($"[DEBUG]   TrailerRegistration: {receivingDetails.TrailerRegistration}");
            Console.WriteLine($"[DEBUG]   DriverName: {receivingDetails.DriverName}");
            Console.WriteLine($"[DEBUG]   OfmWeighbridgeTicket: {receivingDetails.OfmWeighbridgeTicket}");
            Console.WriteLine($"[DEBUG]   ForeignTicket: {receivingDetails.ForeignTicket}");
            Console.WriteLine($"[DEBUG]   CkNumber: {receivingDetails.CkNumber}");
            Console.WriteLine($"[DEBUG]   DeliveryNumber: {receivingDetails.DeliveryNumber}");
            Console.WriteLine($"[DEBUG]   Notes: {receivingDetails.Notes}");
            Console.WriteLine($"[DEBUG]   NetWeightKg: {receivingDetails.NetWeightKg}");
            Console.WriteLine($"[DEBUG]   InitializeWeightKg: {receivingDetails.InitializeWeightKg}");
            Console.WriteLine($"[DEBUG]   Lines count: {receivingDetails.Lines?.Count ?? 0}");

            // Determine ticket type from ticket number prefix
            string ticketTypeKey = receivingDetails.TicketNumber?.StartsWith("RWB") == true ? "weighbridge" : "platform";
            char ticketState = receivingDetails.TicketState;

            Console.WriteLine($"[DEBUG] Switch on TicketState '{ticketState}':");

            switch (ticketState)
            {
                case 'H': // Header only - can edit and add lines
                case 'M': // Multi-item - can edit and add more lines
                    Console.WriteLine($"[DEBUG]   State is '{ticketState}' - EDITABLE MODE");
                    HandleIncompleteTicket(receivingDetails, ticketTypeKey, ticketState);
                    break;

                case 'C': // Complete - viewing only, can create new ticket for same customer
                    Console.WriteLine($"[DEBUG]   State is 'C' (Complete) - VIEWING ONLY MODE");
                    await HandleCompleteTicket(receivingDetails, ticketTypeKey);
                    break;

                default:
                    Console.WriteLine($"[DEBUG]   State '{ticketState}' is unknown - treating as Complete");
                    await HandleCompleteTicket(receivingDetails, ticketTypeKey);
                    break;
            }

            OnPropertyChanged(nameof(SelectedReceivingTicketLinesTotalExVat));
            OnPropertyChanged(nameof(SelectedReceivingTicketLinesTotalVat));
            OnPropertyChanged(nameof(SelectedReceivingTicketLinesTotalInclVat));
            OnPropertyChanged(nameof(CalculatedNetWeightKg));

            Console.WriteLine($"[DEBUG] LoadSelectedReceivingTicketDetailsAsync complete. CurrentTicketState='{CurrentTicketState}', IsViewingTicketOnly={IsViewingTicketOnly}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Exception in LoadSelectedReceivingTicketDetailsAsync: {ex.Message}");
            StatusMessage = $"Error loading ticket details: {ex.Message}";
        }
    }

    private void HandleIncompleteTicket(TicketReceivingDto receivingDetails, string ticketTypeKey, char ticketState)
    {
        Console.WriteLine($"[DEBUG] >>> HandleIncompleteTicket START with ticketState='{ticketState}'");
        
        // CRITICAL FIX: Set LastCreatedTicket FIRST so all subsequent state changes persist
        Console.WriteLine($"[DEBUG CRITICAL] Setting LastCreatedTicket from receivingDetails (TicketId={receivingDetails.TicketReceivingId})");
        LastCreatedTicket = MapReceivingToTicketDto(receivingDetails);
        Console.WriteLine($"[DEBUG CRITICAL] LastCreatedTicket is now: TicketId={LastCreatedTicket?.TicketId}, TicketNumber={LastCreatedTicket?.TicketNumber}, State={LastCreatedTicket?.TicketState}");
        
        // Set state
        CurrentTicketState = ticketState;
        Console.WriteLine($"[DEBUG] After setting CurrentTicketState, value is now: '{CurrentTicketState}'");
        Console.WriteLine($"[DEBUG] CreateHeaderButtonVisible should be: {CreateHeaderButtonVisible} (expects false for state H/M)");
        Console.WriteLine($"[DEBUG] SaveResetButtonVisible should be: {SaveResetButtonVisible} (expects true for state H/M)");
        Console.WriteLine($"[DEBUG] AddLineButtonEnabled should be: {AddLineButtonEnabled} (expects true for state H/M)");
        Console.WriteLine($"[DEBUG] IsFinalizeTicketEnabled should be: {IsFinalizeTicketEnabled} (expects true for state H/M)");
        
        // CRITICAL: Trigger property change notifications so UI updates button states
        OnPropertyChanged(nameof(CreateHeaderButtonVisible));
        OnPropertyChanged(nameof(SaveResetButtonVisible));
        OnPropertyChanged(nameof(AddLineButtonEnabled));
        OnPropertyChanged(nameof(IsFinalizeTicketEnabled));
        Console.WriteLine($"[DEBUG NOTIFY] Property change notifications triggered for button visibility/enabled states");
        
        IsViewingTicketOnly = false;
        IsLoadingExistingTicket = true;

        // Populate ticket type
        InitializeTicketTypeOptions();
        var ticketTypeOption = TicketTypeOptions.FirstOrDefault(t => t.Key == ticketTypeKey);
        if (ticketTypeOption != null)
        {
            SelectedTicketTypeOption = ticketTypeOption;
        }

        // Populate all ticket fields
        TicketNumber = receivingDetails.TicketNumber ?? string.Empty;
        TicketVehicleRegistration = receivingDetails.VehicleRegistration ?? string.Empty;
        TicketTrailerRegistration = receivingDetails.TrailerRegistration ?? string.Empty;
        TicketDriverName = receivingDetails.DriverName ?? string.Empty;
        TicketOfmWeighbridgeTicket = receivingDetails.OfmWeighbridgeTicket ?? string.Empty;
        TicketForeignTicket = receivingDetails.ForeignTicket ?? string.Empty;
        TicketCkNumber = receivingDetails.CkNumber ?? string.Empty;
        TicketDeliveryNumber = receivingDetails.DeliveryNumber ?? string.Empty;
        TicketNotes = receivingDetails.Notes ?? string.Empty;
        
        // DEBUG: Log all populated fields
        Console.WriteLine($"[DEBUG POPULATE] >>> HandleIncompleteTicket POPULATE ALL FIELDS START:");
        Console.WriteLine($"[DEBUG POPULATE]   TicketNumber: '{TicketNumber}'");
        Console.WriteLine($"[DEBUG POPULATE]   VehicleRegistration: '{TicketVehicleRegistration}'");
        Console.WriteLine($"[DEBUG POPULATE]   TrailerRegistration: '{TicketTrailerRegistration}'");
        Console.WriteLine($"[DEBUG POPULATE]   DriverName: '{TicketDriverName}'");
        Console.WriteLine($"[DEBUG POPULATE]   OfmWeighbridgeTicket: '{TicketOfmWeighbridgeTicket}'");
        Console.WriteLine($"[DEBUG POPULATE]   DeliveryNumber: '{TicketDeliveryNumber}'");
        Console.WriteLine($"[DEBUG POPULATE]   ForeignTicket: '{TicketForeignTicket}'");
        Console.WriteLine($"[DEBUG POPULATE]   CkNumber: '{TicketCkNumber}'");
        Console.WriteLine($"[DEBUG POPULATE]   Notes: '{TicketNotes}'");

        // Populate customer
        if (SelectedReceivingTicket != null)
        {
            string fullCustomerInfo;
            if (string.IsNullOrWhiteSpace(SelectedReceivingTicket.CompanyName))
            {
                fullCustomerInfo = $"{SelectedReceivingTicket.CustomerId} | {SelectedReceivingTicket.FirstName} {SelectedReceivingTicket.LastName}";
            }
            else
            {
                fullCustomerInfo = $"{SelectedReceivingTicket.CustomerId} | {SelectedReceivingTicket.FirstName} {SelectedReceivingTicket.LastName} | {SelectedReceivingTicket.CompanyName} | {SelectedReceivingTicket.SiteName}";
            }
            TicketCustomerIdText = fullCustomerInfo;
        }

        // Set weighbridge weights
        if (ticketTypeKey == "weighbridge")
        {
            TicketFirstWeightText = receivingDetails.InitializeWeightKg?.ToString("0.00") ?? "0";
            TicketSecondWeightText = "0";
        }
        else
        {
            TicketPlatformWeightText = "0";
        }

        // Load lines
        SelectedReceivingTicketLines.Clear();
        ReceivingLines.Clear();

        var lines = receivingDetails.Lines ?? new List<TicketReceivingLineDto>();
        if (lines.Count > 0)
        {
            foreach (var line in lines)
            {
                // Add to Details section (for viewing)
                var lineDto = new TicketLineDto
                {
                    TicketLineId = line.ReceivingTicketLineId,
                    TicketId = line.ReceivingTicketId,
                    ProductId = line.ProductId,
                    ProductName = line.ProductName,
                    WeightKg = line.NetWeightKg,
                    FirstWeightKg = line.FirstWeightKg,
                    SecondWeightKg = line.SecondWeightKg,
                    UnitPricePerKg = line.UnitPricePerKg,
                    LineTotal = line.LineTotal,
                    VatAmount = line.VatAmount,
                    TotalInclVat = line.TotalInclVat,
                    Tare = line.Tare,
                    Notes = line.Notes ?? string.Empty
                };
                SelectedReceivingTicketLines.Add(lineDto);

                // Also add to Create section (for editing)
                var lineItem = new ReceivingLineItem
                {
                    TicketLineId = line.ReceivingTicketLineId,
                    TicketId = line.ReceivingTicketId,
                    ProductId = line.ProductId,
                    ProductName = line.ProductName,
                    FirstWeightKg = line.FirstWeightKg ?? 0m,
                    SecondWeightKg = line.SecondWeightKg ?? 0m,
                    WeightKg = line.NetWeightKg,
                    UnitPricePerKg = line.UnitPricePerKg,
                    LineTotal = line.LineTotal,
                    VatAmount = line.VatAmount,
                    TotalInclVat = line.TotalInclVat,
                    Notes = line.Notes ?? string.Empty,
                };
                lineItem.Tare = line.Tare;
                // IsEditable will be set after all lines are loaded (only last/current line editable)
                lineItem.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(ReceivingLineItem.Tare))
                    {
                        RecalculateReceivingTotals();
                        if (lineItem.IsEditable)
                            _ = SaveTareToApiAsync(receivingDetails.TicketReceivingId, lineItem.TicketLineId, lineItem.Tare);
                    }
                };
                ReceivingLines.Add(lineItem);
            }

            // Set which line is editable: ONLY the last active line.
            // CreatedTime can be unreliable across clients; IDs are monotonic.
            var lastActiveLineId = lines.Where(l => l.IsActive).OrderBy(l => l.ReceivingTicketLineId).LastOrDefault()?.ReceivingTicketLineId;
            foreach (var li in ReceivingLines)
                li.IsEditable = lastActiveLineId.HasValue && li.TicketLineId == lastActiveLineId.Value;

            // If lines are loaded and the ticket is in Multi-line state,
            // set First Weight to the last active line's Second Weight.
            if (ticketState == 'M' && lines.Count > 0)
            {
                var lastActiveLine = lines
                    .Where(l => l.IsActive)
                    .OrderBy(l => l.ReceivingTicketLineId)
                    .LastOrDefault();

                if (lastActiveLine != null)
                {
                    TicketFirstWeightText = (lastActiveLine.SecondWeightKg ?? 0m).ToString("0.00");
                    Console.WriteLine($"[DEBUG] Ticket has {lines.Count} lines. (State M) Set FirstWeightText from last ACTIVE line's SecondWeightKg: {TicketFirstWeightText}");
                }
            }
        }

        RecalculateReceivingTotals();

        // Set LastCreatedTicket so buttons work
        LastCreatedTicket = new TicketDto
        {
            TicketId = receivingDetails.TicketReceivingId,
            TicketNumber = receivingDetails.TicketNumber,
            TicketState = ticketState
        };

        // Set SelectedReceivingTicketDetails so the details grid displays
        SelectedReceivingTicketDetails = new TicketDto
        {
            TicketId = receivingDetails.TicketReceivingId,
            CustomerId = receivingDetails.CustomerId,
            TicketNumber = receivingDetails.TicketNumber,
            TicketTypeId = receivingDetails.TicketTypeId,
            TicketType = receivingDetails.TicketTypeName,
            TicketState = receivingDetails.TicketState,
            NetWeightKg = receivingDetails.NetWeightKg,
            InitializeWeightKg = receivingDetails.InitializeWeightKg,
            VehicleRegistration = receivingDetails.VehicleRegistration,
            TrailerRegistration = receivingDetails.TrailerRegistration,
            DriverName = receivingDetails.DriverName,
            OfmWeighbridgeTicket = receivingDetails.OfmWeighbridgeTicket,
            ForeignTicket = receivingDetails.ForeignTicket,
            CkNumber = receivingDetails.CkNumber,
            DeliveryNumber = receivingDetails.DeliveryNumber,
            Notes = receivingDetails.Notes,
            CreatedTime = receivingDetails.CreatedTime,
            UpdatedTime = receivingDetails.UpdatedTime
        };

        Console.WriteLine($"[DEBUG] >>> HandleIncompleteTicket END - Details populated, Ready for editing/adding lines");
        Console.WriteLine($"[DEBUG] FINAL STATE AT END OF HandleIncompleteTicket:");
        Console.WriteLine($"[DEBUG]   CurrentTicketState: '{CurrentTicketState}'");
        Console.WriteLine($"[DEBUG]   CreateHeaderButtonVisible: {CreateHeaderButtonVisible}");
        Console.WriteLine($"[DEBUG]   SaveResetButtonVisible: {SaveResetButtonVisible}");
        Console.WriteLine($"[DEBUG]   AddLineButtonEnabled: {AddLineButtonEnabled}");
        Console.WriteLine($"[DEBUG]   IsFinalizeTicketEnabled: {IsFinalizeTicketEnabled}");
        Console.WriteLine($"[DEBUG]   IsViewingTicketOnly: {IsViewingTicketOnly}");
        Console.WriteLine($"[DEBUG]   LastCreatedTicket?.TicketState: {LastCreatedTicket?.TicketState}");
        Console.WriteLine($"[DEBUG]   SelectedReceivingTicketDetails?.TicketState: {SelectedReceivingTicketDetails?.TicketState}");
    }

    private async Task HandleCompleteTicket(TicketReceivingDto receivingDetails, string ticketTypeKey)
    {
        Console.WriteLine($"[DEBUG] >>> HandleCompleteTicket START");
        
        // Set state
        CurrentTicketState = 'C';
        IsViewingTicketOnly = true;
        LastCreatedTicket = null;

        // Populate ticket type
        InitializeTicketTypeOptions();
        var ticketTypeOption = TicketTypeOptions.FirstOrDefault(t => t.Key == ticketTypeKey);
        if (ticketTypeOption != null)
        {
            SelectedTicketTypeOption = ticketTypeOption;
        }

        // Populate all ticket fields for viewing
        TicketNumber = receivingDetails.TicketNumber ?? string.Empty;
        TicketVehicleRegistration = receivingDetails.VehicleRegistration ?? string.Empty;
        TicketTrailerRegistration = receivingDetails.TrailerRegistration ?? string.Empty;
        TicketDriverName = receivingDetails.DriverName ?? string.Empty;
        TicketOfmWeighbridgeTicket = receivingDetails.OfmWeighbridgeTicket ?? string.Empty;
        TicketForeignTicket = receivingDetails.ForeignTicket ?? string.Empty;
        TicketCkNumber = receivingDetails.CkNumber ?? string.Empty;
        TicketDeliveryNumber = receivingDetails.DeliveryNumber ?? string.Empty;
        TicketNotes = receivingDetails.Notes ?? string.Empty;

        // Populate customer
        if (SelectedReceivingTicket != null)
        {
            string fullCustomerInfo;
            if (string.IsNullOrWhiteSpace(SelectedReceivingTicket.CompanyName))
            {
                fullCustomerInfo = $"{SelectedReceivingTicket.CustomerId} | {SelectedReceivingTicket.FirstName} {SelectedReceivingTicket.LastName}";
            }
            else
            {
                fullCustomerInfo = $"{SelectedReceivingTicket.CustomerId} | {SelectedReceivingTicket.FirstName} {SelectedReceivingTicket.LastName} | {SelectedReceivingTicket.CompanyName} | {SelectedReceivingTicket.SiteName}";
            }
            TicketCustomerIdText = fullCustomerInfo;
        }

        // Load lines for viewing only
        SelectedReceivingTicketLines.Clear();
        ReceivingLines.Clear();

        var lines = receivingDetails.Lines ?? new List<TicketReceivingLineDto>();
        if (lines.Count > 0)
        {
            foreach (var line in lines)
            {
                var lineDto = new TicketLineDto
                {
                    TicketLineId = line.ReceivingTicketLineId,
                    TicketId = line.ReceivingTicketId,
                    ProductId = line.ProductId,
                    ProductName = line.ProductName,
                    WeightKg = line.NetWeightKg,
                    FirstWeightKg = line.FirstWeightKg,
                    SecondWeightKg = line.SecondWeightKg,
                    UnitPricePerKg = line.UnitPricePerKg,
                    LineTotal = line.LineTotal,
                    VatAmount = line.VatAmount,
                    TotalInclVat = line.TotalInclVat,
                    Tare = line.Tare,
                    Notes = line.Notes ?? string.Empty
                };
                SelectedReceivingTicketLines.Add(lineDto);
            }
        }

        // Set SelectedReceivingTicketDetails so the details grid displays
        SelectedReceivingTicketDetails = new TicketDto
        {
            TicketId = receivingDetails.TicketReceivingId,
            CustomerId = receivingDetails.CustomerId,
            TicketNumber = receivingDetails.TicketNumber,
            TicketTypeId = receivingDetails.TicketTypeId,
            TicketType = receivingDetails.TicketTypeName,
            TicketState = receivingDetails.TicketState,
            NetWeightKg = receivingDetails.NetWeightKg,
            InitializeWeightKg = receivingDetails.InitializeWeightKg,
            VehicleRegistration = receivingDetails.VehicleRegistration,
            TrailerRegistration = receivingDetails.TrailerRegistration,
            DriverName = receivingDetails.DriverName,
            OfmWeighbridgeTicket = receivingDetails.OfmWeighbridgeTicket,
            ForeignTicket = receivingDetails.ForeignTicket,
            CkNumber = receivingDetails.CkNumber,
            DeliveryNumber = receivingDetails.DeliveryNumber,
            Notes = receivingDetails.Notes,
            CreatedTime = receivingDetails.CreatedTime,
            UpdatedTime = receivingDetails.UpdatedTime
        };

        // Generate new ticket number for creating a new ticket for same customer
        string prefix = ticketTypeKey == "weighbridge" ? "RWB" : "RPL";
        await GenerateTicketNumberForReceivingAsync(prefix);

        Console.WriteLine($"[DEBUG] >>> HandleCompleteTicket END - Ticket viewed, New number generated for new ticket");
    }
    private void UpdateSearchResultsWithNewWeight(TicketDto? details)
    {
        if (details == null || SelectedReceivingTicket == null)
            return;

        // Find the corresponding search result and update its net weight
        var searchResult = ReceivingTicketSearchResults.FirstOrDefault(r => r.TicketId == details.TicketId);
        if (searchResult != null)
        {
            // Update the NetWeightKg in the search result
            searchResult.NetWeightKg = details.NetWeightKg;
            Console.WriteLine($"[DEBUG] Updated search result for ticket {details.TicketNumber}: NetWeightKg = {details.NetWeightKg}");
        }
    }

    private async Task RefreshCurrentTicketInSearchResultsAsync()
    {
        if (LastCreatedTicket == null || LastCreatedTicket.TicketId <= 0)
            return;

        try
        {
            // Reload the ticket from the server to get updated weight
            var updatedTicket = await _ticketService.GetTicketByIdAsync(LastCreatedTicket.TicketId);
            if (updatedTicket != null)
            {
                // Update the search result with the new weight
                var searchResult = ReceivingTicketSearchResults.FirstOrDefault(r => r.TicketId == updatedTicket.TicketId);
                if (searchResult != null)
                {
                    searchResult.NetWeightKg = updatedTicket.NetWeightKg;
                    Console.WriteLine($"[DEBUG] Refreshed search result for {updatedTicket.TicketNumber}: NetWeightKg = {updatedTicket.NetWeightKg}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Error refreshing search results: {ex.Message}");
        }
    }

    private async Task SearchReceivingTicketsAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        
        // Initialize ticket type options for search dropdown
        InitializeTicketTypeOptions();

        try
        {
            if (SearchReceivingNewCustomersCheckbox)
            {
                StatusMessage = "Searching for customers without tickets...";
                
                var request = new TicketSearchRequestDto
                {
                    CustomerId = ParseLongOrNull(SearchReceivingTicketCustomerIdText),
                    IdNumber = string.IsNullOrWhiteSpace(SearchReceivingTicketIdNumberText) ? null : SearchReceivingTicketIdNumberText.Trim(),
                    FirstName = string.IsNullOrWhiteSpace(SearchReceivingTicketFirstNameText) ? null : SearchReceivingTicketFirstNameText.Trim(),
                    LastName = string.IsNullOrWhiteSpace(SearchReceivingTicketLastNameText) ? null : SearchReceivingTicketLastNameText.Trim(),
                    AccountNumber = ParseLongOrNull(SearchReceivingTicketAccountNumberText),
                    CompanyId = SelectedSearchCompany?.CompanyId,
                    SiteId = SelectedSearchSite?.SiteId,
                    SearchNewCustomersWithoutTickets = true
                };

                var results = await _ticketService.SearchNewCustomersAsync(request);

                ReceivingNewCustomerSearchResults.Clear();
                foreach (var c in results)
                {
                    ReceivingNewCustomerSearchResults.Add(new NewCustomerResultDto
                    {
                        CustomerId = c.CustomerId,
                        FirstName = c.FirstName,
                        LastName = c.LastName,
                        CompanyName = c.CompanyName,
                        SiteName = c.SiteName,
                        AccountNumber = c.AccountNumber?.ToString(),
                        CreatedTime = c.CreatedTime
                    });
                }

                SelectedReceivingNewCustomer = ReceivingNewCustomerSearchResults.FirstOrDefault();

                StatusMessage = $"Loaded {ReceivingNewCustomerSearchResults.Count} customer(s) without tickets.";
            }
            else
            {
                StatusMessage = "Searching receiving tickets...";
                
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
        }
        catch (Exception ex)
        {
            StatusMessage = $"Search failed: {ex.Message}";
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
        SearchReceivingNewCustomersCheckbox = false;

        ReceivingTicketSearchResults.Clear();
        ReceivingNewCustomerSearchResults.Clear();
        SelectedReceivingTicket = null;
        SelectedReceivingNewCustomer = null;
        SelectedReceivingTicketDetails = null;
        SelectedReceivingTicketLines.Clear();
        
        StatusMessage = "Search cleared.";
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

    private async Task SaveTareToApiAsync(long ticketId, long lineId, decimal tare)
    {
        try
        {
            var url = $"http://localhost:5066/api/tickets-receiving/{ticketId}/lines/{lineId}/tare";
            var request = new { tare };
            
            using var client = new HttpClient();
            var json = System.Text.Json.JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await client.PutAsync(url, content);
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[DEBUG] Tare updated successfully for line {lineId}: {tare}");
            }
            else
            {
                Console.WriteLine($"[DEBUG] Failed to update Tare for line {lineId}: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Error saving Tare to API: {ex.Message}");
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
