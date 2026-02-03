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

        // Ensure net weight is recalculated from line items
        // This handles both new tickets and legacy tickets that may have 0.00 weight
        var calculatedWeight = ticket.Lines?.Sum(l => l.NetWeightKg) ?? 0m;
        if (calculatedWeight != ticket.NetWeightKg && calculatedWeight > 0)
        {
            // Weight is inconsistent - recalculate and update
            ticket.RecalculateNetWeightFromLines();
            await _ticketReceivingRepo.UpdateAsync(ticket);
            await _unitOfWork.SaveChangesAsync();
        }

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
        bool needsSave = false;
        
        foreach (var ticket in tickets)
        {
            // Ensure net weight is recalculated from line items
            var calculatedWeight = ticket.Lines?.Sum(l => l.NetWeightKg) ?? 0m;
            if (calculatedWeight != ticket.NetWeightKg && calculatedWeight > 0)
            {
                ticket.RecalculateNetWeightFromLines();
                needsSave = true;
            }
            
            results.Add(await MapToSearchResultDtoAsync(ticket));
        }
        
        // Save all recalculated weights at once
        if (needsSave)
        {
            await _unitOfWork.SaveChangesAsync();
        }
        
        return Ok(results);
    }

    [HttpGet("last-ticket-number/{prefix}")]
    public async Task<IActionResult> GetLastTicketNumberByPrefix(string prefix)
    {
        var lastTicketNumber = await _ticketReceivingRepo.GetLastTicketNumberByPrefixAsync(prefix);
        return Ok(new { ticketNumber = lastTicketNumber });
    }

    [HttpGet("next-ticket-number/{ticketTypeId}")]
    public async Task<IActionResult> GetNextTicketNumber(int ticketTypeId)
    {
        var nextNumber = await _ticketNumberService.GetNextReceivingTicketNumberAsync(ticketTypeId);
        return Ok(new { ticketNumber = nextNumber });
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<TicketReceivingDto>> CreateTicketReceiving([FromBody] CreateTicketReceivingDto dto, CancellationToken ct = default)
    {
        var customer = await _customerRepo.GetByIdAsync(dto.CustomerId);
        if (customer == null)
            return BadRequest($"Customer with ID {dto.CustomerId} not found.");

        // Generate ticket number
        var ticketNumber = await _ticketNumberService.GetNextReceivingTicketNumberAsync(dto.TicketTypeId);

        // Net weight comes directly from dto
        decimal netWeightKg = dto.NetWeightKg;

        // Get operator ID from authenticated user
        var operatorId = (int)User.GetOperatorId();

        var ticket = new TicketReceiving(
            customerId: dto.CustomerId,
            ticketTypeId: dto.TicketTypeId,
            ticketNumber: ticketNumber,
            netWeightKg: netWeightKg,
            createdByOperatorId: operatorId,
            vehicleRegistration: WeightCalculationService.IsWeighbridgeTicket(dto.TicketTypeId) ? dto.VehicleRegistration : null,
            trailerRegistration: WeightCalculationService.IsWeighbridgeTicket(dto.TicketTypeId) ? dto.TrailerRegistration : null,
            driverName: WeightCalculationService.IsWeighbridgeTicket(dto.TicketTypeId) ? dto.DriverName : null,
            notes: dto.Notes
        );

        // Set the ticket state and initialize weight if provided
        if (dto.TicketState == 'H')
        {
            ticket.SetTicketStateToHeader(dto.InitializeWeightKg);
        }
        else if (dto.TicketState == 'M')
        {
            ticket.SetTicketStateToMultiWeight();
        }
        else if (dto.TicketState == 'C')
        {
            ticket.SetTicketStateToComplete();
        }

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

        Console.WriteLine($"[API ADD LINE] Creating line: ProductId={dto.ProductId}, FW={dto.FirstWeightKg}, SW={dto.SecondWeightKg}, NetWeight={dto.NetWeightKg}");
        
        var line = new TicketReceivingLine(
            receivingTicketId: id,
            productId: dto.ProductId,
            netWeightKg: dto.NetWeightKg,
            unitPricePerKg: unitPrice,
            createdByOperatorId: operatorId,
            notes: dto.Notes,
            firstWeightKg: dto.FirstWeightKg,
            secondWeightKg: dto.SecondWeightKg,
            tare: dto.Tare
        );
        
        Console.WriteLine($"[API ADD LINE] Line created: FW={line.FirstWeightKg}, SW={line.SecondWeightKg}, NetWeight={line.NetWeightKg}");

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
        Console.WriteLine($"[API DEBUG] MapToDto called for ticket {ticket.TicketNumber}, Lines.Count={ticket.Lines?.Count ?? 0}");
        
        var lines = ticket.Lines?.Select(l => 
        {
            var lineTotal = l.NetWeightKg * l.UnitPricePerKg;
            var vatAmount = lineTotal * 0.15m;
            var totalInclVat = lineTotal + vatAmount;
            
            Console.WriteLine($"[API DEBUG] Line mapping: ReceivingTicketLineId={l.ReceivingTicketLineId}, ProductId={l.ProductId}, ProductName={l.Product?.ProductName}, FirstWeightKg={l.FirstWeightKg}, SecondWeightKg={l.SecondWeightKg}, NetWeightKg={l.NetWeightKg}, Notes='{l.Notes}'");
            
            return new TicketReceivingLineDto
            {
                ReceivingTicketLineId = l.ReceivingTicketLineId,
                ReceivingTicketId = l.ReceivingTicketId,
                ProductId = l.ProductId,
                ProductCode = l.Product?.ProductCode ?? "",
                ProductName = l.Product?.ProductName ?? "",
                FirstWeightKg = l.FirstWeightKg,
                SecondWeightKg = l.SecondWeightKg,
                NetWeightKg = l.NetWeightKg,
                UnitPricePerKg = l.UnitPricePerKg,
                LineTotal = lineTotal,
                VatAmount = vatAmount,
                TotalInclVat = totalInclVat,
                Tare = l.Tare,
                Notes = l.Notes,
                IsActive = l.IsActive,
                CreatedTime = l.CreatedTime
            };
        }).ToList() ?? new List<TicketReceivingLineDto>();
        
        return new TicketReceivingDto
        {
            TicketReceivingId = ticket.TicketReceivingId,
            CustomerId = ticket.CustomerId,
            CustomerName = ticket.Customer?.FullName ?? "",
            TicketNumber = ticket.TicketNumber,
            TicketTypeId = ticket.TicketTypeId,
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
            Lines = lines
        };
    }

    [HttpPut("{id}/create-header")]
    public async Task<ActionResult> CreateTicketHeaderAsync(long id, [FromBody] CreateHeaderRequest request, CancellationToken ct = default)
    {
        try
        {
            var ticket = await _ticketReceivingRepo.GetByIdAsync(id);
            if (ticket == null)
                return NotFound("Ticket not found");

            // Set the ticket state to 'H' (Header)
            ticket.SetTicketStateToHeader(request.InitializeWeightKg);
            
            await _ticketReceivingRepo.UpdateAsync(ticket);
            await _unitOfWork.SaveChangesAsync(ct);

            return Ok(new { message = "Ticket header created successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("{id}/ticket-state")]
    public async Task<ActionResult> UpdateTicketStateAsync(long id, [FromBody] UpdateTicketStateRequest request, CancellationToken ct = default)
    {
        try
        {
            var ticket = await _ticketReceivingRepo.GetByIdAsync(id);
            if (ticket == null)
                return NotFound("Ticket not found");

            // Update the ticket state directly
            if (request.TicketState == 'M')
            {
                ticket.SetTicketStateToMultiWeight();
            }
            else if (request.TicketState == 'H')
            {
                ticket.SetTicketStateToHeader();
            }
            else if (request.TicketState == 'C')
            {
                ticket.SetTicketStateToComplete();
            }
            
            await _ticketReceivingRepo.UpdateAsync(ticket);
            await _unitOfWork.SaveChangesAsync(ct);

            return Ok(new { message = $"Ticket state updated to '{request.TicketState}'" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("{id}/lines/{lineId}/tare")]
    public async Task<ActionResult> UpdateLineTare(int id, int lineId, [FromBody] UpdateLineTareRequest request, CancellationToken ct = default)
    {
        try
        {
            var ticket = await _ticketReceivingRepo.GetByIdAsync(id);
            if (ticket == null)
                return NotFound($"Ticket with ID {id} not found.");

            var line = ticket.Lines.FirstOrDefault(l => l.ReceivingTicketLineId == lineId);
            if (line == null)
                return NotFound($"Line with ID {lineId} not found in ticket {id}.");

            // Prevent tare changes on finalized tickets
            if (ticket.TicketState == 'F')
                return BadRequest("Cannot update tare on a finalized ticket.");

            line.UpdateTare(request.Tare);
            await _ticketReceivingRepo.UpdateAsync(ticket);
            await _unitOfWork.SaveChangesAsync(ct);

            return Ok(new { message = "Tare updated successfully", tare = request.Tare });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private Task<TicketSearchResultDto> MapToSearchResultDtoAsync(TicketReceiving ticket)
    {
        var accountNumber = ticket.Customer?.AccountNumber?.ToString("D8");
        
        var result = new TicketSearchResultDto
        {
            TicketId = ticket.TicketReceivingId,
            TicketNumber = ticket.TicketNumber,
            TicketType = ticket.TicketType?.TicketTypeName ?? "Unknown",
            TicketTypeId = ticket.TicketTypeId,
            CustomerId = ticket.CustomerId,
            FirstName = ticket.Customer?.FirstName,
            LastName = ticket.Customer?.LastName,
            CompanyName = ticket.Customer?.Company?.CompanyName,
            SiteName = ticket.Customer?.Site?.SiteName,
            AccountNumber = accountNumber,
            NetWeightKg = ticket.NetWeightKg,
            TicketStatus = ticket.TicketState,
            CreatedTime = ticket.CreatedTime
        };

        return Task.FromResult(result);
    }
}

public class UpdateLineTareRequest
{
    public decimal Tare { get; set; }
}

public class CreateHeaderRequest
{
    public int TicketTypeId { get; set; }
    public decimal? InitializeWeightKg { get; set; }
}

public class UpdateTicketStateRequest
{
    public char TicketState { get; set; }
}
