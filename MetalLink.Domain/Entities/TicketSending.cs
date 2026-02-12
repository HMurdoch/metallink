using System;
using System.Collections.Generic;

namespace MetalLink.Domain.Entities;

/// <summary>
/// Represents a sending ticket (selling scrap metal to buyers).
/// </summary>
public class TicketSending
{
    public char TicketState { get; private set; } = 'H'; // H=Header (no lines), M=Multi line, C=Complete
    public int TicketSendingId { get; private set; }

    public int BuyerId { get; private set; }
    public Buyer Buyer { get; set; } = null!;

    public int TicketTypeId { get; private set; }
    public TicketType TicketType { get; set; } = null!;

    public int InvoiceNumber { get; private set; }

    public string TicketNumber { get; private set; } = string.Empty;

    public decimal NetWeightKg { get; private set; }

    // Weighbridge header initial weight (used when TicketState == 'H')
    public decimal? InitializeWeightKg { get; private set; }

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
        InitializeWeightKg = firstWeightKg;
        VehicleRegistration = vehicleRegistration;
        TrailerRegistration = trailerRegistration;
        DriverName = driverName;
        Notes = notes;
        CreatedByOperatorId = createdByOperatorId;

        // A newly created ticket has no lines yet.
        TicketState = 'H';
    }

    public void UpdateWeights(decimal? firstWeightKg, decimal? secondWeightKg, decimal netWeightKg)
    {
        // For Sending: treat firstWeightKg as the "header" initialize weight when ticket is in header state
        if (TicketState == 'H' && firstWeightKg.HasValue)
            InitializeWeightKg = firstWeightKg;

        NetWeightKg = netWeightKg;
        UpdatedTime = DateTimeOffset.UtcNow;
    }

    public void AdvanceInitializeWeight(decimal? nextFirstWeightKg)
    {
        if (nextFirstWeightKg.HasValue)
            InitializeWeightKg = nextFirstWeightKg;

        UpdatedTime = DateTimeOffset.UtcNow;
    }

    public void UpdateHeader(
        string? vehicleRegistration,
        string? trailerRegistration,
        string? driverName,
        string? notes)
    {
        VehicleRegistration = vehicleRegistration;
        TrailerRegistration = trailerRegistration;
        DriverName = driverName;
        Notes = notes;
        UpdatedTime = DateTimeOffset.UtcNow;
    }

    public void SetWeighbridgeReferences(
        string? ofmWeighbridgeTicket,
        string? ckNumber,
        string? deliveryNumber,
        string? foreignTicket)
    {
        OfmWeighbridgeTicket = ofmWeighbridgeTicket;
        CkNumber = ckNumber;
        DeliveryNumber = deliveryNumber;
        ForeignTicket = foreignTicket;
        UpdatedTime = DateTimeOffset.UtcNow;
    }

    public void AddLine(TicketSendingLine line)
    {
        Lines.Add(line);
        UpdatedTime = DateTimeOffset.UtcNow;

        // First line added => ticket becomes multi-line / has lines.
        if (TicketState == 'H')
            TicketState = 'M';
    }

    public void MarkComplete(DateTimeOffset now)
    {
        TicketState = 'C';
        UpdatedTime = now;
    }

    public void SoftDelete(DateTimeOffset now)
    {
        if (!IsActive) return;
        IsActive = false;
        UpdatedTime = now;
    }
}
