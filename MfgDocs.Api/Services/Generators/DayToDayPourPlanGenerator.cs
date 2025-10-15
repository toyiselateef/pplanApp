using System.Collections;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MfgDocs.Api.Extensions;
using MfgDocs.Api.Models;
using Microsoft.Extensions.Options;


#region 2nd

namespace MfgDocs.Api.Services.Generators;

// #region Data Models

public class DailyPourPlan
{
    public string Date { get; set; }
    public string DayName { get; set; }
    public string Color { get; set; }
    public List<StandardMold> AllMolds { get; set; } = new List<StandardMold>();

    public Dictionary<string, List<StandardMold>> PourGroups { get; set; } =
        new Dictionary<string, List<StandardMold>>();

    public List<TableRow> CalculationTable { get; set; } = new List<TableRow>();
    public bool HasItems => AllMolds.Any(m => m.HasItems);
    public int TotalItemsProcessed => AllMolds.Sum(m => m.AllItems.Count());
    public float TotalCubicYards => CalculationTable.Sum(r => r.CubicYards);
    public float TotalArea => CalculationTable.Sum(r => r.TotalAreaRequired);

    public List<WorkOrderProgress> ProcessedWorkOrders { get; set; } = new List<WorkOrderProgress>();
    // public List<TrackerWorkOrder> AllProcessedWorkOrdersTracker => ;
}

public class MultiDayPourPlan
{
    public List<DailyPourPlan> DailyPlans { get; set; } = new List<DailyPourPlan>();
    public List<WorkOrderProgress> UnprocessedOrders { get; set; } = new List<WorkOrderProgress>();
    public List<WorkOrderProgress> PartiallyProcessedOrders { get; set; } = new List<WorkOrderProgress>();
    public List<WorkOrderProgress> FullyProcessedOrders { get; set; } = new List<WorkOrderProgress>();

    public PourPlanSummary Summary { get; set; } = new PourPlanSummary();
}

public class WorkOrderProgress
{
    public string Builder { get; set; } = String.Empty;
    public string PurchaseOrder { get; set; } = String.Empty;
    public string Company { get; set; } = String.Empty;
    public int Priority { get; set; }
    public DateTime OrderDate { get; set; }
    public string ExpectedDeliveryDate { get; set; }
    public List<ItemProgress> ItemProgress { get; set; } = new List<ItemProgress>();
    public bool IsFullyProcessed => ItemProgress.All(i => i.RemainingQuantity == 0);
    public bool IsPartiallyProcessed => ItemProgress.Any(i => i.ProcessedQuantity > 0 && i.RemainingQuantity > 0);
    public int TotalRemainingItems => ItemProgress.Sum(i => i.RemainingQuantity);
    public int TotalOriginalItems => ItemProgress.Sum(i => i.OriginalQuantity);

    public double CompletionPercentage => TotalOriginalItems > 0
        ? (double)(TotalOriginalItems - TotalRemainingItems) / TotalOriginalItems * 100
        : 0;
}

public class ItemProgress
{
    public string LotName { get; set; }
    public int OriginalQuantity { get; set; }
    public int ProcessedQuantity { get; set; }
    public int RemainingQuantity => OriginalQuantity - ProcessedQuantity;
    public double PourWidth { get; set; }
    public double PourLength { get; set; }
    public double FinishedLength { get; set; }
    public double FinishedWidth { get; set; }
    public string Color { get; set; }
    public string Type { get; set; }
    public List<string> ProcessedOnDays { get; set; } = new List<string>(); // Track which days items were processed

    public Dictionary<string, int> DailyProcessedQuantity { get; set; } =
        new Dictionary<string, int>(); // Track quantity per day
}

public class WorkOrderRequest5
{
    private DateTime? _orderDate;

    // Expose as string, store as DateTime
    public DateTime? OrderDate2 { get; set; }

    public DateTime OrderDate { get; set; }

