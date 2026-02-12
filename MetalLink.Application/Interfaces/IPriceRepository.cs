using MetalLink.Domain.Entities;

namespace MetalLink.Application.Interfaces;

public interface IPriceRepository
{
    Task<Price?> GetByIdAsync(long priceId, CancellationToken ct = default);
    Task<IReadOnlyList<Price>> GetByProductIdAsync(long productId, CancellationToken ct = default);
    Task<Price> AddAsync(Price price, CancellationToken ct = default);
    Task UpdateAsync(Price price, CancellationToken ct = default);
    Task DeleteAsync(long priceId, CancellationToken ct = default);
}
