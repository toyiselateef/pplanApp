// Pour Plan PDF Generator - Updated for QuestPDF 2025 + SkiaSharp
// ----------------------------------------------------------------------
// Instructions:
// 1) Create a new .NET 8 console or minimal web API project.
// 2) Add NuGet packages:
//    - QuestPDF (2025.x)
//    - SkiaSharp
//    - (platform native assets) SkiaSharp.NativeAssets.Linux.NoDependencies or platform-specific package
// 3) Replace Program.cs with this file. Build and run.
// ----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;

// -------------------------
// SkiaSharp helper extensions
// -------------------------
static class Sty
    {
        public static readonly Color Light = new Color(4294309882u); // #F5F7FA
        public static readonly Color Blue = new Color(4281033401u); // #2B62B9
        public static readonly Color Red = new Color(4293212469u); // #E53935
        public static readonly Color Yellow = new Color(4294951095u); // #FFC107
        public static readonly Color Dark = new Color(4281545523u); // #333333
        public static readonly Color Grey = new Color(4287994010u); // #95989A

    }
public static class SkiaSharpExtensions
{
    // Vector (SVG) option
    public static void SkiaSharpSvgCanvas(this IContainer container, Action<SKCanvas, Size> drawOnCanvas)
    {
        container.Svg(size =>
        {
            using var stream = new MemoryStream();
            using var svgCanvas = SKSvgCanvas.Create(new SKRect(0, 0, size.Width, size.Height), stream);
            drawOnCanvas(svgCanvas, size);
            svgCanvas.Dispose();
            var data = stream.ToArray();
            return System.Text.Encoding.UTF8.GetString(data);
        });
    }
    
        // Raster option: returns PNG image bytes
        public static void SkiaSharpRasterizedCanvas(this IContainer container, Action<SKCanvas, ImageSize> drawOnCanvas)
    {
        container.Image(payload =>
        {
            var width = Math.Max(1, (int)payload.ImageSize.Width);
            var height = Math.Max(1, (int)payload.ImageSize.Height);

            using var bitmap = new SKBitmap(width, height);
            using var skCanvas = new SKCanvas(bitmap);

            // map drawing coordinate space so payload.AvailableSpace matches 1:1 with drawing coordinates
            var scaleX = width / Math.Max(1f, payload.AvailableSpace.Width);
            var scaleY = height / Math.Max(1f, payload.AvailableSpace.Height);
            skCanvas.Scale(scaleX, scaleY);

            drawOnCanvas(skCanvas, payload.ImageSize);

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        });
    }
}

// -------------------------
// Domain models
// -------------------------
public record SizeInInches(float W, float H)
{
    public float Area => W * H;
    public override string ToString() => $"{W:0.#}\" x {H:0.#}\"";
}

public record PanelSegment(SizeInInches SegmentSize, bool YellowMargin = false, bool RedTrimLineTop = false, bool RedTrimLineBottom = false, bool HasInnerRectangle = true);
public record PanelRow(IReadOnlyList<PanelSegment> Segments, float? TotalWidthInches = null, float? TotalHeightInches = null);
public record MoldPlan(string MoldName, SizeInInches MoldOuterSize, string MoldCode, string Station, IReadOnlyList<PanelRow> Rows);
public record BagRow(string MoldSizeLabel, int PouredW, int PouredH, int NumOfPcs, float ExtraMarginPct, float SqInchTotal);
public record BagsCalculation(IReadOnlyList<BagRow> Rows, string FooterNote);
public record PourPlan(string Title, DateOnly Date, string Notes, IReadOnlyList<MoldPlan> Molds, BagsCalculation BagsCalc);

// -------------------------
// Drawing helpers (SKCanvas)
// -------------------------
static class DrawHelpers
{
    public static void DrawRoundedRect(SKCanvas canvas, SKRect rect, SKColor strokeColor, float strokeWidth, float cornerRadius)
    {
        using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, Color = strokeColor, StrokeWidth = strokeWidth };
        canvas.DrawRoundRect(rect, cornerRadius, cornerRadius, paint);
    }

    public static void DrawInnerRect(SKCanvas canvas, SKRect rect, float inset, SKColor color)
    {
        var inner = new SKRect(rect.Left + inset, rect.Top + inset, rect.Right - inset, rect.Bottom - inset);
        using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, Color = color, StrokeWidth = 1.2f };
        canvas.DrawRoundRect(inner, 4f, 4f, paint);
    }

    public static void DrawDimensionLine(SKCanvas canvas, SKPoint from, SKPoint to, string label)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#E53935"), StrokeWidth = 2f, Style = SKPaintStyle.Stroke };
        canvas.DrawLine(from, to, paint);

        // label background and text
        using var textPaint = new SKPaint { IsAntialias = true, Color = SKColors.Black, TextSize = 10f };
        var textW = textPaint.MeasureText(label);
        var x = (from.X + to.X) / 2f - textW / 2f;
        var y = (from.Y + to.Y) / 2f - 2f;

        var bgRect = new SKRect(x - 4f, y - 12f, x + textW + 4f, y + 2f);
        using var bg = new SKPaint { IsAntialias = true, Color = SKColors.White, Style = SKPaintStyle.Fill };
        canvas.DrawRoundRect(bgRect, 3f, 3f, bg);

        canvas.DrawText(label, x, y, textPaint);
    }

    public static void DrawDoubleArrowWithLabel(SKCanvas canvas, SKPoint from, SKPoint to, string label)
    {
        // shaft
        using var shaft = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#E53935"), StrokeWidth = 2f, Style = SKPaintStyle.Stroke };
        canvas.DrawLine(from, to, shaft);

        // arrow head at 'to'
        var v = new SKPoint(to.X - from.X, to.Y - from.Y);
        var len = MathF.Sqrt(v.X * v.X + v.Y * v.Y);
        if (len > 0.001f)
        {
            var ux = v.X / len; var uy = v.Y / len;
            float headLen = 10f, headW = 6f;
            var left = new SKPoint(to.X - headLen * ux + headW * uy, to.Y - headLen * uy - headW * ux);
            var right = new SKPoint(to.X - headLen * ux - headW * uy, to.Y - headLen * uy + headW * ux);
            using var fill = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#E53935"), Style = SKPaintStyle.Fill };
            using var path = new SKPath();
            path.MoveTo(to); path.LineTo(left); path.LineTo(right); path.Close();
            canvas.DrawPath(path, fill);

            // arrow at 'from' (reverse)
            var left2 = new SKPoint(from.X + headLen * ux + headW * uy, from.Y + headLen * uy - headW * ux);
            var right2 = new SKPoint(from.X + headLen * ux - headW * uy, from.Y + headLen * uy + headW * ux);
            using var path2 = new SKPath();
            path2.MoveTo(from); path2.LineTo(left2); path2.LineTo(right2); path2.Close();
            canvas.DrawPath(path2, fill);

            // label
            using var textPaint = new SKPaint { IsAntialias = true, Color = SKColors.Black, TextSize = 10f };
            var textW = textPaint.MeasureText(label);
            var midX = (from.X + to.X) / 2f - textW / 2f;
            var midY = (from.Y + to.Y) / 2f - 4f;
            var labelBg = new SKRect(midX - 4f, midY - 12f, midX + textW + 4f, midY + 2f);
            using var bg = new SKPaint { IsAntialias = true, Color = SKColors.White, Style = SKPaintStyle.Fill };
            canvas.DrawRoundRect(labelBg, 3f, 3f, bg);
            canvas.DrawText(label, midX, midY, textPaint);
        }
    }
}

// -------------------------
// Document implementation
// -------------------------
public class PourPlanDocument : IDocument
{
    private readonly PourPlan _data;
    public PourPlanDocument() : this(Sample.Build()) { }
    public PourPlanDocument(PourPlan data) => _data = data;

