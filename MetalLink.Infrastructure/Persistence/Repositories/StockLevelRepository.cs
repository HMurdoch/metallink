using MetalLink.Application.Interfaces;
using MetalLink.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MetalLink.Infrastructure.Persistence.Repositories;

/// <summary>
/// Minimal repository for metal_link.stock_levels.
/// We use raw SQL here because the rest of the EF model currently doesn't map this table.
/// </summary>
public sealed class StockLevelRepository : IStockLevelRepository
{
    private readonly MetalLinkDbContext _db;

    public StockLevelRepository(MetalLinkDbContext db)
    {
        _db = db;
    }

    public async Task<decimal> GetOrCreateWeightKgAsync(long productId, int createdByOperatorId, CancellationToken ct = default)
    {
        // Create if missing (weight defaults to 0)
        await _db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO metal_link.stock_levels (product_id, weight_kg, created_by_operator_id, is_active)
            SELECT {productId}, 0, {createdByOperatorId}, true
            WHERE NOT EXISTS (
                SELECT 1 FROM metal_link.stock_levels
                WHERE product_id = {productId}
            );
        ", ct);

        var weight = await _db.Database.SqlQuery<decimal>($@"
                SELECT weight_kg
                FROM metal_link.stock_levels
                WHERE product_id = {productId} AND is_active = true
                LIMIT 1
            ")
            .SingleAsync(ct);

        return weight;
    }

    public Task UpdateWeightKgAsync(long productId, decimal newWeightKg, CancellationToken ct = default)
    {
        return _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE metal_link.stock_levels
            SET weight_kg = {newWeightKg}, updated_time = now()
            WHERE product_id = {productId} AND is_active = true;
        ", ct);
    }
}
