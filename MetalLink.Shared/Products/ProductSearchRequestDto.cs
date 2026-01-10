namespace MetalLink.Shared.Products;

public class ProductSearchRequestDto
{
    public string? Term { get; set; }
    public bool? IsActive { get; set; } = true;
}
