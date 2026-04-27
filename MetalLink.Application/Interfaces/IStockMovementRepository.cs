using System.Threading;
using System.Threading.Tasks;

namespace MetalLink.Application.Interfaces;

public interface IStockMovementRepository
{
    Task AddAsync(
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
        CancellationToken ct = default);
}
