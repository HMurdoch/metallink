using System;

namespace MetalLink.Domain.Entities;

/// <summary>
/// Represents a line item in a receiving ticket (for platform tickets with multiple products).
/// </summary>
public class TicketReceivingLine
{
    public long TicketReceivingLineId { get; private set; }

    public long TicketReceivingId { get; private set; }
    public TicketReceiving TicketReceiving { get; set; } = null!;

    public long ProductId { get; private set; }
    public Product Product { get; set; } = null!;

    public decimal WeightKg { get; private set; }
    public decimal UnitPricePerKg { get; private set; }
    public decimal LineTotal { get; private set; }

    public string? Notes { get; private set; }

    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedTime { get; private set; } = DateTimeOffset.UtcNow;

    private TicketReceivingLine() { }

    public TicketReceivingLine(
        long ticketReceivingId,
        long productId,
        decimal weightKg,
        decimal unitPricePerKg,
        string? notes = null)
    {
        TicketReceivingId = ticketReceivingId;
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
