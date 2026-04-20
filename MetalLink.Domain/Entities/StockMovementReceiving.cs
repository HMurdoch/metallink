using System;

namespace MetalLink.Domain.Entities;

/// <summary>
/// Tracks stock movements from receiving tickets (Stock IN - buying from customers).
/// Auto-generated when TicketReceiving is created/updated.
/// </summary>
public class StockMovementReceiving
{
    public int StockMovementReceivingId { get; private set; }

    // Site where the movement occurred
    public int SiteId { get; private set; }
    public Site Site { get; set; } = null!;

    // Product being moved
    public int ProductId { get; private set; }
    public Product Product { get; set; } = null!;

    // Link to the receiving ticket that caused this movement
    public int TicketReceivingId { get; private set; }
    public TicketReceiving TicketReceiving { get; set; } = null!;

    // Link to the specific ticket line (if applicable - for platform tickets)
    public int? TicketReceivingLineId { get; private set; }
    public TicketReceivingLine? TicketReceivingLine { get; set; }

    // Movement details
    public decimal QuantityKg { get; private set; }  // Always positive for receiving (Stock IN)
    public decimal UnitPricePerKg { get; private set; }
    public decimal TotalValue { get; private set; }
    public string CurrencyCode { get; private set; } = "ZAR";

    // Reference information
    public string TicketNumber { get; private set; } = string.Empty;
    public string? Notes { get; private set; }

    // Customer reference (denormalized for reporting)
    public int CustomerId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;

    // Price list that was active when this movement was recorded
    public int? ProductPriceListId { get; set; }
    public ProductPriceList? ProductPriceList { get; set; }

    // Audit
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset MovementDate { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedTime { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedTime { get; private set; } = DateTimeOffset.UtcNow;

    private StockMovementReceiving() { }

    public StockMovementReceiving(
        int siteId,
        int productId,
        int ticketReceivingId,
        int? ticketReceivingLineId,
        decimal quantityKg,
        decimal unitPricePerKg,
        string currencyCode,
        string ticketNumber,
        int customerId,
        string customerName,
        string? notes = null)
    {
        SiteId = siteId;
        ProductId = productId;
        TicketReceivingId = ticketReceivingId;
        TicketReceivingLineId = ticketReceivingLineId;
        QuantityKg = Math.Abs(quantityKg); // Always positive for stock IN
        UnitPricePerKg = unitPricePerKg;
        TotalValue = QuantityKg * unitPricePerKg;
        CurrencyCode = currencyCode;
        TicketNumber = ticketNumber;
        CustomerId = customerId;
        CustomerName = customerName;
        Notes = notes;
    }

    public void SoftDelete(DateTimeOffset now)
    {
        if (!IsActive) return;
        
        IsActive = false;
        UpdatedTime = now;
    }
}
