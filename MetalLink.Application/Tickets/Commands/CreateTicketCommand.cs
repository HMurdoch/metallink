using MediatR;
using MetalLink.Shared.Tickets;

namespace MetalLink.Application.Tickets.Commands;

public sealed record CreateTicketCommand(
    long SiteId,
    long CustomerId,
    long OperatorId,
    string TicketType,          // "weighbridge" or "platform"
    string TicketNumber,
    decimal? FirstWeightKg,
    decimal? SecondWeightKg,
    decimal UnitPricePerKg,
    string CurrencyCode,
    string? ProductDescription,
    string? Notes,
    string? VehicleRegistration,
    string? OfmWeighbridgeTicket,
    string? ForeignTicket,
    string? CkNumber,
    long? ProductId,
    long? CurrencyId
) : IRequest<TicketDto>;
