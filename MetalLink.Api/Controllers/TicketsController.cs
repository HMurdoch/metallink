using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MetalLink.Application.Tickets.Commands;
using MetalLink.Shared.Tickets;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TicketsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public sealed class CreateTicketRequest
    {
        public long SiteId { get; set; }
        public long CustomerId { get; set; }
        public long OperatorId { get; set; }

        // "weighbridge" or "platform"
        public string TicketType { get; set; } = "weighbridge";

        public string TicketNumber { get; set; } = string.Empty;

        public decimal? FirstWeightKg { get; set; }
        public decimal? SecondWeightKg { get; set; }

        public decimal UnitPricePerKg { get; set; }
        public string CurrencyCode { get; set; } = "ZAR";

        public string? ProductDescription { get; set; }
        public string? Notes { get; set; }
    }

    [HttpPost]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTicket(
        [FromBody] CreateTicketRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateTicketCommand(
            SiteId: request.SiteId,
            CustomerId: request.CustomerId,
            OperatorId: request.OperatorId,
            TicketType: request.TicketType,
            TicketNumber: request.TicketNumber,
            FirstWeightKg: request.FirstWeightKg,
            SecondWeightKg: request.SecondWeightKg,
            UnitPricePerKg: request.UnitPricePerKg,
            CurrencyCode: request.CurrencyCode,
            ProductDescription: request.ProductDescription,
            Notes: request.Notes
        );

        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetTicketById), new { id = result.TicketId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicketById(long id, CancellationToken cancellationToken)
    {
        // Proper query will come in a later chapter
        return Ok(new { message = "GetTicketById not implemented yet", id });
    }
}
