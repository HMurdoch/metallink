using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetalLink.Infrastructure.Persistence.Repositories;

public class TicketSendingRepository : ITicketSendingRepository
{
    private readonly MetalLinkDbContext _context;

    public TicketSendingRepository(MetalLinkDbContext context)
    {
        _context = context;
    }

    public async Task<TicketSending?> GetByIdAsync(long ticketSendingId)
    {
        return await _context.Set<TicketSending>()
            .Include(t => t.Buyer)
                .ThenInclude(b => b.Company)
            .Include(t => t.Buyer)
                .ThenInclude(b => b.Site)
            .Include(t => t.CreatedByOperator)
            .Include(t => t.TicketType)
            .Include(t => t.Lines)
                .ThenInclude(l => l.Product)
            .Include(t => t.Lines)
                .ThenInclude(l => l.CreatedByOperator)
            .FirstOrDefaultAsync(t => t.TicketSendingId == ticketSendingId && t.IsActive);
    }

    public async Task<TicketSending?> GetByTicketNumberAsync(string ticketNumber)
    {
        return await _context.Set<TicketSending>()
            .Include(t => t.Buyer)
            .Include(t => t.CreatedByOperator)
            .Include(t => t.TicketType)
            .Include(t => t.Lines)
                .ThenInclude(l => l.Product)
            .Include(t => t.Lines)
                .ThenInclude(l => l.CreatedByOperator)
            .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber && t.IsActive);
    }

    public async Task<IEnumerable<TicketSending>> SearchAsync(
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
        int pageSize = 50)
    {
        var query = _context.Set<TicketSending>()
            .Include(t => t.Buyer)
                .ThenInclude(b => b.Company)
            .Include(t => t.Buyer)
                .ThenInclude(b => b.Site)
            .Include(t => t.CreatedByOperator)
            .Include(t => t.TicketType)
            .Include(t => t.Lines)
                .ThenInclude(l => l.Product)
            .Include(t => t.Lines)
                .ThenInclude(l => l.CreatedByOperator)
            .Where(t => t.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(t => 
                t.TicketNumber.Contains(searchTerm) ||
                (t.Buyer.FirstName != null && t.Buyer.FirstName.Contains(searchTerm)) ||
                (t.Buyer.LastName != null && t.Buyer.LastName.Contains(searchTerm)) ||
                (t.VehicleRegistration != null && t.VehicleRegistration.Contains(searchTerm)));
        }

        if (companyId.HasValue)
            query = query.Where(t => t.Buyer != null && t.Buyer.CompanyId == companyId.Value);

        if (siteId.HasValue)
            query = query.Where(t => t.Buyer != null && t.Buyer.SiteId == siteId.Value);

        if (buyerId.HasValue)
            query = query.Where(t => t.BuyerId == buyerId.Value);

        if (!string.IsNullOrWhiteSpace(firstName))
            query = query.Where(t => t.Buyer.FirstName != null && t.Buyer.FirstName.Contains(firstName));

        if (!string.IsNullOrWhiteSpace(lastName))
            query = query.Where(t => t.Buyer != null && t.Buyer.LastName != null && t.Buyer.LastName.Contains(lastName));

        if (!string.IsNullOrWhiteSpace(idNumber))
            query = query.Where(t => t.Buyer != null && ((t.Buyer.FirstName != null && t.Buyer.FirstName.Contains(idNumber)) || (t.Buyer.LastName != null && t.Buyer.LastName.Contains(idNumber))));

        if (accountNumber.HasValue)
            query = query.Where(t => t.Buyer != null && t.Buyer.AccountNumber == accountNumber.Value);

        if (productId.HasValue)
            query = query.Where(t => t.Lines.Any(l => l.ProductId == productId.Value));

        if (!string.IsNullOrWhiteSpace(ticketType))
            query = query.Where(t => t.TicketType != null && t.TicketType.TicketTypeName.ToLower() == ticketType.ToLower());

        if (startDate.HasValue)
            query = query.Where(t => t.CreatedTime >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.CreatedTime <= endDate.Value);

        return await query
            .OrderByDescending(t => t.CreatedTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<long> GetCountAsync(
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
        string? deliveryStatus = null)
    {
        var query = _context.Set<TicketSending>()
            .Where(t => t.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(t => 
                t.TicketNumber.Contains(searchTerm) ||
                (t.Buyer.FirstName != null && t.Buyer.FirstName.Contains(searchTerm)) ||
                (t.Buyer.LastName != null && t.Buyer.LastName.Contains(searchTerm)) ||
                (t.VehicleRegistration != null && t.VehicleRegistration.Contains(searchTerm)));
        }

        if (buyerId.HasValue)
            query = query.Where(t => t.BuyerId == buyerId.Value);

        if (!string.IsNullOrWhiteSpace(firstName))
            query = query.Where(t => t.Buyer.FirstName != null && t.Buyer.FirstName.Contains(firstName));

        if (!string.IsNullOrWhiteSpace(lastName))
            query = query.Where(t => t.Buyer != null && t.Buyer.LastName != null && t.Buyer.LastName.Contains(lastName));

        if (!string.IsNullOrWhiteSpace(idNumber))
            query = query.Where(t => t.Buyer != null && ((t.Buyer.FirstName != null && t.Buyer.FirstName.Contains(idNumber)) || (t.Buyer.LastName != null && t.Buyer.LastName.Contains(idNumber))));

        if (accountNumber.HasValue)
            query = query.Where(t => t.Buyer != null && t.Buyer.AccountNumber == accountNumber.Value);

        if (productId.HasValue)
            query = query.Where(t => t.Lines.Any(l => l.ProductId == productId.Value));

        if (startDate.HasValue)
            query = query.Where(t => t.CreatedTime >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.CreatedTime <= endDate.Value);

        return await query.CountAsync();
    }

    public async Task<TicketSending> AddAsync(TicketSending ticket)
    {
        await _context.Set<TicketSending>().AddAsync(ticket);
        return ticket;
    }

    public Task UpdateAsync(TicketSending ticket)
    {
        _context.Set<TicketSending>().Update(ticket);
        return Task.CompletedTask;
    }

    public async Task<string> GenerateTicketNumberAsync(long siteId)
    {
        var site = await _context.Set<Site>().FindAsync(siteId);
        var year = DateTime.Now.Year;
        var prefix = $"SND-{site?.SiteCode ?? "000"}-{year}";

        var lastTicket = await _context.Set<TicketSending>()
            .Where(t => t.TicketNumber.StartsWith(prefix))
            .OrderByDescending(t => t.TicketNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastTicket != null)
        {
            var parts = lastTicket.TicketNumber.Split('-');
            if (parts.Length > 0 && int.TryParse(parts[^1], out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}-{nextNumber:D6}";
    }

    public async Task<string?> GetLastTicketNumberByPrefixAsync(string prefix)
    {
        // IMPORTANT: include soft-deleted tickets when generating next number
        // because ticket numbers are unique even across inactive rows.
        var lastTicket = await _context.Set<TicketSending>()
            .IgnoreQueryFilters()
            .Where(t => t.TicketNumber.StartsWith(prefix))
            .OrderByDescending(t => t.TicketNumber)
            .FirstOrDefaultAsync();

        return lastTicket?.TicketNumber;
    }

    public async Task<long> GetNextTicketSequenceValueAsync(string prefix)
    {
        var seq = $"metal_link.ticket_number_{prefix.ToLowerInvariant()}_seq";
        var sql = $"SELECT nextval('{seq}')";
        return await _context.Database.SqlQueryRaw<long>(sql).SingleAsync();
    }

    public async Task<HashSet<long>> GetBuyerIdsWithActiveTicketsAsync(long? companyId = null, long? siteId = null, CancellationToken ct = default)
    {
        var query = _context.Set<TicketSending>()
            .Where(t => t.IsActive)
            .AsQueryable();

        if (companyId.HasValue)
            query = query.Where(t => t.Buyer != null && t.Buyer.CompanyId == companyId.Value);

        if (siteId.HasValue)
            query = query.Where(t => t.Buyer != null && t.Buyer.SiteId == siteId.Value);

        var ids = await query
            .Select(t => (long)t.BuyerId)
            .Distinct()
            .ToListAsync(ct);

        return ids.ToHashSet();
    }
}
