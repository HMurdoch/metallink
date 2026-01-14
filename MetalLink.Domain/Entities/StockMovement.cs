using System;

namespace MetalLink.Domain.Entities;

/// <summary>
/// Tracks all stock movements (in/out) for inventory management.
/// Auto-generated from tickets (receiving = +IN, sending = -OUT).
/// </summary>
public class StockMovement
{
    public long StockMovementId { get; private set; }

    // Site where the movement occurred
    public long SiteId { get; private set; }
    public Site Site { get; set; } = null!;

    // Product being moved
    public long ProductId { get; private set; }
    public Product Product { get; set; } = null!;

    // Link to the ticket that caused this movement
    public long TicketId { get; private set; }
    public Ticket Ticket { get; set; } = null!;

    // Link to the specific ticket line (if applicable)
    public long? TicketLineId { get; private set; }
    public TicketLine? TicketLine { get; set; }

    // Movement details
    public string MovementType { get; private set; } = string.Empty;  // "receiving" or "delivery"
    public decimal QuantityKg { get; private set; }  // +ve for IN, -ve for OUT
    public decimal UnitPricePerKg { get; private set; }
    public string CurrencyCode { get; private set; } = "ZAR";

    // Reference information
    public string? ReferenceNumber { get; private set; }  // Ticket number
    public string? Notes { get; private set; }

    // Customer or Buyer reference (denormalized for reporting)
    public string? CounterpartyName { get; private set; }  // Customer name (receiving) or Buyer name (sending)
    public string? CounterpartyType { get; private set; }  // "customer" or "buyer"

    // Audit
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedTime { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedTime { get; private set; } = DateTimeOffset.UtcNow;

    private StockMovement() { }

    public StockMovement(
        long siteId,
        long productId,
        long ticketId,
        long? ticketLineId,
        string movementType,
        decimal quantityKg,
        decimal unitPricePerKg,
        string currencyCode,
        string? referenceNumber,
        string? counterpartyName,
        string? counterpartyType,
        string? notes = null)
    {
        SiteId = siteId;
        ProductId = productId;
        TicketId = ticketId;
        TicketLineId = ticketLineId;
        MovementType = movementType;
        
        // For receiving: positive quantity (stock IN)
        // For delivery: negative quantity (stock OUT)
        QuantityKg = movementType == "receiving" ? Math.Abs(quantityKg) : -Math.Abs(quantityKg);
        
        UnitPricePerKg = unitPricePerKg;
        CurrencyCode = currencyCode;
        ReferenceNumber = referenceNumber;
        CounterpartyName = counterpartyName;
        CounterpartyType = counterpartyType;
        Notes = notes;
    }

    public void SoftDelete(DateTimeOffset now)
    {
        if (!IsActive) return;
        
        IsActive = false;
        UpdatedTime = now;
    }
}
