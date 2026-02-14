using System.Threading;
using System.Threading.Tasks;

namespace MetalLink.Application.Interfaces;

public interface IStockLevelRepository
{
    /// <summary>
    /// Returns current stock level (kg) for product. If no row exists, creates one with 0 kg.
    /// </summary>
    Task<decimal> GetOrCreateWeightKgAsync(long productId, int createdByOperatorId, CancellationToken ct = default);

    /// <summary>
    /// Updates stock level (kg) for product (row must exist).
    /// </summary>
    Task UpdateWeightKgAsync(long productId, decimal newWeightKg, CancellationToken ct = default);
}
