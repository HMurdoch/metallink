using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using MetalLink.Shared.Tickets;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/tickets-receiving")]
[Authorize]
public class TicketsReceivingController : ControllerBase
{
    private readonly ITicketReceivingRepository _ticketReceivingRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly IProductRepository _productRepo;
    private readonly IUnitOfWork _unitOfWork;

    public TicketsReceivingController(
        ITicketReceivingRepository ticketReceivingRepo,
        ICustomerRepository customerRepo,
        IProductRepository productRepo,
        IUnitOfWork unitOfWork)
    {
        _ticketReceivingRepo = ticketReceivingRepo;
        _customerRepo = customerRepo;
        _productRepo = productRepo;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Create a new receiving ticket
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TicketReceivingDto>> CreateTicketReceiving([FromBody] CreateTicketReceivingDto dto)
    {
        // Validate customer exists
        var customer = await _customerRepo.GetByIdAsync(dto.CustomerId);
        if (customer == null)
            return BadRequest($"Customer with ID {dto.CustomerId} not found.");

        // Create the ticket entity
        var ticket = new TicketReceiving(
            companyId: dto.CompanyId,
            siteId: dto.SiteId,
            customerId: dto.CustomerId,
            ticketNumber: dto.TicketNumber,
            ticketType: dto.TicketType,
            netWeightKg: dto.NetWeightKg,
            unitPricePerKg: dto.UnitPricePerKg,
            currencyCode: dto.CurrencyCode,
            createdByOperatorId: dto.CreatedByOperatorId,
            productId: dto.ProductId,
            productDescription: dto.ProductDescription,
            firstWeightKg: dto.FirstWeightKg,
            secondWeightKg: dto.SecondWeightKg,
            vehicleRegistration: dto.VehicleRegistration,
            trailerRegistration: dto.TrailerRegistration,
            driverName: dto.DriverName,
            notes: dto.Notes
        );

        await _ticketReceivingRepo.AddAsync(ticket);
        await _unitOfWork.SaveChangesAsync();

        // Map to DTO
        var result = await _ticketReceivingRepo.GetByIdAsync(ticket.TicketReceivingId);
        return Ok(MapToDto(result!));
    }

    /// <summary>
    /// Get a receiving ticket by ID
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<TicketReceivingDto>> GetTicketReceivingById(long id)
    {
        var ticket = await _ticketReceivingRepo.GetByIdAsync(id);
        if (ticket == null)
            return NotFound($"Ticket receiving with ID {id} not found.");

        return Ok(MapToDto(ticket));
    }

    /// <summary>
    /// Search for receiving tickets
    /// </summary>
    [HttpPost("search")]
    public async Task<ActionResult<List<TicketReceivingDto>>> SearchTicketsReceiving([FromBody] TicketReceivingSearchRequestDto request)
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
            productId: request.ProductId,
            startDate: request.StartDate,
            endDate: request.EndDate,
            deliveryStatus: request.DeliveryStatus,
            pageNumber: request.PageNumber,
            pageSize: request.PageSize
        );