    public DocumentMetadata GetMetadata() => new DocumentMetadata { Title = _data.Title, Author = "PourPlanGenerator" };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A3.Landscape());
            page.Margin(24);
            //page.DefaultTextStyle(TextStyle.Default.FontSize(10).FontColor(SKColors.Parse("#333333")));
            page.DefaultTextStyle(TextStyle.Default.FontSize(10).FontColor(Sty.Dark));
            page.Header().Height(48).Row(r =>
            {
                r.RelativeItem().AlignLeft().Text(_data.Date.ToString("yyyy-MM-dd")).Style(TextStyle.Default.FontSize(10));
                r.RelativeItem().AlignCenter().Text(_data.Title).Style(TextStyle.Default.SemiBold().FontSize(22).FontColor(Sty.Blue));
                r.RelativeItem().AlignRight().Text(_data.Notes).Style(TextStyle.Default.FontSize(10));
            });

            page.Content().PaddingVertical(6).Column(col =>
            {
                col.Spacing(12);
                // Molds area: grid of molds
                col.Item().Row(r =>
                {
                    r.Spacing(12);
                    foreach (var mold in _data.Molds)
                    {
                        r.RelativeItem().Element(c => ComposeMold(c, mold));
                    }
                });

                // Bags table area
                col.Item().Element(ComposeBagsTable);
            });

            page.Footer().AlignCenter().Text(_data.BagsCalc.FooterNote).Style(TextStyle.Default.FontSize(9));
        });
    }

    private void ComposeMold(IContainer container, MoldPlan mold)
    {
        container.Border(1).Padding(8).Column(col =>
        {
            col.Item().Text($"MOLD NAME - {mold.MoldCode} ({mold.MoldOuterSize})").Style(TextStyle.Default.SemiBold().FontSize(11)).FontColor(Sty.Red);
            col.Spacing(6);

            col.Item().Row(r =>
            {
                r.ConstantItem(34).AlignMiddle().Element(c => c.Background("#EFEFEF").Padding(6).AlignCenter().Text(mold.Station).Style(TextStyle.Default.SemiBold().FontSize(10)));
                r.RelativeItem().Element(c =>
                {
                    // Use Skia to draw the stacked panel segments precisely
                    c.SkiaSharpRasterizedCanvas((skCanvas, imageSize) =>
                    {
                        var width = imageSize.Width;
                        var height = imageSize.Height;
                        var marginX = 6f;
                        var marginY = 6f;
                        var drawWidth = width - marginX * 2f;
                        var drawHeight = height - marginY * 2f;

                        // background
                        skCanvas.Clear(SKColors.White);

                        // for each row in mold.Rows draw stacked segments vertically
                        float topY = marginY;
                        foreach (var row in mold.Rows)
                        {
                            int segCount = Math.Max(1, row.Segments.Count);
                            float segHeight = (drawHeight / mold.Rows.Count - 8f) / segCount; // split per row

                            foreach (var seg in row.Segments)
                            {
                                var rect = new SKRect(marginX, topY, marginX + drawWidth, topY + segHeight);

                                // outer rounded rect
                                DrawHelpers.DrawRoundedRect(skCanvas, rect, SKColor.Parse("#333333"), 1.2f, 6f);

                                // inner rectangle
                                if (seg.HasInnerRectangle)
                                    DrawHelpers.DrawInnerRect(skCanvas, rect, 8f, SKColor.Parse("#333333"));

                                // yellow margin
                                if (seg.YellowMargin)
                                {
                                    using var yPaint = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#FFC107"), Style = SKPaintStyle.Stroke, StrokeWidth = 2.0f };
                                    var yRect = new SKRect(rect.Left + 2f, rect.Top + 2f, rect.Right - 2f, rect.Bottom - 2f);
                                    skCanvas.DrawRoundRect(yRect, 6f, 6f, yPaint);
                                }

                                // red trim lines
                                if (seg.RedTrimLineTop)
                                {
                                    using var rPaint = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#E53935"), StrokeWidth = 2f };
                                    skCanvas.DrawLine(rect.Left + 6f, rect.Top + 6f, rect.Right - 6f, rect.Top + 6f, rPaint);
                                }
                                if (seg.RedTrimLineBottom)
                                {
                                    using var rPaint = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#E53935"), StrokeWidth = 2f };
                                    skCanvas.DrawLine(rect.Left + 6f, rect.Bottom - 6f, rect.Right - 6f, rect.Bottom - 6f, rPaint);
                                }

                                // label centered
                                using var labelPaint = new SKPaint { IsAntialias = true, Color = SKColors.Black, TextSize = 12f };
                                var label = $"{seg.SegmentSize.W:0.#}\" x {seg.SegmentSize.H:0.#}\"";
                                var tw = labelPaint.MeasureText(label);
                                skCanvas.DrawText(label, rect.MidX - tw / 2f, rect.MidY + 4f, labelPaint);

                                topY += segHeight + 6f;
                            }

                            // after each PanelRow, if the row requests a total height arrow, draw it
                            if (row.TotalHeightInches is float th)
                            {
                                var from = new SKPoint(marginX + drawWidth + 18f, marginY);
                                var to = new SKPoint(marginX + drawWidth + 18f, marginY + drawHeight);
                                DrawHelpers.DrawDoubleArrowWithLabel(skCanvas, from, to, $"TOTAL = {th:0.#}\"");
                            }
                        }

                        // Example: if a PanelRow shares some width on the right side, draw small rectangle overlapping width
                        // We'll draw the last row's small box aligned to the right but not full width
                        if (mold.Rows.Count > 0)
                        {
                            var last = mold.Rows[^1];
                            if (last.TotalWidthInches is float w)
                            {
                                var smallW = drawWidth * 0.4f; // delta - adjust mapping from inches to px if desired
                                var r = new SKRect(marginX + drawWidth - smallW, marginY + drawHeight - 38f, marginX + drawWidth, marginY + drawHeight - 8f);
                                DrawHelpers.DrawRoundedRect(skCanvas, r, SKColor.Parse("#333333"), 1.2f, 6f);

                                // label
                                using var txt = new SKPaint { IsAntialias = true, Color = SKColors.Black, TextSize = 10f };
                                var lbl = $"{w:0.#}\"";
                                var lblW = txt.MeasureText(lbl);
                                skCanvas.DrawText(lbl, r.MidX - lblW / 2f, r.MidY + 4f, txt);
                            }
                        }

                    });
                });
            });
        });
    }

    private void ComposeBagsTable(IContainer container)
    {
        container.PaddingTop(8).Column(col =>
        {
            col.Item().Text("CALCULATING NUMBER OF BAGS - " + _data.Title).Style(TextStyle.Default.SemiBold().FontSize(12));

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(1); c.RelativeColumn(2); c.RelativeColumn(2); });

                table.Header(h =>
                {
                    h.Cell().Background("#95989A").Padding(6).Text("MOLD SIZE").Style(TextStyle.Default.SemiBold().FontSize(8).FontColor(Colors.White));
                    h.Cell().Background("#95989A").Padding(6).Text("POURED W (IN)").Style(TextStyle.Default.SemiBold().FontSize(8).FontColor(Colors.White));
                    h.Cell().Background("#95989A").Padding(6).Text("POURED H (IN)").Style(TextStyle.Default.SemiBold().FontSize(8).FontColor(Colors.White));
                    h.Cell().Background("#95989A").Padding(6).Text("PCS").Style(TextStyle.Default.SemiBold().FontSize(8).FontColor(Colors.White));
                    h.Cell().Background("#95989A").Padding(6).Text("EXTRA %").Style(TextStyle.Default.SemiBold().FontSize(8).FontColor(Colors.White));
                    h.Cell().Background("#95989A").Padding(6).Text("TOTAL AREA (SQ.IN)").Style(TextStyle.Default.SemiBold().FontSize(8).FontColor(Colors.White));
                });

                for (int i = 0; i < _data.BagsCalc.Rows.Count; i++)
                {
                    var r = _data.BagsCalc.Rows[i];
                    var band = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten3;

                    table.Cell().Background(band).Padding(6).Text(r.MoldSizeLabel);
                    table.Cell().Background(band).Padding(6).Text(r.PouredW.ToString(CultureInfo.InvariantCulture));
                    table.Cell().Background(band).Padding(6).Text(r.PouredH.ToString(CultureInfo.InvariantCulture));
                    table.Cell().Background(band).Padding(6).Text(r.NumOfPcs.ToString(CultureInfo.InvariantCulture));
                    table.Cell().Background(band).Padding(6).Text(r.ExtraMarginPct.ToString("0.#", CultureInfo.InvariantCulture));
                    table.Cell().Background(band).Padding(6).Text(r.SqInchTotal.ToString("0", CultureInfo.InvariantCulture));
                }

                var totalArea = 0f;
                foreach (var rr in _data.BagsCalc.Rows) totalArea += rr.SqInchTotal;

                table.Cell().ColumnSpan(5).AlignRight().Padding(6).Text("TOTAL AREA FOR CONCRETE MIX (SQ.INCH)").Style(TextStyle.Default.SemiBold());
                table.Cell().Padding(6).Text(totalArea.ToString("0"));
            });
        });
    }
}

// -------------------------
// Sample data
// -------------------------
static class Sample
{
    public static PourPlan Build()
    {
        var molds = new List<MoldPlan>
        {
            new MoldPlan("J", new SizeInInches(20,120), "J", "P1", new List<PanelRow>
            {
                new PanelRow(new List<PanelSegment>{ new PanelSegment(new SizeInInches(23.5f,88.5f), YellowMargin:true) }, TotalWidthInches: null, TotalHeightInches: null)
            }),

            new MoldPlan("H", new SizeInInches(26,120), "H", "P1", new List<PanelRow>
            {
                new PanelRow(new List<PanelSegment>{ new PanelSegment(new SizeInInches(23.5f,88.5f)) }, TotalWidthInches: null)
            }),

            new MoldPlan("C", new SizeInInches(51,122), "C", "P1", new List<PanelRow>
            {
                new PanelRow(new List<PanelSegment>
                {
                    new PanelSegment(new SizeInInches(23.5f,81.5f), false, false, true),
                    new PanelSegment(new SizeInInches(23.5f,81.5f), false, true, false),
                }, TotalHeightInches: 112f),
                new PanelRow(new List<PanelSegment>
                {
                    new PanelSegment(new SizeInInches(23.5f,27.5f), HasInnerRectangle:false)
                }, TotalWidthInches:23.5f)
            })
        };

        var bagRows = new List<BagRow>
        {
            new BagRow("20\" x 120\"", 20, 20, 3, 10f, 3 * 20 * 20 * 1.10f),
            new BagRow("26\" x 120\"", 20, 20, 2, 10f, 2 * 20 * 20 * 1.10f),
            new BagRow("51\" x 122\"", 22, 22, 5, 9f, 5 * 22 * 22 * 1.09f),
        };

        return new PourPlan("Pour 1", DateOnly.FromDateTime(DateTime.Today), "NOTE:", molds, new BagsCalculation(bagRows, "NUMBER OF BAGS REQUIRED - auto-calculated"));
    }
}

// -------------------------
// Program entry
// -------------------------
 



//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Globalization;
//using System.IO;
//using QuestPDF.Fluent;
//using QuestPDF.Helpers;
//using QuestPDF.Infrastructure;

//// -----------------------------
//// DOMAIN MODELS & CALCULATIONS
//// -----------------------------

//public record PourPlan(
//    string Title,
//    DateOnly Date,
//    string Notes,
//    IReadOnlyList<MoldPlan> Molds,
//    BagsCalculation BagsCalc
//);

//public record MoldPlan(
//    string MoldName,
//    SizeInInches MoldOuterSize,
//    string MoldCode,
//    string Station,
//    IReadOnlyList<PanelRow> Rows
//);

//public record PanelRow(
//    IReadOnlyList<PanelSegment> Segments,
//    float? TotalWidthInches = null,
//    float? TotalHeightInches = null
//);

//public record PanelSegment(
//    SizeInInches SegmentSize,
//    bool YellowMargin = false,
//    bool RedTrimLineTop = false,
//    bool RedTrimLineBottom = false,
//    bool HasInnerRectangle = true
//);

//public record BagsCalculation(
//    IReadOnlyList<BagRow> Rows,
//    string FooterNote
//);

//public record BagRow(
//    string MoldSizeLabel,
//    int PouredWidthInches,
//    int PouredHeightInches,
//    int NumOfPcs,
//    float ExtraMarginPercent,
//    float SqInchTotal
//);

//public readonly record struct SizeInInches(float W, float H)
//{
//    public float AreaSqInch => W * H;
//    public override string ToString() => $"{W:0.#}\" x {H:0.#}\"";
//}

//static class Calc
//{
//    public static float SumSqInch(IEnumerable<BagRow> rows) => rows.Sum(r => r.SqInchTotal);

//    public static string FormatInches(float value)
//    {
//        if (Math.Abs(value - MathF.Round(value)) < 0.001f)
//            return $"{value:0}\"";
//        return $"{value:0.##}\"";
//    }
//}

//// ---------------------------------
//// STYLE TOKENS (colors, dimensions)
//// ---------------------------------

//static class Sty
//{
//    public static readonly Color Light = new Color(4294309882u); // #F5F7FA
//    public static readonly Color Blue = new Color(4281033401u); // #2B62B9
//    public static readonly Color Red = new Color(4293212469u); // #E53935
//    public static readonly Color Yellow = new Color(4294951095u); // #FFC107
//    public static readonly Color Dark = new Color(4281545523u); // #333333
//    public static readonly Color Grey = new Color(4287994010u); // #95989A

//    public const float MoldStroke = 1.6f;
//    public const float SegmentStroke = 1.2f;
//    public const float YellowStroke = 2.0f;
//    public const float TrimStroke = 2.0f;
//    public const float CornerRadius = 6f;
//    public const float InnerInset = 9f;

//    public static TextStyle H1 => TextStyle.Default.SemiBold().FontSize(26).FontColor(Blue);
//    public static TextStyle H2 => TextStyle.Default.Bold().FontSize(14).FontColor(Red);
//    public static TextStyle Label => TextStyle.Default.Medium().FontSize(10).FontColor(Dark);
//    public static TextStyle Small => TextStyle.Default.FontSize(9).FontColor(Dark);
//    public static TextStyle Tiny => TextStyle.Default.FontSize(8).FontColor(Dark);
//    public static TextStyle WhiteTiny => TextStyle.Default.FontSize(8).FontColor(Colors.White);
//}

//// ---------------------------------
//// CUSTOM DRAWING COMPONENTS
//// ---------------------------------

//public static class CustomElements
//{
//    public static void DrawMoldSegment(this IContainer container, PanelSegment segment)
//    {
//        container.Layers(layers =>
//        {
//            // Set the base rectangle as the primary layer. This is the fix.
//            layers.PrimaryLayer().Border(Sty.SegmentStroke).BorderColor(Sty.Dark).Background(Colors.White);

//            // The other layers are stacked on top of the primary layer.
//            // Inner rectangle (void) if needed
//            if (segment.HasInnerRectangle)
//            {
//                layers.Layer().Padding(Sty.InnerInset).Border(Sty.SegmentStroke).BorderColor(Sty.Dark);
//            }

//            // Yellow margin highlight
//            if (segment.YellowMargin)
//            {
//                layers.Layer().Padding(2).Border(Sty.YellowStroke).BorderColor(Sty.Yellow);
//            }

//            // Trim lines using decorative elements
//            if (segment.RedTrimLineTop)
//            {
//                layers.Layer().PaddingTop(6).PaddingHorizontal(6).Height(Sty.TrimStroke).Background(Sty.Red);
//            }
//            if (segment.RedTrimLineBottom)
//            {
//                layers.Layer().AlignBottom().PaddingBottom(6).PaddingHorizontal(6).Height(Sty.TrimStroke).Background(Sty.Red);
//            }

//            // Size label in center
//            layers.Layer().AlignCenter().AlignMiddle().Text($"{segment.SegmentSize.W:0.#}\" x {segment.SegmentSize.H:0.#}\"").Style(Sty.Label);
//        });
//    }

//    public static void DrawDimensionArrow(this IContainer container, string label, bool horizontal = true)
//    {
//        container.Column(col =>
//        {
//            if (horizontal)
//            {
//                col.Item().Height(2).Background(Sty.Red);
//                col.Item().AlignCenter().PaddingVertical(2).Text(label).Style(Sty.Small.FontColor(Sty.Red));
//            }
//            else
//            {
//                col.Item().Row(row =>
//                {
//                    row.ConstantItem(2).Background(Sty.Red);
//                    row.ConstantItem(4);
//                    row.RelativeItem().AlignLeft().AlignMiddle().Text(label).Style(Sty.Small.FontColor(Sty.Red));
//                });
//            }
//        });
//    }
//}

