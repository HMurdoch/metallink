using MediatR;
using MetalLink.Application.Interfaces;

namespace MetalLink.Application.Tickets.Commands;

public sealed record DeleteTicketCommand(long TicketId) : IRequest;

public sealed class DeleteTicketCommandHandler : IRequestHandler<DeleteTicketCommand>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTicketCommandHandler(
        ITicketRepository ticketRepository,
        IUnitOfWork unitOfWork)
    {
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteTicketCommand request, CancellationToken cancellationToken)
    {
        await _ticketRepository.SoftDeleteAsync(request.TicketId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
