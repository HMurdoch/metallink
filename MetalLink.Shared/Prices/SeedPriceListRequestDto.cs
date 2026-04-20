namespace MetalLink.Shared.Prices;

/// <summary>
/// Request body for POST api/product-price-lists/{id}/seed
/// </summary>
public class SeedPriceListRequestDto
{
    /// <summary>
    /// If true, seed from last known transaction price per product.
    /// If false, <see cref="CloneFromPriceListId"/> must be set.
    /// </summary>
    public bool UseLastKnownPrice { get; set; }

    /// <summary>
    /// When <see cref="UseLastKnownPrice"/> is false, copy prices from this price list.
    /// </summary>
    public int? CloneFromPriceListId { get; set; }
}
