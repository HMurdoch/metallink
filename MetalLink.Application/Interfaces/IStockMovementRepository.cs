using System.Threading;
using System.Threading.Tasks;

namespace MetalLink.Application.Interfaces;

public interface IStockMovementRepository
{
    Task AddAsync(
        int productId,
        decimal baseWeightKg,
        decimal buyWeightKg,
        decimal sellWeightKg,
        int createdByOperatorId,
        string notes,
        decimal unitPricePerKg = 0m,
        int? productPriceListId = null,
        CancellationToken ct = default);
}
