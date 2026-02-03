using System;

namespace MetalLink.Shared.Tickets;

public sealed class TicketLineDto
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

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
}