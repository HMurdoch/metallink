using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MetalLink.Application.Interfaces;
using MetalLink.Application.Services;
using MetalLink.Api.Extensions;
using MetalLink.Domain.Entities;
using MetalLink.Shared.Tickets;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/tickets-sending")]
public class TicketsSendingController : ControllerBase
{
    private readonly ITicketSendingRepository _ticketSendingRepo;
    private readonly IBuyerRepository _buyerRepo;
    private readonly IProductRepository _productRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TicketNumberService _ticketNumberService;
    private readonly WeightCalculationService _weightCalculationService;
    private readonly PriceLookupService _priceLookupService;

    public TicketsSendingController(
        ITicketSendingRepository ticketSendingRepo,
        IBuyerRepository buyerRepo,
        IProductRepository productRepo,
        IUnitOfWork unitOfWork,
        TicketNumberService ticketNumberService,
        WeightCalculationService weightCalculationService,
        PriceLookupService priceLookupService)
    {
        _ticketSendingRepo = ticketSendingRepo;
        _buyerRepo = buyerRepo;
        _productRepo = productRepo;
        _unitOfWork = unitOfWork;
        _ticketNumberService = ticketNumberService;
        _weightCalculationService = weightCalculationService;
        _priceLookupService = priceLookupService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TicketSendingDto>> GetTicketSending(int id)
    {
        var ticket = await _ticketSendingRepo.GetByIdAsync(id);
        if (ticket == null)
            return NotFound($"Ticket with ID {id} not found.");

        return Ok(MapToDto(ticket));
    }

    [HttpPost("search")]
    public async Task<ActionResult<List<TicketSearchResultDto>>> SearchTicketsSending([FromBody] TicketSendingSearchRequestDto request)
    {
        var tickets = await _ticketSendingRepo.SearchAsync(
            searchTerm: request.SearchTerm,
            companyId: request.CompanyId,
            siteId: request.SiteId,
            buyerId: request.BuyerId,
            firstName: request.FirstName,
            lastName: request.LastName,
            idNumber: request.IdNumber,
            accountNumber: request.AccountNumber,
            ticketType: request.TicketType,
            startDate: request.StartDate,
            endDate: request.EndDate,
            pageNumber: request.PageNumber,
            pageSize: request.PageSize
        );

        var results = new List<TicketSearchResultDto>();
        foreach (var ticket in tickets)
        {
            results.Add(await MapToSearchResultDtoAsync(ticket));
        }
        
        return Ok(results);
    }

    [HttpGet("last-ticket-number/{prefix}")]
    public async Task<IActionResult> GetLastTicketNumberByPrefix(string prefix)
    {
        var lastTicketNumber = await _ticketSendingRepo.GetLastTicketNumberByPrefixAsync(prefix);
        return Ok(new { ticketNumber = lastTicketNumber });
    }

    [HttpGet("next-ticket-number/{ticketTypeId}")]
    public async Task<IActionResult> GetNextTicketNumber(int ticketTypeId)
    {
        var nextNumber = await _ticketNumberService.GetNextSendingTicketNumberAsync(ticketTypeId);
        return Ok(new { ticketNumber = nextNumber });
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<TicketSendingDto>> CreateTicketSending([FromBody] CreateTicketSendingDto dto, CancellationToken ct = default)
    {
        var buyer = await _buyerRepo.GetByIdAsync(dto.BuyerId);
        if (buyer == null)
            return BadRequest($"Buyer with ID {dto.BuyerId} not found.");

        // Validate ticket type and weights (for sending, the logic is reversed)
        var weightValidation = WeightCalculationService.ValidateTicketWeights(
            ticketTypeId: dto.TicketTypeId,
            firstWeightKg: dto.FirstWeightKg,
            secondWeightKg: dto.SecondWeightKg,
            isReceiving: false
        );

        if (!weightValidation.IsValid)
            return BadRequest(weightValidation.ErrorMessage);

        // Generate ticket number
        var ticketNumber = await _ticketNumberService.GetNextSendingTicketNumberAsync(dto.TicketTypeId);

        // Calculate net weight for weighbridge tickets
        decimal netWeightKg = dto.NetWeightKg;
        if (WeightCalculationService.IsWeighbridgeTicket(dto.TicketTypeId) && dto.FirstWeightKg.HasValue && dto.SecondWeightKg.HasValue)
        {
            netWeightKg = WeightCalculationService.CalculateNetWeightFromScale(
                dto.FirstWeightKg.Value,
                dto.SecondWeightKg.Value,
                isReceiving: false
            );
        }

        // Get operator ID from authenticated user
        var operatorId = (int)User.GetOperatorId();

        var ticket = new TicketSending(
            buyerId: dto.BuyerId,
            ticketTypeId: dto.TicketTypeId,
            ticketNumber: ticketNumber,
            netWeightKg: netWeightKg,
            createdByOperatorId: operatorId,
            firstWeightKg: WeightCalculationService.IsWeighbridgeTicket(dto.TicketTypeId) ? dto.FirstWeightKg : null,
            secondWeightKg: WeightCalculationService.IsWeighbridgeTicket(dto.TicketTypeId) ? dto.SecondWeightKg : null,
            vehicleRegistration: WeightCalculationService.IsWeighbridgeTicket(dto.TicketTypeId) ? dto.VehicleRegistration : null,
            trailerRegistration: WeightCalculationService.IsWeighbridgeTicket(dto.TicketTypeId) ? dto.TrailerRegistration : null,
            driverName: WeightCalculationService.IsWeighbridgeTicket(dto.TicketTypeId) ? dto.DriverName : null,
            notes: dto.Notes
        );

        await _ticketSendingRepo.AddAsync(ticket);
        await _unitOfWork.SaveChangesAsync(ct);

        var result = await _ticketSendingRepo.GetByIdAsync(ticket.TicketSendingId);
        return CreatedAtAction(nameof(GetTicketSending), new { id = result!.TicketSendingId }, MapToDto(result));
    }

    [HttpPost("{id}/lines")]
    public async Task<ActionResult<TicketSendingDto>> AddLineItem(int id, [FromBody] CreateTicketSendingLineDto dto, CancellationToken ct = default)
    {
        var ticket = await _ticketSendingRepo.GetByIdAsync(id);
        if (ticket == null)
            return NotFound($"Ticket with ID {id} not found.");

        var product = await _productRepo.GetByIdAsync(dto.ProductId);
        if (product == null)
            return BadRequest($"Product with ID {dto.ProductId} not found.");

        // Get unit price from buyer's price code if not provided
        var unitPrice = dto.UnitPricePerKg;
        if (unitPrice == 0 && ticket.Buyer?.PriceCode != null)
        {
            unitPrice = await _priceLookupService.GetUnitPriceAsync(dto.ProductId, ticket.Buyer.PriceCode, ct);
            if (unitPrice == 0)
                return BadRequest($"Price not found for product {dto.ProductId} with price code {ticket.Buyer.PriceCode}");
        }

        // Get operator ID from authenticated user
        var operatorId = (int)User.GetOperatorId();

        var line = new TicketSendingLine(
            ticketSendingId: id,
            productId: dto.ProductId,
            netWeightKg: dto.NetWeightKg,
            unitPricePerKg: unitPrice,
            createdByOperatorId: operatorId,
            notes: dto.Notes
        );

        ticket.AddLine(line);
        await _ticketSendingRepo.UpdateAsync(ticket);
        await _unitOfWork.SaveChangesAsync(ct);

        var result = await _ticketSendingRepo.GetByIdAsync(id);
        return Ok(MapToDto(result!));
    }

    [HttpGet("{id}/count")]
    public async Task<ActionResult<long>> GetSearchCount([FromQuery] TicketSendingSearchRequestDto request)
    {
        var count = await _ticketSendingRepo.GetCountAsync(
            searchTerm: request.SearchTerm,
            buyerId: request.BuyerId,
            firstName: request.FirstName,
            lastName: request.LastName,
            idNumber: request.IdNumber,
            accountNumber: request.AccountNumber,
            startDate: request.StartDate,
            endDate: request.EndDate
        );

        return Ok(count);
    }

    private TicketSendingDto MapToDto(TicketSending ticket)
    {
        return new TicketSendingDto
        {
            TicketSendingId = ticket.TicketSendingId,
            BuyerId = ticket.BuyerId,
            BuyerName = $"{ticket.Buyer?.FirstName} {ticket.Buyer?.LastName}".Trim(),
            TicketNumber = ticket.TicketNumber,
            TicketTypeId = ticket.TicketTypeId,
            TicketTypeName = ticket.TicketType?.TicketTypeName ?? "",
            FirstWeightKg = ticket.FirstWeightKg,
            SecondWeightKg = ticket.SecondWeightKg,
            NetWeightKg = ticket.NetWeightKg,
            InvoiceNumber = ticket.InvoiceNumber,
            VehicleRegistration = ticket.VehicleRegistration,
            TrailerRegistration = ticket.TrailerRegistration,
            DriverName = ticket.DriverName,
            Notes = ticket.Notes,
            IsActive = ticket.IsActive,
            CreatedTime = ticket.CreatedTime,
            UpdatedTime = ticket.UpdatedTime,
            CreatedByOperatorId = ticket.CreatedByOperatorId,
            Lines = ticket.Lines?.Select(l => 
            {
                var lineTotal = l.NetWeightKg * l.UnitPricePerKg;
                var vatAmount = lineTotal * 0.15m;
                var totalInclVat = lineTotal + vatAmount;
                
                return new TicketSendingLineDto
                {
                    TicketSendingLineId = l.TicketSendingLineId,
                    TicketSendingId = l.TicketSendingId,
                    ProductId = l.ProductId,
                    ProductCode = l.Product?.ProductCode ?? "",
                    ProductName = l.Product?.ProductName ?? "",
                    NetWeightKg = l.NetWeightKg,
                    UnitPricePerKg = l.UnitPricePerKg,
                    LineTotal = lineTotal,
                    VatAmount = vatAmount,
                    TotalInclVat = totalInclVat,
                    Notes = l.Notes,
                    IsActive = l.IsActive,
                    CreatedTime = l.CreatedTime
                };
            }).ToList() ?? new List<TicketSendingLineDto>()
        };
    }

    private Task<TicketSearchResultDto> MapToSearchResultDtoAsync(TicketSending ticket)
    {
        var accountNumber = ticket.Buyer?.AccountNumber?.ToString("D8");
        
        var result = new TicketSearchResultDto
        {
            TicketId = ticket.TicketSendingId,
            TicketNumber = ticket.TicketNumber,
            TicketType = ticket.TicketType?.TicketTypeName ?? "Unknown",
            CustomerId = ticket.BuyerId,
            FirstName = ticket.Buyer?.FirstName,
            LastName = ticket.Buyer?.LastName,
            CompanyName = ticket.Buyer?.Company?.CompanyName,
            SiteName = ticket.Buyer?.Site?.SiteName,
            AccountNumber = accountNumber,
            NetWeightKg = ticket.NetWeightKg,
            CreatedTime = ticket.CreatedTime
        };

        return Task.FromResult(result);
    }
}
