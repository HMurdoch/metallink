namespace MetalLink.Shared.Stock;

/// <summary>
/// Represents stock level for a product broken down by price list
/// </summary>
public sealed class PriceListStockLevelDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    
    public int ProductPriceListProductPriceId { get; set; }
    public int ProductPriceListId { get; set; }
    public string PriceListName { get; set; } = string.Empty;
    
    public decimal WeightKg { get; set; }
    /// <summary>
    /// Total weight across all price lists for this product
    /// </summary>
    public decimal TotalWeightKg { get; set; }
}
