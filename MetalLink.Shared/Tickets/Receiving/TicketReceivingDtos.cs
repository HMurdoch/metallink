using System;
using System.Collections.Generic;

namespace MetalLink.Shared.Tickets.Receiving;

public class TicketReceivingDto
{
    public int TicketReceivingId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    
    public int TicketTypeId { get; set; }
    public string TicketTypeName { get; set; } = string.Empty;
    
    public string TicketNumber { get; set; } = string.Empty;
    public int? InvoiceNumber { get; set; }
    
    public decimal NetWeightKg { get; set; }
    
    public char TicketState { get; set; } = 'C';
    public decimal? InitializeWeightKg { get; set; }
    
    public string? VehicleRegistration { get; set; }
    public string? TrailerRegistration { get; set; }
    public string? DriverName { get; set; }
    
    public string? OfmWeighbridgeTicket { get; set; }
    public string? ForeignTicket { get; set; }
    public string? CkNumber { get; set; }
    public string? DeliveryNumber { get; set; }
    
    public string? Notes { get; set; }
    
    public List<TicketReceivingLineDto> Lines { get; set; } = new();
    
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
    public int CreatedByOperatorId { get; set; }
    public string? CreatedByOperatorName { get; set; }
}

public class TicketReceivingLineDto
{
    public int ReceivingTicketLineId { get; set; }
    public int ReceivingTicketId { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? ProductGroupName { get; set; }
    public decimal? FirstWeightKg { get; set; }
    public decimal? SecondWeightKg { get; set; }
    public decimal NetWeightKg { get; set; }
    public decimal UnitPricePerKg { get; set; }
    public decimal LineTotal { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalInclVat { get; set; }
    public decimal Tare { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
}

public class CreateTicketReceivingDto
{
    public int CustomerId { get; set; }
    public int TicketTypeId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public int? InvoiceNumber { get; set; }
    
    public decimal NetWeightKg { get; set; }
    
    // Ticket state: 'H' = Header only, 'M' = Multi-weight, 'C' = Complete
    public char TicketState { get; set; } = 'C';
    
    // For weighbridge tickets: the initial weight when header is created
    public decimal? InitializeWeightKg { get; set; }
    
    public string? VehicleRegistration { get; set; }
    public string? TrailerRegistration { get; set; }
    public string? DriverName { get; set; }
    public string? OfmWeighbridgeTicket { get; set; }
    public string? CkNumber { get; set; }
    public string? DeliveryNumber { get; set; }
    public string? ForeignTicket { get; set; }
    
    public string? Notes { get; set; }
    public int CreatedByOperatorId { get; set; }
    
    public List<CreateTicketReceivingLineDto> Lines { get; set; } = new();
}

public class CreateTicketReceivingLineDto
{
    public int ProductId { get; set; }
    public decimal? FirstWeightKg { get; set; }
    public decimal? SecondWeightKg { get; set; }
    public decimal NetWeightKg { get; set; }
    public decimal UnitPricePerKg { get; set; }
    public decimal Tare { get; set; }
    public string? Notes { get; set; }
}

