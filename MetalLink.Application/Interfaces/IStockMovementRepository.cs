using System.Threading;
using System.Threading.Tasks;

namespace MetalLink.Application.Interfaces;

public interface IStockMovementRepository
{
    Task AddAsync(
        long productId,
        decimal baseWeightKg,
        decimal buyWeightKg,
        decimal sellWeightKg,
        int createdByOperatorId,
        string notes,
        CancellationToken ct = default);
}
