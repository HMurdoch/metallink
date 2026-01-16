namespace MetalLink.Shared.Products;

public class ProductLookupDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string? Grade { get; set; }
    public bool IsActive { get; set; }
}
