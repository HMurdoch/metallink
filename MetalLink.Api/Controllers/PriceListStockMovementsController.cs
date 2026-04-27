using MetalLink.Api.Extensions;
using MetalLink.Shared.Stock;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MetalLink.Infrastructure.Persistence;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/stock-movements")]
public class PriceListStockMovementsController : ControllerBase
{
    private readonly MetalLinkDbContext _db;

    public PriceListStockMovementsController(MetalLinkDbContext db)
    {
        _db = db;
    }

    [HttpGet("price-lists")]
    public async Task<ActionResult<List<PriceListStockMovementDto>>> GetPriceListStockMovements(
        [FromQuery] string? entityType = "Customer",
        [FromQuery] int[]? selectedPriceListIds = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int? productId = null,
        [FromQuery] string? movementType = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var entityFlag = entityType == "Customer" ? 'C' : 'B';

        // Build query for stock movements with price list info
        var query = from sm in _db.Database.SqlQuery<StockMovementRow>($@"
            SELECT 
                sm.stock_movement_id,
                sm.product_id,
                sm.product_price_list_product_price_id,
                sm.movement_type,
                sm.weight_kg,
                sm.movement_date,
                sm.ticket_id,
                sm.ticket_line_id,
                sm.notes,
                p.product_code,
                p.isri_product_name as product_name,
                ppl.product_price_list_id,
                ppl.product_price_list_name,
                pplpp.price,
                t.ticket_number,
                tl.line_number
            FROM metal_link.stock_movements sm
            JOIN metal_link.products p ON p.product_id = sm.product_id
            JOIN metal_link.product_price_list_product_prices pplpp ON pplpp.product_price_list_product_price_id = sm.product_price_list_product_price_id
            JOIN metal_link.product_price_lists ppl ON ppl.product_price_list_id = pplpp.product_price_list_id
            LEFT JOIN metal_link.tickets t ON t.ticket_id = sm.ticket_id
            LEFT JOIN metal_link.ticket_lines tl ON tl.ticket_line_id = sm.ticket_line_id
            WHERE sm.is_active = true 
              AND p.is_active = true 
              AND ppl.is_active = true
              AND pplpp.is_active = true
              AND ppl.entity_flag = {entityFlag}
        ")
        where (selectedPriceListIds == null || selectedPriceListIds.Length == 0 || selectedPriceListIds.Contains(sm.ProductPriceListId))
        select sm;

        // Apply filters
        if (fromDate.HasValue)
        {
            query = query.Where(sm => sm.MovementDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(sm => sm.MovementDate <= toDate.Value);
        }

        if (productId.HasValue && productId.Value > 0)
        {
            query = query.Where(sm => sm.ProductId == productId.Value);
        }

        if (!string.IsNullOrWhiteSpace(movementType))
        {
            query = query.Where(sm => sm.MovementType == movementType);
        }

        var results = await query
            .OrderByDescending(sm => sm.MovementDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        var dtos = results.Select(sm => new PriceListStockMovementDto
        {
            StockMovementId = sm.StockMovementId,
            ProductId = sm.ProductId,
            ProductCode = sm.ProductCode,
            ProductName = sm.ProductName,
            ProductPriceListProductPriceId = sm.ProductPriceListProductPriceId,
            ProductPriceListId = sm.ProductPriceListId,
            PriceListName = sm.ProductPriceListName,
            MovementType = sm.MovementType,
            WeightKg = sm.WeightKg,
            MovementDate = sm.MovementDate,
            TicketId = sm.TicketId,
            TicketNumber = sm.TicketNumber,
            TicketLineId = sm.TicketLineId,
            LineNumber = sm.LineNumber,
            Notes = sm.Notes
        }).ToList();

        return dtos;
    }

    private record StockMovementRow(
        int StockMovementId,
        int ProductId,
        int ProductPriceListProductPriceId,
        string MovementType,
        decimal WeightKg,
        DateTime MovementDate,
        int? TicketId,
        int? TicketLineId,
        string? Notes,
        string ProductCode,
        string ProductName,
        int ProductPriceListId,
        string ProductPriceListName,
        decimal Price,
        string? TicketNumber,
        int? LineNumber);
}