    public string PurchaseOrder { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public string Builder { get; set; } = string.Empty;
    public string Site { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string BlkNo { get; set; } = string.Empty;
    public string LotNo { get; set; } = string.Empty;
    public List<Order> Items { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
    public string ExpectedDeliveryDate { get; set; } = string.Empty;
    public DateTime? ExpectedDeliveryDate2 { get; set; }
    public int Priority { get; set; } = 0;
}

public class StandardMold
{
    public string Name { get; set; }
    public double Width { get; set; }
    public double Length { get; set; }
    public string PourCategory { get; set; }
    public List<SlottedItem> TopSideItems { get; set; } = new List<SlottedItem>();
    public List<SlottedItem> BottomSideItems { get; set; } = new List<SlottedItem>();
    public bool HasBottomSide => BottomSideItems.Any();
    public bool HasItems => TopSideItems.Any() || BottomSideItems.Any();
    public List<MoldSide> Sides { get; set; }
    public List<SlottedItem> AllItems => TopSideItems.Concat(BottomSideItems).ToList();
}

public class MoldSide
{
    public List<SlottedItem> Items { get; set; } = new List<SlottedItem>();
    public double StartPosition { get; set; }
    public double Width { get; set; }
    public double MaxLength { get; set; }
    public double UsedLength => Items.Any() ? Items.Max(i => i.XPosition + i.Length) : 0;

    public double RemainingLength => MaxLength - UsedLength - 3; // Account for margin

    //
    public float XPosition { get; set; }
    public float YPosition { get; set; }

    public float Length { get; set; }

    //
    public bool CanFitItem(SlottedItem item) => item.Length <= RemainingLength && item.Width <= Width;
}

public class SlottedItem
{
    public double Width { get; set; }
    public double Length { get; set; }
    public string SourceOrder { get; set; }
    public string LotName { get; set; }
    public double XPosition { get; set; } // Position from left edge
    public double YPosition { get; set; } // Position from top edge (for top side) or bottom edge (for bottom side)
}

public class PourPlan
{
    public string Date { get; set; }
    public string Color { get; set; }
    public List<StandardMold> AllMolds { get; set; } = new List<StandardMold>();

    public Dictionary<string, List<StandardMold>> PourGroups { get; set; } =
        new Dictionary<string, List<StandardMold>>();

    public List<TableRow> CalculationTable { get; set; } = new List<TableRow>();
}

public class WorkOrderPriorityCriteria
{
    public string Field { get; set; } // "Priority", "OrderDate", "ExpectedDeliveryDate"
    public bool Ascending { get; set; } = true;
    public int Order { get; set; } = 0;
}

public class PouringPlanConfig
{
    public List<string> WorkDays { get; set; } = new List<string>
        { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

    public int MaxWorkDaysToGenerate { get; set; } = 1;
    public bool AllowExtensionBeyondMaxDays { get; set; } = false;
    public List<WorkOrderPriorityCriteria> PriorityCriteria { get; set; } = new List<WorkOrderPriorityCriteria>();
    public double MarginSize { get; set; } = 2.0;
    public float ScaleFactor { get; set; } = 1.5f;
    public bool TrackUnprocessedItems { get; set; } = true;
}

public class PourPlanSummary
{
    public int TotalWorkOrders { get; set; }
    public int ProcessedWorkOrders { get; set; }
    public int PartiallyProcessedWorkOrders { get; set; }
    public int UnprocessedWorkOrders { get; set; }
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public int RemainingItems { get; set; }
    public double OverallCompletionPercentage { get; set; }
    public int TotalPourDays { get; set; }
    public List<string> PourDates { get; set; } = new List<string>();
}

// #endregion

public class DailyPouringPlanGenerator
{
    private readonly PouringPlanConfig _config;
    private readonly List<StandardMold> _standardMolds;
    private const double MARGIN_SIZE = 3.0;

    public DailyPouringPlanGenerator(IOptions<PouringPlanConfig> config)
    {
        _config = config.Value;
        _standardMolds = InitializeStandardMolds();
    }

    private List<StandardMold> InitializeStandardMolds()
    {
        return new List<StandardMold>
        {
            new StandardMold { Name = "A", Width = 30, Length = 156, PourCategory = "P2" },
            new StandardMold { Name = "B", Width = 36, Length = 156, PourCategory = "P3" },
            new StandardMold { Name = "C", Width = 50, Length = 122, PourCategory = "P1" },
            new StandardMold { Name = "D", Width = 51.5, Length = 120, PourCategory = "P2" },
            new StandardMold { Name = "E", Width = 26, Length = 144, PourCategory = "P4" },
            new StandardMold { Name = "F", Width = 22, Length = 144, PourCategory = "P5" },
            new StandardMold { Name = "G", Width = 24, Length = 120, PourCategory = "P4" },
            new StandardMold { Name = "H", Width = 26, Length = 120, PourCategory = "P1" },
            new StandardMold { Name = "I", Width = 22, Length = 120, PourCategory = "P4" },
            new StandardMold { Name = "J", Width = 20, Length = 120, PourCategory = "P1" },
            new StandardMold { Name = "K", Width = 51, Length = 122, PourCategory = "P3" },
            new StandardMold { Name = "L", Width = 61, Length = 122, PourCategory = "P5" }
        };
    }

    private void CategorizeWorkOrders(MultiDayPourPlan multiDayPlan, List<WorkOrderProgress> workOrderProgress)
    {
        multiDayPlan.FullyProcessedOrders = workOrderProgress.Where(w => w.IsFullyProcessed).ToList();
        multiDayPlan.PartiallyProcessedOrders =
            workOrderProgress.Where(w => w.IsPartiallyProcessed && !w.IsFullyProcessed).ToList();
        multiDayPlan.UnprocessedOrders =
            workOrderProgress.Where(w => !w.IsPartiallyProcessed && !w.IsFullyProcessed).ToList();
    }

    // Create fresh mold instances for each day
    private List<StandardMold> CreateFreshMoldInstances()
    {
        return _standardMolds.Select(m => new StandardMold
        {
            Name = m.Name,
            Width = m.Width,
            Length = m.Length,
            PourCategory = m.PourCategory,
            TopSideItems = new List<SlottedItem>(),
            BottomSideItems = new List<SlottedItem>()
        }).ToList();
    }

    private List<WorkOrderProgress> InitializeWorkOrderProgress(List<WorkOrderRequest5> workOrders)
    {
        return workOrders.Select(wo => new WorkOrderProgress
        {
            PurchaseOrder = wo.PurchaseOrder,
            Company = wo.Company,
            Priority = wo.Priority,
            OrderDate = wo.OrderDate2 ?? DateTime.Now,
            ExpectedDeliveryDate =
                wo.ExpectedDeliveryDate2?.ToString() ??
                String.Empty, // (wo.ExpectedDeliveryDate2.HasValue ? wo.ExpectedDeliveryDate2.ToString() : string.Empty) ?? string.Empty,
            ItemProgress = wo.Items.Select(item => new ItemProgress
            {
                LotName = item.LotName,
                OriginalQuantity = item.Quantity,
                ProcessedQuantity = 0,
                PourWidth = item.PourWidth,
                PourLength = item.PourLength,
                FinishedLength = item.FinishedLength,
                FinishedWidth = item.FinishedWidth,
                Color = item.Color,
                Type = item.Type
            }).ToList()
        }).ToList();
    }

    private List<WorkOrderRequest5> SortWorkOrdersByPriority(List<WorkOrderRequest5> workOrders)
    {
        var query = workOrders.AsQueryable();
        DateTime date1;
        DateTime date2;
        DateTime date3;
        DateTime date4;
        foreach (var criteria in _config.PriorityCriteria.OrderBy(c => c.Order))
        {
            switch (criteria.Field.ToLower())
            {
                case "priority":
                    query = criteria.Ascending
                        ? query.OrderBy(w => w.Priority)
                        : query.OrderByDescending(w => w.Priority);
                    break;
                case "orderdate":
                    query = criteria.Ascending
                        ? query.OrderBy(w => w.OrderDate)
                        : query.OrderByDescending(w =>
                            w.OrderDate);
                    break;
                case "expecteddeliverydate":
                    query = criteria.Ascending
                        ? query.OrderBy(w =>
                            DateTime.TryParse(w.ExpectedDeliveryDate, out date3) ? date3 : DateTime.MaxValue)
                        : query.OrderByDescending(w =>
                            DateTime.TryParse(w.ExpectedDeliveryDate, out date4) ? date4 : DateTime.MinValue);
                    break;
            }
        }

        return query.ToList();
    }

    private bool ShouldContinueProcessing(int daysGenerated, List<WorkOrderProgress> workOrderProgress)
    {
        // Continue if within max days
        if (daysGenerated < _config.MaxWorkDaysToGenerate)
            return true;

        // Continue if extension is allowed and there are unprocessed orders
        if (_config.AllowExtensionBeyondMaxDays && workOrderProgress.Any(w => !w.IsFullyProcessed))
            return true;

        return false;
    }

    private DateTime GetNextWorkDay(DateTime currentDate, List<string> workDays)
    {
        while (!workDays.Contains(currentDate.DayOfWeek.ToString()))
        {
            currentDate = currentDate.AddDays(1);
        }

        return currentDate;
    }

    private DailyPourPlan GenerateForDailyPourPlan(List<WorkOrderProgress> workOrderProgress, DateTime date,
        string color)
    {
        var dailyPlan = new DailyPourPlan
        {
            Date = date.ToString("yyyy-MM-dd"),
            DayName = date.DayOfWeek.ToString(),
            Color = color
        };

        // Create fresh mold instances for this day
        var availableMolds = CreateFreshMoldInstances();

        // Process work orders for this day and track which work orders were processed
        var processedWorkOrders = ProcessWorkOrdersForDay(workOrderProgress, availableMolds, dailyPlan.Date);

        dailyPlan.AllMolds = availableMolds;
        dailyPlan.ProcessedWorkOrders = processedWorkOrders;

        // Group by pour categories
        foreach (var moldGroup in availableMolds.GroupBy(m => m.PourCategory))
        {
            dailyPlan.PourGroups[moldGroup.Key] = moldGroup.ToList();
        }

        return dailyPlan;
    }

    public MultiDayPourPlan GenerateMultiDayPourPlan(List<WorkOrderRequest5> workOrders, DateTime startDate,
        string color)
    {
        var multiDayPlan = new MultiDayPourPlan();

        // Initialize work order progress tracking
        var workOrderProgress = InitializeWorkOrderProgress(workOrders);

        // Sort work orders based on priority criteriade
        var sortedWorkOrders = SortWorkOrdersByPriority(workOrders);

        var currentDate = GetNextWorkDay(startDate, _config.WorkDays);
        int daysGenerated = 0;
        var processedDates = new List<string>();

        // Continue until max days reached or all orders processed (if extension allowed)
        while (ShouldContinueProcessing(daysGenerated, workOrderProgress))
        {
            var dailyPlan = GenerateForDailyPourPlan(workOrderProgress, currentDate, color);

            if (dailyPlan.HasItems || daysGenerated < _config.MaxWorkDaysToGenerate)
            {
                multiDayPlan.DailyPlans.Add(dailyPlan);
                processedDates.Add(dailyPlan.Date);
                daysGenerated++;
            }

            currentDate = GetNextWorkDay(currentDate.AddDays(1), _config.WorkDays);

            // Break if all orders are processed
            if (workOrderProgress.All(w => w.IsFullyProcessed))
                break;

            // Safety check to prevent infinite loops
            if (daysGenerated > 30) // Maximum 30 days
                break;
        }

        // Categorize work orders by processing status
        CategorizeWorkOrders(multiDayPlan, workOrderProgress);

        // Generate summary
        //GeneratePlanSummary(multiDayPlan, workOrders, processedDates);

        return multiDayPlan;
    }

    public PourPlan GenerateDailyPourPlan(List<WorkOrderRequest5> workOrders, string date, string color)
    {
        var sortedWorkOrders = workOrders
            .OrderBy(w => w.Priority)
            .ThenBy(w => w.OrderDate)
            .ToList();

        var pourPlan = new PourPlan
        {
            Date = date,
            Color = color
        };

        // Create fresh mold instances
        var availableMolds = _standardMolds.Select(m => new StandardMold
        {
            Name = m.Name,
            Width = m.Width,
            Length = m.Length,
            PourCategory = m.PourCategory
        }).ToList();

        // Process work orders with improved slotting
        foreach (var workOrder in sortedWorkOrders)
        {
            ProcessWorkOrder(workOrder, availableMolds);
        }

        pourPlan.AllMolds = availableMolds;

        // Group by pour categories
        foreach (var moldGroup in availableMolds /*.Where(m => m.HasItems)*/.GroupBy(m => m.PourCategory))
        {
            pourPlan.PourGroups[moldGroup.Key] = moldGroup.ToList();
        }

        var usedMolds = availableMolds.Where(x => x.HasItems).ToList();
        // Generate calculation table
        pourPlan.CalculationTable = GenerateCalculationTable(usedMolds);
        return pourPlan;
    }

    private List<WorkOrderProgress> ProcessWorkOrdersForDay(List<WorkOrderProgress> workOrderProgress,
        List<StandardMold> availableMolds, string currentDate)
    {
        var processedWorkOrders = new List<WorkOrderProgress>();

        foreach (var workOrder in workOrderProgress.Where(wo => !wo.IsFullyProcessed))
        {
            bool workOrderProcessedToday = false;

            foreach (var itemProgress in workOrder.ItemProgress.Where(ip => ip.RemainingQuantity > 0))
            {
                int itemsToProcess = itemProgress.RemainingQuantity;
                int processedCount = 0;

                for (int i = 0; i < itemsToProcess; i++)
                {
                    var slottedItem = new SlottedItem
                    {
                        Width = itemProgress.PourWidth,
                        Length = itemProgress.PourLength,
                        SourceOrder = workOrder.PurchaseOrder,
                        LotName = itemProgress.LotName
                    };

                    if (SlotItemIntoMoldNew(slottedItem, availableMolds))
                    {
                        processedCount++;
                        workOrderProcessedToday = true;
                    }
                    else
                    {
                        // No more space available for this day
                        break;
                    }
                }

                if (processedCount > 0)
                {
                    itemProgress.ProcessedQuantity += processedCount;

                    // Track daily processing
                    if (!itemProgress.ProcessedOnDays.Contains(currentDate))
                    {
                        itemProgress.ProcessedOnDays.Add(currentDate);
                    }

                    if (itemProgress.DailyProcessedQuantity.ContainsKey(currentDate))
                    {
                        itemProgress.DailyProcessedQuantity[currentDate] += processedCount;
                    }
                    else
                    {
                        itemProgress.DailyProcessedQuantity[currentDate] = processedCount;
                    }
                }
            }

            if (workOrderProcessedToday && !processedWorkOrders.Contains(workOrder))
            {
                processedWorkOrders.Add(workOrder);
            }
        }

        return processedWorkOrders;
    }

    #region duplicate methods

    private bool CanFitInMold2(SlottedItem item, StandardMold mold)
    {
        double availableWidth = mold.Width - _config.MarginSize;
        double availableLength = mold.Length - _config.MarginSize;

        if (item.Width > availableWidth || item.Length > availableLength)
            return false;

        return CanFitOnTopSide2(item, mold) || CanFitOnBottomSide2(item, mold);
    }

    private bool CanFitOnTopSide2(SlottedItem item, StandardMold mold)
    {
        if (!mold.TopSideItems.Any())
            return true;

        // Check if we can add to existing width groups
        var widthGroups = mold.TopSideItems.GroupBy(i => Math.Round(i.Width, 1)).ToList();

        foreach (var group in widthGroups)
        {
            if (Math.Abs(group.Key - item.Width) < 0.1) // Same width
            {
                double usedLength = group.Sum(i => i.Length);
                if (usedLength + item.Length <= (mold.Length - _config.MarginSize))
                    return true;
            }
        }

        // Check if we can start a new width group
        double totalUsedWidth = widthGroups.Sum(g => g.Key);
        return (totalUsedWidth + item.Width <= (mold.Width - _config.MarginSize));
    }

    private bool CanFitOnBottomSide2(SlottedItem item, StandardMold mold)
    {
        double maxSingleSideWidth = (mold.Width - _config.MarginSize) / 2;

        if (item.Width > maxSingleSideWidth)
            return false;

        // Check if top side allows bottom side usage
        if (mold.TopSideItems.Any())
        {
            double topSideWidth = mold.TopSideItems.GroupBy(i => Math.Round(i.Width, 1)).Sum(g => g.Key);
            if (topSideWidth > maxSingleSideWidth)
                return false;
        }

        if (!mold.BottomSideItems.Any())
            return true;

        // Check existing bottom side groups
        var widthGroups = mold.BottomSideItems.GroupBy(i => Math.Round(i.Width, 1)).ToList();

        foreach (var group in widthGroups)
        {
            if (Math.Abs(group.Key - item.Width) < 0.1)
            {
                double usedLength = group.Sum(i => i.Length);
                if (usedLength + item.Length <= (mold.Length - _config.MarginSize))
                    return true;
            }
        }

        double totalUsedWidth = widthGroups.Sum(g => g.Key);
        return (totalUsedWidth + item.Width <= maxSingleSideWidth);
    }

    private void PlaceOnTopSide2(SlottedItem item, StandardMold mold)
    {
        if (!mold.TopSideItems.Any())
        {
            item.XPosition = 0;
            item.YPosition = 0;
        }
        else
        {
            var widthGroups = mold.TopSideItems.GroupBy(i => Math.Round(i.Width, 1)).ToList();
            var matchingGroup = widthGroups.FirstOrDefault(g => Math.Abs(g.Key - item.Width) < 0.1);

            if (matchingGroup != null)
            {
                // Add to existing width group
                item.XPosition = matchingGroup.Sum(i => i.Length);
                item.YPosition = matchingGroup.First().YPosition;
            }
            else
            {
                // Start new width group
                item.XPosition = 0;
                item.YPosition = widthGroups.Sum(g => g.Key);
            }
        }

        mold.TopSideItems.Add(item);
    }

    private void PlaceOnBottomSide2(SlottedItem item, StandardMold mold)
    {
        if (!mold.BottomSideItems.Any())
        {
            item.XPosition = 0;
            item.YPosition = mold.Width - item.Width;
        }
        else
        {
            var widthGroups = mold.BottomSideItems.GroupBy(i => Math.Round(i.Width, 1)).ToList();
            var matchingGroup = widthGroups.FirstOrDefault(g => Math.Abs(g.Key - item.Width) < 0.1);

            if (matchingGroup != null)
            {
                item.XPosition = matchingGroup.Sum(i => i.Length);
                item.YPosition = matchingGroup.First().YPosition;
            }
            else
            {
                item.XPosition = 0;
                item.YPosition = mold.Width - item.Width;
            }
        }

        mold.BottomSideItems.Add(item);
    }

    #endregion

    private void ProcessWorkOrder(WorkOrderRequest5 workOrder, List<StandardMold> availableMolds)
    {
        foreach (var item in workOrder.Items)
        {
            for (int i = 0; i < item.Quantity; i++)
            {
                var slottedItem = new SlottedItem
                {
                    Width = item.PourWidth,
                    Length = item.PourLength,
                    SourceOrder = workOrder.PurchaseOrder,
                    LotName = item.LotName
                };

                SlotItemIntoMold(slottedItem, availableMolds);
            }
        }
    }


    private void SlotItemIntoMold(SlottedItem item, List<StandardMold> availableMolds)
    {
        foreach (var mold in availableMolds)
        {
            if (CanFitInMold(item, mold))
            {
                PlaceItemInMold(item, mold);
                return;
            }
        }
    }

    private bool SlotItemIntoMoldNew(SlottedItem item, List<StandardMold> availableMolds)
    {
        foreach (var mold in availableMolds)
        {
            if (CanFitInMold(item, mold))
            {
                PlaceItemInMold(item, mold);
                return true;
            }
        }

        return false;
    }

    private bool CanFitInMold(SlottedItem item, StandardMold mold)
    {
        // Check if item width perfectly matches mold width (allow perfect fit for single-side molds)
        bool isPerfectWidthMatch = Math.Abs(item.Width - mold.Width) < 0.1;

        // For perfect width match, only check length constraint
        if (isPerfectWidthMatch)
        {
            // Check if mold can accommodate this item on top side only (no bottom side for perfect match)
            if (!mold.TopSideItems.Any())
            {
                // First item with perfect width - allow if length fits
                return item.Length <= (mold.Length - MARGIN_SIZE);
            }
            else
            {
                // Check if all existing items have same width (perfect match scenario)
                var allSameWidth = mold.TopSideItems.All(i => Math.Abs(i.Width - item.Width) < 0.1);
                if (allSameWidth)
                {
                    double usedLength = mold.TopSideItems.Sum(i => i.Length);
                    return (usedLength + item.Length) <= (mold.Length - MARGIN_SIZE);
                }

                // If mixed widths already exist, can't add perfect width item
                return false;
            }
        }

        // Check basic size constraints with margins for non-perfect fits
        double availableWidth = mold.Width - MARGIN_SIZE;
        double availableLength = mold.Length - MARGIN_SIZE;

        // Item must fit within container dimensions
        if (item.Width > availableWidth || item.Length > availableLength)
            return false;

        return CanFitOnTopSide(item, mold) || CanFitOnBottomSide(item, mold);
    }

    private bool CanFitOnTopSide(SlottedItem item, StandardMold mold)
    {
        if (!mold.TopSideItems.Any())
            return true;

        // Check if this is a perfect width match scenario
        bool isPerfectWidthMatch = Math.Abs(item.Width - mold.Width) < 0.1;
        if (isPerfectWidthMatch)
        {
            // Already handled in CanFitInMold
            return false;
        }

        // Group existing items by width
        var widthGroups = mold.TopSideItems.GroupBy(i => Math.Round(i.Width, 1)).ToList();

        // Check if can fit in existing width group
        foreach (var group in widthGroups)
        {
            if (Math.Abs(group.Key - item.Width) < 0.1)
            {
                double usedLength = group.Sum(i => i.Length);
                if (usedLength + item.Length <= (mold.Length - MARGIN_SIZE))
                    return true;
            }
        }

        // Check if can create new width row
        double usedWidth = widthGroups.Sum(g => g.Key);
        if (usedWidth != 0) return false;
        return (usedWidth + item.Width <= (mold.Width - MARGIN_SIZE));
    }


    private bool CanFitOnBottomSide(SlottedItem item, StandardMold mold)
    {
        double maxSingleSideWidth = (mold.Width - (2 * MARGIN_SIZE)) / 2;

        if (item.Width > maxSingleSideWidth)
            return false;

        if (mold.TopSideItems.Any())
        {
            double topSideWidth = mold.TopSideItems.GroupBy(i => Math.Round(i.Width, 1)).Sum(g => g.Key);
            if (topSideWidth > maxSingleSideWidth)
                return false;
        }

        if (!mold.BottomSideItems.Any())
            return true;

        var widthGroups = mold.BottomSideItems.GroupBy(i => Math.Round(i.Width, 1)).ToList();

        // Check existing width groups
        foreach (var group in widthGroups)
        {
            if (Math.Abs(group.Key - item.Width) < 0.1)
            {
                double usedLength = group.Sum(i => i.Length);
                if (usedLength + item.Length <= (mold.Length - MARGIN_SIZE))
                    return true;
            }
        }

        double usedWidth = widthGroups.Sum(g => g.Key);
        if (usedWidth != 0) return false;
        return (usedWidth + item.Width <= maxSingleSideWidth);
    }

    private void PlaceItemInMold(SlottedItem item, StandardMold mold)
    {
        if (CanFitOnTopSide(item, mold))
        {
            PlaceOnTopSide(item, mold);
        }
        else if (CanFitOnBottomSide(item, mold))
        {
            PlaceOnBottomSide(item, mold);
        }
    }

    private void PlaceOnTopSide(SlottedItem item, StandardMold mold)
    {
        if (!mold.TopSideItems.Any())
        {
            // First item - attach to top-left corner
            item.XPosition = 0;
            item.YPosition = 0;
        }
        else
        {
            // Find matching width group
            var widthGroups = mold.TopSideItems.GroupBy(i => Math.Round(i.Width, 1)).ToList();
            var matchingGroup = widthGroups.FirstOrDefault(g => Math.Abs(g.Key - item.Width) < 0.1);

            if (matchingGroup != null)
            {
                // Place next to existing items of same width
                item.XPosition = matchingGroup.Sum(i => i.Length);
                item.YPosition = matchingGroup.First().YPosition;
            }
            else
            {
                // New width group - place in new row
                item.XPosition = 0;
                item.YPosition = widthGroups.Sum(g => g.Key);
            }
        }

        mold.TopSideItems.Add(item);
    }

    private void PlaceOnBottomSide(SlottedItem item, StandardMold mold)
    {
        var topSidesWidth = mold.TopSideItems.Select(x => x.Width).Distinct().Sum();

        if (!mold.BottomSideItems.Any())
        {
            // First bottom item - attach to bottom-left corner
            item.XPosition = 0;
            //item.YPosition = 0;
            // item.YPosition = topSidesWidth + MARGIN_SIZE;
            item.YPosition = mold.Width - item.Width;
        }
        else
        {
            var widthGroups = mold.BottomSideItems.GroupBy(i => Math.Round(i.Width, 1)).ToList();
            var matchingGroup = widthGroups.FirstOrDefault(g => Math.Abs(g.Key - item.Width) < 0.1);

            if (matchingGroup != null)
            {
                item.XPosition = matchingGroup.Sum(i => i.Length);
                item.YPosition = matchingGroup.First().YPosition;
            }
            else
            {
                item.XPosition = 0;
                // item.YPosition = topSidesWidth + MARGIN_SIZE;
                item.YPosition = mold.Width - item.Width;
                //item.YPosition = widthGroups.Sum(g => g.Key);
            }
        }

        mold.BottomSideItems.Add(item);
    }

    //gen
    private List<TableRow> GenerateCalculationTable(List<StandardMold> usedMolds)
    {
        var tableRows = new List<TableRow>();

        foreach (var mold in usedMolds)
        {
            if (mold.TopSideItems.Any())
            {
                var totalPcs = mold.TopSideItems.Count;
                var totalArea = mold.TopSideItems.Sum(i => i.Width * i.Length);

                tableRows.Add(new TableRow
                {
                    MoldSize = $"{mold.Name} ({mold.Width}\" X {mold.Length}\")",
                    Poured = 1,
                    PourDate = mold.TopSideItems.First().Length + " X " + mold.TopSideItems.First().Width,
                    TotalPcs = totalPcs,
                    CubicYards = (float)totalArea / 144, // Convert to sq ft
                    TotalArea = (float)totalArea
                });
            }

            if (mold.BottomSideItems.Any())
            {
                var totalPcs = mold.BottomSideItems.Count;
                var totalArea = mold.BottomSideItems.Sum(i => i.Width * i.Length);

                tableRows.Add(new TableRow
                {
                    MoldSize = $"{mold.Name} ({mold.Width}\" X {mold.Length}\")",
                    Poured = 1,
                    PourDate = mold.BottomSideItems.First().Length + " X " + mold.BottomSideItems.First().Width,
                    TotalPcs = totalPcs,
                    CubicYards = (float)totalArea / 144, // Convert to sq ft
                    TotalArea = (float)totalArea
                });
            }
        }

        return tableRows;
    }
}

public class DayToDayPourPlanGenerator
{
    // Colors and constants
    private readonly BaseColor TITLE_BLUE = new BaseColor(70, 130, 180);
    private readonly BaseColor YELLOW_LINE = new BaseColor(255, 255, 0);
    private readonly BaseColor RED_LINE = new BaseColor(255, 0, 0);
    private readonly BaseColor BLACK_LINE = BaseColor.BLACK;
    private readonly BaseColor LIGHT_BLUE = new BaseColor(173, 216, 230);
    private readonly BaseColor GRAY_FILL = new BaseColor(240, 240, 240);
    private readonly BaseColor GREEN_FILL = new BaseColor(144, 238, 144);
    private readonly BaseColor WHITE_FILL = BaseColor.WHITE;
    private readonly BaseColor POUR_CATEGORY_BLUE = new BaseColor(100, 149, 237);

    private const float MARGIN_SIZE = 3;
    private const float MARGIN = 50f;
    private const float PAGE_WIDTH = 612f;
    private const float PAGE_HEIGHT = 792f;
    private const float SCALE_FACTOR = 1.5f; // Scale for display 

    private readonly BaseFont _bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);

    private readonly BaseFont _bfBold =
        BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);

    // // Reusable base font
    // private readonly BaseFont _bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);

    public byte[] GenerateMultiDayPourSheet(MultiDayPourPlan multiDayPlan, string pourNumber = "1")
    {
        using (var memoryStream = new MemoryStream())
        {
            var document = new Document(PageSize.LETTER, MARGIN, MARGIN, MARGIN, MARGIN);
            try
            {
                PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
                document.Open();
                PdfContentByte cb = writer.DirectContent;

                bool isFirstPage = true;

                foreach (var dailyPlan in multiDayPlan.DailyPlans.Where(dp => dp.HasItems))
                {
                    if (!isFirstPage) document.NewPage();
                    isFirstPage = false;

                    // Daily overview page
                    string dayName = GetDayName(dailyPlan.Date);
                    DrawHeader(cb, pourNumber, dailyPlan.Date, dailyPlan.Color, $"{dayName.ToUpper()} POURING PLAN",
                        true);
                    DrawDailyOverview(document, cb, dailyPlan.AllMolds);

                    // Pour group detail pages with category-specific tables
                    foreach (var pourGroup in dailyPlan.PourGroups.OrderBy(g => g.Key))
                    {
                        document.NewPage();
                        DrawHeader(cb, pourGroup.Key.Replace("P", "POUR "), dailyPlan.Date, dailyPlan.Color,
                            pourGroup.Key.Replace("P", "POUR "));
                        DrawPourGroupDiagrams(document, cb, pourGroup.Value);

                        // Generate table ONLY for molds in this specific pour category
                        var categoryMolds = dailyPlan.AllMolds
                            .Where(m => m.PourCategory == pourGroup.Key)
                            .ToList();
                        var categoryTable = GeneratePourCategoryCalculationTable(categoryMolds);
                        DrawCalculationTable(cb, categoryTable, pourGroup.Key);
                    }
                }

                document.Close();
                writer.Close();
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating PDF: {ex.Message}", ex);
            }
        }
    }

    private List<TableRow> GeneratePourCategoryCalculationTable(List<StandardMold> moldsInCategory)
    {
        var tableRows = new List<TableRow>();

        foreach (var mold in moldsInCategory)
        {
            // Process top side if it has items
            if (mold.TopSideItems.Any())
            {
                var topSideItems = mold.TopSideItems;

                // Correct calculation: sum of all item lengths + margin
                double sumOfLengths = topSideItems.Sum(i => i.Length);
                double totalLengthWithMargin = sumOfLengths + MARGIN_SIZE;
                float totalArea = (float)topSideItems.Sum(i => i.Width * i.Length);

                var firstItem = topSideItems.First();
                string pouredSize = $"{firstItem.Width:F1}\" X {firstItem.Length:F1}\"";

                tableRows.Add(new TableRow
                {
                    MoldSize = $"{mold.Width}\" X {mold.Length}\"",
                    Poured = 1,
                    PouredSize = pouredSize,
                    NumberOfPcs = topSideItems.Count,
                    PouredVolume = totalArea,
                    PouringPcs = topSideItems.Count,
                    Poureable = totalArea,
                    ExtraMargin = (int)MARGIN_SIZE,
                    TotalLengthRequired = (float)totalLengthWithMargin,
                    TotalAreaRequired = totalArea,
                    Note = $"SIDE1"
                });
            }

            // Process bottom side if it has items
            if (mold.BottomSideItems.Any())
            {
                var bottomSideItems = mold.BottomSideItems;

                // Correct calculation: sum of all item lengths + margin
                double sumOfLengths = bottomSideItems.Sum(i => i.Length);
                double totalLengthWithMargin = sumOfLengths + MARGIN_SIZE;
                float totalArea = (float)bottomSideItems.Sum(i => i.Width * i.Length);

                var firstItem = bottomSideItems.First();
                string pouredSize = $"{firstItem.Width:F1}\" X {firstItem.Length:F1}\"";

                tableRows.Add(new TableRow
                {
                    MoldSize = $"{mold.Width}\" X {mold.Length}\"",
                    Poured = 1,
                    PouredSize = pouredSize,
                    NumberOfPcs = bottomSideItems.Count,
                    PouredVolume = totalArea,
                    PouringPcs = bottomSideItems.Count,
                    Poureable = totalArea,
                    ExtraMargin = (int)MARGIN_SIZE,
                    TotalLengthRequired = (float)totalLengthWithMargin,
                    TotalAreaRequired = totalArea,
                    Note = $"SIDE2"
                });
            }

            // Add row for UNUSED molds in this pour category
            if (!mold.HasItems)
            {
                tableRows.Add(new TableRow
                {
                    MoldSize = $"{mold.Width}\" X {mold.Length}\"",
                    Poured = 0,
                    PouredSize = "X",
                    NumberOfPcs = 0,
                    PouredVolume = 0,
                    PouringPcs = 0,
                    Poureable = 0,
                    ExtraMargin = (int)MARGIN_SIZE,
                    TotalLengthRequired = 0,
                    TotalAreaRequired = 0,
                    Note = ""
                });
            }
        }

        return tableRows;
    }

    public byte[] GeneratePourSheet(PourPlan pourPlan, string pourNumber)
    {
        using (var memoryStream = new MemoryStream())
        {
            var document = new Document(PageSize.LETTER, MARGIN, MARGIN, MARGIN, MARGIN);
            try
            {
                PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
                document.Open();
                PdfContentByte cb = writer.DirectContent;

                // First page: Daily overview
                string dayName = GetDayName(pourPlan.Date);
                DrawHeader(cb, pourNumber, pourPlan.Date, pourPlan.Color, $"{dayName.ToUpper()} POURING PLAN",
                    true);
                DrawDailyOverview(document, cb, pourPlan.AllMolds);

                // Subsequent pages: pour group diagrams
                foreach (var pourGroup in pourPlan.PourGroups.OrderBy(g => g.Key))
                {
                    document.NewPage();
                    DrawHeader(cb, pourGroup.Key.Replace("P", "POUR "), pourPlan.Date, pourPlan.Color,
                        pourGroup.Key.Replace("P", "POUR "));
                    DrawPourGroupDiagrams(document, cb, pourGroup.Value);
                    DrawCalculationTable(cb, pourPlan.CalculationTable, pourGroup.Key);
                    DrawFooter(cb);
                }

                document.Close();
                writer.Close();
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating PDF: {ex.Message}", ex);
            }
        }
    }

    private void DrawEnhancedPourGroupDiagrams(Document doc, PdfContentByte cb, List<StandardMold> molds)
    {
        float currentY = PAGE_HEIGHT - 130;
        float minY = 200;

        foreach (var mold in molds.Where(m => m.HasItems))
        {
            if (currentY < minY)
            {
                doc.NewPage();
                currentY = PAGE_HEIGHT - 130;
            }

            float height = DrawDetailedMoldDiagram(cb, MARGIN, currentY, mold);
            currentY -= (height + 40);
        }
    }

    private float DrawDetailedMoldDiagram(PdfContentByte cb, float x, float y, StandardMold mold)
    {
        // Draw detailed mold with enhanced styling similar to your image
        string moldTitle = $"MOLD NAME - {mold.Name} ({mold.Width}\" X {mold.Length}\")";
        DrawEnhancedMoldTitleBox(cb, moldTitle, x, y + 25);

        // Pour category circle
        DrawPourCategoryCircle(cb, mold.PourCategory, x - 25, y, POUR_CATEGORY_BLUE);

        // Calculate dimensions
        float moldWidth = (float)(mold.Length * SCALE_FACTOR);
        float moldHeight = (float)(mold.Width * SCALE_FACTOR);

        // Draw the mold container
        DrawEnhancedMoldContainer(cb, x, y - moldHeight - 15, moldWidth, moldHeight, false, mold);

        // Draw items with detailed information
        if (mold.HasItems)
        {
            DrawDetailedMoldItems(cb, x, y - 15, mold, moldWidth, moldHeight);
        }

        return moldHeight + 70;
    }

    private void DrawEnhancedMoldTitleBox(PdfContentByte cb, string text, float x, float y)
    {
        float textWidth = _bfBold.GetWidthPoint(text, 10);
        float boxWidth = textWidth + 12;
        float boxHeight = 22;

        // Red border box matching your image style
        cb.SaveState();
        cb.SetColorStroke(RED_LINE);
        cb.SetColorFill(WHITE_FILL);
        cb.SetLineWidth(2f);
        cb.Rectangle(x, y, boxWidth, boxHeight);
        cb.FillStroke();
        cb.RestoreState();

        // Text
        cb.BeginText();
        cb.SetFontAndSize(_bfBold, 10);
        cb.SetColorFill(RED_LINE);
        cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, text, x + 6, y + 6, 0);
        cb.EndText();
    }

    private void DrawDetailedMoldItems(PdfContentByte cb, float containerX, float containerY,
        StandardMold mold, float containerWidth, float containerHeight)
    {
        // Draw items similar to your reference image
        foreach (var item in mold.AllItems)
        {
            float itemX = containerX + (float)(item.XPosition * SCALE_FACTOR);
            float itemY = containerY - (float)(item.YPosition * SCALE_FACTOR) - (float)(item.Width * SCALE_FACTOR);
            float itemWidth = (float)(item.Length * SCALE_FACTOR);
            float itemHeight = (float)(item.Width * SCALE_FACTOR);

            // Draw item with black border
            cb.SaveState();
            cb.SetColorStroke(BLACK_LINE);
            cb.SetColorFill(WHITE_FILL);
            cb.SetLineWidth(1.5f);
            cb.Rectangle(itemX, itemY, itemWidth, itemHeight);
            cb.FillStroke();
            cb.RestoreState();

            // Item dimensions label
            string label = $"{item.Width}\" x {item.Length}\"";
            cb.BeginText();
            cb.SetFontAndSize(_bf, 8);
            cb.SetColorFill(BLACK_LINE);
            cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, label, itemX + itemWidth / 2,
                itemY + itemHeight / 2 - 2, 0);
            cb.EndText();
        }

        // Draw side totals with arrows exactly like your image
        DrawDetailedSideTotals(cb, containerX, containerY, containerWidth, mold);
    }

