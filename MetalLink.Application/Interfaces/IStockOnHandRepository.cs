using System.Collections.Generic;
using System.Threading.Tasks;
using MetalLink.Domain.Entities;

namespace MetalLink.Application.Interfaces;

public interface IStockOnHandRepository
{
    Task<StockOnHand?> GetByIdAsync(long stockOnHandId);
    Task<StockOnHand?> GetBySiteAndProductAsync(long siteId, long productId);
    Task<IEnumerable<StockOnHand>> GetAllBySiteAsync(long siteId);
    Task<IEnumerable<StockOnHand>> GetAllByProductAsync(long productId);
    Task<IEnumerable<StockOnHand>> GetAllAsync();
    Task<StockOnHand> AddAsync(StockOnHand stockOnHand);
    Task UpdateAsync(StockOnHand stockOnHand);
    Task RecalculateStockAsync(long siteId, long productId);
    Task RecalculateAllStockAsync();
}
