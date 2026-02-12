using System;

namespace MetalLink.Domain.Entities;

/// <summary>
/// Tracks stock movements from sending tickets (Stock OUT - selling to buyers).
/// Auto-generated when TicketSending is created/updated.
/// </summary>
public class StockMovementSending
{
    public long StockMovementSendingId { get; private set; }

    // Site where the movement occurred
    public long SiteId { get; private set; }
    public Site Site { get; set; } = null!;

    // Product being moved
    public long ProductId { get; private set; }
    public Product Product { get; set; } = null!;

    // Link to the sending ticket that caused this movement
    public long TicketSendingId { get; private set; }
    public TicketSending TicketSending { get; set; } = null!;

    // Link to the specific ticket line (if applicable - for platform tickets)
    public long? TicketSendingLineId { get; private set; }
    public TicketSendingLine? TicketSendingLine { get; set; }

    // Movement details
    public decimal QuantityKg { get; private set; }  // Always positive (will be subtracted from stock)
    public decimal UnitPricePerKg { get; private set; }
    public decimal TotalValue { get; private set; }
    public string CurrencyCode { get; private set; } = "ZAR";

    // Reference information
    public string TicketNumber { get; private set; } = string.Empty;
    public string? Notes { get; private set; }

    // Buyer reference (denormalized for reporting)
    public long BuyerId { get; private set; }
    public string BuyerName { get; private set; } = string.Empty;

    // Audit
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset MovementDate { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedTime { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedTime { get; private set; } = DateTimeOffset.UtcNow;

    private StockMovementSending() { }

    public StockMovementSending(
        long siteId,
        long productId,
        long ticketSendingId,
        long? ticketSendingLineId,
        decimal quantityKg,
        decimal unitPricePerKg,
        string currencyCode,
        string ticketNumber,
        long buyerId,
        string buyerName,
        string? notes = null)
    {
        SiteId = siteId;
        ProductId = productId;
        TicketSendingId = ticketSendingId;
        TicketSendingLineId = ticketSendingLineId;
        QuantityKg = Math.Abs(quantityKg); // Always positive (represents outbound quantity)
        UnitPricePerKg = unitPricePerKg;
        TotalValue = QuantityKg * unitPricePerKg;
        CurrencyCode = currencyCode;
        TicketNumber = ticketNumber;
        BuyerId = buyerId;
        BuyerName = buyerName;
        Notes = notes;
    }

    public void SoftDelete(DateTimeOffset now)
    {
        if (!IsActive) return;
        
        IsActive = false;
        UpdatedTime = now;
    }
}
