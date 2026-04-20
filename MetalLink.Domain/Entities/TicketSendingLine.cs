using System;

namespace MetalLink.Domain.Entities;

public class TicketSendingLine
{
    public int TicketSendingLineId { get; private set; }

    public int TicketSendingId { get; private set; }
    public TicketSending TicketSending { get; set; } = null!;

    public int ProductId { get; private set; }
    public Product Product { get; set; } = null!;

    public decimal? FirstWeightKg { get; private set; }
    public decimal? SecondWeightKg { get; private set; }

    public decimal NetWeightKg { get; private set; }
    public decimal UnitPricePerKg { get; private set; }

    public decimal Tare { get; private set; } = 0m;

    public string? Notes { get; private set; }

    /// <summary>
    /// Which buyer price list was active when this line was created.
    /// Nullable – populated for new lines; backfilled via migration for existing rows.
    /// </summary>
    public int? ProductPriceListId { get; set; }

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
        decimal tare = 0m,
        string? notes = null,
        decimal? firstWeightKg = null,
        decimal? secondWeightKg = null)
    {
        TicketSendingId = ticketSendingId;
        ProductId = productId;
        FirstWeightKg = firstWeightKg;
        SecondWeightKg = secondWeightKg;
        NetWeightKg = netWeightKg;
        UnitPricePerKg = unitPricePerKg;
        Tare = tare;
        CreatedByOperatorId = createdByOperatorId;
        Notes = notes;
    }

    public void UpdateTare(decimal tare)
    {
        Tare = tare;
        UpdatedTime = DateTimeOffset.UtcNow;
    }

    public void SoftDelete()
    {
        IsActive = false;
        UpdatedTime = DateTimeOffset.UtcNow;
    }
}