    private void DrawDetailedSideTotals(PdfContentByte cb, float containerX, float containerY,
        float containerWidth, StandardMold mold)
    {
        float moldRightX = containerX + containerWidth;
        float labelX = moldRightX + 20;

        int sideCounter = 1;

        // Process each side with items
        if (mold.TopSideItems.Any())
        {
            double sideTotal = mold.TopSideItems.Max(item => item.XPosition + item.Length) + 3;
            float sideY = containerY - 25;

            DrawSideTotalLabel(cb, labelX, sideY, sideCounter, sideTotal);
            DrawDetailedArrow(cb, moldRightX, sideY, RED_LINE);
            sideCounter++;
        }

        if (mold.BottomSideItems.Any())
        {
            double sideTotal = mold.BottomSideItems.Max(item => item.XPosition + item.Length) + 3;
            float sideY = containerY - (float)(mold.Width * SCALE_FACTOR) - 40;

            DrawSideTotalLabel(cb, labelX, sideY, sideCounter, sideTotal);
            DrawDetailedArrow(cb, moldRightX, sideY, RED_LINE);
        }
    }

    private void DrawEnhancedMoldContainer(PdfContentByte cb, float x, float y, float width, float height,
        bool showDimensions, StandardMold mold)
    {
        // Draw main mold rectangle with professional styling
        cb.SaveState();
        //cb.SetColorStroke(MOLD_STROKE);
        cb.SetColorStroke(YELLOW_LINE);
        cb.SetColorFill(WHITE_FILL);
        cb.SetLineWidth(1f);
        cb.Rectangle(x, y, width, height);
        cb.FillStroke();
        cb.RestoreState();

        if (showDimensions)
        {
            DrawMoldDimensions(cb, x, y, width, height, mold.Length, mold.Width);
        }
    }

