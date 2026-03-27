using System.Collections.Generic;
using System.Threading.Tasks;
using MetalLink.Domain.Entities;

namespace MetalLink.Application.Interfaces;

public interface IStockOnHandRepository
{
    Task<StockOnHand?> GetByIdAsync(int stockOnHandId);
    Task<StockOnHand?> GetBySiteAndProductAsync(int siteId, int productId);
    Task<IEnumerable<StockOnHand>> GetAllBySiteAsync(int siteId);
    Task<IEnumerable<StockOnHand>> GetAllByProductAsync(int productId);
    Task<IEnumerable<StockOnHand>> GetAllAsync();
    Task<StockOnHand> AddAsync(StockOnHand stockOnHand);
    Task UpdateAsync(StockOnHand stockOnHand);
    Task RecalculateStockAsync(int siteId, int productId);
    Task RecalculateAllStockAsync();
}
