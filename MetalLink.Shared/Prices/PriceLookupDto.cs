namespace MetalLink.Shared.Prices;

public class PriceLookupDto
{
    public int PriceId { get; set; }
    public int ProductId { get; set; }
    public decimal PriceA { get; set; }
    public decimal PriceB { get; set; }
    public decimal PriceC { get; set; }
    public bool IsActive { get; set; }
}