    private void DrawSideTotalLabel(PdfContentByte cb, float x, float y, int sideNumber, double total)
    {
        // Draw the label exactly like in your image
        cb.BeginText();
        cb.SetFontAndSize(_bf, 7);
        cb.SetColorFill(BLACK_LINE);
        cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, $"SIDE {sideNumber} TOTAL LENGTH", x, y + 4, 0);
        cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, "WITH MARGIN", x, y - 4, 0);
        cb.EndText();

        cb.BeginText();
        cb.SetFontAndSize(_bfBold, 9);
        cb.SetColorFill(RED_LINE);
        cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, $"= {total:F0}\"", x + 60, y - 4, 0);
        cb.EndText();
    }

    private void DrawDetailedArrow(PdfContentByte cb, float moldRightX, float arrowY, BaseColor color)
    {
        cb.SaveState();
        cb.SetColorStroke(color);
        cb.SetColorFill(color);
        cb.SetLineWidth(2f);

        // Horizontal line extending from mold
        float lineLength = 15f;
        cb.MoveTo(moldRightX, arrowY);
        cb.LineTo(moldRightX + lineLength, arrowY);
        cb.Stroke();

        // Arrow pointing towards mold
        float headSize = 4f;
        cb.MoveTo(moldRightX, arrowY);
        cb.LineTo(moldRightX + headSize, arrowY + headSize * 0.7f);
        cb.LineTo(moldRightX + headSize, arrowY - headSize * 0.7f);
        cb.ClosePathFillStroke();

        cb.RestoreState();
    }

    private void DrawHeader(PdfContentByte cb, string pourNumber, string date, string color, string title,
        bool forOverview = false)
    {
        if (!forOverview)
        {
            AddText(cb, "DATE: " + date, MARGIN, PAGE_HEIGHT - 40, 10, Font.BOLD);
            AddText(cb, "COLOR: " + color, MARGIN, PAGE_HEIGHT - 55, 10, Font.BOLD);
            //this comes after the title
            AddText(cb, "NOTE:", PAGE_WIDTH - MARGIN - 100, PAGE_HEIGHT - 40, 10, Font.BOLD);
        }

        // Center title with light-blue background (text only color here)
        AddText(cb, title, PAGE_WIDTH / 2, PAGE_HEIGHT - 30, 16, Font.BOLD, Element.ALIGN_CENTER, LIGHT_BLUE);
    }

    private void DrawDailyOverview(Document doc, PdfContentByte cb, List<StandardMold> allMolds)
    {
        float startY = PAGE_HEIGHT - 120;
        float leftColumnX = MARGIN;
        float rightColumnX = PAGE_WIDTH / 2 + 20;
        float minY = 120;

        // Split molds into left and right groups
        var leftMolds = allMolds.Where(m => "ABCDEF".Contains(m.Name)).OrderBy(m => m.Name).ToList();
        var rightMolds = allMolds.Where(m => "GHIJKL".Contains(m.Name)).OrderBy(m => m.Name).ToList();

        int leftIndex = 0, rightIndex = 0;
        float currentLeftY = startY;
        float currentRightY = startY;

        // Track group box boundaries for current page
        float leftBoxWidth = (PAGE_WIDTH / 2f) - MARGIN - 5;
        float rightBoxWidth = (PAGE_WIDTH - rightColumnX - MARGIN) - 5;

        // Track the topmost and bottommost positions for the group boxes
        float pageTopY = startY + 10;
        float pageBottomY = startY; // Will be updated as we draw

        void DrawGroupBoxesForPage(float finalBottomY)
        {
            // Draw a single group box that encompasses both columns to the same bottom
            DrawTransparentGroupBox(cb, leftColumnX - 8, pageTopY, finalBottomY, leftBoxWidth, LIGHT_BLUE, 0.06f,
                0.35f);
            DrawTransparentGroupBox(cb, rightColumnX - 8, pageTopY, finalBottomY, rightBoxWidth, LIGHT_BLUE, 0.06f,
                0.35f);
        }

        while (leftIndex < leftMolds.Count || rightIndex < rightMolds.Count)
        {
            float nextLeftY = currentLeftY;
            float nextRightY = currentRightY;
            bool drewLeftMold = false;
            bool drewRightMold = false;

            if (leftIndex < leftMolds.Count)
            {
                var mold = leftMolds[leftIndex];
                float h = DrawMoldOverview2(cb, leftColumnX, currentLeftY, mold, true);
                nextLeftY = currentLeftY - (h + 20);
                drewLeftMold = true;
            }

            if (rightIndex < rightMolds.Count)
            {
                var mold = rightMolds[rightIndex];
                float h = DrawMoldOverview2(cb, rightColumnX, currentRightY, mold, true);
                nextRightY = currentRightY - (h + 20);
                drewRightMold = true;
            }

            // Update the bottom boundary - use the lower of the two columns
            pageBottomY = Math.Min(nextLeftY, nextRightY) + 6;

            bool leftWouldOverflow = (nextLeftY < minY && leftIndex + 1 < leftMolds.Count);
            bool rightWouldOverflow = (nextRightY < minY && rightIndex + 1 < rightMolds.Count);

            if (leftWouldOverflow || rightWouldOverflow)
            {
                // Draw group boxes for current page before creating new page
                DrawGroupBoxesForPage(pageBottomY);

                doc.NewPage();
                currentLeftY = startY;
                currentRightY = startY;
                pageTopY = startY + 10;
                pageBottomY = startY;
            }
            else
            {
                if (drewLeftMold)
                {
                    currentLeftY = nextLeftY;
                    leftIndex++;
                }

                if (drewRightMold)
                {
                    currentRightY = nextRightY;
                    rightIndex++;
                }
            }
        }

        // Draw final group boxes for the last page
        DrawGroupBoxesForPage(pageBottomY);
    }

    private void DrawTextWithDoubleRectangle(PdfContentByte cb, string text, float centerX, float baselineY,
        float fontSize, int fontStyle, int alignment, BaseColor fillColor, BaseColor strokeColor,
        float outerPadding = 5f, float innerPadding = 3f)
    {
        float w = MeasureTextWidth(text, fontSize);
        float h = LineHeight(fontSize);

        // Calculate rectangle dimensions based on alignment
        float leftX = alignment == Element.ALIGN_CENTER ? centerX - w / 2 : centerX;

        // Inner rectangle
        float innerRectX = leftX - innerPadding;
        float innerRectY = baselineY - innerPadding;
        float innerRectW = w + innerPadding * 2f;
        float innerRectH = h + innerPadding * 1.5f;

        // Outer rectangle
        float outerRectX = leftX - outerPadding;
        float outerRectY = baselineY - outerPadding;
        float outerRectW = w + outerPadding * 2f;
        float outerRectH = h + outerPadding * 1.5f;

        cb.SaveState();

        // Draw outer rectangle
        if (fillColor != null) cb.SetColorFill(fillColor);
        cb.SetColorStroke(strokeColor ?? BLACK_LINE);
        cb.SetLineWidth(2f);
        cb.Rectangle(outerRectX, outerRectY, outerRectW, outerRectH);
        if (fillColor != null) cb.FillStroke();
        else cb.Stroke();

        // Draw inner rectangle
        cb.SetLineWidth(1f);
        cb.Rectangle(innerRectX, innerRectY, innerRectW, innerRectH);
        cb.Stroke();

        cb.RestoreState();

        // Draw text
        cb.BeginText();
        cb.SetFontAndSize(_bf, fontSize);
        cb.SetColorFill(BaseColor.BLACK);

        switch (alignment)
        {
            case Element.ALIGN_CENTER:
                cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, text, centerX, baselineY, 0);
                break;
            default:
                cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, text, leftX, baselineY, 0);
                break;
        }

        cb.EndText();
    }

    private float DrawMoldOverview2(PdfContentByte cb, float x, float y, StandardMold mold, bool showDimensions,
        bool showItemLengthText = false, bool drawPourCategoryInCircle = false)
    {
        // Mold title inside tight rectangle
        string moldTitle = $"MOLD NAME - {mold.Name} ({mold.Width}\" x {mold.Length}\")";
        DrawTextWithTightRect(cb, moldTitle, x, y + 20, 10, Font.BOLD, LIGHT_BLUE, null, 3f, BaseColor.RED);

        // Pour category in a small circle to the left
        string pourCat = mold.PourCategory ?? "P1";

        float catCenterX = x - 30;
        float catBaselineY = y - 20;
        if (drawPourCategoryInCircle) 
            DrawTextInCircle(cb, pourCat, catCenterX+8, catBaselineY, 10, Font.BOLD, BLACK_LINE, null, 2f);
        else
            DrawPourCategoryCircle(cb, pourCat, x - 25 +10, y, POUR_CATEGORY_BLUE, false); 

        float moldWidth = (float)(mold.Length * SCALE_FACTOR);
        float moldHeight = (float)(mold.Width * SCALE_FACTOR);

        // Yellow container representing mold
        DrawRectangle(cb, x + 8, y - moldHeight - 10, moldWidth, moldHeight, YELLOW_LINE, null);

        if (showDimensions)
        {
            //DrawItemDimensions(cb, x, y - moldHeight - 10, moldWidth, moldHeight, mold.Length, mold.Width);
            DrawMoldDimensions(cb, x + 8, y - moldHeight - 10, moldWidth, moldHeight, mold.Length, mold.Width);
        }

        if (mold.HasItems)
            DrawMoldItems(cb, x + 8, y - 10, mold, moldWidth, moldHeight, false, showItemLengthText);

        return moldHeight + 50;
    }

 
    private float DrawMoldOverview(PdfContentByte cb, float x, float y, StandardMold mold, bool showDimensions,
        bool showItemLengthText = false, bool drawPourCategoryInCircle = false)
    {
        // Mold title inside tight rectangle
        string moldTitle = $"MOLD NAME - {mold.Name} ({mold.Width}\" x {mold.Length}\")";
        DrawTextWithTightRect(cb, moldTitle, x, y + 20, 10, Font.BOLD, LIGHT_BLUE, null, 3f, BaseColor.RED);

        // Pour category to the left
        string pourCat = mold.PourCategory ?? "P1";
        float catCenterX = x - 30;
        float catBaselineY = y - 20;

        if (drawPourCategoryInCircle)
            DrawTextInCircle(cb, pourCat, catCenterX, catBaselineY, 10, Font.BOLD, BLACK_LINE, null, 2f);
        else
            DrawPourCategoryCircle(cb, pourCat, x - 25, y, POUR_CATEGORY_BLUE, false);


        float moldWidth = (float)(mold.Length * SCALE_FACTOR);
        float moldHeight = (float)(mold.Width * SCALE_FACTOR);

        // Center the mold diagram on the page better
        float availableWidth = PAGE_WIDTH - (2 * MARGIN) - 40; // Account for margins and pour category
        float centerOffset = (availableWidth - moldWidth) / 2;
        float centeredX = x + centerOffset;

        // Yellow container representing mold
        DrawRectangle(cb, centeredX, y - moldHeight - 10, moldWidth, moldHeight, YELLOW_LINE, null);

        if (showDimensions)
        {
            DrawMoldDimensions(cb, centeredX, y - moldHeight - 10, moldWidth, moldHeight, mold.Length, mold.Width);
        }

        if (mold.HasItems)
            DrawMoldItems(cb, centeredX, y - 10, mold, moldWidth, moldHeight, false, showItemLengthText);

        return moldHeight + 50;
    }

    private void DrawMoldItems(PdfContentByte cb, float containerX, float containerY, StandardMold mold,
        float containerWidth, float containerHeight, bool showDimensions, bool showItemLengthText)
    {
        // Top side items
        foreach (var item in mold.TopSideItems)
        {
            float itemX = containerX + (float)(item.XPosition * SCALE_FACTOR);
            float itemY = containerY - (float)(item.YPosition * SCALE_FACTOR) - (float)(item.Width * SCALE_FACTOR);
            float itemWidth = (float)(item.Length * SCALE_FACTOR);
            float itemHeight = (float)(item.Width * SCALE_FACTOR);

            DrawRectangle(cb, itemX, itemY, itemWidth, itemHeight, BLACK_LINE, null);

            string label = $"{item.Width}\" x {item.Length}\"";
            AddText(cb, label, itemX + itemWidth / 2, itemY + itemHeight / 2, 7, Font.NORMAL,
                Element.ALIGN_CENTER, BaseColor.BLACK);

            if (showDimensions)
                DrawItemDimensions(cb, itemX, itemY, itemWidth, itemHeight, item.Length, item.Width);
        }

        // Bottom side items
        foreach (var item in mold.BottomSideItems)
        {
            float itemX = containerX + (float)(item.XPosition * SCALE_FACTOR);
            float itemY = containerY - (float)(item.YPosition * SCALE_FACTOR) - (float)(item.Width * SCALE_FACTOR);
            float itemWidth = (float)(item.Length * SCALE_FACTOR);
            float itemHeight = (float)(item.Width * SCALE_FACTOR);

            DrawRectangle(cb, itemX, itemY, itemWidth, itemHeight, BLACK_LINE, null);

            string label = $"{item.Width}\" x {item.Length}\"";
            AddText(cb, label, itemX + itemWidth / 2, itemY + itemHeight / 2, 7, Font.NORMAL,
                Element.ALIGN_CENTER, BaseColor.BLACK);

            if (showDimensions)
                DrawItemDimensions(cb, itemX, itemY, itemWidth, itemHeight, item.Length, item.Width);
        }

        // Totals with margin and left-pointing arrows to show which side
        if (mold.HasItems && showItemLengthText)
        {
            float moldRightX = containerX + containerWidth;
            float labelX = moldRightX + 10; // where labels start

            // Top side total
            if (mold.TopSideItems.Any())
            {
                DrawSideTotalsWithArrows(cb, containerX, containerY, containerWidth, mold);
            }

            // Bottom side total
            if (mold.BottomSideItems.Any())
            {
                // double bottomTotal = mold.BottomSideItems.Max(i => i.XPosition + i.Length) + 3;
                var item0 = mold.BottomSideItems.First();
                float bottomsY = containerY - (float)(item0.YPosition * SCALE_FACTOR) - 16;

                DrawSideTotalsWithArrows(cb, containerX, containerY, containerWidth, mold);
            }
        }
    }

    private void DrawItemDimensions(PdfContentByte cb, float itemX, float itemY, float itemWidth, float itemHeight,
        double itemActualLength, double itemActualWidth)
    {
        cb.SetColorStroke(RED_LINE);
        cb.SetLineWidth(1f);

        // Length dimension (horizontal, above item)
        float lengthY = itemY + itemHeight + 15;

        // Main horizontal line
        cb.MoveTo(itemX, lengthY);
        cb.LineTo(itemX + itemWidth, lengthY);
        cb.Stroke();

        // Vertical connecting lines at edges
        cb.MoveTo(itemX, itemY + itemHeight);
        cb.LineTo(itemX, lengthY + 3);
        cb.MoveTo(itemX + itemWidth, itemY + itemHeight);
        cb.LineTo(itemX + itemWidth, lengthY + 3);
        cb.Stroke();

        // Length label
        AddText(cb, $"{itemActualLength}\"", itemX + itemWidth / 2, lengthY + 5, 8, Font.BOLD,
            Element.ALIGN_CENTER, RED_LINE);

        // Width dimension (vertical, left of item)
        float widthX = itemX - 15;

        // Main vertical line
        cb.MoveTo(widthX, itemY);
        cb.LineTo(widthX, itemY + itemHeight);
        cb.Stroke();

        // Horizontal connecting lines at edges
        cb.MoveTo(itemX, itemY);
        cb.LineTo(widthX + 3, itemY);
        cb.MoveTo(itemX, itemY + itemHeight);
        cb.LineTo(widthX + 3, itemY + itemHeight);
        cb.Stroke();

        // Width label (rotated)
        cb.BeginText();
        cb.SetFontAndSize(_bf, 8);
        cb.SetColorFill(RED_LINE);
        cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, $"{itemActualWidth}\"",
            widthX - 8, itemY + itemHeight / 2, 90);
        cb.EndText();
    }

    private void DrawPourGroupDiagrams(Document doc, PdfContentByte cb, List<StandardMold> molds)
    {
        float startY = PAGE_HEIGHT - 120;
        float currentY = startY;
        float minYBeforeTable = 320; // Reserve space for table

        foreach (var mold in molds)
        {
            if (currentY < minYBeforeTable)
            {
                doc.NewPage();
                currentY = startY;
            }

            float height = DrawMoldOverview(cb, MARGIN, currentY, mold, false, true, true);
            currentY -= height + 25; // Reduced spacing between diagrams
        }
    }

    private void DrawCalculationTable(PdfContentByte cb, List<TableRow> tableData, string pourCategory)
    {
        // Position table much higher to be closer to diagrams
        float tableStartY = 280; // Moved up from 200
        float tableStartX = MARGIN - 10; // Align with page margin

        string[] headers =
        {
            "MOLD SIZE",
            "NO. OF\nPCS",
            "POURED SIZE\n(INCHES)",
            "NO. OF\nPCS",
            "POURED SIZE\n(INCHES)",
            "NO. OF\nPCS",
            "POURED SIZE\n(INCHES)",
            "EXTRA\nMARGIN",
            "TOTAL LENGTH\nREQUIRED\nTO POUR",
            "TOTAL\nAREA\n(SQ.INCH)",
            "NOTE"
        };

        float[] columnWidths = { 52, 32, 50, 32, 50, 32, 50, 35, 58, 55, 35 };

        float currentX = tableStartX;
        float headerY = tableStartY;

        // Draw header row
        for (int i = 0; i < headers.Length; i++)
        {
            BaseColor headerColor = (i == 9) ? GREEN_FILL : GRAY_FILL;
            DrawRectangle(cb, currentX, headerY, columnWidths[i], 35, BLACK_LINE, headerColor);

            // Use multi-line text for headers
            ColumnText ct = new ColumnText(cb);
            Phrase phrase = new Phrase(headers[i], new Font(Font.FontFamily.HELVETICA, 6.5f, Font.BOLD));
            ct.SetSimpleColumn(phrase,
                currentX + 2, headerY + 2,
                currentX + columnWidths[i] - 2, headerY + 33,
                7, Element.ALIGN_CENTER);
            ct.Go();

            currentX += columnWidths[i];
        }

        // Draw data rows
        float rowY = headerY - 22;
        float totalArea = 0;

        foreach (var row in tableData)
        {
            currentX = tableStartX;
            string[] rowData =
            {
                row.MoldSize,
                row.NumberOfPcs > 0 ? row.NumberOfPcs.ToString() : "",
                row.PouredSize,
                row.PouringPcs > 0 ? row.PouringPcs.ToString() : "",
                row.PouredSize,
                row.PouringPcs > 0 ? row.PouringPcs.ToString() : "",
                row.Poureable > 0 ? row.Poureable.ToString("F0") : "",
                row.ExtraMargin.ToString(),
                row.TotalLengthRequired > 0 ? row.TotalLengthRequired.ToString("F0") : "0",
                row.TotalAreaRequired.ToString("F0"),
                row.Note
            };

            for (int i = 0; i < rowData.Length; i++)
            {
                BaseColor cellColor = (i == 9) ? GREEN_FILL : null;
                DrawRectangle(cb, currentX, rowY, columnWidths[i], 22, BLACK_LINE, cellColor);

                if (!string.IsNullOrEmpty(rowData[i]) && rowData[i] != "0")
                {
                    AddText(cb, rowData[i], currentX + columnWidths[i] / 2, rowY + 11, 7.5f, Font.NORMAL,
                        Element.ALIGN_CENTER);
                }

                currentX += columnWidths[i];
            }

            totalArea += row.TotalAreaRequired;
            rowY -= 22;
        }

        // Total row
        currentX = tableStartX;
        for (int i = 0; i < columnWidths.Length; i++)
        {
            BaseColor cellColor = (i == 9) ? GREEN_FILL : GRAY_FILL;
            DrawRectangle(cb, currentX, rowY, columnWidths[i], 22, BLACK_LINE, cellColor);

            if (i == 0)
                AddText(cb, "TOTAL", currentX + columnWidths[i] / 2, rowY + 11, 8, Font.BOLD, Element.ALIGN_CENTER);
            if (i == 9)
                AddText(cb, totalArea.ToString("F0"), currentX + columnWidths[i] / 2, rowY + 11, 8, Font.BOLD,
                    Element.ALIGN_CENTER);

            currentX += columnWidths[i];
        }

        // Draw footer closer to table 
        DrawTableFooter(cb, rowY - 5, totalArea);
    }

    private void DrawTableFooter(PdfContentByte cb, float startY, float totalArea)
    {
        int requiredBags = BagCalculationRules.CalculateRequiredBags(totalArea);

        // Title with gray background bar
        // float barWidth = PAGE_WIDTH - (2 * MARGIN);
        // DrawRectangle(cb, MARGIN - 10, startY - 5, barWidth, 18, BLACK_LINE, GRAY_FILL);
        // AddText(cb, "TOTAL AREA FOR CONCRETE MIX (SQUARE INCH)", MARGIN + 10, startY + 3, 8, Font.BOLD);
        // AddText(cb, totalArea.ToString("F0"), PAGE_WIDTH - MARGIN - 50, startY + 3, 9, Font.BOLD, Element.ALIGN_RIGHT,
        //     RED_LINE);
        float barWidth = PAGE_WIDTH - (2 * MARGIN);
        float bagsY = startY - 35;
        DrawRectangle(cb, MARGIN -10, startY-17, barWidth, 18, BLACK_LINE, GRAY_FILL);
        AddText(cb, "TOTAL AREA FOR CONCRETE MIX (SQUARE INCH)", MARGIN + 10, startY - 12, 8, Font.BOLD);
        AddText(cb, totalArea.ToString("F0"), PAGE_WIDTH - MARGIN - 50, startY - 12, 9, Font.BOLD, Element.ALIGN_RIGHT,
            RED_LINE);

        // Number of bags row
      //  float bagsY = startY - 28;
        DrawRectangle(cb, MARGIN-10, bagsY, barWidth, 18, BLACK_LINE, GRAY_FILL);
        AddText(cb, "NUMBER OF BAGS REQUIRED", MARGIN + 10, bagsY + 6, 8, Font.BOLD);

        // Bags required box (right side)
        float boxWidth = 100;
        float boxX = PAGE_WIDTH - MARGIN - boxWidth - 5;
        DrawRectangle(cb, boxX, bagsY + 2, boxWidth -10, 14, RED_LINE, LIGHT_BLUE);
        AddText(cb, requiredBags.ToString() + " BAGS", boxX + boxWidth / 2, bagsY + 8, 9, Font.BOLD,
            Element.ALIGN_CENTER, RED_LINE);

        // Reference information
        float refY = bagsY - 18;
        string refInfo =
            "1 BAG ⇒ 1575 SQ. INCH | 3 BAGS ⇒ 4725 SQ. INCH | 4 BAGS ⇒ 6300 SQ. INCH | 5 BAGS ⇒ 7875 SQ. INCH | 6 BAGS ⇒ 9450 SQ. INCH";
        AddText(cb, refInfo, PAGE_WIDTH / 2, refY, 6.5f, Font.NORMAL, Element.ALIGN_CENTER);
    }

    private string GetDayName(string date)
    {
        if (DateTime.TryParse(date, out DateTime parsedDate))
            return parsedDate.DayOfWeek.ToString();
        return "DAY";
    }


    private void DrawFooter(PdfContentByte cb)
    {
        float footerY = 80;
        AddText(cb, "TOTAL AREA FOR CONCRETE MIX (SQUARE INCH):", MARGIN, footerY, 8, Font.BOLD);
        AddText(cb, "NUMBER OF BAGS REQUIRED:", MARGIN, footerY - 15, 8, Font.BOLD);

        string footerInfo = "1 BAG = 1675 SQ. INCH, 2 BAGS = 3350 SQ. INCH, etc.";
        AddText(cb, footerInfo, MARGIN, footerY - 30, 8, Font.NORMAL);

        float totalBoxWidth = 80;
        float totalBoxX = PAGE_WIDTH - MARGIN - totalBoxWidth;
        DrawRectangle(cb, totalBoxX, footerY - 15, totalBoxWidth, 30, RED_LINE, LIGHT_BLUE);
        AddText(cb, "TOTAL", totalBoxX + totalBoxWidth / 2, footerY + 5, 10, Font.BOLD, Element.ALIGN_CENTER);
    }
