using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetalLink.Infrastructure.Persistence.Repositories;

public class BuyerRepository : IBuyerRepository
{
    private readonly MetalLinkDbContext _context;

    public BuyerRepository(MetalLinkDbContext context)
    {
        _context = context;
    }

    public async Task<Buyer?> GetByIdAsync(long buyerId)
    {
        return await _context.Set<Buyer>()
            .Include(b => b.Company)
            .Include(b => b.Site)
            .FirstOrDefaultAsync(b => b.BuyerId == buyerId && b.IsActive);
    }

    public async Task<Buyer?> GetByAccountNumberAsync(long accountNumber)
    {
        return await _context.Set<Buyer>()
            .Include(b => b.Company)
            .Include(b => b.Site)
            .FirstOrDefaultAsync(b => b.AccountNumber == accountNumber && b.IsActive);
    }

    public async Task<IEnumerable<Buyer>> GetAllAsync()
    {
        return await _context.Set<Buyer>()
            .Include(b => b.Company)
            .Include(b => b.Site)
            .Where(b => b.IsActive)
            .OrderBy(b => b.BuyerName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Buyer>> SearchAsync(string searchTerm, int pageNumber = 1, int pageSize = 50)
    {
        var query = _context.Set<Buyer>()
            .Include(b => b.Company)
            .Include(b => b.Site)
            .Where(b => b.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(b =>
                (b.BuyerName != null && b.BuyerName.Contains(searchTerm)) ||
                (b.ContactPerson != null && b.ContactPerson.Contains(searchTerm)) ||
                (b.AccountNumber != null && b.AccountNumber.ToString().Contains(searchTerm)));
        }

        return await query
            .OrderBy(b => b.BuyerName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Buyer> AddAsync(Buyer buyer)
    {
        await _context.Set<Buyer>().AddAsync(buyer);
        return buyer;
    }

    public Task UpdateAsync(Buyer buyer)
    {
        _context.Set<Buyer>().Update(buyer);
        return Task.CompletedTask;
    }
}
