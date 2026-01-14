using System;
using System.Collections.Generic;

namespace MetalLink.Shared.Tickets;

public class TicketReceivingDto
{
    public long TicketReceivingId { get; set; }
    public long CompanyId { get; set; }
    public long SiteId { get; set; }
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    
    public string TicketNumber { get; set; } = string.Empty;
    public string TicketType { get; set; } = "weighbridge";
    
    public decimal? FirstWeightKg { get; set; }
    public decimal? SecondWeightKg { get; set; }
    public decimal NetWeightKg { get; set; }
    
    public decimal UnitPricePerKg { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; } = "ZAR";
    
    public long? ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public string? ProductDescription { get; set; }
    
    public string? VehicleRegistration { get; set; }
    public string? TrailerRegistration { get; set; }
    public string? DriverName { get; set; }
    
    public string? OfmWeighbridgeTicket { get; set; }
    public string? ForeignTicket { get; set; }
    public string? CkNumber { get; set; }
    public string? DeliveryNumber { get; set; }
    
    public string? RfidTag { get; set; }
    public DateTimeOffset? RfidFirstScan { get; set; }
    public DateTimeOffset? RfidSecondScan { get; set; }
    
    public string DeliveryStatus { get; set; } = "pending";
    public string? Notes { get; set; }
    
    public string? PlatePhotoUrl { get; set; }
    public string? LoadPhotoUrl { get; set; }
    
    public List<TicketReceivingLineDto> Lines { get; set; } = new();
    
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
    public long CreatedByOperatorId { get; set; }
    public string? CreatedByOperatorName { get; set; }
}

public class TicketReceivingLineDto
{
    public long TicketReceivingLineId { get; set; }
    public long TicketReceivingId { get; set; }
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal WeightKg { get; set; }
    public decimal UnitPricePerKg { get; set; }
    public decimal LineTotal { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
}

public class CreateTicketReceivingDto
{
    public long CompanyId { get; set; }
    public long SiteId { get; set; }
    public long CustomerId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string TicketType { get; set; } = "weighbridge";
    
    public decimal? FirstWeightKg { get; set; }
    public decimal? SecondWeightKg { get; set; }
    public decimal NetWeightKg { get; set; }
    
    public decimal UnitPricePerKg { get; set; }
    public string CurrencyCode { get; set; } = "ZAR";
    
    public long? ProductId { get; set; }
    public string? ProductDescription { get; set; }
    
    public string? VehicleRegistration { get; set; }
    public string? TrailerRegistration { get; set; }
    public string? DriverName { get; set; }
    
    public string? Notes { get; set; }
    public long CreatedByOperatorId { get; set; }
}

public class TicketReceivingSearchRequestDto
{
    public string? SearchTerm { get; set; }
    public long? CompanyId { get; set; }
    public long? SiteId { get; set; }
    public long? CustomerId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? IdNumber { get; set; }
    public long? AccountNumber { get; set; }
    public long? ProductId { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public string? DeliveryStatus { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
