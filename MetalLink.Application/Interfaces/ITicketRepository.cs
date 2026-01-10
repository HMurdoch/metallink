using MetalLink.Domain.Entities;
using MetalLink.Shared.Tickets;

namespace MetalLink.Application.Interfaces;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(long ticketId, CancellationToken cancellationToken = default);
    Task<Ticket?> GetByTicketNumberAsync(string ticketNumber, CancellationToken cancellationToken = default);
    Task AddAsync(Ticket ticket, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Ticket>> SearchAsync(
        TicketSearchRequestDto request,
        CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(long ticketId, CancellationToken cancellationToken = default);
}
