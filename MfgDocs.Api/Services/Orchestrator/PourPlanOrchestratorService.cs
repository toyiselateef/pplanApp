using MfgDocs.Api.Data;
using MfgDocs.Api.Services.Generators;
using MfgDocs.Api.Services.Others;

namespace MfgDocs.Api.Services.Orchestrator;

/// <summary>
/// Orchestrates the complete pour planning workflow from SharePoint to PDF generation and back
/// </summary>
public class PourPlanOrchestrationService
{
    private readonly ISharePointListService _sharePointService;
    private readonly DailyPouringPlanGenerator _planGenerator;
    private readonly DayToDayPourPlanGenerator _sheetGenerator;
    private readonly ILogger<PourPlanOrchestrationService> _logger;
    private readonly IPourPlanHistoryService _historyService; 
    private readonly TrackerDocumentGenerator _trackerGenerator;
    
    public PourPlanOrchestrationService(
        ISharePointListService sharePointService,
        DailyPouringPlanGenerator planGenerator,
        DayToDayPourPlanGenerator sheetGenerator, 
        IPourPlanHistoryService historyService,
        TrackerDocumentGenerator trackerGenerator,
        ILogger<PourPlanOrchestrationService> logger)
    {
        _sharePointService = sharePointService;
        _historyService = historyService;
        _planGenerator = planGenerator;
        _sheetGenerator = sheetGenerator;
        _trackerGenerator = trackerGenerator;  
        _logger = logger;
    }