//// -----------------------------
//// DOCUMENT IMPLEMENTATION
//// -----------------------------

//public class PourPlanDocument : IDocument
//{
//    public PourPlan Data { get; }
//    public PourPlanDocument(PourPlan data) => Data = data;

//    public DocumentMetadata GetMetadata() => new DocumentMetadata
//    {
//        Title = Data.Title,
//        Author = "PourPlan Generator",
//        Subject = "Pour Plan",
//        Keywords = "pour plan, molds, pdf"
//    };

//    public void Compose(IDocumentContainer container)
//    {
//        container.Page(page =>
//        {
//            page.Size(PageSizes.A3.Landscape());
//            page.Margin(30);
//            page.DefaultTextStyle(TextStyle.Default.FontSize(11).FontColor(Sty.Dark));
//            page.Header().Element(ComposeHeader);
//            page.Content().Element(ComposeBody);
//            page.Footer().AlignCenter().Text(Data.BagsCalc.FooterNote).Style(Sty.Tiny);
//        });
//    }

//    void ComposeHeader(IContainer container)
//    {
//        container.Row(row =>
//        {
//            row.RelativeItem().Text(Data.Date.ToString("yyyy-MM-dd")).Style(Sty.Small);
//            row.RelativeItem().AlignCenter().Layers(layers =>
//            {
//                // This is the primary layer that sets the size for the layers component
//                layers.PrimaryLayer().Background("#E9EEF9").Border(1).BorderColor(Sty.Blue);

//                // This is an additional layer that will be stacked on top
//                layers.Layer().PaddingVertical(8).PaddingHorizontal(20).Text(Data.Title).Style(Sty.H1);
//            });
//            row.RelativeItem().AlignRight().Text(Data.Notes).Style(Sty.Small);
//        });
//    }

//    void ComposeBody(IContainer container)
//    {
//        container.Column(col =>
//        {
//            col.Spacing(18);

//            // Molds area
//            col.Item().Row(r =>
//            {
//                r.Spacing(16);
//                foreach (var mold in Data.Molds)
//                {
//                    r.RelativeItem(1).Element(c => ComposeMold(c, mold));
//                }
//            });

//            // Bags table
//            col.Item().Element(ComposeBagsTable);
//        });
//    }

//    void ComposeMold(IContainer container, MoldPlan mold)
//    {
//        container.Padding(8).Border(Sty.MoldStroke).BorderColor(Sty.Dark).Background(Colors.White).Column(col =>
//        {
//            col.Spacing(6);
//            // Header with mold name
//            var label = $"MOLD NAME - {mold.MoldCode} ({mold.MoldOuterSize})";
//            col.Item().Border(1).BorderColor(Sty.Red).PaddingVertical(4).PaddingHorizontal(6)
//                .AlignLeft().Text(label).Style(Sty.H2);

//            // Station and mold content
//            col.Item().Row(r =>
//            {
//                // Station circle - FIX IS HERE
//                r.ConstantItem(40).AlignMiddle().Layers(layers =>
//                {
//                    // This is the primary layer that sets the size
//                    layers.PrimaryLayer().Width(30).Height(30).Background("#EFEFEF").Border(1).BorderColor(Sty.Dark);
//                    // This is the additional layer for the text
//                    layers.Layer().AlignCenter().AlignMiddle().Text(mold.Station).Style(TextStyle.Default.Bold().FontSize(12));
//                });

//                r.Spacing(12);

//                // Mold rows
//                r.RelativeItem().Column(cc =>
//                {
//                    foreach (var row in mold.Rows)
//                    {
//                        cc.Item().Element(c => ComposeMoldRow(c, row));
//                        cc.Spacing(8);
//                    }
//                });
//            });
//        });
//    }

//    void ComposeMoldRow(IContainer container, PanelRow row)
//    {
//        container.Column(col =>
//        {
//            // Main segments
//            col.Item().Height(120).Column(segCol =>
//            {
//                var segmentHeight = 120f / row.Segments.Count;
//                foreach (var segment in row.Segments)
//                {
//                    segCol.Item().Height(segmentHeight - 4).Padding(2)
//                        .Element(c => c.DrawMoldSegment(segment));
//                }
//            });

//            // Dimension arrows and labels
//            if (row.TotalHeightInches.HasValue)
//            {
//                col.Item().Row(dimRow =>
//                {
//                    dimRow.RelativeItem();
//                    dimRow.ConstantItem(120).Element(c => c.DrawDimensionArrow(
//                        $"TOTAL LENGTH WITH WASTE = {row.TotalHeightInches:0.#}\"", false));
//                });
//            }

//            if (row.TotalWidthInches.HasValue)
//            {
//                col.Item().PaddingTop(4).Element(c => c.DrawDimensionArrow(
//                    $"{row.TotalWidthInches:0.#}\"", true));
//            }
//        });
//    }

//    void ComposeBagsTable(IContainer container)
//    {
//        var rows = Data.BagsCalc.Rows;

//        container.Column(col =>
//        {
//            col.Item().PaddingTop(8).Text("CALCULATING NUMBER OF BAGS - " + Data.Title.ToUpperInvariant())
//                .Style(TextStyle.Default.SemiBold().FontSize(12));

//            col.Item().Table(t =>
//            {
//                t.ColumnsDefinition(c =>
//                {
//                    c.RelativeColumn(3); // mold size
//                    c.RelativeColumn(2); // poured size W
//                    c.RelativeColumn(2); // poured size H
//                    c.RelativeColumn(1); // pcs
//                    c.RelativeColumn(2); // extra margin
//                    c.RelativeColumn(2); // total sq inch
//                });

//                // Header
//                t.Header(h =>
//                {
//                    h.Cell().Background(Sty.Grey).Padding(6).Text("MOLD SIZE").Style(Sty.WhiteTiny);
//                    h.Cell().Background(Sty.Grey).Padding(6).Text("POURED W (INCHES)").Style(Sty.WhiteTiny);
//                    h.Cell().Background(Sty.Grey).Padding(6).Text("POURED H (INCHES)").Style(Sty.WhiteTiny);
//                    h.Cell().Background(Sty.Grey).Padding(6).Text("PCS").Style(Sty.WhiteTiny);
//                    h.Cell().Background(Sty.Grey).Padding(6).Text("EXTRA MARGIN %").Style(Sty.WhiteTiny);
//                    h.Cell().Background(Sty.Grey).Padding(6).Text("TOTAL AREA (SQ.INCH)").Style(Sty.WhiteTiny);
//                });

//                // Body rows
//                for (int i = 0; i < rows.Count; i++)
//                {
//                    var r = rows[i];
//                    var band = i % 2 == 0 ? Colors.White : Sty.Light;
//                    t.Cell().Background(band).Padding(6).Text(r.MoldSizeLabel).Style(Sty.Small);
//                    t.Cell().Background(band).Padding(6).Text(r.PouredWidthInches.ToString(CultureInfo.InvariantCulture)).Style(Sty.Small);
//                    t.Cell().Background(band).Padding(6).Text(r.PouredHeightInches.ToString(CultureInfo.InvariantCulture)).Style(Sty.Small);
//                    t.Cell().Background(band).Padding(6).Text(r.NumOfPcs.ToString(CultureInfo.InvariantCulture)).Style(Sty.Small);
//                    t.Cell().Background(band).Padding(6).Text(r.ExtraMarginPercent.ToString("0.#", CultureInfo.InvariantCulture)).Style(Sty.Small);
//                    t.Cell().Background(band).Padding(6).Text(r.SqInchTotal.ToString("0", CultureInfo.InvariantCulture)).Style(Sty.Small);
//                }

//                // Total row
//                var total = Calc.SumSqInch(rows);
//                t.Cell().ColumnSpan(5).AlignRight().Padding(6).Text("TOTAL AREA FOR CONCRETE MIX (SQ.INCH)")
//                    .Style(TextStyle.Default.SemiBold().FontSize(10));
//                t.Cell().Padding(6).Text(total.ToString("0")).Style(TextStyle.Default.SemiBold().FontSize(10));
//            });
//        });
//    }
//}

//// -----------------------------
//// SAMPLE DATA
//// -----------------------------

//static class Sample
//{
//    public static PourPlan Build()
//    {
//        var molds = new List<MoldPlan>
//        {
//            new(
//                MoldName: "J",
//                MoldOuterSize: new SizeInInches(20, 120),
//                MoldCode: "J",
//                Station: "P1",
//                Rows: new List<PanelRow>
//                {
//                    new(
//                        Segments: new List<PanelSegment>
//                        {
//                            new(new SizeInInches(23.5f, 88.5f), YellowMargin: true)
//                        }
//                    )
//                }
//            ),
//            new(
//                MoldName: "H",
//                MoldOuterSize: new SizeInInches(26, 120),
//                MoldCode: "H",
//                Station: "P1",
//                Rows: new List<PanelRow>
//                {
//                    new(
//                        Segments: new List<PanelSegment>
//                        {
//                            new(new SizeInInches(23.5f, 88.5f))
//                        },
//                        TotalWidthInches: 91.5f
//                    )
//                }
//            ),
//            new(
//                MoldName: "C",
//                MoldOuterSize: new SizeInInches(51, 122),
//                MoldCode: "C",
//                Station: "P1",
//                Rows: new List<PanelRow>
//                {
//                    new(
//                        Segments: new List<PanelSegment>
//                        {
//                            new(new SizeInInches(23.5f, 81.5f), RedTrimLineBottom: true),
//                            new(new SizeInInches(23.5f, 81.5f), RedTrimLineTop: true)
//                        },
//                        TotalHeightInches: 112f
//                    ),
//                    new(
//                        Segments: new List<PanelSegment>
//                        {
//                            new(new SizeInInches(23.5f, 27.5f), HasInnerRectangle: false)
//                        },
//                        TotalWidthInches: 23.5f
//                    )
//                }
//            )
//        };

//        var bagRows = new List<BagRow>
//        {
//            new("20\" x 120\"", 20, 20, 3, 10, 3 * 20 * 20 * 1.10f),
//            new("26\" x 120\"", 20, 20, 2, 10, 2 * 20 * 20 * 1.10f),
//            new("51\" x 122\"", 22, 22, 5, 9, 5 * 22 * 22 * 1.09f),
//        };

//        return new PourPlan(
//            Title: "Pour 1",
//            Date: DateOnly.FromDateTime(DateTime.Today),
//            Notes: "NOTE:",
//            Molds: molds,
//            BagsCalc: new BagsCalculation(bagRows, "NUMBER OF BAGS REQUIRED - auto-calculated")
//        );
//    }
//}


//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Globalization;
//using System.IO;
//using QuestPDF.Fluent;
//using QuestPDF.Helpers;
//using QuestPDF.Infrastructure;

//// -----------------------------
//// DOMAIN MODELS & CALCULATIONS
//// -----------------------------

//public record PourPlan(
//    string Title,
//    DateOnly Date,
//    string Notes,
//    IReadOnlyList<MoldPlan> Molds,
//    BagsCalculation BagsCalc
//);

//public record MoldPlan(
//    string MoldName,
//    SizeInInches MoldOuterSize,
//    string MoldCode,
//    string Station,
//    IReadOnlyList<PanelRow> Rows
//);

//public record PanelRow(
//    IReadOnlyList<PanelSegment> Segments,
//    float? TotalWidthInches = null,
//    float? TotalHeightInches = null
//);

//public record PanelSegment(
//    SizeInInches SegmentSize,
//    bool YellowMargin = false,
//    bool RedTrimLineTop = false,
//    bool RedTrimLineBottom = false,
//    bool HasInnerRectangle = true
//);

//public record BagsCalculation(
//    IReadOnlyList<BagRow> Rows,
//    string FooterNote
//);

