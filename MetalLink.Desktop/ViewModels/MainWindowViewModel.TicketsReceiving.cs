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
                        _ = SaveTareToApiAsync(LastCreatedTicket.TicketId, lineItem.TicketLineId, lineItem.Tare);
                    }
                };
                ReceivingLines.Add(lineItem);
            }

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
    
    /// <summary>
    /// Calculated Net Weight as the SUM of all line item weights
    /// For VIEWING existing tickets: Falls back to the ticket's net weight if no lines are loaded yet
    /// </summary>
    public decimal CalculatedNetWeightKg 
    { 
        get
        {
            var sumFromLines = SelectedReceivingTicketLines.Sum(l => l.WeightKg);
            // If lines are loaded, use their sum; otherwise use the ticket's net weight
            if (sumFromLines > 0 || SelectedReceivingTicketLines.Count > 0)
            {
                return sumFromLines;
            }
            // Fallback to ticket details value
            return SelectedReceivingTicketDetails?.NetWeightKg ?? 0m;
        }
    }

    /// <summary>
    /// Total weight from line items being created
    /// For the CREATE section: Always shows the sum of ReceivingLines (0.00 for new tickets with no lines yet)
    /// </summary>
    public decimal CreatingTicketTotalWeight 
    { 
        get => ReceivingLinesTotalWeight;  // This is already the sum of ReceivingLines
        set => ReceivingLinesTotalWeight = value;
    }

    private async Task LoadSelectedReceivingTicketDetailsAsync(long ticketId)
    {
        Console.WriteLine($"[DEBUG] LoadSelectedReceivingTicketDetailsAsync called with ticketId={ticketId}");
        try
        {
            var details = await _ticketService.GetTicketByIdAsync(ticketId);
            Console.WriteLine($"[DEBUG] Got ticket details: {details?.TicketNumber}, Type: {details?.TicketType}");
            SelectedReceivingTicketDetails = details;

            // IMPORTANT: When loading a ticket from search for VIEWING, we set IsViewingTicketOnly = true
            // This prevents the Create section from being populated with this ticket's data
            // LastCreatedTicket should ONLY be set when creating a NEW ticket from scratch
            IsViewingTicketOnly = true;
            
            // Clear the Create section - we're viewing an existing ticket, not creating a new one
            ReceivingLines.Clear();
            LastCreatedTicket = null;  // CRITICAL: Do NOT set LastCreatedTicket when viewing existing tickets!
            // Note: Don't clear TicketCustomerIdText yet - we may populate it below if creating from a complete ticket
            TicketNumber = string.Empty;
            TicketFirstWeightText = "0";
            TicketSecondWeightText = "0";
            TicketPlatformWeightText = "0";

            // Populate Details section (for viewing only)
            if (details != null)
            {
                // Determine ticket type from ticket number prefix
                // RWB = Receiving Weighbridge, RPL = Receiving Platform
                string ticketTypeKey = "platform"; // default
                if (details.TicketNumber?.StartsWith("RWB") == true)
                {
                    ticketTypeKey = "weighbridge";
                    Console.WriteLine($"[DEBUG] Detected weighbridge from ticket number: {details.TicketNumber}");
                }
                else if (details.TicketNumber?.StartsWith("RPL") == true)
                {
                    ticketTypeKey = "platform";
                    Console.WriteLine($"[DEBUG] Detected platform from ticket number: {details.TicketNumber}");
                }
                
                // Set ticket type for display
                Console.WriteLine($"[DEBUG] Initializing ticket type options");
                InitializeTicketTypeOptions();
                Console.WriteLine($"[DEBUG] Available options: {string.Join(", ", TicketTypeOptions.Select(t => t.Key))}");
                Console.WriteLine($"[DEBUG] Looking for type: {ticketTypeKey}");
                var ticketTypeOption = TicketTypeOptions.FirstOrDefault(t => t.Key == ticketTypeKey);
                Console.WriteLine($"[DEBUG] Found ticketTypeOption: {ticketTypeOption?.Key ?? "null"}");
                
                if (ticketTypeOption != null)
                {
                    Console.WriteLine($"[DEBUG] Setting SelectedTicketTypeOption to {ticketTypeOption.Key}");
                    SelectedTicketTypeOption = ticketTypeOption;
                }
                else
                {
                    Console.WriteLine($"[DEBUG] ticketTypeOption was null! ticketTypeKey={ticketTypeKey}");
                }

                // Load ticket state 
                if (details is TicketDto ticketDto)
                {
                    CurrentTicketState = ticketDto.TicketState;
                    Console.WriteLine($"[DEBUG] Loaded ticket state: {ticketDto.TicketState}");
                    
                    // Load weighbridge-specific fields for display
                    TicketVehicleRegistration = ticketDto.VehicleRegistration ?? string.Empty;
                    TicketTrailerRegistration = ticketDto.TrailerRegistration ?? string.Empty;
                    TicketDriverName = ticketDto.DriverName ?? string.Empty;
                    TicketOfmWeighbridgeTicket = ticketDto.OfmWeighbridgeTicket ?? string.Empty;
                    TicketForeignTicket = ticketDto.ForeignTicket ?? string.Empty;
                    TicketCkNumber = ticketDto.CkNumber ?? string.Empty;
                    TicketNotes = ticketDto.Notes ?? string.Empty;
                    
                    // For weighbridge tickets with state 'H', populate First Weight from initialize_weight_kg (for display only)
                    if (ticketTypeKey == "weighbridge" && ticketDto.TicketState == 'H')
                    {
                        if (ticketDto.InitializeWeightKg.HasValue)
                        {
                            TicketFirstWeightText = ticketDto.InitializeWeightKg.Value.ToString("0.00");
                            Console.WriteLine($"[DEBUG] Loaded weighbridge First Weight from initialize_weight_kg: {ticketDto.InitializeWeightKg}");
                        }
                        else
                        {
                            TicketFirstWeightText = "0";
                        }
                    }
                    // For weighbridge tickets with state 'M', load First Weight from last line's second_weight_kg (for display only)
                    else if (ticketTypeKey == "weighbridge" && ticketDto.TicketState == 'M' && details.Lines != null && details.Lines.Count > 0)
                    {
                        var lastLine = details.Lines.Last();
                        // Try to get second_weight_kg from last line
                        if (lastLine.SecondWeightKg.HasValue)
                        {
                            TicketFirstWeightText = lastLine.SecondWeightKg.Value.ToString("0.00");
                            Console.WriteLine($"[DEBUG] Loaded weighbridge First Weight from last line SecondWeightKg: {lastLine.SecondWeightKg}");
                        }
                        else
                        {
                            TicketFirstWeightText = "0";
                        }
                        // Reset Second Weight to 0 after loading
                        TicketSecondWeightText = "0";
                    }
                }
                else
                {
                    // Default to 'C' if not a TicketDto
                    CurrentTicketState = 'C';
                }

                // NOW load the Details section with the ticket's line items (for viewing)
                if (details is TicketDto ticketDto2 && (ticketDto2.TicketState == 'H' || ticketDto2.TicketState == 'M'))
                {
                    // Incomplete ticket (H or M): User can continue editing - populate Create section
                    IsViewingTicketOnly = false;  // Allow editing
                    TicketNumber = details.TicketNumber ?? string.Empty;
                    
                    // Populate Customer ID in Create section so user can continue adding lines
                    // Get customer full info from the SelectedReceivingTicket which was loaded from search
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
                        Console.WriteLine($"[DEBUG] Populated TicketCustomerIdText from SelectedReceivingTicket: {TicketCustomerIdText}");
                    }
                    
                    // Set LastCreatedTicket so we can continue adding lines to this ticket
                    LastCreatedTicket = ticketDto2;
                    CurrentTicketState = ticketDto2.TicketState;
                    
                    Console.WriteLine($"[DEBUG] Incomplete ticket (state={ticketDto2.TicketState}): Set IsViewingTicketOnly=false, TicketNumber={TicketNumber}, TicketCustomerIdText={TicketCustomerIdText}");
                    
                    // After populating all the fields above, NOW populate ReceivingLines from SelectedReceivingTicketLines
                    // This makes line items appear in the Create section grid
                    // IMPORTANT: SelectedReceivingTicketLines are already loaded, just copy them to ReceivingLines
                    if (SelectedReceivingTicketLines.Count > 0)
                    {
                        Console.WriteLine($"[DEBUG CREATE] Copying {SelectedReceivingTicketLines.Count} lines from SelectedReceivingTicketLines to ReceivingLines");
                        ReceivingLines.Clear();
                        foreach (var lineDto in SelectedReceivingTicketLines)
                        {
                            var lineItem = new ReceivingLineItem
                            {
                                TicketLineId = lineDto.TicketLineId,
                                TicketId = lineDto.TicketId,
                                ProductId = lineDto.ProductId,
                                ProductName = lineDto.ProductName,
                                WeightKg = lineDto.WeightKg,
                                UnitPricePerKg = lineDto.UnitPricePerKg,
                                LineTotal = lineDto.LineTotal,
                                VatAmount = lineDto.VatAmount,
                                TotalInclVat = lineDto.TotalInclVat,
                                Notes = lineDto.Notes ?? string.Empty,
                            };
                            lineItem.Tare = lineDto.Tare;
                            lineItem.PropertyChanged += (s, e) => 
                            {
                                if (e.PropertyName == nameof(ReceivingLineItem.Tare))
                                {
                                    RecalculateReceivingTotals();
                                    _ = SaveTareToApiAsync(SelectedReceivingTicket?.TicketId ?? 0, lineItem.TicketLineId, lineItem.Tare);
                                }
                            };
                            ReceivingLines.Add(lineItem);
                            CreatingTicketTotalWeight += lineDto.WeightKg;
                        }
                        RecalculateReceivingTotals();
                        Console.WriteLine($"[DEBUG CREATE] AFTER copying: ReceivingLines.Count={ReceivingLines.Count}, CreatingTicketTotalWeight={CreatingTicketTotalWeight}");
                        
                        // Calculate FW and Total Net Weight
                        CalculateReceivingWeights();
                    }
                    
                }
                else if (details is TicketDto ticketDto3 && ticketDto3.TicketState == 'C')
                {
                    // Complete ticket (C): creating a new one - populate customer info and generate new ticket number
                    IsViewingTicketOnly = false;
                    Console.WriteLine($"[DEBUG] Complete ticket (state=C): Set IsViewingTicketOnly=false, generating new ticket number");
                    
                    // Populate Customer ID from the selected ticket so user can create a new ticket from it
                    if (SelectedReceivingTicket != null)
                    {
                        string fullCustomerInfo;
                        if (string.IsNullOrWhiteSpace(SelectedReceivingTicket.CompanyName))
                        {
                            // No company, just show customer ID and name
                            fullCustomerInfo = $"{SelectedReceivingTicket.CustomerId} | {SelectedReceivingTicket.FirstName} {SelectedReceivingTicket.LastName}";
                        }
                        else
                        {
                            // Include company and site
                            fullCustomerInfo = $"{SelectedReceivingTicket.CustomerId} | {SelectedReceivingTicket.FirstName} {SelectedReceivingTicket.LastName} | {SelectedReceivingTicket.CompanyName} | {SelectedReceivingTicket.SiteName}";
                        }
                        TicketCustomerIdText = fullCustomerInfo;
                        Console.WriteLine($"[DEBUG] Populated TicketCustomerIdText from completed ticket: {fullCustomerInfo}");
                    }
                    
                    // Generate new ticket number based on detected ticket type
                    string prefix = ticketTypeKey == "weighbridge" ? "RWB" : "RPL";
                    await GenerateTicketNumberForReceivingAsync(prefix);
                    Console.WriteLine($"[DEBUG] Generated new TicketNumber={TicketNumber}");
                }
                else
                {
                    // Fallback
                    IsViewingTicketOnly = true;
                    TicketNumber = details.TicketNumber ?? string.Empty;
                }

                // Load lines from the ticket details (already included in DTO)
                SelectedReceivingTicketLines.Clear();
                ReceivingLines.Clear();
                if (details.Lines != null && details.Lines.Count > 0)
                {
                    Console.WriteLine($"[DEBUG DETAILS] Loading {details.Lines.Count} lines for Details section");
                    foreach (var line in details.Lines)
                    {
                        Console.WriteLine($"[DEBUG DETAILS] Line: TicketLineId={line.TicketLineId}, ProductId={line.ProductId}, ProductName={line.ProductName}, WeightKg={line.WeightKg}, Notes='{line.Notes}', LineTotal={line.LineTotal}, VatAmount={line.VatAmount}, TotalInclVat={line.TotalInclVat}");
                        // Add to SelectedReceivingTicketLines (Details section) for viewing
                        SelectedReceivingTicketLines.Add(line);
                        
                        // For INCOMPLETE tickets (H or M state), ALSO add to ReceivingLines (Create section) so user can edit
                        if (details is TicketDto && (details.TicketState == 'H' || details.TicketState == 'M'))
                        {
                            Console.WriteLine($"[DEBUG DTO] TicketLineDto FROM API: ProductId={line.ProductId}, ProductName={line.ProductName}");
                            Console.WriteLine($"[DEBUG DTO]   Weights from DTO: FirstWeightKg={line.FirstWeightKg}, SecondWeightKg={line.SecondWeightKg}, NetWeightKg={line.WeightKg}");
                            
                            var lineItem = new ReceivingLineItem
                            {
                                TicketLineId = line.TicketLineId,
                                TicketId = line.TicketId,
                                ProductId = line.ProductId,
                                ProductName = line.ProductName,
                                FirstWeightKg = line.FirstWeightKg ?? 0m,
                                SecondWeightKg = line.SecondWeightKg ?? 0m,
                                WeightKg = line.WeightKg,
                                UnitPricePerKg = line.UnitPricePerKg,
                                LineTotal = line.LineTotal,
                                VatAmount = line.VatAmount,
                                TotalInclVat = line.TotalInclVat,
                                Notes = line.Notes ?? string.Empty,
                            };
                            lineItem.Tare = line.Tare;
                            lineItem.PropertyChanged += (s, e) => 
                            {
                                if (e.PropertyName == nameof(ReceivingLineItem.Tare))
                                {
                                    RecalculateReceivingTotals();
                                    _ = SaveTareToApiAsync(line.TicketId, lineItem.TicketLineId, lineItem.Tare);
                                }
                            };
                            
                            Console.WriteLine($"[DEBUG CREATE] Adding line: ProductId={lineItem.ProductId}, WeightKg={lineItem.WeightKg}, FirstWeightKg={lineItem.FirstWeightKg}, SecondWeightKg={lineItem.SecondWeightKg}, Tare={lineItem.Tare}, Notes='{lineItem.Notes}'");
                            ReceivingLines.Add(lineItem);
                            
                        }
                    }
                }
                
                // Recalculate totals for the Create section
                RecalculateReceivingTotals();
                
                // Notify that net weight has changed
                OnPropertyChanged(nameof(CalculatedNetWeightKg));
                
                // Store the loaded ticket as the current working ticket
                LastCreatedTicket = details;
            }
            
            OnPropertyChanged(nameof(SelectedReceivingTicketLinesTotalExVat));
            OnPropertyChanged(nameof(SelectedReceivingTicketLinesTotalVat));
            OnPropertyChanged(nameof(SelectedReceivingTicketLinesTotalInclVat));
            
            // Update the search results table with the new net weight
            UpdateSearchResultsWithNewWeight(details);
            
            Console.WriteLine($"[DEBUG] LoadSelectedReceivingTicketDetailsAsync complete. TicketNumber={TicketNumber}, TicketCustomerIdText={TicketCustomerIdText}, IsViewingTicketOnly={IsViewingTicketOnly}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Exception in LoadSelectedReceivingTicketDetailsAsync: {ex}");
            StatusMessage = $"Error loading ticket details: {ex.Message}";
        }
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
