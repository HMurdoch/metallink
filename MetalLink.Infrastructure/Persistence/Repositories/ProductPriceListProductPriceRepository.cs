using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetalLink.Infrastructure.Persistence.Repositories;

public class ProductPriceListProductPriceRepository : IProductPriceListProductPriceRepository
{
    private readonly MetalLinkDbContext _context;

    public ProductPriceListProductPriceRepository(MetalLinkDbContext context)
    {
        _context = context;
    }

    public async Task<ProductPriceListProductPrice?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.ProductPriceListProductPrices
            .FirstOrDefaultAsync(x => x.ProductPriceListProductPriceId == id, ct);
    }

    public async Task<ProductPriceListProductPrice?> GetByProductAndListAsync(int productId, int priceListId, CancellationToken ct = default)
    {
        return await _context.ProductPriceListProductPrices
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.ProductPriceListId == priceListId && x.IsActive, ct);
    }

    public async Task<IEnumerable<ProductPriceListProductPrice>> GetByPriceListIdAsync(int priceListId, CancellationToken ct = default)
    {
        return await _context.ProductPriceListProductPrices
            .Where(x => x.ProductPriceListId == priceListId && x.IsActive)
            .Include(x => x.Product)
            .ToListAsync(ct);
    }

    public async Task AddAsync(ProductPriceListProductPrice price, CancellationToken ct = default)
    {
        await _context.ProductPriceListProductPrices.AddAsync(price, ct);

        // Create stock_levels for starred products
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO metal_link.stock_levels (product_id, product_price_list_product_price_id, weight_kg, created_by_operator_id, is_active, created_time, updated_time)
            SELECT p.product_id, {price.ProductPriceListProductPriceId}, 0, {price.CreatedByOperatorId}, true, now(), now()
            FROM metal_link.products p
            WHERE p.product_id = {price.ProductId} AND p.starred_product = true AND p.is_active = true
              AND NOT EXISTS (
                  SELECT 1 FROM metal_link.stock_levels sl
                  WHERE sl.product_id = p.product_id
                    AND sl.product_price_list_product_price_id = {price.ProductPriceListProductPriceId}
                    AND sl.is_active = true
              );
        ", ct);
    }

    public void Update(ProductPriceListProductPrice price)
    {
        _context.ProductPriceListProductPrices.Update(price);
    }
}