//public record BagRow(
//    string MoldSizeLabel,
//    int PouredWidthInches,
//    int PouredHeightInches,
//    int NumOfPcs,
//    float ExtraMarginPercent,
//    float SqInchTotal
//);

//public readonly record struct SizeInInches(float W, float H)
//{
//    public float AreaSqInch => W * H;
//    public override string ToString() => $"{W:0.#}\" x {H:0.#}\"";
//}

//static class Calc
//{
//    public static float SumSqInch(IEnumerable<BagRow> rows) => rows.Sum(r => r.SqInchTotal);

//    public static string FormatInches(float value)
//    {
//        if (Math.Abs(value - MathF.Round(value)) < 0.001f)
//            return $"{value:0}\"";
//        return $"{value:0.##}\"";
//    }
//}

//// ---------------------------------
//// STYLE TOKENS (colors, dimensions)
//// ---------------------------------

//static class Sty
//{
//    //public static readonly string Blue = "#2B62B9";
//    //public static readonly string Red = "#E53935";
//    //public static readonly string Yellow = "#FFC107";
//    //public static readonly string Dark = "#333333";
//    //public static readonly string Light = "#F5F7FA";
//    //public static readonly string Grey = "#95989A";
//    public static readonly Color Light = new Color(4294309882u); // This is the uint value for #F5F7FA

//    public static readonly Color Blue = new Color(4279060385u);
//    public static readonly Color Red = new Color(4294198070u);
//    public static readonly Color Yellow = new Color(4294961979u);
//    public static readonly Color Dark = new Color(4278546128u);
//    public static readonly Color Grey = new Color(4288585374u);

//    public const float MoldStroke = 1.6f;
//    public const float SegmentStroke = 1.2f;
//    public const float YellowStroke = 2.0f;
//    public const float TrimStroke = 2.0f;
//    public const float CornerRadius = 6f;
//    public const float InnerInset = 9f;

//    public static TextStyle H1 => TextStyle.Default.SemiBold().FontSize(26).FontColor(Blue);
//    public static TextStyle H2 => TextStyle.Default.Bold().FontSize(14).FontColor(Red);
//    public static TextStyle Label => TextStyle.Default.Medium().FontSize(10).FontColor(Dark);
//    public static TextStyle Small => TextStyle.Default.FontSize(9).FontColor(Dark);
//    public static TextStyle Tiny => TextStyle.Default.FontSize(8).FontColor(Dark);
//    public static TextStyle WhiteTiny => TextStyle.Default.FontSize(8).FontColor(Colors.White);
//}

//// ---------------------------------
//// CUSTOM DRAWING COMPONENTS
//// ---------------------------------

//public static class CustomElements
//{
//    public static void DrawMoldSegment(this IContainer container, PanelSegment segment)
//    {
//        container.Layers(layers =>
//        {
//            // Set the base rectangle as the primary layer. This is the fix.
//            layers.PrimaryLayer().Border(Sty.SegmentStroke).BorderColor(Sty.Dark).Background(Colors.White);

//            // The other layers are stacked on top of the primary layer.
//            // Inner rectangle (void) if needed
//            if (segment.HasInnerRectangle)
//            {
//                layers.Layer().Padding(Sty.InnerInset).Border(Sty.SegmentStroke).BorderColor(Sty.Dark);
//            }

//            // Yellow margin highlight
//            if (segment.YellowMargin)
//            {
//                layers.Layer().Padding(2).Border(Sty.YellowStroke).BorderColor(Sty.Yellow);
//            }

//            // Trim lines using decorative elements
//            if (segment.RedTrimLineTop)
//            {
//                layers.Layer().PaddingTop(6).PaddingHorizontal(6).Height(Sty.TrimStroke).Background(Sty.Red);
//            }
//            if (segment.RedTrimLineBottom)
//            {
//                layers.Layer().AlignBottom().PaddingBottom(6).PaddingHorizontal(6).Height(Sty.TrimStroke).Background(Sty.Red);
//            }

//            // Size label in center
//            layers.Layer().AlignCenter().AlignMiddle().Text($"{segment.SegmentSize.W:0.#}\" x {segment.SegmentSize.H:0.#}\"").Style(Sty.Label);
//        });
//    }
//    //public static void DrawMoldSegment(this IContainer container, PanelSegment segment)
//    //{
//    //    container.Layers(layers =>
//    //    {
//    //        // Base segment rectangle
//    //        layers.Layer().Border(Sty.SegmentStroke).BorderColor(Sty.Dark).Background(Colors.White);

//    //        // Inner rectangle (void) if needed
//    //        if (segment.HasInnerRectangle)
//    //        {
//    //            layers.Layer().Padding(Sty.InnerInset).Border(Sty.SegmentStroke).BorderColor(Sty.Dark);
//    //        }

//    //        // Yellow margin highlight
//    //        if (segment.YellowMargin)
//    //        {
//    //            layers.Layer().Padding(2).Border(Sty.YellowStroke).BorderColor(Sty.Yellow);
//    //        }

//    //        // Trim lines using decorative elements
//    //        if (segment.RedTrimLineTop)
//    //        {
//    //            layers.Layer().PaddingTop(6).PaddingHorizontal(6).Height(Sty.TrimStroke).Background(Sty.Red);
//    //        }
//    //        if (segment.RedTrimLineBottom)
//    //        {
//    //            layers.Layer().AlignBottom().PaddingBottom(6).PaddingHorizontal(6).Height(Sty.TrimStroke).Background(Sty.Red);
//    //        }

//    //        // Size label in center
//    //        layers.Layer().AlignCenter().AlignMiddle().Text($"{segment.SegmentSize.W:0.#}\" x {segment.SegmentSize.H:0.#}\"").Style(Sty.Label);
//    //    });
//    //}

//    public static void DrawDimensionArrow(this IContainer container, string label, bool horizontal = true)
//    {
//        container.Column(col =>
//        {
//            if (horizontal)
//            {
//                col.Item().Height(2).Background(Sty.Red);
//                col.Item().AlignCenter().PaddingVertical(2).Text(label).Style(Sty.Small.FontColor(Sty.Red));
//            }
//            else
//            {
//                col.Item().Row(row =>
//                {
//                    row.ConstantItem(2).Background(Sty.Red);
//                    row.ConstantItem(4);
//                    row.RelativeItem().AlignLeft().AlignMiddle().Text(label).Style(Sty.Small.FontColor(Sty.Red));
//                });
//            }
//        });
//    }
//}

//// -----------------------------
//// DOCUMENT IMPLEMENTATION
//// -----------------------------

//public class PourPlanDocument : IDocument
//{
//    public PourPlan Data { get; }
//    public PourPlanDocument(PourPlan data) => Data = data;

//    public DocumentMetadata GetMetadata() => new DocumentMetadata
//    {
//        Title = Data.Title,
//        Author = "PourPlan Generator",
//        Subject = "Pour Plan",
//        Keywords = "pour plan, molds, pdf"
//    };

//    public void Compose(IDocumentContainer container)
//    {
//        container.Page(page =>
//        {
//            page.Size(PageSizes.A3.Landscape());
//            page.Margin(30);
//            page.DefaultTextStyle(TextStyle.Default.FontSize(11).FontColor(Sty.Dark));
//            page.Header().Element(ComposeHeader);
//            page.Content().Element(ComposeBody);
//            page.Footer().AlignCenter().Text(Data.BagsCalc.FooterNote).Style(Sty.Tiny);
//        });
//    }

//    //void ComposeHeader(IContainer container)
//    //{
//    //    container.Row(row =>
//    //    {
//    //        row.RelativeItem().Text(Data.Date.ToString("yyyy-MM-dd")).Style(Sty.Small);
//    //        row.RelativeItem().AlignCenter().Layers(layers =>
//    //        {
//    //            layers.Layer().Background("#E9EEF9").Border(1).BorderColor(Sty.Blue);
//    //            layers.Layer().PaddingVertical(8).PaddingHorizontal(20).Text(Data.Title).Style(Sty.H1);
//    //        });
//    //        row.RelativeItem().AlignRight().Text(Data.Notes).Style(Sty.Small);
//    //    });
//    //}

//    void ComposeHeader(IContainer container)
//    {
//        container.Row(row =>
//        {
//            row.RelativeItem().Text(Data.Date.ToString("yyyy-MM-dd")).Style(Sty.Small);
//            row.RelativeItem().AlignCenter().Layers(layers =>
//            {
//                // This is the primary layer that sets the size for the layers component
//                layers.PrimaryLayer().Background("#E9EEF9").Border(1).BorderColor(Sty.Blue);

//                // This is an additional layer that will be stacked on top
//                layers.Layer().PaddingVertical(8).PaddingHorizontal(20).Text(Data.Title).Style(Sty.H1);
//            });
//            row.RelativeItem().AlignRight().Text(Data.Notes).Style(Sty.Small);
//        });
//    }
//    void ComposeBody(IContainer container)
//    {
//        container.Column(col =>
//        {
//            col.Spacing(18);

//            // Molds area
//            col.Item().Row(r =>
//            {
//                r.Spacing(16);
//                foreach (var mold in Data.Molds)
//                {
//                    r.RelativeItem(1).Element(c => ComposeMold(c, mold));
//                }
//            });

//            // Bags table
//            col.Item().Element(ComposeBagsTable);
//        });
//    }
//    // The corrected ComposeMold function
//    void ComposeMold(IContainer container, MoldPlan mold)
//    {
//        container.Padding(8).Border(Sty.MoldStroke).BorderColor(Sty.Dark).Background(Colors.White).Column(col =>
//        {
//            col.Spacing(6);
//            // Header with mold name
//            var label = $"MOLD NAME - {mold.MoldCode} ({mold.MoldOuterSize})";
//            col.Item().Border(1).BorderColor(Sty.Red).PaddingVertical(4).PaddingHorizontal(6)
//                .AlignLeft().Text(label).Style(Sty.H2);

//            // Station and mold content
//            col.Item().Row(r =>
//            {
//                // Station circle - FIX IS HERE
//                r.ConstantItem(40).AlignMiddle().Layers(layers =>
//                {
//                    // This is the primary layer that sets the size
//                    layers.PrimaryLayer().Width(30).Height(30).Background("#EFEFEF").Border(1).BorderColor(Sty.Dark);
//                    // This is the additional layer for the text
//                    layers.Layer().AlignCenter().AlignMiddle().Text(mold.Station).Style(TextStyle.Default.Bold().FontSize(12));
//                });

//                r.Spacing(12);

//                // Mold rows
//                r.RelativeItem().Column(cc =>
//                {
//                    foreach (var row in mold.Rows)
//                    {
//                        cc.Item().Element(c => ComposeMoldRow(c, row));
//                        cc.Spacing(8);
//                    }
//                });
//            });
//        });
//    }
//    //void ComposeMold(IContainer container, MoldPlan mold)
//    //{
//    //    container.Padding(8).Border(Sty.MoldStroke).BorderColor(Sty.Dark).Background(Colors.White).Column(col =>
//    //    {
//    //        col.Spacing(6);

//    //        // Header with mold name
//    //        var label = $"MOLD NAME - {mold.MoldCode} ({mold.MoldOuterSize})";
//    //        col.Item().Border(1).BorderColor(Sty.Red).PaddingVertical(4).PaddingHorizontal(6)
//    //            .AlignLeft().Text(label).Style(Sty.H2);

//    //        // Station and mold content
//    //        col.Item().Row(r =>
//    //        {
//    //            // Station circle
//    //            r.ConstantItem(40).AlignMiddle().Layers(layers =>
//    //            {
//    //                layers.Layer().Width(30).Height(30).Background("#EFEFEF").Border(1).BorderColor(Sty.Dark);
//    //                layers.Layer().AlignCenter().AlignMiddle().Text(mold.Station).Style(TextStyle.Default.Bold().FontSize(12));
//    //            });

//    //            r.Spacing(12);

//    //            // Mold rows
//    //            r.RelativeItem().Column(cc =>
//    //            {
//    //                foreach (var row in mold.Rows)
//    //                {
//    //                    cc.Item().Element(c => ComposeMoldRow(c, row));
//    //                    cc.Spacing(8);
//    //                }
//    //            });
//    //        });
//    //    });
//    //}

