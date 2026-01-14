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
            .Include(t => t.Company)
            .Include(t => t.Site)
            .Include(t => t.Buyer)
            .Include(t => t.Product)
            .Include(t => t.Lines)
                .ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(t => t.TicketSendingId == ticketSendingId && t.IsActive);
    }

    public async Task<TicketSending?> GetByTicketNumberAsync(string ticketNumber)
    {
        return await _context.Set<TicketSending>()
            .Include(t => t.Company)
            .Include(t => t.Site)
            .Include(t => t.Buyer)
            .Include(t => t.Product)
            .Include(t => t.Lines)
                .ThenInclude(l => l.Product)
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
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        string? deliveryStatus = null,
        int pageNumber = 1,
        int pageSize = 50)
    {
        var query = _context.Set<TicketSending>()
            .Include(t => t.Company)
            .Include(t => t.Site)
            .Include(t => t.Buyer)
            .Include(t => t.Product)
            .Where(t => t.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(t => 
                t.TicketNumber.Contains(searchTerm) ||
                (t.Buyer.BuyerName != null && t.Buyer.BuyerName.Contains(searchTerm)) ||
                (t.VehicleRegistration != null && t.VehicleRegistration.Contains(searchTerm)));
        }

        if (companyId.HasValue)
            query = query.Where(t => t.CompanyId == companyId.Value);

        if (siteId.HasValue)
            query = query.Where(t => t.SiteId == siteId.Value);

        if (buyerId.HasValue)
            query = query.Where(t => t.BuyerId == buyerId.Value);

        if (!string.IsNullOrWhiteSpace(firstName))
            query = query.Where(t => t.Buyer.BuyerName != null && t.Buyer.BuyerName.Contains(firstName));

        if (!string.IsNullOrWhiteSpace(lastName))
            query = query.Where(t => t.Buyer.BuyerName != null && t.Buyer.BuyerName.Contains(lastName));

        if (!string.IsNullOrWhiteSpace(idNumber))
            query = query.Where(t => t.Buyer.RegistrationNumber != null && t.Buyer.RegistrationNumber.Contains(idNumber));

        if (accountNumber.HasValue)
            query = query.Where(t => t.Buyer.AccountNumber == accountNumber.Value);

        if (productId.HasValue)
            query = query.Where(t => t.ProductId == productId.Value);

        if (startDate.HasValue)
            query = query.Where(t => t.CreatedTime >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.CreatedTime <= endDate.Value);

        if (!string.IsNullOrWhiteSpace(deliveryStatus))
            query = query.Where(t => t.DeliveryStatus == deliveryStatus);

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
                (t.Buyer.BuyerName != null && t.Buyer.BuyerName.Contains(searchTerm)) ||
                (t.VehicleRegistration != null && t.VehicleRegistration.Contains(searchTerm)));
        }

        if (companyId.HasValue)
            query = query.Where(t => t.CompanyId == companyId.Value);

        if (siteId.HasValue)
            query = query.Where(t => t.SiteId == siteId.Value);

        if (buyerId.HasValue)
            query = query.Where(t => t.BuyerId == buyerId.Value);

        if (!string.IsNullOrWhiteSpace(firstName))
            query = query.Where(t => t.Buyer.BuyerName != null && t.Buyer.BuyerName.Contains(firstName));

        if (!string.IsNullOrWhiteSpace(lastName))
            query = query.Where(t => t.Buyer.BuyerName != null && t.Buyer.BuyerName.Contains(lastName));

        if (!string.IsNullOrWhiteSpace(idNumber))
            query = query.Where(t => t.Buyer.RegistrationNumber != null && t.Buyer.RegistrationNumber.Contains(idNumber));

        if (accountNumber.HasValue)
            query = query.Where(t => t.Buyer.AccountNumber == accountNumber.Value);

        if (productId.HasValue)
            query = query.Where(t => t.ProductId == productId.Value);

        if (startDate.HasValue)
            query = query.Where(t => t.CreatedTime >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.CreatedTime <= endDate.Value);

        if (!string.IsNullOrWhiteSpace(deliveryStatus))
            query = query.Where(t => t.DeliveryStatus == deliveryStatus);

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
}
