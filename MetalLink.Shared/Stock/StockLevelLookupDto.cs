namespace MetalLink.Shared.Stock;

public sealed class StockLevelLookupDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal WeightKg { get; set; }
}
