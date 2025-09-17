using iTextSharp.text.pdf;
using iTextSharp.text;

namespace MfgDocs.Api.Extensions;

  
    public static class DocumentExtensions
    {
        public static PdfPTable CreateBorderedTable(int columns, float[] widths)
        {
            var table = new PdfPTable(columns) { WidthPercentage = 100 };
            if (widths != null) table.SetWidths(widths);

            table.DefaultCell.Border = Rectangle.BOX;
            table.DefaultCell.BorderWidth = 1;
            table.DefaultCell.BorderColor = BaseColor.BLACK;

            return table;
        }

        public static void AddTableBorder(this PdfPTable table, BaseColor borderColor, float borderWidth = 1f)
        {
            table.DefaultCell.BorderColor = borderColor;
            table.DefaultCell.BorderWidth = borderWidth;
        }

        public static PdfPCell CreateMergedCell(string text, int colspan, BaseColor backgroundColor, Font font)
        {
            var cell = new PdfPCell(new Phrase(text, font))
            {
                Colspan = colspan,
                BackgroundColor = backgroundColor,
                HorizontalAlignment = Element.ALIGN_CENTER,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Padding = 5,
                Border = Rectangle.BOX
            };
            return cell;
        }
    }

    public static class ColorPalette
    {
        public static readonly BaseColor PrimaryBlue = new BaseColor(70, 130, 180);
        public static readonly BaseColor PrimaryRed = new BaseColor(220, 20, 60);
        public static readonly BaseColor HeaderGray = new BaseColor(128, 128, 128);
        public static readonly BaseColor LightGray = new BaseColor(240, 240, 240);
        public static readonly BaseColor Yellow = new BaseColor(255, 255, 0);
        public static readonly BaseColor LightBlue = new BaseColor(173, 216, 230);
        public static readonly BaseColor LightGreen = new BaseColor(144, 238, 144);
    }
