using MediatR;
using MetalLink.Application.Interfaces;
using MetalLink.Shared.Tickets;

namespace MetalLink.Application.Tickets.Queries;

public sealed record GetTicketByIdQuery(long TicketId) : IRequest<TicketDto?>;

public sealed class GetTicketByIdQueryHandler
    : IRequestHandler<GetTicketByIdQuery, TicketDto?>
{
    private readonly ITicketRepository _ticketRepository;

    public GetTicketByIdQueryHandler(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<TicketDto?> Handle(GetTicketByIdQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
        {
            return null;
        }

        return new TicketDto
        {
            TicketId = ticket.TicketId,
            SiteId = ticket.SiteId,
            CustomerId = ticket.CustomerId ?? 0,
            BuyerId = ticket.BuyerId,
            OperatorId = ticket.OperatorId,
            TicketNumber = ticket.TicketNumber,
            TicketType = ticket.TicketType,
            FirstWeightKg = ticket.FirstWeightKg,
            SecondWeightKg = ticket.SecondWeightKg,
            NetWeightKg = ticket.NetWeightKg,
            UnitPricePerKg = ticket.UnitPricePerKg,
            TotalAmount = ticket.TotalAmount,
            CurrencyCode = ticket.CurrencyCode,
            CurrencyId = ticket.CurrencyId,
            ProductId = ticket.ProductId,
            VehicleRegistration = ticket.VehicleRegistration,
            OfmWeighbridgeTicket = ticket.OfmWeighbridgeTicket,
            ForeignTicket = ticket.ForeignTicket,
            CkNumber = ticket.CkNumber,
            VatRate = ticket.VatRate,
            VatAmount = ticket.VatAmount,
            TotalInclVat = ticket.TotalInclVat,
            ProductDescription = ticket.ProductDescription,
            Notes = ticket.Notes,
            CreatedTime = ticket.CreatedTime,
            UpdatedTime = ticket.UpdatedTime
        };
    }
}
