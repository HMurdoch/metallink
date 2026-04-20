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

    public async Task<LegacyPrice?> GetByIdAsync(int priceId, CancellationToken ct = default)
    {
        return await _context.LegacyPrices
            .Include(p => p.Product)
            .FirstOrDefaultAsync(p => p.LegacyPriceId == priceId, ct);
    }

    public async Task<IReadOnlyList<LegacyPrice>> GetByProductIdAsync(int productId, CancellationToken ct = default)
    {
        return await _context.LegacyPrices
            .Where(p => p.ProductId == productId && p.IsActive)
            .ToListAsync(ct);
    }

    public async Task<LegacyPrice> AddAsync(LegacyPrice price, CancellationToken ct = default)
    {
        _context.LegacyPrices.Add(price);
        await _context.SaveChangesAsync(ct);
        return price;
    }

    public async Task UpdateAsync(LegacyPrice price, CancellationToken ct = default)
    {
        price.UpdatedTime = DateTimeOffset.UtcNow;
        _context.LegacyPrices.Update(price);
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
