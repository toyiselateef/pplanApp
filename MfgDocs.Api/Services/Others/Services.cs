using DocumentFormat.OpenXml.Office2010.PowerPoint;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MfgDocs.Api.Extensions;
using MfgDocs.Api.Models;
//using Microsoft.Playwright;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
// using PuppeteerSharp;
// using PuppeteerSharp.Media;
using RazorEngine;
using Scriban;
using Scriban.Runtime;

namespace MfgDocs.Api.Services.Others;

public interface IDocumentService
{
    Task<byte[]> GenerateAdvancedPourPlan(PourPlanRequest2 request);
    Task<byte[]> GenerateWorkOrderWithCalculations(WorkOrderRequest2 request);
    Task<byte[]> GenerateExcelStyleSchedule(DailyScheduleRequest request);
}

public class DocumentService : IDocumentService
{
    public async Task<byte[]> GenerateAdvancedPourPlan(PourPlanRequest2 request)
    {
        using var memoryStream = new MemoryStream();
        var document = new Document(PageSize.A3.Rotate(), 15, 15, 15, 15); // A3 for better layout
        var writer = PdfWriter.GetInstance(document, memoryStream);

        document.Open();

        // Advanced pour plan with precise AutoCAD-style layout
        var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.BLUE);
        var title = new Paragraph($"{request.Day.ToUpper()} POURING PLAN", titleFont);
        title.Alignment = Element.ALIGN_CENTER;
        title.SpacingAfter = 20;
        document.Add(title);

        // Create main container table
        var mainTable = new PdfPTable(3) { WidthPercentage = 100 };
        mainTable.SetWidths(new float[] { 1.2f, 0.1f, 1.8f }); // Left schedule, spacer, right details

        // Left side - Enhanced schedule blocks with precise timing
        var leftCell = new PdfPCell();
        leftCell.Border = Rectangle.BOX;
        leftCell.BorderWidth = 2;
        leftCell.Padding = 10;

        // Create two-column layout for schedule blocks
        var scheduleContainer = new PdfPTable(2) { WidthPercentage = 100 };
        scheduleContainer.SetWidths(new float[] { 1f, 1f });

        var leftColumnBlocks = request.ScheduleBlocks.Take(request.ScheduleBlocks.Count / 2).ToList();
        var rightColumnBlocks = request.ScheduleBlocks.Skip(request.ScheduleBlocks.Count / 2).ToList();

        // Left column of schedule blocks
        var leftColumnCell = new PdfPCell();
        leftColumnCell.Border = Rectangle.NO_BORDER;
        leftColumnCell.Padding = 5;

        foreach (var block in leftColumnBlocks)
        {
            var blockTable = CreateEnhancedScheduleBlock(block);
            leftColumnCell.AddElement(blockTable);
            leftColumnCell.AddElement(new Paragraph(" "));
        }

        // Right column of schedule blocks
        var rightColumnCell = new PdfPCell();
        rightColumnCell.Border = Rectangle.NO_BORDER;
        rightColumnCell.Padding = 5;

        foreach (var block in rightColumnBlocks)
        {
            var blockTable = CreateEnhancedScheduleBlock(block);
            rightColumnCell.AddElement(blockTable);
            rightColumnCell.AddElement(new Paragraph(" "));
        }

        scheduleContainer.AddCell(leftColumnCell);
        scheduleContainer.AddCell(rightColumnCell);
        leftCell.AddElement(scheduleContainer);

        // Spacer
        var spacerCell = new PdfPCell();
        spacerCell.Border = Rectangle.NO_BORDER;

        // Right side - Detailed mold layouts with technical drawings
        var rightCell = new PdfPCell();
        rightCell.Border = Rectangle.BOX;
        rightCell.BorderWidth = 2;
        rightCell.Padding = 10;

