using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MetalLink.Application.Interfaces;
using MetalLink.Shared.Tickets;

namespace MetalLink.Api.Controllers;

/// <summary>
/// Unified Tickets endpoint for legacy support.
/// Routes requests to appropriate TicketReceiving or TicketSending endpoints based on ticket type.
/// </summary>
[ApiController]
[Route("api/tickets")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly ITicketReceivingRepository _receivingRepo;
    private readonly ITicketSendingRepository _sendingRepo;
    private readonly IUnitOfWork _unitOfWork;

    public TicketsController(
        ITicketReceivingRepository receivingRepo,
        ITicketSendingRepository sendingRepo,
        IUnitOfWork unitOfWork)
    {
        _receivingRepo = receivingRepo;
        _sendingRepo = sendingRepo;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Get ticket by ID (searches both receiving and sending tickets)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TicketDto>> GetTicket(long id)
    {
        try
        {
            // Try receiving ticket first
            var receivingTicket = await _receivingRepo.GetByIdAsync(id);
            if (receivingTicket != null)
            {
                return Ok(MapReceivingTicketToDto(receivingTicket));
            }

            // Try sending ticket
            var sendingTicket = await _sendingRepo.GetByIdAsync(id);
            if (sendingTicket != null)
            {
                return Ok(MapSendingTicketToDto(sendingTicket));
            }

            return NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private TicketDto MapReceivingTicketToDto(MetalLink.Domain.Entities.TicketReceiving ticket)
    {
        return new TicketDto
        {
            TicketId = ticket.TicketReceivingId,
            TicketNumber = ticket.TicketNumber,
            TicketType = "receiving",
            CustomerId = ticket.CustomerId,
            BuyerId = null,
            FirstWeightKg = ticket.FirstWeightKg,
            SecondWeightKg = ticket.SecondWeightKg,
            NetWeightKg = ticket.NetWeightKg,
            UnitPricePerKg = 0m, // Receiving tickets don't have unit price
            Notes = ticket.Notes,
            CreatedTime = ticket.CreatedTime,
            Lines = ticket.Lines?.Select(l => new TicketLineDto
            {
                TicketLineId = l.ReceivingTicketLineId,
                TicketId = ticket.TicketReceivingId,
                ProductId = l.ProductId,
                ProductName = l.Product?.ProductName ?? string.Empty,
                WeightKg = l.NetWeightKg,
                UnitPricePerKg = l.UnitPricePerKg,
                LineTotal = l.NetWeightKg * l.UnitPricePerKg
            }).ToList() ?? new List<TicketLineDto>()
        };
    }

    private TicketDto MapSendingTicketToDto(MetalLink.Domain.Entities.TicketSending ticket)
    {
        return new TicketDto
        {
            TicketId = ticket.TicketSendingId,
            TicketNumber = ticket.TicketNumber,
            TicketType = "sending",
            CustomerId = 0,
            BuyerId = ticket.BuyerId,
            FirstWeightKg = ticket.FirstWeightKg,
            SecondWeightKg = ticket.SecondWeightKg,
            NetWeightKg = ticket.NetWeightKg,
            UnitPricePerKg = 0m, // Sending tickets don't have unit price
            Notes = ticket.Notes,
            CreatedTime = ticket.CreatedTime,
            Lines = ticket.Lines?.Select(l => new TicketLineDto
            {
                TicketLineId = l.TicketSendingLineId,
                TicketId = ticket.TicketSendingId,
                ProductId = l.ProductId,
                ProductName = l.Product?.ProductName ?? string.Empty,
                WeightKg = l.NetWeightKg,
                UnitPricePerKg = l.UnitPricePerKg,
                LineTotal = l.NetWeightKg * l.UnitPricePerKg
            }).ToList() ?? new List<TicketLineDto>()
        };
    }
}
