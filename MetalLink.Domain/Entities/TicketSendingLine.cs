using System;

namespace MetalLink.Domain.Entities;

/// <summary>
/// Represents a line item in a sending ticket (for platform tickets with multiple products).
/// </summary>
public class TicketSendingLine
{
    public long TicketSendingLineId { get; private set; }

    public long TicketSendingId { get; private set; }
    public TicketSending TicketSending { get; set; } = null!;

    public long ProductId { get; private set; }
    public Product Product { get; set; } = null!;

    public decimal WeightKg { get; private set; }
    public decimal UnitPricePerKg { get; private set; }
    public decimal LineTotal { get; private set; }

    public string? Notes { get; private set; }

    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedTime { get; private set; } = DateTimeOffset.UtcNow;

    private TicketSendingLine() { }

    public TicketSendingLine(
        long ticketSendingId,
        long productId,
        decimal weightKg,
        decimal unitPricePerKg,
        string? notes = null)
    {
        TicketSendingId = ticketSendingId;
        ProductId = productId;
        WeightKg = weightKg;
        UnitPricePerKg = unitPricePerKg;
        LineTotal = weightKg * unitPricePerKg;
        Notes = notes;
    }

    public void UpdateWeight(decimal weightKg)
    {
        WeightKg = weightKg;
        LineTotal = weightKg * UnitPricePerKg;
    }

    public void UpdatePrice(decimal unitPricePerKg)
    {
        UnitPricePerKg = unitPricePerKg;
        LineTotal = WeightKg * unitPricePerKg;
    }

    public void SoftDelete()
    {
        IsActive = false;
    }
}
