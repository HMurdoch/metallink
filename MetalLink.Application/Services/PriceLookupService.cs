using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;

namespace MetalLink.Application.Services;

/// <summary>
/// Service for looking up unit prices based on customer/buyer price code and product
/// </summary>
public class PriceLookupService
{
    private readonly IPriceRepository _priceRepository;

    public PriceLookupService(IPriceRepository priceRepository)
    {
        _priceRepository = priceRepository;
    }

    /// <summary>
    /// Gets the unit price per kg for a product based on the customer/buyer's price code
    /// </summary>
    /// <param name="productId">The product ID</param>
    /// <param name="priceCode">The price code (A, B, or C) from customer/buyer</param>
    /// <returns>The unit price per kg, or 0 if not found</returns>
    public async Task<decimal> GetUnitPriceAsync(long productId, string? priceCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(priceCode) || priceCode.Length != 1)
            return 0;

        var prices = await _priceRepository.GetByProductIdAsync(productId, ct);
        if (prices == null || !prices.Any())
            return 0;

        // Get the first price for this product
        var price = prices.First();

        return priceCode.ToUpper() switch
        {
            "A" => price.PriceA,
            "B" => price.PriceB,
            "C" => price.PriceC,
            _ => 0
        };
    }

    /// <summary>
    /// Validates that a price code is valid (A, B, or C)
    /// </summary>
    public static bool IsValidPriceCode(string? priceCode)
    {
        if (string.IsNullOrWhiteSpace(priceCode) || priceCode.Length != 1)
            return false;

        return priceCode.ToUpper() is "A" or "B" or "C";
    }
}
