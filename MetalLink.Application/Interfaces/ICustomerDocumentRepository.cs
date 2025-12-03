using MetalLink.Domain.Entities;

namespace MetalLink.Application.Interfaces;

public interface ICustomerDocumentRepository
{
    Task AddAsync(CustomerDocument document, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerDocument>> GetByCustomerIdAsync(long customerId, CancellationToken cancellationToken = default);
}
