using System;
using System.Collections.Generic;

namespace MetalLink.Domain.Entities;

public class ProductSpecificationFlag
{
    public int ProductSpecificationFlagId { get; set; }
    public char ProductSpecificationTypeFlag { get; set; }
    public string ProductSpecificationDescription { get; set; } = string.Empty;
    public int CreatedByOperatorId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedTime { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedTime { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<ProductGroup> ProductGroups { get; set; } = new List<ProductGroup>();
}
