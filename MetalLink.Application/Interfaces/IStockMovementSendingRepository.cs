using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MetalLink.Domain.Entities;

namespace MetalLink.Application.Interfaces;

public interface IStockMovementSendingRepository
{
    Task<StockMovementSending?> GetByIdAsync(long stockMovementSendingId);
    Task<IEnumerable<StockMovementSending>> GetByTicketSendingIdAsync(long ticketSendingId);
    Task<IEnumerable<StockMovementSending>> SearchAsync(
        long? siteId = null,
        long? productId = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        int pageNumber = 1,
        int pageSize = 100);
    Task<StockMovementSending> AddAsync(StockMovementSending movement);
    Task UpdateAsync(StockMovementSending movement);
}
