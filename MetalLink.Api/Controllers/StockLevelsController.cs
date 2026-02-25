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
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 500);

        // Normalize filters
        term = string.IsNullOrWhiteSpace(term) ? null : term.Trim();
        letter = string.IsNullOrWhiteSpace(letter) ? null : letter.Trim().ToUpperInvariant();
        if (letter == "ALL") letter = null;

        // We use raw SQL because stock_levels is not mapped in the EF model.
        // Only active products; stock_levels is active-only. Missing stock_levels rows => treat as 0.
        var like = term is null ? null : $"%{term}%";

        FormattableString sql = $@"
            SELECT
                p.product_id        AS ""ProductId"",
                p.product_code      AS ""ProductCode"",
                p.product_name      AS ""ProductName"",
                COALESCE(sl.weight_kg, 0) AS ""WeightKg""
            FROM metal_link.products p
            LEFT JOIN metal_link.stock_levels sl
                ON sl.product_id = p.product_id AND sl.is_active = true
            WHERE p.is_active = true
              AND ({like}::text IS NULL OR (p.product_name ILIKE {like} OR p.product_code ILIKE {like}))
              AND ({letter}::text IS NULL OR LEFT(p.product_name, 1) = {letter})
            ORDER BY p.product_name
            LIMIT {take};
        ";

        var result = await _db.Database
            .SqlQuery<StockLevelLookupDto>(sql)
            .ToArrayAsync(ct);

        return Ok(result);
    }
}
