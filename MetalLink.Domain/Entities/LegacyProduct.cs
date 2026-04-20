using System;

namespace MetalLink.Domain.Entities;

public class LegacyProduct
{
    public int LegacyProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Grade { get; set; }
    public int? ProductGroupId { get; set; }
    public bool MustDeclare { get; set; }

    public ProductGroup? ProductGroup { get; set; }
    public int CreatedByOperatorId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedTime { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedTime { get; set; } = DateTimeOffset.UtcNow;
}
