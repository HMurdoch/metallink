using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using MetalLink.Shared.Tickets;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/tickets-sending")]
[Authorize]
public class TicketsSendingController : ControllerBase
{
    private readonly ITicketSendingRepository _ticketSendingRepo;
    private readonly IBuyerRepository _buyerRepo;
    private readonly IProductRepository _productRepo;
    private readonly IUnitOfWork _unitOfWork;

    public TicketsSendingController(
        ITicketSendingRepository ticketSendingRepo,
        IBuyerRepository buyerRepo,
        IProductRepository productRepo,
        IUnitOfWork unitOfWork)
    {
        _ticketSendingRepo = ticketSendingRepo;
        _buyerRepo = buyerRepo;
        _productRepo = productRepo;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Create a new sending ticket
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TicketSendingDto>> CreateTicketSending([FromBody] CreateTicketSendingDto dto)
    {
        // Validate buyer exists
        var buyer = await _buyerRepo.GetByIdAsync(dto.BuyerId);
        if (buyer == null)
            return BadRequest($"Buyer with ID {dto.BuyerId} not found.");

        // Create the ticket entity
        var ticket = new TicketSending(
            companyId: dto.CompanyId,
            siteId: dto.SiteId,
            buyerId: dto.BuyerId,
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

        await _ticketSendingRepo.AddAsync(ticket);
        await _unitOfWork.SaveChangesAsync();

        // Map to DTO
        var result = await _ticketSendingRepo.GetByIdAsync(ticket.TicketSendingId);
        return Ok(MapToDto(result!));
    }

    /// <summary>
    /// Get a sending ticket by ID
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<TicketSendingDto>> GetTicketSendingById(long id)
    {
        var ticket = await _ticketSendingRepo.GetByIdAsync(id);
        if (ticket == null)
            return NotFound($"Ticket sending with ID {id} not found.");

        return Ok(MapToDto(ticket));
    }

    /// <summary>
    /// Search for sending tickets
    /// </summary>
    [HttpPost("search")]
    public async Task<ActionResult<List<TicketSendingDto>>> SearchTicketsSending([FromBody] TicketSendingSearchRequestDto request)
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
    /// Update a sending ticket
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ActionResult<TicketSendingDto>> UpdateTicketSending(long id, [FromBody] CreateTicketSendingDto dto)
    {
        var ticket = await _ticketSendingRepo.GetByIdAsync(id);
        if (ticket == null)
            return NotFound($"Ticket sending with ID {id} not found.");

        ticket.UpdateWeights(dto.FirstWeightKg, dto.SecondWeightKg, dto.NetWeightKg);
        ticket.UpdatePrice(dto.UnitPricePerKg);

        await _unitOfWork.SaveChangesAsync();

        var updated = await _ticketSendingRepo.GetByIdAsync(id);
        return Ok(MapToDto(updated!));
    }

    /// <summary>
    /// Delete a sending ticket
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteTicketSending(long id)
    {
        var ticket = await _ticketSendingRepo.GetByIdAsync(id);
        if (ticket == null)
            return NotFound($"Ticket sending with ID {id} not found.");

        ticket.SoftDelete(DateTimeOffset.UtcNow);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Add lines to a platform sending ticket
    /// </summary>
    [HttpPost("{ticketId:long}/lines")]
    public async Task<ActionResult<List<TicketSendingLineDto>>> AddLines(long ticketId, [FromBody] List<AddTicketLineRequest> lines)
    {
        var ticket = await _ticketSendingRepo.GetByIdAsync(ticketId);
        if (ticket == null)
            return NotFound($"Ticket sending with ID {ticketId} not found.");

        foreach (var lineReq in lines)
        {
            var line = new TicketSendingLine(
                ticketSendingId: ticketId,
                productId: lineReq.ProductId,
                weightKg: lineReq.WeightKg,
                unitPricePerKg: lineReq.UnitPricePerKg ?? ticket.UnitPricePerKg,
                notes: lineReq.Notes
            );

            ticket.AddLine(line);
        }

        await _unitOfWork.SaveChangesAsync();

        var updated = await _ticketSendingRepo.GetByIdAsync(ticketId);
        var resultLines = updated!.Lines.Select(l => new TicketSendingLineDto
        {
            TicketSendingLineId = l.TicketSendingLineId,
            TicketSendingId = l.TicketSendingId,
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
        var ticket = await _ticketSendingRepo.GetByIdAsync(id);
        if (ticket == null)
            return NotFound($"Ticket sending with ID {id} not found.");

        ticket.UpdateDeliveryStatus(request.DeliveryStatus);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    private TicketSendingDto MapToDto(TicketSending ticket)
    {
        return new TicketSendingDto
        {
            TicketSendingId = ticket.TicketSendingId,
            CompanyId = ticket.CompanyId,
            SiteId = ticket.SiteId,
            BuyerId = ticket.BuyerId,
            BuyerName = ticket.Buyer?.BuyerName ?? "",
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
            Lines = ticket.Lines?.Select(l => new TicketSendingLineDto
            {
                TicketSendingLineId = l.TicketSendingLineId,
                TicketSendingId = l.TicketSendingId,
                ProductId = l.ProductId,
                ProductCode = l.Product?.ProductCode ?? "",
                ProductName = l.Product?.ProductName ?? "",
                WeightKg = l.WeightKg,
                UnitPricePerKg = l.UnitPricePerKg,
                LineTotal = l.LineTotal,
                Notes = l.Notes,
                IsActive = l.IsActive,
                CreatedTime = l.CreatedTime
            }).ToList() ?? new List<TicketSendingLineDto>(),
            IsActive = ticket.IsActive,
            CreatedTime = ticket.CreatedTime,
            UpdatedTime = ticket.UpdatedTime,
            CreatedByOperatorId = ticket.CreatedByOperatorId
        };
    }
}

