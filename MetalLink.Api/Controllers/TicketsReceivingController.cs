using Microsoft.AspNetCore.Mvc;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using MetalLink.Shared.Tickets;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsReceivingController : ControllerBase
{
    private readonly ITicketReceivingRepository _ticketReceivingRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly IUnitOfWork _unitOfWork;

    public TicketsReceivingController(
        ITicketReceivingRepository ticketReceivingRepo,
        ICustomerRepository customerRepo,
        IUnitOfWork unitOfWork)
    {
        _ticketReceivingRepo = ticketReceivingRepo;
        _customerRepo = customerRepo;
        _unitOfWork = unitOfWork;
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
            customerId: request.CustomerId,
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
    public async Task<ActionResult<TicketReceivingDto>> CreateTicketReceiving([FromBody] CreateTicketReceivingDto dto)
    {
        var customer = await _customerRepo.GetByIdAsync(dto.CustomerId);
        if (customer == null)
            return BadRequest($"Customer with ID {dto.CustomerId} not found.");

        var ticket = new TicketReceiving(
            customerId: dto.CustomerId,
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

        await _ticketReceivingRepo.AddAsync(ticket);
        await _unitOfWork.SaveChangesAsync();

        var result = await _ticketReceivingRepo.GetByIdAsync(ticket.TicketReceivingId);
        return Ok(MapToDto(result!));
    }

    [HttpPost("{id}/lines")]
    public async Task<ActionResult<TicketReceivingDto>> AddLineItem(int id, [FromBody] CreateTicketReceivingLineDto dto)
    {
        var ticket = await _ticketReceivingRepo.GetByIdAsync(id);
        if (ticket == null)
            return NotFound($"Ticket with ID {id} not found.");

        var line = new TicketReceivingLine(
            receivingTicketId: id,
            productId: dto.ProductId,
            netWeightKg: dto.NetWeightKg,
            unitPricePerKg: dto.UnitPricePerKg,
            createdByOperatorId: 1,
            notes: dto.Notes
        );

        ticket.AddLine(line);
        await _ticketReceivingRepo.UpdateAsync(ticket);
        await _unitOfWork.SaveChangesAsync();

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
            Lines = ticket.Lines?.Select(l => new TicketReceivingLineDto
            {
                ReceivingTicketLineId = l.ReceivingTicketLineId,
                ReceivingTicketId = l.ReceivingTicketId,
                ProductId = l.ProductId,
                ProductCode = l.Product?.ProductCode ?? "",
                ProductName = l.Product?.ProductName ?? "",
                NetWeightKg = l.NetWeightKg,
                UnitPricePerKg = l.UnitPricePerKg,
                Notes = l.Notes,
                IsActive = l.IsActive,
                CreatedTime = l.CreatedTime
            }).ToList() ?? new List<TicketReceivingLineDto>()
        };
    }

    private async Task<TicketSearchResultDto> MapToSearchResultDtoAsync(TicketReceiving ticket)
    {
        var accountNumber = ticket.Customer?.AccountNumber?.ToString("D8");
        
        return new TicketSearchResultDto
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
            Price = 0, // TODO: Calculate from line items
            TotalExclVat = 0, // TODO: Calculate from line items
            VatAmount = 0,
            TotalInclVat = 0,
            CreatedTime = ticket.CreatedTime
        };
    }
}
