using MfgDocs.Api.Services.Others;

namespace MfgDocs.Api.Services.Generators;

using System.Collections;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MfgDocs.Api.Services.Generators;

/// <summary>
/// Generates tracker documents showing planned vs actual pour data
/// </summary>
public class TrackerDocumentGenerator
{
    private readonly BaseFont _bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);

    private readonly BaseFont _bfBold =
        BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);

    // Colors matching your screenshot
    private readonly BaseColor YELLOW_BG = new BaseColor(255, 255, 0);
    private readonly BaseColor GRAY_BG = new BaseColor(211, 211, 211);
    private readonly BaseColor LIGHT_BLUE_BG = new BaseColor(173, 216, 230);
    private readonly BaseColor WHITE_BG = BaseColor.WHITE;
    private readonly BaseColor BLACK_LINE = BaseColor.BLACK;

    private const float MARGIN = 20f;

    public byte[] GenerateTrackerDocument(MultiDayPourPlan plannedData, ActualPourFeedback actualData = null)
    {
        using (var memoryStream = new MemoryStream())
        {
            // Landscape orientation for the tracker format
            var document = new Document(PageSize.LETTER.Rotate(), MARGIN, MARGIN, MARGIN, MARGIN);

            try
            {
                PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
                document.Open();
                PdfContentByte cb = writer.DirectContent;

                // Group work orders by daily plan date, then by order date, then by company
                var groupedOrders = GroupWorkOrdersForTracker(plannedData, actualData);

                bool firstPage = true;
                foreach (var dailyPlanGroup in groupedOrders)
                {
                    if (!firstPage)
                    {
                        document.NewPage();
                    }
                    firstPage = false;

                    DrawTrackerPage(cb, dailyPlanGroup.Key, dailyPlanGroup.Value, actualData);
                }

                document.Close();
                writer.Close();
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating tracker document: {ex.Message}", ex);
            }
        }
    }

    private Dictionary<DateTime, List<TrackerDayGroup>> GroupWorkOrdersForTracker(
        MultiDayPourPlan plannedData,
        ActualPourFeedback actualData = null)
    {
        var result = new Dictionary<DateTime, List<TrackerDayGroup>>();

        foreach (var dailyPlan in plannedData.DailyPlans)
        {
            var dailyPlanDate = DateTime.Parse(dailyPlan.Date);
            var dayGroups = new List<TrackerDayGroup>();

            // Group by order date first (the date when the order was placed, shown in yellow column)
            var ordersByDate = dailyPlan.ProcessedWorkOrders
                .GroupBy(wo => wo.OrderDate != DateTime.MinValue ? wo.OrderDate : dailyPlanDate) // Use OrderDate if available, else use plan date
                .OrderBy(g => g.Key);

            foreach (var orderDateGroup in ordersByDate)
            {
                var orderDate = orderDateGroup.Key;
                
                // Then group by company within each order date
                var companiesForDate = orderDateGroup
                    .GroupBy(wo => wo.Company)
                    .OrderBy(g => g.Key);

                var companyRows = new List<TrackerWorkOrder>();

                foreach (var companyGroup in companiesForDate)
                {
                    var company = companyGroup.Key;
                    var companyWorkOrders = companyGroup.ToList();

                    var trackerOrder = new TrackerWorkOrder
                    {
                        OrderDate = orderDate,
                        DailyPlanDate = dailyPlanDate,
                        DayName = dailyPlan.DayName,
                        Company = company,
                        PurchaseOrderString = string.Join(", ", companyWorkOrders.Select(w => $"PO# {w.PurchaseOrder}").Distinct()),
                        Builder = companyWorkOrders.FirstOrDefault()?.Builder ?? "",
                        FullOrder = new List<string>(),
                        PlannedToBePoured = new List<string>(),
                        SuggestedMolds = new List<string>(),
                        LeftToBePoured = new List<string>()
                    };

                    // Collect all lot names for this company
                    var lotNames = new HashSet<string>();

                    // Group items by PO and Lot for organized display
                    var itemsByPOAndLot = new Dictionary<(string PO, string Lot), List<dynamic>>();

                    foreach (var companyWo in companyWorkOrders)
                    {
                        foreach (var item in companyWo.ItemProgress)
                        {
                            lotNames.Add(item.LotName);
                            var key = (companyWo.PurchaseOrder, item.LotName);
                            
                            if (!itemsByPOAndLot.ContainsKey(key))
                            {
                                itemsByPOAndLot[key] = new List<dynamic>();
                            }
                            
                            itemsByPOAndLot[key].Add(new 
                            { 
                                Item = item, 
                                PO = companyWo.PurchaseOrder,
                                DailyPlan = dailyPlan
                            });
                        }
                    }

                    // Process items grouped by PO and Lot
                    foreach (var poLotGroup in itemsByPOAndLot.OrderBy(x => x.Key.PO).ThenBy(x => x.Key.Lot))
                    {
                        var po = poLotGroup.Key.PO;
                        var lot = poLotGroup.Key.Lot;
                        
                        // Add PO# and Lot header
                        trackerOrder.FullOrder.Add($"PO# {po}, {lot}");
                        var hasPlannedItems = false;
                        var hasLeftItems = false;

                        foreach (var itemData in poLotGroup.Value)
                        {
                            var item = itemData.Item;
                            var dailyPlanForItem = itemData.DailyPlan;

                            string type = item.Type;
                            var nameString = string.Join("\"",
                                type.Split(" ", StringSplitOptions.RemoveEmptyEntries)
                                    .Select(x => x.ToUpper()[0]));

                            // Full Order - show all items under this PO/Lot
                            trackerOrder.FullOrder.Add(
                                $"{item.OriginalQuantity}-({item.PourWidth}\" x {item.PourLength}\") {nameString} (BLK A)");

                            // Planned for this day
                            if (item.DailyProcessedQuantity.ContainsKey(dailyPlanForItem.Date))
                            {
                                if (!hasPlannedItems)
                                {
                                    trackerOrder.PlannedToBePoured.Add($"PO# {po}, {lot}");
                                    hasPlannedItems = true;
                                }

                                int plannedQty = item.DailyProcessedQuantity[dailyPlanForItem.Date];
                                trackerOrder.PlannedToBePoured.Add(
                                    $"{plannedQty}-({item.PourWidth}\" x {item.PourLength}\") {nameString} (BLK A)");

                                // Find suggested molds for these items
                                List<string> molds = ((List<StandardMold>)dailyPlanForItem.AllMolds)
                                    .Where(m => m.AllItems.Any(mi => mi.LotName == item.LotName))
                                    .Select(m => m.Name)
                                    .Distinct()
                                    .ToList();
                                
                                foreach (var mold in molds)
                                {
                                    if (!trackerOrder.SuggestedMolds.Contains(mold))
                                        trackerOrder.SuggestedMolds.Add(mold);
                                }
                            }

                            // Left to be poured (remaining after this day)
                            int remaining = item.RemainingQuantity;
                            if (item.DailyProcessedQuantity.ContainsKey(dailyPlanForItem.Date))
                            {
                                remaining = item.RemainingQuantity - item.DailyProcessedQuantity[dailyPlanForItem.Date];
                            }

                            if (remaining > 0)
                            {
                                if (!hasLeftItems)
                                {
                                    trackerOrder.LeftToBePoured.Add($"PO# {po}, {lot}");
                                    hasLeftItems = true;
                                }
                                
                                trackerOrder.LeftToBePoured.Add(
                                    $"{remaining}-({item.PourWidth}\" x {item.PourLength}\") {nameString} (BLK A)");
                            }
                        }
                    }

                    // Set the lot string
                    trackerOrder.LotString = string.Join(", ", lotNames);

                    companyRows.Add(trackerOrder);
                }

                dayGroups.Add(new TrackerDayGroup
                {
                    OrderDate = orderDate,
                    CompanyRows = companyRows
                });
            }

            if (dayGroups.Any())
            {
                result[dailyPlanDate] = dayGroups;
            }
        }

        return result;
    }

    private void DrawTrackerPage(PdfContentByte cb, DateTime dailyPlanDate, List<TrackerDayGroup> dayGroups,
        ActualPourFeedback actualData)
    {
        float pageWidth = PageSize.LETTER.Rotate().Width - (2 * MARGIN);
        float pageHeight = PageSize.LETTER.Rotate().Height - (2 * MARGIN);

        float startY = PageSize.LETTER.Rotate().Height - MARGIN - 20;
        float currentY = startY;

        // Column widths
        float dateColWidth = 60f;
        float companyColWidth = 80f;
        float poLotColWidth = 100f;
        float builderColWidth = 60f;
        float dateSectionWidth = dateColWidth + companyColWidth + poLotColWidth + builderColWidth;
        float contentSectionWidth = (pageWidth - dateSectionWidth) / 4;

        // Get the first order to extract day name
        var firstOrder = dayGroups.FirstOrDefault()?.CompanyRows.FirstOrDefault();
        if (firstOrder == null) return;

        // Draw the day header (spans entire date section)
        DrawDayHeader(cb, MARGIN, currentY, dateSectionWidth, dailyPlanDate, firstOrder.DayName);
        
        // Draw main column headers
        float currentX = MARGIN + dateSectionWidth;
        DrawMainColumnHeader(cb, currentX, currentY, contentSectionWidth, "FULL ORDER");
        currentX += contentSectionWidth;
        DrawMainColumnHeader(cb, currentX, currentY, contentSectionWidth+2, "PLANNED TO BE POURED");
        currentX += contentSectionWidth;
        DrawMainColumnHeader(cb, currentX, currentY, contentSectionWidth, "SUGGESTED MOLD");
        currentX += contentSectionWidth;
        DrawMainColumnHeader(cb, currentX, currentY, contentSectionWidth, "LEFT TO BE POURED");

        currentY -= 40f;

        // Draw each order date group
        foreach (var dayGroup in dayGroups)
        {
            // Calculate total height for all company rows in this date group
            float totalDateGroupHeight = 0f;
            var rowHeights = new List<float>();

            foreach (var order in dayGroup.CompanyRows)
            {
                int maxItems = Math.Max(
                    Math.Max(order.FullOrder.Count, order.PlannedToBePoured.Count),
                    Math.Max(order.SuggestedMolds.Count, order.LeftToBePoured.Count)
                );
                float rowHeight = Math.Max(60f, maxItems * 12f + 20f);
                rowHeights.Add(rowHeight);
                totalDateGroupHeight += rowHeight + 2f;
            }

            // Check if we need a new page
            if (currentY - totalDateGroupHeight < MARGIN)
            {
                break; // For now, just break. In production, add new page logic
            }

            float dateGroupStartY = currentY;

            // Draw the merged date cell for all companies with this order date
            DrawDateCell(cb, MARGIN, dateGroupStartY, dateColWidth, totalDateGroupHeight - 2f, 
                dayGroup.OrderDate.ToString("dd-MMM"));

            // Draw each company row
            for (int i = 0; i < dayGroup.CompanyRows.Count; i++)
            {
                var order = dayGroup.CompanyRows[i];
                float rowHeight = rowHeights[i];

                currentX = MARGIN + dateColWidth;

                // Column 2: Company (yellow)
                DrawYellowCell(cb, currentX, currentY, companyColWidth, rowHeight, order.Company);
                currentX += companyColWidth;

                // Column 3: PO/Lot (yellow)
                DrawPOLotCell(cb, currentX, currentY, poLotColWidth, rowHeight, 
                    order.PurchaseOrderString, order.LotString);
                currentX += poLotColWidth;

                // Column 4: Builder (yellow)
                DrawYellowCell(cb, currentX, currentY, builderColWidth, rowHeight, order.Builder);
                currentX += builderColWidth;

                // Content cells
                DrawContentCell(cb, currentX, currentY, contentSectionWidth, rowHeight,
                    order.FullOrder, GRAY_BG);
                currentX += contentSectionWidth;

                DrawContentCell(cb, currentX, currentY, contentSectionWidth, rowHeight,
                    order.PlannedToBePoured, LIGHT_BLUE_BG);
                currentX += contentSectionWidth;

                DrawContentCell(cb, currentX, currentY, contentSectionWidth, rowHeight,
                    order.SuggestedMolds, WHITE_BG);
                currentX += contentSectionWidth;

                DrawContentCell(cb, currentX, currentY, contentSectionWidth, rowHeight,
                    order.LeftToBePoured, WHITE_BG);

                currentY -= rowHeight + 2f;
            }
        }
    }

    private void DrawDayHeader(PdfContentByte cb, float x, float y, float width, DateTime date, string dayName)
    {
        float height = 40f;

        // Light blue/gray background
        cb.SaveState();
        cb.SetColorFill(new BaseColor(200, 200, 220));
        cb.Rectangle(x, y - height, width, height);
        cb.Fill();
        cb.RestoreState();

        // Border
        cb.SaveState();
        cb.SetColorStroke(BLACK_LINE);
        cb.SetLineWidth(1f);
        cb.Rectangle(x, y - height, width, height);
        cb.Stroke();
        cb.RestoreState();

        // Text
        cb.BeginText();
        cb.SetFontAndSize(_bfBold, 11);
        cb.SetColorFill(BaseColor.BLACK);
        cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, dayName, x + width / 2, y - 15, 0);
        cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, date.ToString("dd-MMM"), x + width / 2, y - 30, 0);
        cb.EndText();
    }

    private void DrawMainColumnHeader(PdfContentByte cb, float x, float y, float width, string text)
    {
        float height = 40f;

        // White background
        cb.SaveState();
        cb.SetColorFill(WHITE_BG);
        cb.Rectangle(x, y - height, width, height);
        cb.Fill();
        cb.RestoreState();

        // Border
        cb.SaveState();
        cb.SetColorStroke(BLACK_LINE);
        cb.SetLineWidth(1f);
        cb.Rectangle(x, y - height, width, height);
        cb.Stroke();
        cb.RestoreState();

        // Text
        cb.BeginText();
        cb.SetFontAndSize(_bfBold, 9);
        cb.SetColorFill(BaseColor.BLACK);
        cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, text, x + width / 2, y - 22, 0);
        cb.EndText();
    }

    private void DrawDateCell(PdfContentByte cb, float x, float y, float width, float height, string dateText)
    {
        // Yellow background
        cb.SaveState();
        cb.SetColorFill(YELLOW_BG);
        cb.Rectangle(x, y - height, width, height);
        cb.Fill();
        cb.RestoreState();

        // Border
        cb.SaveState();
        cb.SetColorStroke(BLACK_LINE);
        cb.SetLineWidth(1f);
        cb.Rectangle(x, y - height, width, height);
        cb.Stroke();
        cb.RestoreState();

        // Text - centered vertically in the merged cell
        cb.BeginText();
        cb.SetFontAndSize(_bfBold, 9);
        cb.SetColorFill(BaseColor.BLACK);
        cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, dateText, x + width / 2, y - height / 2 + 3, 0);
        cb.EndText();
    }

    private void DrawYellowCell(PdfContentByte cb, float x, float y, float width, float height, string text)
    {
        // Yellow background
        cb.SaveState();
        cb.SetColorFill(WHITE_BG);
        cb.Rectangle(x, y - height, width, height);
        cb.Fill();
        cb.RestoreState();

        // Border
        cb.SaveState();
        cb.SetColorStroke(BLACK_LINE);
        cb.SetLineWidth(1f);
        cb.Rectangle(x, y - height, width, height);
        cb.Stroke();
        cb.RestoreState();

        // Text with wrapping
        ColumnText ct = new ColumnText(cb);
        Phrase phrase = new Phrase(text, new Font(_bfBold, 8));
        ct.SetSimpleColumn(x + 3, y - height + 3, x + width - 3, y - 3, 10, Element.ALIGN_LEFT);
        ct.AddText(phrase);
        ct.Go();
    }

    private void DrawPOLotCell(PdfContentByte cb, float x, float y, float width, float height, 
        string poString, string lotString)
    {
        // Yellow background
        cb.SaveState();
        cb.SetColorFill(WHITE_BG);
        cb.Rectangle(x, y - height, width, height);
        cb.Fill();
        cb.RestoreState();

        // Border
        cb.SaveState();
        cb.SetColorStroke(BLACK_LINE);
        cb.SetLineWidth(1f);
        cb.Rectangle(x, y - height, width, height);
        cb.Stroke();
        cb.RestoreState();

        // Text - PO on top, Lot below
        float midY = y - height / 2;
        
        ColumnText ct = new ColumnText(cb);
        
        // PO String (top half)
        Phrase poPhrase = new Phrase(poString, new Font(_bfBold, 7));
        ct.SetSimpleColumn(x + 3, midY, x + width - 3, y - 3, 8, Element.ALIGN_LEFT);
        ct.AddText(poPhrase);
        ct.Go();
        
        // Lot String (bottom half)
        ct = new ColumnText(cb);
        Phrase lotPhrase = new Phrase(lotString, new Font(_bf, 7));
        ct.SetSimpleColumn(x + 3, y - height + 3, x + width - 3, midY, 8, Element.ALIGN_LEFT);
        ct.AddText(lotPhrase);
        ct.Go();
    }

    private void DrawContentCell(PdfContentByte cb, float x, float y, float width, float height,
        List<string> items, BaseColor bgColor)
    {
        // Background
        cb.SaveState();
        cb.SetColorFill((items != null && items.Any()) ? bgColor : GRAY_BG);
        cb.Rectangle(x, y - height, width, height);
        cb.Fill();
        cb.RestoreState();

        // Border
        cb.SaveState();
        cb.SetColorStroke(BLACK_LINE);
        cb.SetLineWidth(1f);
        cb.Rectangle(x, y - height, width, height);
        cb.Stroke();
        cb.RestoreState();

        // Content
        if (items != null && items.Any())
        {
            float textY = y - 12f;

            foreach (var item in items.Take(30))
            {
                cb.BeginText();
                cb.SetFontAndSize(_bf, 7);
                cb.SetColorFill(BaseColor.BLACK);

                // Wrap text if too long
                string displayText = item.Length > 50 ? item.Substring(0, 47) + "..." : item;
                cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, displayText, x + 5, textY, 0);
                cb.EndText();

                textY -= 10f;

                if (textY < y - height + 10) break;
            }

            if (items.Count > 30)
            {
                cb.BeginText();
                cb.SetFontAndSize(_bfBold, 7);
                cb.SetColorFill(BaseColor.RED);
                cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT,
                    $"...and {items.Count - 30} more", x + 5, y - height + 5, 0);
                cb.EndText();
            }
        }
        else
        {
            // Show "NILL" for empty cells
            cb.BeginText();
            cb.SetFontAndSize(_bfBold, 8);
            cb.SetColorFill(BaseColor.GRAY);
            //cb.SetColorFill(GRAY_BG);
            cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, "NILL", x + width / 2, y - height / 2, 0);
            cb.EndText();
        }
    }
}