//nneeww

    private void DrawEnhancedTitleBox(PdfContentByte cb, string text, float centerX, float centerY, float fontSize,
        BaseColor color)
    {
        float textWidth = _bfBold.GetWidthPoint(text, fontSize);
        float textHeight = fontSize * 1.2f;

        // Outer border
        float outerPadding = 8f;
        float outerX = centerX - textWidth / 2 - outerPadding;
        float outerY = centerY - textHeight / 2 - outerPadding;
        float outerW = textWidth + outerPadding * 2;
        float outerH = textHeight + outerPadding * 2;

        // Inner border
        float innerPadding = 4f;
        float innerX = centerX - textWidth / 2 - innerPadding;
        float innerY = centerY - textHeight / 2 - innerPadding;
        float innerW = textWidth + innerPadding * 2;
        float innerH = textHeight + innerPadding * 2;

        cb.SaveState();

        // Draw outer rectangle with fill
        cb.SetColorFill(color);
        cb.SetColorStroke(BLACK_LINE);
        cb.SetLineWidth(2f);
        cb.Rectangle(outerX, outerY, outerW, outerH);
        cb.FillStroke();

        // Draw inner rectangle
        cb.SetColorFill(WHITE_FILL);
        cb.SetLineWidth(1f);
        cb.Rectangle(innerX, innerY, innerW, innerH);
        cb.FillStroke();

        cb.RestoreState();

        // Draw text
        cb.BeginText();
        cb.SetFontAndSize(_bfBold, fontSize);
        cb.SetColorFill(BLACK_LINE);
        cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, text, centerX, centerY - fontSize / 3, 0);
        cb.EndText();
    }

    private void DrawInfoBox(PdfContentByte cb, string label, string value, float x, float y, float width,
        float height)
    {
        // Draw box
        cb.SaveState();
        cb.SetColorStroke(BLACK_LINE);
        cb.SetColorFill(WHITE_FILL);
        cb.SetLineWidth(1f);
        cb.Rectangle(x, y, width, height);
        cb.FillStroke();
        cb.RestoreState();

        // Draw label
        cb.BeginText();
        cb.SetFontAndSize(_bfBold, 9);
        cb.SetColorFill(BLACK_LINE);
        cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, label, x + 5, y + height / 2 - 3, 0);
        cb.EndText();

        // Draw value
        if (!string.IsNullOrEmpty(value))
        {
            float labelWidth = _bfBold.GetWidthPoint(label, 9);
            cb.BeginText();
            cb.SetFontAndSize(_bf, 9);
            cb.SetColorFill(BLACK_LINE);
            cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, value, x + labelWidth + 10, y + height / 2 - 3, 0);
            cb.EndText();
        }
    }

    private void DrawEnhancedHeader(PdfContentByte cb, string date, string color, string title,
        bool showTitleHeaderAlone = false)
    {
        if (!showTitleHeaderAlone)
        {
            AddText(cb, "DATE: " + date, MARGIN, PAGE_HEIGHT - 40, 10, Font.BOLD);
            AddText(cb, "COLOR: " + color, MARGIN, PAGE_HEIGHT - 55, 10, Font.BOLD);
            //this comes after the title
            AddText(cb, "NOTE:", PAGE_WIDTH - MARGIN - 100, PAGE_HEIGHT - 40, 10, Font.BOLD);
        }

        // Center title with light-blue background (text only color here)
        AddText(cb, title, PAGE_WIDTH / 2, PAGE_HEIGHT - 30, 16, Font.BOLD, Element.ALIGN_CENTER, LIGHT_BLUE);
        // Draw main title with professional double-border styling
        float titleY = PAGE_HEIGHT - 30;
        DrawEnhancedTitleBox(cb, title, PAGE_WIDTH / 2, titleY, 14, TITLE_BLUE);

        cb.Stroke();
    }

    private void DrawGroupBackgroundBox(PdfContentByte cb, float x, float y, float width, float height,
        BaseColor color)
    {
        cb.SaveState();
        var gs = new PdfGState { FillOpacity = 0.1f, StrokeOpacity = 0.6f };
        cb.SetGState(gs);
        cb.SetColorFill(color);
        cb.SetColorStroke(color);
        cb.SetLineWidth(2f);
        cb.Rectangle(x, y - height, width, height);
        cb.FillStroke();
        cb.RestoreState();
    }

    private void DrawSideLengthInfo(PdfContentByte cb, float x, float y, string line1, string line2, string total)
    {
        cb.BeginText();
        cb.SetFontAndSize(_bf, 7);
        cb.SetColorFill(BLACK_LINE);
        cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, line1, x, y + 5, 0);
        cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, line2, x, y - 3, 0);
        cb.EndText();

        cb.BeginText();
        cb.SetFontAndSize(_bfBold, 8);
        cb.SetColorFill(RED_LINE);
        cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, total, x + 55, y - 3, 0);
        cb.EndText();
    }

    private void DrawSideTotalsWithArrows(PdfContentByte cb, float containerX, float containerY,
        float containerWidth, StandardMold mold)
    {
        float moldRightX = containerX + containerWidth;
        float labelX = moldRightX + 15;

        // Draw totals for top side
        if (mold.TopSideItems.Any())
        {
            // Calculate actual total: sum of all item lengths + margin
            double sumOfLengths = mold.TopSideItems.Sum(item => item.Length);
            double topSideTotal = sumOfLengths + MARGIN_SIZE;

            // Calculate center Y position for top side items
            var topItems = mold.TopSideItems.OrderBy(i => i.YPosition).ToList();
            float topMinY = (float)topItems.First().YPosition;
            float topMaxY = (float)(topItems.Last().YPosition + topItems.Last().Width);
            float topCenterY = containerY - ((topMinY + topMaxY) / 2) * SCALE_FACTOR;

            DrawSideLengthInfo(cb, labelX, topCenterY, "SIDE 1 TOTAL LENGTH", "WITH MARGIN",
                $"= {topSideTotal:F0}\"");
            DrawArrowToMold(cb, moldRightX, topCenterY, RED_LINE);
        }

        // Draw totals for bottom side
        if (mold.BottomSideItems.Any())
        {
            // Calculate actual total: sum of all item lengths + margin
            double sumOfLengths = mold.BottomSideItems.Sum(item => item.Length);
            double bottomSideTotal = sumOfLengths + MARGIN_SIZE;

            // Calculate center Y position for bottom side items
            var bottomItems = mold.BottomSideItems.OrderBy(i => i.YPosition).ToList();
            float bottomMinY = (float)bottomItems.First().YPosition;
            float bottomMaxY = (float)(bottomItems.Last().YPosition + bottomItems.Last().Width);
            float bottomCenterY = containerY - ((bottomMinY + bottomMaxY) / 2) * SCALE_FACTOR;

            DrawSideLengthInfo(cb, labelX, bottomCenterY, "SIDE 2 TOTAL LENGTH", "WITH MARGIN",
                $"= {bottomSideTotal:F0}\"");
            DrawArrowToMold(cb, moldRightX, bottomCenterY, RED_LINE);
        }
    }

    private void DrawArrowToMold(PdfContentByte cb, float moldRightX, float arrowY, BaseColor color)
    {
        cb.SaveState();
        cb.SetColorStroke(color);
        cb.SetColorFill(color);
        cb.SetLineWidth(1.5f);

        // Arrow shaft - pointing FROM label TO mold
        float shaftLength = 12f;
        float arrowEnd = moldRightX;
        float arrowStart = moldRightX + shaftLength;

        // Draw shaft
        cb.MoveTo(arrowStart, arrowY);
        cb.LineTo(arrowEnd, arrowY);
        cb.Stroke();

        // Draw arrowhead pointing LEFT (into the mold)
        float headSize = 4f;
        cb.MoveTo(arrowEnd, arrowY);
        cb.LineTo(arrowEnd + headSize, arrowY + headSize * 0.7f);
        cb.LineTo(arrowEnd + headSize, arrowY - headSize * 0.7f);
        cb.ClosePathFillStroke();

        cb.RestoreState();
    }


    private void DrawMoldDimensions(PdfContentByte cb, float x, float y, float width, float height,
        double actualLength, double actualWidth)
    {
        cb.SetColorStroke(RED_LINE);
        cb.SetLineWidth(1f);

        // Length dimension (horizontal, above mold)
        float dimY = y + height + 10;
        cb.MoveTo(x, dimY);
        cb.LineTo(x + width, dimY);
        cb.Stroke();

        // Dimension markers
        cb.MoveTo(x, y + height);
        cb.LineTo(x, dimY + 3);
        cb.MoveTo(x + width, y + height);
        cb.LineTo(x + width, dimY + 3);
        cb.Stroke();

        // Dimension text
        cb.BeginText();
        cb.SetFontAndSize(_bf, 8);
        cb.SetColorFill(RED_LINE);
        cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, $"{actualLength}\"", x + width / 2, dimY + 5, 0);
        cb.EndText();

        // Width dimension (vertical, left side)
        float dimX = x - 15;
        cb.MoveTo(dimX, y);
        cb.LineTo(dimX, y + height);
        cb.Stroke();

        // Width markers
        cb.MoveTo(x, y);
        cb.LineTo(dimX - 3, y);
        cb.MoveTo(x, y + height);
        cb.LineTo(dimX - 3, y + height);
        cb.Stroke();

        // Width text (rotated)
        cb.BeginText();
        cb.SetFontAndSize(_bf, 8);
        cb.SetColorFill(RED_LINE);
        cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, $"{actualWidth}\"", dimX - 8, y + height / 2, 90);
        cb.EndText();
    }

    private void DrawPourCategoryCircle(PdfContentByte cb, string category, float centerX, float centerY,
        BaseColor color, bool needsCircle = true)
    {
        float radius = 12f;

        cb.SaveState();
        cb.SetColorFill(WHITE_FILL);
        cb.SetColorStroke(color);
        cb.SetLineWidth(1f);
        if (needsCircle)
        {
            cb.Circle(centerX, centerY - 2, radius);
        }

        cb.FillStroke();
        cb.RestoreState();

        cb.BeginText();
        cb.SetFontAndSize(_bfBold, 8);
        cb.SetColorFill(color);
        cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, category, centerX, centerY - 3, 0);
        cb.EndText();
    }

    #region Helper Methods

    private void DrawRectangle(PdfContentByte cb, float x, float y, float width, float height,
        BaseColor strokeColor, BaseColor fillColor, bool isItemrectangle = false)
    {
        cb.SaveState();
        if (fillColor != null)
        {
            cb.SetColorFill(fillColor);
            cb.Rectangle(x, y, width, height);
            cb.Fill();
        }

        cb.SetColorStroke(strokeColor ?? BLACK_LINE);
        cb.SetLineWidth(isItemrectangle ? 1.5f : 1f);
        cb.Rectangle(x, y, width, height);
        cb.Stroke();
        cb.RestoreState();
    }

    private void AddText(PdfContentByte cb, string text, float x, float y, float fontSize, int fontStyle,
        int alignment = Element.ALIGN_LEFT, BaseColor color = null)
    {
        cb.BeginText();
        cb.SetFontAndSize(_bf, fontSize);
        cb.SetColorFill(color ?? BaseColor.BLACK);

        switch (alignment)
        {
            case Element.ALIGN_CENTER:
                cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, text, x, y, 0);
                break;
            case Element.ALIGN_RIGHT:
                cb.ShowTextAligned(PdfContentByte.ALIGN_RIGHT, text, x, y, 0);
                break;
            default:
                cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, text, x, y, 0);
                break;
        }

        cb.EndText();
    }

    private float MeasureTextWidth(string text, float fontSize)
    {
        return _bf.GetWidthPoint(text ?? string.Empty, fontSize);
    }

    private float LineHeight(float fontSize) => fontSize * 1.2f;

    // Draw a left-pointing arrow whose shaft crosses the mold's right edge equally
    private void DrawLeftArrowAcrossEdge(PdfContentByte cb, float centerY, float moldRightX,
        float halfShaftLen = 18f, float headSize = 4.5f, BaseColor color = null, float lineWidth = 1.1f)
    {
        cb.SaveState();
        cb.SetColorStroke(color ?? RED_LINE);
        cb.SetColorFill(color ?? RED_LINE);
        cb.SetLineWidth(lineWidth);

        float xOutside = moldRightX + halfShaftLen; // outside the mold
        float xInside = moldRightX - halfShaftLen; // inside the mold

        // shaft
        cb.MoveTo(xOutside, centerY);
        cb.LineTo(xInside, centerY);
        cb.Stroke();

        // arrow head at the left end (points left)
        cb.MoveTo(xInside, centerY);
        cb.LineTo(xInside + headSize, centerY + headSize * 0.6f);
        cb.LineTo(xInside + headSize, centerY - headSize * 0.6f);
        cb.ClosePathFillStroke();

        cb.RestoreState();
    }

    // Draw text centered and surround it with a tight circle
    private void DrawTextInCircle(PdfContentByte cb, string text, float centerX, float baselineY,
        float fontSize, int fontStyle, BaseColor stroke, BaseColor fill = null, float padding = 2f)
    {
        // Draw the text
        cb.BeginText();
        cb.SetFontAndSize(_bf, fontSize);
        cb.SetColorFill(BaseColor.BLACK);
        cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, text ?? string.Empty, centerX, baselineY, 0);
        cb.EndText();

        // Compute size for circle
        float textW = MeasureTextWidth(text, fontSize);
        float textH = LineHeight(fontSize);
        float diameter = Math.Max(textW, textH) + padding * 2f;

        // Optical center adjustment
        float cy = baselineY + textH * 0.35f;
        float r = diameter / 2f;

        cb.SaveState();
        if (fill != null) cb.SetColorFill(fill);
        cb.SetColorStroke(stroke ?? BLACK_LINE);
        cb.SetLineWidth(1f);
        cb.Circle(centerX, cy, r);
        if (fill != null) cb.FillStroke();
        else cb.Stroke();
        cb.RestoreState();
    }

    // Draw left-aligned text and place a tight rectangle around it
    private void DrawTextWithTightRect(PdfContentByte cb, string text, float leftX, float baselineY,
        float fontSize, int fontStyle, BaseColor stroke, BaseColor fill = null, float padding = 3f,
        BaseColor textColor = null)
    {
        // Draw text
        cb.BeginText();
        cb.SetFontAndSize(_bf, fontSize);
        cb.SetColorFill(textColor ?? BaseColor.BLACK);
        cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, text ?? string.Empty, leftX, baselineY, 0);
        cb.EndText();

        // Compute rectangle around text
        float w = MeasureTextWidth(text, fontSize);
        float h = LineHeight(fontSize);

        float rectX = leftX - padding;
        float rectY = baselineY - padding;
        float rectW = w + padding * 2f;
        float rectH = h + padding * 1.2f;

        cb.SaveState();
        if (fill != null) cb.SetColorFill(fill);
        cb.SetColorStroke(stroke ?? BLACK_LINE);
        cb.SetLineWidth(1f);
        cb.Rectangle(rectX, rectY, rectW, rectH);
        if (fill != null) cb.FillStroke();
        else cb.Stroke();
        cb.RestoreState();
    }

    // Draw a faint translucent group rectangle
    private void DrawTransparentGroupBox(PdfContentByte cb, float leftX, float topY, float bottomY, float width,
        BaseColor color, float fillOpacity = 0.08f, float strokeOpacity = 0.35f)
    {
        if (topY <= bottomY) return;

        var gs = new PdfGState { FillOpacity = fillOpacity, StrokeOpacity = strokeOpacity };
        cb.SaveState();
        cb.SetGState(gs);
        cb.SetColorFill(color);
        cb.SetColorStroke(color);
        cb.SetLineWidth(0.8f);

        float height = topY - bottomY;
        cb.Rectangle(leftX, bottomY, width, height);
        cb.FillStroke();
        cb.RestoreState();
    }

    #endregion
}

