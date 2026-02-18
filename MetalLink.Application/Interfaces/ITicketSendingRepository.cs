using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MetalLink.Domain.Entities;

namespace MetalLink.Application.Interfaces;

public interface ITicketSendingRepository
{
    Task<TicketSending?> GetByIdAsync(long ticketSendingId);
    Task<TicketSending?> GetByTicketNumberAsync(string ticketNumber);
    Task<IEnumerable<TicketSending>> SearchAsync(
        string? searchTerm = null,
        long? companyId = null,
        long? siteId = null,
        long? buyerId = null,
        string? firstName = null,
        string? lastName = null,
        string? idNumber = null,
        long? accountNumber = null,
        long? productId = null,
        string? ticketType = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        string? deliveryStatus = null,
        int pageNumber = 1,
        int pageSize = 50);
    Task<long> GetCountAsync(
        string? searchTerm = null,
        long? companyId = null,
        long? siteId = null,
        long? buyerId = null,
        string? firstName = null,
        string? lastName = null,
        string? idNumber = null,
        long? accountNumber = null,
        long? productId = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        string? deliveryStatus = null);
    Task<TicketSending> AddAsync(TicketSending ticket);
    Task UpdateAsync(TicketSending ticket);
    Task<string> GenerateTicketNumberAsync(long siteId);
    Task<string?> GetLastTicketNumberByPrefixAsync(string prefix);
    Task<long> GetNextTicketSequenceValueAsync(string prefix);
    Task<long> PeekNextTicketSequenceValueAsync(string prefix);

    Task<HashSet<long>> GetBuyerIdsWithActiveTicketsAsync(long? companyId = null, long? siteId = null, CancellationToken ct = default);
}
