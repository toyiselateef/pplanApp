// using iTextSharp.text;
// using iTextSharp.text.pdf;
//
// namespace MfgDocs.Api.Services.Generators
// {
//     #region Enhanced Configuration Models
//
//     public class PouringPlanConfig
//     {
//         public List<string> WorkDays { get; set; } = new List<string> { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
//         public int MaxWorkDaysToGenerate { get; set; } = 3;
//         public bool AllowExtensionBeyondMaxDays { get; set; } = false;
//         public List<WorkOrderPriorityCriteria> PriorityCriteria { get; set; } = new List<WorkOrderPriorityCriteria>();
//         public double MarginSize { get; set; } = 3.0;
//         public float ScaleFactor { get; set; } = 1.5f;
//         public bool TrackUnprocessedItems { get; set; } = true;
//     }
//
//     public class WorkOrderPriorityCriteria
//     {
//         public string Field { get; set; } // "Priority", "OrderDate", "ExpectedDeliveryDate"
//         public bool Ascending { get; set; } = true;
//         public int Order { get; set; } = 0;
//     }
//
//     public class WorkOrderProgress
//     {
//         public string PurchaseOrder { get; set; }
//         public string Company { get; set; }
//         public int Priority { get; set; }
//         public string OrderDate { get; set; }
//         public string ExpectedDeliveryDate { get; set; }
//         public List<ItemProgress> ItemProgress { get; set; } = new List<ItemProgress>();
//         public bool IsFullyProcessed => ItemProgress.All(i => i.RemainingQuantity == 0);
//         public bool IsPartiallyProcessed => ItemProgress.Any(i => i.ProcessedQuantity > 0 && i.RemainingQuantity > 0);
//         public int TotalRemainingItems => ItemProgress.Sum(i => i.RemainingQuantity);
//         public int TotalOriginalItems => ItemProgress.Sum(i => i.OriginalQuantity);
//         public double CompletionPercentage => TotalOriginalItems > 0 ? 
//             (double)(TotalOriginalItems - TotalRemainingItems) / TotalOriginalItems * 100 : 0;
//     }
//
//     public class ItemProgress
//     {
//         public string LotName { get; set; }
//         public int OriginalQuantity { get; set; }
//         public int ProcessedQuantity { get; set; }
//         public int RemainingQuantity => OriginalQuantity - ProcessedQuantity;
//         public double PourWidth { get; set; }
//         public double PourLength { get; set; }
//         public double FinishedLength { get; set; }
//         public double FinishedWidth { get; set; }
//         public string Color { get; set; }
//         public string Type { get; set; }
//         public List<string> ProcessedOnDays { get; set; } = new List<string>(); // Track which days items were processed
//         public Dictionary<string, int> DailyProcessedQuantity { get; set; } = new Dictionary<string, int>(); // Track quantity per day
//     }
//
//     #endregion
//
//     #region Enhanced Data Models
//
//     public class MultiDayPourPlan
//     {
//         public List<DailyPourPlan> DailyPlans { get; set; } = new List<DailyPourPlan>();
//         public List<WorkOrderProgress> UnprocessedOrders { get; set; } = new List<WorkOrderProgress>();
//         public List<WorkOrderProgress> PartiallyProcessedOrders { get; set; } = new List<WorkOrderProgress>();
//         public List<WorkOrderProgress> FullyProcessedOrders { get; set; } = new List<WorkOrderProgress>();
//         public PouringPlanConfig Config { get; set; }
//         public PourPlanSummary Summary { get; set; } = new PourPlanSummary();
//     }
//
//     public class PourPlanSummary
//     {
//         public int TotalWorkOrders { get; set; }
//         public int ProcessedWorkOrders { get; set; }
//         public int PartiallyProcessedWorkOrders { get; set; }
//         public int UnprocessedWorkOrders { get; set; }
//         public int TotalItems { get; set; }
//         public int ProcessedItems { get; set; }
//         public int RemainingItems { get; set; }
//         public double OverallCompletionPercentage { get; set; }
//         public int TotalPourDays { get; set; }
//         public List<string> PourDates { get; set; } = new List<string>();
//     }
//
//     public class DailyPourPlan
//     {
//         public string Date { get; set; }
//         public string DayName { get; set; }
//         public string Color { get; set; }
//         public List<StandardMold> AllMolds { get; set; } = new List<StandardMold>();
//         public Dictionary<string, List<StandardMold>> PourGroups { get; set; } = new Dictionary<string, List<StandardMold>>();
//         public List<TableRow> CalculationTable { get; set; } = new List<TableRow>();
//         public bool HasItems => AllMolds.Any(m => m.HasItems);
//         public int TotalItemsProcessed => AllMolds.Sum(m => m.AllItems.Count());
//         public float TotalCubicYards => CalculationTable.Sum(r => r.CubicYards);
//         public float TotalArea => CalculationTable.Sum(r => r.TotalArea);
//         public List<WorkOrderProgress> ProcessedWorkOrders { get; set; } = new List<WorkOrderProgress>();
//     }
//
//     public class MoldSide
//     {
//         public List<SlottedItem> Items { get; set; } = new List<SlottedItem>();
//         public double StartPosition { get; set; }
//         public double Width { get; set; }
//         public double MaxLength { get; set; }
//         public double UsedLength => Items.Any() ? Items.Max(i => i.XPosition + i.Length) : 0;
//         public double RemainingLength => MaxLength - UsedLength - 3; // Account for margin
//        //
//         public float XPosition { get; set; }
//         public float YPosition { get; set; }
//
//         public float Length { get; set; }
//
//         //
//         public bool CanFitItem(SlottedItem item) => item.Length <= RemainingLength && item.Width <= Width;
//     }
//
//     #endregion
//
//     public class EnhancedPouringPlanGenerator
//     {
//         private readonly List<StandardMold> _standardMolds;
//         private readonly PouringPlanConfig _config;
//         private const double DEFAULT_MARGIN_SIZE = 3.0;
//
//         public EnhancedPouringPlanGenerator(PouringPlanConfig config = null)
//         {
//             _config = config ?? GetDefaultConfig();
//             _standardMolds = InitializeStandardMolds();
//         }
//
//         private PouringPlanConfig GetDefaultConfig()
//         {
//             return new PouringPlanConfig
//             {
//                 WorkDays = new List<string> { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" },
//                 MaxWorkDaysToGenerate = 3,
//                 AllowExtensionBeyondMaxDays = true,
//                 PriorityCriteria = new List<WorkOrderPriorityCriteria>
//                 {
//                     new WorkOrderPriorityCriteria { Field = "Priority", Ascending = true, Order = 0 },
//                     new WorkOrderPriorityCriteria { Field = "OrderDate", Ascending = true, Order = 1 },
//                     new WorkOrderPriorityCriteria { Field = "ExpectedDeliveryDate", Ascending = true, Order = 2 }
//                 },
//                 MarginSize = 3.0,
//                 ScaleFactor = 1.5f,
//                 TrackUnprocessedItems = true
//             };
//         }
//
//         private List<StandardMold> InitializeStandardMolds()
//         {
//             return new List<StandardMold>
//             {
//                 new StandardMold { Name = "A", Width = 30, Length = 156, PourCategory = "P2" },
//                 new StandardMold { Name = "B", Width = 36, Length = 156, PourCategory = "P3" },
//                 new StandardMold { Name = "C", Width = 50, Length = 122, PourCategory = "P1" },
//                 new StandardMold { Name = "D", Width = 51.5, Length = 120, PourCategory = "P2" },
//                 new StandardMold { Name = "E", Width = 26, Length = 144, PourCategory = "P4" },
//                 new StandardMold { Name = "F", Width = 22, Length = 144, PourCategory = "P5" },
//                 new StandardMold { Name = "G", Width = 24, Length = 120, PourCategory = "P4" },
//                 new StandardMold { Name = "H", Width = 26, Length = 120, PourCategory = "P1" },
//                 new StandardMold { Name = "I", Width = 22, Length = 120, PourCategory = "P4" },
//                 new StandardMold { Name = "J", Width = 20, Length = 120, PourCategory = "P1" },
//                 new StandardMold { Name = "K", Width = 51, Length = 122, PourCategory = "P3" },
//                 new StandardMold { Name = "L", Width = 61, Length = 122, PourCategory = "P5" }
//             };
//         }
//
//         // Create fresh mold instances for each day
//         private List<StandardMold> CreateFreshMoldInstances()
//         {
//             return _standardMolds.Select(m => new StandardMold
//             {
//                 Name = m.Name,
//                 Width = m.Width,
//                 Length = m.Length,
//                 PourCategory = m.PourCategory,
//                 TopSideItems = new List<SlottedItem>(),
//                 BottomSideItems = new List<SlottedItem>()
//             }).ToList();
//         }
//
//         public MultiDayPourPlan GenerateMultiDayPourPlan(List<WorkOrderRequest5> workOrders, DateTime startDate, string color)
//         {
//             var multiDayPlan = new MultiDayPourPlan
//             {
//                 Config = _config
//             };
//
//             // Initialize work order progress tracking
//             var workOrderProgress = InitializeWorkOrderProgress(workOrders);
//             
//             // Sort work orders based on priority criteria
//             var sortedWorkOrders = SortWorkOrdersByPriority(workOrders);
//
//             var currentDate = GetNextWorkDay(startDate, _config.WorkDays);
//             int daysGenerated = 0;
//             var processedDates = new List<string>();
//
//             // Continue until max days reached or all orders processed (if extension allowed)
//             while (ShouldContinueProcessing(daysGenerated, workOrderProgress))
//             {
//                 var dailyPlan = GenerateDailyPourPlan(workOrderProgress, currentDate, color);
//                 
//                 if (dailyPlan.HasItems || daysGenerated < _config.MaxWorkDaysToGenerate)
//                 {
//                     multiDayPlan.DailyPlans.Add(dailyPlan);
//                     processedDates.Add(dailyPlan.Date);
//                     daysGenerated++;
//                 }
//
//                 currentDate = GetNextWorkDay(currentDate.AddDays(1), _config.WorkDays);
//
//                 // Break if all orders are processed
//                 if (workOrderProgress.All(w => w.IsFullyProcessed))
//                     break;
//
//                 // Safety check to prevent infinite loops
//                 if (daysGenerated > 30) // Maximum 30 days
//                     break;
//             }
//
//             // Categorize work orders by processing status
//             CategorizeWorkOrders(multiDayPlan, workOrderProgress);
//             
//             // Generate summary
//             GeneratePlanSummary(multiDayPlan, workOrders, processedDates);
//
//             return multiDayPlan;
//         }
//
//         private bool ShouldContinueProcessing(int daysGenerated, List<WorkOrderProgress> workOrderProgress)
//         {
//             // Continue if within max days
//             if (daysGenerated < _config.MaxWorkDaysToGenerate)
//                 return true;
//
//             // Continue if extension is allowed and there are unprocessed orders
//             if (_config.AllowExtensionBeyondMaxDays && workOrderProgress.Any(w => !w.IsFullyProcessed))
//                 return true;
//
//             return false;
//         }
//
//         private DateTime GetNextWorkDay(DateTime currentDate, List<string> workDays)
//         {
//             while (!workDays.Contains(currentDate.DayOfWeek.ToString()))
//             {
//                 currentDate = currentDate.AddDays(1);
//             }
//             return currentDate;
//         }
//
//         private List<WorkOrderProgress> InitializeWorkOrderProgress(List<WorkOrderRequest5> workOrders)
//         {
//             return workOrders.Select(wo => new WorkOrderProgress
//             {
//                 PurchaseOrder = wo.PurchaseOrder,
//                 Company = wo.Company ?? string.Empty,
//                 Priority = wo.Priority,
//                 OrderDate = wo.OrderDate ?? string.Empty,
//                 ExpectedDeliveryDate = wo.ExpectedDeliveryDate ?? string.Empty,
//                 ItemProgress = wo.Items.Select(item => new ItemProgress
//                 {
//                     LotName = item.LotName,
//                     OriginalQuantity = item.Quantity,
//                     ProcessedQuantity = 0,
//                     PourWidth = item.PourWidth,
//                     PourLength = item.PourLength,
//                     FinishedLength = item.FinishedLength,
//                     FinishedWidth = item.FinishedWidth,
//                     Color = item.Color,
//                     Type = item.Type
//                 }).ToList()
//             }).ToList();
//         }
//
//         private List<WorkOrderRequest5> SortWorkOrdersByPriority(List<WorkOrderRequest5> workOrders)
//         {
//             var query = workOrders.AsQueryable();
//             DateTime date1;
//             DateTime date2;
//             DateTime date3;
//             DateTime date4;
//             foreach (var criteria in _config.PriorityCriteria.OrderBy(c => c.Order))
//             {
//                 switch (criteria.Field.ToLower())
//                 {
//                     case "priority":
//                         query = criteria.Ascending ? query.OrderBy(w => w.Priority) : query.OrderByDescending(w => w.Priority);
//                         break;
//                     case "orderdate":
//                         query = criteria.Ascending 
//                             ? query.OrderBy(w => DateTime.TryParse(w.OrderDate, out  date1) ? date1 : DateTime.MaxValue)
//                             : query.OrderByDescending(w => DateTime.TryParse(w.OrderDate, out  date2) ? date2 : DateTime.MinValue);
//                         break;
//                     case "expecteddeliverydate":
//                         query = criteria.Ascending 
//                             ? query.OrderBy(w => DateTime.TryParse(w.ExpectedDeliveryDate, out  date3) ? date3 : DateTime.MaxValue)
//                             : query.OrderByDescending(w => DateTime.TryParse(w.ExpectedDeliveryDate, out  date4) ? date4 : DateTime.MinValue);
//                         break;
//                 }
//             }
//
//             return query.ToList();
//         }
//
//         private DailyPourPlan GenerateDailyPourPlan(List<WorkOrderProgress> workOrderProgress, DateTime date, string color)
//         {
//             var dailyPlan = new DailyPourPlan
//             {
//                 Date = date.ToString("yyyy-MM-dd"),
//                 DayName = date.DayOfWeek.ToString(),
//                 Color = color
//             };
//
//             // Create fresh mold instances for this day
//             var availableMolds = CreateFreshMoldInstances();
//
//             // Process work orders for this day and track which work orders were processed
//             var processedWorkOrders = ProcessWorkOrdersForDay(workOrderProgress, availableMolds, dailyPlan.Date);
//             
//             dailyPlan.AllMolds = availableMolds;
//             dailyPlan.ProcessedWorkOrders = processedWorkOrders;
//
//             // Group by pour categories
//             foreach (var moldGroup in availableMolds.Where(m => m.HasItems).GroupBy(m => m.PourCategory))
//             {
//                 dailyPlan.PourGroups[moldGroup.Key] = moldGroup.ToList();
//             }
//
//             // Generate calculation table
//             var usedMolds = availableMolds.Where(x => x.HasItems).ToList();
//             dailyPlan.CalculationTable = GenerateCalculationTable(usedMolds);
//
//             return dailyPlan;
//         }
//
//         private List<WorkOrderProgress> ProcessWorkOrdersForDay(List<WorkOrderProgress> workOrderProgress, 
//             List<StandardMold> availableMolds, string currentDate)
//         {
//             var processedWorkOrders = new List<WorkOrderProgress>();
//
//             foreach (var workOrder in workOrderProgress.Where(wo => !wo.IsFullyProcessed))
//             {
//                 bool workOrderProcessedToday = false;
//
//                 foreach (var itemProgress in workOrder.ItemProgress.Where(ip => ip.RemainingQuantity > 0))
//                 {
//                     int itemsToProcess = itemProgress.RemainingQuantity;
//                     int processedCount = 0;
//
//                     for (int i = 0; i < itemsToProcess; i++)
//                     {
//                         var slottedItem = new SlottedItem
//                         {
//                             Width = itemProgress.PourWidth,
//                             Length = itemProgress.PourLength,
//                             SourceOrder = workOrder.PurchaseOrder,
//                             LotName = itemProgress.LotName
//                         };
//
//                         if (SlotItemIntoMold(slottedItem, availableMolds))
//                         {
//                             processedCount++;
//                             workOrderProcessedToday = true;
//                         }
//                         else
//                         {
//                             // No more space available for this day
//                             break;
//                         }
//                     }
//
//                     if (processedCount > 0)
//                     {
//                         itemProgress.ProcessedQuantity += processedCount;
//                         
//                         // Track daily processing
//                         if (!itemProgress.ProcessedOnDays.Contains(currentDate))
//                         {
//                             itemProgress.ProcessedOnDays.Add(currentDate);
//                         }
//                         
//                         if (itemProgress.DailyProcessedQuantity.ContainsKey(currentDate))
//                         {
//                             itemProgress.DailyProcessedQuantity[currentDate] += processedCount;
//                         }
//                         else
//                         {
//                             itemProgress.DailyProcessedQuantity[currentDate] = processedCount;
//                         }
//                     }
//                 }
//
//                 if (workOrderProcessedToday && !processedWorkOrders.Contains(workOrder))
//                 {
//                     processedWorkOrders.Add(workOrder);
//                 }
//             }
//
//             return processedWorkOrders;
//         }
//
//         private bool SlotItemIntoMold(SlottedItem item, List<StandardMold> availableMolds)
//         {
//             // Try to find the best fit mold (closest size match)
//             var suitableMolds = availableMolds.Where(m => CanFitInMold(item, m))
//                                             .OrderBy(m => (m.Width * m.Length)) // Prefer smaller molds first
//                                             .ToList();
//
//             foreach (var mold in suitableMolds)
//             {
//                 if (PlaceItemInMold(item, mold))
//                 {
//                     return true;
//                 }
//             }
//             
//             return false; // Could not fit item in any mold
//         }
//
//         private bool CanFitInMold(SlottedItem item, StandardMold mold)
//         {
//             double availableWidth = mold.Width - _config.MarginSize;
//             double availableLength = mold.Length - _config.MarginSize;
//
//             if (item.Width > availableWidth || item.Length > availableLength)
//                 return false;
//
//             return CanFitOnTopSide(item, mold) || CanFitOnBottomSide(item, mold);
//         }
//
//         private bool CanFitOnTopSide(SlottedItem item, StandardMold mold)
//         {
//             if (!mold.TopSideItems.Any())
//                 return true;
//
//             // Check if we can add to existing width groups
//             var widthGroups = mold.TopSideItems.GroupBy(i => Math.Round(i.Width, 1)).ToList();
//
//             foreach (var group in widthGroups)
//             {
//                 if (Math.Abs(group.Key - item.Width) < 0.1) // Same width
//                 {
//                     double usedLength = group.Sum(i => i.Length);
//                     if (usedLength + item.Length <= (mold.Length - _config.MarginSize))
//                         return true;
//                 }
//             }
//
//             // Check if we can start a new width group
//             double totalUsedWidth = widthGroups.Sum(g => g.Key);
//             return (totalUsedWidth + item.Width <= (mold.Width - _config.MarginSize));
//         }
//
//         private bool CanFitOnBottomSide(SlottedItem item, StandardMold mold)
//         {
//             double maxSingleSideWidth = (mold.Width - _config.MarginSize) / 2;
//
//             if (item.Width > maxSingleSideWidth)
//                 return false;
//
//             // Check if top side allows bottom side usage
//             if (mold.TopSideItems.Any())
//             {
//                 double topSideWidth = mold.TopSideItems.GroupBy(i => Math.Round(i.Width, 1)).Sum(g => g.Key);
//                 if (topSideWidth > maxSingleSideWidth)
//                     return false;
//             }
//
//             if (!mold.BottomSideItems.Any())
//                 return true;
//
//             // Check existing bottom side groups
//             var widthGroups = mold.BottomSideItems.GroupBy(i => Math.Round(i.Width, 1)).ToList();
//
//             foreach (var group in widthGroups)
//             {
//                 if (Math.Abs(group.Key - item.Width) < 0.1)
//                 {
//                     double usedLength = group.Sum(i => i.Length);
//                     if (usedLength + item.Length <= (mold.Length - _config.MarginSize))
//                         return true;
//                 }
//             }
//
//             double totalUsedWidth = widthGroups.Sum(g => g.Key);
//             return (totalUsedWidth + item.Width <= maxSingleSideWidth);
//         }
//
//         private bool PlaceItemInMold(SlottedItem item, StandardMold mold)
//         {
//             if (CanFitOnTopSide(item, mold))
//             {
//                 PlaceOnTopSide(item, mold);
//                 return true;
//             }
//             else if (CanFitOnBottomSide(item, mold))
//             {
//                 PlaceOnBottomSide(item, mold);
//                 return true;
//             }
//             return false;
//         }
//
//         private void PlaceOnTopSide(SlottedItem item, StandardMold mold)
//         {
//             if (!mold.TopSideItems.Any())
//             {
//                 item.XPosition = 0;
//                 item.YPosition = 0;
//             }
//             else
//             {
//                 var widthGroups = mold.TopSideItems.GroupBy(i => Math.Round(i.Width, 1)).ToList();
//                 var matchingGroup = widthGroups.FirstOrDefault(g => Math.Abs(g.Key - item.Width) < 0.1);
//
//                 if (matchingGroup != null)
//                 {
//                     // Add to existing width group
//                     item.XPosition = matchingGroup.Sum(i => i.Length);
//                     item.YPosition = matchingGroup.First().YPosition;
//                 }
//                 else
//                 {
//                     // Start new width group
//                     item.XPosition = 0;
//                     item.YPosition = widthGroups.Sum(g => g.Key);
//                 }
//             }
//
//             mold.TopSideItems.Add(item);
//         }
//
//         private void PlaceOnBottomSide(SlottedItem item, StandardMold mold)
//         {
//             if (!mold.BottomSideItems.Any())
//             {
//                 item.XPosition = 0;
//                 item.YPosition = mold.Width - item.Width;
//             }
//             else
//             {
//                 var widthGroups = mold.BottomSideItems.GroupBy(i => Math.Round(i.Width, 1)).ToList();
//                 var matchingGroup = widthGroups.FirstOrDefault(g => Math.Abs(g.Key - item.Width) < 0.1);
//
//                 if (matchingGroup != null)
//                 {
//                     item.XPosition = matchingGroup.Sum(i => i.Length);
//                     item.YPosition = matchingGroup.First().YPosition;
//                 }
//                 else
//                 {
//                     item.XPosition = 0;
//                     item.YPosition = mold.Width - item.Width;
//                 }
//             }
//
//             mold.BottomSideItems.Add(item);
//         }
//
//         private List<TableRow> GenerateCalculationTable(List<StandardMold> usedMolds)
//         {
//             var tableRows = new List<TableRow>();
//
//             foreach (var mold in usedMolds.Where(m => m.HasItems))
//             {
//                 var allItems = mold.AllItems.ToList();
//                 var totalPcs = allItems.Count;
//                 var totalArea = allItems.Sum(i => i.Width * i.Length);
//
//                 // Group items by dimensions for better display
//                 var dimensionGroups = allItems.GroupBy(i => $"{i.Length} X {i.Width}").ToList();
//                 string dimensions = string.Join(", ", dimensionGroups.Select(g => 
//                     g.Count() > 1 ? $"{g.Count()}x ({g.Key})" : g.Key));
//
//                 tableRows.Add(new TableRow
//                 {
//                     MoldSize = $"{mold.Name} ({mold.Width}\" X {mold.Length}\")",
//                     Poured = 1,
//                     PourDate = dimensions,
//                     TotalPcs = totalPcs,
//                     CubicYards = (float)(totalArea / 144), // Convert to square feet
//                     TotalArea = (float)totalArea
//                 });
//             }
//
//             return tableRows;
//         }
//
//         private void CategorizeWorkOrders(MultiDayPourPlan multiDayPlan, List<WorkOrderProgress> workOrderProgress)
//         {
//             multiDayPlan.FullyProcessedOrders = workOrderProgress.Where(w => w.IsFullyProcessed).ToList();
//             multiDayPlan.PartiallyProcessedOrders = workOrderProgress.Where(w => w.IsPartiallyProcessed && !w.IsFullyProcessed).ToList();
//             multiDayPlan.UnprocessedOrders = workOrderProgress.Where(w => !w.IsPartiallyProcessed && !w.IsFullyProcessed).ToList();
//         }
//
//         private void GeneratePlanSummary(MultiDayPourPlan multiDayPlan, List<WorkOrderRequest5> originalWorkOrders, List<string> processedDates)
//         {
//             var summary = multiDayPlan.Summary;
//             
//             summary.TotalWorkOrders = originalWorkOrders.Count;
//             summary.ProcessedWorkOrders = multiDayPlan.FullyProcessedOrders.Count;
//             summary.PartiallyProcessedWorkOrders = multiDayPlan.PartiallyProcessedOrders.Count;
//             summary.UnprocessedWorkOrders = multiDayPlan.UnprocessedOrders.Count;
//             
//             summary.TotalItems = originalWorkOrders.SelectMany(wo => wo.Items).Sum(i => i.Quantity);
//             summary.ProcessedItems = multiDayPlan.DailyPlans.Sum(dp => dp.TotalItemsProcessed);
//             summary.RemainingItems = summary.TotalItems - summary.ProcessedItems;
//             
//             summary.OverallCompletionPercentage = summary.TotalItems > 0 ? 
//                 (double)summary.ProcessedItems / summary.TotalItems * 100 : 0;
//             
//             summary.TotalPourDays = multiDayPlan.DailyPlans.Count(dp => dp.HasItems);
//             summary.PourDates = processedDates;
//         }
//     }
//
//     #region Enhanced PDF Generator
//
//     public class EnhancedPourPlanPdfGenerator
//     {
//         // Enhanced color scheme
//         private readonly BaseColor TITLE_BLUE = new BaseColor(70, 130, 180);
//         private readonly BaseColor LIGHT_BLUE = new BaseColor(173, 216, 230);
//         private readonly BaseColor POUR_CATEGORY_BLUE = new BaseColor(100, 149, 237);
//         private readonly BaseColor YELLOW_LINE = new BaseColor(255, 255, 0);
//         private readonly BaseColor RED_LINE = new BaseColor(220, 20, 60);
//         private readonly BaseColor BLACK_LINE = BaseColor.BLACK;
//         private readonly BaseColor GRAY_FILL = new BaseColor(240, 240, 240);
//         private readonly BaseColor GREEN_FILL = new BaseColor(144, 238, 144);
//         private readonly BaseColor WHITE_FILL = BaseColor.WHITE;
//         private readonly BaseColor MOLD_STROKE = new BaseColor(70, 70, 70);
//         
//         private const float MARGIN = 50f;
//         private const float PAGE_WIDTH = 612f;
//         private const float PAGE_HEIGHT = 792f;
//         private float _scaleFactor = 1.5f;
//
//         private readonly BaseFont _bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
//         private readonly BaseFont _bfBold = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
//
//         public byte[] GenerateMultiDayPourSheet(MultiDayPourPlan multiDayPlan, string pourNumber = "1")
//         {
//             using (var memoryStream = new MemoryStream())
//             {
//                 var document = new Document(PageSize.LETTER, MARGIN, MARGIN, MARGIN, MARGIN);
//                 try
//                 {
//                     PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
//                     document.Open();
//                     PdfContentByte cb = writer.DirectContent;
//
//                     bool isFirstPage = true;
//
//                     foreach (var dailyPlan in multiDayPlan.DailyPlans.Where(dp => dp.HasItems))
//                     {
//                         if (!isFirstPage) document.NewPage();
//                         isFirstPage = false;
//
//                         _scaleFactor = multiDayPlan.Config?.ScaleFactor ?? 1.5f;
//
//                         // Draw overview page for this day
//                         DrawEnhancedHeader(cb, dailyPlan.Date, dailyPlan.Color, $"{dailyPlan.DayName.ToUpper()} POURING PLAN", true);
//                         DrawEnhancedDailyOverview(document, cb, dailyPlan.AllMolds);
//
//                         // Draw pour group pages
//                         foreach (var pourGroup in dailyPlan.PourGroups.OrderBy(g => g.Key))
//                         {
//                             document.NewPage();
//                             DrawEnhancedHeader(cb, dailyPlan.Date, dailyPlan.Color, pourGroup.Key.Replace("P", "POUR "));
//                             DrawEnhancedPourGroupDiagrams(document, cb, pourGroup.Value);
//                             DrawEnhancedCalculationTable(cb, dailyPlan.CalculationTable);
//                             DrawEnhancedFooter(cb);
//                         }
//                     }
//
//                     // Add summary page if requested
//                     if (multiDayPlan.DailyPlans.Any(dp => dp.HasItems))
//                     {
//                         document.NewPage();
//                         DrawSummaryPage(cb, multiDayPlan);
//                     }
//
//                     document.Close();
//                     writer.Close();
//                     return memoryStream.ToArray();
//                 }
//                 catch (Exception ex)
//                 {
//                     throw new Exception($"Error generating PDF: {ex.Message}", ex);
//                 }
//             }
//         }
//
//         private void DrawEnhancedHeader(PdfContentByte cb, string date, string color, string title, bool showTitleHeaderAlone = false)
//         {
//             // Draw main title with professional double-border styling
//             float titleY = PAGE_HEIGHT - 30;
//             DrawEnhancedTitleBox(cb, title, PAGE_WIDTH / 2, titleY, 14, TITLE_BLUE);
//
//             // Draw information boxes in header
//             float infoY = PAGE_HEIGHT - 70;
//
//             if (showTitleHeaderAlone)
//             {
//                 // Date box
//                 DrawInfoBox(cb, "DATE:", date, MARGIN, infoY, 120, 20);
//
//                 // Color box  
//                 DrawInfoBox(cb, "COLOR:", color, MARGIN + 130, infoY, 120, 20);
//
//                 // Notes box
//                 DrawInfoBox(cb, "NOTES:", "", PAGE_WIDTH - MARGIN - 150, infoY, 150, 20);
//             }
//
//             // Draw separator line
//             cb.SetColorStroke(GRAY_FILL);
//             cb.SetLineWidth(1f);
//             cb.MoveTo(MARGIN, PAGE_HEIGHT - 100);
//             cb.LineTo(PAGE_WIDTH - MARGIN, PAGE_HEIGHT - 100);
//             cb.Stroke();
//         }
//
//         private void DrawEnhancedTitleBox(PdfContentByte cb, string text, float centerX, float centerY, float fontSize, BaseColor color)
//         {
//             float textWidth = _bfBold.GetWidthPoint(text, fontSize);
//             float textHeight = fontSize * 1.2f;
//             
//             // Outer border
//             float outerPadding = 8f;
//             float outerX = centerX - textWidth/2 - outerPadding;
//             float outerY = centerY - textHeight/2 - outerPadding;
//             float outerW = textWidth + outerPadding * 2;
//             float outerH = textHeight + outerPadding * 2;
//             
//             // Inner border
//             float innerPadding = 4f;
//             float innerX = centerX - textWidth/2 - innerPadding;
//             float innerY = centerY - textHeight/2 - innerPadding;
//             float innerW = textWidth + innerPadding * 2;
//             float innerH = textHeight + innerPadding * 2;
//
//             cb.SaveState();
//             
//             // Draw outer rectangle with fill
//             cb.SetColorFill(color);
//             cb.SetColorStroke(BLACK_LINE);
//             cb.SetLineWidth(2f);
//             cb.Rectangle(outerX, outerY, outerW, outerH);
//             cb.FillStroke();
//             
//             // Draw inner rectangle
//             cb.SetColorFill(WHITE_FILL);
//             cb.SetLineWidth(1f);
//             cb.Rectangle(innerX, innerY, innerW, innerH);
//             cb.FillStroke();
//             
//             cb.RestoreState();
//
//             // Draw text
//             cb.BeginText();
//             cb.SetFontAndSize(_bfBold, fontSize);
//             cb.SetColorFill(BLACK_LINE);
//             cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, text, centerX, centerY - fontSize/3, 0);
//             cb.EndText();
//         }
//
//         private void DrawInfoBox(PdfContentByte cb, string label, string value, float x, float y, float width, float height)
//         {
//             // Draw box
//             cb.SaveState();
//             cb.SetColorStroke(BLACK_LINE);
//             cb.SetColorFill(WHITE_FILL);
//             cb.SetLineWidth(1f);
//             cb.Rectangle(x, y, width, height);
//             cb.FillStroke();
//             cb.RestoreState();
//
//             // Draw label
//             cb.BeginText();
//             cb.SetFontAndSize(_bfBold, 9);
//             cb.SetColorFill(BLACK_LINE);
//             cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, label, x + 5, y + height/2 - 3, 0);
//             cb.EndText();
//
//             // Draw value
//             if (!string.IsNullOrEmpty(value))
//             {
//                 float labelWidth = _bfBold.GetWidthPoint(label, 9);
//                 cb.BeginText();
//                 cb.SetFontAndSize(_bf, 9);
//                 cb.SetColorFill(BLACK_LINE);
//                 cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, value, x + labelWidth + 10, y + height/2 - 3, 0);
//                 cb.EndText();
//             }
//         }
//
//         private void DrawEnhancedDailyOverview(Document doc, PdfContentByte cb, List<StandardMold> allMolds)
//         {
//             float startY = PAGE_HEIGHT - 130;
//             float leftColumnX = MARGIN;
//             float rightColumnX = PAGE_WIDTH / 2 + 20;
//             
//             // Calculate required space for each column
//             var leftMolds = allMolds.Where(m => "ABCDEF".Contains(m.Name)).OrderBy(m => m.Name).ToList();
//             var rightMolds = allMolds.Where(m => "GHIJKL".Contains(m.Name)).OrderBy(m => m.Name).ToList();
//
//             float leftBoxWidth = (PAGE_WIDTH / 2f) - MARGIN - 30;
//             float rightBoxWidth = (PAGE_WIDTH - rightColumnX - MARGIN);
//             
//             // Draw section titles first
//             DrawSectionTitle(cb, "MOLD POURING PLANS", leftColumnX + leftBoxWidth/2, startY + 30, LIGHT_BLUE);
//             DrawSectionTitle(cb, "MOLD POURING PLANS", rightColumnX + rightBoxWidth/2, startY + 30, LIGHT_BLUE);
//
//             // Calculate and draw group boxes
//             if (leftMolds.Any() || rightMolds.Any())
//             {
//                 float leftBoxHeight = CalculateRequiredHeight(leftMolds);
//                 float rightBoxHeight = CalculateRequiredHeight(rightMolds);
//                 float maxBoxHeight = Math.Max(leftBoxHeight, rightBoxHeight);
//                 
//                 // Draw background boxes
//                 DrawGroupBackgroundBox(cb, leftColumnX - 5, startY + 10, leftBoxWidth, maxBoxHeight + 20, LIGHT_BLUE);
//                 DrawGroupBackgroundBox(cb, rightColumnX - 5, startY + 10, rightBoxWidth, maxBoxHeight + 20, LIGHT_BLUE);
//             }
//
//             // Draw molds within boxes
//             DrawMoldsInColumns(doc, cb, leftMolds, rightMolds, leftColumnX, rightColumnX, startY, 150);
//         }
//
//         private void DrawSectionTitle(PdfContentByte cb, string text, float centerX, float centerY, BaseColor bgColor)
//         {
//             float textWidth = _bfBold.GetWidthPoint(text, 11);
//             float padding = 6f;
//             
//             // Double rectangle styling
//             DrawDoubleRectangleText(cb, text, centerX, centerY, 11, bgColor, BLACK_LINE, padding, padding - 2);
//         }
//
//         private void DrawDoubleRectangleText(PdfContentByte cb, string text, float centerX, float centerY, 
//             float fontSize, BaseColor fillColor, BaseColor strokeColor, float outerPadding, float innerPadding)
//         {
//             float textWidth = _bfBold.GetWidthPoint(text, fontSize);
//             float textHeight = fontSize * 1.2f;
//             
//             // Outer rectangle
//             float outerX = centerX - textWidth/2 - outerPadding;
//             float outerY = centerY - textHeight/2 - outerPadding;
//             float outerW = textWidth + outerPadding * 2;
//             float outerH = textHeight + outerPadding * 2;
//             
//             // Inner rectangle
//             float innerX = centerX - textWidth/2 - innerPadding;
//             float innerY = centerY - textHeight/2 - innerPadding;
//             float innerW = textWidth + innerPadding * 2;
//             float innerH = textHeight + innerPadding * 2;
//
//             cb.SaveState();
//             
//             // Draw outer rectangle
//             cb.SetColorFill(fillColor);
//             cb.SetColorStroke(strokeColor);
//             cb.SetLineWidth(2f);
//             cb.Rectangle(outerX, outerY, outerW, outerH);
//             cb.FillStroke();
//             
//             // Draw inner rectangle
//             cb.SetLineWidth(1f);
//             cb.Rectangle(innerX, innerY, innerW, innerH);
//             cb.Stroke();
//             
//             cb.RestoreState();
//
//             // Draw text
//             cb.BeginText();
//             cb.SetFontAndSize(_bfBold, fontSize);
//             cb.SetColorFill(BLACK_LINE);
//             cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, text, centerX, centerY - fontSize/3, 0);
//             cb.EndText();
//         }
//
//         private void DrawGroupBackgroundBox(PdfContentByte cb, float x, float y, float width, float height, BaseColor color)
//         {
//             cb.SaveState();
//             var gs = new PdfGState { FillOpacity = 0.1f, StrokeOpacity = 0.6f };
//             cb.SetGState(gs);
//             cb.SetColorFill(color);
//             cb.SetColorStroke(color);
//             cb.SetLineWidth(2f);
//             cb.Rectangle(x, y - height, width, height);
//             cb.FillStroke();
//             cb.RestoreState();
//         }
//
//         private float CalculateRequiredHeight(List<StandardMold> molds)
//         {
//             if (!molds.Any()) return 50f;
//             
//             float totalHeight = 30; // Base padding
//             foreach (var mold in molds)
//             {
//                 float moldHeight = (float)(mold.Width * _scaleFactor);
//                 totalHeight += moldHeight + 60; // Mold + spacing + labels
//             }
//             return totalHeight;
//         }
//
//         private void DrawMoldsInColumns(Document doc, PdfContentByte cb, List<StandardMold> leftMolds, 
//             List<StandardMold> rightMolds, float leftColumnX, float rightColumnX, float startY, float minY)
//         {
//             int leftIndex = 0, rightIndex = 0;
//             float currentLeftY = startY;
//             float currentRightY = startY;
//
//             while (leftIndex < leftMolds.Count || rightIndex < rightMolds.Count)
//             {
//                 // Draw left column mold
//                 if (leftIndex < leftMolds.Count)
//                 {
//                     var mold = leftMolds[leftIndex];
//                     float height = DrawEnhancedMoldOverview(cb, leftColumnX, currentLeftY, mold, true);
//                     currentLeftY -= (height + 25);
//                     leftIndex++;
//                 }
//
//                 // Draw right column mold
//                 if (rightIndex < rightMolds.Count)
//                 {
//                     var mold = rightMolds[rightIndex];
//                     float height = DrawEnhancedMoldOverview(cb, rightColumnX, currentRightY, mold, true);
//                     currentRightY -= (height + 25);
//                     rightIndex++;
//                 }
//
//                 // Check if we need a new page
//                 bool needNewPage = (currentLeftY < minY && leftIndex < leftMolds.Count) || 
//                                   (currentRightY < minY && rightIndex < rightMolds.Count);
//
//                 if (needNewPage)
//                 {
//                     doc.NewPage();
//                     currentLeftY = startY;
//                     currentRightY = startY;
//                     
//                     // Redraw section headers and boxes on new page
//                     DrawSectionTitle(cb, "MOLD POURING PLANS", leftColumnX + 100, startY + 30, LIGHT_BLUE);
//                     DrawSectionTitle(cb, "MOLD POURING PLANS", rightColumnX + 100, startY + 30, LIGHT_BLUE);
//                 }
//             }
//         }
//
//         private float DrawEnhancedMoldOverview(PdfContentByte cb, float x, float y, StandardMold mold, bool showDimensions)
//         {
//             // Enhanced mold title with professional styling
//             string moldTitle = $"MOLD NAME - {mold.Name} ({mold.Width}\" x {mold.Length}\")";
//             DrawMoldTitleBox(cb, moldTitle, x, y + 25, LIGHT_BLUE);
//
//             // Pour category in circle
//             string pourCat = mold.PourCategory ?? "P1";
//             DrawPourCategoryCircle(cb, pourCat, x - 25, y, POUR_CATEGORY_BLUE, false);
//
//             // Calculate mold dimensions
//             float moldWidth = (float)(mold.Length * _scaleFactor);
//             float moldHeight = (float)(mold.Width * _scaleFactor);
//
//             // Draw mold container with enhanced styling
//             DrawEnhancedMoldContainer(cb, x, y - moldHeight - 15, moldWidth, moldHeight, showDimensions, mold);
//
//             // Draw items if present
//             if (mold.HasItems)
//             {
//                 DrawEnhancedMoldItems(cb, x, y - 15, mold, moldWidth, moldHeight, false);
//             }
//
//             return moldHeight + 60;
//         }
//
//         private void DrawMoldTitleBox(PdfContentByte cb, string text, float x, float y, BaseColor bgColor)
//         {
//             float textWidth = _bfBold.GetWidthPoint(text, 9);
//             float padding = 4f;
//             
//             cb.SaveState();
//             cb.SetColorFill(bgColor);
//             cb.SetColorStroke(BLACK_LINE);
//             cb.SetLineWidth(1.5f);
//             cb.Rectangle(x, y, textWidth + padding * 2, 18);
//             cb.FillStroke();
//             cb.RestoreState();
//
//             cb.BeginText();
//             cb.SetFontAndSize(_bfBold, 9);
//             cb.SetColorFill(BLACK_LINE);
//             cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, text, x + padding, y + 5, 0);
//             cb.EndText();
//         }
//
//         private void DrawPourCategoryCircle(PdfContentByte cb, string category, float centerX, float centerY, BaseColor color, bool needsCircle = true)
//         {
//             float radius = 12f;
//             
//             cb.SaveState();
//             cb.SetColorFill(WHITE_FILL);
//             cb.SetColorStroke(color);
//             cb.SetLineWidth(2f);
//             if (needsCircle)
//             {
//                 cb.Circle(centerX, centerY, radius);
//             }
//
//             cb.FillStroke();
//             cb.RestoreState();
//
//             cb.BeginText();
//             cb.SetFontAndSize(_bfBold, 10);
//             cb.SetColorFill(color);
//             cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, category, centerX, centerY - 3, 0);
//             cb.EndText();
//         }
//
//         private void DrawEnhancedMoldContainer(PdfContentByte cb, float x, float y, float width, float height, 
//             bool showDimensions, StandardMold mold)
//         {
//             // Draw main mold rectangle with professional styling
//             cb.SaveState();
//             //cb.SetColorStroke(MOLD_STROKE);
//             cb.SetColorStroke(YELLOW_LINE);
//             cb.SetColorFill(WHITE_FILL);
//             //cb.SetLineWidth(2f);
//             cb.SetLineWidth(1f);
//             cb.Rectangle(x, y, width, height);
//             cb.FillStroke();
//             cb.RestoreState();
//
//             if (showDimensions)
//             {
//                 DrawMoldDimensions(cb, x, y, width, height, mold.Length, mold.Width);
//             }
//         }
//
//         private void DrawMoldDimensions(PdfContentByte cb, float x, float y, float width, float height, 
//             double actualLength, double actualWidth)
//         {
//             cb.SetColorStroke(RED_LINE);
//             cb.SetLineWidth(1f);
//
//             // Length dimension (horizontal, above mold)
//             float dimY = y + height + 10;
//             cb.MoveTo(x, dimY);
//             cb.LineTo(x + width, dimY);
//             cb.Stroke();
//
//             // Dimension markers
//             cb.MoveTo(x, y + height);
//             cb.LineTo(x, dimY + 3);
//             cb.MoveTo(x + width, y + height);
//             cb.LineTo(x + width, dimY + 3);
//             cb.Stroke();
//
//             // Dimension text
//             cb.BeginText();
//             cb.SetFontAndSize(_bf, 8);
//             cb.SetColorFill(RED_LINE);
//             cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, $"{actualLength}\"", x + width/2, dimY + 5, 0);
//             cb.EndText();
//
//             // Width dimension (vertical, left side)
//             float dimX = x - 15;
//             cb.MoveTo(dimX, y);
//             cb.LineTo(dimX, y + height);
//             cb.Stroke();
//
//             // Width markers
//             cb.MoveTo(x, y);
//             cb.LineTo(dimX - 3, y);
//             cb.MoveTo(x, y + height);
//             cb.LineTo(dimX - 3, y + height);
//             cb.Stroke();
//
//             // Width text (rotated)
//             cb.BeginText();
//             cb.SetFontAndSize(_bf, 8);
//             cb.SetColorFill(RED_LINE);
//             cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, $"{actualWidth}\"", dimX - 8, y + height/2, 90);
//             cb.EndText();
//         }
//
//         private void DrawEnhancedMoldItems(PdfContentByte cb, float containerX, float containerY, 
//             StandardMold mold, float containerWidth, float containerHeight, bool showItemsTotalLength )
//         {
//             // Draw all items
//             foreach (var item in mold.AllItems)
//             {
//                 float itemX = containerX + (float)(item.XPosition * _scaleFactor);
//                 float itemY = containerY - (float)(item.YPosition * _scaleFactor) - (float)(item.Width * _scaleFactor);
//                 float itemWidth = (float)(item.Length * _scaleFactor);
//                 float itemHeight = (float)(item.Width * _scaleFactor);
//
//                 // Draw item rectangle
//                 cb.SaveState();
//                 cb.SetColorStroke(BLACK_LINE);
//                 cb.SetColorFill(WHITE_FILL);
//                 cb.SetLineWidth(1.5f);
//                 cb.Rectangle(itemX, itemY, itemWidth, itemHeight);
//                 cb.FillStroke();
//                 cb.RestoreState();
//
//                 // Item label
//                 string label = $"{item.Width}\" x {item.Length}\"";
//                 cb.BeginText();
//                 cb.SetFontAndSize(_bf, 7);
//                 cb.SetColorFill(BLACK_LINE);
//                 cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, label, itemX + itemWidth/2, itemY + itemHeight/2 - 2, 0);
//                 cb.EndText();
//             }
//
//             // Draw side totals with arrows
//             if(showItemsTotalLength)
//             DrawSideTotalsWithArrows(cb, containerX, containerY, containerWidth, mold);
//         }
//
//         private void DrawSideTotalsWithArrows(PdfContentByte cb, float containerX, float containerY, 
//             float containerWidth, StandardMold mold)
//         {
//             float moldRightX = containerX + containerWidth;
//             float labelX = moldRightX + 15;
//
//             // Draw totals for top side
//             if (mold.TopSideItems.Any())
//             {
//                 double topSideTotal = mold.TopSideItems.Max(item => item.XPosition + item.Length) + 3;
//                 float topSideY = containerY - 20;
//                 
//                 DrawSideLengthInfo(cb, labelX, topSideY, "SIDE 1 TOTAL LENGTH", "WITH MARGIN", $"= {topSideTotal}\"");
//                 DrawArrowToMold(cb, moldRightX, topSideY, RED_LINE);
//             }
//
//             // Draw totals for bottom side
//             if (mold.BottomSideItems.Any())
//             {
//                 double bottomSideTotal = mold.BottomSideItems.Max(item => item.XPosition + item.Length) + 3;
//                 float bottomSideY = containerY - (float)(mold.Width * _scaleFactor) - 35;
//                 
//                 DrawSideLengthInfo(cb, labelX, bottomSideY, "SIDE 2 TOTAL LENGTH", "WITH MARGIN", $"= {bottomSideTotal}\"");
//                 DrawArrowToMold(cb, moldRightX, bottomSideY, RED_LINE);
//             }
//         }
//
//         private void DrawSideLengthInfo(PdfContentByte cb, float x, float y, string line1, string line2, string total)
//         {
//             cb.BeginText();
//             cb.SetFontAndSize(_bf, 7);
//             cb.SetColorFill(BLACK_LINE);
//             cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, line1, x, y + 5, 0);
//             cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, line2, x, y - 3, 0);
//             cb.EndText();
//
//             cb.BeginText();
//             cb.SetFontAndSize(_bfBold, 8);
//             cb.SetColorFill(RED_LINE);
//             cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, total, x + 55, y - 3, 0);
//             cb.EndText();
//         }
//
//         private void DrawArrowToMold(PdfContentByte cb, float moldRightX, float arrowY, BaseColor color)
//         {
//             cb.SaveState();
//             cb.SetColorStroke(color);
//             cb.SetColorFill(color);
//             cb.SetLineWidth(1.5f);
//
//             // Arrow shaft
//             float shaftLength = 12f;
//             float arrowStart = moldRightX + shaftLength;
//             
//             cb.MoveTo(arrowStart, arrowY);
//             cb.LineTo(moldRightX, arrowY);
//             cb.Stroke();
//
//             // Arrowhead
//             float headSize = 3f;
//             cb.MoveTo(moldRightX, arrowY);
//             cb.LineTo(moldRightX + headSize, arrowY + headSize * 0.6f);
//             cb.LineTo(moldRightX + headSize, arrowY - headSize * 0.6f);
//             cb.ClosePathFillStroke();
//
//             cb.RestoreState();
//         }
//
//         private void DrawEnhancedPourGroupDiagrams(Document doc, PdfContentByte cb, List<StandardMold> molds)
//         {
//             float currentY = PAGE_HEIGHT - 130;
//             float minY = 200;
//
//             foreach (var mold in molds.Where(m => m.HasItems))
//             {
//                 if (currentY < minY)
//                 {
//                     doc.NewPage();
//                     currentY = PAGE_HEIGHT - 130;
//                 }
//
//                 float height = DrawDetailedMoldDiagram(cb, MARGIN, currentY, mold);
//                 currentY -= (height + 40);
//             }
//         }
//
//         private float DrawDetailedMoldDiagram(PdfContentByte cb, float x, float y, StandardMold mold)
//         {
//             // Draw detailed mold with enhanced styling similar to your image
//             string moldTitle = $"MOLD NAME - {mold.Name} ({mold.Width}\" X {mold.Length}\")";
//             DrawEnhancedMoldTitleBox(cb, moldTitle, x, y + 25);
//
//             // Pour category circle
//             DrawPourCategoryCircle(cb, mold.PourCategory, x - 25, y, POUR_CATEGORY_BLUE);
//
//             // Calculate dimensions
//             float moldWidth = (float)(mold.Length * _scaleFactor);
//             float moldHeight = (float)(mold.Width * _scaleFactor);
//
//             // Draw the mold container
//             DrawEnhancedMoldContainer(cb, x, y - moldHeight - 15, moldWidth, moldHeight, false, mold);
//
//             // Draw items with detailed information
//             if (mold.HasItems)
//             {
//                 DrawDetailedMoldItems(cb, x, y - 15, mold, moldWidth, moldHeight);
//             }
//
//             return moldHeight + 70;
//         }
//
//         private void DrawEnhancedMoldTitleBox(PdfContentByte cb, string text, float x, float y)
//         {
//             float textWidth = _bfBold.GetWidthPoint(text, 10);
//             float boxWidth = textWidth + 12;
//             float boxHeight = 22;
//
//             // Red border box matching your image style
//             cb.SaveState();
//             cb.SetColorStroke(RED_LINE);
//             cb.SetColorFill(WHITE_FILL);
//             cb.SetLineWidth(2f);
//             cb.Rectangle(x, y, boxWidth, boxHeight);
//             cb.FillStroke();
//             cb.RestoreState();
//
//             // Text
//             cb.BeginText();
//             cb.SetFontAndSize(_bfBold, 10);
//             cb.SetColorFill(RED_LINE);
//             cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, text, x + 6, y + 6, 0);
//             cb.EndText();
//         }
//
//         private void DrawDetailedMoldItems(PdfContentByte cb, float containerX, float containerY, 
//             StandardMold mold, float containerWidth, float containerHeight)
//         {
//             // Draw items similar to your reference image
//             foreach (var item in mold.AllItems)
//             {
//                 float itemX = containerX + (float)(item.XPosition * _scaleFactor);
//                 float itemY = containerY - (float)(item.YPosition * _scaleFactor) - (float)(item.Width * _scaleFactor);
//                 float itemWidth = (float)(item.Length * _scaleFactor);
//                 float itemHeight = (float)(item.Width * _scaleFactor);
//
//                 // Draw item with black border
//                 cb.SaveState();
//                 cb.SetColorStroke(BLACK_LINE);
//                 cb.SetColorFill(WHITE_FILL);
//                 cb.SetLineWidth(1.5f);
//                 cb.Rectangle(itemX, itemY, itemWidth, itemHeight);
//                 cb.FillStroke();
//                 cb.RestoreState();
//
//                 // Item dimensions label
//                 string label = $"{item.Width}\" x {item.Length}\"";
//                 cb.BeginText();
//                 cb.SetFontAndSize(_bf, 8);
//                 cb.SetColorFill(BLACK_LINE);
//                 cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, label, itemX + itemWidth/2, itemY + itemHeight/2 - 2, 0);
//                 cb.EndText();
//             }
//
//             // Draw side totals with arrows exactly like your image
//             DrawDetailedSideTotals(cb, containerX, containerY, containerWidth, mold);
//         }
//
//         private void DrawDetailedSideTotals(PdfContentByte cb, float containerX, float containerY, 
//             float containerWidth, StandardMold mold)
//         {
//             float moldRightX = containerX + containerWidth;
//             float labelX = moldRightX + 20;
//
//             int sideCounter = 1;
//             
//             // Process each side with items
//             if (mold.TopSideItems.Any())
//             {
//                 double sideTotal = mold.TopSideItems.Max(item => item.XPosition + item.Length) + 3;
//                 float sideY = containerY - 25;
//                 
//                 DrawSideTotalLabel(cb, labelX, sideY, sideCounter, sideTotal);
//                 DrawDetailedArrow(cb, moldRightX, sideY, RED_LINE);
//                 sideCounter++;
//             }
//
//             if (mold.BottomSideItems.Any())
//             {
//                 double sideTotal = mold.BottomSideItems.Max(item => item.XPosition + item.Length) + 3;
//                 float sideY = containerY - (float)(mold.Width * _scaleFactor) - 40;
//                 
//                 DrawSideTotalLabel(cb, labelX, sideY, sideCounter, sideTotal);
//                 DrawDetailedArrow(cb, moldRightX, sideY, RED_LINE);
//             }
//         }
//
//         private void DrawSideTotalLabel(PdfContentByte cb, float x, float y, int sideNumber, double total)
//         {
//             // Draw the label exactly like in your image
//             cb.BeginText();
//             cb.SetFontAndSize(_bf, 7);
//             cb.SetColorFill(BLACK_LINE);
//             cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, $"SIDE {sideNumber} TOTAL LENGTH", x, y + 4, 0);
//             cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, "WITH MARGIN", x, y - 4, 0);
//             cb.EndText();
//
//             cb.BeginText();
//             cb.SetFontAndSize(_bfBold, 9);
//             cb.SetColorFill(RED_LINE);
//             cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, $"= {total:F0}\"", x + 60, y - 4, 0);
//             cb.EndText();
//         }
//
//         private void DrawDetailedArrow(PdfContentByte cb, float moldRightX, float arrowY, BaseColor color)
//         {
//             cb.SaveState();
//             cb.SetColorStroke(color);
//             cb.SetColorFill(color);
//             cb.SetLineWidth(2f);
//
//             // Horizontal line extending from mold
//             float lineLength = 15f;
//             cb.MoveTo(moldRightX, arrowY);
//             cb.LineTo(moldRightX + lineLength, arrowY);
//             cb.Stroke();
//
//             // Arrow pointing towards mold
//             float headSize = 4f;
//             cb.MoveTo(moldRightX, arrowY);
//             cb.LineTo(moldRightX + headSize, arrowY + headSize * 0.7f);
//             cb.LineTo(moldRightX + headSize, arrowY - headSize * 0.7f);
//             cb.ClosePathFillStroke();
//
//             cb.RestoreState();
//         }
//
//         private void DrawEnhancedCalculationTable(PdfContentByte cb, List<TableRow> tableData)
//         {
//             float tableStartY = 280;
//             
//             // Table headers exactly like your image
//             string[] headers = {
//                 "MOLD SIZE", "NO OF POURED", "POURED SIZE (INCHES)", "NO OF PCS", 
//                 "POURED VLM (INCHES)", "NO OF PCS", "POUREABLE (INCHES)", "VLM (1 DAY MAXIMUM)",
//                 "TOTAL REQUIRING VOL/AREA", "TOTAL AREA REQUIRING (SQFT)"
//             };
//             
//             float[] columnWidths = { 70, 45, 80, 40, 70, 40, 70, 65, 80, 90 };
//             float totalWidth = columnWidths.Sum();
//             float startX = MARGIN;
//
//             // Draw table header with enhanced styling
//             DrawTableHeader(cb, startX, tableStartY, headers, columnWidths);
//
//             // Draw data rows
//             float currentY = tableStartY - 25;
//             float totalCubicYards = 0;
//             float totalArea = 0;
//
//             foreach (var row in tableData)
//             {
//                 DrawTableDataRow(cb, startX, currentY, row, columnWidths);
//                 totalCubicYards += row.CubicYards;
//                 totalArea += row.TotalArea;
//                 currentY -= 20;
//             }
//
//             // Draw totals row
//             DrawTableTotalsRow(cb, startX, currentY, columnWidths, totalCubicYards, totalArea);
//         }
//
//         private void DrawTableHeader(PdfContentByte cb, float startX, float y, string[] headers, float[] columnWidths)
//         {
//             float currentX = startX;
//             
//             for (int i = 0; i < headers.Length; i++)
//             {
//                 // Different colors for different sections
//                 BaseColor headerColor = i >= 8 ? GREEN_FILL : GRAY_FILL;
//                 
//                 // Draw header cell
//                 cb.SaveState();
//                 cb.SetColorFill(headerColor);
//                 cb.SetColorStroke(BLACK_LINE);
//                 cb.SetLineWidth(1.5f);
//                 cb.Rectangle(currentX, y, columnWidths[i], 25);
//                 cb.FillStroke();
//                 cb.RestoreState();
//
//                 // Header text with word wrapping
//                 DrawWrappedText(cb, headers[i], currentX + 2, y + 2, columnWidths[i] - 4, 23, 7, _bfBold);
//                 currentX += columnWidths[i];
//             }
//         }
//
//         private void DrawTableDataRow(PdfContentByte cb, float startX, float y, TableRow row, float[] columnWidths)
//         {
//             float currentX = startX;
//             
//             string[] rowData = {
//                 row.MoldSize,
//                 row.Poured.ToString(),
//                 row.PourDate,
//                 row.TotalPcs.ToString(),
//                 row.CubicYards.ToString("F1"),
//                 row.TotalPcs.ToString(),
//                 row.CubicYards.ToString("F1"),
//                 "1",
//                 row.CubicYards.ToString("F0"),
//                 row.TotalArea.ToString("F0")
//             };
//
//             for (int i = 0; i < rowData.Length; i++)
//             {
//                 BaseColor cellColor = i >= 8 ? GREEN_FILL : WHITE_FILL;
//                 
//                 // Draw cell
//                 cb.SaveState();
//                 cb.SetColorFill(cellColor);
//                 cb.SetColorStroke(BLACK_LINE);
//                 cb.SetLineWidth(1f);
//                 cb.Rectangle(currentX, y, columnWidths[i], 20);
//                 cb.FillStroke();
//                 cb.RestoreState();
//
//                 // Cell text
//                 cb.BeginText();
//                 cb.SetFontAndSize(_bf, 8);
//                 cb.SetColorFill(BLACK_LINE);
//                 cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, rowData[i], 
//                     currentX + columnWidths[i]/2, y + 6, 0);
//                 cb.EndText();
//
//                 currentX += columnWidths[i];
//             }
//         }
//
//         private void DrawTableTotalsRow(PdfContentByte cb, float startX, float y, float[] columnWidths, 
//             float totalCubicYards, float totalArea)
//         {
//             float currentX = startX;
//             
//             for (int i = 0; i < columnWidths.Length; i++)
//             {
//                 BaseColor cellColor = i >= 8 ? GREEN_FILL : GRAY_FILL;
//                 
//                 // Draw cell with thicker border for totals
//                 cb.SaveState();
//                 cb.SetColorFill(cellColor);
//                 cb.SetColorStroke(BLACK_LINE);
//                 cb.SetLineWidth(2f);
//                 cb.Rectangle(currentX, y, columnWidths[i], 20);
//                 cb.FillStroke();
//                 cb.RestoreState();
//
//                 // Total values
//                 string cellText = "";
//                 if (i == 0) cellText = "TOTAL";
//                 else if (i == 8) cellText = totalCubicYards.ToString("F0");
//                 else if (i == 9) cellText = totalArea.ToString("F0");
//
//                 if (!string.IsNullOrEmpty(cellText))
//                 {
//                     cb.BeginText();
//                     cb.SetFontAndSize(_bfBold, 8);
//                     cb.SetColorFill(BLACK_LINE);
//                     cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, cellText, 
//                         currentX + columnWidths[i]/2, y + 6, 0);
//                     cb.EndText();
//                 }
//
//                 currentX += columnWidths[i];
//             }
//         }
//
//         private void DrawWrappedText(PdfContentByte cb, string text, float x, float y, float width, float height, 
//             float fontSize, BaseFont font)
//         {
//             var ct = new ColumnText(cb);
//             var phrase = new Phrase(text, new Font(font, fontSize, Font.BOLD));
//             ct.SetSimpleColumn(phrase, x, y, x + width, y + height, fontSize * 1.2f, Element.ALIGN_CENTER);
//             ct.Go();
//         }
//
//         private void DrawEnhancedFooter(PdfContentByte cb)
//         {
//             float footerY = 80;
//             
//             // Footer information boxes
//             DrawFooterInfoBox(cb, "TOTAL AREA FOR CONCRETE MIX (SQUARE INCH):", "", MARGIN, footerY + 30);
//             DrawFooterInfoBox(cb, "NUMBER OF BAGS REQUIRED:", "", MARGIN, footerY + 10);
//             
//             // Information text
//             string bagInfo = "1 BAG = 1675 SQ. INCH, 2 BAGS = 3350 SQ. INCH, 3 BAGS = 5025 SQ. INCH, 4 BAGS = 6700 SQ. INCH, 5 BAGS = 8375 SQ. INCH";
//             DrawFooterText(cb, bagInfo, MARGIN, footerY - 10);
//
//             // Total calculation box
//             DrawTotalCalculationBox(cb, PAGE_WIDTH - MARGIN - 100, footerY);
//         }
//
//         private void DrawFooterInfoBox(PdfContentByte cb, string label, string value, float x, float y)
//         {
//             float boxWidth = 200;
//             float boxHeight = 15;
//             
//             cb.SaveState();
//             cb.SetColorStroke(BLACK_LINE);
//             cb.SetColorFill(WHITE_FILL);
//             cb.SetLineWidth(1f);
//             cb.Rectangle(x, y, boxWidth, boxHeight);
//             cb.FillStroke();
//             cb.RestoreState();
//
//             cb.BeginText();
//             cb.SetFontAndSize(_bfBold, 8);
//             cb.SetColorFill(BLACK_LINE);
//             cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, label, x + 5, y + 4, 0);
//             cb.EndText();
//         }
//
//         private void DrawFooterText(PdfContentByte cb, string text, float x, float y)
//         {
//             cb.BeginText();
//             cb.SetFontAndSize(_bf, 7);
//             cb.SetColorFill(BLACK_LINE);
//             cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, text, x, y, 0);
//             cb.EndText();
//         }
//
//         private void DrawTotalCalculationBox(PdfContentByte cb, float x, float y)
//         {
//             float boxWidth = 90;
//             float boxHeight = 40;
//             
//             // Blue background box with red border
//             cb.SaveState();
//             cb.SetColorFill(LIGHT_BLUE);
//             cb.SetColorStroke(RED_LINE);
//             cb.SetLineWidth(2f);
//             cb.Rectangle(x, y, boxWidth, boxHeight);
//             cb.FillStroke();
//             cb.RestoreState();
//
//             // "TOTAL" text
//             cb.BeginText();
//             cb.SetFontAndSize(_bfBold, 12);
//             cb.SetColorFill(BLACK_LINE);
//             cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, "TOTAL", x + boxWidth/2, y + boxHeight/2 - 3, 0);
//             cb.EndText();
//         }
//
//         private void DrawSummaryPage(PdfContentByte cb, MultiDayPourPlan multiDayPlan)
//         {
//             // Summary page header
//             DrawEnhancedTitleBox(cb, "MULTI-DAY POURING PLAN SUMMARY", PAGE_WIDTH/2, PAGE_HEIGHT - 50, 16, TITLE_BLUE);
//             
//             float currentY = PAGE_HEIGHT - 120;
//             var summary = multiDayPlan.Summary;
//
//             // Summary statistics
//             DrawSummarySection(cb, "WORK ORDER SUMMARY", MARGIN, currentY, new Dictionary<string, string>
//             {
//                 ["Total Work Orders"] = summary.TotalWorkOrders.ToString(),
//                 ["Fully Processed"] = summary.ProcessedWorkOrders.ToString(),
//                 ["Partially Processed"] = summary.PartiallyProcessedWorkOrders.ToString(),
//                 ["Unprocessed"] = summary.UnprocessedWorkOrders.ToString(),
//                 ["Overall Completion"] = $"{summary.OverallCompletionPercentage:F1}%"
//             });
//
//             currentY -= 150;
//
//             DrawSummarySection(cb, "ITEM SUMMARY", MARGIN, currentY, new Dictionary<string, string>
//             {
//                 ["Total Items"] = summary.TotalItems.ToString(),
//                 ["Processed Items"] = summary.ProcessedItems.ToString(),
//                 ["Remaining Items"] = summary.RemainingItems.ToString(),
//                 ["Total Pour Days"] = summary.TotalPourDays.ToString()
//             });
//
//             currentY -= 150;
//
//             // Unprocessed items details
//             if (multiDayPlan.UnprocessedOrders.Any())
//             {
//                 DrawUnprocessedOrdersSection(cb, MARGIN, currentY, multiDayPlan.UnprocessedOrders);
//             }
//         }
//
//         private void DrawSummarySection(PdfContentByte cb, string title, float x, float y, Dictionary<string, string> data)
//         {
//             // Section title
//             DrawSectionTitle(cb, title, x + 150, y + 20, LIGHT_BLUE);
//             
//             float currentY = y - 10;
//             foreach (var item in data)
//             {
//                 cb.BeginText();
//                 cb.SetFontAndSize(_bfBold, 10);
//                 cb.SetColorFill(BLACK_LINE);
//                 cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, $"{item.Key}:", x, currentY, 0);
//                 cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, item.Value, x + 150, currentY, 0);
//                 cb.EndText();
//                 currentY -= 20;
//             }
//         }
//
//         private void DrawUnprocessedOrdersSection(PdfContentByte cb, float x, float y, List<WorkOrderProgress> unprocessedOrders)
//         {
//             DrawSectionTitle(cb, "UNPROCESSED WORK ORDERS", x + 150, y + 20, RED_LINE);
//             
//             float currentY = y - 10;
//             foreach (var order in unprocessedOrders.Take(10)) // Show first 10
//             {
//                 string orderInfo = $"{order.PurchaseOrder} - {order.TotalRemainingItems} items remaining";
//                 cb.BeginText();
//                 cb.SetFontAndSize(_bf, 9);
//                 cb.SetColorFill(BLACK_LINE);
//                 cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, orderInfo, x, currentY, 0);
//                 cb.EndText();
//                 currentY -= 15;
//             }
//
//             if (unprocessedOrders.Count > 10)
//             {
//                 cb.BeginText();
//                 cb.SetFontAndSize(_bf, 9);
//                 cb.SetColorFill(GRAY_FILL);
//                 cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, $"... and {unprocessedOrders.Count - 10} more", x, currentY, 0);
//                 cb.EndText();
//             }
//         }
//     }
//
//     #endregion
//
//     #region Enhanced Service Integration
//
//     public class EnhancedPouringPlanService
//     {
//         private readonly EnhancedPouringPlanGenerator _planGenerator;
//         private readonly EnhancedPourPlanPdfGenerator _pdfGenerator;
//
//         public EnhancedPouringPlanService(PouringPlanConfig config = null)
//         {
//             _planGenerator = new EnhancedPouringPlanGenerator(config);
//             _pdfGenerator = new EnhancedPourPlanPdfGenerator();
//         }
//
//         /// <summary>
//         /// Generates a complete multi-day pour plan PDF
//         /// </summary>
//         public byte[] GenerateMultiDayPourPlan(List<WorkOrderRequest5> workOrders, DateTime startDate, 
//             string color, string pourNumber = "1")
//         {
//             var multiDayPlan = _planGenerator.GenerateMultiDayPourPlan(workOrders, startDate, color);
//             return _pdfGenerator.GenerateMultiDayPourSheet(multiDayPlan, pourNumber);
//         }
//
//         /// <summary>
//         /// Gets the planning data without generating PDF - useful for API responses or further processing
//         /// </summary>
//         public MultiDayPourPlan GenerateMultiDayPourPlanData(List<WorkOrderRequest5> workOrders, 
//             DateTime startDate, string color)
//         {
//             return _planGenerator.GenerateMultiDayPourPlan(workOrders, startDate, color);
//         }
//
//         /// <summary>
//         /// Gets detailed progress information for SharePoint integration
//         /// </summary>
//         public PourPlanProgress GetPourPlanProgress(List<WorkOrderRequest5> workOrders, 
//             DateTime startDate, string color)
//         {
//             var multiDayPlan = _planGenerator.GenerateMultiDayPourPlan(workOrders, startDate, color);
//             
//             return new PourPlanProgress
//             {
//                 Summary = multiDayPlan.Summary,
//                 UnprocessedOrders = multiDayPlan.UnprocessedOrders,
//                 PartiallyProcessedOrders = multiDayPlan.PartiallyProcessedOrders,
//                 FullyProcessedOrders = multiDayPlan.FullyProcessedOrders,
//                 DailyBreakdown = multiDayPlan.DailyPlans.Select(dp => new DailyProgress
//                 {
//                     Date = dp.Date,
//                     DayName = dp.DayName,
//                     ItemsProcessed = dp.TotalItemsProcessed,
//                     CubicYards = dp.TotalCubicYards,
//                     TotalArea = dp.TotalArea,
//                     MoldsUsed = dp.AllMolds.Count(m => m.HasItems),
//                     ProcessedWorkOrders = dp.ProcessedWorkOrders.Select(wo => new WorkOrderSummary
//                     {
//                         PurchaseOrder = wo.PurchaseOrder,
//                         Company = wo.Company,
//                         ItemsProcessedToday = wo.ItemProgress
//                             .Where(ip => ip.DailyProcessedQuantity.ContainsKey(dp.Date))
//                             .Sum(ip => ip.DailyProcessedQuantity[dp.Date])
//                     }).ToList()
//                 }).ToList()
//             };
//         }
//
//         /// <summary>
//         /// Updates work order progress for SharePoint tracking
//         /// </summary>
//         public List<WorkOrderUpdateRecord> GenerateSharePointUpdateRecords(List<WorkOrderRequest5> workOrders, 
//             DateTime startDate, string color)
//         {
//             var multiDayPlan = _planGenerator.GenerateMultiDayPourPlan(workOrders, startDate, color);
//             var updateRecords = new List<WorkOrderUpdateRecord>();
//
//             foreach (var processedOrder in multiDayPlan.FullyProcessedOrders.Concat(multiDayPlan.PartiallyProcessedOrders))
//             {
//                 updateRecords.Add(new WorkOrderUpdateRecord
//                 {
//                     PurchaseOrder = processedOrder.PurchaseOrder,
//                     ProcessingStatus = processedOrder.IsFullyProcessed ? "Completed" : "In Progress",
//                     CompletionPercentage = processedOrder.CompletionPercentage,
//                     LastProcessedDate = processedOrder.ItemProgress
//                         .SelectMany(ip => ip.ProcessedOnDays)
//                         .DefaultIfEmpty()
//                         .Max(),
//                     RemainingItems = processedOrder.TotalRemainingItems,
//                     ProcessedItems = processedOrder.TotalOriginalItems - processedOrder.TotalRemainingItems,
//                     ItemDetails = processedOrder.ItemProgress.Select(ip => new ItemUpdateRecord
//                     {
//                         LotName = ip.LotName,
//                         OriginalQuantity = ip.OriginalQuantity,
//                         ProcessedQuantity = ip.ProcessedQuantity,
//                         RemainingQuantity = ip.RemainingQuantity,
//                         ProcessedOnDays = ip.ProcessedOnDays,
//                         DailyQuantities = ip.DailyProcessedQuantity
//                     }).ToList()
//                 });
//             }
//
//             return updateRecords;
//         }
//
//         /// <summary>
//         /// Generate sample work orders for testing
//         /// </summary>
//         public static List<WorkOrderRequest5> GetEnhancedSampleWorkOrders()
//         {
//             return new List<WorkOrderRequest5>
//             {
//                 new WorkOrderRequest5
//                 {
//                     OrderDate = DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd"),
//                     PurchaseOrder = "WO-2025-001",
//                     Company = "ABC Construction Ltd",
//                     Priority = 1,
//                     ExpectedDeliveryDate = DateTime.Now.AddDays(5).ToString("yyyy-MM-dd"),
//                     Items = new List<Order>
//                     {
//                         new Order { LotName = "ABC-A1", Quantity = 8, PourWidth = 20, PourLength = 22, Color = "Gray", Type = "Standard" },
//                         new Order { LotName = "ABC-A2", Quantity = 4, PourWidth = 34, PourLength = 34, Color = "Gray", Type = "Large" },
//                         new Order { LotName = "ABC-A3", Quantity = 6, PourWidth = 18, PourLength = 20, Color = "Gray", Type = "Small" },
//                         new Order { LotName = "ABC-B1", Quantity = 3, PourWidth = 24, PourLength = 50, Color = "Gray", Type = "Medium" }
//                     }
//                 },
//                 new WorkOrderRequest5
//                 {
//                     OrderDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"),
//                     PurchaseOrder = "WO-2025-002",
//                     Company = "XYZ Developers Inc",
//                     Priority = 2,
//                     ExpectedDeliveryDate = DateTime.Now.AddDays(7).ToString("yyyy-MM-dd"),
//                     Items = new List<Order>
//                     {
//                         new Order { LotName = "XYZ-C1", Quantity = 5, PourWidth = 22, PourLength = 48, Color = "Gray", Type = "Standard" },
//                         new Order { LotName = "XYZ-C2", Quantity = 7, PourWidth = 26, PourLength = 60, Color = "Gray", Type = "Long" },
//                         new Order { LotName = "XYZ-D1", Quantity = 3, PourWidth = 30, PourLength = 40, Color = "Gray", Type = "Wide" }
//                     }
//                 },
//                 new WorkOrderRequest5
//                 {
//                     OrderDate = DateTime.Now.ToString("yyyy-MM-dd"),
//                     PurchaseOrder = "WO-2025-003",
//                     Company = "DEF Infrastructure",
//                     Priority = 3,
//                     ExpectedDeliveryDate = DateTime.Now.AddDays(10).ToString("yyyy-MM-dd"),
//                     Items = new List<Order>
//                     {
//                         new Order { LotName = "DEF-E1", Quantity = 10, PourWidth = 24, PourLength = 36, Color = "Gray", Type = "Standard" },
//                         new Order { LotName = "DEF-E2", Quantity = 4, PourWidth = 28, PourLength = 55, Color = "Gray", Type = "Custom" },
//                         new Order { LotName = "DEF-F1", Quantity = 6, PourWidth = 20, PourLength = 30, Color = "Gray", Type = "Small" }
//                     }
//                 }
//             };
//         }
//     }
//
//     #region Progress Tracking Models
//
//     public class PourPlanProgress
//     {
//         public PourPlanSummary Summary { get; set; }
//         public List<WorkOrderProgress> UnprocessedOrders { get; set; }
//         public List<WorkOrderProgress> PartiallyProcessedOrders { get; set; }
//         public List<WorkOrderProgress> FullyProcessedOrders { get; set; }
//         public List<DailyProgress> DailyBreakdown { get; set; }
//     }
//
//     public class DailyProgress
//     {
//         public string Date { get; set; }
//         public string DayName { get; set; }
//         public int ItemsProcessed { get; set; }
//         public float CubicYards { get; set; }
//         public float TotalArea { get; set; }
//         public int MoldsUsed { get; set; }
//         public List<WorkOrderSummary> ProcessedWorkOrders { get; set; }
//     }
//
//     public class WorkOrderSummary
//     {
//         public string PurchaseOrder { get; set; }
//         public string Company { get; set; }
//         public int ItemsProcessedToday { get; set; }
//     }
//
//     public class WorkOrderUpdateRecord
//     {
//         public string PurchaseOrder { get; set; }
//         public string ProcessingStatus { get; set; }
//         public double CompletionPercentage { get; set; }
//         public string LastProcessedDate { get; set; }
//         public int RemainingItems { get; set; }
//         public int ProcessedItems { get; set; }
//         public List<ItemUpdateRecord> ItemDetails { get; set; }
//     }
//
//     public class ItemUpdateRecord
//     {
//         public string LotName { get; set; }
//         public int OriginalQuantity { get; set; }
//         public int ProcessedQuantity { get; set; }
//         public int RemainingQuantity { get; set; }
//         public List<string> ProcessedOnDays { get; set; }
//         public Dictionary<string, int> DailyQuantities { get; set; }
//     }
//
//     #endregion
//
//     #endregion
// }