using MetalLink.Domain.Entities;
using MetalLink.Shared.Buyers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MetalLink.Application.Interfaces;

public interface IBuyerRepository
{
    Task<Buyer?> GetByIdAsync(long buyerId);
    Task<Buyer?> GetByIdAsync(long buyerId, CancellationToken cancellationToken);
    Task<Buyer?> GetByAccountNumberAsync(long accountNumber, CancellationToken cancellationToken = default);
    Task AddAsync(Buyer buyer, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Buyer>> SearchAsync(
        BuyerSearchRequestDto criteria,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Buyer>> SearchAsync(
        int? buyerId,
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

    Task UpdateAsync(Buyer buyer, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(int buyerId, CancellationToken cancellationToken = default);
    Task<long> GetNextAccountNumberAsync(CancellationToken ct);

    Task<IReadOnlyList<Buyer>> SearchBuyersWithZeroSendingTicketsAsync(
        long? companyId,
        long? siteId,
        int? buyerId,
        string? firstName,
        string? lastName,
        string? idNumber,
        long? accountNumber,
        CancellationToken cancellationToken = default);
}
