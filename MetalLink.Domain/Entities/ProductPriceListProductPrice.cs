namespace MetalLink.Domain.Entities;

/// <summary>
/// Represents the price of a product within a specific price list
/// Links products to price lists with their corresponding prices
/// </summary>
public class ProductPriceListProductPrice
{
    public int ProductPriceListProductPriceId { get; set; }
    
    /// <summary>
    /// FK to the product price list
    /// </summary>
    public int ProductPriceListId { get; set; }
    
    /// <summary>
    /// FK to the product
    /// </summary>
    public int ProductId { get; set; }
    
    /// <summary>
    /// The price of this product in this price list
    /// </summary>
    public decimal Price { get; set; }
    
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
    public virtual ProductPriceList? ProductPriceList { get; set; }
    public virtual Product? Product { get; set; }
    public virtual Operator? CreatedByOperator { get; set; }
}
