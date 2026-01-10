using System;

namespace MetalLink.Shared.Prices;

public class PriceDto
{
    public long PriceId { get; set; }
    public long ProductId { get; set; }
    public decimal PriceA { get; set; }
    public decimal PriceB { get; set; }
    public decimal PriceC { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
}
