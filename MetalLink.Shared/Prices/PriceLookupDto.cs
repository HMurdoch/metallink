namespace MetalLink.Shared.Prices;

public class PriceLookupDto
{
    public long PriceId { get; set; }
    public long ProductId { get; set; }
    public decimal PriceA { get; set; }
    public decimal PriceB { get; set; }
    public decimal PriceC { get; set; }
    public bool IsActive { get; set; }
}
