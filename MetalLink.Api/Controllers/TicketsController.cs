using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MetalLink.Application.Tickets.Commands;
using MetalLink.Application.Tickets.Queries;
using MetalLink.Domain.Entities;
using MetalLink.Infrastructure.Persistence;
using MetalLink.Shared.Tickets;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly MetalLinkDbContext _db;

    public TicketsController(IMediator mediator, MetalLinkDbContext db)
    {
        _mediator = mediator;
        _db = db;
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

        // Header / vehicle
        public string? VehicleRegistration { get; set; }
        public string? OfmWeighbridgeTicket { get; set; }
        public string? ForeignTicket { get; set; }
        public string? CkNumber { get; set; }
        
        // Optional FK references
        public long? ProductId { get; set; }
        public long? CurrencyId { get; set; }
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
            Notes: request.Notes,
            VehicleRegistration: request.VehicleRegistration,
            OfmWeighbridgeTicket: request.OfmWeighbridgeTicket,
            ForeignTicket: request.ForeignTicket,
            CkNumber: request.CkNumber,
            ProductId: request.ProductId,
            CurrencyId: request.CurrencyId
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

    // Search must come before the {id:long} route to avoid routing "search" into the id parameter.
    [HttpPost("search")]
    [ProducesResponseType(typeof(IEnumerable<TicketSearchResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchTickets(
        [FromBody] TicketSearchRequestDto request,
        CancellationToken cancellationToken)
    {
        var query = new SearchTicketsQuery(request);
        var results = await _mediator.Send(query, cancellationToken);
        return Ok(results);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicketById(long id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTicketByIdQuery(id), cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpDelete("{ticketId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteTicket(long ticketId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteTicketCommand(ticketId), cancellationToken);
        return NoContent();
    }

    // ---------------- Ticket lines (receiving) ----------------

    public sealed class CreateTicketLineRequest
    {
        public long ProductId { get; set; }
        public decimal WeightKg { get; set; }
    }

    /// <summary>
    /// Create one or more ticket lines for a given ticket.
    /// Prices are derived from the customer's price code (A/B/C) and the product's prices.
    /// </summary>
    [HttpPost("{ticketId:long}/lines")]
    [ProducesResponseType(typeof(IEnumerable<TicketLineDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddTicketLines(
        long ticketId,
        [FromBody] CreateTicketLineRequest[] lines,
        CancellationToken cancellationToken)
    {
        if (lines == null || lines.Length == 0)
            return BadRequest(new { message = "At least one line is required." });

        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.TicketId == ticketId && t.IsActive, cancellationToken);
        if (ticket == null)
            return NotFound(new { message = $"Ticket {ticketId} not found or inactive." });

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.CustomerId == ticket.CustomerId && c.IsActive, cancellationToken);
        if (customer == null)
            return BadRequest(new { message = $"Customer {ticket.CustomerId} not found or inactive." });

        var priceCode = (customer.PriceCode ?? "A").Trim().ToUpperInvariant();
        const decimal vatRate = 0.15m;
        var now = DateTimeOffset.UtcNow;

        var createdLines = new List<TicketLine>();

        foreach (var line in lines)
        {
            if (line.WeightKg <= 0)
                return BadRequest(new { message = "WeightKg must be greater than zero for all lines." });

            var product = await _db.Products.FirstOrDefaultAsync(p => p.ProductId == line.ProductId && p.IsActive, cancellationToken);
            if (product == null)
                return BadRequest(new { message = $"Product {line.ProductId} not found or inactive." });

            var price = await _db.Prices.FirstOrDefaultAsync(p => p.ProductId == line.ProductId && p.IsActive, cancellationToken);
            if (price == null)
                return BadRequest(new { message = $"Price for product {line.ProductId} not found." });

            decimal unitPrice = priceCode switch
            {
                "B" => price.PriceB,
                "C" => price.PriceC,
                _ => price.PriceA
            };

            var lineTotal = decimal.Round(line.WeightKg * unitPrice, 2, MidpointRounding.AwayFromZero);
            var vatAmount = decimal.Round(lineTotal * vatRate, 2, MidpointRounding.AwayFromZero);
            var lineTotalIncl = lineTotal + vatAmount;

            var entity = new TicketLine
            {
                TicketId = ticket.TicketId,
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                WeightKg = line.WeightKg,
                UnitPricePerKg = unitPrice,
                LineTotal = lineTotal,
                VatAmount = vatAmount,
                TotalInclVat = lineTotalIncl,
                IsActive = true,
                CreatedTime = now,
                UpdatedTime = now
            };

            createdLines.Add(entity);
        }

        _db.TicketLines.AddRange(createdLines);

        // Recompute header totals from all active lines for this ticket
        var allLines = await _db.TicketLines
            .Where(l => l.TicketId == ticketId && l.IsActive)
            .ToListAsync(cancellationToken);

        var totalExcl = allLines.Sum(l => l.LineTotal);
        var totalVat = allLines.Sum(l => l.VatAmount);
        var headerTotalIncl = allLines.Sum(l => l.TotalInclVat);

        ticket.UpdateTotalsFromLines(vatRate, totalExcl, totalVat, headerTotalIncl);

        await _db.SaveChangesAsync(cancellationToken);

        var dtos = createdLines.Select(l => new TicketLineDto
        {
            TicketLineId = l.TicketLineId,
            TicketId = l.TicketId,
            ProductId = l.ProductId,
            ProductName = l.ProductName,
            WeightKg = l.WeightKg,
            UnitPricePerKg = l.UnitPricePerKg,
            LineTotal = l.LineTotal,
            VatAmount = l.VatAmount,
            TotalInclVat = l.TotalInclVat,
            IsActive = l.IsActive,
            CreatedTime = l.CreatedTime,
            UpdatedTime = l.UpdatedTime
        }).ToArray();

        return CreatedAtAction(nameof(GetTicketById), new { id = ticketId }, dtos);
    }

    [HttpGet("{ticketId:long}/lines")]
    [ProducesResponseType(typeof(IEnumerable<TicketLineDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicketLines(long ticketId, CancellationToken cancellationToken)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.TicketId == ticketId && t.IsActive, cancellationToken);
        if (ticket == null)
            return NotFound(new { message = $"Ticket {ticketId} not found or inactive." });

        var lines = await _db.TicketLines
            .Where(l => l.TicketId == ticketId && l.IsActive)
            .OrderBy(l => l.TicketLineId)
            .ToListAsync(cancellationToken);

        var dtos = lines.Select(l => new TicketLineDto
        {
            TicketLineId = l.TicketLineId,
            TicketId = l.TicketId,
            ProductId = l.ProductId,
            ProductName = l.ProductName,
            WeightKg = l.WeightKg,
            UnitPricePerKg = l.UnitPricePerKg,
            LineTotal = l.LineTotal,
            VatAmount = l.VatAmount,
            TotalInclVat = l.TotalInclVat,
            IsActive = l.IsActive,
            CreatedTime = l.CreatedTime,
            UpdatedTime = l.UpdatedTime
        }).ToArray();

        return Ok(dtos);
    }

    [HttpDelete("{ticketId:long}/lines/{lineId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTicketLine(long ticketId, long lineId, CancellationToken cancellationToken)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.TicketId == ticketId && t.IsActive, cancellationToken);
        if (ticket == null)
            return NotFound(new { message = $"Ticket {ticketId} not found or inactive." });

        var line = await _db.TicketLines.FirstOrDefaultAsync(
            l => l.TicketLineId == lineId && l.TicketId == ticketId && l.IsActive,
            cancellationToken);

        if (line == null)
            return NotFound(new { message = $"Ticket line {lineId} not found or inactive for ticket {ticketId}." });

        var now = DateTimeOffset.UtcNow;
        line.IsActive = false;
        line.UpdatedTime = now;

        var allLines = await _db.TicketLines
            .Where(l => l.TicketId == ticketId && l.IsActive)
            .ToListAsync(cancellationToken);

        var totalExcl = allLines.Sum(l => l.LineTotal);
        var totalVat = allLines.Sum(l => l.VatAmount);
        var headerTotalIncl = allLines.Sum(l => l.TotalInclVat);

        var vatRate = ticket.VatRate != 0 ? ticket.VatRate : 0.15m;
        ticket.UpdateTotalsFromLines(vatRate, totalExcl, totalVat, headerTotalIncl);

        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
