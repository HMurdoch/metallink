using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetalLink.Infrastructure.Persistence.Repositories;

public class StockMovementSendingRepository : IStockMovementSendingRepository
{
    private readonly MetalLinkDbContext _context;

    public StockMovementSendingRepository(MetalLinkDbContext context)
    {
        _context = context;
    }

    public async Task<StockMovementSending?> GetByIdAsync(long stockMovementSendingId)
    {
        return await _context.Set<StockMovementSending>()
            .Include(m => m.Site)
            .Include(m => m.Product)
            .Include(m => m.TicketSending)
            .FirstOrDefaultAsync(m => m.StockMovementSendingId == stockMovementSendingId && m.IsActive);
    }

    public async Task<IEnumerable<StockMovementSending>> GetByTicketSendingIdAsync(long ticketSendingId)
    {
        return await _context.Set<StockMovementSending>()
            .Include(m => m.Site)
            .Include(m => m.Product)
            .Where(m => m.TicketSendingId == ticketSendingId && m.IsActive)
            .OrderBy(m => m.CreatedTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<StockMovementSending>> SearchAsync(
        long? siteId = null,
        long? productId = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        int pageNumber = 1,
        int pageSize = 100)
    {
        var query = _context.Set<StockMovementSending>()
            .Include(m => m.Site)
            .Include(m => m.Product)
            .Include(m => m.TicketSending)
            .Where(m => m.IsActive);

        if (siteId.HasValue)
            query = query.Where(m => m.SiteId == siteId.Value);

        if (productId.HasValue)
            query = query.Where(m => m.ProductId == productId.Value);

        if (startDate.HasValue)
            query = query.Where(m => m.MovementDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(m => m.MovementDate <= endDate.Value);

        return await query
            .OrderByDescending(m => m.MovementDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<StockMovementSending> AddAsync(StockMovementSending movement)
    {
        await _context.Set<StockMovementSending>().AddAsync(movement);
        return movement;
    }

    public Task UpdateAsync(StockMovementSending movement)
    {
        _context.Set<StockMovementSending>().Update(movement);
        return Task.CompletedTask;
    }
}
