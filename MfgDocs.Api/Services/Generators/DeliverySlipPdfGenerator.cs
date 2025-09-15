using MfgDocs.Api.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Microsoft.Extensions.Options;
using QuestPDF.Helpers;

namespace MfgDocs.Api.Services.Generators;

public class DeliverySlipPdfGenerator
{
    private readonly BrandingOptions _brand;

    public DeliverySlipPdfGenerator(Microsoft.Extensions.Options.IOptions<BrandingOptions> brand)
    {
        _brand = brand.Value;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Generate(DeliverySlipRequest request)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(t => t.FontSize(10));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(_brand.CompanyName).Bold().FontSize(16);
                        col.Item().Text(_brand.Address);
                        col.Item().Text(_brand.Phone);
                    });
                    row.ConstantItem(140).Border(1).Padding(5).Column(col =>
                    {
                        col.Item().Text("DELIVERY SLIP").Bold().FontSize(14).AlignCenter();
                        col.Item().Text($"Order #: {request.OrderNumber}");
                        col.Item().Text($"Delivery: {request.DeliveryDate:yyyy-MM-dd}");
                    });
                });

                page.Content().Column(col =>
                {
                    col.Item().Border(1).Padding(6).Column(info =>
                    {
                        info.Item().Text($"Customer: {request.Customer}");
                        info.Item().Text($"Address: {request.Address}");
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(5);
                            c.RelativeColumn(2);
                            c.RelativeColumn(3);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(CellHeader).Text("Description");
                            h.Cell().Element(CellHeader).Text("Qty");
                            h.Cell().Element(CellHeader).Text("Weight Each (lb)");
                            static QuestPDF.Infrastructure.IContainer CellHeader(QuestPDF.Infrastructure.IContainer x) => x.Padding(4).Background("#eee").DefaultTextStyle(t => t.SemiBold());
                        });

                        foreach (var line in request.Lines)
                        {
                            table.Cell().Padding(4).Text(line.Description);
                            table.Cell().Padding(4).Text(line.Quantity.ToString());
                            table.Cell().Padding(4).Text(line.WeightEachPounds.ToString("0.##"));
                        }
                    });

                    col.Item().PaddingTop(20).Text("Received By: ______________________    Signature: ______________________    Date: __________");
                });

                page.Footer().AlignCenter().Text("Thank you for your business.");
            });
        });

        return doc.GeneratePdf();
    }
}