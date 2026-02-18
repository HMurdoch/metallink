using System;
using System.Collections.Generic;

namespace MetalLink.Shared.Tickets.Sending;

/// <summary>
/// Sending-only UI DTO used by Desktop for state tracking/details binding.
/// This must not be shared with Receiving.
/// </summary>
public sealed class SendingTicketDto
{
    public long TicketId { get; set; }
    public long BuyerId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string TicketType { get; set; } = string.Empty;
    public int TicketTypeId { get; set; }
    public char TicketState { get; set; }
    public decimal NetWeightKg { get; set; }
    public decimal? InitializeWeightKg { get; set; }

    public string? VehicleRegistration { get; set; }
    public string? TrailerRegistration { get; set; }
    public string? DriverName { get; set; }
    public string? OfmWeighbridgeTicket { get; set; }
    public string? ForeignTicket { get; set; }
    public string? CkNumber { get; set; }
    public string? DeliveryNumber { get; set; }
    public string? Notes { get; set; }

    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }

    public ICollection<SendingTicketLineDto> Lines { get; set; } = new List<SendingTicketLineDto>();
}

public sealed class SendingTicketLineDto
{
    public long TicketLineId { get; set; }
    public long TicketId { get; set; }
    public long ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal? FirstWeightKg { get; set; }
    public decimal? SecondWeightKg { get; set; }
    public decimal WeightKg { get; set; }

    public decimal UnitPricePerKg { get; set; }
    public decimal LineTotal { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalInclVat { get; set; }
    public decimal Tare { get; set; }

    public string? Notes { get; set; }
}
