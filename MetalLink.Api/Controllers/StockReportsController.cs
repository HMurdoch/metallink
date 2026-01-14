using Microsoft.AspNetCore.Mvc;
using MetalLink.Application.Interfaces;
using MetalLink.Api.Reports;
using QuestPDF.Fluent;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockReportsController : ControllerBase
{
    private readonly IStockOnHandRepository _stockOnHandRepo;
    private readonly IStockMovementReceivingRepository _receivingRepo;
    private readonly IStockMovementSendingRepository _sendingRepo;

    public StockReportsController(
        IStockOnHandRepository stockOnHandRepo,
        IStockMovementReceivingRepository receivingRepo,
        IStockMovementSendingRepository sendingRepo)
    {
        _stockOnHandRepo = stockOnHandRepo;
        _receivingRepo = receivingRepo;
        _sendingRepo = sendingRepo;
    }

    /// <summary>
    /// Get stock on hand summary for all sites
    /// </summary>
    [HttpGet("stock-on-hand")]
    public async Task<ActionResult<StockOnHandReportModel>> GetStockOnHand([FromQuery] long? siteId = null)
    {
        var stockItems = siteId.HasValue
            ? await _stockOnHandRepo.GetAllBySiteAsync(siteId.Value)
            : await _stockOnHandRepo.GetAllAsync();

        var model = new StockOnHandReportModel
        {
            ReportDate = DateTimeOffset.UtcNow,
            Items = stockItems.Select(s => new StockOnHandReportItem
            {
                ProductName = s.Product?.ProductName ?? "Unknown",
                SiteName = s.Site?.SiteName ?? "Unknown",
                QuantityOnHandKg = s.QuantityOnHandKg,
                TotalReceivedKg = s.TotalReceivedKg,
                TotalSentKg = s.TotalSentKg,
                AverageUnitCost = s.AverageUnitCost,
                TotalValue = s.TotalValue,
                LastMovementDate = s.LastMovementDate,
                LastMovementType = s.LastMovementType
            }).ToList()
        };

        if (model.Items.Any())
        {
            model.SiteName = siteId.HasValue ? model.Items.First().SiteName : "All Sites";
            model.CompanyName = "MetalLink"; // TODO: Get from site
        }

        model.TotalQuantityKg = model.Items.Sum(i => i.QuantityOnHandKg);
        model.TotalValue = model.Items.Sum(i => i.TotalValue);

        return Ok(model);
    }

    /// <summary>
    /// Export stock on hand report to PDF
    /// </summary>
    [HttpGet("stock-on-hand/pdf")]
    public async Task<IActionResult> ExportStockOnHandToPdf([FromQuery] long? siteId = null)
    {
        var stockItems = siteId.HasValue
            ? await _stockOnHandRepo.GetAllBySiteAsync(siteId.Value)
            : await _stockOnHandRepo.GetAllAsync();

        var model = new StockOnHandReportModel
        {
            ReportDate = DateTimeOffset.UtcNow,
            Items = stockItems.Select(s => new StockOnHandReportItem
            {
                ProductName = s.Product?.ProductName ?? "Unknown",
                SiteName = s.Site?.SiteName ?? "Unknown",
                QuantityOnHandKg = s.QuantityOnHandKg,
                TotalReceivedKg = s.TotalReceivedKg,
                TotalSentKg = s.TotalSentKg,
                AverageUnitCost = s.AverageUnitCost,
                TotalValue = s.TotalValue,
                LastMovementDate = s.LastMovementDate,
                LastMovementType = s.LastMovementType
            }).ToList()
        };

        if (model.Items.Any())
        {
            model.SiteName = siteId.HasValue ? model.Items.First().SiteName : "All Sites";
            model.CompanyName = "MetalLink";
        }

        model.TotalQuantityKg = model.Items.Sum(i => i.QuantityOnHandKg);
        model.TotalValue = model.Items.Sum(i => i.TotalValue);

        var document = new StockOnHandReportDocument(model);
        var pdfBytes = document.GeneratePdf();

        return File(pdfBytes, "application/pdf", $"StockOnHand_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
    }

    /// <summary>
    /// Get stock movements for a date range
    /// </summary>
    [HttpGet("stock-movements")]
    public async Task<ActionResult<StockMovementReportModel>> GetStockMovements(
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        [FromQuery] long? siteId = null,
        [FromQuery] long? productId = null)
    {
        fromDate ??= DateTimeOffset.UtcNow.AddDays(-30);
        toDate ??= DateTimeOffset.UtcNow;

        // Get receiving movements
        var filteredReceiving = (await _receivingRepo.SearchAsync(
            siteId: siteId,
            productId: productId,
            startDate: fromDate,
            endDate: toDate,
            pageNumber: 1,
            pageSize: 10000)).ToList();

        // Get sending movements
        var filteredSending = (await _sendingRepo.SearchAsync(
            siteId: siteId,
            productId: productId,
            startDate: fromDate,
            endDate: toDate,
            pageNumber: 1,
            pageSize: 10000)).ToList();

        var model = new StockMovementReportModel
        {
            ReportDate = DateTimeOffset.UtcNow,
            FromDate = fromDate.Value,
            ToDate = toDate.Value,
            Items = new List<StockMovementReportItem>()
        };

        // Add receiving movements
        foreach (var r in filteredReceiving)
        {
            model.Items.Add(new StockMovementReportItem
            {
                MovementDate = r.MovementDate,
                TicketNumber = r.TicketNumber,
                MovementType = "receiving",
                ProductName = r.Product?.ProductName ?? "Unknown",
                SiteName = r.Site?.SiteName ?? "Unknown",
                CounterpartyName = r.CustomerName,
                QuantityKg = r.QuantityKg,
                UnitPricePerKg = r.UnitPricePerKg,
                TotalValue = r.TotalValue
            });
        }

        // Add sending movements
        foreach (var s in filteredSending)
        {
            model.Items.Add(new StockMovementReportItem
            {
                MovementDate = s.MovementDate,
                TicketNumber = s.TicketNumber,
                MovementType = "sending",
                ProductName = s.Product?.ProductName ?? "Unknown",
                SiteName = s.Site?.SiteName ?? "Unknown",
                CounterpartyName = s.BuyerName,
                QuantityKg = s.QuantityKg,
                UnitPricePerKg = s.UnitPricePerKg,
                TotalValue = s.TotalValue
            });
        }

        model.TotalReceivingKg = filteredReceiving.Sum(r => r.QuantityKg);
        model.TotalSendingKg = filteredSending.Sum(s => s.QuantityKg);

        if (model.Items.Any())
        {
            model.SiteName = siteId.HasValue ? model.Items.First().SiteName : "All Sites";
            model.ProductName = productId.HasValue ? model.Items.First().ProductName : "All Products";
        }

        return Ok(model);
    }

    /// <summary>
    /// Export stock movements report to PDF
    /// </summary>
    [HttpGet("stock-movements/pdf")]
    public async Task<IActionResult> ExportStockMovementsToPdf(
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        [FromQuery] long? siteId = null,
        [FromQuery] long? productId = null)
    {
        fromDate ??= DateTimeOffset.UtcNow.AddDays(-30);
        toDate ??= DateTimeOffset.UtcNow;

        // Get receiving movements
        var filteredReceiving = (await _receivingRepo.SearchAsync(
            siteId: siteId,
            productId: productId,
            startDate: fromDate,
            endDate: toDate,
            pageNumber: 1,
            pageSize: 10000)).ToList();

        // Get sending movements
        var filteredSending = (await _sendingRepo.SearchAsync(
            siteId: siteId,
            productId: productId,
            startDate: fromDate,
            endDate: toDate,
            pageNumber: 1,
            pageSize: 10000)).ToList();

        var model = new StockMovementReportModel
        {
            ReportDate = DateTimeOffset.UtcNow,
            FromDate = fromDate.Value,
            ToDate = toDate.Value,
            Items = new List<StockMovementReportItem>()
        };

        // Add receiving movements
        foreach (var r in filteredReceiving)
        {
            model.Items.Add(new StockMovementReportItem
            {
                MovementDate = r.MovementDate,
                TicketNumber = r.TicketNumber,
                MovementType = "receiving",
                ProductName = r.Product?.ProductName ?? "Unknown",
                SiteName = r.Site?.SiteName ?? "Unknown",
                CounterpartyName = r.CustomerName,
                QuantityKg = r.QuantityKg,
                UnitPricePerKg = r.UnitPricePerKg,
                TotalValue = r.TotalValue
            });
        }

        // Add sending movements
        foreach (var s in filteredSending)
        {
            model.Items.Add(new StockMovementReportItem
            {
                MovementDate = s.MovementDate,
                TicketNumber = s.TicketNumber,
                MovementType = "sending",
                ProductName = s.Product?.ProductName ?? "Unknown",
                SiteName = s.Site?.SiteName ?? "Unknown",
                CounterpartyName = s.BuyerName,
                QuantityKg = s.QuantityKg,
                UnitPricePerKg = s.UnitPricePerKg,
                TotalValue = s.TotalValue
            });
        }

        model.TotalReceivingKg = filteredReceiving.Sum(r => r.QuantityKg);
        model.TotalSendingKg = filteredSending.Sum(s => s.QuantityKg);

        if (model.Items.Any())
        {
            model.SiteName = siteId.HasValue ? model.Items.First().SiteName : "All Sites";
            model.ProductName = productId.HasValue ? model.Items.First().ProductName : "All Products";
        }

        var document = new StockMovementReportDocument(model);
        var pdfBytes = document.GeneratePdf();

        return File(pdfBytes, "application/pdf", $"StockMovements_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
    }

    /// <summary>
    /// Recalculate stock on hand for a specific site and product
    /// </summary>
    [HttpPost("recalculate")]
    public async Task<IActionResult> RecalculateStock([FromQuery] long? siteId = null, [FromQuery] long? productId = null)
    {
        if (siteId.HasValue && productId.HasValue)
        {
            await _stockOnHandRepo.RecalculateStockAsync(siteId.Value, productId.Value);
            return Ok(new { message = "Stock recalculated successfully for site and product" });
        }
        else
        {
            await _stockOnHandRepo.RecalculateAllStockAsync();
            return Ok(new { message = "All stock recalculated successfully" });
        }
    }
}
