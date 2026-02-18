using MetalLink.Domain.Entities;
using MetalLink.Shared.Customers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MetalLink.Application.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int customerId, CancellationToken cancellationToken = default);
    Task<Customer?> GetByAccountNumberAsync(long accountNumber, CancellationToken cancellationToken = default);
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Customer>> SearchAsync(
        CustomerSearchRequestDto criteria,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Customer>> SearchAsync(
        int? customerId,
        int? siteId,
        string? firstName,
        string? lastName,
        string? companyName,
        string? idNumber,
        long? accountNumber,
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

    // NEW:
    Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(int customerId, CancellationToken cancellationToken = default);
    Task<long> GetNextAccountNumberAsync(CancellationToken ct);

    Task<IReadOnlyList<Customer>> SearchCustomersWithZeroReceivingTicketsAsync(
        long? companyId,
        long? siteId,
        int? customerId,
        string? firstName,
        string? lastName,
        string? idNumber,
        long? accountNumber,
        CancellationToken cancellationToken = default);

}
