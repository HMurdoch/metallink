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
    }

    public void Update(ProductPriceListProductPrice price)
    {
        _context.ProductPriceListProductPrices.Update(price);
    }
}