    /// <summary>
    /// Complete workflow: Fetch from SharePoint → Generate Plan → Create PDF → Update SharePoint
    /// </summary>
    public async Task<PourPlanResult> ExecuteCompletePourPlanWorkflowAsync(
        DateTime startDate,
        string color,
        string pourNumber = "1",
        bool autoUpdateSharePoint = true,
        bool saveToHistory = true)
    {
        try
        {
            _logger.LogInformation("Starting pour plan workflow for date {StartDate}", startDate);

            // Step 1: Fetch unpoured work orders from SharePoint
            var workOrders = await _sharePointService.GetUnpouredWorkOrdersAsync();

            if (!workOrders.Any())
            {
                _logger.LogWarning("No unpoured work orders found");
                return new PourPlanResult
                {
                    Success = false,
                    Message = "No unpoured work orders available for planning"
                };
            }

            _logger.LogInformation("Retrieved {Count} unpoured work orders", workOrders.Count);

            // Step 2: Generate multi-day pour plan
            var multiDayPlan = _planGenerator.GenerateMultiDayPourPlan(workOrders, startDate, color);
            byte[] historyPdfBytes = [];
            // Step 3: Save plan to history (NEW)
            string historyId = null;
            if (saveToHistory)
            {
                historyId = await _historyService.SavePourPlanAsync(multiDayPlan, startDate);
                _logger.LogInformation("Saved pour plan to history with ID {HistoryId}", historyId);
                
                // NEW: Save detailed tracker data
                await SaveTrackerDataAsync(multiDayPlan, startDate);
                
                //
                historyPdfBytes = _trackerGenerator.GenerateTrackerDocument(multiDayPlan);
                var uploadHistoryResult = await _sharePointService.UploadFileToSharePointAsync(
                    historyPdfBytes,
                    $"PourPlansHistoryDoc_{startDate:yyyyMMdd}.pdf",
                    "PourPlansHistoryDocs",
                    allowReplaceFile: true
                );
                    
                if (uploadHistoryResult.Success)
                {
                    _logger.LogInformation("Uploaded PDF to SharePoint: {FileUrl}", uploadHistoryResult.FileUrl);
                }
            }

            // Step 4: Generate PDF
            var pdfBytes = _sheetGenerator.GenerateMultiDayPourSheet(multiDayPlan, pourNumber);

            // Step 5: Calculate what was poured
            var pourUpdates = CalculatePourUpdates(multiDayPlan);

            // Step 6: Update SharePoint (if enabled)
            if (autoUpdateSharePoint && pourUpdates.Any())
            {
                if (await _sharePointService.UpdatePourProgressAsync(pourUpdates))
                {
                    _logger.LogInformation("Updated SharePoint with {Count} pour progress records", pourUpdates.Count);
                    var uploadResult = await _sharePointService.UploadFileToSharePointAsync(
                        pdfBytes,
                        $"PourPlan_{startDate:yyyyMMdd}.pdf",
                        "PourPlans",
                        allowReplaceFile: true
                    );
                    
                    if (uploadResult.Success)
                    {
                        _logger.LogInformation("Uploaded PDF to SharePoint: {FileUrl}", uploadResult.FileUrl);
                    }
                }
            }

            // Step 7: Generate summary report
            var summary = GenerateWorkflowSummary(multiDayPlan, workOrders);

            return new PourPlanResult
            {
                Success = true,
                PdfDocument = pdfBytes,
                Summary = summary,
                PourUpdates = pourUpdates,
                MultiDayPlan = multiDayPlan,
                Message = $"Pour plan generated successfully for {multiDayPlan.DailyPlans.Count} days"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing pour plan workflow");
            return new PourPlanResult
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    private async Task SaveTrackerDataAsync(MultiDayPourPlan multiDayPlan, DateTime planDate)
    {
        var trackerList = await _sharePointService.GetListByTitleAsync("Pour Plan Tracker");

        foreach (var dailyPlan in multiDayPlan.DailyPlans)
        {
            var dailyDate = DateTime.Parse(dailyPlan.Date);

            foreach (var workOrder in dailyPlan.ProcessedWorkOrders)
            {
                foreach (var item in workOrder.ItemProgress)
                {
                    int plannedForToday = item.DailyProcessedQuantity.ContainsKey(dailyPlan.Date)
                        ? item.DailyProcessedQuantity[dailyPlan.Date]
                        : 0;

                    if (plannedForToday > 0)
                    {
                        // Find which mold this item is in
                        var mold = dailyPlan.AllMolds.FirstOrDefault(m =>
                            m.AllItems.Any(mi => mi.LotName == item.LotName));

                        await _sharePointService.CreateListItemAsync(
                            trackerList.Id,
                            new Dictionary<string, object>
                            {
                                { "PlanDate", planDate.ToString("yyyy-MM-dd") },
                                { "DailyPlanDate", dailyDate.ToString("yyyy-MM-dd") },
                                { "PurchaseOrder", workOrder.PurchaseOrder },
                                { "Company", workOrder.Company },
                                { "LotName", item.LotName },
                                { "ProductType", item.Type },
                                { "Dimensions", $"{item.PourWidth}\" x {item.PourLength}\"" },
                                { "FullOrderQuantity", item.OriginalQuantity },
                                { "PlannedQuantity", plannedForToday },
                                { "ActualPouredQuantity", 0 }, // Builders will update
                                { "RemainingQuantity", item.OriginalQuantity - plannedForToday },
                                { "MoldName", mold?.Name ?? "N/A" },
                                { "Status", "Planned" },
                                { "IsEditable", dailyDate.Date == DateTime.Today },
                                { "Color", item.Color },
                                { "PourCategory", mold?.PourCategory ?? "" }
                            }
                        );
                    }
                }
            }
        }
    }

    /// <summary>
    /// Preview workflow without updating SharePoint
    /// </summary>
    public async Task<PourPlanPreview> PreviewPourPlanAsync(DateTime startDate, string color)
    {
        var workOrders = await _sharePointService.GetUnpouredWorkOrdersAsync();
        var multiDayPlan = _planGenerator.GenerateMultiDayPourPlan(workOrders, startDate, color);

        return new PourPlanPreview
        {
            TotalWorkOrders = workOrders.Count,
            PlannedDays = multiDayPlan.DailyPlans.Count,
            FullyProcessedOrders = multiDayPlan.FullyProcessedOrders.Count,
            PartiallyProcessedOrders = multiDayPlan.PartiallyProcessedOrders.Count,
            UnprocessedOrders = multiDayPlan.UnprocessedOrders.Count,
            DailyBreakdown = multiDayPlan.DailyPlans.Select(dp => new DailyPreview
            {
                Date = dp.Date,
                DayName = dp.DayName,
                MoldsUsed = dp.AllMolds.Count(m => m.HasItems),
                ItemsToProcess = dp.TotalItemsProcessed,
                TotalArea = dp.TotalArea
            }).ToList()
        };
    }

    /// <summary>
    /// Manual update of SharePoint after pour execution
    /// </summary>
    public async Task<bool> UpdateSharePointAfterPourAsync(MultiDayPourPlan multiDayPlan)
    {
        try
        {
            var pourUpdates = CalculatePourUpdates(multiDayPlan);
            await _sharePointService.UpdatePourProgressAsync(pourUpdates);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating SharePoint");
            return false;
        }
    }

    /// <summary>
    /// Get current status of all work orders
    /// </summary>
    public async Task<List<WorkOrderStatusReport>> GetWorkOrderStatusAsync()
    {
        var workOrders = await _sharePointService.GetUnpouredWorkOrdersAsync();

        return workOrders.Select(wo => new WorkOrderStatusReport
        {
            PurchaseOrder = wo.PurchaseOrder,
            Company = wo.Company,
            ExpectedDeliveryDate = wo.ExpectedDeliveryDate,
            Priority = wo.Priority,
            TotalItems = wo.Items.Sum(i => i.Quantity),
            DaysUntilDelivery = CalculateDaysUntilDelivery(wo.ExpectedDeliveryDate),
            Status = DetermineStatus(wo)
        }).OrderBy(r => r.Priority).ThenBy(r => r.DaysUntilDelivery).ToList();
    }
    /// <summary>
    /// Regenerate document for a specific historical date
    /// </summary>
    public async Task<byte[]> RegenerateDocumentForDateAsync(DateTime date, bool includeActualData = true)
    {
        try
        {
            _logger.LogInformation("Regenerating document for date {Date}", date);

            var historicalPlan = await _historyService.GetPourPlanForDateAsync(date);
            
            if (historicalPlan == null)
            {
                throw new Exception($"No pour plan found for {date:yyyy-MM-dd}");
            }

            ActualPourFeedback actualData = null;
            if (includeActualData)
            {
                actualData = await _historyService.GetActualPourDataAsync(date);
            }

            // Generate appropriate document based on whether we have actual data
            if (actualData != null)
            {
                // Generate tracker document showing planned vs actual
                return _trackerGenerator.GenerateTrackerDocument(historicalPlan, actualData);
            }
            else
            {
                // Generate standard pour plan document
                return _sheetGenerator.GenerateMultiDayPourSheet(historicalPlan, "1");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating document for {Date}", date);
            throw;
        }
    }

    /// <summary>
    /// Save worker feedback after pour execution
    /// </summary>
    public async Task<bool> SaveWorkerFeedbackAsync(ActualPourFeedback feedback)
    {
        try
        {
            _logger.LogInformation("Saving worker feedback for date {Date}", feedback.PourDate);

            await _historyService.SaveActualPourFeedbackAsync(feedback.PourDate, feedback);

            // Update SharePoint based on actual results
            var pourUpdates = ConvertFeedbackToPourUpdates(feedback);
            
            if (pourUpdates.Any())
            {
                await _sharePointService.UpdatePourProgressAsync(pourUpdates);
                _logger.LogInformation("Updated SharePoint with {Count} items from feedback", pourUpdates.Count);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving worker feedback");
            return false;
        }
    }

    /// <summary>
    /// Get historical plan with comparison to actual
    /// </summary>
    public async Task<PourPlanComparison> GetPlanComparisonAsync(DateTime date)
    {
        var plannedData = await _historyService.GetPourPlanForDateAsync(date);
        var actualData = await _historyService.GetActualPourDataAsync(date);

        if (plannedData == null)
        {
            throw new Exception($"No plan found for {date:yyyy-MM-dd}");
        }

        return new PourPlanComparison
        {
            Date = date,
            PlannedPlan = plannedData,
            ActualFeedback = actualData,
            HasActualData = actualData != null,
            Variances = actualData != null ? CalculateVariances(plannedData, actualData) : null
        };
    }

    /// <summary>
    /// Get all historical plans
    /// </summary>
    public async Task<List<PourPlanHistoryRecord>> GetAllHistoryAsync()
    {
        return await _historyService.GetAllHistoryAsync();
    }


    #region Private Helper Methods

    private List<PourProgressUpdate> ConvertFeedbackToPourUpdates(ActualPourFeedback feedback)
    {
        var updates = new List<PourProgressUpdate>();

        foreach (var moldFeedback in feedback.MoldFeedbacks.Where(m => m.WasUsed))
        {
            foreach (var item in moldFeedback.Items.Where(i => i.WasPoured))
            {
                var existingUpdate = updates.FirstOrDefault(u =>
                    u.PurchaseOrder == item.PurchaseOrder &&
                    u.LotName == item.LotName);

                if (existingUpdate != null)
                {
                    existingUpdate.QuantityPouredToday++;
                }
                else
                {
                    updates.Add(new PourProgressUpdate
                    {
                        PurchaseOrder = item.PurchaseOrder,
                        LotName = item.LotName,
                        ProductDetails = $"{item.Width}\" x {item.Length}\"",
                        QuantityPouredToday = 1,
                        PourDate = feedback.PourDate.ToString("yyyy-MM-dd")
                    });
                }
            }
        }

        return updates;
    }
    private List<PlanVariance> CalculateVariances(MultiDayPourPlan planned, ActualPourFeedback actual)
    {
        var variances = new List<PlanVariance>();

        foreach (var dailyPlan in planned.DailyPlans)
        {
            foreach (var mold in dailyPlan.AllMolds.Where(m => m.HasItems))
            {
                var moldFeedback = actual.MoldFeedbacks.FirstOrDefault(m => m.MoldName == mold.Name);
                
                var plannedItems = mold.AllItems.Count();
                var actualItems = moldFeedback?.Items.Count(i => i.WasPoured) ?? 0;

                if (plannedItems != actualItems)
                {
                    variances.Add(new PlanVariance
                    {
                        MoldName = mold.Name,
                        PlannedCount = plannedItems,
                        ActualCount = actualItems,
                        Variance = actualItems - plannedItems,
                        Reason = moldFeedback?.Notes ?? "No feedback provided"
                    });
                }
            }
        }

        return variances;
    }
    private List<PourProgressUpdate> CalculatePourUpdates(MultiDayPourPlan multiDayPlan)
    {
        var updates = new List<PourProgressUpdate>();

        foreach (var dailyPlan in multiDayPlan.DailyPlans.Where(dp => dp.HasItems))
        {
            // Extract pour information from each mold's items
            foreach (var mold in dailyPlan.AllMolds.Where(m => m.HasItems))
            {
                foreach (var item in mold.AllItems)
                {
                    var existingUpdate = updates.FirstOrDefault(u =>
                        u.PurchaseOrder == item.SourceOrder &&
                        u.LotName == item.LotName &&
                        u.PourDate == dailyPlan.Date);

                    if (existingUpdate != null)
                    {
                        existingUpdate.QuantityPouredToday++;
                    }
                    else
                    {
                        updates.Add(new PourProgressUpdate
                        {
                            PurchaseOrder = item.SourceOrder,
                            LotName = item.LotName,
                            ProductDetails = $"{item.Width}\" x {item.Length}\"",
                            QuantityPouredToday = 1,
                            PourDate = dailyPlan.Date
                        });
                    }
                }
            }
        }

        return updates;
    }

    private WorkflowSummary GenerateWorkflowSummary(MultiDayPourPlan multiDayPlan,
        List<WorkOrderRequest5> originalWorkOrders)
    {
        var totalOriginalItems = originalWorkOrders.Sum(wo => wo.Items.Sum(i => i.Quantity));
        var totalProcessedItems = multiDayPlan.DailyPlans.Sum(dp => dp.TotalItemsProcessed);

        return new WorkflowSummary
        {
            TotalWorkOrdersInSystem = originalWorkOrders.Count,
            FullyProcessedWorkOrders = multiDayPlan.FullyProcessedOrders.Count,
            PartiallyProcessedWorkOrders = multiDayPlan.PartiallyProcessedOrders.Count,
            UnprocessedWorkOrders = multiDayPlan.UnprocessedOrders.Count,
            TotalItemsInSystem = totalOriginalItems,
            ItemsProcessedInPlan = totalProcessedItems,
            ItemsRemaining = totalOriginalItems - totalProcessedItems,
            CompletionPercentage = totalOriginalItems > 0
                ? (double)totalProcessedItems / totalOriginalItems * 100
                : 0,
            PourDaysRequired = multiDayPlan.DailyPlans.Count(dp => dp.HasItems),
            TotalCubicYards = multiDayPlan.DailyPlans.Sum(dp => dp.TotalCubicYards),
            TotalArea = multiDayPlan.DailyPlans.Sum(dp => dp.TotalArea),
            DailyDetails = multiDayPlan.DailyPlans.Select(dp => new DailySummary
            {
                Date = dp.Date,
                DayName = dp.DayName,
                ItemsProcessed = dp.TotalItemsProcessed,
                CubicYards = dp.TotalCubicYards,
                TotalArea = dp.TotalArea,
                MoldsUsed = dp.AllMolds.Count(m => m.HasItems),
                PourGroups = dp.PourGroups.Count
            }).ToList()
        };
    }

    private int CalculateDaysUntilDelivery(string deliveryDate)
    {
        if (DateTime.TryParse(deliveryDate, out var date))
        {
            return (date - DateTime.Now).Days;
        }

        return int.MaxValue;
    }

    private string DetermineStatus(WorkOrderRequest5 workOrder)
    {
        var daysUntil = CalculateDaysUntilDelivery(workOrder.ExpectedDeliveryDate);

        if (daysUntil < 0) return "OVERDUE";
        if (daysUntil < 3) return "URGENT";
        if (daysUntil < 7) return "HIGH PRIORITY";
        if (daysUntil < 14) return "NORMAL";
        return "LOW PRIORITY";
    }

    #endregion
}

#region Result Classes

public class PourPlanResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public byte[] PdfDocument { get; set; }
    public WorkflowSummary Summary { get; set; }
    public List<PourProgressUpdate> PourUpdates { get; set; }
    public MultiDayPourPlan MultiDayPlan { get; set; }
    public string HistoryId { get; set; }
}

public class PourPlanComparison
{
    public DateTime Date { get; set; }
    public MultiDayPourPlan PlannedPlan { get; set; }
    public ActualPourFeedback ActualFeedback { get; set; }
    public bool HasActualData { get; set; }
    public List<PlanVariance> Variances { get; set; }
}

public class PlanVariance
{
    public string MoldName { get; set; }
    public int PlannedCount { get; set; }
    public int ActualCount { get; set; }
    public int Variance { get; set; }
    public string Reason { get; set; }
}

public class PourPlanPreview
{
    public int TotalWorkOrders { get; set; }
    public int PlannedDays { get; set; }
    public int FullyProcessedOrders { get; set; }
    public int PartiallyProcessedOrders { get; set; }
    public int UnprocessedOrders { get; set; }
    public List<DailyPreview> DailyBreakdown { get; set; }
}

public class DailyPreview
{
    public string Date { get; set; }
    public string DayName { get; set; }
    public int MoldsUsed { get; set; }
    public int ItemsToProcess { get; set; }
    public float TotalArea { get; set; }
}

// public class WorkflowSummary
// {
//     public int TotalWorkOrdersInSystem { get; set; }
//     public int FullyProcessedWorkOrders { get; set; }
//     public int PartiallyProcessedWorkOrders { get; set; }
//     public int UnprocessedWorkOrders { get; set; }
//     public int TotalItemsInSystem { get; set; }
//     public int ItemsProcessedInPlan { get; set; }
//     public int ItemsRemaining { get; set; }
//     public double CompletionPercentage { get; set; }
//     public int PourDaysRequired { get; set; }
//     public float TotalCubicYards { get; set; }
//     public float TotalArea { get; set; }
//     public List<DailySummary> DailyDetails { get; set; }
// }
//
// public class DailySummary
// {
//     public string Date { get; set; }
//     public string DayName { get; set; }
//     public int ItemsProcessed { get; set; }
//     public float CubicYards { get; set; }
//     public float TotalArea { get; set; }
//     public int MoldsUsed { get; set; }
//     public int PourGroups { get; set; }
// }

// public class PourPlanResult
// {
//     public bool Success { get; set; }
//     public string Message { get; set; }
//     public byte[] PdfDocument { get; set; }
//     public WorkflowSummary Summary { get; set; }
//     public List<PourProgressUpdate> PourUpdates { get; set; }
//     public MultiDayPourPlan MultiDayPlan { get; set; }
// }

// public class PourPlanPreview
// {
//     public int TotalWorkOrders { get; set; }
//     public int PlannedDays { get; set; }
//     public int FullyProcessedOrders { get; set; }
//     public int PartiallyProcessedOrders { get; set; }
//     public int UnprocessedOrders { get; set; }
//     public List<DailyPreview> DailyBreakdown { get; set; }
// }

// public class DailyPreview
// {
//     public string Date { get; set; }
//     public string DayName { get; set; }
//     public int MoldsUsed { get; set; }
//     public int ItemsToProcess { get; set; }
//     public float TotalArea { get; set; }
// }

public class WorkflowSummary
{
    public int TotalWorkOrdersInSystem { get; set; }
    public int FullyProcessedWorkOrders { get; set; }
    public int PartiallyProcessedWorkOrders { get; set; }
    public int UnprocessedWorkOrders { get; set; }
    public int TotalItemsInSystem { get; set; }
    public int ItemsProcessedInPlan { get; set; }
    public int ItemsRemaining { get; set; }
    public double CompletionPercentage { get; set; }
    public int PourDaysRequired { get; set; }
    public float TotalCubicYards { get; set; }
    public float TotalArea { get; set; }
    public List<DailySummary> DailyDetails { get; set; }
}

public class DailySummary
{
    public string Date { get; set; }
    public string DayName { get; set; }
    public int ItemsProcessed { get; set; }
    public float CubicYards { get; set; }
    public float TotalArea { get; set; }
    public int MoldsUsed { get; set; }
    public int PourGroups { get; set; }
}

public class WorkOrderStatusReport
{
    public string PurchaseOrder { get; set; }
    public string Company { get; set; }
    public string ExpectedDeliveryDate { get; set; }
    public int Priority { get; set; }
    public int TotalItems { get; set; }
    public int DaysUntilDelivery { get; set; }
    public string Status { get; set; }
}

#endregion

// Extension to DailyPourPlan class
// public partial class DailyPourPlan
// {
//     public int TotalItemsProcessed => AllMolds.Sum(m => m.AllItems.Count);
//     public float TotalCubicYards => CalculationTable?.Sum(t => t.CubicYards) ?? 0;
//     public float TotalArea => CalculationTable?.Sum(t => t.TotalArea) ?? 0;
//     public List<WorkOrderProgress> ProcessedWorkOrders { get; set; } = new();
// }