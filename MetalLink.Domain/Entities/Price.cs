using System;

namespace MetalLink.Domain.Entities;

public class Price
{
    public int PriceId { get; set; }
    public int ProductId { get; set; }
    public decimal PriceA { get; set; }
    public decimal PriceB { get; set; }
    public decimal PriceC { get; set; }
    public int CreatedByOperatorId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedTime { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedTime { get; set; } = DateTimeOffset.UtcNow;

    public Product? Product { get; set; }
}
