using System;

namespace MetalLink.Shared.Prices;

public class ProductPriceListDto
{
    public int ProductPriceListId { get; set; }
    public string ProductPriceListName { get; set; } = string.Empty;
    public string? ProductPriceListDescription { get; set; }
    public string EntityFlag { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
}
