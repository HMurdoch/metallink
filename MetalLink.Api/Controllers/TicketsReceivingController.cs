using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MetalLink.Application.Interfaces;
using MetalLink.Application.Services;
using MetalLink.Api.Extensions;
using MetalLink.Domain.Entities;
using MetalLink.Shared.Tickets.Receiving;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/tickets-receiving")]
public class TicketsReceivingController : ControllerBase
{
    private readonly ITicketReceivingRepository _ticketReceivingRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly IProductRepository _productRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStockLevelRepository _stockLevelRepo;
    private readonly IStockMovementRepository _stockMovementRepo;
    private readonly TicketNumberService _ticketNumberService;
    private readonly WeightCalculationService _weightCalculationService;
    private readonly PriceLookupService _priceLookupService;

    public TicketsReceivingController(
        ITicketReceivingRepository ticketReceivingRepo,
        ICustomerRepository customerRepo,
        IProductRepository productRepo,
        IUnitOfWork unitOfWork,
        IStockLevelRepository stockLevelRepo,
        IStockMovementRepository stockMovementRepo,
        TicketNumberService ticketNumberService,
        WeightCalculationService weightCalculationService,
        PriceLookupService priceLookupService)
    {
        _ticketReceivingRepo = ticketReceivingRepo;
        _customerRepo = customerRepo;
        _productRepo = productRepo;
        _unitOfWork = unitOfWork;
        _stockLevelRepo = stockLevelRepo;
        _stockMovementRepo = stockMovementRepo;
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
            var firstLine = ticket.Lines?.FirstOrDefault();
            ticket.UpdateWeights(firstLine?.FirstWeightKg, firstLine?.SecondWeightKg, calculatedWeight);
            await _ticketReceivingRepo.UpdateAsync(ticket);
            await _unitOfWork.SaveChangesAsync();
        }

