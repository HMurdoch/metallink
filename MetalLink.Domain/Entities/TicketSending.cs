using System;
using System.Collections.Generic;

namespace MetalLink.Domain.Entities;

/// <summary>
/// Represents a sending ticket (selling scrap metal to buyers).
/// </summary>
public class TicketSending
{
    public int TicketSendingId { get; private set; }

    public int BuyerId { get; private set; }
    public Buyer Buyer { get; set; } = null!;

    public int TicketTypeId { get; private set; }
    public TicketType TicketType { get; set; } = null!;

    public int InvoiceNumber { get; private set; }

    public string TicketNumber { get; private set; } = string.Empty;

    public decimal NetWeightKg { get; private set; }

    public string? DriverName { get; private set; }
    public string? VehicleRegistration { get; private set; }
    public string? TrailerRegistration { get; private set; }

    public string? Notes { get; private set; }

    public string? OfmWeighbridgeTicket { get; private set; }
    public string? CkNumber { get; private set; }
    public string? DeliveryNumber { get; private set; }
    public string? ForeignTicket { get; private set; }

    public int CreatedByOperatorId { get; private set; }
    public Operator CreatedByOperator { get; set; } = null!;

    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedTime { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedTime { get; private set; } = DateTimeOffset.UtcNow;

    public ICollection<TicketSendingLine> Lines { get; set; } = new List<TicketSendingLine>();

    private TicketSending() { }

    public TicketSending(
        int buyerId,
        int ticketTypeId,
        string ticketNumber,
        decimal netWeightKg,
        int createdByOperatorId,
        decimal? firstWeightKg = null,
        decimal? secondWeightKg = null,
        string? vehicleRegistration = null,
        string? trailerRegistration = null,
        string? driverName = null,
        string? notes = null)
    {
        BuyerId = buyerId;
        TicketTypeId = ticketTypeId;
        TicketNumber = ticketNumber;
        NetWeightKg = netWeightKg;
        VehicleRegistration = vehicleRegistration;
        TrailerRegistration = trailerRegistration;
        DriverName = driverName;
        Notes = notes;
        CreatedByOperatorId = createdByOperatorId;
    }

    public void UpdateWeights(decimal? firstWeightKg, decimal? secondWeightKg, decimal netWeightKg)
    {
        // FirstWeightKg and SecondWeightKg are stored on individual TicketSendingLine items, not on the ticket itself
        NetWeightKg = netWeightKg;
        UpdatedTime = DateTimeOffset.UtcNow;
    }

    public void AddLine(TicketSendingLine line)
    {
        Lines.Add(line);
        UpdatedTime = DateTimeOffset.UtcNow;
    }

    public void SoftDelete(DateTimeOffset now)
    {
        if (!IsActive) return;
        IsActive = false;
        UpdatedTime = now;
    }
}
