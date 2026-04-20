using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MetalLink.Domain.Entities;

namespace MetalLink.Application.Interfaces;

public interface IProductPriceListRepository
{
    Task<ProductPriceList?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<ProductPriceList>> GetByEntityFlagAsync(char entityFlag, CancellationToken ct = default);
    Task<IEnumerable<ProductPriceList>> GetAllActiveAsync(CancellationToken ct = default);
    Task AddAsync(ProductPriceList priceList, CancellationToken ct = default);
    void Update(ProductPriceList priceList);
}
