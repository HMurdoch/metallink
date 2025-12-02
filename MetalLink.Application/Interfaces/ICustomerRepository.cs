using MetalLink.Domain.Entities;

namespace MetalLink.Application.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(long customerId, CancellationToken cancellationToken = default);
    Task<Customer?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
}
