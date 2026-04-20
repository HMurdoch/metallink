using MetalLink.Domain.Entities;

namespace MetalLink.Application.Interfaces;

public interface IPriceRepository
{
    Task<LegacyPrice?> GetByIdAsync(int priceId, CancellationToken ct = default);
    Task<IReadOnlyList<LegacyPrice>> GetByProductIdAsync(int productId, CancellationToken ct = default);
    Task<LegacyPrice> AddAsync(LegacyPrice price, CancellationToken ct = default);
    Task UpdateAsync(LegacyPrice price, CancellationToken ct = default);
    Task DeleteAsync(int priceId, CancellationToken ct = default);
}
