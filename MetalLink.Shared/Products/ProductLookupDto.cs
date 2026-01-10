namespace MetalLink.Shared.Products;

public class ProductLookupDto
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public decimal? Grade { get; set; }
    public bool IsActive { get; set; }
}
