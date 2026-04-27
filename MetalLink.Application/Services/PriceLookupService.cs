using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;

namespace MetalLink.Application.Services;

/// <summary>
/// Service for looking up unit prices based on customer/buyer price code and product
/// </summary>
public class PriceLookupService
{
    private readonly IPriceRepository _priceRepository;
    private readonly IProductPriceListProductPriceRepository _priceListPriceRepository;

    public PriceLookupService(
        IPriceRepository priceRepository,
        IProductPriceListProductPriceRepository priceListPriceRepository)
    {
        _priceRepository = priceRepository;
        _priceListPriceRepository = priceListPriceRepository;
    }

    /// <summary>
    /// Gets the unit price per kg for a product based on the customer/buyer's price list or legacy price code
    /// </summary>
    /// <param name="productId">The product ID</param>
    /// <param name="priceListId">The product price list ID (new system)</param>
    /// <param name="priceCode">The legacy price code (A, B, or C)</param>
    /// <returns>The unit price per kg, or 0 if not found</returns>
    public async Task<decimal> GetUnitPriceAsync(
        int productId,
        int? priceListId,
        string? priceCode,
        CancellationToken ct = default)
    {
        // 1. Try the new semantic Price List system first
        if (priceListId.HasValue)
        {
            var priceListPrice = await _priceListPriceRepository.GetByProductAndListAsync(productId, priceListId.Value, ct);
            if (priceListPrice != null)
            {
                return priceListPrice.Price;
            }
        }

        // 2. Legacy Price Code system is no longer supported
        return 0;
    }

    public async Task<int?> GetProductPriceListProductPriceIdAsync(
        int productId,
        int priceListId,
        CancellationToken ct = default)
    {
        var priceListPrice = await _priceListPriceRepository.GetByProductAndListAsync(productId, priceListId, ct);
        return priceListPrice?.ProductPriceListProductPriceId;
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
