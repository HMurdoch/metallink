using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MetalLink.Domain.Entities;

namespace MetalLink.Application.Interfaces;

public interface IProductPriceListProductPriceRepository
{
    Task<ProductPriceListProductPrice?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ProductPriceListProductPrice?> GetByProductAndListAsync(int productId, int priceListId, CancellationToken ct = default);
    Task<IEnumerable<ProductPriceListProductPrice>> GetByPriceListIdAsync(int priceListId, CancellationToken ct = default);
    Task AddAsync(ProductPriceListProductPrice price, CancellationToken ct = default);
    void Update(ProductPriceListProductPrice price);
}
