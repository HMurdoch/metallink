using Microsoft.AspNetCore.Mvc;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using MetalLink.Shared.Tickets;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsSendingController : ControllerBase
{
    private readonly ITicketSendingRepository _ticketSendingRepo;
    private readonly IBuyerRepository _buyerRepo;
    private readonly IUnitOfWork _unitOfWork;

    public TicketsSendingController(
        ITicketSendingRepository ticketSendingRepo,
        IBuyerRepository buyerRepo,
        IUnitOfWork unitOfWork)
    {
        _ticketSendingRepo = ticketSendingRepo;
        _buyerRepo = buyerRepo;
        _unitOfWork = unitOfWork;
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
            buyerId: request.BuyerId,
            firstName: request.FirstName,
            lastName: request.LastName,
            idNumber: request.IdNumber,
            accountNumber: request.AccountNumber,
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

    [HttpPost]
    public async Task<ActionResult<TicketSendingDto>> CreateTicketSending([FromBody] CreateTicketSendingDto dto)
    {
        var buyer = await _buyerRepo.GetByIdAsync(dto.BuyerId);
        if (buyer == null)
            return BadRequest($"Buyer with ID {dto.BuyerId} not found.");

        var ticket = new TicketSending(
            buyerId: dto.BuyerId,
            ticketTypeId: dto.TicketTypeId,
            ticketNumber: dto.TicketNumber,
            netWeightKg: dto.NetWeightKg,
            createdByOperatorId: dto.CreatedByOperatorId,
            firstWeightKg: dto.FirstWeightKg,
            secondWeightKg: dto.SecondWeightKg,
            vehicleRegistration: dto.VehicleRegistration,
            trailerRegistration: dto.TrailerRegistration,
            driverName: dto.DriverName,
            notes: dto.Notes
        );

        await _ticketSendingRepo.AddAsync(ticket);
        await _unitOfWork.SaveChangesAsync();

        var result = await _ticketSendingRepo.GetByIdAsync(ticket.TicketSendingId);
        return Ok(MapToDto(result!));
    }

    [HttpPost("{id}/lines")]
    public async Task<ActionResult<TicketSendingDto>> AddLineItem(int id, [FromBody] CreateTicketSendingLineDto dto)
    {
        var ticket = await _ticketSendingRepo.GetByIdAsync(id);
        if (ticket == null)
            return NotFound($"Ticket with ID {id} not found.");

        var line = new TicketSendingLine(
            ticketSendingId: id,
            productId: dto.ProductId,
            netWeightKg: dto.NetWeightKg,
            unitPricePerKg: dto.UnitPricePerKg,
            createdByOperatorId: 1,
            notes: dto.Notes
        );

        ticket.AddLine(line);
        await _ticketSendingRepo.UpdateAsync(ticket);
        await _unitOfWork.SaveChangesAsync();

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
            Lines = ticket.Lines?.Select(l => new TicketSendingLineDto
            {
                TicketSendingLineId = l.TicketSendingLineId,
                TicketSendingId = l.TicketSendingId,
                ProductId = l.ProductId,
                ProductCode = l.Product?.ProductCode ?? "",
                ProductName = l.Product?.ProductName ?? "",
                NetWeightKg = l.NetWeightKg,
                UnitPricePerKg = l.UnitPricePerKg,
                Notes = l.Notes,
                IsActive = l.IsActive,
                CreatedTime = l.CreatedTime
            }).ToList() ?? new List<TicketSendingLineDto>()
        };
    }

    private async Task<TicketSearchResultDto> MapToSearchResultDtoAsync(TicketSending ticket)
    {
        var accountNumber = ticket.Buyer?.AccountNumber?.ToString("D8");
        
        return new TicketSearchResultDto
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
            Price = 0, // TODO: Calculate from line items
            TotalExclVat = 0, // TODO: Calculate from line items
            VatAmount = 0,
            TotalInclVat = 0,
            CreatedTime = ticket.CreatedTime
        };
    }
}