// Example usage
public class PouringPlanService
{
    private readonly DailyPouringPlanGenerator _planGenerator;
    private readonly DayToDayPourPlanGenerator _sheetGenerator;

    public PouringPlanService(DailyPouringPlanGenerator pouringPlanGenerator,
        DayToDayPourPlanGenerator dayToDayPourPlanGenerator)
    {
        _planGenerator = pouringPlanGenerator;
        _sheetGenerator = dayToDayPourPlanGenerator;
    }


    public byte[] GenerateDailyPourPlan(List<WorkOrderRequest5> workOrders, string date, string color,
        string pourNumber)
    {
        var pourPlan = _planGenerator.GenerateDailyPourPlan(workOrders, date, color);
        return _sheetGenerator.GeneratePourSheet(pourPlan, pourNumber);
    }

    /// <summary>
    /// Generates a complete multi-day pour plan PDF
    /// </summary>
    public byte[] GenerateMultiDayPourPlan(List<WorkOrderRequest5> workOrders, DateTime startDate,
        string color, string pourNumber = "1")
    {
        var multiDayPlan = _planGenerator.GenerateMultiDayPourPlan(workOrders, startDate, color);
        return _sheetGenerator.GenerateMultiDayPourSheet(multiDayPlan, pourNumber);
    }

