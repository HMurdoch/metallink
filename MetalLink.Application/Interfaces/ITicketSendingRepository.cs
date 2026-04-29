using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MetalLink.Domain.Entities;

namespace MetalLink.Application.Interfaces;

public interface ITicketSendingRepository
{
    Task<TicketSending?> GetByIdAsync(int ticketSendingId);
    Task<TicketSending?> GetByTicketNumberAsync(string ticketNumber);
    Task<IEnumerable<TicketSending>> SearchAsync(
        string? searchTerm = null,
        int? companyId = null,
        int? siteId = null,
        int? buyerId = null,
        string? firstName = null,
        string? lastName = null,
        string? idNumber = null,
        long? accountNumber = null,
        int? productId = null,
        int? productGroupId = null,
        string? ticketType = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        string? deliveryStatus = null,
        string? sortBy = "created_time",
        string? sortDirection = "desc",
        int pageNumber = 1,
        int pageSize = 20);
    Task<long> GetCountAsync(
        string? searchTerm = null,
        int? companyId = null,
        int? siteId = null,
        int? buyerId = null,
        string? firstName = null,
        string? lastName = null,
        string? idNumber = null,
        long? accountNumber = null,
        int? productId = null,
        int? productGroupId = null,
        string? ticketType = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        string? deliveryStatus = null);
    Task<TicketSending> AddAsync(TicketSending ticket);
    Task UpdateAsync(TicketSending ticket);
    Task<string> GenerateTicketNumberAsync(int siteId);
    Task<string?> GetLastTicketNumberByPrefixAsync(string prefix);
    Task<long> GetNextTicketSequenceValueAsync(string prefix);
    Task<long> PeekNextTicketSequenceValueAsync(string prefix);

    Task<HashSet<int>> GetBuyerIdsWithActiveTicketsAsync(int? companyId = null, int? siteId = null, CancellationToken ct = default);
}
