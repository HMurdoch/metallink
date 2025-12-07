using System;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MetalLink.Api.Reports;

public sealed class TicketReportDocument : IDocument
{
    private readonly TicketReportModel _model;

    public TicketReportDocument(TicketReportModel model)
    {
        _model = model;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(30);
            page.Size(PageSizes.A4);

            // HEADER
            page.Header().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Metal Link Ticket").FontSize(18).SemiBold();
                    col.Item().Text($"Ticket #: {_model.TicketNumber}").FontSize(12);
                    col.Item().Text($"Date: {_model.CreatedTime:yyyy-MM-dd HH:mm}");
                    col.Item().Text($"Site: {_model.SiteName} (ID: {_model.SiteId})");
                });

                row.ConstantItem(120).Column(col =>
                {
                    col.Item().AlignRight().Text("Metal Link").FontSize(16).SemiBold();
                    col.Item().AlignRight().Text("Powered by Elementech");
                });
            });

            // CONTENT
            page.Content().PaddingTop(15).Column(col =>
            {
                // CUSTOMER SECTION
                col.Item().Text("Customer").FontSize(14).SemiBold().Underline();

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(130);
                        columns.RelativeColumn();
                    });

                    table.Cell().Element(HeadingCell).Text("Customer ID:");
                    table.Cell().Element(ValueCell).Text(_model.CustomerId.ToString());

                    table.Cell().Element(HeadingCell).Text("Name:");
                    table.Cell().Element(ValueCell).Text(_model.CustomerName);

                    table.Cell().Element(HeadingCell).Text("Account #:");
                    table.Cell().Element(ValueCell).Text(_model.CustomerAccountNumber ?? "-");

                    table.Cell().Element(HeadingCell).Text("Price Code:");
                    table.Cell().Element(ValueCell).Text(_model.CustomerPriceCode ?? "-");
                });

                col.Item().PaddingTop(15);

                // TICKET SECTION
                col.Item().Text("Ticket Details").FontSize(14).SemiBold().Underline();

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(130);
                        columns.RelativeColumn();
                    });

                    table.Cell().Element(HeadingCell).Text("Ticket ID:");
                    table.Cell().Element(ValueCell).Text(_model.TicketId.ToString());

                    table.Cell().Element(HeadingCell).Text("Type:");
                    table.Cell().Element(ValueCell).Text(_model.TicketType);

                    table.Cell().Element(HeadingCell).Text("Product:");
                    table.Cell().Element(ValueCell).Text(_model.ProductDescription ?? "-");

                    table.Cell().Element(HeadingCell).Text("Notes:");
                    table.Cell().Element(ValueCell).Text(_model.Notes ?? "-");
                });

                col.Item().PaddingTop(15);

                // WEIGHTS & PRICING
                col.Item().Text("Weights & Pricing").FontSize(14).SemiBold().Underline();

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(130);
                        columns.RelativeColumn();
                    });

                    table.Cell().Element(HeadingCell).Text("First Weight (kg):");
                    table.Cell().Element(ValueCell).Text(_model.FirstWeightKg?.ToString("0.0") ?? "-");

                    table.Cell().Element(HeadingCell).Text("Second Weight (kg):");
                    table.Cell().Element(ValueCell).Text(_model.SecondWeightKg?.ToString("0.0") ?? "-");

                    table.Cell().Element(HeadingCell).Text("Net Weight (kg):");
                    table.Cell().Element(ValueCell).Text(_model.NetWeightKg?.ToString("0.0") ?? "-");

                    table.Cell().Element(HeadingCell).Text("Unit Price / kg:");
                    table.Cell().Element(ValueCell)
                          .Text($"{_model.UnitPricePerKg:0.00} {_model.CurrencyCode}");

                    table.Cell().Element(HeadingCell).Text("Total Amount:");
                    table.Cell().Element(ValueCell)
                          .Text($"{_model.TotalAmount:0.00} {_model.CurrencyCode}");
                });

                col.Item().PaddingTop(25);

                // SIGNATURE
                col.Item().Text("Signature").FontSize(12).SemiBold();
                col.Item().LineHorizontal(1).LineColor(Colors.Black);

                col.Item()
                    .Text("Customer acknowledges receipt and correctness of the above weights and amount.")
                    .FontSize(9);
            });

            // FOOTER
            page.Footer().AlignRight().Text(txt =>
            {
                txt.Span("Metal Link ");
                txt.Span($"Ticket #{_model.TicketNumber}").SemiBold();
                txt.Span(" | Generated on ");
                txt.Span($"{DateTime.Now:yyyy-MM-dd HH:mm}");
            });
        });
    }

    private static IContainer HeadingCell(IContainer container)
        => container.PaddingVertical(2)
                   .PaddingRight(5)
                   .AlignLeft()
                   .TextStyle(Style.Default.SemiBold());

    private static IContainer ValueCell(IContainer container)
        => container.PaddingVertical(2);
}
