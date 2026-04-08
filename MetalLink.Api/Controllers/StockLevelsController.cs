using MetalLink.Infrastructure.Persistence;
using MetalLink.Shared.Stock;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/stock-levels")]
[Authorize]
public sealed class StockLevelsController : ControllerBase
{
    private readonly MetalLinkDbContext _db;

    public StockLevelsController(MetalLinkDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns stock_levels joined with products (active only), filterable by search term and/or first-letter.
    /// </summary>
    [HttpGet("lookup")]
    public async Task<ActionResult<StockLevelLookupDto[]>> Lookup(
        [FromQuery] string? term = null,
        [FromQuery] string? letter = null,
        [FromQuery] int? groupId = null,
        [FromQuery] bool includeNonStarred = false,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 500);

        // Normalize filters
        term = string.IsNullOrWhiteSpace(term) ? null : term.Trim();
        letter = string.IsNullOrWhiteSpace(letter) ? null : letter.Trim().ToUpperInvariant();
        if (letter == "ALL") letter = null;

        // We use raw SQL because stock_levels is not mapped in the EF model.
        var like = term is null ? null : $"%{term}%";

        // Using explicit parameters to avoid "could not determine data type" errors with nulls in PostgreSQL
        var pIncludeNonStarred = new Npgsql.NpgsqlParameter("includeNonStarred", includeNonStarred);
        var pGroupId = new Npgsql.NpgsqlParameter("groupId", (object?)groupId ?? DBNull.Value);
        var pLike = new Npgsql.NpgsqlParameter("like", (object?)like ?? DBNull.Value);
        var pLetter = new Npgsql.NpgsqlParameter("letter", (object?)letter ?? DBNull.Value);
        var pTake = new Npgsql.NpgsqlParameter("take", take);

        var sql = @"
            SELECT
                p.product_id        AS ""ProductId"",
                p.isri_product_code AS ""ProductCode"",
                p.q_key             AS ""QKey"",
                COALESCE(p.starred_product_alias, p.isri_product_name) AS ""ProductName"",
                COALESCE(sl.weight_kg, 0) AS ""WeightKg""
            FROM metal_link.products p
            LEFT JOIN metal_link.stock_levels sl
                ON sl.product_id = p.product_id AND sl.is_active = true
            WHERE p.is_active = true
              AND (@includeNonStarred::boolean = true OR p.starred_product = true)
              AND (@groupId::int IS NULL OR @groupId::int = 0 OR p.product_group_id = @groupId::int)
              AND (@like::text IS NULL OR (p.isri_product_name ILIKE @like::text OR p.isri_product_code ILIKE @like::text OR p.starred_product_alias ILIKE @like::text OR p.q_key ILIKE @like::text))
              AND (@letter::text IS NULL OR LEFT(COALESCE(p.starred_product_alias, p.isri_product_name), 1) = @letter::text)
            ORDER BY ""ProductName""
            LIMIT @take::int;
        ";

        var result = await _db.Database
            .SqlQueryRaw<StockLevelLookupDto>(sql, pIncludeNonStarred, pGroupId, pLike, pLetter, pTake)
            .ToArrayAsync(ct);

        Console.WriteLine($"[DEBUG] StockLevelsController.Lookup: Found {result.Length} products for term='{term}', letter='{letter}'.");
        foreach(var r in result.Take(5)) {
            Console.WriteLine($"  - Product: {r.ProductName} ({r.ProductCode}), Stock: {r.WeightKg}kg");
        }
        return Ok(result);
    }
}
