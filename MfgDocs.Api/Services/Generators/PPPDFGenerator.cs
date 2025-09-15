using MfgDocs.Api.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using iText.Html2pdf;
using System.IO;
using DocumentFormat.OpenXml.Drawing.Charts;
using iText.Kernel.Pdf;


// This is a static class to hold the main logic for generating the PDF.
namespace MfgDocs.Api.Services.Generators;

    public class PPPDFGenerator
    {
        private readonly SizeCalculator _sizeCalc;

        public PPPDFGenerator(SizeCalculator sizeCalc)
        {
            _sizeCalc = sizeCalc;
        }
     
        public byte[] Generate(PourPlanRequest request) {
         
            string htmlContent = @"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <title>Work Order</title>
    <style>
        body { font-family: Arial, sans-serif; font-size: 10pt; margin: 0.5in; }
        .header { display: flex; justify-content: space-between; align-items: flex-start; }
        .company-info { font-size: 12pt; }
        .company-name { font-size: 18pt; font-weight: bold; }
        .work-order-box { background-color: #FFD1DC; border: 1px solid #D3D3D3; padding: 10px; text-align: center; font-weight: bold; font-size: 14pt; width: 150px; }
        .order-date { text-align: center; margin-top: 5px; font-size: 10pt; }
        .purchase-order { background-color: #E6F0FA; border: 1px solid #D3D3D3; padding: 5px; margin-top: 10px; }
        .info-row { display: flex; margin-top: 5px; }
        .builder-site { flex: 2; border: 1px solid #D3D3D3; }
        .blk-no, .company, .contact { flex: 1; border: 1px solid #D3D3D3; text-align: center; }
        .header-cell { background-color: #DCDCDC; font-weight: bold; font-size: 9pt; padding: 5px; }
        .lot-no { background-color: #DCDCDC; border: 1px solid #D3D3D3; padding: 5px; margin-top: 5px; }
        table { width: 100%; border-collapse: collapse; margin-top: 10px; }
        th, td { border: 1px solid #D3D3D3; padding: 5px; text-align: center; }
        th { background-color: #DCDCDC; font-weight: bold; }
        .lot-header { background-color: #E6F3FA; font-weight: bold; font-size: 10pt; text-align: left; }
        .notes { font-weight: bold; margin-top: 10px; }
        .total-weight { display: flex; justify-content: flex-end; margin-top: 5px; }
        .total-box { background-color: #DCDCDC; padding: 5px; width: 100px; text-align: right; font-weight: bold; }
        .expected-delivery { background-color: #E6F0FA; padding: 5px; margin-top: 5px; }
        .multi-line { white-space: pre-line; }
    </style>
</head>
<body>
    <div class='header'>
        <div class='company-info'>
            <div class='company-name'>🏠 MFG PRECAST</div>
            PO Box 7101, Maplehurst<br>
            Burlington, ON L7T 2J0<br>
            Phone: (905) 634 114, (905) 469 1119<br>
            Email: info@mfgprecast.com<br>
            www.mfgprecast.com
        </div>
        <div>
            <div class='work-order-box'>WORK ORDER</div>
            <div class='order-date'>Order Date 24-Jun-2025</div>
        </div>
    </div>
    <div class='purchase-order'>Purchase Order BL-4578</div>
    <div class='info-row'>
        <div class='builder-site'>
            <div class='header-cell'>BUILDER / SITE / CITY</div>
            <div style='border-top: 1px solid #D3D3D3; padding: 5px;'>Mattamy Homes</div>
            <div style='border-top: 1px solid #D3D3D3; padding: 5px;'>Lakehaven</div>
            <div style='border-top: 1px solid #D3D3D3; padding: 5px;'>Milton</div>
        </div>
        <div class='blk-no'>
            <div class='header-cell'>BLK No.</div>
        </div>
        <div class='company'>
            <div class='header-cell'>COMPANY LEGACY</div>
        </div>
        <div class='contact'>
            <div class='header-cell'>CONTACT Alsino</div>
        </div>
    </div>
    <div class='lot-no'>LOT No. 68, 63, 31, 35</div>
    <table>
        <thead>
            <tr>
                <th>QTY</th>
                <th>POURED SIZE<br><span style='font-size: 8pt;'>WIDTH X LENGTH</span></th>
                <th>FINISHED SIZE<br><span style='font-size: 8pt;'>WIDTH X LENGTH</span></th>
                <th>COLOR</th>
                <th>DESCRIPTION</th>
                <th>AREA<br>[SQ IN]</th>
                <th>WEIGHT<br>[Lbs]</th>
            </tr>
        </thead>
        <tbody>
            <tr><td colspan='7' class='lot-header'>LOT 68</td></tr>
            <tr>
                <td>1</td>
                <td class='multi-line'>(24 X 30)<br>(14 X 68)</td>
                <td class='multi-line'>21 X 28<br>11 X 66</td>
                <td>NEW WHITE</td>
                <td>SMOOTH FACE</td>
                <td style='text-align: right;'>720</td>
                <td style='text-align: right;'>134</td>
            </tr>
            <tr><td colspan='7' class='lot-header'>LOT 63</td></tr>
            <tr>
                <td>1</td>
                <td>(26 X 26)</td>
                <td>23 X 23</td>
                <td>GRAY</td>
                <td>SMOOTH FACE</td>
                <td style='text-align: right;'>676</td>
                <td style='text-align: right;'>121</td>
            </tr>
            <tr><td colspan='7' class='lot-header'>LOT 31</td></tr>
            <tr>
                <td>1</td>
                <td>(24 X 24)</td>
                <td>23 X 23</td>
                <td>GRAY</td>
                <td>ROCK FACE 21.5</td>
                <td style='text-align: right;'>576</td>
                <td style='text-align: right;'>121</td>
            </tr>
            <tr>
                <td>1</td>
                <td class='multi-line'>(26 X 102)<br>(12 X 151.5)</td>
                <td class='multi-line'>23 X 100<br>11 X 150</td>
                <td>GRAY</td>
                <td>SMOOTH FACE BUTT</td>
                <td style='text-align: right;'>2,652</td>
                <td style='text-align: right;'>509</td>
            </tr>
            <tr><td colspan='7' class='lot-header'>LOT 35</td></tr>
            <tr>
                <td>1</td>
                <td>(24 X 28.5)</td>
                <td>23 X 28</td>
                <td>GRAY</td>
                <td>ROCK FACE</td>
                <td style='text-align: right;'>708</td>
                <td style='text-align: right;'>146</td>
            </tr>
            <tr>
                <td>1</td>
                <td>(12 X 49.5)</td>
                <td>11 X 48</td>
                <td>GRAY</td>
                <td>ROCK FACE</td>
                <td style='text-align: right;'>594</td>
                <td style='text-align: right;'>123</td>
            </tr>
        </tbody>
    </table>
    <div class='notes'>Notes: NEED BRICK TIES</div>
    <div class='total-weight'>
        <div>Total Weight:</div>
        <div class='total-box'>1,704</div>
    </div>
    <div class='expected-delivery'>Expected Delivery Date is Before: 08 Jul 2025 (Tuesday)</div>
</body>
</html>";

        using var stream = new MemoryStream();
        var writer = new PdfWriter(stream);
        var pdfDoc = new iText.Kernel.Pdf.PdfDocument(writer); 
        var converterProperties = new ConverterProperties();

        // Create the PDF
        HtmlConverter.ConvertToPdf(htmlContent, pdfDoc, converterProperties);

        // Close the document to ensure all content is written
        pdfDoc.Close();

        return stream.ToArray();
    }

}

 




//using iText.IO.Font.Constants;
//using iText.Kernel.Colors;
//using iText.Kernel.Font;
//using iText.Kernel.Geom;
//using iText.Kernel.Pdf;
//using iText.Layout;
//using iText.Layout.Borders;
//using iText.Layout.Element;
//using iText.Layout.Properties;
//using MfgDocs.Api.Models;

//namespace MfgDocs.Api.Services.Generators
//{
//    public class PPPDFGenerator 
//    {
//        private readonly SizeCalculator _sizeCalc;

//        public PPPDFGenerator(SizeCalculator sizeCalc)
//        {
//            _sizeCalc = sizeCalc;
//        }

//        public byte[] Generate(PourPlanRequest request)
//        {
//            using var ms = new MemoryStream();
//            using var writer = new PdfWriter(ms, new WriterProperties()); // no crypto triggered
//            using var pdf = new PdfDocument(writer);
//            using var doc = new Document(pdf, PageSize.A4);

//            //using var ms = new MemoryStream();
//            //using var writer = new PdfWriter(ms);
//            //using var pdf = new PdfDocument(writer);
//            //var doc = new Document(pdf, PageSize.A4);

//            //doc.Add(new Paragraph($"Pour Plan - {request.PlanDate:yyyy-MM-dd}")
//            //    .SetFontSize(14).SetBold().SetTextAlignment(TextAlignment.CENTER));

//            PdfFont regular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
//            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

//            doc.Add(new Paragraph($"Pour Plan - {request.PlanDate:yyyy-MM-dd}")
//                .SetFont(bold)                // <-- Use the bold font here
//                .SetFontSize(14)
//                .SetTextAlignment(TextAlignment.CENTER));


//            doc.Add(new Paragraph("\nOrders Scheduled:\n"));

//            // Orders Table
//            var table = new Table(UnitValue.CreatePercentArray(new float[] { 20, 20, 20, 20, 20 }))
//                .UseAllAvailableWidth();
//            table.AddHeaderCell("Order #");
//            table.AddHeaderCell("Customer");
//            table.AddHeaderCell("Product");
//            table.AddHeaderCell("Poured Size");
//            table.AddHeaderCell("Qty");

//            foreach (var order in request.Orders)
//            {
//                foreach (var line in order.Lines)
//                {
//                    var poured = _sizeCalc.ComputePouredSize(line);
//                    table.AddCell(order.OrderNumber);
//                    table.AddCell(order.Customer);
//                    table.AddCell(line.ProductName);
//                    table.AddCell($"{poured.WidthInches} x {poured.LengthInches}");
//                    table.AddCell(line.Quantity.ToString());
//                }
//            }

//            doc.Add(table);

//            doc.Add(new Paragraph("\nPour Schedule:\n"));

//            // Simple Gantt blocks
//            var canvas = new iText.Kernel.Pdf.Canvas.PdfCanvas(pdf.AddNewPage());
//            float y = 700;
//            int slot = 1;
//            foreach (var order in request.Orders.Take(5))
//            {
//                canvas.SetStrokeColor(ColorConstants.BLUE)
//                    .SetFillColor(ColorConstants.LIGHT_GRAY)
//                    .Rectangle(100, y, 400, 20)
//                    .FillStroke();
//                canvas.BeginText()
//                    .SetFontAndSize(iText.Kernel.Font.PdfFontFactory.CreateFont(), 10)
//                    .MoveText(105, y + 5)
//                    .ShowText($"Order {order.OrderNumber} - {order.Customer}")
//                    .EndText();
//                y -= 30;
//                slot++;
//            }

//            doc.Close();
//            return ms.ToArray();
//        }
//    }

//}
