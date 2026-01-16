using System;
using System.Collections.Generic;

namespace MetalLink.Shared.Tickets;

public class TicketSendingDto
{
    public int TicketSendingId { get; set; }
    public int BuyerId { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    
    public int TicketTypeId { get; set; }
    public string TicketTypeName { get; set; } = string.Empty;
    
    public string TicketNumber { get; set; } = string.Empty;
    public int InvoiceNumber { get; set; }
    
    public decimal? FirstWeightKg { get; set; }
    public decimal? SecondWeightKg { get; set; }
    public decimal NetWeightKg { get; set; }
    
    public string? VehicleRegistration { get; set; }
    public string? TrailerRegistration { get; set; }
    public string? DriverName { get; set; }
    
    public string? OfmWeighbridgeTicket { get; set; }
    public string? ForeignTicket { get; set; }
    public string? CkNumber { get; set; }
    public string? DeliveryNumber { get; set; }
    
    public string? Notes { get; set; }
    
    public List<TicketSendingLineDto> Lines { get; set; } = new();
    
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
    public int CreatedByOperatorId { get; set; }
    public string? CreatedByOperatorName { get; set; }
}

public class TicketSendingLineDto
{
    public int TicketSendingLineId { get; set; }
    public int TicketSendingId { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal NetWeightKg { get; set; }
    public decimal UnitPricePerKg { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
}

public class CreateTicketSendingDto
{
    public int BuyerId { get; set; }
    public int TicketTypeId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public int InvoiceNumber { get; set; }
    
    public decimal? FirstWeightKg { get; set; }
    public decimal? SecondWeightKg { get; set; }
    public decimal NetWeightKg { get; set; }
    
    public string? VehicleRegistration { get; set; }
    public string? TrailerRegistration { get; set; }
    public string? DriverName { get; set; }
    
    public string? Notes { get; set; }
    public int CreatedByOperatorId { get; set; }
    
    public List<CreateTicketSendingLineDto> Lines { get; set; } = new();
}

public class CreateTicketSendingLineDto
{
    public int ProductId { get; set; }
    public decimal NetWeightKg { get; set; }
    public decimal UnitPricePerKg { get; set; }
    public string? Notes { get; set; }
}

public class TicketSendingSearchRequestDto
{
    public string? SearchTerm { get; set; }
    public int? BuyerId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? IdNumber { get; set; }
    public long? AccountNumber { get; set; }
    public int? ProductId { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
