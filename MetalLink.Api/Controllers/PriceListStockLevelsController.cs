using MetalLink.Api.Extensions;
using MetalLink.Shared.Stock;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MetalLink.Infrastructure.Persistence;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/stock-levels")]
public class PriceListStockLevelsController : ControllerBase
{
    private readonly MetalLinkDbContext _db;

    public PriceListStockLevelsController(MetalLinkDbContext db)
    {
        _db = db;
    }

    [HttpGet("price-lists")]
    public async Task<ActionResult<List<PriceListStockLevelDto>>> GetPriceListStockLevels(
        [FromQuery] string? entityType = "Customer",
        [FromQuery] int[]? selectedPriceListIds = null,
        [FromQuery] int? productGroupId = null,
        [FromQuery] int? productId = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? letter = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var entityFlag = entityType == "Customer" ? 'C' : 'B';

        // Build query for stock levels with price list info
        var query = from sl in _db.Database.SqlQuery<StockLevelRow>($@"
            SELECT 
                sl.stock_level_id,
                sl.product_id,
                sl.product_price_list_product_price_id,
                sl.weight_kg,
                p.product_code,
                COALESCE(p.starred_product_alias, p.isri_product_name) as product_name,
                p.product_group_id,
                ppl.product_price_list_id,
                ppl.product_price_list_name,
                pplpp.price
            FROM metal_link.stock_levels sl
            JOIN metal_link.products p ON p.product_id = sl.product_id
            JOIN metal_link.product_price_list_product_prices pplpp ON pplpp.product_price_list_product_price_id = sl.product_price_list_product_price_id
            JOIN metal_link.product_price_lists ppl ON ppl.product_price_list_id = pplpp.product_price_list_id
            WHERE sl.is_active = true 
              AND p.is_active = true 
              AND p.starred_product = true
              AND ppl.is_active = true
              AND pplpp.is_active = true
              AND ppl.entity_flag = {entityFlag}
        ")
        where (selectedPriceListIds == null || selectedPriceListIds.Length == 0 || selectedPriceListIds.Contains(sl.ProductPriceListId))
        select sl;

        // Apply filters
        if (productGroupId.HasValue && productGroupId.Value > 0)
        {
            query = query.Where(sl => sl.ProductGroupId == productGroupId.Value);
        }

        if (productId.HasValue && productId.Value > 0)
        {
            query = query.Where(sl => sl.ProductId == productId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(sl => 
                sl.ProductName.ToLower().Replace("* ", "").Contains(term) || 
                sl.ProductCode.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(letter) && letter != "ALL")
        {
            query = query.Where(sl => 
                sl.ProductName.StartsWith("* ") 
                    ? sl.ProductName.Substring(2, 1).ToUpper() == letter.ToUpper()
                    : sl.ProductName.StartsWith(letter, StringComparison.OrdinalIgnoreCase));
        }

        // Group by product to calculate totals
        var groupedQuery = from sl in query
                          group sl by sl.ProductId into g
                          select new
                          {
                              ProductId = g.Key,
                              ProductCode = g.First().ProductCode,
                              ProductName = g.First().ProductName,
                              PriceListLevels = g.ToList(),
                              TotalWeightKg = g.Sum(x => x.WeightKg)
                          };

        var results = await groupedQuery
            .OrderBy(x => x.ProductName.StartsWith("* ") ? x.ProductName.Substring(2) : x.ProductName)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        var dtos = new List<PriceListStockLevelDto>();
        foreach (var product in results)
        {
            foreach (var level in product.PriceListLevels)
            {
                dtos.Add(new PriceListStockLevelDto
                {
                    ProductId = product.ProductId,
                    ProductCode = product.ProductCode,
                    ProductName = product.ProductName,
                    ProductPriceListProductPriceId = level.ProductPriceListProductPriceId,
                    ProductPriceListId = level.ProductPriceListId,
                    PriceListName = level.ProductPriceListName,
                    WeightKg = level.WeightKg,
                    TotalWeightKg = product.TotalWeightKg
                });
            }
        }

        return dtos;
    }

    private record StockLevelRow(
        int StockLevelId,
        int ProductId,
        int ProductPriceListProductPriceId,
        decimal WeightKg,
        string ProductCode,
        string ProductName,
        int ProductGroupId,
        int ProductPriceListId,
        string ProductPriceListName,
        decimal Price);
}