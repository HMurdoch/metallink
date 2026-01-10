using MediatR;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using MetalLink.Shared.Tickets;

namespace MetalLink.Application.Tickets.Commands;

public sealed class CreateTicketCommandHandler
    : IRequestHandler<CreateTicketCommand, TicketDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTicketCommandHandler(
        ICustomerRepository customerRepository,
        ITicketRepository ticketRepository,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TicketDto> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            throw new InvalidOperationException($"Customer {request.CustomerId} not found.");
        }

        var existing = await _ticketRepository.GetByTicketNumberAsync(request.TicketNumber, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Ticket number '{request.TicketNumber}' already exists.");
        }

        var ticket = new Ticket(
            siteId: request.SiteId,
            customerId: request.CustomerId,
            operatorId: request.OperatorId,
            ticketNumber: request.TicketNumber,
            ticketType: request.TicketType,
            firstWeightKg: request.FirstWeightKg,
            secondWeightKg: request.SecondWeightKg,
            unitPricePerKg: request.UnitPricePerKg,
            currencyCode: request.CurrencyCode,
            productDescription: request.ProductDescription,
            notes: request.Notes,
            vehicleRegistration: request.VehicleRegistration,
            ofmWeighbridgeTicket: request.OfmWeighbridgeTicket,
            foreignTicket: request.ForeignTicket,
            ckNumber: request.CkNumber,
            productId: request.ProductId,
            currencyId: request.CurrencyId
        );

        await _ticketRepository.AddAsync(ticket, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TicketDto
        {
            TicketId = ticket.TicketId,
            SiteId = ticket.SiteId,
            CustomerId = ticket.CustomerId,
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
