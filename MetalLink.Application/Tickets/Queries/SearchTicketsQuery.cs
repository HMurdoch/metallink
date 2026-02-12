using MediatR;
using MetalLink.Shared.Tickets;

namespace MetalLink.Application.Tickets.Queries;

public sealed record SearchTicketsQuery(TicketSearchRequestDto Request)
    : IRequest<TicketSearchResultDto[]>;