        // Pour header with enhanced styling
        var pourHeaderTable = new PdfPTable(1) { WidthPercentage = 100 };
        var pourHeaderCell = new PdfPCell(new Phrase("POUR 1", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.BLUE)));
        pourHeaderCell.BackgroundColor = new BaseColor(220, 220, 220);
        pourHeaderCell.HorizontalAlignment = Element.ALIGN_CENTER;
        pourHeaderCell.Padding = 8;
        pourHeaderCell.Border = Rectangle.BOX;
        pourHeaderCell.BorderWidth = 2;
        pourHeaderTable.AddCell(pourHeaderCell);
        rightCell.AddElement(pourHeaderTable);
        rightCell.AddElement(new Paragraph(" "));

        // Enhanced mold sections with technical precision
        foreach (var mold in request.Molds)
        {
            var moldSection = CreateTechnicalMoldSection(mold);
            rightCell.AddElement(moldSection);
            rightCell.AddElement(new Paragraph(" "));
        }

        mainTable.AddCell(leftCell);
        mainTable.AddCell(spacerCell);
        mainTable.AddCell(rightCell);
        document.Add(mainTable);

        // Enhanced calculation table at bottom
        if (request.CalculationData != null)
        {
            document.Add(new Paragraph(" "));
            var enhancedCalcTable = CreateEnhancedCalculationTable(request.CalculationData);
            document.Add(enhancedCalcTable);
        }

        document.Close();
        return memoryStream.ToArray();
    }

    private PdfPTable CreateEnhancedScheduleBlock(ScheduleBlock block)
    {
        var table = new PdfPTable(1) { WidthPercentage = 100 };
        table.SpacingAfter = 5;

        // Enhanced header with gradient-like effect
        var headerCell = new PdfPCell(new Phrase(block.TimeSlot, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.WHITE)));
        headerCell.BackgroundColor = new BaseColor(70, 130, 180); // Steel blue
        headerCell.HorizontalAlignment = Element.ALIGN_CENTER;
        headerCell.Padding = 5;
        headerCell.Border = Rectangle.BOX;
        table.AddCell(headerCell);

        // Content with alternating row colors
        for (int i = 0; i < block.Items.Count; i++)
        {
            var item = block.Items[i];
            var contentTable = new PdfPTable(4);
            contentTable.SetWidths(new float[] { 0.3f, 2.5f, 0.8f, 0.8f });
            var rowColor = i % 2 == 0 ? BaseColor.WHITE : new BaseColor(248, 248, 248);
            var seqColor = BaseColor.RED;

            contentTable.AddCell(CreateStyledCell(item.Sequence.ToString(), seqColor, BaseColor.WHITE, true));
            contentTable.AddCell(CreateStyledCell(item.Description, rowColor, BaseColor.BLACK, false));
            contentTable.AddCell(CreateStyledCell(item.Duration, rowColor, BaseColor.BLACK, false));
            contentTable.AddCell(CreateStyledCell(item.Status, rowColor, BaseColor.BLACK, false));

            var contentCell = new PdfPCell(contentTable);
            contentCell.Border = Rectangle.NO_BORDER;
            table.AddCell(contentCell);
        }

        return table;
    }

    private PdfPTable CreateTechnicalMoldSection(MoldDetails mold)
    {
        var container = new PdfPTable(1) { WidthPercentage = 100 };
        container.SpacingAfter = 10;

        // Mold name header with red background (matching screenshot)
        var nameCell = new PdfPCell(new Phrase($"MOLD NAME - {mold.Name}", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, BaseColor.WHITE)));
        nameCell.BackgroundColor = BaseColor.RED;
        nameCell.HorizontalAlignment = Element.ALIGN_CENTER;
        nameCell.Padding = 6;
        nameCell.Border = Rectangle.BOX;
        nameCell.BorderWidth = 1;
        container.AddCell(nameCell);

        // Technical drawing area
        var drawingCell = new PdfPCell();
        drawingCell.FixedHeight = 150;
        drawingCell.Border = Rectangle.BOX;
        drawingCell.Padding = 10;
        drawingCell.BackgroundColor = new BaseColor(255, 255, 240); // Light yellow background

        // Create technical layout with precise measurements
        var technicalTable = CreateTechnicalLayout(mold);
        drawingCell.AddElement(technicalTable);

        container.AddCell(drawingCell);
        return container;
    }

    private PdfPTable CreateTechnicalLayout(MoldDetails mold)
    {
        var layout = new PdfPTable(3) { WidthPercentage = 100 };
        layout.SetWidths(new float[] { 1f, 2f, 1f });

        foreach (var section in mold.Sections)
        {
            // Left dimension
            var leftDimCell = new PdfPCell(new Phrase($"{section.Width}\"", FontFactory.GetFont(FontFactory.HELVETICA, 9)));
            leftDimCell.HorizontalAlignment = Element.ALIGN_CENTER;
            leftDimCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            leftDimCell.Border = Rectangle.NO_BORDER;
            layout.AddCell(leftDimCell);

            // Center section with border (representing the mold piece)
            var centerCell = new PdfPCell(new Phrase($"{section.Width}\" x {section.Height}\"",
                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));
            centerCell.HorizontalAlignment = Element.ALIGN_CENTER;
            centerCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            centerCell.Border = Rectangle.BOX;
            centerCell.BorderWidth = 2;
            centerCell.Padding = 15;
            centerCell.BackgroundColor = BaseColor.WHITE;
            layout.AddCell(centerCell);

            // Right dimension with arrow and measurement
            var rightDimCell = new PdfPCell();
            rightDimCell.Border = Rectangle.NO_BORDER;
            rightDimCell.HorizontalAlignment = Element.ALIGN_CENTER;
            rightDimCell.VerticalAlignment = Element.ALIGN_MIDDLE;

            var dimText = new Paragraph();
            dimText.Add(new Chunk("→ ", FontFactory.GetFont(FontFactory.HELVETICA, 12)));
            dimText.Add(new Chunk($"={section.TotalMeasurement}\"", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.RED)));
            rightDimCell.AddElement(dimText);
            layout.AddCell(rightDimCell);
        }
        return layout;
    }

    private PdfPTable CreateEnhancedCalculationTable(CalculationData data)
    {
        var table = new PdfPTable(8) { WidthPercentage = 100 };
        table.SetWidths(new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1.2f });
        table.SpacingBefore = 10;

        // Title row
        var titleCell = new PdfPCell(new Phrase("CALCULATION FOR MOLDS ON POUR - POUR 1",
            FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.WHITE)));
        titleCell.BackgroundColor = new BaseColor(70, 70, 70);
        titleCell.HorizontalAlignment = Element.ALIGN_CENTER;
        titleCell.Colspan = 8;
        titleCell.Padding = 5;
        table.AddCell(titleCell);

        // Enhanced headers with color coding
        var headers = new[] { "MOLD SIZE", "SQ. OF FACE", "POURED SIZE (INCHES)", "SQ. OF FACE", "POURED SIZE (INCHES)", "SQ. OF FACE", "EXTRA MARGIN TO POUR", "TOTAL PRODUCT POURED (SQ.IN/SQ.M)" };
        var headerColors = new BaseColor[] {
                new BaseColor(180, 180, 255), new BaseColor(255, 180, 180), new BaseColor(180, 255, 180),
                new BaseColor(255, 180, 180), new BaseColor(180, 255, 180), new BaseColor(255, 180, 180),
                new BaseColor(180, 255, 255), new BaseColor(255, 255, 180)
            };

        for (int i = 0; i < headers.Length; i++)
        {
            var headerCell = new PdfPCell(new Phrase(headers[i], FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8)));
            headerCell.BackgroundColor = headerColors[i];
            headerCell.HorizontalAlignment = Element.ALIGN_CENTER;
            headerCell.Padding = 4;
            headerCell.Border = Rectangle.BOX;
            table.AddCell(headerCell);
        }

        // Data rows with enhanced formatting
        foreach (var row in data.Rows)
        {
            table.AddCell(CreateCalculationCell(row.MoldSize, BaseColor.WHITE));
            table.AddCell(CreateCalculationCell(row.SqFace1.ToString(), BaseColor.WHITE));
            table.AddCell(CreateCalculationCell(row.PouredSize1, BaseColor.WHITE));
            table.AddCell(CreateCalculationCell(row.SqFace2.ToString(), BaseColor.WHITE));
            table.AddCell(CreateCalculationCell(row.PouredSize2, BaseColor.WHITE));
            table.AddCell(CreateCalculationCell(row.SqFace3.ToString(), BaseColor.WHITE));
            table.AddCell(CreateCalculationCell(row.ExtraMargin.ToString(), BaseColor.WHITE));
            table.AddCell(CreateCalculationCell(row.TotalProduct.ToString(), new BaseColor(200, 255, 200))); // Light green for totals
        }

        return table;
    }

    public async Task<byte[]> GenerateWorkOrderWithCalculations(WorkOrderRequest2 request)
    {
        using var memoryStream = new MemoryStream();
        var document = new Document(PageSize.A4, 20, 20, 20, 20);
        var writer = PdfWriter.GetInstance(document, memoryStream);

        document.Open();

        // Enhanced header section
        var headerTable = new PdfPTable(3) { WidthPercentage = 100 };
        headerTable.SetWidths(new float[] { 2f, 1f, 2f });

        // Company logo and info
        var companyCell = new PdfPCell();
        companyCell.Border = Rectangle.NO_BORDER;
        companyCell.Padding = 5;

        var logoFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.RED);
        var logoTable = new PdfPTable(2);
        logoTable.SetWidths(new float[] { 0.3f, 1.7f });

        var logoSymbolCell = new PdfPCell(new Phrase("🏠", FontFactory.GetFont(FontFactory.HELVETICA, 20)));
        logoSymbolCell.Border = Rectangle.NO_BORDER;
        logoSymbolCell.HorizontalAlignment = Element.ALIGN_CENTER;
        logoSymbolCell.VerticalAlignment = Element.ALIGN_MIDDLE;

        var logoTextCell = new PdfPCell(new Phrase("MFG PRECAST", logoFont));
        logoTextCell.Border = Rectangle.NO_BORDER;
        logoTextCell.VerticalAlignment = Element.ALIGN_MIDDLE;

        logoTable.AddCell(logoSymbolCell);
        logoTable.AddCell(logoTextCell);
        companyCell.AddElement(logoTable);

        companyCell.AddElement(new Paragraph($"PO Box 730-71, Magaliesmã", FontFactory.GetFont(FontFactory.HELVETICA, 9)));
        companyCell.AddElement(new Paragraph($"Burlington, ON L7T 2H0", FontFactory.GetFont(FontFactory.HELVETICA, 9)));
        companyCell.AddElement(new Paragraph($"Phone: {request.CompanyInfo.Phone}", FontFactory.GetFont(FontFactory.HELVETICA, 9)));
        companyCell.AddElement(new Paragraph($"Email: {request.CompanyInfo.Email}", FontFactory.GetFont(FontFactory.HELVETICA, 9)));

        // Spacer
        var spacer = new PdfPCell();
        spacer.Border = Rectangle.NO_BORDER;

        // Work order info with enhanced styling
        var orderInfoCell = new PdfPCell();
        orderInfoCell.Border = Rectangle.NO_BORDER;
        orderInfoCell.Padding = 5;

        var workOrderTitle = new Paragraph("WORK ORDER", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.GRAY));
        workOrderTitle.Alignment = Element.ALIGN_RIGHT;
        orderInfoCell.AddElement(workOrderTitle);

        // Info boxes with distinct styling
        var infoBoxes = new PdfPTable(1) { WidthPercentage = 100 };

        infoBoxes.AddCell(CreateInfoBox("ORDER DATE", request.OrderDate.ToString("dd-MMM-yyyy"), new BaseColor(255, 200, 200)));
        infoBoxes.AddCell(CreateInfoBox("PURCHASE ORDER", request.PurchaseOrder, new BaseColor(200, 200, 255)));
        infoBoxes.AddCell(CreateInfoBox("COMPANY", request.Company, new BaseColor(200, 200, 255)));
        infoBoxes.AddCell(CreateInfoBox("CONTACT", request.Contact, new BaseColor(200, 200, 255)));

        orderInfoCell.AddElement(infoBoxes);

        headerTable.AddCell(companyCell);
        headerTable.AddCell(spacer);
        headerTable.AddCell(orderInfoCell);
        document.Add(headerTable);

        document.Add(new Paragraph(" "));

        // Enhanced builder/site information
        var builderTable = new PdfPTable(4) { WidthPercentage = 100 };
        builderTable.SetWidths(new float[] { 2f, 1f, 1f, 1f });

        // Headers
        builderTable.AddCell(CreateHeaderCell("BUILDER / SITE / CITY"));
        builderTable.AddCell(CreateHeaderCell("BLK NO."));
        builderTable.AddCell(CreateHeaderCell("COMPANY"));
        builderTable.AddCell(CreateHeaderCell("CONTACT"));

        foreach (var site in request.Sites)
        {
            builderTable.AddCell(CreateDataCell(site.Builder));
            builderTable.AddCell(CreateDataCell(site.BlockNumber));
            builderTable.AddCell(CreateDataCell(site.Company));
            builderTable.AddCell(CreateDataCell(site.Contact));
            builderTable.AddCell(CreateDataCell(site.Location));
            builderTable.AddCell(CreateDataCell($"LOT NO.\n{site.LotNumber}"));
            builderTable.AddCell(CreateDataCell(""));
            builderTable.AddCell(CreateDataCell(""));
        }

        document.Add(builderTable);
        document.Add(new Paragraph(" "));

        // Enhanced order details table
        var detailsTable = CreateEnhancedOrderDetailsTable(request.OrderItems);
        document.Add(detailsTable);

        // Footer with notes and totals
        document.Add(new Paragraph(" "));

        var footerTable = new PdfPTable(2) { WidthPercentage = 100 };
        footerTable.SetWidths(new float[] { 3f, 1f });

        var notesCell = new PdfPCell(new Phrase($"NOTES: {request.Notes}", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, BaseColor.RED)));
        notesCell.Border = Rectangle.BOX;
        notesCell.Padding = 5;
        notesCell.VerticalAlignment = Element.ALIGN_MIDDLE;

        var totalsCell = new PdfPCell();
        totalsCell.Border = Rectangle.BOX;
        totalsCell.Padding = 5;

        var totalTable = new PdfPTable(1);
        totalTable.AddCell(Helper.CreateInfoCell("TOTAL WEIGHT:", request.TotalWeight.ToString()));
        totalTable.AddCell(Helper.CreateInfoCell("EXPECTED DELIVERY DATES BEFORE:", request.ExpectedDelivery.ToString("dd MMM yyyy (dddd)")));
        totalsCell.AddElement(totalTable);

        footerTable.AddCell(notesCell);
        footerTable.AddCell(totalsCell);
        document.Add(footerTable);

        document.Close();
        return memoryStream.ToArray();
    }

    public async Task<byte[]> GenerateExcelStyleSchedule(DailyScheduleRequest request)
    {
        using var memoryStream = new MemoryStream();
        var document = new Document(PageSize.A3.Rotate(), 15, 15, 15, 15);
        var writer = PdfWriter.GetInstance(document, memoryStream);

        document.Open();

        // Title matching Excel style
        var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.BLACK);
        var title = new Paragraph("EXAMPLE: DAY TO DAY POURING PLAN", titleFont);
        title.Alignment = Element.ALIGN_CENTER;
        title.SpacingAfter = 5;
        document.Add(title);

        var subtitle = new Paragraph("(EXCEL)", FontFactory.GetFont(FontFactory.HELVETICA, 12, BaseColor.BLUE));
        subtitle.Alignment = Element.ALIGN_CENTER;
        subtitle.SpacingAfter = 15;
        document.Add(subtitle);

        // Excel-style schedule table
        var scheduleTable = new PdfPTable(5) { WidthPercentage = 100 };
        scheduleTable.SetWidths(new float[] { 1f, 2f, 2f, 1.5f, 2f });

        // Headers with Excel-like styling
        var headers = new[] { "Date", "FULL ORDER", "PLANNED TO BE POURED", "SUGGESTED MOLD", "LEFT TO BE POURED" };

        foreach (var header in headers)
        {
            var headerCell = new PdfPCell(new Phrase(header, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));
            headerCell.BackgroundColor = new BaseColor(200, 200, 255); // Light blue like Excel
            headerCell.HorizontalAlignment = Element.ALIGN_CENTER;
            headerCell.Padding = 6;
            headerCell.Border = Rectangle.BOX;
            scheduleTable.AddCell(headerCell);
        }

        // Data rows
        foreach (var day in request.ScheduleDays)
        {
            var maxRows = Math.Max(1, day.Projects.Max(p => Math.Max(p.FullOrder.Count,
                Math.Max(p.PlannedToPour.Count, p.LeftToPour?.Count ?? 0))));

            for (int rowIndex = 0; rowIndex < maxRows; rowIndex++)
            {
                foreach (var project in day.Projects)
                {
                    // Date cell (yellow background, spans multiple rows for first project)
                    if (rowIndex == 0 && project == day.Projects.First())
                    {
                        var dateCell = new PdfPCell(new Phrase($"{day.Date:dd-MMM}", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));
                        dateCell.BackgroundColor = BaseColor.YELLOW;
                        dateCell.HorizontalAlignment = Element.ALIGN_CENTER;
                        dateCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        dateCell.Padding = 8;
                        dateCell.Rowspan = maxRows * day.Projects.Count;
                        scheduleTable.AddCell(dateCell);
                    }

                    // Project info (first row only)
                    if (rowIndex == 0)
                    {
                        var projectCell = new PdfPCell();
                        projectCell.Padding = 5;
                        projectCell.AddElement(new Paragraph(project.ProjectName, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)));
                        projectCell.AddElement(new Paragraph(project.Location, FontFactory.GetFont(FontFactory.HELVETICA, 8)));
                        scheduleTable.AddCell(projectCell);
                    }
                    else
                    {
                        scheduleTable.AddCell(Helper.CreateCell("", BaseColor.WHITE));
                    }

                    // Full order
                    var fullOrderText = rowIndex < project.FullOrder.Count ? project.FullOrder[rowIndex] : "";
                    scheduleTable.AddCell(CreateExcelCell(fullOrderText, new BaseColor(220, 220, 220)));

                    // Planned to pour
                    var plannedText = rowIndex < project.PlannedToPour.Count ? project.PlannedToPour[rowIndex] : "";
                    scheduleTable.AddCell(CreateExcelCell(plannedText, BaseColor.WHITE));

                    // Suggested mold (first row only)
                    if (rowIndex == 0)
                    {
                        scheduleTable.AddCell(CreateExcelCell(project.SuggestedMold ?? "", BaseColor.WHITE));
                    }
                    else
                    {
                        scheduleTable.AddCell(Helper.CreateCell("", BaseColor.WHITE));
                    }

                    // Left to pour
                    var leftText = "";
                    if (project.LeftToPour?.Any() == true && rowIndex < project.LeftToPour.Count)
                    {
                        leftText = project.LeftToPour[rowIndex];
                        scheduleTable.AddCell(CreateExcelCell(leftText, BaseColor.WHITE));
                    }
                    else
                    {
                        var nilCell = CreateExcelCell("NIL", new BaseColor(180, 180, 180));
                        scheduleTable.AddCell(nilCell);
                    }
                }
            }
        }

        document.Add(scheduleTable);
        document.Close();
        return memoryStream.ToArray();
    }

    // Helper methods for enhanced styling
    private PdfPCell CreateStyledCell(string text, BaseColor backgroundColor, BaseColor textColor, bool bold)
    {
        var font = bold ? FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8, textColor) :
                         FontFactory.GetFont(FontFactory.HELVETICA, 8, textColor);
        var cell = new PdfPCell(new Phrase(text, font));
        cell.BackgroundColor = backgroundColor;
        cell.Padding = 3;
        cell.HorizontalAlignment = Element.ALIGN_CENTER;
        cell.Border = Rectangle.BOX;
        return cell;
    }

    private PdfPCell CreateInfoBox(string label, string value, BaseColor backgroundColor)
    {
        var table = new PdfPTable(1);

        var labelCell = new PdfPCell(new Phrase(label, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8)));
        labelCell.BackgroundColor = backgroundColor;
        labelCell.Padding = 3;
        labelCell.HorizontalAlignment = Element.ALIGN_CENTER;
        labelCell.Border = Rectangle.BOX;

        var valueCell = new PdfPCell(new Phrase(value, FontFactory.GetFont(FontFactory.HELVETICA, 9)));
        valueCell.Padding = 4;
        valueCell.HorizontalAlignment = Element.ALIGN_CENTER;
        valueCell.Border = Rectangle.BOX;

        table.AddCell(labelCell);
        table.AddCell(valueCell);

        var containerCell = new PdfPCell(table);
        containerCell.Border = Rectangle.NO_BORDER;
        containerCell.Padding = 2;
        return containerCell;
    }

    private PdfPCell CreateHeaderCell(string text)
    {
        var cell = new PdfPCell(new Phrase(text, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)));
        cell.BackgroundColor = new BaseColor(128, 128, 128);
        cell.HorizontalAlignment = Element.ALIGN_CENTER;
        cell.Padding = 5;
        cell.Border = Rectangle.BOX;
        return cell;
    }

    private PdfPCell CreateDataCell(string text)
    {
        var cell = new PdfPCell(new Phrase(text, FontFactory.GetFont(FontFactory.HELVETICA, 9)));
        cell.BackgroundColor = new BaseColor(220, 220, 220);
        cell.Padding = 5;
        cell.Border = Rectangle.BOX;
        return cell;
    }

    private PdfPTable CreateEnhancedOrderDetailsTable(List<OrderItem> orderItems)
    {
        var table = new PdfPTable(9) { WidthPercentage = 100 };
        table.SetWidths(new float[] { 0.5f, 1f, 1f, 1f, 1f, 1f, 1.5f, 0.8f, 0.8f });

        // Multi-level headers
        var mainHeaders = new[] { "QTY", "POURED SIZE", "", "FINISHED SIZE", "", "DESCRIPTION", "", "AREA", "WEIGHT" };
        var subHeaders = new[] { "", "WIDTH", "LENGTH", "WIDTH", "LENGTH", "COLOR", "TYPE", "(SQ. IN)", "(LBS)" };

        // Main header row
        for (int i = 0; i < mainHeaders.Length; i++)
        {
            if (!string.IsNullOrEmpty(mainHeaders[i]))
            {
                var headerCell = new PdfPCell(new Phrase(mainHeaders[i], FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)));
                headerCell.BackgroundColor = new BaseColor(128, 128, 128);
                headerCell.HorizontalAlignment = Element.ALIGN_CENTER;
                headerCell.Padding = 4;
                if (i == 1 || i == 3) headerCell.Colspan = 2; // POURED SIZE and FINISHED SIZE span 2 columns
                table.AddCell(headerCell);
            }
        }

        // Sub-header row
        foreach (var subHeader in subHeaders)
        {
            var subCell = new PdfPCell(new Phrase(subHeader, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8)));
            subCell.BackgroundColor = new BaseColor(200, 200, 200);
            subCell.HorizontalAlignment = Element.ALIGN_CENTER;
            subCell.Padding = 3;
            subCell.Border = Rectangle.BOX;
            table.AddCell(subCell);
        }

        // Order items data
        foreach (var item in orderItems)
        {
            // Lot header
            var lotCell = new PdfPCell(new Phrase($"LOT {item.LotNumber}", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)));
            lotCell.Colspan = 9;
            lotCell.BackgroundColor = new BaseColor(180, 180, 180);
            lotCell.Padding = 4;
            lotCell.HorizontalAlignment = Element.ALIGN_LEFT;
            table.AddCell(lotCell);

            // Item details
            foreach (var detail in item.Details)
            {
                table.AddCell(CreateOrderDetailCell(detail.Quantity.ToString(), BaseColor.WHITE));

                // Poured size with yellow background (as shown in screenshot)
                table.AddCell(CreateOrderDetailCell($"({detail.PouredWidth} X {detail.PouredLength})", BaseColor.YELLOW));
                table.AddCell(CreateOrderDetailCell("", BaseColor.YELLOW));

                // Finished size
                table.AddCell(CreateOrderDetailCell(detail.FinishedWidth, BaseColor.WHITE));
                table.AddCell(CreateOrderDetailCell(detail.FinishedLength, BaseColor.WHITE));

                // Color with special formatting for NEW WHITE
                var colorBg = detail.Color == "NEW WHITE" ? BaseColor.RED : BaseColor.WHITE;
                var colorText = detail.Color == "NEW WHITE" ? BaseColor.WHITE : BaseColor.BLACK;
                table.AddCell(CreateOrderDetailCell(detail.Color, colorBg, colorText));

                table.AddCell(CreateOrderDetailCell(detail.Type, BaseColor.WHITE));
                table.AddCell(CreateOrderDetailCell(detail.Area.ToString(), BaseColor.WHITE));
                table.AddCell(CreateOrderDetailCell(detail.Weight.ToString(), BaseColor.WHITE));
            }
        }

        return table;
    }

    private PdfPCell CreateCalculationCell(string text, BaseColor backgroundColor)
    {
        var cell = new PdfPCell(new Phrase(text, FontFactory.GetFont(FontFactory.HELVETICA, 8)));
        cell.BackgroundColor = backgroundColor;
        cell.HorizontalAlignment = Element.ALIGN_CENTER;
        cell.Padding = 3;
        cell.Border = Rectangle.BOX;
        return cell;
    }

    private PdfPCell CreateExcelCell(string text, BaseColor backgroundColor)
    {
        var cell = new PdfPCell(new Phrase(text, FontFactory.GetFont(FontFactory.HELVETICA, 8)));
        cell.BackgroundColor = backgroundColor;
        cell.Padding = 4;
        cell.VerticalAlignment = Element.ALIGN_TOP;
        cell.Border = Rectangle.BOX;
        cell.BorderWidth = 1;
        cell.BorderColor = BaseColor.GRAY;
        return cell;
    }

    private PdfPCell CreateOrderDetailCell(string text, BaseColor backgroundColor, BaseColor? textColor = null)
    {
        var font = FontFactory.GetFont(FontFactory.HELVETICA, 8, textColor ?? BaseColor.BLACK);
        var cell = new PdfPCell(new Phrase(text, font));
        cell.BackgroundColor = backgroundColor;
        cell.HorizontalAlignment = Element.ALIGN_CENTER;
        cell.VerticalAlignment = Element.ALIGN_MIDDLE;
        cell.Padding = 3;
        cell.Border = Rectangle.BOX;
        return cell;
    }

    public PdfPCell CreateInfoCell(string label, string value)
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

}



    ////////////
    ///

    // Services
    public interface ITemplateService
    {
        Task<string> RenderTemplate(string templateName, object model, TemplateFormat format);
        Task<string> RenderCustomTemplate(string template, object model, TemplateFormat format);
        Task<string> GetTemplateContent(string templateName, TemplateFormat format);
    }

    public interface IPdfGeneratorService
    {
        Task<byte[]> ConvertHtmlToPdf(string html, PdfPageFormat format);
    }

    public class TemplateService : ITemplateService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly Dictionary<string, string> _templateCache = new();

        public TemplateService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }
    public async Task<byte[]> ConvertHtmlToPdf(string html, PdfPageFormat format)
    {
        //var document = QuestPDF.Fluent.Document.Create(container =>
        //{
        //    container.Page(page =>
        //    {
        //        page.Size(PageSizes.A4);
        //        if (format == PdfPageFormat.A3Landscape) page.Size(PageSizes.A3).Landscape();
        //        else if (format == PdfPageFormat.A4Landscape) page.Size(PageSizes.A4).Landscape();
        //        else if (format == PdfPageFormat.A3Portrait) page.Size(PageSizes.A3);
        //        else page.Size(PageSizes.A4);

        //        page.Margin(0.5, Unit.Inch);

        //        page.Content().Html(html);
        //    });
        //});

        using var ms = new MemoryStream();
       // document.GeneratePdf(ms);
        return ms.ToArray();
    }


    public async Task<string> RenderTemplate(string templateName, object model, TemplateFormat format)
        {
            var templateContent = await GetTemplateContent(templateName, format);
            return await RenderCustomTemplate(templateContent, model, format);
        }

        public async Task<string> RenderCustomTemplate(string template, object model, TemplateFormat format)
        {
            return format switch
            {
                TemplateFormat.Html => await RenderHtmlTemplate(template, model),
                TemplateFormat.Razor => await RenderRazorTemplate(template, model),
                TemplateFormat.Liquid => await RenderLiquidTemplate(template, model),
                _ => throw new ArgumentException($"Unsupported template format: {format}")
            };
        }

        public async Task<string> GetTemplateContent(string templateName, TemplateFormat format)
        {
        var cacheKey = $"{templateName}_{format}";

        if (_templateCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var templatePath = Path.Combine(_environment.ContentRootPath, "Templates", format.ToString(), $"{templateName}.{GetFileExtension(format)}");

        if (!File.Exists(templatePath))
        {
            var builtIn = GetBuiltInTemplate(templateName, format);
            _templateCache[cacheKey] = builtIn;
            return builtIn;
        }

        var content = await File.ReadAllTextAsync(templatePath);
        _templateCache[cacheKey] = content;
        return content;
    }

        private async Task<string> RenderHtmlTemplate(string template, object model)
        {
            // Simple token replacement for HTML templates
            var json = JsonConvert.SerializeObject(model, Formatting.None);
            var modelDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            var result = template;
            foreach (var kvp in modelDict)
            {
                result = result.Replace($"{{{{{kvp.Key}}}}}", kvp.Value?.ToString() ?? "");
            }

            return result;
        }

        private async Task<string> RenderRazorTemplate(string template, object model)
        {
            var templateKey = Guid.NewGuid().ToString();
        //return Engine.Razor.RunCompile(template, templateKey, null, model);
        return "";
        }

        private async Task<string> RenderLiquidTemplate(string template, object model)
        {
            var liquidTemplate = Template.Parse(template);
            var scriptObject = new ScriptObject();

            var json = JsonConvert.SerializeObject(model);
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            foreach (var kvp in dict)
            {
                scriptObject.Add(kvp.Key, kvp.Value);
            }

            var context = new TemplateContext();
            context.PushGlobal(scriptObject);

            return await liquidTemplate.RenderAsync(context);
        }

        private string GetFileExtension(TemplateFormat format)
        {
            return format switch
            {
                TemplateFormat.Html => "html",
                TemplateFormat.Razor => "cshtml",
                TemplateFormat.Liquid => "liquid",
                _ => "html"
            };
        }

        private string GetBuiltInTemplate(string templateName, TemplateFormat format)
        {
            return templateName.ToLower() switch
            {
                "pourplan" => GetPourPlanTemplate(format),
                "workorder" => GetWorkOrderTemplate(format),
                "dailyschedule" => GetDailyScheduleTemplate(format),
                _ => throw new ArgumentException($"Unknown template: {templateName}")
            };
        }

        private string GetPourPlanTemplate(TemplateFormat format)
        {
            if (format == TemplateFormat.Html)
            {
                return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>{{Day}} Pouring Plan</title>
    <style>
        @page { 
            size: A3 landscape; 
            margin: 0.5in; 
        }
        
        body { 
            font-family: Arial, sans-serif; 
            margin: 0; 
            padding: 0;
            font-size: 12px;
        }
        
        .header {
            text-align: center;
            margin-bottom: 20px;
        }
        
        .title {
            font-size: 18px;
            font-weight: bold;
            color: #4682B4;
            text-transform: uppercase;
        }
        
        .main-container {
            display: flex;
            gap: 20px;
            height: calc(100vh - 100px);
        }
        
        .left-panel {
            flex: 1;
            border: 2px solid #000;
            padding: 10px;
            background: #fafafa;
        }
        
        .right-panel {
            flex: 1.5;
            border: 2px solid #000;
            padding: 10px;
            background: #fff;
        }
        
        .schedule-columns {
            display: flex;
            gap: 10px;
            height: 100%;
        }
        
        .schedule-column {
            flex: 1;
        }
        
        .schedule-block {
            margin-bottom: 15px;
            border: 1px solid #ccc;
            border-radius: 4px;
            overflow: hidden;
        }
        
        .time-header {
            background: #4682B4;
            color: white;
            padding: 8px;
            text-align: center;
            font-weight: bold;
            font-size: 11px;
        }
        
        .schedule-item {
            display: flex;
            align-items: center;
            padding: 4px;
            border-bottom: 1px solid #eee;
        }
        
        .schedule-item:last-child {
            border-bottom: none;
        }
        
        .sequence-number {
            background: #DC143C;
            color: white;
            width: 20px;
            height: 20px;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-weight: bold;
            font-size: 9px;
            margin-right: 8px;
            flex-shrink: 0;
        }
        
        .item-description {
            flex: 2;
            font-size: 9px;
            padding-right: 5px;
        }
        
        .item-duration {
            flex: 0.7;
            font-size: 9px;
            text-align: center;
        }
        
        .item-status {
            flex: 0.7;
            font-size: 9px;
            text-align: center;
            padding: 2px 4px;
            border-radius: 3px;
            font-weight: bold;
        }
        
        .status-ready { background: #90EE90; }
        .status-pending { background: #FFD700; }
        .status-scheduled { background: #ADD8E6; }
        
        .pour-header {
            background: #E0E0E0;
            padding: 12px;
            text-align: center;
            font-size: 16px;
            font-weight: bold;
            color: #4682B4;
            border: 2px solid #ccc;
            margin-bottom: 15px;
        }
        
        .mold-section {
            margin-bottom: 20px;
        }
        
        .mold-name {
            background: #DC143C;
            color: white;
            padding: 8px;
            text-align: center;
            font-weight: bold;
            font-size: 11px;
        }
        
        .mold-drawing {
            border: 1px solid #000;
            padding: 15px;
            background: #FFFACD;
            min-height: 120px;
            position: relative;
        }
        
        .mold-piece {
            border: 2px solid #000;
            background: white;
            padding: 15px;
            margin: 10px;
            text-align: center;
            font-weight: bold;
            display: inline-block;
            position: relative;
        }
        
        .dimension-label {
            position: absolute;
            font-size: 10px;
            font-weight: bold;
        }
        
        .dimension-right {
            right: -40px;
            top: 50%;
            transform: translateY(-50%);
            color: #DC143C;
        }
        
        .calculation-table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 20px;
            font-size: 9px;
        }
        
        .calc-header {
            background: #4A4A4A;
            color: white;
            padding: 6px;
            text-align: center;
            font-weight: bold;
        }
        
        .calc-subheader {
            background: #B0C4DE;
            padding: 4px;
            text-align: center;
            font-weight: bold;
            font-size: 8px;
        }
        
        .calc-cell {
            border: 1px solid #000;
            padding: 4px;
            text-align: center;
        }
        
        .calc-total {
            background: #F0F8FF;
            font-weight: bold;
        }
        
        @media print {
            .main-container {
                height: auto;
            }
        }
    </style>
</head>
<body>
    <div class='header'>
        <div class='title'>{{Day}} POURING PLAN</div>
    </div>
    
    <div class='main-container'>
        <div class='left-panel'>
            <div class='schedule-columns'>
                <div class='schedule-column'>
                    {{#each ScheduleBlocks}}
                    {{#if @first}}
                    <div class='schedule-block'>
                        <div class='time-header'>{{TimeSlot}}</div>
                        {{#each Items}}
                        <div class='schedule-item'>
                            <div class='sequence-number'>{{Sequence}}</div>
                            <div class='item-description'>{{Description}}</div>
                            <div class='item-duration'>{{Duration}}</div>
                            <div class='item-status status-{{Status}}'>{{Status}}</div>
                        </div>
                        {{/each}}
                    </div>
                    {{/if}}
                    {{/each}}
                </div>
                <div class='schedule-column'>
                    {{#each ScheduleBlocks}}
                    {{#unless @first}}
                    <div class='schedule-block'>
                        <div class='time-header'>{{TimeSlot}}</div>
                        {{#each Items}}
                        <div class='schedule-item'>
                            <div class='sequence-number'>{{Sequence}}</div>
                            <div class='item-description'>{{Description}}</div>
                            <div class='item-duration'>{{Duration}}</div>
                            <div class='item-status status-{{Status}}'>{{Status}}</div>
                        </div>
                        {{/each}}
                    </div>
                    {{/unless}}
                    {{/each}}
                </div>
            </div>
        </div>
        
        <div class='right-panel'>
            <div class='pour-header'>POUR 1</div>
            
            {{#each Molds}}
            <div class='mold-section'>
                <div class='mold-name'>MOLD NAME - {{Name}}</div>
                <div class='mold-drawing'>
                    {{#each Sections}}
                    <div class='mold-piece'>
                        {{Width}}' x {{Height}}'
                        <div class='dimension-label dimension-right'>={{TotalMeasurement}}'</div>
                    </div>
                    {{/each}}
                </div>
            </div>
            {{/each}}
            
            {{#if CalculationData}}
            <table class='calculation-table'>
                <tr>
                    <td colspan='8' class='calc-header'>CALCULATION FOR MOLDS ON POUR - POUR 1</td>
                </tr>
                <tr>
                    <td class='calc-subheader'>MOLD SIZE</td>
                    <td class='calc-subheader'>SQ. OF FACE</td>
                    <td class='calc-subheader'>POURED SIZE</td>
                    <td class='calc-subheader'>SQ. OF FACE</td>
                    <td class='calc-subheader'>POURED SIZE</td>
                    <td class='calc-subheader'>SQ. OF FACE</td>
                    <td class='calc-subheader'>EXTRA MARGIN</td>
                    <td class='calc-subheader'>TOTAL PRODUCT</td>
                </tr>
                {{#each CalculationData.Rows}}
                <tr>
                    <td class='calc-cell'>{{MoldSize}}</td>
                    <td class='calc-cell'>{{SqFace1}}</td>
                    <td class='calc-cell'>{{PouredSize1}}</td>
                    <td class='calc-cell'>{{SqFace2}}</td>
                    <td class='calc-cell'>{{PouredSize2}}</td>
                    <td class='calc-cell'>{{SqFace3}}</td>
                    <td class='calc-cell'>{{ExtraMargin}}</td>
                    <td class='calc-cell calc-total'>{{TotalProduct}}</td>
                </tr>
                {{/each}}
            </table>
            {{/if}}
        </div>
    </div>
</body>
</html>";
            }

            // Return Liquid template for Scriban
            return @"
<div class='pour-plan-container'>
    <h1>{{ day | upcase }} POURING PLAN</h1>
    
    <div class='schedule-section'>
        {% for block in schedule_blocks %}
        <div class='time-block'>
            <h3>{{ block.time_slot }}</h3>
            {% for item in block.items %}
            <div class='schedule-item'>
                <span class='seq'>{{ item.sequence }}</span>
                <span class='desc'>{{ item.description }}</span>
                <span class='duration'>{{ item.duration }}</span>
                <span class='status'>{{ item.status }}</span>
            </div>
            {% endfor %}
        </div>
        {% endfor %}
    </div>
    
    <div class='mold-section'>
        <h2>POUR 1</h2>
        {% for mold in molds %}
        <div class='mold-container'>
            <h4>MOLD NAME - {{ mold.name }}</h4>
            <div class='mold-drawing'>
                {% for section in mold.sections %}
                <div class='mold-piece'>{{ section.width }}' x {{ section.height }}'</div>
                {% endfor %}
            </div>
        </div>
        {% endfor %}
    </div>
</div>";
        }

        private string GetWorkOrderTemplate(TemplateFormat format)
        {
            if (format == TemplateFormat.Html)
            {
                return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Work Order {{OrderNumber}}</title>
    <style>
        @page { 
            size: A4 portrait; 
            margin: 0.5in; 
        }
        
        body { 
            font-family: Arial, sans-serif; 
            margin: 0; 
            padding: 0;
            font-size: 11px;
            line-height: 1.2;
        }
        
        .header {
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            margin-bottom: 20px;
            padding-bottom: 10px;
            border-bottom: 1px solid #ccc;
        }
        
        .company-info {
            flex: 1;
        }
        
        .company-logo {
            display: flex;
            align-items: center;
            margin-bottom: 10px;
        }
        
        .logo-icon {
            font-size: 20px;
            color: #DC143C;
            margin-right: 8px;
        }
        
        .logo-text {
            font-size: 16px;
            font-weight: bold;
            color: #DC143C;
        }
        
        .company-details {
            font-size: 9px;
            line-height: 1.3;
        }
        
        .order-info {
            flex: 1;
            text-align: right;
        }
        
        .work-order-title {
            font-size: 20px;
            font-weight: bold;
            color: #666;
            margin-bottom: 15px;
        }
        
        .info-box {
            display: inline-block;
            margin: 2px;
            border: 1px solid #ccc;
            border-radius: 3px;
            overflow: hidden;
        }
        
        .info-label {
            background: #FFB6C1;
            padding: 3px 8px;
            font-weight: bold;
            font-size: 8px;
            text-align: center;
        }
        
        .info-value {
            background: white;
            padding: 4px 8px;
            font-size: 9px;
            text-align: center;
        }
        
        .info-label.blue { background: #B0C4DE; }
        
        .builder-table {
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 15px;
        }
        
        .builder-table th {
            background: #808080;
            color: white;
            padding: 6px;
            text-align: center;
            font-weight: bold;
            font-size: 9px;
            border: 1px solid #000;
        }
        
        .builder-table td {
            background: #DCDCDC;
            padding: 6px;
            border: 1px solid #000;
            font-size: 9px;
        }
        
        .details-table {
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 15px;
        }
        
        .details-table th {
            background: #808080;
            color: white;
            padding: 4px;
            text-align: center;
            font-weight: bold;
            font-size: 8px;
            border: 1px solid #000;
        }
        
        .details-table .sub-header {
            background: #C0C0C0;
            font-size: 7px;
        }
        
        .details-table td {
            padding: 4px;
            border: 1px solid #000;
            text-align: center;
            font-size: 8px;
        }
        
        .lot-header {
            background: #B0B0B0 !important;
            font-weight: bold;
            text-align: left !important;
            padding: 4px 6px !important;
        }
        
        .poured-size {
            background: #FFFF00 !important;
        }
        
        .new-white {
            background: #DC143C !important;
            color: white !important;
            font-weight: bold;
        }
        
        .footer {
            display: flex;
            gap: 20px;
            margin-top: 20px;
        }
        
        .notes-section {
            flex: 2;
            border: 1px solid #000;
            padding: 8px;
        }
        
        .notes-text {
            font-weight: bold;
            color: #DC143C;
            font-size: 12px;
        }
        
        .totals-section {
            flex: 1;
            border: 1px solid #000;
            padding: 8px;
        }
        
        .total-row {
            display: flex;
            justify-content: space-between;
            margin: 5px 0;
            padding: 3px;
            border-bottom: 1px solid #eee;
        }
        
        .total-label {
            font-weight: bold;
            background: #E0E0E0;
            padding: 2px 5px;
        }
        
        .total-value {
            padding: 2px 5px;
        }
    </style>
</head>
<body>
    <div class='header'>
        <div class='company-info'>
            <div class='company-logo'>
                <span class='logo-icon'>🏠</span>
                <span class='logo-text'>MFG PRECAST</span>
            </div>
            <div class='company-details'>
                PO Box 730-71, Magaliesmã<br>
                Burlington, ON L7T 2H0<br>
                Phone: {{CompanyInfo.Phone}}<br>
                Email: {{CompanyInfo.Email}}<br>
                www.mfgprecast.com
            </div>
        </div>
        
        <div class='order-info'>
            <div class='work-order-title'>WORK ORDER</div>
            <div class='info-box'>
                <div class='info-label'>ORDER DATE</div>
                <div class='info-value'>{{OrderDate}}</div>
            </div>
            <div class='info-box'>
                <div class='info-label blue'>PURCHASE ORDER</div>
                <div class='info-value'>{{PurchaseOrder}}</div>
            </div>
            <div class='info-box'>
                <div class='info-label blue'>COMPANY</div>
                <div class='info-value'>{{Company}}</div>
            </div>
            <div class='info-box'>
                <div class='info-label blue'>CONTACT</div>
                <div class='info-value'>{{Contact}}</div>
            </div>
        </div>
    </div>
    
    <table class='builder-table'>
        <tr>
            <th>BUILDER / SITE / CITY</th>
            <th>BLK NO.</th>
            <th>COMPANY</th>
            <th>CONTACT</th>
        </tr>
        {{#each Sites}}
        <tr>
            <td>{{Builder}}</td>
            <td>{{BlockNumber}}</td>
            <td>{{Company}}</td>
            <td>{{Contact}}</td>
        </tr>
        <tr>
            <td>{{Location}}</td>
            <td>LOT NO.<br>{{LotNumber}}</td>
            <td></td>
            <td></td>
        </tr>
        {{/each}}
    </table>
    
    <table class='details-table'>
        <tr>
            <th rowspan='2'>QTY</th>
            <th colspan='2'>POURED SIZE</th>
            <th colspan='2'>FINISHED SIZE</th>
            <th rowspan='2'>DESCRIPTION</th>
            <th rowspan='2'>AREA<br>(SQ. IN)</th>
            <th rowspan='2'>WEIGHT<br>(LBS)</th>
        </tr>
        <tr class='sub-header'>
            <th>WIDTH</th>
            <th>LENGTH</th>
            <th>WIDTH</th>
            <th>LENGTH</th>
            <th>COLOR</th>
            <th>TYPE</th>
        </tr>
        
        {{#each OrderItems}}
        <tr>
            <td colspan='8' class='lot-header'>LOT {{LotNumber}}</td>
        </tr>
        {{#each Details}}
        <tr>
            <td>{{Quantity}}</td>
            <td class='poured-size'>({{PouredWidth}} X {{PouredLength}})</td>
            <td class='poured-size'></td>
            <td>{{FinishedWidth}}</td>
            <td>{{FinishedLength}}</td>
            <td class='{{#ifeq Color 'NEW WHITE'}}new-white{{/ifeq}}'>{{Color}}</td>
            <td>{{Type}}</td>
            <td>{{Area}}</td>
            <td>{{Weight}}</td>
        </tr>
        {{/each}}
        {{/each}}
    </table>
    
    <div class='footer'>
        <div class='notes-section'>
            <div class='notes-text'>NOTES: {{Notes}}</div>
        </div>
        <div class='totals-section'>
            <div class='total-row'>
                <span class='total-label'>TOTAL WEIGHT:</span>
                <span class='total-value'>{{TotalWeight}}</span>
            </div>
            <div class='total-row'>
                <span class='total-label'>EXPECTED DELIVERY DATES BEFORE:</span>
                <span class='total-value'>{{ExpectedDelivery}}</span>
            </div>
        </div>
    </div>
</body>
</html>";
            }

            return "<!-- Razor/Liquid templates for WorkOrder -->";
        }

        private string GetDailyScheduleTemplate(TemplateFormat format)
        {
            if (format == TemplateFormat.Html)
            {
                return @"
<!DOCTYPE html";

                return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Daily Schedule</title>
    <style>
        @page { 
            size: A3 landscape; 
            margin: 0.4in; 
        }
        
        body { 
            font-family: Arial, sans-serif; 
            margin: 0; 
            padding: 0;
            font-size: 10px;
        }
        
        .header {
            text-align: center;
            margin-bottom: 25px;
        }
        
        .main-title {
            font-size: 16px;
            font-weight: bold;
            color: #000;
            margin-bottom: 5px;
        }
        
        .sub-title {
            font-size: 12px;
            color: #4682B4;
            font-style: italic;
        }
        
        .schedule-table {
            width: 100%;
            border-collapse: collapse;
            border: 2px solid #000;
            margin-bottom: 20px;
        }
        
        .schedule-table th {
            background: #D3D3D3;
            padding: 8px;
            text-align: center;
            font-weight: bold;
            font-size: 9px;
            border: 1px solid #000;
        }
        
        .schedule-table td {
            padding: 6px;
            border: 1px solid #000;
            vertical-align: top;
            font-size: 8px;
        }
        
        .date-cell {
            background: #FFFF00 !important;
            font-weight: bold;
            text-align: center;
            vertical-align: middle;
            font-size: 9px;
        }
        
        .project-cell {
            background: white;
            padding: 6px;
        }
        
        .project-name {
            font-weight: bold;
            font-size: 9px;
            margin-bottom: 3px;
        }
        
        .project-location {
            font-size: 8px;
            color: #666;
            line-height: 1.2;
        }
        
        .full-order-cell {
            background: #E0E0E0;
            padding: 4px;
            font-size: 7px;
            line-height: 1.3;
        }
        
        .planned-cell {
            background: white;
            padding: 4px;
            font-size: 7px;
            line-height: 1.3;
        }
        
        .mold-cell {
            background: white;
            text-align: center;
            vertical-align: middle;
            font-size: 8px;
        }
        
        .left-cell {
            background: white;
            padding: 4px;
            font-size: 7px;
            line-height: 1.3;
        }
        
        .nil-cell {
            background: #C0C0C0 !important;
            text-align: center;
            vertical-align: middle;
            font-weight: bold;
            font-size: 8px;
        }
        
        .order-line {
            margin: 1px 0;
            padding: 1px 0;
        }
        
        .highlight-blue {
            background: #E6F3FF;
        }
        
        .footer-note {
            text-align: center;
            margin-top: 20px;
            font-style: italic;
            color: #4682B4;
            font-size: 10px;
        }
    </style>
</head>
<body>
    <div class='header'>
        <div class='main-title'>EXAMPLE: DAY TO DAY POURING PLAN</div>
        <div class='sub-title'>(EXCEL)</div>
    </div>
    
    <table class='schedule-table'>
        <thead>
            <tr>
                <th style='width: 12%;'>Date</th>
                <th style='width: 20%;'>Project Info</th>
                <th style='width: 25%;'>FULL ORDER</th>
                <th style='width: 25%;'>PLANNED TO BE POURED</th>
                <th style='width: 10%;'>SUGGESTED MOLD</th>
                <th style='width: 18%;'>LEFT TO BE POURED</th>
            </tr>
        </thead>
        <tbody>
            {{#each ScheduleDays}}
            {{#each Projects}}
            <tr>
                {{#if @first}}
                <td class='date-cell' rowspan='{{../Projects.length}}'>{{../Date}}</td>
                {{/if}}
                <td class='project-cell'>
                    <div class='project-name'>{{ProjectName}}</div>
                    <div class='project-location'>{{Location}}</div>
                </td>
                <td class='full-order-cell'>
                    {{#each FullOrder}}
                    <div class='order-line'>{{this}}</div>
                    {{/each}}
                </td>
                <td class='planned-cell'>
                    {{#each PlannedToPour}}
                    <div class='order-line'>{{this}}</div>
                    {{/each}}
                </td>
                <td class='mold-cell'>{{SuggestedMold}}</td>
                <td class='{{#if LeftToPour}}left-cell{{else}}nil-cell{{/if}}'>
                    {{#if LeftToPour}}
                        {{#each LeftToPour}}
                        <div class='order-line'>{{this}}</div>
                        {{/each}}
                    {{else}}
                        NIL
                    {{/if}}
                </td>
            </tr>
            {{/each}}
            {{/each}}
        </tbody>
    </table>
    
    <div class='footer-note'>
        Generated on {{Date}} - Precast Manufacturing Schedule
    </div>
</body>
</html>";
            }

            return "<!-- Liquid template for Daily Schedule -->";
        }
    }

    public class PdfGeneratorService : IPdfGeneratorService
    {
        public async Task<byte[]> ConvertHtmlToPdf(string html, PdfPageFormat format)
        {
            // Download Chromium if not already downloaded
            // await new BrowserFetcher().DownloadAsync();
            //
            // using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            // {
            //     Headless = true,
            //     Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
            // });
            //
            // using var page = await browser.NewPageAsync();
            // await page.SetContentAsync(html);
            //
            // var pdfOptions = new PdfOptions
            // {
            //     Format = GetPuppeteerFormat(format),
            //     PrintBackground = true,
            //     MarginOptions = new MarginOptions
            //     {
            //         Top = "0.5in",
            //         Right = "0.5in",
            //         Bottom = "0.5in",
            //         Left = "0.5in"
            //     }
            // };

            // return await page.PdfDataAsync(pdfOptions);
            return new byte[0];
        }

        // private PaperFormat GetPuppeteerFormat(PdfPageFormat format)
        // {
        //     return format switch
        //     {
        //         PdfPageFormat.A4Portrait => PaperFormat.A4,
        //         PdfPageFormat.A4Landscape => PaperFormat.A4,
        //         PdfPageFormat.A3Portrait => PaperFormat.A3,
        //         PdfPageFormat.A3Landscape => PaperFormat.A3,
        //         _ => PaperFormat.A4
        //     };
        // }
    }

    // Enhanced Template Manager with multiple format support
    public class TemplateManager
    {
        private readonly IWebHostEnvironment _environment;
        private readonly Dictionary<string, Dictionary<TemplateFormat, string>> _templates = new();

        public TemplateManager(IWebHostEnvironment environment)
        {
            _environment = environment;
            InitializeBuiltInTemplates();
        }

        private void InitializeBuiltInTemplates()
        {
            // Store all built-in templates organized by name and format
            _templates["PourPlan"] = new Dictionary<TemplateFormat, string>
            {
                [TemplateFormat.Html] = GetAdvancedPourPlanHtml(),
               // [TemplateFormat.Razor] = GetPourPlanRazor(),
              //  [TemplateFormat.Liquid] = GetPourPlanLiquid()
            };

            _templates["WorkOrder"] = new Dictionary<TemplateFormat, string>
            {
                [TemplateFormat.Html] = GetAdvancedWorkOrderHtml(),
             //   [TemplateFormat.Razor] = GetWorkOrderRazor(),
             //   [TemplateFormat.Liquid] = GetWorkOrderLiquid()
            };

            _templates["DailySchedule"] = new Dictionary<TemplateFormat, string>
            {
              //  [TemplateFormat.Html] = GetAdvancedDailyScheduleHtml(),
                //[TemplateFormat.Razor] = GetDailyScheduleRazor(),
               // [TemplateFormat.Liquid] = GetDailyScheduleLiquid()
            };
        }

        public async Task<string> LoadTemplate(string templateName, TemplateFormat format)
        {
            // Try to load from file system first
            var templatePath = Path.Combine(_environment.ContentRootPath, "Templates", format.ToString(), $"{templateName}.{GetExtension(format)}");

            if (File.Exists(templatePath))
            {
                return await File.ReadAllTextAsync(templatePath);
            }

            // Fall back to built-in templates
            if (_templates.TryGetValue(templateName, out var formatDict) &&
                formatDict.TryGetValue(format, out var template))
            {
                return template;
            }

            throw new FileNotFoundException($"Template {templateName} not found in format {format}");
        }

        public async Task SaveTemplate(string templateName, TemplateFormat format, string content)
        {
            var templatesDir = Path.Combine(_environment.ContentRootPath, "Templates", format.ToString());
            Directory.CreateDirectory(templatesDir);

            var templatePath = Path.Combine(templatesDir, $"{templateName}.{GetExtension(format)}");
            await File.WriteAllTextAsync(templatePath, content);
        }

        private string GetExtension(TemplateFormat format)
        {
            return format switch
            {
                TemplateFormat.Html => "html",
                TemplateFormat.Razor => "cshtml",
                TemplateFormat.Liquid => "liquid",
                _ => "html"
            };
        }

        private string GetAdvancedPourPlanHtml()
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>{{Day}} Pouring Plan</title>
    <style>
        /* Advanced CSS matching the exact screenshot layout */
        @page { 
            size: A3 landscape; 
            margin: 0.3in; 
        }
        
        * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
        }
        
        body { 
            font-family: Arial, sans-serif;
            font-size: 10px;
            line-height: 1.2;
            background: white;
        }
        
        .document-container {
            width: 100%;
            height: 100vh;
            display: flex;
            flex-direction: column;
        }
        
        .document-header {
            text-align: center;
            padding: 15px;
            border-bottom: 2px solid #4682B4;
            margin-bottom: 15px;
        }
        
        .main-title {
            font-size: 20px;
            font-weight: bold;
            color: #4682B4;
            text-transform: uppercase;
            letter-spacing: 1px;
        }
        
        .content-area {
            flex: 1;
            display: flex;
            gap: 15px;
        }
        
        .schedule-panel {
            flex: 1;
            border: 3px solid #000;
            background: #f8f8f8;
            padding: 12px;
            display: flex;
            flex-direction: column;
        }
        
        .mold-panel {
            flex: 1.4;
            border: 3px solid #000;
            background: white;
            padding: 12px;
            display: flex;
            flex-direction: column;
        }
        
        .schedule-grid {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 10px;
            height: 100%;
        }
        
        .schedule-block {
            background: white;
            border: 1px solid #ccc;
            border-radius: 4px;
            margin-bottom: 8px;
            overflow: hidden;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        
        .time-slot-header {
            background: linear-gradient(135deg, #4682B4, #5A9BD4);
            color: white;
            padding: 6px 8px;
            text-align: center;
            font-weight: bold;
            font-size: 9px;
            border-bottom: 1px solid #333;
        }
        
        .schedule-items {
            padding: 4px;
        }
        
        .schedule-item-row {
            display: flex;
            align-items: center;
            padding: 3px 4px;
            border-bottom: 1px solid #f0f0f0;
            min-height: 24px;
        }
        
        .schedule-item-row:last-child {
            border-bottom: none;
        }
        
        .sequence-badge {
            background: #DC143C;
            color: white;
            width: 18px;
            height: 18px;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-weight: bold;
            font-size: 8px;
            margin-right: 6px;
            flex-shrink: 0;
        }
        
        .item-description {
            flex: 2.5;
            font-size: 8px;
            padding-right: 4px;
        }
        
        .item-duration {
            flex: 0.8;
            text-align: center;
            font-size: 8px;
            background: #f9f9f9;
            padding: 2px;
            border-radius: 2px;
            margin-right: 4px;
        }
        
        .item-status {
            flex: 0.8;
            text-align: center;
            font-size: 8px;
            padding: 2px 4px;
            border-radius: 3px;
            font-weight: bold;
        }
        
        .status-ready { background: #90EE90; color: #006400; }
        .status-pending { background: #FFD700; color: #B8860B; }
        .status-scheduled { background: #ADD8E6; color: #000080; }
        .status-waiting { background: #FFA500; color: #FF4500; }
        
        .pour-section-header {
            background: linear-gradient(135deg, #E0E0E0, #C0C0C0);
            border: 2px solid #999;
            padding: 12px;
            text-align: center;
            font-size: 16px;
            font-weight: bold;
            color: #4682B4;
            margin-bottom: 15px;
            border-radius: 4px;
        }
        
        .mold-container {
            margin-bottom: 18px;
        }
        
        .mold-header {
            background: #DC143C;
            color: white;
            padding: 8px;
            text-align: center;
            font-weight: bold;
            font-size: 10px;
            border-radius: 4px 4px 0 0;
        }
        
        .mold-drawing-area {
            border: 2px solid #000;
            border-top: none;
            background: linear-gradient(135deg, #FFFACD, #FFF8DC);
            padding: 15px;
            min-height: 100px;
            position: relative;
            border-radius: 0 0 4px 4px;
        }
        
        .mold-sections {
            display: flex;
            justify-content: center;
            align-items: center;
            gap: 15px;
            flex-wrap: wrap;
        }
        
        .mold-piece {
            border: 2px solid #000;
            background: white;
            padding: 12px 8px;
            text-align: center;
            font-weight: bold;
            font-size: 9px;
            position: relative;
            box-shadow: 2px 2px 4px rgba(0,0,0,0.2);
            min-width: 80px;
        }
        
        .mold-piece::after {
            content: '→ =' attr(data-total) '""';
            position: absolute;
        right: -45px;
        top: 50 %;
        transform: translateY(-50 %);
        color: #DC143C;
            font - weight: bold;
            font - size: 9px;
            white - space: nowrap;
        }
        
        .calculation-section {
            margin-top: 20px;
        }
        
        .calc-table {
            width: 100%;
            border-collapse: collapse;
            font-size: 8px;
        }
        
        .calc-title {
            background: #4A4A4A;
            color: white;
            padding: 6px;
            text-align: center;
            font-weight: bold;
            font-size: 10px;
        }
        
        .calc - header {
background: #87CEEB;
            padding: 4px;
    text - align: center;
    font - weight: bold;
border: 1px solid #000;
            font - size: 7px;
}
        
        .calc - header.red {
background: #FFB6C1; }
        .calc - header.green {
    background: #98FB98; }
        .calc - header.yellow {
        background: #FFFFE0; }
        
        .calc - cell {
            border: 1px solid #000;
            padding: 3px;
                text - align: center;
            background: white;
            }
        
        .calc - total {
            background: #F0F8FF;
            font - weight: bold;
            }
    </ style >
</ head >
< body >
    < div class= 'document-container' >
        < div class= 'document-header' >
            < div class= 'main-title' >{ { Day} }
POURING PLAN</div>
        </div>
        
        <div class= 'content-area' >
            < div class= 'schedule-panel' >
                < div class= 'schedule-grid' >
                    < div class= 'schedule-column' >
                        {
    {#each ScheduleBlocks}}
                        {
            {
#if @first}}
                        <div class='schedule-block'>
                            <div class='time-slot-header'>{{TimeSlot}}</div>
                            <div class='schedule-items'>
                                {{#each Items}}
                                <div class='schedule-item-row'>
                                    <div class='sequence-badge'>{{Sequence}}</div>
                                    <div class='item-description'>{{Description}}</div>
                                    <div class='item-duration'>{{Duration}}</div>
                                    <div class='item-status status-{{Status}}'>{{Status}}</div>
                                </div>
                                {{/each}}
                            </div>
                        </div>
                        {{/if}}
                        {{/each}}
                    </div>
                    
                    <div class='schedule-column'>
                        {{#each ScheduleBlocks}}
                        {{#unless @first}}
                        <div class='schedule-block'>
                            <div class='time-slot-header'>{{TimeSlot}}</div>
                            <div class='schedule-items'>
                                {{#each Items}}
                                <div class='schedule-item-row'>
                                    <div class='sequence-badge'>{{Sequence}}</div>
                                    <div class='item-description'>{{Description}}</div>
                                    <div class='item-duration'>{{Duration}}</div>
                                    <div class='item-status status-{{Status}}'>{{Status}}</div>
                                </div>
                                {{/each}}
                            </div>
                        </div>
                        {{/unless}}
                        {{/each}}
                    </div>
                </div>
            </div>
            
            <div class='mold-panel'>
                <div class='pour-section-header'>POUR 1</div>
                
                {{#each Molds}}
                <div class='mold-container'>
                    <div class='mold-header'>MOLD NAME - {{Name}}</div>
                    <div class='mold-drawing-area'>
                        <div class='mold-sections'>
                            {{#each Sections}}
                            <div class='mold-piece' data-total='{{TotalMeasurement}}'>
                                {{Width}}"" x {{Height}}""
                            </div>
                            {{/each}}
                        </div>
                    </div>
                </div>
                {{/each}}
                
                {{#if CalculationData}}
                <div class='calculation-section'>
                    <table class='calc-table'>
                        <tr>
                            <td colspan='8' class='calc-title'>CALCULATION FOR MOLDS ON POUR - POUR 1</td>
                        </tr>
                        <tr>
                            <th class='calc-header'>MOLD SIZE</th>
                            <th class='calc-header red'>SQ. OF FACE</th>
                            <th class='calc-header green'>POURED SIZE</th>
                            <th class='calc-header red'>SQ. OF FACE</th>
                            <th class='calc-header green'>POURED SIZE</th>
                            <th class='calc-header red'>SQ. OF FACE</th>
                            <th class='calc-header yellow'>EXTRA MARGIN</th>
                            <th class='calc-header'>TOTAL PRODUCT</th>
                        </tr>
                        {{#each CalculationData.Rows}}
                        <tr>
                            <td class='calc-cell'>{{MoldSize}}</td>
                            <td class='calc-cell'>{{SqFace1}}</td>
                            <td class='calc-cell'>{{PouredSize1}}</td>
                            <td class='calc-cell'>{{SqFace2}}</td>
                            <td class='calc-cell'>{{PouredSize2}}</td>
                            <td class='calc-cell'>{{SqFace3}}</td>
                            <td class='calc-cell'>{{ExtraMargin}}</td>
                            <td class='calc-cell calc-total'>{{TotalProduct}}</td>
                        </tr>
                        {{/each}}
                    </table>
                </div>
                {{/if}}
            </div>
        </div>
    </div>
</body>
</html>";
        }

        private string GetAdvancedWorkOrderHtml()
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Work Order</title>
    <style>
        @page { 
            size: A4 portrait; 
            margin: 0.4in; 
        }
        
        body { 
            font-family: Arial, sans-serif;
            font-size: 10px;
            line-height: 1.2;
            color: #000;
        }
        
        .work-order-header {
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            margin-bottom: 20px;
            padding-bottom: 10px;
        }
        
        .company-section {
            flex: 1;
        }
        
        .company-logo-row {
            display: flex;
            align-items: center;
            margin-bottom: 8px;
        }
        
        .logo-house {
            font-size: 18px;
            color: #DC143C;
            margin-right: 6px;
        }
        
        .company-name {
            font-size: 14px;
            font-weight: bold;
            color: #DC143C;
        }
        
        .company-details {
            font-size: 8px;
            line-height: 1.4;
            color: #333;
        }
        
        .order-section {
            flex: 1;
            text-align: right;
        }
        
        .work-order-title {
            font-size: 18px;
            font-weight: bold;
            color: #666;
            margin-bottom: 12px;
        }
        
        .order-info-grid {
            display: inline-block;
            text-align: left;
        }
        
        .info-item {
            display: inline-block;
            margin: 2px;
            border: 1px solid #999;
            border-radius: 3px;
            overflow: hidden;
            vertical-align: top;
        }
        
        .info-label {
            display: block;
            padding: 3px 8px;
            font-size: 7px;
            font-weight: bold;
            text-align: center;
            color: #000;
        }
        
        .info-value {
            display: block;
            padding: 4px 8px;
            font-size: 8px;
            text-align: center;
            background: white;
            min-width: 60px;
        }
        
        .label-red { background: #FFB6C1; }
        .label-blue { background: #B0C4DE; }
        
        .builder-info-table {
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 15px;
        }
        
        .builder-info-table th {
            background: #808080;
            color: white;
            padding: 5px;
            text-align: center;
            font-weight: bold;
            font-size: 8px;
            border: 1px solid #000;
        }
        
        .builder-info-table td {
            background: #DCDCDC;
            padding: 5px;
            border: 1px solid #000;
            font-size: 8px;
            vertical-align: top;
        }
        
        .order-details-table {
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 15px;
        }
        
        .order-details-table th {
            background: #808080;
            color: white;
            padding: 4px;
            text-align: center;
            font-weight: bold;
            font-size: 7px;
            border: 1px solid #000;
        }
        
        .order-details-table .sub-header {
            background: #C0C0C0;
            color: #000;
            font-size: 6px;
        }
        
        .order-details-table td {
            padding: 3px;
            border: 1px solid #000;
            text-align: center;
            font-size: 7px;
            vertical-align: middle;
        }
        
        .lot-row {
            background: #B0B0B0 !important;
            font-weight: bold;
            text-align: left !important;
            padding: 4px 6px !important;
        }
        
        .poured-size-cell {
            background: #FFFF00 !important;
            font-weight: bold;
        }
        
        .new-white-cell {
            background: #DC143C !important;
            color: white !important;
            font-weight: bold;
        }
        
        .footer-section {
            display: flex;
            gap: 15px;
            margin-top: 15px;
        }
        
        .notes-box {
            flex: 2;
            border: 2px solid #000;
            padding: 8px;
            background: #f9f9f9;
        }
        
        .notes-text {
            font-weight: bold;
            color: #DC143C;
            font-size: 11px;
        }
        
        .totals-box {
            flex: 1;
            border: 2px solid #000;
            padding: 8px;
            background: white;
        }
        
        .total-item {
            display: flex;
            justify-content: space-between;
            margin: 4px 0;
            padding: 3px;
            border-bottom: 1px solid #eee;
        }
        
        .total-label {
            font-weight: bold;
            background: #E0E0E0;
            padding: 2px 4px;
            font-size: 8px;
        }
        
        .total-value {
            padding: 2px 4px;
            font-size: 8px;
        }
    </style>
</head>
<body>
    <div class='work-order-header'>
        <div class='company-section'>
            <div class='company-logo-row'>
                <span class='logo-house'>🏠</span>
                <span class='company-name'>MFG PRECAST</span>
            </div>
            <div class='company-details'>
                PO Box 730-71, Magaliesmã<br>
                Burlington, ON L7T 2H0<br>
                Phone: {{CompanyInfo.Phone}}<br>
                Email: {{CompanyInfo.Email}}<br>
                www.mfgprecast.com
            </div>
        </div>
        
        <div class='order-section'>
            <div class='work-order-title'>WORK ORDER</div>
            <div class='order-info-grid'>
                <div class='info-item'>
                    <div class='info-label label-red'>ORDER DATE</div>
                    <div class='info-value'>{{OrderDate}}</div>
                </div>
                <div class='info-item'>
                    <div class='info-label label-blue'>PURCHASE ORDER</div>
                    <div class='info-value'>{{PurchaseOrder}}</div>
                </div>
                <br>
                <div class='info-item'>
                    <div class='info-label label-blue'>COMPANY</div>
                    <div class='info-value'>{{Company}}</div>
                </div>
                <div class='info-item'>
                    <div class='info-label label-blue'>CONTACT</div>
                    <div class='info-value'>{{Contact}}</div>
                </div>
            </div>
        </div>
    </div>
    
    <table class='builder-info-table'>
        <thead>
            <tr>
                <th 
";
        }
    }





     // Services