    /// <summary>
    /// Gets the planning data without generating PDF - useful for API responses or further processing
    /// </summary>
    public MultiDayPourPlan GenerateMultiDayPourPlanData(List<WorkOrderRequest5> workOrders,
        DateTime startDate, string color)
    {
        return _planGenerator.GenerateMultiDayPourPlan(workOrders, startDate, color);
    }

    /// <summary>
    /// Gets detailed progress information for SharePoint integration
    /// </summary>
    public PourPlanProgress GetPourPlanProgress(List<WorkOrderRequest5> workOrders,
        DateTime startDate, string color)
    {
        var multiDayPlan = _planGenerator.GenerateMultiDayPourPlan(workOrders, startDate, color);

        return new PourPlanProgress
        {
            Summary = multiDayPlan.Summary,
            UnprocessedOrders = multiDayPlan.UnprocessedOrders,
            PartiallyProcessedOrders = multiDayPlan.PartiallyProcessedOrders,
            FullyProcessedOrders = multiDayPlan.FullyProcessedOrders,
            DailyBreakdown = multiDayPlan.DailyPlans.Select(dp => new DailyProgress
            {
                Date = dp.Date,
                DayName = dp.DayName,
                ItemsProcessed = dp.TotalItemsProcessed,
                CubicYards = dp.TotalCubicYards,
                TotalArea = dp.TotalArea,
                MoldsUsed = dp.AllMolds.Count(m => m.HasItems),
                ProcessedWorkOrders = dp.ProcessedWorkOrders.Select(wo => new WorkOrderSummary
                {
                    PurchaseOrder = wo.PurchaseOrder,
                    Company = wo.Company,
                    ItemsProcessedToday = wo.ItemProgress
                        .Where(ip => ip.DailyProcessedQuantity.ContainsKey(dp.Date))
                        .Sum(ip => ip.DailyProcessedQuantity[dp.Date])
                }).ToList()
            }).ToList()
        };
    }

