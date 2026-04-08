using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MetalLink.Domain.Entities;

namespace MetalLink.Application.Interfaces;

public interface ITicketReceivingRepository
{
    Task<TicketReceiving?> GetByIdAsync(int ticketReceivingId);
    Task<TicketReceiving?> GetByTicketNumberAsync(string ticketNumber);
    Task<IEnumerable<TicketReceiving>> SearchAsync(
        string? searchTerm = null,
        int? companyId = null,
        int? siteId = null,
        int? customerId = null,
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
        int pageNumber = 1,
        int pageSize = 50);
    Task<long> GetCountAsync(
        string? searchTerm = null,
        int? companyId = null,
        int? siteId = null,
        int? customerId = null,
        string? firstName = null,
        string? lastName = null,
        string? idNumber = null,
        long? accountNumber = null,
        int? productId = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        string? deliveryStatus = null);
    Task<TicketReceiving> AddAsync(TicketReceiving ticket);
    Task UpdateAsync(TicketReceiving ticket);
    Task<string> GenerateTicketNumberAsync(int siteId);
    Task<string?> GetLastTicketNumberByPrefixAsync(string prefix);
    Task<long> GetNextTicketSequenceValueAsync(string prefix);
    Task<long> PeekNextTicketSequenceValueAsync(string prefix);
}

