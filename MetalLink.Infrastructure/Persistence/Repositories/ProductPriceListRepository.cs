using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetalLink.Infrastructure.Persistence.Repositories;

public class ProductPriceListRepository : IProductPriceListRepository
{
    private readonly MetalLinkDbContext _context;

    public ProductPriceListRepository(MetalLinkDbContext context)
    {
        _context = context;
    }

    public async Task<ProductPriceList?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.ProductPriceLists
            .FirstOrDefaultAsync(x => x.ProductPriceListId == id, ct);
    }

    public async Task<IEnumerable<ProductPriceList>> GetByEntityFlagAsync(char entityFlag, CancellationToken ct = default)
    {
        return await _context.ProductPriceLists
            .Where(x => x.EntityFlag == entityFlag && x.IsActive)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<ProductPriceList>> GetAllActiveAsync(CancellationToken ct = default)
    {
        return await _context.ProductPriceLists
            .Where(x => x.IsActive)
            .ToListAsync(ct);
    }

    public async Task AddAsync(ProductPriceList priceList, CancellationToken ct = default)
    {
        await _context.ProductPriceLists.AddAsync(priceList, ct);
    }

    public void Update(ProductPriceList priceList)
    {
        _context.ProductPriceLists.Update(priceList);
    }
}
