using MediatR;
using MetalLink.Shared.Tickets;

namespace MetalLink.Application.Tickets.Commands;

public sealed record CreateTicketCommand(
    long SiteId,
    long OperatorId,
    string TicketType,          // "weighbridge" or "platform"
    string TicketNumber,
    decimal? FirstWeightKg,
    decimal? SecondWeightKg,
    decimal UnitPricePerKg,
    string CurrencyCode,
    string? ProductDescription,
    string? Notes,
    string Status = "receiving", // "receiving" or "delivery"
    long? CustomerId = null,     // For receiving tickets (buying from customers)
    long? BuyerId = null,        // For delivery tickets (selling to buyers)
    string? VehicleRegistration = null,
    string? TrailerRegistration = null,
    string? DriverName = null,
    string? OfmWeighbridgeTicket = null,
    string? ForeignTicket = null,
    string? CkNumber = null,
    string? DeliveryNumber = null,
    string? RfidCardNumber = null,
    long? ProductId = null,
    long? CurrencyId = null
) : IRequest<TicketDto>;
