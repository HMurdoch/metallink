using System;

namespace MetalLink.Domain.Entities;

public class Price
{
    public long PriceId { get; set; }
    public long ProductId { get; set; }
    public decimal PriceA { get; set; }
    public decimal PriceB { get; set; }
    public decimal PriceC { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedTime { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedTime { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Product? Product { get; set; }
}
