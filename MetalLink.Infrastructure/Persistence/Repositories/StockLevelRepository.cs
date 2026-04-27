using MetalLink.Application.Interfaces;
using MetalLink.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MetalLink.Infrastructure.Persistence.Repositories;

/// <summary>
/// Minimal repository for metal_link.stock_levels.
/// We use raw SQL here because the rest of the EF model currently doesn't map this table.
/// Now supports multiple stock levels per product (one per active price list).
/// </summary>
public sealed class StockLevelRepository : IStockLevelRepository
{
    private readonly MetalLinkDbContext _db;

    public StockLevelRepository(MetalLinkDbContext db)
    {
        _db = db;
    }

    public async Task<decimal> GetOrCreateWeightKgAsync(int productId, int? productPriceListProductPriceId, int createdByOperatorId, CancellationToken ct = default)
    {
        if (productPriceListProductPriceId.HasValue)
        {
            await EnsureStockLevelForProductPriceAsync(productId, productPriceListProductPriceId.Value, createdByOperatorId, ct);

            return await _db.Database.SqlQuery<decimal>($@"
                    SELECT COALESCE(SUM(weight_kg), 0) AS ""Value""
                    FROM metal_link.stock_levels
                    WHERE product_id = {productId}
                      AND product_price_list_product_price_id = {productPriceListProductPriceId}
                      AND is_active = true
                ")
                .SingleAsync(ct);
        }

        await EnsureStockLevelsForProductAsync(productId, createdByOperatorId, ct);

        return await _db.Database.SqlQuery<decimal>($@"
                SELECT COALESCE(SUM(weight_kg), 0) AS ""Value""
                FROM metal_link.stock_levels
                WHERE product_id = {productId} AND is_active = true
            ")
            .SingleAsync(ct);
    }

    public async Task UpdateWeightKgAsync(int productId, int? productPriceListProductPriceId, decimal deltaWeightKg, int createdByOperatorId, CancellationToken ct = default)
    {
        if (deltaWeightKg == 0)
            return;

        if (productPriceListProductPriceId.HasValue)
        {
            await EnsureStockLevelForProductPriceAsync(productId, productPriceListProductPriceId.Value, createdByOperatorId, ct);

            await _db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE metal_link.stock_levels
                SET weight_kg = weight_kg + {deltaWeightKg}, updated_time = now()
                WHERE product_id = {productId}
                  AND product_price_list_product_price_id = {productPriceListProductPriceId}
                  AND is_active = true;
            ", ct);
            return;
        }

        await EnsureStockLevelForProductPriceAsync(productId, null, createdByOperatorId, ct);

        await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE metal_link.stock_levels
            SET weight_kg = weight_kg + {deltaWeightKg}, updated_time = now()
            WHERE product_id = {productId}
              AND product_price_list_product_price_id IS NULL
              AND is_active = true;
        ", ct);
    }

    private async Task EnsureStockLevelForProductPriceAsync(int productId, int? productPriceListProductPriceId, int createdByOperatorId, CancellationToken ct)
    {
        if (productPriceListProductPriceId.HasValue)
        {
            await _db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO metal_link.stock_levels (product_id, product_price_list_product_price_id, weight_kg, created_by_operator_id, is_active, created_time, updated_time)
                SELECT {productId}, {productPriceListProductPriceId}, 0, {createdByOperatorId}, true, now(), now()
                WHERE NOT EXISTS (
                    SELECT 1 FROM metal_link.stock_levels sl
                    WHERE sl.product_id = {productId}
                      AND sl.product_price_list_product_price_id = {productPriceListProductPriceId}
                      AND sl.is_active = true
                );
            ", ct);
            return;
        }

        await _db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO metal_link.stock_levels (product_id, product_price_list_product_price_id, weight_kg, created_by_operator_id, is_active, created_time, updated_time)
            SELECT {productId}, NULL, 0, {createdByOperatorId}, true, now(), now()
            WHERE NOT EXISTS (
                SELECT 1 FROM metal_link.stock_levels sl
                WHERE sl.product_id = {productId}
                  AND sl.product_price_list_product_price_id IS NULL
                  AND sl.is_active = true
            );
        ", ct);
    }

    private async Task EnsureStockLevelsForProductAsync(int productId, int createdByOperatorId, CancellationToken ct)
    {
        // Create stock levels for any missing active price list combinations
        await _db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO metal_link.stock_levels (product_id, product_price_list_product_price_id, weight_kg, created_by_operator_id, is_active, created_time, updated_time)
            SELECT {productId}, pplpp.product_price_list_product_price_id, 0, {createdByOperatorId}, true, now(), now()
            FROM metal_link.product_price_list_product_prices pplpp
            JOIN metal_link.product_price_lists ppl ON ppl.product_price_list_id = pplpp.product_price_list_id
            WHERE pplpp.product_id = {productId}
              AND ppl.is_active = true
              AND pplpp.is_active = true
              AND NOT EXISTS (
                  SELECT 1 FROM metal_link.stock_levels sl
                  WHERE sl.product_id = {productId}
                    AND sl.product_price_list_product_price_id = pplpp.product_price_list_product_price_id
                    AND sl.is_active = true
              );
        ", ct);
    }
}