//    void ComposeMoldRow(IContainer container, PanelRow row)
//    {
//        container.Column(col =>
//        {
//            // Main segments
//            col.Item().Height(120).Column(segCol =>
//            {
//                var segmentHeight = 120f / row.Segments.Count;
//                foreach (var segment in row.Segments)
//                {
//                    segCol.Item().Height(segmentHeight - 4).Padding(2)
//                        .Element(c => c.DrawMoldSegment(segment));
//                }
//            });

//            // Dimension arrows and labels
//            if (row.TotalHeightInches.HasValue)
//            {
//                col.Item().Row(dimRow =>
//                {
//                    dimRow.RelativeItem();
//                    dimRow.ConstantItem(120).Element(c => c.DrawDimensionArrow(
//                        $"TOTAL LENGTH WITH WASTE = {row.TotalHeightInches:0.#}\"", false));
//                });
//            }

//            if (row.TotalWidthInches.HasValue)
//            {
//                col.Item().PaddingTop(4).Element(c => c.DrawDimensionArrow(
//                    $"{row.TotalWidthInches:0.#}\"", true));
//            }
//        });
//    }

//    void ComposeBagsTable(IContainer container)
//    {
//        var rows = Data.BagsCalc.Rows;

//        container.Column(col =>
//        {
//            col.Item().PaddingTop(8).Text("CALCULATING NUMBER OF BAGS - " + Data.Title.ToUpperInvariant())
//                .Style(TextStyle.Default.SemiBold().FontSize(12));

//            col.Item().Table(t =>
//            {
//                t.ColumnsDefinition(c =>
//                {
//                    c.RelativeColumn(3); // mold size
//                    c.RelativeColumn(2); // poured size W
//                    c.RelativeColumn(2); // poured size H
//                    c.RelativeColumn(1); // pcs
//                    c.RelativeColumn(2); // extra margin
//                    c.RelativeColumn(2); // total sq inch
//                });

//                // Header
//                t.Header(h =>
//                {
//                    h.Cell().Background(Sty.Grey).Padding(6).Text("MOLD SIZE").Style(Sty.WhiteTiny);
//                    h.Cell().Background(Sty.Grey).Padding(6).Text("POURED W (INCHES)").Style(Sty.WhiteTiny);
//                    h.Cell().Background(Sty.Grey).Padding(6).Text("POURED H (INCHES)").Style(Sty.WhiteTiny);
//                    h.Cell().Background(Sty.Grey).Padding(6).Text("PCS").Style(Sty.WhiteTiny);
//                    h.Cell().Background(Sty.Grey).Padding(6).Text("EXTRA MARGIN %").Style(Sty.WhiteTiny);
//                    h.Cell().Background(Sty.Grey).Padding(6).Text("TOTAL AREA (SQ.INCH)").Style(Sty.WhiteTiny);
//                });

//                // Body rows
//                for (int i = 0; i < rows.Count; i++)
//                {
//                    var r = rows[i];
//                    var band = i % 2 == 0 ? Colors.White : Sty.Light;
//                    t.Cell().Background(band).Padding(6).Text(r.MoldSizeLabel).Style(Sty.Small);
//                    t.Cell().Background(band).Padding(6).Text(r.PouredWidthInches.ToString(CultureInfo.InvariantCulture)).Style(Sty.Small);
//                    t.Cell().Background(band).Padding(6).Text(r.PouredHeightInches.ToString(CultureInfo.InvariantCulture)).Style(Sty.Small);
//                    t.Cell().Background(band).Padding(6).Text(r.NumOfPcs.ToString(CultureInfo.InvariantCulture)).Style(Sty.Small);
//                    t.Cell().Background(band).Padding(6).Text(r.ExtraMarginPercent.ToString("0.#", CultureInfo.InvariantCulture)).Style(Sty.Small);
//                    t.Cell().Background(band).Padding(6).Text(r.SqInchTotal.ToString("0", CultureInfo.InvariantCulture)).Style(Sty.Small);
//                }

//                // Total row
//                var total = Calc.SumSqInch(rows);
//                t.Cell().ColumnSpan(5).AlignRight().Padding(6).Text("TOTAL AREA FOR CONCRETE MIX (SQ.INCH)")
//                    .Style(TextStyle.Default.SemiBold().FontSize(10));
//                t.Cell().Padding(6).Text(total.ToString("0")).Style(TextStyle.Default.SemiBold().FontSize(10));
//            });
//        });
//    }
//}

//// -----------------------------
//// SAMPLE DATA
//// -----------------------------

//static class Sample
//{
//    public static PourPlan Build()
//    {
//        var molds = new List<MoldPlan>
//        {
//            new(
//                MoldName: "J",
//                MoldOuterSize: new SizeInInches(20, 120),
//                MoldCode: "J",
//                Station: "P1",
//                Rows: new List<PanelRow>
//                {
//                    new(
//                        Segments: new List<PanelSegment>
//                        {
//                            new(new SizeInInches(23.5f, 88.5f), YellowMargin: true)
//                        }
//                    )
//                }
//            ),
//            new(
//                MoldName: "H",
//                MoldOuterSize: new SizeInInches(26, 120),
//                MoldCode: "H",
//                Station: "P1",
//                Rows: new List<PanelRow>
//                {
//                    new(
//                        Segments: new List<PanelSegment>
//                        {
//                            new(new SizeInInches(23.5f, 88.5f))
//                        },
//                        TotalWidthInches: 91.5f
//                    )
//                }
//            ),
//            new(
//                MoldName: "C",
//                MoldOuterSize: new SizeInInches(51, 122),
//                MoldCode: "C",
//                Station: "P1",
//                Rows: new List<PanelRow>
//                {
//                    new(
//                        Segments: new List<PanelSegment>
//                        {
//                            new(new SizeInInches(23.5f, 81.5f), RedTrimLineBottom: true),
//                            new(new SizeInInches(23.5f, 81.5f), RedTrimLineTop: true)
//                        },
//                        TotalHeightInches: 112f
//                    ),
//                    new(
//                        Segments: new List<PanelSegment>
//                        {
//                            new(new SizeInInches(23.5f, 27.5f), HasInnerRectangle: false)
//                        },
//                        TotalWidthInches: 23.5f
//                    )
//                }
//            )
//        };

//        var bagRows = new List<BagRow>
//        {
//            new("20\" x 120\"", 20, 20, 3, 10, 3 * 20 * 20 * 1.10f),
//            new("26\" x 120\"", 20, 20, 2, 10, 2 * 20 * 20 * 1.10f),
//            new("51\" x 122\"", 22, 22, 5, 9, 5 * 22 * 22 * 1.09f),
//        };

//        return new PourPlan(
//            Title: "Pour 1",
//            Date: DateOnly.FromDateTime(DateTime.Today),
//            Notes: "NOTE:",
//            Molds: molds,
//            BagsCalc: new BagsCalculation(bagRows, "NUMBER OF BAGS REQUIRED - auto-calculated")
//        );
//    }
//}

// -----------------------------
// PROGRAM ENTRY POINT
// -----------------------------

//class Program
//{
//    static void Main()
//    {
//        //// Initialize QuestPDF license (for development/testing)
//        //QuestPDF.Settings.License = LicenseType.Community;

//        //var data = Sample.Build();
//        //var document = new PourPlanDocument(data);

//        //var outputPath = Path.Combine(Path.GetTempPath(), "pour-plan.pdf");
//        //document.GeneratePdf(outputPath);

//        //Console.WriteLine($"PDF generated successfully at: {outputPath}");
//    }
//}




//namespace MfgDocs.Api.Services.Generators;

//using System;
//using System.Collections.Generic; 
//using System.Linq;

//// ------------------------------------------------------------
//// Pour Plan PDF Generator - .NET 8 + QuestPDF (high-fidelity)
//// ------------------------------------------------------------
//// How to run:
//// 1) dotnet new console -n PourPlanPdf
//// 2) cd PourPlanPdf
//// 3) dotnet add package QuestPDF --version 2024.5.0
////    (or any latest 2024.x version)
//// 4) dotnet add package SkiaSharp --version 2.88.6
//// 5) Replace Program.cs with this file content
//// 6) dotnet run
//// Output: /tmp/pour-plan.pdf (change outputPath below)
//// ------------------------------------------------------------

//using QuestPDF.Fluent;
//using QuestPDF.Helpers;
//using QuestPDF.Infrastructure;
//using QuestPDF.Drawing;
//using QuestPDF.Elements;
//using SkiaSharp;
//using System.Globalization; 

//// -----------------------------
//// DOMAIN MODELS & CALCULATIONS
//// -----------------------------

//public record PourPlan(
//    string Title,
//    DateOnly Date,
//    string Notes,
//    IReadOnlyList<MoldPlan> Molds,
//    BagsCalculation BagsCalc
//);

//public record MoldPlan(
//    string MoldName,
//    SizeInInches MoldOuterSize,   // e.g., 20" x 120"
//    string MoldCode,              // e.g., J, H, C
//    string Station,               // e.g., P1
//    IReadOnlyList<PanelRow> Rows
//);

//public record PanelRow(
//    // Many molds hold one or more panels stacked vertically
//    IReadOnlyList<PanelSegment> Segments,
//    float? TotalWidthInches = null, // optional right-side arrow total
//    float? TotalHeightInches = null // optional bottom-side arrow total
//);

//public record PanelSegment(
//    SizeInInches SegmentSize,
//    bool YellowMargin = false, // draw yellow highlight rectangle around segment
//    bool RedTrimLineTop = false,
//    bool RedTrimLineBottom = false,
//    bool HasInnerRectangle = true // draw inner void
//);

//public record BagsCalculation(
//    IReadOnlyList<BagRow> Rows,
//    string FooterNote
//);

//public record BagRow(
//    string MoldSizeLabel,
//    int PouredWidthInches,
//    int PouredHeightInches,
//    int NumOfPcs,
//    float ExtraMarginPercent,
//    float SqInchTotal // precomputed or computed below
//);

//public readonly record struct SizeInInches(float W, float H)
//{
//    public float AreaSqInch => W * H;
//    public override string ToString() => $"{W:0.#}\" x {H:0.#}\"";
//}

//static class Calc
//{
//    public static float SumSqInch(IEnumerable<BagRow> rows) => rows.Sum(r => r.SqInchTotal);

//    public static string FormatInches(float value)
//    {
//        // Show halves or quarters if present; otherwise integer
//        if (Math.Abs(value - MathF.Round(value)) < 0.001f)
//            return $"{value:0}\"";
//        return $"{value:0.##}\"";
//    }
//}

//// ---------------------------------
//// STYLE TOKENS (colors, dimensions)
//// ---------------------------------

//static class Sty
//{
//    // Base colors from screenshot intent
//    public static readonly string Blue = "#2B62B9";          // title blue
//    public static readonly string Red = "#E53935";           // measurement / trim
//    public static readonly string Yellow = "#FFC107";        // margin highlight
//    public static readonly string Dark = "#333333";          // text / strokes
//    public static readonly string Light = "#F5F7FA";         // table banding
//    public static readonly string Grey = "#95989A";          // subtitle grey

//    // Stroke widths (points)
//    public const float MoldStroke = 1.6f;
//    public const float SegmentStroke = 1.2f;
//    public const float YellowStroke = 2.0f;
//    public const float TrimStroke = 2.0f;

//    // Sizes
//    public const float CornerRadius = 6f;
//    public const float InnerInset = 9f; // inner rectangle inset (pt)

//    // Fonts
//    public static TextStyle H1 => TextStyle.Default.SemiBold().FontSize(26).FontColor(Blue);
//    public static TextStyle H2 => TextStyle.Default.Bold().FontSize(14).FontColor(Red);
//    public static TextStyle Label => TextStyle.Default.Medium().FontSize(10).FontColor(Dark);
//    public static TextStyle Small => TextStyle.Default.FontSize(9).FontColor(Dark);
//    public static TextStyle Tiny => TextStyle.Default.FontSize(8).FontColor(Dark);
//    public static TextStyle WhiteTiny => TextStyle.Default.FontSize(8).FontColor(Colors.White);
//}

