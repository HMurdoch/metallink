using System;

namespace MetalLink.Domain.Entities;

public class StockMovement
{
    public int StockMovementId { get; private set; }

    public int SiteId { get; private set; }
    public Site Site { get; set; } = null!;

    public int ProductId { get; private set; }
    public Product Product { get; set; } = null!;

    // Optional references (no FK enforced in DB after legacy tables dropped)
    public int TicketId { get; private set; }
    public int? TicketLineId { get; private set; }

    public string MovementType { get; private set; } = string.Empty;
    public decimal QuantityKg { get; private set; }
    public decimal UnitPricePerKg { get; private set; }
    public string CurrencyCode { get; private set; } = "ZAR";

    public string? ReferenceNumber { get; private set; }
    public string? CounterpartyName { get; private set; }
    public string? CounterpartyType { get; private set; }
    public string? Notes { get; private set; }

    public int CreatedByOperatorId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedTime { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedTime { get; private set; } = DateTimeOffset.UtcNow;

    private StockMovement() { }
}
