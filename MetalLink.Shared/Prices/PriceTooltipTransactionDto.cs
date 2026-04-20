using System;

namespace MetalLink.Shared.Prices;

public class PriceTooltipTransactionDto
{
    public bool IsBuy { get; set; }
    public DateTimeOffset Date { get; set; }
    public decimal QuantityKg { get; set; }
}
