using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetalLink.Infrastructure.Persistence.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly MetalLinkDbContext _context;

    public ProductRepository(MetalLinkDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(long productId, CancellationToken ct = default)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.ProductId == productId, ct);
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(string? term, bool? isActive, CancellationToken ct = default)
    {
        var query = _context.Products.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(term))
        {
            var searchTerm = term.Trim().ToLower();
            query = query.Where(p =>
                p.ProductName.ToLower().Contains(searchTerm) ||
                (p.ProductCode != null && p.ProductCode.ToLower().Contains(searchTerm)));
        }

        return await query
            .OrderBy(p => p.ProductName)
            .ToListAsync(ct);
    }

    public async Task<Product> AddAsync(Product product, CancellationToken ct = default)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync(ct);
        return product;
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        product.UpdatedTime = DateTimeOffset.UtcNow;
        _context.Products.Update(product);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long productId, CancellationToken ct = default)
    {
        var product = await GetByIdAsync(productId, ct);
        if (product != null)
        {
            product.IsActive = false;
            product.UpdatedTime = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(ct);
        }
    }
}
