using System;

namespace MetalLink.Domain.Entities;

public class TicketSendingLine
{
    public int TicketSendingLineId { get; private set; }

    public int TicketSendingId { get; private set; }
    public TicketSending TicketSending { get; set; } = null!;

    public int ProductId { get; private set; }
    public Product Product { get; set; } = null!;

    public decimal NetWeightKg { get; private set; }
    public decimal UnitPricePerKg { get; private set; }

    public string? Notes { get; private set; }

    public int CreatedByOperatorId { get; private set; }
    public Operator CreatedByOperator { get; set; } = null!;

    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedTime { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedTime { get; private set; } = DateTimeOffset.UtcNow;

    private TicketSendingLine() { }

    public TicketSendingLine(
        int ticketSendingId,
        int productId,
        decimal netWeightKg,
        decimal unitPricePerKg,
        int createdByOperatorId,
        string? notes = null)
    {
        TicketSendingId = ticketSendingId;
        ProductId = productId;
        NetWeightKg = netWeightKg;
        UnitPricePerKg = unitPricePerKg;
        CreatedByOperatorId = createdByOperatorId;
        Notes = notes;
    }

    public void SoftDelete()
    {
        IsActive = false;
        UpdatedTime = DateTimeOffset.UtcNow;
    }
}
