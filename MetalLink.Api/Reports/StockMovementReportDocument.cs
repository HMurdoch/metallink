using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MetalLink.Api.Reports;

public class StockMovementReportDocument : IDocument
{
    private readonly StockMovementReportModel _model;

    public StockMovementReportDocument(StockMovementReportModel model)
    {
        _model = model;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Margin(50);
                page.Size(PageSizes.A4.Landscape());
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("STOCK MOVEMENT REPORT")
                    .FontSize(20)
                    .SemiBold()
                    .FontColor(Colors.Blue.Darken2);

                column.Item().PaddingTop(5).Text(text =>
                {
                    text.Span("Report Date: ").SemiBold();
                    text.Span($"{_model.ReportDate:yyyy-MM-dd HH:mm}");
                });

                column.Item().Text(text =>
                {
                    text.Span("Period: ").SemiBold();
                    text.Span($"{_model.FromDate:yyyy-MM-dd} to {_model.ToDate:yyyy-MM-dd}");
                });

                if (!string.IsNullOrEmpty(_model.SiteName))
                {
                    column.Item().Text(text =>
                    {
                        text.Span("Site: ").SemiBold();
                        text.Span(_model.SiteName);
                    });
                }

                if (!string.IsNullOrEmpty(_model.ProductName))
                {
                    column.Item().Text(text =>
                    {
                        text.Span("Product: ").SemiBold();
                        text.Span(_model.ProductName);
                    });
                }
            });

            row.ConstantItem(100).Height(50).Placeholder();
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(20).Column(column =>
        {
            column.Spacing(10);

            // Summary section
            column.Item().Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
                {
                    col.Item().Text("Total Movements").SemiBold();
                    col.Item().Text(_model.Items.Count.ToString()).FontSize(16).FontColor(Colors.Blue.Darken2);
                });

                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
                {
                    col.Item().Text("Stock IN (kg)").SemiBold();
                    col.Item().Text(_model.TotalReceivingKg.ToString("N2")).FontSize(16).FontColor(Colors.Green.Darken2);
                });

                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
                {
                    col.Item().Text("Stock OUT (kg)").SemiBold();
                    col.Item().Text(_model.TotalSendingKg.ToString("N2")).FontSize(16).FontColor(Colors.Red.Darken2);
                });

                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
                {
                    col.Item().Text("Net Change (kg)").SemiBold();
                    var netChange = _model.TotalReceivingKg - _model.TotalSendingKg;
                    col.Item().Text(netChange.ToString("N2")).FontSize(16)
                        .FontColor(netChange >= 0 ? Colors.Green.Darken2 : Colors.Red.Darken2);
                });
            });

            // Table
            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);   // #
                    columns.RelativeColumn(2);     // Date
                    columns.RelativeColumn(2);     // Ticket Number
                    columns.RelativeColumn(2);     // Type
                    columns.RelativeColumn(3);     // Product
                    columns.RelativeColumn(2);     // Site
                    columns.RelativeColumn(3);     // Counterparty
                    columns.RelativeColumn(2);     // Quantity
                    columns.RelativeColumn(2);     // Unit Price
                    columns.RelativeColumn(2);     // Total Value
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("#").SemiBold();
                    header.Cell().Element(CellStyle).Text("Date").SemiBold();
                    header.Cell().Element(CellStyle).Text("Ticket #").SemiBold();
                    header.Cell().Element(CellStyle).Text("Type").SemiBold();
                    header.Cell().Element(CellStyle).Text("Product").SemiBold();
                    header.Cell().Element(CellStyle).Text("Site").SemiBold();
                    header.Cell().Element(CellStyle).Text("Counterparty").SemiBold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Qty (kg)").SemiBold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Unit Price").SemiBold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Total Value").SemiBold();

                    static IContainer CellStyle(IContainer container) => container
                        .BorderBottom(1)
                        .BorderColor(Colors.Grey.Darken1)
                        .PaddingVertical(5);
                });

                // Content
                var index = 1;
                foreach (var item in _model.Items.OrderBy(i => i.MovementDate))
                {
                    var bgColor = index % 2 == 0 ? Colors.Grey.Lighten3 : Colors.White;
                    var typeColor = item.MovementType == "receiving" ? Colors.Green.Lighten3 : Colors.Red.Lighten3;

                    table.Cell().Element(c => CellStyle(c, bgColor)).Text(index.ToString());
                    table.Cell().Element(c => CellStyle(c, bgColor)).Text(item.MovementDate.ToString("yyyy-MM-dd HH:mm"));
                    table.Cell().Element(c => CellStyle(c, bgColor)).Text(item.TicketNumber);
                    table.Cell().Element(c => CellStyle(c, typeColor))
                        .Text(item.MovementType == "receiving" ? "IN" : "OUT")
                        .SemiBold();
                    table.Cell().Element(c => CellStyle(c, bgColor)).Text(item.ProductName);
                    table.Cell().Element(c => CellStyle(c, bgColor)).Text(item.SiteName);
                    table.Cell().Element(c => CellStyle(c, bgColor)).Text(item.CounterpartyName);
                    table.Cell().Element(c => CellStyle(c, bgColor)).AlignRight()
                        .Text(item.QuantityKg.ToString("N2"));
                    table.Cell().Element(c => CellStyle(c, bgColor)).AlignRight()
                        .Text(item.UnitPricePerKg.ToString("N4"));
                    table.Cell().Element(c => CellStyle(c, bgColor)).AlignRight()
                        .Text(item.TotalValue.ToString("N2"));

                    index++;
                }

                static IContainer CellStyle(IContainer container, string bgColor) => container
                    .Background(bgColor)
                    .BorderBottom(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .PaddingVertical(3)
                    .PaddingHorizontal(5);
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.Span("Page ");
            text.CurrentPageNumber();
            text.Span(" of ");
            text.TotalPages();
        });
    }
}

public class StockMovementReportModel
{
    public string CompanyName { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public DateTimeOffset ReportDate { get; set; }
    public DateTimeOffset FromDate { get; set; }
    public DateTimeOffset ToDate { get; set; }
    public decimal TotalReceivingKg { get; set; }
    public decimal TotalSendingKg { get; set; }
    public List<StockMovementReportItem> Items { get; set; } = new();
}

public class StockMovementReportItem
{
    public DateTimeOffset MovementDate { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty; // "receiving" or "sending"
    public string ProductName { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public string CounterpartyName { get; set; } = string.Empty;
    public decimal QuantityKg { get; set; }
    public decimal UnitPricePerKg { get; set; }
    public decimal TotalValue { get; set; }
}
