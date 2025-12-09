using MetalLink.Domain.Entities;
using MetalLink.Shared.Customers;
using System.Threading;
using System.Threading.Tasks;

namespace MetalLink.Application.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(long customerId, CancellationToken cancellationToken = default);
    Task<Customer?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);

    // NEW: search with multiple filters
    Task<IReadOnlyList<Customer>> SearchAsync(
        CustomerSearchRequestDto criteria,
        CancellationToken cancellationToken = default);
}
