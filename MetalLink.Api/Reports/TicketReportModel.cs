using System;

namespace MetalLink.Api.Reports;

public sealed class TicketReportModel
{
    public long TicketId { get; init; }
    public string TicketNumber { get; init; } = string.Empty;
    public string TicketType { get; init; } = string.Empty;
    public DateTime CreatedTime { get; init; }

    // Site
    public long SiteId { get; init; }
    public string SiteName { get; init; } = "Metal Link Site";

    // Customer
    public long CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerAccountNumber { get; init; }
    public string? CustomerPriceCode { get; init; }

    // Weights
    public decimal? FirstWeightKg { get; init; }
    public decimal? SecondWeightKg { get; init; }
    public decimal? NetWeightKg { get; init; }

    // Pricing
    public decimal UnitPricePerKg { get; init; }
    public decimal TotalAmount { get; init; }
    public string CurrencyCode { get; init; } = "ZAR";

    // Product
    public string? ProductDescription { get; init; }
    public string? Notes { get; init; }
}
