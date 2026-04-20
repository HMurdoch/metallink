using System;

namespace MetalLink.Shared.Stock;

public class StockOnHandDto
{
    public int StockOnHandId { get; set; }
    public int SiteId { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal? Grade { get; set; }
    
    public decimal QuantityOnHandKg { get; set; }
    public decimal TotalReceivedKg { get; set; }
    public decimal TotalSentKg { get; set; }
    
    public decimal AverageUnitCost { get; set; }
    public decimal TotalValue { get; set; }
    
    public DateTimeOffset? LastMovementDate { get; set; }
    public string? LastMovementType { get; set; }
    
    public DateTimeOffset UpdatedTime { get; set; }
}

public class StockMovementDto
{
    public int MovementId { get; set; }
    public string MovementType { get; set; } = string.Empty; // "receiving" or "sending"
    public DateTimeOffset MovementDate { get; set; }
    
    public int SiteId { get; set; }
    public string SiteName { get; set; } = string.Empty;
    
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    
    public decimal QuantityKg { get; set; }
    public decimal UnitPricePerKg { get; set; }
    public decimal TotalValue { get; set; }
    public string CurrencyCode { get; set; } = "ZAR";
    
    public string TicketNumber { get; set; } = string.Empty;
    public string CounterpartyName { get; set; } = string.Empty; // Customer or Buyer
    public string CounterpartyType { get; set; } = string.Empty; // "customer" or "buyer"
    
    public string? Notes { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
}

public class StockMovementSearchRequestDto
{
    public int? SiteId { get; set; }
    public int? ProductId { get; set; }
    public string? MovementType { get; set; } // "receiving" or "sending"
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 100;
}