//// ---------------------------------
//// CUSTOM PRIMITIVES (arrows, dims)
//// ---------------------------------

//public class Arrow : IElement
//{
//    public SKPoint From { get; init; }
//    public SKPoint To { get; init; }
//    public float Width { get; init; } = 1.6f;
//    public string Color { get; init; } = Sty.Red;
//    public float HeadLength { get; init; } = 10f;
//    public float HeadWidth { get; init; } = 7f;

//    public void Compose(IContainer container)
//    {
//        container.Canvas((canvas, size) =>
//        {
//            using var paint = new SKPaint
//            {
//                IsAntialias = true,
//                Color = SKColor.Parse(Color),
//                StrokeWidth = Width,
//                Style = SKPaintStyle.Stroke
//            };

//            // main line
//            canvas.DrawLine(From, To, paint);

//            // arrow head
//            var v = new SKPoint(To.X - From.X, To.Y - From.Y);
//            var len = MathF.Sqrt(v.X * v.X + v.Y * v.Y);
//            if (len < 0.001f) return;
//            var ux = v.X / len; var uy = v.Y / len;
//            var left = new SKPoint(
//                To.X - HeadLength * ux + HeadWidth * uy,
//                To.Y - HeadLength * uy - HeadWidth * ux);
//            var right = new SKPoint(
//                To.X - HeadLength * ux - HeadWidth * uy,
//                To.Y - HeadLength * uy + HeadWidth * ux);

//            using var fill = new SKPaint
//            {
//                IsAntialias = true,
//                Color = SKColor.Parse(Color),
//                Style = SKPaintStyle.Fill
//            };
//            var path = new SKPath();
//            path.MoveTo(To);
//            path.LineTo(left);
//            path.LineTo(right);
//            path.Close();
//            canvas.DrawPath(path, fill);
//        });
//    }
//}

//public class DimensionLine : IElement
//{
//    // A red dimension line with label (e.g., 23.5" x 27.5")
//    public SKPoint From { get; init; }
//    public SKPoint To { get; init; }
//    public string Label { get; init; } = "";
//    public float LabelPadding { get; init; } = 4f;
//    public string Color { get; init; } = Sty.Red;
//    public float Stroke { get; init; } = Sty.TrimStroke;

//    public void Compose(IContainer container)
//    {
//        container.Canvas((canvas, size) =>
//        {
//            using var paint = new SKPaint
//            {
//                IsAntialias = true,
//                Color = SKColor.Parse(Color),
//                StrokeWidth = Stroke,
//                Style = SKPaintStyle.Stroke
//            };
//            canvas.DrawLine(From, To, paint);

//            if (!string.IsNullOrWhiteSpace(Label))
//            {
//                using var textPaint = new SKPaint
//                {
//                    IsAntialias = true,
//                    Color = SKColors.Black,
//                    TextSize = 10,
//                    Typeface = SKTypeface.Default
//                };
//                // simple background rectangle in white for readability
//                var textWidth = textPaint.MeasureText(Label);
//                var x = (From.X + To.X) / 2 - textWidth / 2;
//                var y = (From.Y + To.Y) / 2 - 2; // offset
//                var rect = new SKRect(x - LabelPadding, y - 10 - LabelPadding, x + textWidth + LabelPadding, y + LabelPadding);
//                using var bg = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill, IsAntialias = true };
//                canvas.DrawRoundRect(rect, 3, 3, bg);
//                canvas.DrawText(Label, x, y - 2, textPaint);
//            }
//        });
//    }
//}

//public class MoldBlock : IElement
//{
//    public Size SizePt { get; init; }              // outer drawing size in points
//    public string MoldLabel { get; init; } = "MOLD NAME";
//    public string Station { get; init; } = "P1";
//    public IReadOnlyList<PanelRow> Rows { get; init; } = new List<PanelRow>();

//    public void Compose(IContainer container)
//    {
//        container.Padding(8).Border(Sty.Dark).BorderThickness(Sty.MoldStroke)
//            .Background(Colors.White).Element(c =>
//            {
//                c.Column(col =>
//                {
//                    col.Spacing(6);
//                    // top header (red label)
//                    col.Item().Border(Sty.Red).BorderThickness(1)
//                        .PaddingVertical(4).PaddingHorizontal(6)
//                        .AlignLeft().Text(MoldLabel).Style(Sty.H2);

//                    // station bubble + mold area stack
//                    col.Item().Row(r =>
//                    {
//                        r.ConstantItem(26).AlignMiddle().Text(Station).Style(TextStyle.Default.Bold().FontSize(12)).
//                            Background("#EFEFEF").PaddingVertical(4).PaddingHorizontal(8).Border("#333").Circle();
//                        r.Spacing(8);
//                        r.RelativeItem().Column(cc =>
//                        {
//                            foreach (var row in Rows)
//                            {
//                                cc.Item().Element(_ => DrawRow(row));
//                                cc.Spacing(6);
//                            }
//                        });
//                    });
//                });
//            });
//    }

//    private IContainer DrawRow(PanelRow row)
//    {
//        return new Container().Height(SizePt.Height / 3).Padding(2).Canvas((canvas, size) =>
//        {
//            float x = 6, y = 6;
//            float width = size.Width - 12;
//            float rowHeight = size.Height - 12;

//            using var stroke = new SKPaint { IsAntialias = true, Color = SKColor.Parse(Sty.Dark), Style = SKPaintStyle.Stroke, StrokeWidth = Sty.SegmentStroke };

//            float currentY = y;
//            foreach (var seg in row.Segments)
//            {
//                float segHeight = rowHeight / row.Segments.Count - 4;
//                var rect = new SKRect(x, currentY, x + width, currentY + segHeight);

//                // base outer rectangle of segment
//                canvas.DrawRoundRect(rect, Sty.CornerRadius, Sty.CornerRadius, stroke);

//                // inner rectangle (void)
//                if (seg.HasInnerRectangle)
//                {
//                    var inner = new SKRect(rect.Left + Sty.InnerInset, rect.Top + Sty.InnerInset, rect.Right - Sty.InnerInset, rect.Bottom - Sty.InnerInset);
//                    canvas.DrawRoundRect(inner, Sty.CornerRadius / 2, Sty.CornerRadius / 2, stroke);
//                }

//                // yellow margin highlight
//                if (seg.YellowMargin)
//                {
//                    using var yellow = new SKPaint { IsAntialias = true, Color = SKColor.Parse(Sty.Yellow), Style = SKPaintStyle.Stroke, StrokeWidth = Sty.YellowStroke };
//                    var yRect = new SKRect(rect.Left + 2, rect.Top + 2, rect.Right - 2, rect.Bottom - 2);
//                    canvas.DrawRoundRect(yRect, Sty.CornerRadius, Sty.CornerRadius, yellow);
//                }

//                // red trim lines
//                if (seg.RedTrimLineTop)
//                {
//                    using var red = new SKPaint { IsAntialias = true, Color = SKColor.Parse(Sty.Red), Style = SKPaintStyle.Stroke, StrokeWidth = Sty.TrimStroke };
//                    canvas.DrawLine(rect.Left + 6, rect.Top + 6, rect.Right - 6, rect.Top + 6, red);
//                }
//                if (seg.RedTrimLineBottom)
//                {
//                    using var red = new SKPaint { IsAntialias = true, Color = SKColor.Parse(Sty.Red), Style = SKPaintStyle.Stroke, StrokeWidth = Sty.TrimStroke };
//                    canvas.DrawLine(rect.Left + 6, rect.Bottom - 6, rect.Right - 6, rect.Bottom - 6, red);
//                }

//                // centered size label
//                using var text = new SKPaint { IsAntialias = true, Color = SKColors.Black, TextSize = 12, Typeface = SKTypeface.Default };
//                var label = $"{seg.SegmentSize.W:0.#}\"x {seg.SegmentSize.H:0.#}\"";
//                var tw = text.MeasureText(label);
//                canvas.DrawText(label, rect.MidX - tw / 2, rect.MidY + 4, text);

//                currentY += segHeight + 4;
//            }

//            // optional dimension arrows (totals)
//            if (row.TotalHeightInches is float totalH)
//            {
//                // vertical arrows both sides and a label
//                var xRight = x + width + 20;
//                var top = y;
//                var bottom = y + rowHeight;
//                var from = new SKPoint(xRight, top);
//                var to = new SKPoint(xRight, bottom);
//                new Arrow { From = from, To = new SKPoint(xRight, top + 12) }.Compose(new Container());
//                new Arrow { From = new SKPoint(xRight, bottom - 12), To = to }.Compose(new Container());
//                // draw the shaft
//                new DimensionLine { From = new SKPoint(xRight, top + 12), To = new SKPoint(xRight, bottom - 12), Label = $"TOTAL LENGTH WITH WASTE = {totalH:0.#}\"" }.Compose(new Container());
//            }

//            if (row.TotalWidthInches is float totalW)
//            {
//                var yBottom = y + rowHeight + 18;
//                new DimensionLine { From = new SKPoint(x, yBottom), To = new SKPoint(x + width, yBottom), Label = $"{totalW:0.#}\"" }.Compose(new Container());
//            }
//        });
//    }
//}

//// -----------------------------
//// DOCUMENT IMPLEMENTATION
//// -----------------------------

//public class PourPlanDocument : IDocument
//{
//    public PourPlan Data { get; }
//    public PourPlanDocument(PourPlan data) => Data = data;

//    public DocumentMetadata GetMetadata() => new DocumentMetadata { Title = Data.Title, Author = "PourPlan Generator", Subject = "Pour Plan", Keywords = "pour plan, molds, pdf" };

//    public void Compose(IDocumentContainer container)
//    {
//        container.Page(page =>
//        {
//            page.Size(PageSizes.A3.Landscape());
//            page.Margin(30);
//            page.DefaultTextStyle(TextStyle.Default.FontSize(11).FontColor(Sty.Dark));
//            page.Header().Element(ComposeHeader);
//            page.Content().Element(ComposeBody);
//            page.Footer().AlignCenter().Text(Data.BagsCalc.FooterNote).Style(Sty.Tiny);
//        });
//    }

//    void ComposeHeader(IContainer container)
//    {
//        container.Row(row =>
//        {
//            row.RelativeItem().Text(Data.Date.ToString("yyyy-MM-dd")).Style(Sty.Small);
//            row.RelativeItem().AlignCenter().Text(Data.Title).Style(Sty.H1).Underline(false).Background("#E9EEF9").PaddingVertical(4).PaddingHorizontal(14).Border("#2B62B9");
//            row.RelativeItem().AlignRight().Text(Data.Notes).Style(Sty.Small);
//        });
//    }

//    void ComposeBody(IContainer container)
//    {
//        container.Column(col =>
//        {
//            col.Spacing(18);

//            // molds area
//            col.Item().Row(r =>
//            {
//                r.Spacing(16);
//                foreach (var mold in Data.Molds)
//                {
//                    r.RelativeItem(1).Element(_ => ComposeMold(mold));
//                }
//            });

//            // bags table
//            col.Item().Element(ComposeBagsTable);
//        });
//    }

//    void ComposeMold(MoldPlan mold)
//    {
//        var label = $"MOLD NAME - {mold.MoldCode} ({mold.MoldOuterSize})";
//        var block = new MoldBlock
//        {
//            SizePt = new Size(400, 260),
//            MoldLabel = label,
//            Station = mold.Station,
//            Rows = mold.Rows
//        };

//        block.Compose(new Container());
//    }

//    void ComposeBagsTable(IContainer container)
//    {
//        var rows = Data.BagsCalc.Rows;

//        container.Column(col =>
//        {
//            col.Item().PaddingTop(8).Text("CALCULATING NUMBER OF BAGS - " + Data.Title.ToUpperInvariant()).Style(TextStyle.Default.SemiBold().FontSize(12));
//            col.Item().Table(t =>
//            {
//                t.ColumnsDefinition(c =>
//                {
//                    c.RelativeColumn(3); // mold size
//                    c.RelativeColumn(2); // poured size W
//                    c.RelativeColumn(2); // poured size H
//                    c.RelativeColumn(1); // pcs
//                    c.RelativeColumn(2); // extra margin
//                    c.RelativeColumn(2); // total sq inch
//                });

