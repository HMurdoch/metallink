using System;
using System.Threading;
using System.Threading.Tasks;
using MetalLink.Api.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using MetalLink.Infrastructure.Persistence;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize] // use same auth setup as other controllers
public class TicketReportsController : ControllerBase
{
    private readonly MetalLinkDbContext _dbContext;

    public TicketReportsController(MetalLinkDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("{ticketId:long}/report")]
    public async Task<IActionResult> GetTicketReport(long ticketId, CancellationToken cancellationToken)
    {
        var ticket = await _dbContext.Tickets
            // navigation removed for now: .Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.TicketId == ticketId, cancellationToken);

        if (ticket == null)
            return NotFound();

        // TODO: If you have a Customers table/entity, we can hydrate real customer details here.
        var model = new TicketReportModel
        {
            TicketId = ticket.TicketId,
            TicketNumber = ticket.TicketNumber,
            TicketType = ticket.TicketType,
            CreatedTime = Convert.ToDateTime(ticket.CreatedTime),

            SiteId = ticket.SiteId,
            SiteName = "Metal Link Site",   // TODO: map real SiteName if you have a Site table

            CustomerId = ticket.CustomerId ?? 0,
            CustomerName = "Unknown",          // TODO: map from related customer entity
            CustomerAccountNumber = null,      // TODO
            CustomerPriceCode = null,          // TODO

            FirstWeightKg = ticket.FirstWeightKg,
            SecondWeightKg = ticket.SecondWeightKg,
            NetWeightKg = ticket.NetWeightKg,

            UnitPricePerKg = ticket.UnitPricePerKg,
            TotalAmount = ticket.TotalAmount,
            CurrencyCode = ticket.CurrencyCode,

            ProductDescription = ticket.ProductDescription,
            Notes = ticket.Notes
        };

        var document = new TicketReportDocument(model);
        var pdfBytes = document.GeneratePdf();

        var fileName = $"ticket-{ticket.TicketNumber}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }
}
