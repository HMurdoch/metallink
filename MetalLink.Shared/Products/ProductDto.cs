using System;

namespace MetalLink.Shared.Products;

public class ProductDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Grade { get; set; }
    public bool MustDeclare { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
}
