using System;

namespace MetalLink.Domain.Entities;

/// <summary>
/// Represents current stock on hand for a product at a site.
/// This is a calculated/aggregated view based on stock movements.
/// Can be stored as a materialized view or calculated on-demand.
/// </summary>
public class StockOnHand
{
    public long StockOnHandId { get; private set; }

    // Location
    public long SiteId { get; private set; }
    public Site Site { get; set; } = null!;

    // Product
    public long ProductId { get; private set; }
    public Product Product { get; set; } = null!;

    // Quantities (in kg)
    public decimal QuantityOnHandKg { get; private set; }
    public decimal TotalReceivedKg { get; private set; }  // Lifetime total received
    public decimal TotalSentKg { get; private set; }      // Lifetime total sent

    // Value (weighted average cost)
    public decimal AverageUnitCost { get; private set; }
    public decimal TotalValue { get; private set; }

    // Last movement
    public DateTimeOffset? LastMovementDate { get; private set; }
    public string? LastMovementType { get; private set; }  // "receiving" or "sending"

    // Audit
    public DateTimeOffset CreatedTime { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedTime { get; private set; } = DateTimeOffset.UtcNow;

    private StockOnHand() { }

    public StockOnHand(long siteId, long productId)
    {
        SiteId = siteId;
        ProductId = productId;
        QuantityOnHandKg = 0;
        TotalReceivedKg = 0;
        TotalSentKg = 0;
        AverageUnitCost = 0;
        TotalValue = 0;
    }

    /// <summary>
    /// Add stock from receiving operation (Stock IN)
    /// </summary>
    public void AddStock(decimal quantityKg, decimal unitCost, DateTimeOffset movementDate)
    {
        if (quantityKg <= 0) return;

        // Calculate new weighted average cost
        var currentValue = QuantityOnHandKg * AverageUnitCost;
        var addedValue = quantityKg * unitCost;
        var newQuantity = QuantityOnHandKg + quantityKg;
        
        if (newQuantity > 0)
        {
            AverageUnitCost = (currentValue + addedValue) / newQuantity;
        }

        QuantityOnHandKg = newQuantity;
        TotalReceivedKg += quantityKg;
        TotalValue = QuantityOnHandKg * AverageUnitCost;
        LastMovementDate = movementDate;
        LastMovementType = "receiving";
        UpdatedTime = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Remove stock from sending operation (Stock OUT)
    /// </summary>
    public void RemoveStock(decimal quantityKg, DateTimeOffset movementDate)
    {
        if (quantityKg <= 0) return;

        QuantityOnHandKg -= quantityKg;
        
        // Prevent negative stock (business rule - can be changed)
        if (QuantityOnHandKg < 0)
        {
            QuantityOnHandKg = 0;
        }

        TotalSentKg += quantityKg;
        TotalValue = QuantityOnHandKg * AverageUnitCost;
        LastMovementDate = movementDate;
        LastMovementType = "sending";
        UpdatedTime = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Recalculate stock from scratch based on movements
    /// </summary>
    public void Recalculate(decimal totalReceivedKg, decimal totalSentKg, decimal averageUnitCost, DateTimeOffset? lastMovementDate, string? lastMovementType)
    {
        TotalReceivedKg = totalReceivedKg;
        TotalSentKg = totalSentKg;
        QuantityOnHandKg = totalReceivedKg - totalSentKg;
        
        if (QuantityOnHandKg < 0)
        {
            QuantityOnHandKg = 0;
        }

        AverageUnitCost = averageUnitCost;
        TotalValue = QuantityOnHandKg * AverageUnitCost;
        LastMovementDate = lastMovementDate;
        LastMovementType = lastMovementType;
        UpdatedTime = DateTimeOffset.UtcNow;
    }
}
