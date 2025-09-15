using MfgDocs.Api.Models;
using ClosedXML.Excel;
using System.IO;
using MfgDocs.Api.Services;

namespace MfgDocs.Api.Services.Generators;

public class PourPlanExcelGenerator
{
    private readonly SizeCalculator _sizeCalc;
    private readonly WeightCalculator _weightCalc;

    public PourPlanExcelGenerator(SizeCalculator sizeCalc, WeightCalculator weightCalc)
    {
        _sizeCalc = sizeCalc;
        _weightCalc = weightCalc;
    }

    public byte[] Generate(PourPlanRequest request)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Pour Plan");

        ws.Cell("A1").Value = "Plan Date";
        ws.Cell("B1").Value = request.PlanDate.ToString("yyyy-MM-dd");
        ws.Range("A1:B1").Style.Font.SetBold();

        var headers = new[] { "Order #", "Customer", "Product", "Finished (in)", "Poured (in)", "Qty", "Weight Each (lb)", "Notes" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(3, i + 1).Value = headers[i];
            ws.Cell(3, i + 1).Style.Font.SetBold();
            ws.Cell(3, i + 1).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#EEEEEE"));
            ws.Cell(3, i + 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }

        int row = 4;
        foreach (var order in request.Orders)
        {
            foreach (var line in order.Lines)
            {
                var poured = _sizeCalc.ComputePouredSize(line);
                var weight = _weightCalc.ComputeUnitWeight(poured);

                ws.Cell(row, 1).Value = order.OrderNumber;
                ws.Cell(row, 2).Value = order.Customer;
                ws.Cell(row, 3).Value = line.ProductName;
                ws.Cell(row, 4).Value = $"{line.FinishedSize.WidthInches} x {line.FinishedSize.LengthInches} x {line.FinishedSize.ThicknessInches}";
                ws.Cell(row, 5).Value = $"{poured.WidthInches} x {poured.LengthInches}";
                ws.Cell(row, 6).Value = line.Quantity;
                ws.Cell(row, 7).Value = weight;
                ws.Cell(row, 8).Value = (line.FinishType == FinishType.RockFace ? "RockFace" : "SmoothFace");
                row++;
            }
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}