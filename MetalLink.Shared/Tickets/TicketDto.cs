namespace MetalLink.Shared.Tickets;

public sealed class TicketDto
{
    public long TicketId { get; set; }
    public long SiteId { get; set; }
    public long CustomerId { get; set; }
    public long OperatorId { get; set; }

    public string TicketNumber { get; set; } = string.Empty;
    public string TicketType { get; set; } = string.Empty;

    public decimal? FirstWeightKg { get; set; }
    public decimal? SecondWeightKg { get; set; }
    public decimal NetWeightKg { get; set; }

    public decimal UnitPricePerKg { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; } = "ZAR";

    public string? ProductDescription { get; set; }
    public string? Notes { get; set; }

    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
}