        var results = tickets.Select(MapToDto).ToList();
        return Ok(results);
    }

    /// <summary>
    /// Update a receiving ticket
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ActionResult<TicketReceivingDto>> UpdateTicketReceiving(long id, [FromBody] CreateTicketReceivingDto dto)
    {
        var ticket = await _ticketReceivingRepo.GetByIdAsync(id);
        if (ticket == null)
            return NotFound($"Ticket receiving with ID {id} not found.");

        ticket.UpdateWeights(dto.FirstWeightKg, dto.SecondWeightKg, dto.NetWeightKg);
        ticket.UpdatePrice(dto.UnitPricePerKg);

        await _unitOfWork.SaveChangesAsync();

        var updated = await _ticketReceivingRepo.GetByIdAsync(id);
        return Ok(MapToDto(updated!));
    }

    /// <summary>
    /// Delete a receiving ticket
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteTicketReceiving(long id)
    {
        var ticket = await _ticketReceivingRepo.GetByIdAsync(id);
        if (ticket == null)
            return NotFound($"Ticket receiving with ID {id} not found.");

        ticket.SoftDelete(DateTimeOffset.UtcNow);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Add lines to a platform receiving ticket
    /// </summary>
    [HttpPost("{ticketId:long}/lines")]
    public async Task<ActionResult<List<TicketReceivingLineDto>>> AddLines(long ticketId, [FromBody] List<AddTicketLineRequest> lines)
    {
        var ticket = await _ticketReceivingRepo.GetByIdAsync(ticketId);
        if (ticket == null)
            return NotFound($"Ticket receiving with ID {ticketId} not found.");

        foreach (var lineReq in lines)
        {
            var line = new TicketReceivingLine(
                ticketReceivingId: ticketId,
                productId: lineReq.ProductId,
                weightKg: lineReq.WeightKg,
                unitPricePerKg: lineReq.UnitPricePerKg ?? ticket.UnitPricePerKg,
                notes: lineReq.Notes
            );

            ticket.AddLine(line);
        }

        await _unitOfWork.SaveChangesAsync();

        var updated = await _ticketReceivingRepo.GetByIdAsync(ticketId);
        var resultLines = updated!.Lines.Select(l => new TicketReceivingLineDto
        {
            TicketReceivingLineId = l.TicketReceivingLineId,
            TicketReceivingId = l.TicketReceivingId,
            ProductId = l.ProductId,
            ProductCode = l.Product?.ProductCode ?? "",
            ProductName = l.Product?.ProductName ?? "",
            WeightKg = l.WeightKg,
            UnitPricePerKg = l.UnitPricePerKg,
            LineTotal = l.LineTotal,
            Notes = l.Notes,
            IsActive = l.IsActive,
            CreatedTime = l.CreatedTime
        }).ToList();

        return Ok(resultLines);
    }

    /// <summary>
    /// Update delivery status
    /// </summary>
    [HttpPut("{id:long}/status")]
    public async Task<IActionResult> UpdateDeliveryStatus(long id, [FromBody] UpdateDeliveryStatusRequest request)
    {
        var ticket = await _ticketReceivingRepo.GetByIdAsync(id);
        if (ticket == null)
            return NotFound($"Ticket receiving with ID {id} not found.");

        ticket.UpdateDeliveryStatus(request.DeliveryStatus);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    private TicketReceivingDto MapToDto(TicketReceiving ticket)
    {
        return new TicketReceivingDto
        {
            TicketReceivingId = ticket.TicketReceivingId,
            CompanyId = ticket.CompanyId,
            SiteId = ticket.SiteId,
            CustomerId = ticket.CustomerId,
            CustomerName = ticket.Customer?.FullName ?? "",
            TicketNumber = ticket.TicketNumber,
            TicketType = ticket.TicketType,
            FirstWeightKg = ticket.FirstWeightKg,
            SecondWeightKg = ticket.SecondWeightKg,
            NetWeightKg = ticket.NetWeightKg,
            UnitPricePerKg = ticket.UnitPricePerKg,
            TotalAmount = ticket.TotalAmount,
            CurrencyCode = ticket.CurrencyCode,
            ProductId = ticket.ProductId,
            ProductCode = ticket.Product?.ProductCode,
            ProductName = ticket.Product?.ProductName,
            ProductDescription = ticket.ProductDescription,
            VehicleRegistration = ticket.VehicleRegistration,
            TrailerRegistration = ticket.TrailerRegistration,
            DriverName = ticket.DriverName,
            OfmWeighbridgeTicket = ticket.OfmWeighbridgeTicket,
            ForeignTicket = ticket.ForeignTicket,
            CkNumber = ticket.CkNumber,
            DeliveryNumber = ticket.DeliveryNumber,
            RfidTag = ticket.RfidTag,
            RfidFirstScan = ticket.RfidFirstScan,
            RfidSecondScan = ticket.RfidSecondScan,
            DeliveryStatus = ticket.DeliveryStatus,
            Notes = ticket.Notes,
            PlatePhotoUrl = ticket.PlatePhotoUrl,
            LoadPhotoUrl = ticket.LoadPhotoUrl,
            Lines = ticket.Lines?.Select(l => new TicketReceivingLineDto
            {
                TicketReceivingLineId = l.TicketReceivingLineId,
                TicketReceivingId = l.TicketReceivingId,
                ProductId = l.ProductId,
                ProductCode = l.Product?.ProductCode ?? "",
                ProductName = l.Product?.ProductName ?? "",
                WeightKg = l.WeightKg,
                UnitPricePerKg = l.UnitPricePerKg,
                LineTotal = l.LineTotal,
                Notes = l.Notes,
                IsActive = l.IsActive,
                CreatedTime = l.CreatedTime
            }).ToList() ?? new List<TicketReceivingLineDto>(),
            IsActive = ticket.IsActive,
            CreatedTime = ticket.CreatedTime,
            UpdatedTime = ticket.UpdatedTime,
            CreatedByOperatorId = ticket.CreatedByOperatorId
        };
    }
}

public class AddTicketLineRequest
{
    public long ProductId { get; set; }
    public decimal WeightKg { get; set; }
    public decimal? UnitPricePerKg { get; set; }
    public string? Notes { get; set; }
}

public class UpdateDeliveryStatusRequest
{
    public string DeliveryStatus { get; set; } = string.Empty;
}
