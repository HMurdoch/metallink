using System;

namespace MetalLink.Domain.Entities;

public class ProductGroup
{
    public int ProductGroupId { get; set; }
    public string ProductGroupName { get; set; } = string.Empty;
    public string ProductGroupDescription { get; set; } = string.Empty;
    public int ProductSpecificationFlagId { get; set; }
    public int CreatedByOperatorId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedTime { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedTime { get; set; } = DateTimeOffset.UtcNow;

    public ProductSpecificationFlag? ProductSpecificationFlag { get; set; }
}
