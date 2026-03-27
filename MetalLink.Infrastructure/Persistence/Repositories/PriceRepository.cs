using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetalLink.Infrastructure.Persistence.Repositories;

public class PriceRepository : IPriceRepository
{
    private readonly MetalLinkDbContext _context;

    public PriceRepository(MetalLinkDbContext context)
    {
        _context = context;
    }

    public async Task<Price?> GetByIdAsync(int priceId, CancellationToken ct = default)
    {
        return await _context.Prices
            .Include(p => p.Product)
            .FirstOrDefaultAsync(p => p.PriceId == priceId, ct);
    }

    public async Task<IReadOnlyList<Price>> GetByProductIdAsync(int productId, CancellationToken ct = default)
    {
        return await _context.Prices
            .Where(p => p.ProductId == productId && p.IsActive)
            .ToListAsync(ct);
    }

    public async Task<Price> AddAsync(Price price, CancellationToken ct = default)
    {
        _context.Prices.Add(price);
        await _context.SaveChangesAsync(ct);
        return price;
    }

    public async Task UpdateAsync(Price price, CancellationToken ct = default)
    {
        price.UpdatedTime = DateTimeOffset.UtcNow;
        _context.Prices.Update(price);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int priceId, CancellationToken ct = default)
    {
        var price = await GetByIdAsync(priceId, ct);
        if (price != null)
        {
            price.IsActive = false;
            price.UpdatedTime = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(ct);
        }
    }
}
