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

    // Weights captured at the time the line item is created
    public decimal? FirstWeightKg { get; private set; }
    public decimal? SecondWeightKg { get; private set; }
    public decimal NetWeightKg { get; private set; }
    public decimal UnitPricePerKg { get; private set; }

    // Tare weight (material to be deducted from first weight, e.g., packaging)
    public decimal Tare { get; private set; }

    public string? Notes { get; private set; }

    /// <summary>
    /// Which customer price list was active when this line was created.
    /// Nullable – populated for new lines; backfilled via migration for existing rows.
    /// </summary>
    public int? ProductPriceListId { get; set; }

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
        string? notes = null,
        decimal? firstWeightKg = null,
        decimal? secondWeightKg = null,
        decimal tare = 0m)
    {
        ReceivingTicketId = receivingTicketId;
        ProductId = productId;
        FirstWeightKg = firstWeightKg;
        SecondWeightKg = secondWeightKg;
        NetWeightKg = netWeightKg;
        UnitPricePerKg = unitPricePerKg;
        CreatedByOperatorId = createdByOperatorId;
        Notes = notes;
        Tare = tare;
    }

    public void UpdateWeight(decimal netWeightKg)
    {
        NetWeightKg = netWeightKg;
        UpdatedTime = DateTimeOffset.UtcNow;
    }

    public void UpdateTare(decimal tare)
    {
        Tare = tare;
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