//                // header
//                t.Header(h =>
//                {
//                    h.Cell().Background(Sty.Grey).Padding(6).Text("MOLD SIZE").Style(Sty.WhiteTiny);
//                    h.Cell().Background(Sty.Grey).Padding(6).Text("POURED W (INCHES)").Style(Sty.WhiteTiny);
//                    h.Cell().Background(Sty.Grey).Padding(6).Text("POURED H (INCHES)").Style(Sty.WhiteTiny);
//                    h.Cell().Background(Sty.Grey).Padding(6).Text("PCS").Style(Sty.WhiteTiny);
//                    h.Cell().Background(Sty.Grey).Padding(6).Text("EXTRA MARGIN %").Style(Sty.WhiteTiny);
//                    h.Cell().Background(Sty.Grey).Padding(6).Text("TOTAL AREA (SQ.INCH)").Style(Sty.WhiteTiny);
//                });

//                // body
//                for (int i = 0; i < rows.Count; i++)
//                {
//                    var r = rows[i];
//                    var band = i % 2 == 0 ? Colors.White : Sty.Light;
//                    t.Cell().Background(band).Padding(6).Text(r.MoldSizeLabel).Style(Sty.Small);
//                    t.Cell().Background(band).Padding(6).Text(r.PouredWidthInches.ToString(CultureInfo.InvariantCulture));
//                    t.Cell().Background(band).Padding(6).Text(r.PouredHeightInches.ToString(CultureInfo.InvariantCulture));
//                    t.Cell().Background(band).Padding(6).Text(r.NumOfPcs.ToString(CultureInfo.InvariantCulture));
//                    t.Cell().Background(band).Padding(6).Text(r.ExtraMarginPercent.ToString("0.#", CultureInfo.InvariantCulture));
//                    t.Cell().Background(band).Padding(6).Text(r.SqInchTotal.ToString("0", CultureInfo.InvariantCulture));
//                }

//                var total = Calc.SumSqInch(rows);
//                t.Cell().ColumnSpan(5).AlignRight().Padding(6).Text("TOTAL AREA FOR CONCRETE MIX (SQ.INCH)").Style(TextStyle.Default.SemiBold());
//                t.Cell().Padding(6).Text(total.ToString("0"));
//            });
//        });
//    }
//}

//// -----------------------------
//// SAMPLE DATA mirroring screenshot
//// -----------------------------

//static class Sample
//{
//    public static PourPlan Build()
//    {
//        var molds = new List<MoldPlan>
//        {
//            new(
//                MoldName: "J",
//                MoldOuterSize: new SizeInInches(20,120),
//                MoldCode: "J",
//                Station: "P1",
//                Rows: new List<PanelRow>
//                {
//                    new(
//                        Segments: new List<PanelSegment>{
//                            new(new SizeInInches(23.5f,88.5f), YellowMargin:true, RedTrimLineTop:false, RedTrimLineBottom:false)
//                        }
//                    ),
//                }
//            ),
//            new(
//                MoldName: "H",
//                MoldOuterSize: new SizeInInches(26,120),
//                MoldCode: "H",
//                Station: "P1",
//                Rows: new List<PanelRow>
//                {
//                    new(
//                        Segments: new List<PanelSegment>{
//                            new(new SizeInInches(23.5f,88.5f), YellowMargin:false, RedTrimLineTop:false, RedTrimLineBottom:false)
//                        }
//                    )
//                }
//            ),
//            new(
//                MoldName: "C",
//                MoldOuterSize: new SizeInInches(51,122),
//                MoldCode: "C",
//                Station: "P1",
//                Rows: new List<PanelRow>
//                {
//                    new(
//                        Segments: new List<PanelSegment>{
//                            new(new SizeInInches(23.5f,81.5f), YellowMargin:false, RedTrimLineTop:false, RedTrimLineBottom:true),
//                            new(new SizeInInches(23.5f,81.5f), YellowMargin:false, RedTrimLineTop:true, RedTrimLineBottom:false)
//                        },
//                        TotalHeightInches:112f
//                    ),
//                    new(
//                        Segments: new List<PanelSegment>{
//                            new(new SizeInInches(23.5f,27.5f), YellowMargin:false, HasInnerRectangle:false)
//                        },
//                        TotalWidthInches:23.5f
//                    )
//                }
//            )
//        };

//        var bagRows = new List<BagRow>
//        {
//            new("20\" x 120\"", 20, 20, 3, 10, 3*20*20*1.10f),
//            new("26\" x 120\"", 20, 20, 2, 10, 2*20*20*1.10f),
//            new("51\" x 122\"", 22, 22, 5, 9, 5*22*22*1.09f),
//        };

//        return new PourPlan(
//            Title: "Pour 1",
//            Date: DateOnly.FromDateTime(DateTime.Today),
//            Notes: "NOTE:",
//            Molds: molds,
//            BagsCalc: new BagsCalculation(bagRows, FooterNote: "NUMBER OF BAGS REQUIRED - auto-calculated")
//        );
//    }
//}

//// -----------------------------
//// ENTRY POINT
//// -----------------------------

//class Program
//{
//    static void Main()
//    {
//        QuestPDF.Settings.License = LicenseType.Community;

//        var data = Sample.Build();
//        var doc = new PourPlanDocument(data);

//        var outputPath = "/tmp/pour-plan.pdf";
//        doc.GeneratePdf(outputPath);
//        Console.WriteLine($"Generated: {outputPath}");
//    }
//}












////using iTextSharp.text.pdf;
////using iTextSharp.text;
////using MfgDocs.Api.Models;

////namespace MfgDocs.Api.Services.Generators;

////    using iTextSharp.text;
////using iTextSharp.text.pdf;
////using System;
////using System.Collections.Generic;
////using System.IO;
////using System.Threading.Tasks;
////using System.Linq;

////public class AdvancedPourPlanGenerator
////{
////    private static readonly BaseColor BLUE_HEADER = new BaseColor(0, 102, 204);
////    private static readonly BaseColor RED_ACCENT = new BaseColor(220, 20, 20);
////    private static readonly BaseColor YELLOW_LINE = new BaseColor(255, 255, 0);
////    private static readonly BaseColor LIGHT_GRAY = new BaseColor(240, 240, 240);
////    private static readonly BaseColor DARK_GRAY = new BaseColor(128, 128, 128);
////    private static readonly BaseColor WHITE = BaseColor.WHITE;
////    private static readonly BaseColor BLACK = BaseColor.BLACK;

////    public async Task<byte[]> GeneratePourPlanPdf(PourPlanRequest2 request)
////    {
////        using var memoryStream = new MemoryStream();
////        var document = new Document(PageSize.A4.Rotate(), 15, 15, 15, 15);
////        var writer = PdfWriter.GetInstance(document, memoryStream);

////        document.Open();
////        var cb = writer.DirectContent;

////        // Generate main content
////        GenerateMainLayout(document, cb, request);

////        document.Close();
////        return memoryStream.ToArray();
////    }

////    private void GenerateMainLayout(Document document, PdfContentByte cb, PourPlanRequest2 request)
////    {
////        // Title
////        GenerateTitle(document, request);
////        document.Add(new Paragraph(" ", FontFactory.GetFont(FontFactory.HELVETICA, 8)));

////        // Main container table
////        var mainTable = new PdfPTable(2) { WidthPercentage = 100 };
////        mainTable.SetWidths(new float[] { 1f, 1f });

////        // Left column - Schedule blocks
////        var leftCell = GenerateScheduleColumn(request.ScheduleBlocks);

////        // Right column - Mold details with advanced graphics
////        var rightCell = GenerateMoldColumn(cb, request);

////        mainTable.AddCell(leftCell);
////        mainTable.AddCell(rightCell);

////        document.Add(mainTable);
////    }

////    private void GenerateTitle(Document document, PourPlanRequest2 request)
////    {
////        var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BLUE_HEADER);
////        var title = new Paragraph($"{request.Day.ToUpper()} POURING PLAN", titleFont);
////        title.Alignment = Element.ALIGN_CENTER;
////        title.SpacingAfter = 10f;
////        document.Add(title);
////    }

////    private PdfPCell GenerateScheduleColumn(List<ScheduleBlock> scheduleBlocks)
////    {
////        var leftCell = new PdfPCell();
////        leftCell.Border = Rectangle.BOX;
////        leftCell.BorderWidth = 2f;
////        leftCell.Padding = 8;

////        foreach (var block in scheduleBlocks)
////        {
////            var scheduleTable = CreateDetailedScheduleBlock(block);
////            leftCell.AddElement(scheduleTable);
////            leftCell.AddElement(new Paragraph(" ", FontFactory.GetFont(FontFactory.HELVETICA, 4)));
////        }

////        return leftCell;
////    }

////    private PdfPCell GenerateMoldColumn(PdfContentByte cb, PourPlanRequest2 request)
////    {
////        var rightCell = new PdfPCell();
////        rightCell.Border = Rectangle.BOX;
////        rightCell.BorderWidth = 2f;
////        rightCell.Padding = 8;

////        // Header section
////        var headerTable = CreatePourHeaderSection(request);
////        rightCell.AddElement(headerTable);

////        // Mold sections with advanced graphics
////        foreach (var mold in request.Molds)
////        {
////            var moldTable = CreateAdvancedMoldSection(mold, cb);
////            rightCell.AddElement(moldTable);
////            rightCell.AddElement(new Paragraph(" ", FontFactory.GetFont(FontFactory.HELVETICA, 6)));
////        }

////        // Calculation table
////        if (request.CalculationData != null)
////        {
////            var calcTable = CreateAdvancedCalculationTable(request.CalculationData);
////            rightCell.AddElement(calcTable);
////        }

////        return rightCell;
////    }

////    private PdfPTable CreateDetailedScheduleBlock(ScheduleBlock block)
////    {
////        var table = new PdfPTable(1) { WidthPercentage = 100 };

////        // Enhanced header with better styling
////        var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, WHITE);
////        var headerCell = new PdfPCell(new Phrase(block.TimeSlot, headerFont));
////        headerCell.BackgroundColor = DARK_GRAY;
////        headerCell.HorizontalAlignment = Element.ALIGN_CENTER;
////        headerCell.Padding = 5;
////        headerCell.Border = Rectangle.BOX;
////        headerCell.BorderWidth = 1.5f;
////        table.AddCell(headerCell);

////        // Enhanced content rows with better visual hierarchy
////        foreach (var item in block.Items)
////        {
////            var contentTable = new PdfPTable(4);
////            contentTable.SetWidths(new float[] { 0.4f, 2.2f, 0.8f, 0.6f });

////            // Sequence number with red background
////            var seqCell = CreateStyledCell(item.Sequence.ToString(),
////                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8, WHITE), RED_ACCENT);
////            seqCell.HorizontalAlignment = Element.ALIGN_CENTER;

////            // Description with proper formatting
////            var descCell = CreateStyledCell(item.Description,
////                FontFactory.GetFont(FontFactory.HELVETICA, 8, BLACK), WHITE);

////            // Duration with light background
////            var durCell = CreateStyledCell(item.Duration,
////                FontFactory.GetFont(FontFactory.HELVETICA, 8, BLACK), LIGHT_GRAY);
////            durCell.HorizontalAlignment = Element.ALIGN_CENTER;

////            // Status
////            var statusCell = CreateStyledCell(item.Status,
////                FontFactory.GetFont(FontFactory.HELVETICA, 7, BLACK), WHITE);
////            statusCell.HorizontalAlignment = Element.ALIGN_CENTER;

////            contentTable.AddCell(seqCell);
////            contentTable.AddCell(descCell);
////            contentTable.AddCell(durCell);
////            contentTable.AddCell(statusCell);

////            var contentCell = new PdfPCell(contentTable);
////            contentCell.Border = Rectangle.NO_BORDER;
////            contentCell.Padding = 1;
////            table.AddCell(contentCell);
////        }

////        return table;
////    }

////    private PdfPTable CreatePourHeaderSection(PourPlanRequest2 request)
////    {
////        var headerTable = new PdfPTable(3);
////        headerTable.SetWidths(new float[] { 1f, 1f, 1f });
////        headerTable.WidthPercentage = 100;