    /// <summary>
    /// Updates work order progress for SharePoint tracking
    /// </summary>
    public List<WorkOrderUpdateRecord> GenerateSharePointUpdateRecords(List<WorkOrderRequest5> workOrders,
        DateTime startDate, string color)
    {
        var multiDayPlan = _planGenerator.GenerateMultiDayPourPlan(workOrders, startDate, color);
        var updateRecords = new List<WorkOrderUpdateRecord>();

        foreach (var processedOrder in multiDayPlan.FullyProcessedOrders.Concat(multiDayPlan
                     .PartiallyProcessedOrders))
        {
            updateRecords.Add(new WorkOrderUpdateRecord
            {
                PurchaseOrder = processedOrder.PurchaseOrder,
                ProcessingStatus = processedOrder.IsFullyProcessed ? "Completed" : "In Progress",
                CompletionPercentage = processedOrder.CompletionPercentage,
                LastProcessedDate = processedOrder.ItemProgress
                    .SelectMany(ip => ip.ProcessedOnDays)
                    .DefaultIfEmpty()
                    .Max(),
                RemainingItems = processedOrder.TotalRemainingItems,
                ProcessedItems = processedOrder.TotalOriginalItems - processedOrder.TotalRemainingItems,
                ItemDetails = processedOrder.ItemProgress.Select(ip => new ItemUpdateRecord
                {
                    LotName = ip.LotName,
                    OriginalQuantity = ip.OriginalQuantity,
                    ProcessedQuantity = ip.ProcessedQuantity,
                    RemainingQuantity = ip.RemainingQuantity,
                    ProcessedOnDays = ip.ProcessedOnDays,
                    DailyQuantities = ip.DailyProcessedQuantity
                }).ToList()
            });
        }

        return updateRecords;
    }

    /// <summary>
    /// Generate sample work orders for testing
    /// </summary>
    public static List<WorkOrderRequest5> GetEnhancedSampleWorkOrders()
    {
        return new List<WorkOrderRequest5>
        {
            new WorkOrderRequest5
            {
                OrderDate = DateTime.Now.AddDays(-2),
                // OrderDate = DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd"),
                PurchaseOrder = "WO-2025-001",
                Company = "ABC Construction Ltd",
                Priority = 1,
                ExpectedDeliveryDate = DateTime.Now.AddDays(5).ToString("yyyy-MM-dd"),
                Items = new List<Order>
                {
                    // new Order
                    // {
                    //     LotName = "ABC-A1", Quantity = 2, PourWidth = 30, PourLength = 153, Color = "Gray",
                    //     Type = "Standard"
                    // },
                    // new Order
                    // {
                    //     LotName = "ABC-A1", Quantity = 3, PourWidth = 36, PourLength = 76.5, Color = "Gray",
                    //     Type = "Standard"
                    // },
                    new Order
                    {
                        LotName = "ABC-A1", Quantity = 8, PourWidth = 20, PourLength = 22, Color = "Gray",
                        Type = "Standard"
                    },
                    new Order
                    {
                        LotName = "ABC-A2", Quantity = 4, PourWidth = 34, PourLength = 34, Color = "Gray",
                        Type = "Large"
                    },
                    new Order
                    {
                        LotName = "ABC-A3", Quantity = 6, PourWidth = 18, PourLength = 20, Color = "Gray",
                        Type = "Small"
                    },
                    new Order
                    {
                        LotName = "ABC-B1", Quantity = 3, PourWidth = 24, PourLength = 50, Color = "Gray",
                        Type = "Medium"
                    }
                }
            },
            new WorkOrderRequest5
            {
                OrderDate = DateTime.Now.AddDays(-1),
                //OrderDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"),
                PurchaseOrder = "WO-2025-002",
                Company = "XYZ Developers Inc",
                Priority = 2,
                ExpectedDeliveryDate = DateTime.Now.AddDays(7).ToString("yyyy-MM-dd"),
                Items = new List<Order>
                {
                    new Order
                    {
                        LotName = "XYZ-C1", Quantity = 5, PourWidth = 22, PourLength = 48, Color = "Gray",
                        Type = "Standard"
                    },
                    new Order
                    {
                        LotName = "XYZ-C2", Quantity = 7, PourWidth = 26, PourLength = 60, Color = "Gray",
                        Type = "Long"
                    },
                    new Order
                    {
                        LotName = "XYZ-D1", Quantity = 3, PourWidth = 30, PourLength = 40, Color = "Gray",
                        Type = "Wide"
                    }
                }
            },
            new WorkOrderRequest5
            {
                OrderDate = DateTime.Now,
                //OrderDate = DateTime.Now.ToString("yyyy-MM-dd"),
                PurchaseOrder = "WO-2025-003",
                Company = "DEF Infrastructure",
                Priority = 3,
                ExpectedDeliveryDate = DateTime.Now.AddDays(10).ToString("yyyy-MM-dd"),
                Items = new List<Order>
                {
                    new Order
                    {
                        LotName = "DEF-E1", Quantity = 10, PourWidth = 24, PourLength = 36, Color = "Gray",
                        Type = "Standard"
                    },
                    new Order
                    {
                        LotName = "DEF-E2", Quantity = 4, PourWidth = 28, PourLength = 55, Color = "Gray",
                        Type = "Custom"
                    },
                    new Order
                    {
                        LotName = "DEF-F1", Quantity = 6, PourWidth = 20, PourLength = 30, Color = "Gray",
                        Type = "Small"
                    }
                }
            }
        };
    }


    public static List<WorkOrderRequest5> GetSampleWorkOrders()
    {
        return new List<WorkOrderRequest5>
        {
            new WorkOrderRequest5
            {
                OrderDate = new DateTime(2025, 09, 05),
                PurchaseOrder = "WO-001",
                Company = "ABC Construction",
                Priority = 1,
                Items = new List<Order>
                {
                    new Order { LotName = "A1", Quantity = 1, PourWidth = 30, PourLength = 154 },
                    new Order { LotName = "A1", Quantity = 2, PourWidth = 36, PourLength = 77 },
                    new Order { LotName = "A1", Quantity = 6, PourWidth = 20, PourLength = 22 },
                    new Order { LotName = "A10", Quantity = 2, PourWidth = 34, PourLength = 34 },
                    new Order { LotName = "A11", Quantity = 2, PourWidth = 30, PourLength = 34 },
                    new Order { LotName = "A12", Quantity = 2, PourWidth = 18, PourLength = 20 },
                    new Order { LotName = "A1", Quantity = 2, PourWidth = 24, PourLength = 34 },
                    new Order { LotName = "A2", Quantity = 1, PourWidth = 20, PourLength = 100 },
                    new Order { LotName = "A3", Quantity = 1, PourWidth = 49, PourLength = 80 }
                }
            },
            new WorkOrderRequest5
            {
                OrderDate = new DateTime(2025, 09, 04),
                PurchaseOrder = "WO-002",
                Company = "XYZ Builders",
                Priority = 2,
                Items = new List<Order>
                {
                    new Order { LotName = "B1", Quantity = 3, PourWidth = 22, PourLength = 50 },
                    new Order { LotName = "B2", Quantity = 2, PourWidth = 26, PourLength = 60 }
                }
            }
        };
    }


    // #region Progress Tracking Models
//
    public class PourPlanProgress
    {
        public PourPlanSummary Summary { get; set; }
        public List<WorkOrderProgress> UnprocessedOrders { get; set; }
        public List<WorkOrderProgress> PartiallyProcessedOrders { get; set; }
        public List<WorkOrderProgress> FullyProcessedOrders { get; set; }
        public List<DailyProgress> DailyBreakdown { get; set; }
    }

    public class DailyProgress
    {
        public string Date { get; set; }
        public string DayName { get; set; }
        public int ItemsProcessed { get; set; }
        public float CubicYards { get; set; }
        public float TotalArea { get; set; }
        public int MoldsUsed { get; set; }
        public List<WorkOrderSummary> ProcessedWorkOrders { get; set; }
    }

    public class WorkOrderSummary
    {
        public string PurchaseOrder { get; set; }
        public string Company { get; set; }
        public int ItemsProcessedToday { get; set; }
    }

    public class WorkOrderUpdateRecord
    {
        public string PurchaseOrder { get; set; }
        public string ProcessingStatus { get; set; }
        public double CompletionPercentage { get; set; }
        public string LastProcessedDate { get; set; }
        public int RemainingItems { get; set; }
        public int ProcessedItems { get; set; }
        public List<ItemUpdateRecord> ItemDetails { get; set; }
    }

    public class ItemUpdateRecord
    {
        public string LotName { get; set; }
        public int OriginalQuantity { get; set; }
        public int ProcessedQuantity { get; set; }
        public int RemainingQuantity { get; set; }
        public List<string> ProcessedOnDays { get; set; }
        public Dictionary<string, int> DailyQuantities { get; set; }
    }

    #endregion
}