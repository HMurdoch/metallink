using MetalLink.Domain.Entities;
using MetalLink.Shared.Customers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MetalLink.Application.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(long customerId, CancellationToken cancellationToken = default);
    Task<Customer?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);

    // Search with DTO criteria (FirstName / LastName / Company / Site / Address etc.)
    Task<IReadOnlyList<Customer>> SearchAsync(
        CustomerSearchRequestDto criteria,
        CancellationToken cancellationToken = default);

    // Legacy-style search with flattened parameters (used by SearchCustomersQueryHandler)
    Task<IReadOnlyList<Customer>> SearchAsync(
        long? customerId,
        long? siteId,
        string? fullName,
        string? companyName,
        string? idNumber,
        string? accountNumber,
        string? priceCode,
        string? addressLine1,
        string? addressLine2,
        string? suburb,
        string? city,
        string? postalCode,
        string? phoneNumber,
        string? mobileNumber,
        string? email,
        CancellationToken cancellationToken = default);
}