        return Ok(MapToDto(ticket));
    }

    [HttpPost("search")]
    public async Task<ActionResult<List<TicketReceivingSearchResultDto>>> SearchTicketsReceiving([FromBody] TicketReceivingSearchRequestDto request)
    {
        if (request.NewCustomerOnly)
        {
            var customers = await _customerRepo.SearchCustomersWithZeroReceivingTicketsAsync(
                companyId: request.CompanyId,
                siteId: request.SiteId,
                customerId: request.CustomerId,
                firstName: request.FirstName,
                lastName: request.LastName,
                idNumber: request.IdNumber,
                accountNumber: request.AccountNumber);

            var newCustomerResults = customers.Select(c => new TicketReceivingSearchResultDto
            {
                TicketId = 0,
                TicketNumber = string.Empty,
                TicketType = string.Empty,
                TicketTypeId = 0,
                CustomerId = c.CustomerId,
                FirstName = c.FirstName,
                LastName = c.LastName,
                CompanyName = c.Company?.CompanyName,
                SiteName = c.Site?.SiteName,
                AccountNumber = c.AccountNumber.HasValue ? c.AccountNumber.Value.ToString("D8") : null,
                NetWeightKg = 0m,
                TicketStatus = '\0',
                CreatedTime = c.CreatedTime
            }).ToList();

            return Ok(newCustomerResults);
        }

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

        var results = new List<TicketReceivingSearchResultDto>();
        bool needsSave = false;
        
        foreach (var ticket in tickets)
        {
            // Ensure net weight is recalculated from line items
            var calculatedWeight = ticket.Lines?.Sum(l => l.NetWeightKg) ?? 0m;
            if (calculatedWeight != ticket.NetWeightKg && calculatedWeight > 0)
            {
                // Get the first line's weight values if available
                var firstLine = ticket.Lines?.FirstOrDefault();
                ticket.UpdateWeights(firstLine?.FirstWeightKg, firstLine?.SecondWeightKg, calculatedWeight);
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
        // Backwards-compatible route: historically used by the desktop.
        // WARNING: this *advances* the sequence (consumes a number).
        var ticketTypeId = prefix.ToUpperInvariant() switch
        {
            "RWB" => 1,
            "RPL" => 2,
            _ => throw new ArgumentException($"Invalid prefix: {prefix}")
        };

        var next = await _ticketNumberService.GetNextReceivingTicketNumberAsync(ticketTypeId);
        return Ok(new { ticketNumber = next });
    }

    [HttpGet("peek-next-ticket-number/{ticketTypeId}")]
    public async Task<IActionResult> PeekNextTicketNumber(int ticketTypeId)
    {
        var nextNumber = await _ticketNumberService.PeekNextReceivingTicketNumberAsync(ticketTypeId);
        return Ok(new { ticketNumber = nextNumber });
    }

    [HttpGet("peek-next-ticket-number-prefix/{prefix}")]
    public async Task<IActionResult> PeekNextTicketNumberByPrefix(string prefix)
    {
        var ticketTypeId = prefix.ToUpperInvariant() switch
        {
            "RWB" => 1,
            "RPL" => 2,
            _ => throw new ArgumentException($"Invalid prefix: {prefix}")
        };

        var nextNumber = await _ticketNumberService.PeekNextReceivingTicketNumberAsync(ticketTypeId);
        return Ok(new { ticketNumber = nextNumber });
    }

    // Returns the last *stored* ticket number in the table for a prefix (non-mutating).
    // Example: RWB-00000033
    [HttpGet("last-stored-ticket-number/{prefix}")]
    public async Task<IActionResult> GetLastStoredTicketNumberByPrefix(string prefix)
    {
        var last = await _ticketReceivingRepo.GetLastTicketNumberByPrefixAsync(prefix.ToUpperInvariant());
        return Ok(new { ticketNumber = last });
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
        
        // Set additional fields that should be saved
        ticket.OfmWeighbridgeTicket = dto.OfmWeighbridgeTicket;
        ticket.CkNumber = dto.CkNumber;
        ticket.DeliveryNumber = dto.DeliveryNumber;
        ticket.ForeignTicket = dto.ForeignTicket;

        // Set ticket state and initialize weight from DTO
        ticket.TicketState = dto.TicketState;
        ticket.InitializeWeightKg = dto.InitializeWeightKg;
        
        Console.WriteLine($"[API DEBUG] CreateTicketReceiving: Set ticket.TicketState='{ticket.TicketState}', ticket.InitializeWeightKg={ticket.InitializeWeightKg}");
        Console.WriteLine($"[API DEBUG] CreateTicketReceiving: Before save - TicketNumber={ticket.TicketNumber}, CustomerId={ticket.CustomerId}, TicketTypeId={ticket.TicketTypeId}");

        await _ticketReceivingRepo.AddAsync(ticket);
        await _unitOfWork.SaveChangesAsync(ct);

        var result = await _ticketReceivingRepo.GetByIdAsync(ticket.TicketReceivingId);
        return CreatedAtAction(nameof(GetTicketReceiving), new { id = result!.TicketReceivingId }, MapToDto(result));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TicketReceivingDto>> UpdateTicketReceiving(int id, [FromBody] CreateTicketReceivingDto dto, CancellationToken ct = default)
    {
        var ticket = await _ticketReceivingRepo.GetByIdAsync(id);
        if (ticket == null)
            return NotFound($"Ticket with ID {id} not found.");

        // Update basic fields
        // Ticket number is server-controlled; keep existing.
        ticket.UpdateHeader(dto.VehicleRegistration, dto.TrailerRegistration, dto.DriverName, dto.Notes);
        ticket.OfmWeighbridgeTicket = dto.OfmWeighbridgeTicket;
        ticket.CkNumber = dto.CkNumber;
        ticket.DeliveryNumber = dto.DeliveryNumber;
        ticket.ForeignTicket = dto.ForeignTicket;

        // State + weights
        ticket.TicketState = dto.TicketState;
        ticket.InitializeWeightKg = dto.InitializeWeightKg;
        ticket.UpdateWeights(null, null, dto.NetWeightKg);

        await _ticketReceivingRepo.UpdateAsync(ticket);
        await _unitOfWork.SaveChangesAsync(ct);

        var result = await _ticketReceivingRepo.GetByIdAsync(id);
        return Ok(MapToDto(result!));
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
        if (unitPrice == 0 && ticket.Customer?.ProductPriceListId != null)
        {
            unitPrice = await _priceLookupService.GetUnitPriceAsync(dto.ProductId, ticket.Customer.ProductPriceListId, null, ct);
            if (unitPrice == 0)
                return BadRequest($"Price not found for product {dto.ProductId} in Customer's Price List.");
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

        // Ticket state transitions and header net-weight tracking
        // - TicketState: 'H' if no active lines, 'M' if 1+ active lines
        // - NetWeightKg on ticket header: SUM of active line net weights
        var activeLinesAfterAdd = ticket.Lines?.Where(l => l.IsActive).ToList() ?? new List<TicketReceivingLine>();
        ticket.TicketState = activeLinesAfterAdd.Count == 0 ? 'H' : 'M';
        ticket.UpdateWeights(null, null, activeLinesAfterAdd.Sum(l => l.NetWeightKg));

        // Stock update + movement log (Purchase)
        var baseWeight = await _stockLevelRepo.GetOrCreateWeightKgAsync(dto.ProductId, operatorId, ct);
        await _stockMovementRepo.AddAsync(
            productId: dto.ProductId,
            baseWeightKg: baseWeight,
            buyWeightKg: line.NetWeightKg,
            sellWeightKg: 0m,
            createdByOperatorId: operatorId,
            notes: $"Purchase - KGs: {ticket.TicketNumber} | {line.NetWeightKg:0.00}",
            ct: ct);
        await _stockLevelRepo.UpdateWeightKgAsync(dto.ProductId, baseWeight + line.NetWeightKg, ct);

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
            
            Console.WriteLine($"[API DEBUG] Line mapping: ReceivingTicketLineId={l.ReceivingTicketLineId}, ProductId={l.ProductId}, ProductName={l.Product?.StarredProductAlias ?? l.Product?.IsriProductName}, FirstWeightKg={l.FirstWeightKg}, SecondWeightKg={l.SecondWeightKg}, NetWeightKg={l.NetWeightKg}, Notes='{l.Notes}'");
            
            return new TicketReceivingLineDto
            {
                ReceivingTicketLineId = l.ReceivingTicketLineId,
                ReceivingTicketId = l.ReceivingTicketId,
                ProductId = l.ProductId,
                ProductCode = l.Product?.IsriProductCode ?? "",
                ProductName = l.Product?.StarredProductAlias ?? l.Product?.IsriProductName ?? "",
                ProductGroupName = l.Product?.ProductGroup?.ProductGroupName,
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
            TicketState = ticket.TicketState,
            NetWeightKg = ticket.NetWeightKg,
            InvoiceNumber = ticket.InvoiceNumber,
            InitializeWeightKg = ticket.InitializeWeightKg,
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
    public async Task<ActionResult> CreateTicketHeaderAsync(int id, [FromBody] CreateHeaderRequest request, CancellationToken ct = default)
    {
        try
        {
            var ticket = await _ticketReceivingRepo.GetByIdAsync(id);
            if (ticket == null)
                return NotFound("Ticket not found");

            // Set the ticket state to 'H' (Header)
            // ticket.SetTicketStateToHeader(request.InitializeWeightKg);
            
            await _ticketReceivingRepo.UpdateAsync(ticket);
            await _unitOfWork.SaveChangesAsync(ct);

            return Ok(new { message = "Ticket header created successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("{id}/state")]
    public async Task<ActionResult> UpdateTicketStateAsync(int id, [FromBody] UpdateTicketStateRequest request, CancellationToken ct = default)
    {
        try
        {
            Console.WriteLine($"[DEBUG API] UpdateTicketStateAsync called: id={id}, newState={request.TicketState}");
            
            var ticket = await _ticketReceivingRepo.GetByIdAsync(id);
            if (ticket == null)
            {
                Console.WriteLine($"[DEBUG API] Ticket not found with id={id}");
                return NotFound("Ticket not found");
            }

            Console.WriteLine($"[DEBUG API] Found ticket with id={id}, current state={ticket.TicketState}");
            
            // Update the ticket state
            ticket.TicketState = request.TicketState;
            Console.WriteLine($"[DEBUG API] Updated ticket state to '{request.TicketState}'");
            
            await _ticketReceivingRepo.UpdateAsync(ticket);
            Console.WriteLine($"[DEBUG API] Ticket updated in repository");
            
            await _unitOfWork.SaveChangesAsync(ct);
            Console.WriteLine($"[DEBUG API] Changes saved successfully");

            return Ok(new { message = $"Ticket state updated to '{request.TicketState}'" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG API] Exception occurred: {ex.Message}");
            Console.WriteLine($"[DEBUG API] Stack trace: {ex.StackTrace}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{id}/lines/{lineId}")]
    public async Task<ActionResult> DeleteTicketReceivingLine(int id, int lineId, CancellationToken ct = default)
    {
        var ticket = await _ticketReceivingRepo.GetByIdAsync(id);
        if (ticket == null)
            return NotFound("Ticket not found");

        var line = ticket.Lines?.FirstOrDefault(l => l.ReceivingTicketLineId == lineId);
        if (line == null)
            return NotFound("Line item not found");

        // Soft delete line
        line.SoftDelete();

        // Stock update + movement log (Purchase Deleted)
        var operatorId = (int)User.GetOperatorId();

        var baseWeight = await _stockLevelRepo.GetOrCreateWeightKgAsync(line.ProductId, operatorId, ct);
        await _stockMovementRepo.AddAsync(
            productId: line.ProductId,
            baseWeightKg: baseWeight,
            buyWeightKg: 0m,
            sellWeightKg: line.NetWeightKg,
            createdByOperatorId: operatorId,
            notes: $"Purchase Deleted - KGs: {ticket.TicketNumber} | {line.NetWeightKg:0.00}",
            ct: ct);
        await _stockLevelRepo.UpdateWeightKgAsync(line.ProductId, baseWeight - line.NetWeightKg, ct);

        // Ticket state transitions and header net-weight tracking
        var activeLinesAfterDelete = ticket.Lines?.Where(l => l.IsActive).ToList() ?? new List<TicketReceivingLine>();
        ticket.TicketState = activeLinesAfterDelete.Count == 0 ? 'H' : 'M';
        ticket.UpdateWeights(null, null, activeLinesAfterDelete.Sum(l => l.NetWeightKg));

        await _ticketReceivingRepo.UpdateAsync(ticket);
        await _unitOfWork.SaveChangesAsync(ct);

        return Ok(new { message = "Line deleted (soft).", ticketState = ticket.TicketState });
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
            // TODO: Implement ticket state tracking
            // if (ticket.TicketState == 'F')
            //     return BadRequest("Cannot update tare on a finalized ticket.");

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

    private Task<TicketReceivingSearchResultDto> MapToSearchResultDtoAsync(TicketReceiving ticket)
    {
        var accountNumber = ticket.Customer?.AccountNumber?.ToString("D8");
        
        var result = new TicketReceivingSearchResultDto
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
            ProductGroupName = ticket.Lines?.FirstOrDefault()?.Product?.ProductGroup?.ProductGroupName,
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
