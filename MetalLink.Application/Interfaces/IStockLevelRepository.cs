using System.Threading;
using System.Threading.Tasks;

namespace MetalLink.Application.Interfaces;

public interface IStockLevelRepository
{
    /// <summary>
    /// Returns current stock level (kg) for a product / price-list combination.
    /// If no row exists, creates one with 0 kg.
    /// </summary>
    Task<decimal> GetOrCreateWeightKgAsync(int productId, int? productPriceListProductPriceId, int createdByOperatorId, CancellationToken ct = default);

    /// <summary>
    /// Applies a delta to the stock level for a product / price-list combination.
    /// </summary>
    Task UpdateWeightKgAsync(int productId, int? productPriceListProductPriceId, decimal deltaWeightKg, int createdByOperatorId, CancellationToken ct = default);
}
