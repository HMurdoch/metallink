using MetalLink.Domain.Entities;

namespace MetalLink.Application.Interfaces;

public interface IPriceRepository
{
    Task<Price?> GetByIdAsync(int priceId, CancellationToken ct = default);
    Task<IReadOnlyList<Price>> GetByProductIdAsync(int productId, CancellationToken ct = default);
    Task<Price> AddAsync(Price price, CancellationToken ct = default);
    Task UpdateAsync(Price price, CancellationToken ct = default);
    Task DeleteAsync(int priceId, CancellationToken ct = default);
}
