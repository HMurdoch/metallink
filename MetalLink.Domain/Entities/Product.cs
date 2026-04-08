using System;

namespace MetalLink.Domain.Entities;

public class Product
{
    public int ProductId { get; set; }
    public string? HtsCode { get; set; }
    public string IsriProductCode { get; set; } = string.Empty;
    public string IsriProductName { get; set; } = string.Empty;
    public string? IsriProductDescription { get; set; }
    public string? QKey { get; set; }
    public string? IsriProductUrl { get; set; }
    public bool IsriProduct { get; set; }
    public int ProductGroupId { get; set; }
    public int ProductSpecificationFlagId { get; set; }
    public bool StarredProduct { get; set; }
    public string? StarredProductAlias { get; set; }
    public bool MustDeclare { get; set; }
    public int CreatedByOperatorId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedTime { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedTime { get; set; } = DateTimeOffset.UtcNow;

    public ProductGroup? ProductGroup { get; set; }
    public ProductSpecificationFlag? ProductSpecificationFlag { get; set; }
}
