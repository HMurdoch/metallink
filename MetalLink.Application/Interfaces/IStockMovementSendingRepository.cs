using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MetalLink.Domain.Entities;

namespace MetalLink.Application.Interfaces;

public interface IStockMovementSendingRepository
{
    Task<StockMovementSending?> GetByIdAsync(int stockMovementSendingId);
    Task<IEnumerable<StockMovementSending>> GetByTicketSendingIdAsync(int ticketSendingId);
    Task<IEnumerable<StockMovementSending>> SearchAsync(
        int? siteId = null,
        int? productId = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        int pageNumber = 1,
        int pageSize = 100);
    Task<StockMovementSending> AddAsync(StockMovementSending movement);
    Task UpdateAsync(StockMovementSending movement);
}
