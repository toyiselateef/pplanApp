using iTextSharp.text.pdf;
using iTextSharp.text;
using MfgDocs.Api.Models;

namespace MfgDocs.Api.Extensions;

public  static class Helper
{

    public static PdfPCell CreateCell(string text, BaseColor backgroundColor)
    {
        var cell = new PdfPCell(new Phrase(text, FontFactory.GetFont(FontFactory.HELVETICA, 8)));
        cell.BackgroundColor = backgroundColor;
        cell.Padding = 3;
        cell.HorizontalAlignment = Element.ALIGN_CENTER;
        return cell;
    }
    public static PdfPCell CreateInfoCell(string label, string value)
    {
        var table = new PdfPTable(1);
        var labelCell = new PdfPCell(new Phrase(label, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8)));
        labelCell.BackgroundColor = BaseColor.LIGHT_GRAY;
        labelCell.Padding = 2;
        table.AddCell(labelCell);

        var valueCell = new PdfPCell(new Phrase(value, FontFactory.GetFont(FontFactory.HELVETICA, 8)));
        valueCell.Padding = 2;
        table.AddCell(valueCell);

        var containerCell = new PdfPCell(table);
        containerCell.Border = Rectangle.NO_BORDER;
        return containerCell;
    }

    public static class DocumentHelpers
    {
        public static string FormatMultilineText(string text)
        {
            return text.Replace("\n", "<br>");
        }

        public static string CalculateTotalWeight(List<WorkOrderItem> items)
        {
            var total = items.Sum(i => int.TryParse(i.Weight, out int w) ? w : 0);
            return total.ToString();
        }

        public static string FormatDate(string date)
        {
            if (DateTime.TryParse(date, out DateTime parsedDate))
            {
                return parsedDate.ToString("dd-MMM-yyyy");
            }
            return date;
        }
    }
}