////        // Date section
////        var dateCell = new PdfPCell(new Phrase($"DATE: {request.Date:MM/dd/yyyy}",
////            FontFactory.GetFont(FontFactory.HELVETICA, 10, BLACK)));
////        dateCell.Border = Rectangle.BOX;
////        dateCell.Padding = 5;

////        // Pour 1 header with blue background
////        var pourHeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, WHITE);
////        var pourCell = new PdfPCell(new Phrase("POUR 1", pourHeaderFont));
////        pourCell.BackgroundColor = BLUE_HEADER;
////        pourCell.HorizontalAlignment = Element.ALIGN_CENTER;
////        pourCell.Border = Rectangle.BOX;
////        pourCell.Padding = 8;

////        // Notes section
////        var notesCell = new PdfPCell(new Phrase("NOTE:",
////            FontFactory.GetFont(FontFactory.HELVETICA, 10, BLACK)));
////        notesCell.Border = Rectangle.BOX;
////        notesCell.Padding = 5;

////        headerTable.AddCell(dateCell);
////        headerTable.AddCell(pourCell);
////        headerTable.AddCell(notesCell);

////        return headerTable;
////    }

////    private PdfPTable CreateAdvancedMoldSection(MoldDetails mold, PdfContentByte cb)
////    {
////        var table = new PdfPTable(1) { WidthPercentage = 100 };

////        // Enhanced mold name header
////        var nameFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, WHITE);
////        var nameCell = new PdfPCell(new Phrase($"MOLD NAME - {mold.Name}", nameFont));
////        nameCell.BackgroundColor = RED_ACCENT;
////        nameCell.HorizontalAlignment = Element.ALIGN_CENTER;
////        nameCell.Padding = 6;
////        nameCell.Border = Rectangle.BOX;
////        nameCell.BorderWidth = 2f;
////        table.AddCell(nameCell);

////        // Advanced mold diagram with precise measurements
////        var diagramCell = CreateMoldDiagramCell(mold);
////        table.AddCell(diagramCell);

////        return table;
////    }

////    private PdfPCell CreateMoldDiagramCell(MoldDetails mold)
////    {
////        var diagramCell = new PdfPCell();
////        diagramCell.FixedHeight = 180;
////        diagramCell.Border = Rectangle.BOX;
////        diagramCell.BorderWidth = 2f;
////        diagramCell.Padding = 10;

////        // Create nested table for complex layout
////        var diagramTable = new PdfPTable(1) { WidthPercentage = 100 };

////        // P1 identifier
////        var p1Cell = new PdfPCell(new Phrase("P1",
////            FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BLACK)));
////        p1Cell.HorizontalAlignment = Element.ALIGN_CENTER;
////        p1Cell.Border = Rectangle.BOX;
////        p1Cell.BorderWidth = 1f;
////        p1Cell.FixedHeight = 25;
////        p1Cell.BackgroundColor = LIGHT_GRAY;
////        diagramTable.AddCell(p1Cell);

////        // Measurement sections with enhanced visual details
////        foreach (var section in mold.Sections)
////        {
////            var measurementTable = CreateMeasurementSection(section);
////            var measurementCell = new PdfPCell(measurementTable);
////            measurementCell.Border = Rectangle.NO_BORDER;
////            measurementCell.Padding = 5;
////            diagramTable.AddCell(measurementCell);
////        }

////        diagramCell.AddElement(diagramTable);
////        return diagramCell;
////    }

////    private PdfPTable CreateMeasurementSection(MoldSection section)
////    {
////        var table = new PdfPTable(3);
////        table.SetWidths(new float[] { 1f, 2f, 1f });
////        table.WidthPercentage = 100;

////        // Dimension display
////        var dimFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BLACK);
////        var dimensionCell = new PdfPCell(new Phrase($"{section.Width}\" x {section.Height}\"", dimFont));
////        dimensionCell.HorizontalAlignment = Element.ALIGN_CENTER;
////        dimensionCell.VerticalAlignment = Element.ALIGN_MIDDLE;
////        dimensionCell.Border = Rectangle.BOX;
////        dimensionCell.BorderWidth = 1.5f;
////        dimensionCell.Padding = 8;
////        dimensionCell.FixedHeight = 40;

////        // Visual separator/arrow area
////        var arrowCell = new PdfPCell();
////        arrowCell.Border = Rectangle.NO_BORDER;
////        arrowCell.HorizontalAlignment = Element.ALIGN_CENTER;

////        // Add arrow symbol
////        var arrowPhrase = new Phrase("→", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, RED_ACCENT));
////        arrowCell.AddElement(arrowPhrase);

////        // Total measurement with red highlighting
////        var totalFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, RED_ACCENT);
////        var totalCell = new PdfPCell(new Phrase($"={section.TotalMeasurement}\"", totalFont));
////        totalCell.HorizontalAlignment = Element.ALIGN_CENTER;
////        totalCell.VerticalAlignment = Element.ALIGN_MIDDLE;
////        totalCell.Border = Rectangle.BOX;
////        totalCell.BorderWidth = 2f;
////        totalCell.BorderColor = RED_ACCENT;
////        totalCell.Padding = 8;
////        totalCell.FixedHeight = 40;

////        table.AddCell(dimensionCell);
////        table.AddCell(arrowCell);
////        table.AddCell(totalCell);

////        return table;
////    }

////    private PdfPTable CreateAdvancedCalculationTable(CalculationData data)
////    {
////        var table = new PdfPTable(8);
////        table.WidthPercentage = 100;
////        table.SetWidths(new float[] { 1.2f, 1f, 1.3f, 1f, 1.3f, 1f, 1.2f, 1.5f });

////        // Enhanced headers with better formatting
////        var headers = new[] {
////            "MOLD SIZE",
////            "SQ. OF FACE",
////            "POURED SIZE (INCHES)",
////            "SQ. OF FACE",
////            "POURED SIZE (INCHES)",
////            "SQ. OF FACE",
////            "EXTRA MARGIN TO POUR",
////            "TOTAL PRODUCT POURED (SQ.IN/SQ.M)"
////        };

////        foreach (var header in headers)
////        {
////            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 7, WHITE);
////            var headerCell = new PdfPCell(new Phrase(header, headerFont));
////            headerCell.BackgroundColor = DARK_GRAY;
////            headerCell.HorizontalAlignment = Element.ALIGN_CENTER;
////            headerCell.VerticalAlignment = Element.ALIGN_MIDDLE;
////            headerCell.Padding = 4;
////            headerCell.Border = Rectangle.BOX;
////            headerCell.BorderWidth = 1f;
////            table.AddCell(headerCell);
////        }

////        // Enhanced data rows with alternating colors and better formatting
////        bool alternateRow = false;
////        foreach (var row in data.Rows)
////        {
////            var bgColor = alternateRow ? LIGHT_GRAY : WHITE;

////            table.AddCell(CreateCalculationCell(row.MoldSize, bgColor, true));
////            table.AddCell(CreateCalculationCell(row.SqFace1.ToString(), bgColor));
////            table.AddCell(CreateCalculationCell(row.PouredSize1, bgColor));
////            table.AddCell(CreateCalculationCell(row.SqFace2.ToString(), bgColor));
////            table.AddCell(CreateCalculationCell(row.PouredSize2, bgColor));
////            table.AddCell(CreateCalculationCell(row.SqFace3.ToString(), bgColor));
////            table.AddCell(CreateCalculationCell(row.ExtraMargin.ToString(), bgColor));
////            table.AddCell(CreateCalculationCell(row.TotalProduct.ToString(), RED_ACCENT, true, WHITE));

////            alternateRow = !alternateRow;
////        }

////        // Add total row
////        AddCalculationTotalRow(table, data);

////        return table;
////    }

////    private void AddCalculationTotalRow(PdfPTable table, CalculationData data)
////    {
////        var totalFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8, WHITE);

////        // Total label
////        var totalLabelCell = new PdfPCell(new Phrase("TOTAL", totalFont));
////        totalLabelCell.BackgroundColor = DARK_GRAY;
////        totalLabelCell.HorizontalAlignment = Element.ALIGN_CENTER;
////        totalLabelCell.Padding = 5;
////        totalLabelCell.Border = Rectangle.BOX;
////        totalLabelCell.BorderWidth = 2f;
////        table.AddCell(totalLabelCell);

////        // Empty cells for spacing
////        for (int i = 0; i < 6; i++)
////        {
////            var emptyCell = new PdfPCell(new Phrase(""));
////            emptyCell.BackgroundColor = DARK_GRAY;
////            emptyCell.Border = Rectangle.BOX;
////            table.AddCell(emptyCell);
////        }

////        // Grand total
////        var grandTotal = data.Rows.Sum(r => r.TotalProduct);
////        var grandTotalCell = new PdfPCell(new Phrase(grandTotal.ToString(),
////            FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, WHITE)));
////        grandTotalCell.BackgroundColor = RED_ACCENT;
////        grandTotalCell.HorizontalAlignment = Element.ALIGN_CENTER;
////        grandTotalCell.Padding = 5;
////        grandTotalCell.Border = Rectangle.BOX;
////        grandTotalCell.BorderWidth = 2f;
////        table.AddCell(grandTotalCell);
////    }

////    private PdfPCell CreateStyledCell(string text, Font font, BaseColor backgroundColor)
////    {
////        var cell = new PdfPCell(new Phrase(text, font));
////        cell.BackgroundColor = backgroundColor;
////        cell.Padding = 4;
////        cell.Border = Rectangle.BOX;
////        cell.BorderWidth = 0.5f;
////        cell.VerticalAlignment = Element.ALIGN_MIDDLE;
////        return cell;
////    }

////    private PdfPCell CreateCalculationCell(string text, BaseColor backgroundColor,
////        bool isBold = false, BaseColor? textColor = null)
////    {
////        var font = FontFactory.GetFont(
////            isBold ? FontFactory.HELVETICA_BOLD : FontFactory.HELVETICA,
////            8,
////            textColor ?? BLACK);

////        var cell = new PdfPCell(new Phrase(text, font));
////        cell.BackgroundColor = backgroundColor;
////        cell.HorizontalAlignment = Element.ALIGN_CENTER;
////        cell.VerticalAlignment = Element.ALIGN_MIDDLE;
////        cell.Padding = 3;
////        cell.Border = Rectangle.BOX;
////        cell.BorderWidth = 0.5f;
////        return cell;
////    }
////}

////// Enhanced helper class for drawing custom graphics
////public class MoldDiagramDrawer
////{
////    public static void DrawYellowConnectorLines(PdfContentByte cb, float x, float y, float width, float height)
////    {
////        cb.SaveState();
////        cb.SetColorStroke(new BaseColor(255, 255, 0)); // Yellow
////        cb.SetLineWidth(2f);

////        // Draw connecting lines based on mold layout
////        cb.MoveTo(x, y);
////        cb.LineTo(x + width, y + height);
////        cb.Stroke();

////        cb.RestoreState();
////    }

////    public static void DrawDirectionalArrows(PdfContentByte cb, float x, float y, string direction = "right")
////    {
////        cb.SaveState();
////        cb.SetColorFill(BaseColor.RED);

////        // Draw arrow shape based on direction
////        switch (direction.ToLower())
////        {
////            case "right":
////                DrawRightArrow(cb, x, y);
////                break;
////            case "down":
////                DrawDownArrow(cb, x, y);
////                break;
////        }

////        cb.RestoreState();
////    }

////    private static void DrawRightArrow(PdfContentByte cb, float x, float y)
////    {
////        cb.MoveTo(x, y);
////        cb.LineTo(x + 10, y + 5);
////        cb.LineTo(x, y + 10);
////        cb.LineTo(x + 2, y + 5);
////        cb.ClosePath();
////        cb.Fill();
////    }

////    private static void DrawDownArrow(PdfContentByte cb, float x, float y)
////    {
////        cb.MoveTo(x, y);
////        cb.LineTo(x + 5, y + 10);
////        cb.LineTo(x + 10, y);
////        cb.LineTo(x + 5, y + 2);
////        cb.ClosePath();
////        cb.Fill();
////    }
////}