#region Helper Classes

public class TrackerDayGroup
{
    public DateTime OrderDate { get; set; }
    public List<TrackerWorkOrder> CompanyRows { get; set; }
}

public class TrackerWorkOrder
{
    public DateTime OrderDate { get; set; }
    public DateTime DailyPlanDate { get; set; }
    public string DayName { get; set; }
    public string Company { get; set; }
    public string PurchaseOrder { get; set; }
    public string PurchaseOrderString { get; set; }
    public string LotString { get; set; }
    public string Builder { get; set; }
    public List<string> FullOrder { get; set; }
    public List<string> PlannedToBePoured { get; set; }
    public List<string> SuggestedMolds { get; set; }
    public List<string> LeftToBePoured { get; set; }
}

#endregion


// using MfgDocs.Api.Services.Others; 
// using System.Collections;
// using iTextSharp.text;
// using iTextSharp.text.pdf;
// using MfgDocs.Api.Services.Generators;
//
// /// <summary>
// /// Generates tracker documents showing planned vs actual pour data
// /// </summary>
// public class TrackerDocumentGenerator
// {
//     private readonly BaseFont _bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
//
//     private readonly BaseFont _bfBold =
//         BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
//
//     // Colors matching your screenshot
//     private readonly BaseColor YELLOW_BG = new BaseColor(255, 255, 0);
//     private readonly BaseColor GRAY_BG = new BaseColor(211, 211, 211);
//     private readonly BaseColor LIGHT_BLUE_BG = new BaseColor(173, 216, 230);
//     private readonly BaseColor WHITE_BG = BaseColor.WHITE;
//     private readonly BaseColor BLACK_LINE = BaseColor.BLACK;
//
//     private const float MARGIN = 20f;
//
//     public byte[] GenerateTrackerDocument(MultiDayPourPlan plannedData, ActualPourFeedback actualData = null)
//     {
//         using (var memoryStream = new MemoryStream())
//         {
//             // Landscape orientation for the tracker format
//             var document = new Document(PageSize.LETTER.Rotate(), MARGIN, MARGIN, MARGIN, MARGIN);
//
//             try
//             {
//                 PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
//                 document.Open();
//                 PdfContentByte cb = writer.DirectContent;
//
//                 // Group work orders by date and company
//                 var groupedOrders = GroupWorkOrdersByDateAndCompany(plannedData, actualData);
//
//                 bool firstPage = true;
//                 foreach (var dateGroup in groupedOrders)
//                 {
//                     if (!firstPage)
//                     {
//                         document.NewPage();
//                     }
//                     firstPage = false;
//
//                     DrawTrackerPage(cb, dateGroup.Key, dateGroup.Value, actualData);
//                 }
//
//                 document.Close();
//                 writer.Close();
//                 return memoryStream.ToArray();
//             }
//             catch (Exception ex)
//             {
//                 throw new Exception($"Error generating tracker document: {ex.Message}", ex);
//             }
//         }
//     }
//
//     private Dictionary<DateTime, List<TrackerWorkOrder>> GroupWorkOrdersByDateAndCompany(
//         MultiDayPourPlan plannedData,
//         ActualPourFeedback actualData = null)
//     {
//         var result = new Dictionary<DateTime, List<TrackerWorkOrder>>();
//
//         foreach (var dailyPlan in plannedData.DailyPlans)
//         {
//             var date = DateTime.Parse(dailyPlan.Date);
//             var trackerOrders = new List<TrackerWorkOrder>();
//
//             // Group work orders by company for this day
//             var companiesProcessed = new HashSet<string>();
//
//             foreach (var wo in dailyPlan.ProcessedWorkOrders)
//             {
//                 // Skip if we've already processed this company for this day
//                 if (companiesProcessed.Contains(wo.Company))
//                     continue;
//
//                 companiesProcessed.Add(wo.Company);
//
//                 // Get all work orders for this company on this day
//                 var companyWorkOrders = dailyPlan.ProcessedWorkOrders
//                     .Where(w => w.Company == wo.Company)
//                     .ToList();
//
//                 var trackerOrder = new TrackerWorkOrder
//                 {
//                     Date = date,
//                     DayName = dailyPlan.DayName,
//                     Company = wo.Company,
//                     PurchaseOrderString = string.Join(", ", companyWorkOrders.Select(w => w.PurchaseOrder).Distinct()),
//                     FullOrder = new List<string>(),
//                     PlannedToBePoured = new List<string>(),
//                     SuggestedMolds = new List<string>(),
//                     LeftToBePoured = new List<string>()
//                 };
//
//                 // Collect all lot names for this company
//                 var lotNames = new HashSet<string>();
//                 
//              
//                 // Process all work orders for this company
//                 foreach (var companyWo in companyWorkOrders)
//                 { 
//                     trackerOrder.FullOrder.Add( $"PO# {companyWo.PurchaseOrder}, ");
//                     foreach (var item in companyWo.ItemProgress)
//                     {
//                         lotNames.Add(item.LotName);
//
//                         var nameString = string.Join("",
//                             item.Type.Split(" ", StringSplitOptions.RemoveEmptyEntries)
//                                 .Select(x => x.ToUpper()[0]));
//
//                         // Full Order
//                         trackerOrder.FullOrder.Add(
//                             $"{item.OriginalQuantity}-({item.PourWidth}\" x {item.PourLength}\") {nameString} (BLK A)");
//
//                         // Planned for this day
//                         if (item.DailyProcessedQuantity.ContainsKey(dailyPlan.Date))
//                         {
//                             int plannedQty = item.DailyProcessedQuantity[dailyPlan.Date];
//                             trackerOrder.PlannedToBePoured.Add(
//                                 $"{plannedQty}-({item.PourWidth}\" x {item.PourLength}\") {nameString} (BLK A)");
//
//                             // Find suggested molds for these items
//                             var molds = dailyPlan.AllMolds
//                                 .Where(m => m.AllItems.Any(mi => mi.LotName == item.LotName))
//                                 .Select(m => m.Name)
//                                 .Distinct();
//                             foreach (var mold in molds)
//                             {
//                                 if (!trackerOrder.SuggestedMolds.Contains(mold))
//                                     trackerOrder.SuggestedMolds.Add(mold);
//                             }
//                         }
//
//                         // Left to be poured (remaining after this day)
//                         int remaining = item.RemainingQuantity;
//                         if (item.DailyProcessedQuantity.ContainsKey(dailyPlan.Date))
//                         {
//                             remaining = item.RemainingQuantity - item.DailyProcessedQuantity[dailyPlan.Date];
//                         }
//
//                         if (remaining > 0)
//                         {
//                             trackerOrder.LeftToBePoured.Add(
//                                 $"{remaining}-({item.PourWidth}\" x {item.PourLength}\") {nameString} (BLK A)");
//                         }
//                     }
//                 }
//
//                 // Set the lot string
//                 trackerOrder.LotString = string.Join(", ", lotNames);
//
//                 // Get builder info (assuming it's available somewhere in the work order)
//                 trackerOrder.Builder = companyWorkOrders.FirstOrDefault()?.Builder ?? "";
//
//                 trackerOrders.Add(trackerOrder);
//             }
//
//             if (trackerOrders.Any())
//             {
//                 result[date] = trackerOrders;
//             }
//         }
//
//         return result;
//     }
//
//     private void DrawTrackerPage(PdfContentByte cb, DateTime date, List<TrackerWorkOrder> orders,
//         ActualPourFeedback actualData)
//     {
//         float pageWidth = PageSize.LETTER.Rotate().Width - (2 * MARGIN);
//         float pageHeight = PageSize.LETTER.Rotate().Height - (2 * MARGIN);
//
//         float startY = PageSize.LETTER.Rotate().Height - MARGIN - 20;
//         float currentY = startY;
//
//         // Draw main section headers first
//         float currentX = MARGIN;
//         
//         // Date section width (4 sub-columns)
//         float dateSectionWidth = 300f;
//         float dateColWidth = 60f;
//         float companyColWidth = 80f;
//         float poLotColWidth = 100f;
//         float builderColWidth = 60f;
//
//         // Content sections
//         float contentSectionWidth = (pageWidth - dateSectionWidth) / 4;
//         
//         // Draw the day header (spans entire date section)
//         DrawDayHeader(cb, currentX, currentY, dateSectionWidth, date, orders[0].DayName);
//         
//         // Draw main column headers
//         currentX = MARGIN + dateSectionWidth;
//         DrawMainColumnHeader(cb, currentX, currentY, contentSectionWidth, "FULL ORDER");
//         currentX += contentSectionWidth;
//         DrawMainColumnHeader(cb, currentX, currentY, contentSectionWidth, "PLANNED TO BE POURED");
//         currentX += contentSectionWidth;
//         DrawMainColumnHeader(cb, currentX, currentY, contentSectionWidth, "SUGGESTED MOLD");
//         currentX += contentSectionWidth;
//         DrawMainColumnHeader(cb, currentX, currentY, contentSectionWidth, "LEFT TO BE POURED");
//
//         currentY -= 40f;
//
//         // Draw each company row
//         foreach (var order in orders)
//         {
//             // Calculate row height based on content
//             int maxItems = Math.Max(
//                 Math.Max(order.FullOrder.Count, order.PlannedToBePoured.Count),
//                 Math.Max(order.SuggestedMolds.Count, order.LeftToBePoured.Count)
//             );
//             float rowHeight = Math.Max(60f, maxItems * 12f + 20f);
//
//             // Check if we need a new page
//             if (currentY - rowHeight < MARGIN)
//             {
//                 break; // For now, just break. In production, add new page logic
//             }
//
//             currentX = MARGIN;
//
//             // Draw date section (4 columns)
//             // Column 1: Date (yellow)
//             DrawDateCell(cb, currentX, currentY, dateColWidth, rowHeight, date.ToString("dd-MMM"));
//             currentX += dateColWidth;
//
//             // Column 2: Company (yellow)
//             DrawYellowCell(cb, currentX, currentY, companyColWidth, rowHeight, order.Company);
//             currentX += companyColWidth;
//
//             // Column 3: PO/Lot (yellow)
//             DrawPOLotCell(cb, currentX, currentY, poLotColWidth, rowHeight, 
//                 order.PurchaseOrderString, order.LotString);
//             currentX += poLotColWidth;
//
//             // Column 4: Builder (yellow)
//             DrawYellowCell(cb, currentX, currentY, builderColWidth, rowHeight, order.Builder);
//             currentX += builderColWidth;
//
//             // Content cells
//             DrawContentCell(cb, currentX, currentY, contentSectionWidth, rowHeight,
//                 order.FullOrder, GRAY_BG);
//             currentX += contentSectionWidth;
//
//             DrawContentCell(cb, currentX, currentY, contentSectionWidth, rowHeight,
//                 order.PlannedToBePoured, LIGHT_BLUE_BG);
//             currentX += contentSectionWidth;
//
//             DrawContentCell(cb, currentX, currentY, contentSectionWidth, rowHeight,
//                 order.SuggestedMolds, WHITE_BG);
//             currentX += contentSectionWidth;
//
//             DrawContentCell(cb, currentX, currentY, contentSectionWidth, rowHeight,
//                 order.LeftToBePoured, WHITE_BG);
//
//             currentY -= rowHeight + 2f;
//         }
//     }
//
//     private void DrawDayHeader(PdfContentByte cb, float x, float y, float width, DateTime date, string dayName)
//     {
//         float height = 40f;
//
//         // Light blue/gray background
//         cb.SaveState();
//         cb.SetColorFill(new BaseColor(200, 200, 220));
//         cb.Rectangle(x, y - height, width, height);
//         cb.Fill();
//         cb.RestoreState();
//
//         // Border
//         cb.SaveState();
//         cb.SetColorStroke(BLACK_LINE);
//         cb.SetLineWidth(1f);
//         cb.Rectangle(x, y - height, width, height);
//         cb.Stroke();
//         cb.RestoreState();
//
//         // Text
//         cb.BeginText();
//         cb.SetFontAndSize(_bfBold, 11);
//         cb.SetColorFill(BaseColor.BLACK);
//         cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, dayName, x + width / 2, y - 15, 0);
//         cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, date.ToString("dd-MMM"), x + width / 2, y - 30, 0);
//         cb.EndText();
//     }
//
//     private void DrawMainColumnHeader(PdfContentByte cb, float x, float y, float width, string text)
//     {
//         float height = 40f;
//
//         // White background
//         cb.SaveState();
//         cb.SetColorFill(WHITE_BG);
//         cb.Rectangle(x, y - height, width, height);
//         cb.Fill();
//         cb.RestoreState();
//
//         // Border
//         cb.SaveState();
//         cb.SetColorStroke(BLACK_LINE);
//         cb.SetLineWidth(1f);
//         cb.Rectangle(x, y - height, width, height);
//         cb.Stroke();
//         cb.RestoreState();
//
//         // Text
//         cb.BeginText();
//         cb.SetFontAndSize(_bfBold, 9);
//         cb.SetColorFill(BaseColor.BLACK);
//         cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, text, x + width / 2, y - 22, 0);
//         cb.EndText();
//     }
//
//     private void DrawDateCell(PdfContentByte cb, float x, float y, float width, float height, string dateText)
//     {
//         // Yellow background
//         cb.SaveState();
//         cb.SetColorFill(YELLOW_BG);
//         cb.Rectangle(x, y - height, width, height);
//         cb.Fill();
//         cb.RestoreState();
//
//         // Border
//         cb.SaveState();
//         cb.SetColorStroke(BLACK_LINE);
//         cb.SetLineWidth(1f);
//         cb.Rectangle(x, y - height, width, height);
//         cb.Stroke();
//         cb.RestoreState();
//
//         // Text
//         cb.BeginText();
//         cb.SetFontAndSize(_bfBold, 9);
//         cb.SetColorFill(BaseColor.BLACK);
//         cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, dateText, x + width / 2, y - height / 2 + 3, 0);
//         cb.EndText();
//     }
//
//     private void DrawYellowCell(PdfContentByte cb, float x, float y, float width, float height, string text)
//     {
//         // Yellow background
//         cb.SaveState();
//         cb.SetColorFill(YELLOW_BG);
//         cb.Rectangle(x, y - height, width, height);
//         cb.Fill();
//         cb.RestoreState();
//
//         // Border
//         cb.SaveState();
//         cb.SetColorStroke(BLACK_LINE);
//         cb.SetLineWidth(1f);
//         cb.Rectangle(x, y - height, width, height);
//         cb.Stroke();
//         cb.RestoreState();
//
//         // Text with wrapping
//         ColumnText ct = new ColumnText(cb);
//         Phrase phrase = new Phrase(text, new Font(_bfBold, 8));
//         ct.SetSimpleColumn(x + 3, y - height + 3, x + width - 3, y - 3, 10, Element.ALIGN_LEFT);
//         ct.AddText(phrase);
//         ct.Go();
//     }
//
//     private void DrawPOLotCell(PdfContentByte cb, float x, float y, float width, float height, 
//         string poString, string lotString)
//     {
//         // Yellow background
//         cb.SaveState();
//         cb.SetColorFill(YELLOW_BG);
//         cb.Rectangle(x, y - height, width, height);
//         cb.Fill();
//         cb.RestoreState();
//
//         // Border
//         cb.SaveState();
//         cb.SetColorStroke(BLACK_LINE);
//         cb.SetLineWidth(1f);
//         cb.Rectangle(x, y - height, width, height);
//         cb.Stroke();
//         cb.RestoreState();
//
//         // Text - PO on top, Lot below
//         float midY = y - height / 2;
//         
//         ColumnText ct = new ColumnText(cb);
//         
//         // PO String (top half)
//         Phrase poPhrase = new Phrase(poString, new Font(_bfBold, 7));
//         ct.SetSimpleColumn(x + 3, midY, x + width - 3, y - 3, 8, Element.ALIGN_LEFT);
//         ct.AddText(poPhrase);
//         ct.Go();
//         
//         // Lot String (bottom half)
//         ct = new ColumnText(cb);
//         Phrase lotPhrase = new Phrase(lotString, new Font(_bf, 7));
//         ct.SetSimpleColumn(x + 3, y - height + 3, x + width - 3, midY, 8, Element.ALIGN_LEFT);
//         ct.AddText(lotPhrase);
//         ct.Go();
//     }
//
//     private void DrawContentCell(PdfContentByte cb, float x, float y, float width, float height,
//         List<string> items, BaseColor bgColor)
//     {
//         // Background
//         cb.SaveState();
//         cb.SetColorFill(bgColor);
//         cb.Rectangle(x, y - height, width, height);
//         cb.Fill();
//         cb.RestoreState();
//
//         // Border
//         cb.SaveState();
//         cb.SetColorStroke(BLACK_LINE);
//         cb.SetLineWidth(1f);
//         cb.Rectangle(x, y - height, width, height);
//         cb.Stroke();
//         cb.RestoreState();
//
//         // Content
//         if (items != null && items.Any())
//         {
//             float textY = y - 12f;
//
//             foreach (var item in items.Take(30)) // Limit items shown
//             {
//                 cb.BeginText();
//                 cb.SetFontAndSize(_bf, 7);
//                 cb.SetColorFill(BaseColor.BLACK);
//
//                 // Wrap text if too long
//                 string displayText = item.Length > 50 ? item.Substring(0, 47) + "..." : item;
//                 cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, displayText, x + 5, textY, 0);
//                 cb.EndText();
//
//                 textY -= 10f;
//
//                 if (textY < y - height + 10) break;
//             }
//
//             if (items.Count > 30)
//             {
//                 cb.BeginText();
//                 cb.SetFontAndSize(_bfBold, 7);
//                 cb.SetColorFill(BaseColor.RED);
//                 cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT,
//                     $"...and {items.Count - 30} more", x + 5, y - height + 5, 0);
//                 cb.EndText();
//             }
//         }
//         else
//         {
//             // Show "NILL" for empty cells
//             cb.BeginText();
//             cb.SetFontAndSize(_bfBold, 8);
//             cb.SetColorFill(BaseColor.GRAY);
//             cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, "NILL", x + width / 2, y - height / 2, 0);
//             cb.EndText();
//         }
//     }
// }
//
// #region Helper Classes
//
// public class TrackerWorkOrder
// {
//     public DateTime Date { get; set; }
//     public string DayName { get; set; }
//     public string Company { get; set; }
//     public string PurchaseOrder { get; set; }
//     public string PurchaseOrderString { get; set; }
//     public string LotString { get; set; }
//     public string Builder { get; set; }
//     public List<string> FullOrder { get; set; }
//     public List<string> PlannedToBePoured { get; set; }
//     public List<string> SuggestedMolds { get; set; }
//     public List<string> LeftToBePoured { get; set; }
// }
//
// #endregion
//

