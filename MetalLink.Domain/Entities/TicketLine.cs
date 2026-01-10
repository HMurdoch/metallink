using System;

namespace MetalLink.Domain.Entities;

public class TicketLine
{
    public long TicketLineId { get; set; }

    public long TicketId { get; set; }
    public long ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal WeightKg { get; set; }
    public decimal UnitPricePerKg { get; set; }   // ex-VAT per kg
    public decimal LineTotal { get; set; }        // ex-VAT line total
    public decimal VatAmount { get; set; }
    public decimal TotalInclVat { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedTime { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedTime { get; set; } = DateTimeOffset.UtcNow;

    public Ticket Ticket { get; set; } = null!;
    public Product Product { get; set; } = null!;
}