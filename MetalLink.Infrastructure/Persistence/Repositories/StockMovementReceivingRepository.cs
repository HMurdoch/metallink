using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetalLink.Infrastructure.Persistence.Repositories;

public class StockMovementReceivingRepository : IStockMovementReceivingRepository
{
    private readonly MetalLinkDbContext _context;

    public StockMovementReceivingRepository(MetalLinkDbContext context)
    {
        _context = context;
    }

    public async Task<StockMovementReceiving?> GetByIdAsync(long stockMovementReceivingId)
    {
        return await _context.Set<StockMovementReceiving>()
            .Include(m => m.Site)
            .Include(m => m.Product)
            .Include(m => m.TicketReceiving)
            .FirstOrDefaultAsync(m => m.StockMovementReceivingId == stockMovementReceivingId && m.IsActive);
    }

    public async Task<IEnumerable<StockMovementReceiving>> GetByTicketReceivingIdAsync(long ticketReceivingId)
    {
        return await _context.Set<StockMovementReceiving>()
            .Include(m => m.Site)
            .Include(m => m.Product)
            .Where(m => m.TicketReceivingId == ticketReceivingId && m.IsActive)
            .OrderBy(m => m.CreatedTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<StockMovementReceiving>> SearchAsync(
        long? siteId = null,
        long? productId = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        int pageNumber = 1,
        int pageSize = 100)
    {
        var query = _context.Set<StockMovementReceiving>()
            .Include(m => m.Site)
            .Include(m => m.Product)
            .Include(m => m.TicketReceiving)
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

    public async Task<StockMovementReceiving> AddAsync(StockMovementReceiving movement)
    {
        await _context.Set<StockMovementReceiving>().AddAsync(movement);
        return movement;
    }

    public Task UpdateAsync(StockMovementReceiving movement)
    {
        _context.Set<StockMovementReceiving>().Update(movement);
        return Task.CompletedTask;
    }
}
