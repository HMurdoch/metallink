using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using MetalLink.Shared.Stock;

namespace MetalLink.Api.Reports;

public class StockOnHandReportDocument : IDocument
{
    private readonly StockOnHandReportModel _model;

    public StockOnHandReportDocument(StockOnHandReportModel model)
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
                page.Size(PageSizes.A4);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

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
                column.Item().Text("STOCK ON HAND SUMMARY")
                    .FontSize(20)
                    .SemiBold()
                    .FontColor(Colors.Blue.Darken2);

                column.Item().PaddingTop(5).Text(text =>
                {
                    text.Span("Report Date: ").SemiBold();
                    text.Span($"{_model.ReportDate:yyyy-MM-dd HH:mm}");
                });

                if (!string.IsNullOrEmpty(_model.SiteName))
                {
                    column.Item().Text(text =>
                    {
                        text.Span("Site: ").SemiBold();
                        text.Span(_model.SiteName);
                    });
                }

                if (!string.IsNullOrEmpty(_model.CompanyName))
                {
                    column.Item().Text(text =>
                    {
                        text.Span("Company: ").SemiBold();
                        text.Span(_model.CompanyName);
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
                    col.Item().Text("Total Products").SemiBold();
                    col.Item().Text(_model.Items.Count.ToString()).FontSize(16).FontColor(Colors.Blue.Darken2);
                });

                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
                {
                    col.Item().Text("Total Quantity (kg)").SemiBold();
                    col.Item().Text(_model.TotalQuantityKg.ToString("N2")).FontSize(16).FontColor(Colors.Green.Darken2);
                });

                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
                {
                    col.Item().Text("Total Value (ZAR)").SemiBold();
                    col.Item().Text(_model.TotalValue.ToString("N2")).FontSize(16).FontColor(Colors.Orange.Darken2);
                });
            });

            // Table
            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(40);  // #
                    columns.RelativeColumn(3);    // Product
                    columns.RelativeColumn(2);    // Site
                    columns.RelativeColumn(2);    // Qty On Hand
                    columns.RelativeColumn(2);    // Total Received
                    columns.RelativeColumn(2);    // Total Sent
                    columns.RelativeColumn(2);    // Avg Cost
                    columns.RelativeColumn(2);    // Total Value
                    columns.RelativeColumn(2);    // Last Movement
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("#").SemiBold();
                    header.Cell().Element(CellStyle).Text("Product").SemiBold();
                    header.Cell().Element(CellStyle).Text("Site").SemiBold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Qty On Hand (kg)").SemiBold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Total Received (kg)").SemiBold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Total Sent (kg)").SemiBold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Avg Cost").SemiBold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Total Value").SemiBold();
                    header.Cell().Element(CellStyle).Text("Last Movement").SemiBold();

                    static IContainer CellStyle(IContainer container) => container
                        .BorderBottom(1)
                        .BorderColor(Colors.Grey.Darken1)
                        .PaddingVertical(5);
                });

                // Content
                var index = 1;
                foreach (var item in _model.Items)
                {
                    var bgColor = index % 2 == 0 ? Colors.Grey.Lighten3 : Colors.White;

                    table.Cell().Element(c => CellStyle(c, bgColor)).Text(index.ToString());
                    table.Cell().Element(c => CellStyle(c, bgColor)).Text(item.ProductName);
                    table.Cell().Element(c => CellStyle(c, bgColor)).Text(item.SiteName);
                    table.Cell().Element(c => CellStyle(c, bgColor)).AlignRight().Text(item.QuantityOnHandKg.ToString("N2"));
                    table.Cell().Element(c => CellStyle(c, bgColor)).AlignRight().Text(item.TotalReceivedKg.ToString("N2"));
                    table.Cell().Element(c => CellStyle(c, bgColor)).AlignRight().Text(item.TotalSentKg.ToString("N2"));
                    table.Cell().Element(c => CellStyle(c, bgColor)).AlignRight().Text(item.AverageUnitCost.ToString("N4"));
                    table.Cell().Element(c => CellStyle(c, bgColor)).AlignRight().Text(item.TotalValue.ToString("N2"));
                    table.Cell().Element(c => CellStyle(c, bgColor)).Text(item.LastMovementDate.HasValue 
                        ? $"{item.LastMovementDate.Value:yyyy-MM-dd} ({item.LastMovementType})" 
                        : "-");

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

public class StockOnHandReportModel
{
    public string CompanyName { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public DateTimeOffset ReportDate { get; set; }
    public decimal TotalQuantityKg { get; set; }
    public decimal TotalValue { get; set; }
    public List<StockOnHandReportItem> Items { get; set; } = new();
}

public class StockOnHandReportItem
{
    public string ProductName { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public decimal QuantityOnHandKg { get; set; }
    public decimal TotalReceivedKg { get; set; }
    public decimal TotalSentKg { get; set; }
    public decimal AverageUnitCost { get; set; }
    public decimal TotalValue { get; set; }
    public DateTimeOffset? LastMovementDate { get; set; }
    public string? LastMovementType { get; set; }
}