// using MfgDocs.Api.Services.Others;
//
// namespace MfgDocs.Api.Services.Generators;
//  
// using System.Collections;
// using iTextSharp.text;
// using iTextSharp.text.pdf;
// using MfgDocs.Api.Services.Generators; 
//  
//
// /// <summary>
// /// Generates tracker documents showing planned vs actual pour data
// /// </summary>
// public class TrackerDocumentGenerator
// {
//     private readonly BaseFont _bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
//     private readonly BaseFont _bfBold = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
//
//     // Colors matching your screenshot
//     private readonly BaseColor YELLOW_BG = new BaseColor(255, 255, 0);
//     private readonly BaseColor GRAY_BG = new BaseColor(211, 211, 211);
//     private readonly BaseColor LIGHT_BLUE_BG = new BaseColor(173, 216, 230);
//     private readonly BaseColor WHITE_BG = BaseColor.WHITE;
//     private readonly BaseColor BLACK_LINE = BaseColor.BLACK;
//
//     private const float MARGIN = 20f;
//
//     public byte[] GenerateTrackerDocument(MultiDayPourPlan plannedData, ActualPourFeedback actualData = null)
//     {
//         using (var memoryStream = new MemoryStream())
//         {
//             // Landscape orientation for the tracker format
//             var document = new Document(PageSize.LETTER.Rotate(), MARGIN, MARGIN, MARGIN, MARGIN);
//             
//             try
//             {
//                 PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
//                 document.Open();
//                 PdfContentByte cb = writer.DirectContent;
//
//                 // Group work orders by date
//                 var workOrdersByDate = GroupWorkOrdersByDate(plannedData, actualData);
//
//                 foreach (KeyValuePair<DateTime, List<TrackerWorkOrder>> dateGroup in workOrdersByDate)
//                 {
//                     var firstDateGroup = workOrdersByDate.FirstOrDefault(); // Line 46
//      
//                     if (dateGroup.Key != firstDateGroup.Key) 
//                     {
//                         document.NewPage();
//                     }
//
//                     DrawTrackerPage(cb, dateGroup.Key, dateGroup.Value, actualData);
//                 }
//
//                 document.Close();
//                 writer.Close();
//                 return memoryStream.ToArray();
//             }
//             catch (Exception ex)
//             {
//                 throw new Exception($"Error generating tracker document: {ex.Message}", ex);
//             }
//         }
//     }
//
//     private Dictionary<DateTime, List<TrackerWorkOrder>> GroupWorkOrdersByDate(
//         MultiDayPourPlan plannedData, 
//         ActualPourFeedback actualData = null)
//     {
//         var result = new Dictionary<DateTime, List<TrackerWorkOrder>>();
//
//         foreach (var dailyPlan in plannedData.DailyPlans)
//         {
//             var date = DateTime.Parse(dailyPlan.Date);
//             var trackerOrders = new List<TrackerWorkOrder>();
//
//             // foreach (var wo in dailyPlan.ProcessedWorkOrders)
//             foreach (var wo in dailyPlan.ProcessedWorkOrders)
//             {
//                 var trackerOrder = new TrackerWorkOrder
//                 {
//                     Date = date,
//                     DayName = dailyPlan.DayName,
//                     Company = wo.Company,
//                     PurchaseOrder = wo.PurchaseOrder,
//                     FullOrder = new List<string>(),
//                     PlannedToBePoured = new List<string>(),
//                     SuggestedMolds = new List<string>(),
//                     LeftToBePoured = new List<string>()
//                 };
//
//                 // Get all items for this work order
//                 foreach (var item in wo.ItemProgress)
//                 {
//                     // Full order
//                     for (int i = 0; i < item.OriginalQuantity; i++)
//                     {
//                         var nameString = string.Join("\"",
//                             item.Type.Split(" ").Where(x => x.Length > 0).Select(x => (x.ToUpper())[0]));
//                        trackerOrder.FullOrder.Add($"{item.PourWidth}\" x {item.PourLength}\" {item.Type} ({item.LotName})");
//                       //  trackerOrder.FullOrder.Add($"{item.OriginalQuantity}-({item.PourWidth}\" x {item.PourLength}\") {nameString} (BLK A)");
//                     }
//
//                     // Planned for this day
//                     if (item.DailyProcessedQuantity.ContainsKey(dailyPlan.Date))
//                     {
//                         int plannedQty = item.DailyProcessedQuantity[dailyPlan.Date];
//                         for (int i = 0; i < plannedQty; i++)
//                         {
//                             trackerOrder.PlannedToBePoured.Add($"{item.PourWidth}\" x {item.PourLength}\" {item.Type} ({item.LotName})");
//                         }
//
//                         // Find suggested molds for these items
//                         var molds = dailyPlan.AllMolds
//                             .Where(m => m.AllItems.Any(mi => mi.LotName == item.LotName))
//                             .Select(m => m.Name)
//                             .Distinct();
//                         trackerOrder.SuggestedMolds.AddRange(molds);
//                     }
//
//                     // Left to be poured (remaining after this day)
//                     int remaining = item.RemainingQuantity;
//                     if (item.DailyProcessedQuantity.ContainsKey(dailyPlan.Date))
//                     {
//                         remaining = item.RemainingQuantity - item.DailyProcessedQuantity[dailyPlan.Date];
//                     }
//
//                     for (int i = 0; i < remaining; i++)
//                     {
//                         trackerOrder.LeftToBePoured.Add($"{item.PourWidth}\" x {item.PourLength}\" {item.Type} ({item.LotName})");
//                     }
//                 }
//
//                 trackerOrders.Add(trackerOrder);
//             }
//
//             result[date] = trackerOrders;
//         }
//
//         return result;
//     }
//
//     private void DrawTrackerPage(PdfContentByte cb, DateTime date, List<TrackerWorkOrder> orders, ActualPourFeedback actualData)
//     {
//         float pageWidth = PageSize.LETTER.Rotate().Width - (2 * MARGIN);
//         float pageHeight = PageSize.LETTER.Rotate().Height - (2 * MARGIN);
//         
//         float startY = PageSize.LETTER.Rotate().Height - MARGIN - 20;
//         float currentY = startY;
//
//         // Column widths (5 columns total)
//         float col1Width = 100f; // Date/Info column (yellow)
//         float col2Width = (pageWidth - col1Width) / 4; // FULL ORDER
//         float col3Width = (pageWidth - col1Width) / 4; // PLANNED TO BE POURED
//         float col4Width = (pageWidth - col1Width) / 4; // SUGGESTED MOLD
//         float col5Width = (pageWidth - col1Width) / 4; // LEFT TO BE POURED
//
//         float currentX = MARGIN;
//
//         foreach (var order in orders)
//         {
//             // Calculate row height based on content
//             int maxItems = Math.Max(
//                 Math.Max(order.FullOrder.Count, order.PlannedToBePoured.Count),
//                 Math.Max(order.SuggestedMolds.Count, order.LeftToBePoured.Count)
//             );
//             float rowHeight = Math.Max(80f, maxItems * 15f + 40f);
//
//             // Check if we need a new page
//             if (currentY - rowHeight < MARGIN)
//             {
//                 // Continue on same page for now, in real implementation add new page logic
//             }
//
//             currentX = MARGIN;
//
//             // Draw the row structure
//             // Column 1: Date header (spanning 2 rows)
//             DrawDateHeader(cb, currentX, currentY, col1Width, order.Date, order.DayName);
//             
//             // Column 1: Company info
//             DrawCompanyInfo(cb, currentX, currentY - 40f, col1Width, rowHeight - 40f, 
//                 order.Company, order.PurchaseOrder);
//
//             currentX += col1Width;
//
//             // Column headers
//             DrawColumnHeader(cb, currentX, currentY, col2Width, "FULL ORDER");
//             DrawColumnHeader(cb, currentX + col2Width, currentY, col3Width, "PLANNED TO BE POURED");
//             DrawColumnHeader(cb, currentX + col2Width + col3Width, currentY, col4Width, "SUGGESTED MOLD");
//             DrawColumnHeader(cb, currentX + col2Width + col3Width + col4Width, currentY, col5Width, "LEFT TO BE POURED");
//
//             // Content cells
//             DrawContentCell(cb, currentX, currentY - 40f, col2Width, rowHeight - 40f, 
//                 order.FullOrder, GRAY_BG);
//             
//             DrawContentCell(cb, currentX + col2Width, currentY - 40f, col3Width, rowHeight - 40f, 
//                 order.PlannedToBePoured, LIGHT_BLUE_BG);
//             
//             DrawContentCell(cb, currentX + col2Width + col3Width, currentY - 40f, col4Width, rowHeight - 40f, 
//                 order.SuggestedMolds, WHITE_BG);
//             
//             DrawContentCell(cb, currentX + col2Width + col3Width + col4Width, currentY - 40f, col5Width, rowHeight - 40f, 
//                 order.LeftToBePoured, WHITE_BG);
//
//             currentY -= rowHeight + 5f;
//         }
//     }
//
//     private void DrawDateHeader(PdfContentByte cb, float x, float y, float width, DateTime date, string dayName)
//     {
//         float height = 40f;
//
//         // Yellow background
//         cb.SaveState();
//         cb.SetColorFill(YELLOW_BG);
//         cb.Rectangle(x, y - height, width, height);
//         cb.Fill();
//         cb.RestoreState();
//
//         // Black border
//         cb.SaveState();
//         cb.SetColorStroke(BLACK_LINE);
//         cb.SetLineWidth(1.5f);
//         cb.Rectangle(x, y - height, width, height);
//         cb.Stroke();
//         cb.RestoreState();
//
//         // Text
//         cb.BeginText();
//         cb.SetFontAndSize(_bfBold, 10);
//         cb.SetColorFill(BaseColor.BLACK);
//         cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, dayName, x + width / 2, y - 15, 0);
//         cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, date.ToString("dd-MMM"), x + width / 2, y - 30, 0);
//         cb.EndText();
//     }
//
//     private void DrawCompanyInfo(PdfContentByte cb, float x, float y, float width, float height, 
//         string company, string poNumber)
//     {
//         // Yellow background
//         cb.SaveState();
//         cb.SetColorFill(YELLOW_BG);
//         cb.Rectangle(x, y - height, width, height);
//         cb.Fill();
//         cb.RestoreState();
//
//         // Black border
//         cb.SaveState();
//         cb.SetColorStroke(BLACK_LINE);
//         cb.SetLineWidth(1.5f);
//         cb.Rectangle(x, y - height, width, height);
//         cb.Stroke();
//         cb.RestoreState();
//
//         // Company text
//         cb.BeginText();
//         cb.SetFontAndSize(_bfBold, 9);
//         cb.SetColorFill(BaseColor.BLACK);
//         
//         ColumnText ct = new ColumnText(cb);
//         Phrase phrase = new Phrase(company, new Font(_bfBold, 8));
//         ct.SetSimpleColumn(phrase, x + 5, y - height + 5, x + width - 5, y, 10, Element.ALIGN_LEFT);
//         ct.Go();
//
//         // PO Number
//         cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, $"PO#: {poNumber}", x + 5, y - height + 25, 0);
//         cb.EndText();
//     }
//
//     private void DrawColumnHeader(PdfContentByte cb, float x, float y, float width, string text)
//     {
//         float height = 40f;
//
//         // White background
//         cb.SaveState();
//         cb.SetColorFill(WHITE_BG);
//         cb.Rectangle(x, y - height, width, height);
//         cb.Fill();
//         cb.RestoreState();
//
//         // Black border
//         cb.SaveState();
//         cb.SetColorStroke(BLACK_LINE);
//         cb.SetLineWidth(1.5f);
//         cb.Rectangle(x, y - height, width, height);
//         cb.Stroke();
//         cb.RestoreState();
//
//         // Text
//         cb.BeginText();
//         cb.SetFontAndSize(_bfBold, 9);
//         cb.SetColorFill(BaseColor.BLACK);
//         cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, text, x + width / 2, y - 20, 0);
//         cb.EndText();
//     }
//
//     private void DrawContentCell(PdfContentByte cb, float x, float y, float width, float height, 
//         List<string> items, BaseColor bgColor)
//     {
//         // Background
//         cb.SaveState();
//         cb.SetColorFill(bgColor);
//         cb.Rectangle(x, y - height, width, height);
//         cb.Fill();
//         cb.RestoreState();
//
//         // Border
//         cb.SaveState();
//         cb.SetColorStroke(BLACK_LINE);
//         cb.SetLineWidth(1.5f);
//         cb.Rectangle(x, y - height, width, height);
//         cb.Stroke();
//         cb.RestoreState();
//
//         // Content
//         if (items != null && items.Any())
//         {
//             float textY = y - 15f;
//             
//             foreach (var item in items.Take(20)) // Limit items shown
//             {
//                 cb.BeginText();
//                 cb.SetFontAndSize(_bf, 7);
//                 cb.SetColorFill(BaseColor.BLACK);
//                 
//                 // Wrap text if too long
//                 string displayText = item.Length > 40 ? item.Substring(0, 37) + "..." : item;
//                 cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, displayText, x + 5, textY, 0);
//                 cb.EndText();
//                 
//                 textY -= 12f;
//                 
//                 if (textY < y - height + 10) break; // Stop if no more room
//             }
//
//             // Show count if more items
//             if (items.Count > 20)
//             {
//                 cb.BeginText();
//                 cb.SetFontAndSize(_bfBold, 7);
//                 cb.SetColorFill(BaseColor.RED);
//                 cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, 
//                     $"...and {items.Count - 20} more", x + 5, y - height + 5, 0);
//                 cb.EndText();
//             }
//         }
//         else
//         {
//             // Show "NILL" or "None" for empty cells
//             cb.BeginText();
//             cb.SetFontAndSize(_bfBold, 8);
//             cb.SetColorFill(BaseColor.GRAY);
//             cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, "NILL", x + width / 2, y - height / 2, 0);
//             cb.EndText();
//         }
//     }
// }
//
// #region Helper Classes
//
// public class TrackerWorkOrder
// {
//     public DateTime Date { get; set; }
//     public string DayName { get; set; }
//     public string Company { get; set; }
//     public string PurchaseOrder { get; set; }
//     public string PurchaseOrderString { get; set; }
//     public string LotString { get; set; }
//     public List<string> FullOrder { get; set; }
//     public List<string> PlannedToBePoured { get; set; }
//     public List<string> SuggestedMolds { get; set; }
//     public List<string> LeftToBePoured { get; set; }
// }
//
// #endregion