using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetalLink.Infrastructure.Persistence.Repositories;

public class StockOnHandRepository : IStockOnHandRepository
{
    private readonly MetalLinkDbContext _context;

    public StockOnHandRepository(MetalLinkDbContext context)
    {
        _context = context;
    }

    public async Task<StockOnHand?> GetByIdAsync(long stockOnHandId)
    {
        return await _context.Set<StockOnHand>()
            .Include(s => s.Site)
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.StockOnHandId == stockOnHandId);
    }

    public async Task<StockOnHand?> GetBySiteAndProductAsync(long siteId, long productId)
    {
        return await _context.Set<StockOnHand>()
            .Include(s => s.Site)
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.SiteId == siteId && s.ProductId == productId);
    }

    public async Task<IEnumerable<StockOnHand>> GetAllBySiteAsync(long siteId)
    {
        return await _context.Set<StockOnHand>()
            .Include(s => s.Site)
            .Include(s => s.Product)
            .Where(s => s.SiteId == siteId)
            .OrderBy(s => s.Product.ProductName)
            .ToListAsync();
    }

    public async Task<IEnumerable<StockOnHand>> GetAllByProductAsync(long productId)
    {
        return await _context.Set<StockOnHand>()
            .Include(s => s.Site)
            .Include(s => s.Product)
            .Where(s => s.ProductId == productId)
            .OrderBy(s => s.Site.SiteName)
            .ToListAsync();
    }

    public async Task<IEnumerable<StockOnHand>> GetAllAsync()
    {
        return await _context.Set<StockOnHand>()
            .Include(s => s.Site)
            .Include(s => s.Product)
            .OrderBy(s => s.Site.SiteName)
            .ThenBy(s => s.Product.ProductName)
            .ToListAsync();
    }

    public async Task<StockOnHand> AddAsync(StockOnHand stockOnHand)
    {
        await _context.Set<StockOnHand>().AddAsync(stockOnHand);
        return stockOnHand;
    }

    public Task UpdateAsync(StockOnHand stockOnHand)
    {
        _context.Set<StockOnHand>().Update(stockOnHand);
        return Task.CompletedTask;
    }

    public async Task RecalculateStockAsync(long siteId, long productId)
    {
        // Get or create stock on hand record
        var stockOnHand = await GetBySiteAndProductAsync(siteId, productId);
        if (stockOnHand == null)
        {
            stockOnHand = new StockOnHand(siteId, productId);
            await AddAsync(stockOnHand);
        }

        // Calculate from receiving movements (Stock IN)
        var totalReceived = await _context.Set<StockMovementReceiving>()
            .Where(m => m.SiteId == siteId && m.ProductId == productId && m.IsActive)
            .SumAsync(m => m.QuantityKg);

        // Calculate from sending movements (Stock OUT)
        var totalSent = await _context.Set<StockMovementSending>()
            .Where(m => m.SiteId == siteId && m.ProductId == productId && m.IsActive)
            .SumAsync(m => m.QuantityKg);

        // Calculate weighted average cost from receiving movements
        var receivingMovements = await _context.Set<StockMovementReceiving>()
            .Where(m => m.SiteId == siteId && m.ProductId == productId && m.IsActive)
            .ToListAsync();

        decimal averageCost = 0;
        if (receivingMovements.Any() && totalReceived > 0)
        {
            var totalValue = receivingMovements.Sum(m => m.TotalValue);
            averageCost = totalValue / totalReceived;
        }

        // Get last movement info
        var lastReceiving = await _context.Set<StockMovementReceiving>()
            .Where(m => m.SiteId == siteId && m.ProductId == productId && m.IsActive)
            .OrderByDescending(m => m.MovementDate)
            .FirstOrDefaultAsync();

        var lastSending = await _context.Set<StockMovementSending>()
            .Where(m => m.SiteId == siteId && m.ProductId == productId && m.IsActive)
            .OrderByDescending(m => m.MovementDate)
            .FirstOrDefaultAsync();

        DateTimeOffset? lastMovementDate = null;
        string? lastMovementType = null;

        if (lastReceiving != null && lastSending != null)
        {
            if (lastReceiving.MovementDate > lastSending.MovementDate)
            {
                lastMovementDate = lastReceiving.MovementDate;
                lastMovementType = "receiving";
            }
            else
            {
                lastMovementDate = lastSending.MovementDate;
                lastMovementType = "sending";
            }
        }
        else if (lastReceiving != null)
        {
            lastMovementDate = lastReceiving.MovementDate;
            lastMovementType = "receiving";
        }
        else if (lastSending != null)
        {
            lastMovementDate = lastSending.MovementDate;
            lastMovementType = "sending";
        }

        stockOnHand.Recalculate(totalReceived, totalSent, averageCost, lastMovementDate, lastMovementType);
        await UpdateAsync(stockOnHand);
    }

    public async Task RecalculateAllStockAsync()
    {
        // Get all unique site/product combinations from movements
        var receivingCombos = await _context.Set<StockMovementReceiving>()
            .Where(m => m.IsActive)
            .Select(m => new { m.SiteId, m.ProductId })
            .Distinct()
            .ToListAsync();

        var sendingCombos = await _context.Set<StockMovementSending>()
            .Where(m => m.IsActive)
            .Select(m => new { m.SiteId, m.ProductId })
            .Distinct()
            .ToListAsync();

        var allCombos = receivingCombos.Union(sendingCombos).Distinct();

        foreach (var combo in allCombos)
        {
            await RecalculateStockAsync(combo.SiteId, combo.ProductId);
        }
    }
}
