using System.Collections.Generic;

namespace MetalLink.Shared.Prices;

public class ProductPriceTooltipDto
{
    public decimal StockOnHandKg { get; set; }
    public List<PriceTooltipTransactionDto> LastTransactions { get; set; } = new();
}
