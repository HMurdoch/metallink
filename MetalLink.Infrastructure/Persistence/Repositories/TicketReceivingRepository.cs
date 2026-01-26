using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetalLink.Infrastructure.Persistence.Repositories;

public class TicketReceivingRepository : ITicketReceivingRepository
{
    private readonly MetalLinkDbContext _context;

    public TicketReceivingRepository(MetalLinkDbContext context)
    {
        _context = context;
    }

    public async Task<TicketReceiving?> GetByIdAsync(long ticketReceivingId)
    {
        return await _context.Set<TicketReceiving>()
            .Include(t => t.Customer)
                .ThenInclude(c => c.Company)
            .Include(t => t.Customer)
                .ThenInclude(c => c.Site)
            .Include(t => t.CreatedByOperator)
            .Include(t => t.TicketType)
            .Include(t => t.Lines)
                .ThenInclude(l => l.Product)
            .Include(t => t.Lines)
                .ThenInclude(l => l.CreatedByOperator)
            .FirstOrDefaultAsync(t => t.TicketReceivingId == ticketReceivingId && t.IsActive);
    }

    public async Task<TicketReceiving?> GetByTicketNumberAsync(string ticketNumber)
    {
        return await _context.Set<TicketReceiving>()
            .Include(t => t.Customer)
            .Include(t => t.CreatedByOperator)
            .Include(t => t.TicketType)
            .Include(t => t.Lines)
                .ThenInclude(l => l.Product)
            .Include(t => t.Lines)
                .ThenInclude(l => l.CreatedByOperator)
            .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber && t.IsActive);
    }

    public async Task<IEnumerable<TicketReceiving>> SearchAsync(
        string? searchTerm = null,
        long? companyId = null,
        long? siteId = null,
        long? customerId = null,
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
        var query = _context.Set<TicketReceiving>()
            .Include(t => t.Customer)
                .ThenInclude(c => c.Company)
            .Include(t => t.Customer)
                .ThenInclude(c => c.Site)
            .Include(t => t.CreatedByOperator)
            .Include(t => t.TicketType)
            .Include(t => t.Lines)
                .ThenInclude(l => l.CreatedByOperator)
            .Where(t => t.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(t => 
                t.TicketNumber.Contains(searchTerm) ||
                (t.Customer != null && t.Customer.FullName.Contains(searchTerm)) ||
                (t.VehicleRegistration != null && t.VehicleRegistration.Contains(searchTerm)));
        }

        if (companyId.HasValue)
            query = query.Where(t => t.Customer != null && t.Customer.CompanyId == companyId.Value);

        if (siteId.HasValue)
            query = query.Where(t => t.Customer != null && t.Customer.SiteId == siteId.Value);

        if (customerId.HasValue)
            query = query.Where(t => t.CustomerId == customerId.Value);

        if (!string.IsNullOrWhiteSpace(firstName))
            query = query.Where(t => t.Customer != null && t.Customer.FirstName != null && t.Customer.FirstName.Contains(firstName));

        if (!string.IsNullOrWhiteSpace(lastName))
            query = query.Where(t => t.Customer != null && t.Customer.LastName != null && t.Customer.LastName.Contains(lastName));

        if (!string.IsNullOrWhiteSpace(idNumber))
            query = query.Where(t => t.Customer != null && t.Customer.IdNumber != null && t.Customer.IdNumber.Contains(idNumber));

        if (accountNumber.HasValue)
            query = query.Where(t => t.Customer != null && t.Customer.AccountNumber == accountNumber.Value);

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
        long? customerId = null,
        string? firstName = null,
        string? lastName = null,
        string? idNumber = null,
        long? accountNumber = null,
        long? productId = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        string? deliveryStatus = null)
    {
        var query = _context.Set<TicketReceiving>()
            .Where(t => t.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(t => 
                t.TicketNumber.Contains(searchTerm) ||
                (t.Customer != null && t.Customer.FullName.Contains(searchTerm)) ||
                (t.VehicleRegistration != null && t.VehicleRegistration.Contains(searchTerm)));
        }

        if (customerId.HasValue)
            query = query.Where(t => t.CustomerId == customerId.Value);

        if (!string.IsNullOrWhiteSpace(firstName))
            query = query.Where(t => t.Customer != null && t.Customer.FirstName != null && t.Customer.FirstName.Contains(firstName));

        if (!string.IsNullOrWhiteSpace(lastName))
            query = query.Where(t => t.Customer != null && t.Customer.LastName != null && t.Customer.LastName.Contains(lastName));

        if (!string.IsNullOrWhiteSpace(idNumber))
            query = query.Where(t => t.Customer != null && t.Customer.IdNumber != null && t.Customer.IdNumber.Contains(idNumber));

        if (accountNumber.HasValue)
            query = query.Where(t => t.Customer != null && t.Customer.AccountNumber == accountNumber.Value);

        if (startDate.HasValue)
            query = query.Where(t => t.CreatedTime >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.CreatedTime <= endDate.Value);

        return await query.CountAsync();
    }

    public async Task<TicketReceiving> AddAsync(TicketReceiving ticket)
    {
        await _context.Set<TicketReceiving>().AddAsync(ticket);
        return ticket;
    }

    public Task UpdateAsync(TicketReceiving ticket)
    {
        _context.Set<TicketReceiving>().Update(ticket);
        return Task.CompletedTask;
    }

    public async Task<string> GenerateTicketNumberAsync(long siteId)
    {
        var site = await _context.Set<Site>().FindAsync(siteId);
        var year = DateTime.Now.Year;
        var prefix = $"RCV-{site?.SiteCode ?? "000"}-{year}";

        var lastTicket = await _context.Set<TicketReceiving>()
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
        var lastTicket = await _context.Set<TicketReceiving>()
            .Where(t => t.TicketNumber.StartsWith(prefix + "-"))
            .OrderByDescending(t => t.TicketNumber)
            .FirstOrDefaultAsync();

        return lastTicket?.TicketNumber;
    }
}
