namespace MetalLink.Shared.Products;

public class ProductLookupDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string? QKey { get; set; }
    public string? HtsCode { get; set; }
    public bool IsriProduct { get; set; }
    public string? ProductGroupName { get; set; }
    public int ProductSpecificationFlagId { get; set; }
    public string? ProductSpecificationDescription { get; set; }
    public string? StarredProductAlias { get; set; }
    public bool StarredProduct { get; set; }
    public string? IsriProductDescription { get; set; }
    public string? IsriProductUrl { get; set; }
    public bool IsActive { get; set; }
}
