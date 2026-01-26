using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MetalLink.Application.Interfaces;
using MetalLink.Application.Services;
using MetalLink.Api.Extensions;
using MetalLink.Domain.Entities;
using MetalLink.Shared.Tickets;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/tickets-receiving")]
public class TicketsReceivingController : ControllerBase
{
    private readonly ITicketReceivingRepository _ticketReceivingRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly IProductRepository _productRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TicketNumberService _ticketNumberService;
    private readonly WeightCalculationService _weightCalculationService;
    private readonly PriceLookupService _priceLookupService;

    public TicketsReceivingController(
        ITicketReceivingRepository ticketReceivingRepo,
        ICustomerRepository customerRepo,
        IProductRepository productRepo,
        IUnitOfWork unitOfWork,
        TicketNumberService ticketNumberService,
        WeightCalculationService weightCalculationService,
        PriceLookupService priceLookupService)
    {
        _ticketReceivingRepo = ticketReceivingRepo;
        _customerRepo = customerRepo;
        _productRepo = productRepo;
        _unitOfWork = unitOfWork;
        _ticketNumberService = ticketNumberService;
        _weightCalculationService = weightCalculationService;
        _priceLookupService = priceLookupService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TicketReceivingDto>> GetTicketReceiving(int id)
    {
        var ticket = await _ticketReceivingRepo.GetByIdAsync(id);
        if (ticket == null)
            return NotFound($"Ticket with ID {id} not found.");

        return Ok(MapToDto(ticket));
    }

    [HttpPost("search")]
    public async Task<ActionResult<List<TicketSearchResultDto>>> SearchTicketsReceiving([FromBody] TicketReceivingSearchRequestDto request)
    {
        var tickets = await _ticketReceivingRepo.SearchAsync(
            searchTerm: request.SearchTerm,
            companyId: request.CompanyId,
            siteId: request.SiteId,
            customerId: request.CustomerId,
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

    [HttpGet("next-ticket-number/{ticketTypeId}")]
    public async Task<ActionResult<string>> GetNextTicketNumber(int ticketTypeId)
    {
        var nextNumber = await _ticketNumberService.GetNextReceivingTicketNumberAsync(ticketTypeId);
        return Ok(nextNumber);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<TicketReceivingDto>> CreateTicketReceiving([FromBody] CreateTicketReceivingDto dto, CancellationToken ct = default)
    {
        var customer = await _customerRepo.GetByIdAsync(dto.CustomerId);
        if (customer == null)
            return BadRequest($"Customer with ID {dto.CustomerId} not found.");

        // Validate ticket type and weights
        var weightValidation = WeightCalculationService.ValidateTicketWeights(
            ticketTypeId: dto.TicketTypeId,
            firstWeightKg: dto.FirstWeightKg,
            secondWeightKg: dto.SecondWeightKg,
            isReceiving: true
        );

        if (!weightValidation.IsValid)
            return BadRequest(weightValidation.ErrorMessage);

        // Generate ticket number
        var ticketNumber = await _ticketNumberService.GetNextReceivingTicketNumberAsync(dto.TicketTypeId);

        // Calculate net weight for weighbridge tickets
        decimal netWeightKg = dto.NetWeightKg;
        if (WeightCalculationService.IsWeighbridgeTicket(dto.TicketTypeId) && dto.FirstWeightKg.HasValue && dto.SecondWeightKg.HasValue)
        {
            netWeightKg = WeightCalculationService.CalculateNetWeightFromScale(
                dto.FirstWeightKg.Value,
                dto.SecondWeightKg.Value,
                isReceiving: true
            );
        }

        // Get operator ID from authenticated user
        var operatorId = (int)User.GetOperatorId();

        var ticket = new TicketReceiving(
            customerId: dto.CustomerId,
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

        await _ticketReceivingRepo.AddAsync(ticket);
        await _unitOfWork.SaveChangesAsync(ct);

        var result = await _ticketReceivingRepo.GetByIdAsync(ticket.TicketReceivingId);
        return CreatedAtAction(nameof(GetTicketReceiving), new { id = result!.TicketReceivingId }, MapToDto(result));
    }

    [HttpPost("{id}/lines")]
    public async Task<ActionResult<TicketReceivingDto>> AddLineItem(int id, [FromBody] CreateTicketReceivingLineDto dto, CancellationToken ct = default)
    {
        var ticket = await _ticketReceivingRepo.GetByIdAsync(id);
        if (ticket == null)
            return NotFound($"Ticket with ID {id} not found.");

        var product = await _productRepo.GetByIdAsync(dto.ProductId);
        if (product == null)
            return BadRequest($"Product with ID {dto.ProductId} not found.");

        // Get unit price from customer's price code if not provided
        var unitPrice = dto.UnitPricePerKg;
        if (unitPrice == 0 && ticket.Customer?.PriceCode != null)
        {
            unitPrice = await _priceLookupService.GetUnitPriceAsync(dto.ProductId, ticket.Customer.PriceCode, ct);
            if (unitPrice == 0)
                return BadRequest($"Price not found for product {dto.ProductId} with price code {ticket.Customer.PriceCode}");
        }

        // Get operator ID from authenticated user
        var operatorId = (int)User.GetOperatorId();

        var line = new TicketReceivingLine(
            receivingTicketId: id,
            productId: dto.ProductId,
            netWeightKg: dto.NetWeightKg,
            unitPricePerKg: unitPrice,
            createdByOperatorId: operatorId,
            notes: dto.Notes
        );

        ticket.AddLine(line);
        await _ticketReceivingRepo.UpdateAsync(ticket);
        await _unitOfWork.SaveChangesAsync(ct);

        var result = await _ticketReceivingRepo.GetByIdAsync(id);
        return Ok(MapToDto(result!));
    }

    [HttpGet("{id}/count")]
    public async Task<ActionResult<long>> GetSearchCount([FromQuery] TicketReceivingSearchRequestDto request)
    {
        var count = await _ticketReceivingRepo.GetCountAsync(
            searchTerm: request.SearchTerm,
            customerId: request.CustomerId,
            firstName: request.FirstName,
            lastName: request.LastName,
            idNumber: request.IdNumber,
            accountNumber: request.AccountNumber,
            startDate: request.StartDate,
            endDate: request.EndDate
        );

        return Ok(count);
    }

    private TicketReceivingDto MapToDto(TicketReceiving ticket)
    {
        return new TicketReceivingDto
        {
            TicketReceivingId = ticket.TicketReceivingId,
            CustomerId = ticket.CustomerId,
            CustomerName = ticket.Customer?.FullName ?? "",
            TicketNumber = ticket.TicketNumber,
            TicketTypeId = ticket.TicketTypeId,
            FirstWeightKg = ticket.FirstWeightKg,
            SecondWeightKg = ticket.SecondWeightKg,
            NetWeightKg = ticket.NetWeightKg,
            InvoiceNumber = ticket.InvoiceNumber,
            VehicleRegistration = ticket.VehicleRegistration,
            TrailerRegistration = ticket.TrailerRegistration,
            DriverName = ticket.DriverName,
            OfmWeighbridgeTicket = ticket.OfmWeighbridgeTicket,
            CkNumber = ticket.CkNumber,
            DeliveryNumber = ticket.DeliveryNumber,
            ForeignTicket = ticket.ForeignTicket,
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
                
                return new TicketReceivingLineDto
                {
                    ReceivingTicketLineId = l.ReceivingTicketLineId,
                    ReceivingTicketId = l.ReceivingTicketId,
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
            }).ToList() ?? new List<TicketReceivingLineDto>()
        };
    }

    private Task<TicketSearchResultDto> MapToSearchResultDtoAsync(TicketReceiving ticket)
    {
        var accountNumber = ticket.Customer?.AccountNumber?.ToString("D8");
        
        var result = new TicketSearchResultDto
        {
            TicketId = ticket.TicketReceivingId,
            TicketNumber = ticket.TicketNumber,
            TicketType = ticket.TicketType?.TicketTypeName ?? "Unknown",
            CustomerId = ticket.CustomerId,
            FirstName = ticket.Customer?.FirstName,
            LastName = ticket.Customer?.LastName,
            CompanyName = ticket.Customer?.Company?.CompanyName,
            SiteName = ticket.Customer?.Site?.SiteName,
            AccountNumber = accountNumber,
            NetWeightKg = ticket.NetWeightKg,
            CreatedTime = ticket.CreatedTime
        };

        return Task.FromResult(result);
    }
}
