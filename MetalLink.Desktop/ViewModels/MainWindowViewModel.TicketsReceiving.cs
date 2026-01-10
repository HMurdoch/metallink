using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MetalLink.Shared.Products;

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
        public decimal UnitPricePerKg { get; init; }
        public decimal LineTotal { get; init; }
        public decimal VatAmount { get; init; }
        public decimal TotalInclVat { get; init; }
    }

    private ObservableCollection<ReceivingLineItem> _receivingLines = new();
    public ObservableCollection<ReceivingLineItem> ReceivingLines
    {
        get => _receivingLines;
        set
        {
            _receivingLines = value;
            OnPropertyChanged();
            RecalculateReceivingTotals();
        }
    }

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
            _ = SearchReceivingProductsAsync(value);
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
                        UnitPricePerKg = dto.UnitPricePerKg,
                        LineTotal = dto.LineTotal,
                        VatAmount = dto.VatAmount,
                        TotalInclVat = dto.TotalInclVat
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
            var lines = new[] { (ReceivingSelectedProduct.ProductId, weightKg) };

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
                    UnitPricePerKg = dto.UnitPricePerKg,
                    LineTotal = dto.LineTotal,
                    VatAmount = dto.VatAmount,
                    TotalInclVat = dto.TotalInclVat
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
        var totalExcl = ReceivingLines.Sum(l => l.LineTotal);
        var totalVat = ReceivingLines.Sum(l => l.VatAmount);
        var totalIncl = ReceivingLines.Sum(l => l.TotalInclVat);

        ReceivingTotalExclVat = totalExcl;
        ReceivingTotalVat = totalVat;
        ReceivingTotalInclVat = totalIncl;
    }

    private async Task RemoveReceivingLineAsync(ReceivingLineItem? line)
    {
        if (line == null) return;
        if (IsBusy) return;

        IsBusy = true;
        StatusMessage = "Removing ticket line...";

        try
        {
            await _ticketService.DeleteTicketLineAsync(line.TicketId, line.TicketLineId);

            ReceivingLines.Remove(line);
            RecalculateReceivingTotals();

            StatusMessage = $"Removed line for product {line.ProductName}.";
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
}
