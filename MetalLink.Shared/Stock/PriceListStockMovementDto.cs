namespace MetalLink.Shared.Stock;

/// <summary>
/// Represents a stock movement for a specific product and price list combination
/// </summary>
public sealed class PriceListStockMovementDto
{
    public int StockMovementId { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int ProductPriceListProductPriceId { get; set; }
    public int ProductPriceListId { get; set; }
    public string PriceListName { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
    public decimal WeightKg { get; set; }
    public DateTime MovementDate { get; set; }
    public int? TicketId { get; set; }
    public string? TicketNumber { get; set; }
    public int? TicketLineId { get; set; }
    public int? LineNumber { get; set; }
    public string? Notes { get; set; }
}