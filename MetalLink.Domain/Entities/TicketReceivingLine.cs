using System;

namespace MetalLink.Domain.Entities;

/// <summary>
/// Represents a line item in a receiving ticket (for platform tickets with multiple products).
/// </summary>
public class TicketReceivingLine
{
    public int ReceivingTicketLineId { get; private set; }

    public int ReceivingTicketId { get; private set; }
    public TicketReceiving TicketReceiving { get; set; } = null!;

    public int ProductId { get; private set; }
    public Product Product { get; set; } = null!;

    public decimal NetWeightKg { get; private set; }
    public decimal UnitPricePerKg { get; private set; }

    public string? Notes { get; private set; }

    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedTime { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedTime { get; private set; } = DateTimeOffset.UtcNow;
    public int CreatedByOperatorId { get; private set; }
    public Operator CreatedByOperator { get; set; } = null!;

    private TicketReceivingLine() { }

    public TicketReceivingLine(
        int receivingTicketId,
        int productId,
        decimal netWeightKg,
        decimal unitPricePerKg,
        int createdByOperatorId,
        string? notes = null)
    {
        ReceivingTicketId = receivingTicketId;
        ProductId = productId;
        NetWeightKg = netWeightKg;
        UnitPricePerKg = unitPricePerKg;
        CreatedByOperatorId = createdByOperatorId;
        Notes = notes;
    }

    public void UpdateWeight(decimal netWeightKg)
    {
        NetWeightKg = netWeightKg;
        UpdatedTime = DateTimeOffset.UtcNow;
    }

    public void UpdatePrice(decimal unitPricePerKg)
    {
        UnitPricePerKg = unitPricePerKg;
        UpdatedTime = DateTimeOffset.UtcNow;
    }

    public void SoftDelete()
    {
        IsActive = false;
        UpdatedTime = DateTimeOffset.UtcNow;
    }
}
