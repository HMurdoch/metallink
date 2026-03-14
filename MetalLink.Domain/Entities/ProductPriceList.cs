namespace MetalLink.Domain.Entities;

/// <summary>
/// Represents a product price list (e.g., "Customer Alpha", "Buyer Beta")
/// Replaces the old price_code system (A, B, C) with semantic named price lists
/// </summary>
public class ProductPriceList
{
    public int ProductPriceListId { get; set; }
    
    /// <summary>
    /// Name of the price list (e.g., "Customer Alpha", "Buyer Beta")
    /// </summary>
    public string ProductPriceListName { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional description of the price list
    /// </summary>
    public string? ProductPriceListDescription { get; set; }
    
    /// <summary>
    /// Entity flag: 'C' for Customer, 'B' for Buyer
    /// </summary>
    public char EntityFlag { get; set; }
    
    /// <summary>
    /// FK to operator who created this record
    /// </summary>
    public int CreatedByOperatorId { get; set; }
    
    /// <summary>
    /// Soft delete flag - we never delete records, just set to false
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// When this record was created
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }
    
    /// <summary>
    /// When this record was last updated
    /// </summary>
    public DateTimeOffset UpdatedTime { get; set; }
    
    // Navigation properties
    public virtual Operator? CreatedByOperator { get; set; }
    public virtual ICollection<ProductPriceListProductPrice> ProductPrices { get; set; } = new List<ProductPriceListProductPrice>();
}
