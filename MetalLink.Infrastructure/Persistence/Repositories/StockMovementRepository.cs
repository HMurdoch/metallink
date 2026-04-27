using MetalLink.Application.Interfaces;
using MetalLink.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MetalLink.Infrastructure.Persistence.Repositories;

/// <summary>
/// Minimal repository for metal_link.stock_movements.
/// Inserts one movement row per stock change.
/// </summary>
public sealed class StockMovementRepository : IStockMovementRepository
{
    private readonly MetalLinkDbContext _db;

    public StockMovementRepository(MetalLinkDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(
        int productId,
        decimal baseWeightKg,
        decimal buyWeightKg,
        decimal sellWeightKg,
        decimal unitPricePerKg,
        int createdByOperatorId,
        string notes,
        int? productPriceListId = null,
        int? productPriceListProductPriceId = null,
        int? receivingTicketId = null,
        int? receivingTicketLineId = null,
        int? sendingTicketId = null,
        int? sendingTicketLineId = null,
        CancellationToken ct = default)
    {
        return _db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO metal_link.stock_movements
                (product_id, base_weight_kg, buy_weight_kg, sell_weight_kg, product_price_list_id, product_price_list_product_price_id, receiving_ticket_id, receiving_ticket_line_id, sending_ticket_id, sending_ticket_line_id, created_by_operator_id, notes, is_active)
            VALUES
                ({productId}, {baseWeightKg}, {buyWeightKg}, {sellWeightKg}, {productPriceListId}, {productPriceListProductPriceId}, {receivingTicketId}, {receivingTicketLineId}, {sendingTicketId}, {sendingTicketLineId}, {createdByOperatorId}, {notes}, true);
        ", ct);
    }
}
