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
        // Use Include() now that we fixed the shadow property issue
        var query = _dbContext.Tickets
            .Include(t => t.Customer)
                .ThenInclude(c => c.Company)
            .Include(t => t.Customer)
                .ThenInclude(c => c.Site)
            .Where(t => t.IsActive && t.Customer!.IsActive)
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
            query = query.Where(t => t.TicketType.ToLower() == type);
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
