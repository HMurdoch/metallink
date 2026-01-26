using System;
using System.Collections.Generic;

namespace MetalLink.Domain.Entities;

/// <summary>
/// Represents a receiving ticket (buying scrap metal from customers).
/// Stock IN operation.
/// </summary>
public class TicketReceiving
{
    public int TicketReceivingId { get; private set; }

    // Customer (who we're buying from)
    public int CustomerId { get; private set; }
    public Customer Customer { get; set; } = null!;

    // Ticket type
    public int TicketTypeId { get; private set; }
    public TicketType TicketType { get; set; } = null!;

    // Ticket details
    public string TicketNumber { get; private set; } = string.Empty;
    
    // Weights (kg)
    public decimal? FirstWeightKg { get; private set; }
    public decimal? SecondWeightKg { get; private set; }
    public decimal NetWeightKg { get; private set; }

    // Invoice tracking
    public int InvoiceNumber { get; private set; }

    // Vehicle & driver information
    public string? VehicleRegistration { get; private set; }
    public string? TrailerRegistration { get; private set; }
    public string? DriverName { get; private set; }

    // Reference numbers
    public string? OfmWeighbridgeTicket { get; private set; }
    public string? ForeignTicket { get; private set; }
    public string? CkNumber { get; private set; }
    public string? DeliveryNumber { get; private set; }

    // Additional information
    public string? Notes { get; private set; }

    // Lines (for platform tickets with multiple products)
    public ICollection<TicketReceivingLine> Lines { get; set; } = new List<TicketReceivingLine>();

    // Stock movements generated from this ticket
    public ICollection<StockMovementReceiving> StockMovements { get; set; } = new List<StockMovementReceiving>();

    // Audit
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedTime { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedTime { get; private set; } = DateTimeOffset.UtcNow;
    public int CreatedByOperatorId { get; private set; }
    public Operator CreatedByOperator { get; set; } = null!;

    private TicketReceiving() { }

    public TicketReceiving(
        int customerId,
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
        CustomerId = customerId;
        TicketTypeId = ticketTypeId;
        TicketNumber = ticketNumber;
        NetWeightKg = netWeightKg;
        FirstWeightKg = firstWeightKg;
        SecondWeightKg = secondWeightKg;
        VehicleRegistration = vehicleRegistration;
        TrailerRegistration = trailerRegistration;
        DriverName = driverName;
        Notes = notes;
        CreatedByOperatorId = createdByOperatorId;
    }

    public void UpdateWeights(decimal? firstWeightKg, decimal? secondWeightKg, decimal netWeightKg)
    {
        FirstWeightKg = firstWeightKg;
        SecondWeightKg = secondWeightKg;
        NetWeightKg = netWeightKg;
        UpdatedTime = DateTimeOffset.UtcNow;
    }

    public void AddLine(TicketReceivingLine line)
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
