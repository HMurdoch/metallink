using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MetalLink.Application.Interfaces;
using MetalLink.Application.Services;
using MetalLink.Api.Extensions;
using MetalLink.Domain.Entities;
using MetalLink.Shared.Tickets.Sending;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/tickets-sending")]
public class TicketsSendingController : ControllerBase
{
    private readonly ITicketSendingRepository _ticketSendingRepo;
    private readonly IBuyerRepository _buyerRepo;
    private readonly IProductRepository _productRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStockLevelRepository _stockLevelRepo;
    private readonly IStockMovementRepository _stockMovementRepo;
    private readonly TicketNumberService _ticketNumberService;
    private readonly WeightCalculationService _weightCalculationService;
    private readonly PriceLookupService _priceLookupService;

    public TicketsSendingController(
        ITicketSendingRepository ticketSendingRepo,
        IBuyerRepository buyerRepo,
        IProductRepository productRepo,
        IUnitOfWork unitOfWork,
        IStockLevelRepository stockLevelRepo,
        IStockMovementRepository stockMovementRepo,
        TicketNumberService ticketNumberService,
        WeightCalculationService weightCalculationService,
        PriceLookupService priceLookupService)
    {
        _ticketSendingRepo = ticketSendingRepo;
        _buyerRepo = buyerRepo;
        _productRepo = productRepo;
        _unitOfWork = unitOfWork;
        _stockLevelRepo = stockLevelRepo;
        _stockMovementRepo = stockMovementRepo;
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
    public async Task<ActionResult<List<TicketSendingSearchResultDto>>> SearchTicketsSending([FromBody] TicketSendingSearchRequestDto request)

    {
        if (request.NewBuyerOnly)
        {
            var buyers = await _buyerRepo.SearchBuyersWithZeroSendingTicketsAsync(
                companyId: request.CompanyId,
                siteId: request.SiteId,
                buyerId: request.BuyerId,
                firstName: request.FirstName,
                lastName: request.LastName,
                idNumber: request.IdNumber,
                accountNumber: request.AccountNumber);

            var newBuyerResults = buyers.Select(b => new TicketSendingSearchResultDto
            {
                TicketId = 0,
                TicketNumber = string.Empty,
                TicketType = string.Empty,
                TicketTypeId = 0,
                BuyerId = b.BuyerId,
                FirstName = b.FirstName,
                LastName = b.LastName,
                CompanyName = b.Company?.CompanyName,
                SiteName = b.Site?.SiteName,
                AccountNumber = b.AccountNumber.HasValue ? b.AccountNumber.Value.ToString("D8") : null,
                NetWeightKg = 0m,
                TicketStatus = '\0',
                CreatedTime = b.CreatedTime
            }).ToList();

            return Ok(newBuyerResults);
        }

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

        var results = new List<TicketSendingSearchResultDto>();
        foreach (var ticket in tickets)
        {
            results.Add(await MapToSearchResultDtoAsync(ticket));
        }
        
        return Ok(results);
    }

    [HttpGet("last-ticket-number/{prefix}")]
    public async Task<IActionResult> GetLastTicketNumberByPrefix(string prefix)
    {
        // Backwards-compatible route.
        // WARNING: this *advances* the sequence (consumes a number).
        var ticketTypeId = prefix.ToUpperInvariant() switch
        {
            "SWB" => 1,
            "SPL" => 2,
            _ => throw new ArgumentException($"Invalid prefix: {prefix}")
        };

        var next = await _ticketNumberService.GetNextSendingTicketNumberAsync(ticketTypeId);
        return Ok(new { ticketNumber = next });
    }

    [HttpGet("peek-next-ticket-number/{ticketTypeId}")]
    public async Task<IActionResult> PeekNextTicketNumber(int ticketTypeId)
    {
        var nextNumber = await _ticketNumberService.PeekNextSendingTicketNumberAsync(ticketTypeId);
        return Ok(new { ticketNumber = nextNumber });
    }

    [HttpGet("peek-next-ticket-number-prefix/{prefix}")]
    public async Task<IActionResult> PeekNextTicketNumberByPrefix(string prefix)
    {
        var ticketTypeId = prefix.ToUpperInvariant() switch
        {
            "SWB" => 1,
            "SPL" => 2,
            _ => throw new ArgumentException($"Invalid prefix: {prefix}")
        };

        var nextNumber = await _ticketNumberService.PeekNextSendingTicketNumberAsync(ticketTypeId);
        return Ok(new { ticketNumber = nextNumber });
    }

    // Returns the last *stored* ticket number in the table for a prefix (non-mutating).
    // Example: SWB-00000033
    [HttpGet("last-stored-ticket-number/{prefix}")]
    public async Task<IActionResult> GetLastStoredTicketNumberByPrefix(string prefix)
    {
        var last = await _ticketSendingRepo.GetLastTicketNumberByPrefixAsync(prefix.ToUpperInvariant());
        return Ok(new { ticketNumber = last });
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
        Console.WriteLine($"[API DEBUG] CreateTicketSending: TicketTypeId={dto.TicketTypeId}, TicketState='{dto.TicketState}', InitializeWeightKg={dto.InitializeWeightKg}, FirstWeightKg={dto.FirstWeightKg}, SecondWeightKg={dto.SecondWeightKg}, NetWeightKg={dto.NetWeightKg}");
        var buyer = await _buyerRepo.GetByIdAsync(dto.BuyerId);
        if (buyer == null)
            return BadRequest($"Buyer with ID {dto.BuyerId} not found.");

        // Validate ticket type and weights.
        // IMPORTANT: for header-only creation (TicketState == 'H'), Weighbridge tickets only require FirstWeightKg.
        // SecondWeightKg is captured later when adding lines / completing.
        var isWeighbridge = WeightCalculationService.IsWeighbridgeTicket(dto.TicketTypeId);

        // Header-only weighbridge tickets: allow creating with ONLY initialize_weight_kg.
        // If the client explicitly requests TicketState == 'H', do NOT require second weight.
        var isHeaderWeighbridge = isWeighbridge && (dto.TicketState == 'H' || dto.TicketState == default(char));

        if (isHeaderWeighbridge)
        {
            if (!dto.InitializeWeightKg.HasValue)
                return BadRequest("Weighbridge tickets require initialize_weight_kg when creating a header.");
        }
        else
        {
            var weightValidation = WeightCalculationService.ValidateTicketWeights(
                ticketTypeId: dto.TicketTypeId,
                firstWeightKg: dto.FirstWeightKg,
                secondWeightKg: dto.SecondWeightKg,
                isReceiving: false
            );

            if (!weightValidation.IsValid)
                return BadRequest(weightValidation.ErrorMessage);
        }

        // Generate ticket number
        // NOTE: ticket numbers are unique (even across soft-deleted rows). In case of race conditions or
        // mis-detected "last ticket" due to data drift, we retry on unique-constraint violations.
        string? ticketNumber = null;

        // Calculate net weight for weighbridge tickets
        // For header-only creation, NetWeightKg stays 0 (net weight is calculated per line as weights are captured)
        decimal netWeightKg = dto.NetWeightKg;
        if (WeightCalculationService.IsWeighbridgeTicket(dto.TicketTypeId) && dto.TicketState != 'H' && dto.FirstWeightKg.HasValue && dto.SecondWeightKg.HasValue)
        {
            netWeightKg = WeightCalculationService.CalculateNetWeightFromScale(
                dto.FirstWeightKg.Value,
                dto.SecondWeightKg.Value,
                isReceiving: false
            );
        }

        // Get operator ID from authenticated user
        var operatorId = (int)User.GetOperatorId();

        // Try create (with retry on unique constraint)
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            ticketNumber = await _ticketNumberService.GetNextSendingTicketNumberAsync(dto.TicketTypeId);

            var ticket = new TicketSending(
                buyerId: dto.BuyerId,
                ticketTypeId: dto.TicketTypeId,
                ticketNumber: ticketNumber,
                netWeightKg: netWeightKg,
                createdByOperatorId: operatorId,
                // For header create: dto.InitializeWeightKg should carry the initial weighbridge reading
                firstWeightKg: WeightCalculationService.IsWeighbridgeTicket(dto.TicketTypeId) ? dto.InitializeWeightKg : null,
                secondWeightKg: null,
                vehicleRegistration: WeightCalculationService.IsWeighbridgeTicket(dto.TicketTypeId) ? dto.VehicleRegistration : null,
                trailerRegistration: WeightCalculationService.IsWeighbridgeTicket(dto.TicketTypeId) ? dto.TrailerRegistration : null,
                driverName: WeightCalculationService.IsWeighbridgeTicket(dto.TicketTypeId) ? dto.DriverName : null,
                notes: dto.Notes
            );

            if (WeightCalculationService.IsWeighbridgeTicket(dto.TicketTypeId))
            {
                ticket.SetWeighbridgeReferences(dto.OfmWeighbridgeTicket, dto.CkNumber, dto.DeliveryNumber, dto.ForeignTicket);
            }

            try
            {
                await _ticketSendingRepo.AddAsync(ticket);
                await _unitOfWork.SaveChangesAsync(ct);

                var result = await _ticketSendingRepo.GetByIdAsync(ticket.TicketSendingId);
                return CreatedAtAction(nameof(GetTicketSending), new { id = result!.TicketSendingId }, MapToDto(result));
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
            {
                // Unique violation - retry with a new generated number
                if (attempt == 3)
                    return StatusCode(500, new { error = "Failed to generate unique ticket number after retries." });

                continue;
            }
        }

        return StatusCode(500, new { error = "Failed to create ticket." });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TicketSendingDto>> UpdateTicketSending(int id, [FromBody] CreateTicketSendingDto dto, CancellationToken ct = default)
    {
        var ticket = await _ticketSendingRepo.GetByIdAsync(id);
        if (ticket == null)
            return NotFound($"Ticket with ID {id} not found.");

        // Update editable header fields
        // (TicketNumber is not updated - server controls numbering.)
        // Do not allow changing BuyerId for an existing ticket.
        if (ticket.BuyerId != dto.BuyerId)
            return BadRequest("BuyerId cannot be changed once a ticket is created.");

        ticket.UpdateHeader(dto.VehicleRegistration, dto.TrailerRegistration, dto.DriverName, dto.Notes);

        if (WeightCalculationService.IsWeighbridgeTicket(ticket.TicketTypeId))
        {
            ticket.SetWeighbridgeReferences(dto.OfmWeighbridgeTicket, dto.CkNumber, dto.DeliveryNumber, dto.ForeignTicket);
        }

        // Allow updating InitializeWeightKg while in Header state (Save & Reset)
        if (ticket.TicketState == 'H')
        {
            ticket.UpdateWeights(dto.InitializeWeightKg, null, dto.NetWeightKg);
        }
        else
        {
            ticket.UpdateWeights(dto.FirstWeightKg, dto.SecondWeightKg, dto.NetWeightKg);
        }

        await _ticketSendingRepo.UpdateAsync(ticket);
        await _unitOfWork.SaveChangesAsync(ct);

        var result = await _ticketSendingRepo.GetByIdAsync(id);
        return Ok(MapToDto(result!));
    }

    [HttpGet("{id}/lines")]
    public async Task<ActionResult<List<TicketSendingLineDto>>> GetTicketSendingLines(int id, CancellationToken ct = default)
    {
        var ticket = await _ticketSendingRepo.GetByIdAsync(id);
        if (ticket == null)
            return NotFound($"Ticket with ID {id} not found.");

        var lines = ticket.Lines?
            .Where(l => l.IsActive)
            .Select(l =>
            {
                var lineTotal = l.NetWeightKg * l.UnitPricePerKg;
                var vatAmount = lineTotal * 0.15m;
                var totalInclVat = lineTotal + vatAmount;

                return new TicketSendingLineDto
                {
                    TicketSendingLineId = l.TicketSendingLineId,
                    TicketSendingId = l.TicketSendingId,
                    ProductId = l.ProductId,
                    ProductCode = l.Product?.IsriProductCode ?? "",
                    ProductName = l.Product?.StarredProductAlias ?? l.Product?.IsriProductName ?? "",
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
            })
            .ToList() ?? new List<TicketSendingLineDto>();

        return Ok(lines);
    }

    [HttpDelete("{id}/lines/{lineId}")]
    public async Task<ActionResult> DeleteTicketSendingLine(int id, int lineId, CancellationToken ct = default)
    {
        var ticket = await _ticketSendingRepo.GetByIdAsync(id);
        if (ticket == null)
            return NotFound("Ticket not found");

        var line = ticket.Lines?.FirstOrDefault(l => l.TicketSendingLineId == lineId);
        if (line == null)
            return NotFound("Line item not found");

        line.SoftDelete();

        // Stock update + movement log (Sale Deleted)
        var operatorId = (int)User.GetOperatorId();

        var baseWeight = await _stockLevelRepo.GetOrCreateWeightKgAsync(line.ProductId, line.ProductPriceListProductPriceId, operatorId, ct);

        await _stockMovementRepo.AddAsync(
            productId: line.ProductId,
            baseWeightKg: baseWeight,
            buyWeightKg: line.NetWeightKg,
            sellWeightKg: 0m,
            unitPricePerKg: line.UnitPricePerKg,
            createdByOperatorId: operatorId,
            notes: $"Sale Deleted - KGs: {ticket.TicketNumber} | {line.NetWeightKg:0.00}",
            productPriceListId: line.ProductPriceListId,
            productPriceListProductPriceId: line.ProductPriceListProductPriceId,
            sendingTicketId: ticket.TicketSendingId,
            sendingTicketLineId: line.TicketSendingLineId,
            ct: ct);

        await _stockLevelRepo.UpdateWeightKgAsync(line.ProductId, line.ProductPriceListProductPriceId, line.NetWeightKg, operatorId, ct);

        // Ticket state transitions and header net-weight tracking
        // If the last active line is deleted, revert ticket back to Header-only state.
        ticket.RevertToHeaderIfNoActiveLines();

        var activeLinesAfterDelete = ticket.Lines?.Where(l => l.IsActive).ToList() ?? new List<TicketSendingLine>();
        ticket.UpdateWeights(null, null, activeLinesAfterDelete.Sum(l => l.NetWeightKg));

        await _ticketSendingRepo.UpdateAsync(ticket);
        await _unitOfWork.SaveChangesAsync(ct);

        return Ok(new { message = "Line deleted (soft).", ticketState = ticket.TicketState });
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
        if (unitPrice == 0 && ticket.Buyer?.ProductPriceListId != null)
        {
            unitPrice = await _priceLookupService.GetUnitPriceAsync(dto.ProductId, ticket.Buyer.ProductPriceListId, null, ct);
            if (unitPrice == 0)
                return BadRequest($"Price not found for product {dto.ProductId} in Buyer's Price List.");
        }

        // Get operator ID from authenticated user
        var operatorId = (int)User.GetOperatorId();

        if (WeightCalculationService.IsWeighbridgeTicket(ticket.TicketTypeId))
        {
            if (!dto.SecondWeightKg.HasValue)
                return BadRequest("Weighbridge sending line requires second_weight_kg.");

            var fw = ticket.InitializeWeightKg ?? 0m;
            var sw = dto.SecondWeightKg.Value;

            var net = WeightCalculationService.CalculateNetWeightFromScale(fw, sw, isReceiving: false);

            var line = new TicketSendingLine(
                ticketSendingId: id,
                productId: dto.ProductId,
                netWeightKg: net,
                unitPricePerKg: unitPrice,
                createdByOperatorId: operatorId,
                tare: dto.Tare,
                notes: dto.Notes,
                firstWeightKg: fw,
                secondWeightKg: sw
            );
            line.ProductPriceListId = ticket.Buyer?.ProductPriceListId;
            if (ticket.Buyer?.ProductPriceListId is int priceListId)
            {
                line.ProductPriceListProductPriceId = await _priceLookupService.GetProductPriceListProductPriceIdAsync(dto.ProductId, priceListId, ct);
            }

            ticket.AddLine(line);

            // Stock update + movement log (Sale)
            var baseWeight = await _stockLevelRepo.GetOrCreateWeightKgAsync(dto.ProductId, line.ProductPriceListProductPriceId, operatorId, ct);

            await _stockMovementRepo.AddAsync(
                productId: dto.ProductId,
                baseWeightKg: baseWeight,
                buyWeightKg: 0m,
                sellWeightKg: line.NetWeightKg,
                unitPricePerKg: line.UnitPricePerKg,
                createdByOperatorId: operatorId,
                notes: $"Sale - KGs: {ticket.TicketNumber} | {line.NetWeightKg:0.00}",
                productPriceListId: line.ProductPriceListId,
                productPriceListProductPriceId: line.ProductPriceListProductPriceId,
                sendingTicketId: ticket.TicketSendingId,
                sendingTicketLineId: line.TicketSendingLineId == 0 ? null : line.TicketSendingLineId,
                ct: ct);

            await _stockLevelRepo.UpdateWeightKgAsync(dto.ProductId, line.ProductPriceListProductPriceId, -line.NetWeightKg, operatorId, ct);

            // Ticket state transitions and header net-weight tracking
            var activeLinesAfterAdd = ticket.Lines?.Where(l => l.IsActive).ToList() ?? new List<TicketSendingLine>();
            // Ticket.AddLine already transitions H -> M.
            // NetWeightKg on header = SUM of active line net weights.
            ticket.UpdateWeights(null, null, activeLinesAfterAdd.Sum(l => l.NetWeightKg));
        }
        else
        {
            var line = new TicketSendingLine(
                ticketSendingId: id,
                productId: dto.ProductId,
                netWeightKg: dto.NetWeightKg,
                unitPricePerKg: unitPrice,
                createdByOperatorId: operatorId,
                tare: dto.Tare,
                notes: dto.Notes
            );
            line.ProductPriceListId = ticket.Buyer?.ProductPriceListId;
            if (ticket.Buyer?.ProductPriceListId is int priceListId)
            {
                line.ProductPriceListProductPriceId = await _priceLookupService.GetProductPriceListProductPriceIdAsync(dto.ProductId, priceListId, ct);
            }

            ticket.AddLine(line);

            // Stock update + movement log (Sale)
            var baseWeight = await _stockLevelRepo.GetOrCreateWeightKgAsync(dto.ProductId, line.ProductPriceListProductPriceId, operatorId, ct);

            await _stockMovementRepo.AddAsync(
                productId: dto.ProductId,
                baseWeightKg: baseWeight,
                buyWeightKg: 0m,
                sellWeightKg: line.NetWeightKg,
                unitPricePerKg: line.UnitPricePerKg,
                createdByOperatorId: operatorId,
                notes: $"Sale - KGs: {ticket.TicketNumber} | {line.NetWeightKg:0.00}",
                productPriceListId: line.ProductPriceListId,
                productPriceListProductPriceId: line.ProductPriceListProductPriceId,
                sendingTicketId: ticket.TicketSendingId,
                sendingTicketLineId: line.TicketSendingLineId == 0 ? null : line.TicketSendingLineId,
                ct: ct);

            await _stockLevelRepo.UpdateWeightKgAsync(dto.ProductId, line.ProductPriceListProductPriceId, -line.NetWeightKg, operatorId, ct);
        }
        await _ticketSendingRepo.UpdateAsync(ticket);
        await _unitOfWork.SaveChangesAsync(ct);

        var result = await _ticketSendingRepo.GetByIdAsync(id);
        return Ok(MapToDto(result!));
    }

    [HttpPut("{id}/lines/{lineId}/tare")]
    public async Task<ActionResult> UpdateLineTare(int id, int lineId, [FromBody] SendingUpdateLineTareRequest request, CancellationToken ct = default)
    {
        try
        {
            var ticket = await _ticketSendingRepo.GetByIdAsync(id);
            if (ticket == null)
                return NotFound("Ticket not found");

            var line = ticket.Lines?.FirstOrDefault(l => l.TicketSendingLineId == lineId);
            if (line == null)
                return NotFound("Line item not found");

            line.UpdateTare(request.Tare);

            await _ticketSendingRepo.UpdateAsync(ticket);
            await _unitOfWork.SaveChangesAsync(ct);

            return Ok(new { message = $"Line tare updated to {request.Tare}" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    public class SendingUpdateLineTareRequest
    {
        public decimal Tare { get; set; }
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

    [HttpPut("{id}/state")]
    public async Task<ActionResult> UpdateTicketStateAsync(int id, [FromBody] UpdateTicketStateRequest request, CancellationToken ct = default)
    {
        try
        {
            var ticket = await _ticketSendingRepo.GetByIdAsync(id);
            if (ticket == null)
                return NotFound("Ticket not found");

            // Only allow completing tickets via this endpoint (match Receiving's finalize flow)
            if (request.TicketState != 'C')
                return BadRequest("Only transition to 'C' (Complete) is supported.");

            ticket.MarkComplete(DateTimeOffset.UtcNow);
            await _ticketSendingRepo.UpdateAsync(ticket);
            await _unitOfWork.SaveChangesAsync(ct);

            return Ok(new { message = $"Ticket state updated to '{request.TicketState}'" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    public class UpdateTicketStateRequest
    {
        public char TicketState { get; set; }
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
            TicketState = ticket.TicketState,
            InitializeWeightKg = ticket.InitializeWeightKg,
            NetWeightKg = ticket.NetWeightKg,
            OfmWeighbridgeTicket = ticket.OfmWeighbridgeTicket,
            ForeignTicket = ticket.ForeignTicket,
            CkNumber = ticket.CkNumber,
            DeliveryNumber = ticket.DeliveryNumber,
            InvoiceNumber = ticket.InvoiceNumber,
            VehicleRegistration = ticket.VehicleRegistration,
            TrailerRegistration = ticket.TrailerRegistration,
            DriverName = ticket.DriverName,
            Notes = ticket.Notes,
            IsActive = ticket.IsActive,
            CreatedTime = ticket.CreatedTime,
            UpdatedTime = ticket.UpdatedTime,
            CreatedByOperatorId = ticket.CreatedByOperatorId,
            Lines = ticket.Lines?.Where(l => l.IsActive).Select(l => 
            {
                var lineTotal = l.NetWeightKg * l.UnitPricePerKg;
                var vatAmount = lineTotal * 0.15m;
                var totalInclVat = lineTotal + vatAmount;
                
                return new TicketSendingLineDto
                {
                    TicketSendingLineId = l.TicketSendingLineId,
                    TicketSendingId = l.TicketSendingId,
                    ProductId = l.ProductId,
                    ProductCode = l.Product?.IsriProductCode ?? "",
                    ProductName = l.Product?.StarredProductAlias ?? l.Product?.IsriProductName ?? "",
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
            }).ToList() ?? new List<TicketSendingLineDto>()
        };
    }

    private Task<TicketSendingSearchResultDto> MapToSearchResultDtoAsync(TicketSending ticket)
    {
        var accountNumber = ticket.Buyer?.AccountNumber?.ToString("D8");
        
        var result = new TicketSendingSearchResultDto
        {
            TicketId = ticket.TicketSendingId,
            TicketNumber = ticket.TicketNumber,
            TicketType = ticket.TicketType?.TicketTypeName ?? "Unknown",
            TicketTypeId = ticket.TicketTypeId,
            BuyerId = ticket.BuyerId,
            FirstName = ticket.Buyer?.FirstName,
            LastName = ticket.Buyer?.LastName,
            CompanyName = ticket.Buyer?.Company?.CompanyName,
            SiteName = ticket.Buyer?.Site?.SiteName,
            AccountNumber = accountNumber,
            NetWeightKg = ticket.NetWeightKg,
            ProductGroupName = ticket.Lines?.FirstOrDefault()?.Product?.ProductGroup?.ProductGroupName,
            TicketStatus = ticket.TicketState,
            CreatedTime = ticket.CreatedTime
        };

        return Task.FromResult(result);
    }
}
