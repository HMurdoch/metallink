using Microsoft.EntityFrameworkCore;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using MetalLink.Shared.Tickets;

namespace MetalLink.Infrastructure.Persistence.Repositories;

public sealed class TicketRepository : ITicketRepository
{
    private readonly MetalLinkDbContext _dbContext;

    public TicketRepository(MetalLinkDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Ticket?> GetByIdAsync(long ticketId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId, cancellationToken);
    }

    public async Task<Ticket?> GetByTicketNumberAsync(string ticketNumber, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber, cancellationToken);
    }

    public async Task AddAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        await _dbContext.Tickets.AddAsync(ticket, cancellationToken);
    }

    public async Task<IReadOnlyList<Ticket>> SearchAsync(
        TicketSearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // Include both Customer and Buyer (one will be null depending on ticket type)
        var query = _dbContext.Tickets
            .Include(t => t.Customer)
                .ThenInclude(c => c!.Company)
            .Include(t => t.Customer)
                .ThenInclude(c => c!.Site)
            .Include(t => t.Buyer)
            .Where(t => t.IsActive && 
                   (t.Customer == null || t.Customer.IsActive))  // Allow null customer for sending tickets
            .AsQueryable();

        if (request.CustomerId.HasValue)
        {
            query = query.Where(t => t.CustomerId == request.CustomerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.IdNumber))
        {
            var id = request.IdNumber.Trim();
            query = query.Where(t => t.Customer!.IdNumber == id);
        }

        if (!string.IsNullOrWhiteSpace(request.FirstName))
        {
            var first = request.FirstName.Trim();
            query = query.Where(t => t.Customer!.FirstName!.Contains(first));
        }

        if (!string.IsNullOrWhiteSpace(request.LastName))
        {
            var last = request.LastName.Trim();
            query = query.Where(t => t.Customer!.LastName!.Contains(last));
        }

        if (!string.IsNullOrWhiteSpace(request.CompanyLetter))
        {
            var letter = char.ToUpperInvariant(request.CompanyLetter![0]);
            query = query.Where(t => t.Customer!.Company != null &&
                                     t.Customer.Company.CompanyName != null &&
                                     t.Customer.Company.CompanyName.ToUpper().StartsWith(letter));
        }

        if (request.CompanyId.HasValue)
        {
            query = query.Where(t => t.Customer!.CompanyId == request.CompanyId.Value);
        }

        if (request.SiteId.HasValue)
        {
            query = query.Where(t => t.Customer!.SiteId == request.SiteId.Value);
        }

        if (request.AccountNumber.HasValue)
        {
            query = query.Where(t => t.Customer!.AccountNumber == request.AccountNumber.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.TicketNumber))
        {
            var number = request.TicketNumber.Trim();
            query = query.Where(t => t.TicketNumber == number);
        }

        if (!string.IsNullOrWhiteSpace(request.TicketType))
        {
            var type = request.TicketType.Trim().ToLowerInvariant();
            // Note: TicketSearchRequestDto.TicketType is actually used to filter by Status field
            // Status can be "receiving" or "delivery"
            query = query.Where(t => t.Status.ToLower() == type);
        }

        if (request.CreatedFrom.HasValue)
        {
            query = query.Where(t => t.CreatedTime >= request.CreatedFrom.Value);
        }

        if (request.CreatedTo.HasValue)
        {
            query = query.Where(t => t.CreatedTime <= request.CreatedTo.Value);
        }

        return await query
            .OrderByDescending(t => t.CreatedTime)
            .Take(500)
            .ToListAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(long ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _dbContext.Tickets
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.TicketId == ticketId, cancellationToken);

        if (ticket == null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        ticket.SoftDelete(now);
    }
}
