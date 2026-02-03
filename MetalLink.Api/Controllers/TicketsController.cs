using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MetalLink.Application.Interfaces;
using MetalLink.Shared.Tickets;
using System.Linq;

namespace MetalLink.Api.Controllers;

/// <summary>
/// Unified Tickets endpoint for legacy support.
/// Routes requests to appropriate TicketReceiving or TicketSending endpoints based on ticket type.
/// </summary>
[ApiController]
[Route("api/tickets")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly ITicketReceivingRepository _receivingRepo;
    private readonly ITicketSendingRepository _sendingRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly IUnitOfWork _unitOfWork;

    public TicketsController(
        ITicketReceivingRepository receivingRepo,
        ITicketSendingRepository sendingRepo,
        ICustomerRepository customerRepo,
        IUnitOfWork unitOfWork)
    {
        _receivingRepo = receivingRepo;
        _sendingRepo = sendingRepo;
        _customerRepo = customerRepo;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Search for customers that do NOT have any tickets created yet
    /// </summary>
    [HttpPost("search-new-customers")]
    public async Task<ActionResult<NewCustomerResultDto[]>> SearchNewCustomersWithoutTickets(
        [FromBody] TicketSearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Build search criteria using DTO
            var searchCriteria = new MetalLink.Shared.Customers.CustomerSearchRequestDto
            {
                CustomerId = request.CustomerId.HasValue ? (int)request.CustomerId.Value : null,
                FirstName = request.FirstName,
                LastName = request.LastName,
                IdNumber = request.IdNumber,
                AccountNumber = request.AccountNumber,
                SiteId = request.SiteId.HasValue ? (int)request.SiteId.Value : null
            };

            // Get customers matching the search criteria
            var allCustomers = await _customerRepo.SearchAsync(searchCriteria, cancellationToken);
            
            // Get all customers that have receiving tickets
            var allReceivingTickets = await _receivingRepo.SearchAsync(
                searchTerm: null,
                companyId: null,
                siteId: null,
                customerId: null,
                firstName: null,
                lastName: null,
                idNumber: null,
                accountNumber: null,
                ticketType: null,
                startDate: null,
                endDate: null
            );
            
            var customerIdsWithReceivingTickets = new HashSet<long>(
                allReceivingTickets
                    .GroupBy(t => t.CustomerId)
                    .Select(g => (long)g.Key)
            );
            
            // Filter: customers without tickets
            var customersWithoutTickets = allCustomers
                .Where(c => !customerIdsWithReceivingTickets.Contains(c.CustomerId))
                .ToList();
            
            // Map to DTOs
            var results = customersWithoutTickets
                .Select(c => new NewCustomerResultDto
                {
                    CustomerId = c.CustomerId,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    CompanyName = c.Company?.CompanyName,
                    SiteName = c.Site?.SiteName,
                    AccountNumber = c.AccountNumber?.ToString(),
                    CreatedTime = c.CreatedTime
                })
                .ToArray();
            
            return Ok(results);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Get ticket by ID (searches both receiving and sending tickets)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TicketDto>> GetTicket(long id)
    {
        try
        {
            // Try receiving ticket first
            var receivingTicket = await _receivingRepo.GetByIdAsync(id);
            if (receivingTicket != null)
            {
                // Ensure net weight is recalculated from line items
                var calculatedWeight = receivingTicket.Lines?.Sum(l => l.NetWeightKg) ?? 0m;
                if (calculatedWeight != receivingTicket.NetWeightKg && calculatedWeight > 0)
                {
                    receivingTicket.RecalculateNetWeightFromLines();
                    await _receivingRepo.UpdateAsync(receivingTicket);
                    await _unitOfWork.SaveChangesAsync();
                }
                
                return Ok(MapReceivingTicketToDto(receivingTicket));
            }

            // Try sending ticket
            var sendingTicket = await _sendingRepo.GetByIdAsync(id);
            if (sendingTicket != null)
            {
                return Ok(MapSendingTicketToDto(sendingTicket));
            }

            return NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private TicketDto MapReceivingTicketToDto(MetalLink.Domain.Entities.TicketReceiving ticket)
    {
        return new TicketDto
        {
            TicketId = ticket.TicketReceivingId,
            TicketNumber = ticket.TicketNumber,
            TicketType = "receiving",
            TicketTypeId = ticket.TicketTypeId,
            CustomerId = ticket.CustomerId,
            BuyerId = null,
            NetWeightKg = ticket.NetWeightKg,
            UnitPricePerKg = 0m, // Receiving tickets don't have unit price
            Notes = ticket.Notes,
            CreatedTime = ticket.CreatedTime,
            TicketState = ticket.TicketState,
            InitializeWeightKg = ticket.InitializeWeightKg,
            VehicleRegistration = ticket.VehicleRegistration,
            TrailerRegistration = ticket.TrailerRegistration,
            DriverName = ticket.DriverName,
            OfmWeighbridgeTicket = ticket.OfmWeighbridgeTicket,
            ForeignTicket = ticket.ForeignTicket,
            CkNumber = ticket.CkNumber,
            Lines = ticket.Lines?
                .Where(l => l.IsActive) // Only include active lines
                .Select(l => 
                {
                    decimal lineTotal = l.NetWeightKg * l.UnitPricePerKg;
                    decimal vatAmount = lineTotal * 0.15m; // 15% VAT
                    decimal totalInclVat = lineTotal + vatAmount;
                    
                    return new TicketLineDto
                    {
                        TicketLineId = l.ReceivingTicketLineId,
                        TicketId = ticket.TicketReceivingId,
                        ProductId = l.ProductId,
                        ProductName = l.Product?.ProductName ?? string.Empty,
                        FirstWeightKg = l.FirstWeightKg,
                        SecondWeightKg = l.SecondWeightKg,
                        WeightKg = l.NetWeightKg,
                        UnitPricePerKg = l.UnitPricePerKg,
                        LineTotal = lineTotal,
                        VatAmount = vatAmount,
                        TotalInclVat = totalInclVat,
                        Tare = l.Tare,
                        Notes = l.Notes
                    };
                }).ToList() ?? new List<TicketLineDto>()
        };
    }

    [HttpDelete("{ticketId}/lines/{ticketLineId}")]
    public async Task<ActionResult> DeleteTicketLine(long ticketId, long ticketLineId)
    {
        try
        {
            // Try receiving ticket first
            var receivingTicket = await _receivingRepo.GetByIdAsync(ticketId);
            if (receivingTicket != null)
            {
                var line = receivingTicket.Lines.FirstOrDefault(l => l.ReceivingTicketLineId == ticketLineId);
                if (line == null)
                    return NotFound($"Line item {ticketLineId} not found in ticket {ticketId}");

                // Soft delete - set is_active to false
                line.SoftDelete();

                // Recalculate ticket net weight (sum of active lines only)
                receivingTicket.RecalculateNetWeightFromLines();

                await _receivingRepo.UpdateAsync(receivingTicket);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { message = "Line item deleted successfully" });
            }

            // Try sending ticket
            var sendingTicket = await _sendingRepo.GetByIdAsync(ticketId);
            if (sendingTicket != null)
            {
                var line = sendingTicket.Lines.FirstOrDefault(l => l.TicketSendingLineId == ticketLineId);
                if (line == null)
                    return NotFound($"Line item {ticketLineId} not found in ticket {ticketId}");

                // Soft delete - set is_active to false
                line.SoftDelete();

                // Recalculate ticket net weight (sum of active lines only)
                sendingTicket.RecalculateNetWeightFromLines();

                await _sendingRepo.UpdateAsync(sendingTicket);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { message = "Line item deleted successfully" });
            }

            return NotFound($"Ticket {ticketId} not found");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private TicketDto MapSendingTicketToDto(MetalLink.Domain.Entities.TicketSending ticket)
    {
        return new TicketDto
        {
            TicketId = ticket.TicketSendingId,
            TicketNumber = ticket.TicketNumber,
            TicketType = "sending",
            TicketTypeId = ticket.TicketTypeId,
            CustomerId = 0,
            BuyerId = ticket.BuyerId,
            NetWeightKg = ticket.NetWeightKg,
            UnitPricePerKg = 0m, // Sending tickets don't have unit price
            Notes = ticket.Notes,
            CreatedTime = ticket.CreatedTime,
            Lines = ticket.Lines?.Select(l => new TicketLineDto
            {
                TicketLineId = l.TicketSendingLineId,
                TicketId = ticket.TicketSendingId,
                ProductId = l.ProductId,
                ProductName = l.Product?.ProductName ?? string.Empty,
                WeightKg = l.NetWeightKg,
                UnitPricePerKg = l.UnitPricePerKg,
                LineTotal = l.NetWeightKg * l.UnitPricePerKg
            }).ToList() ?? new List<TicketLineDto>()
        };
    }
}
