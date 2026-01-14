using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MetalLink.Domain.Entities;

namespace MetalLink.Application.Interfaces;

public interface IStockMovementReceivingRepository
{
    Task<StockMovementReceiving?> GetByIdAsync(long stockMovementReceivingId);
    Task<IEnumerable<StockMovementReceiving>> GetByTicketReceivingIdAsync(long ticketReceivingId);
    Task<IEnumerable<StockMovementReceiving>> SearchAsync(
        long? siteId = null,
        long? productId = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        int pageNumber = 1,
        int pageSize = 100);
    Task<StockMovementReceiving> AddAsync(StockMovementReceiving movement);
    Task UpdateAsync(StockMovementReceiving movement);
}
