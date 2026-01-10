using MetalLink.Domain.Entities;

namespace MetalLink.Application.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(long productId, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> SearchAsync(string? term, bool? isActive, CancellationToken ct = default);
    Task<Product> AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    Task DeleteAsync(long productId, CancellationToken ct = default);
}
