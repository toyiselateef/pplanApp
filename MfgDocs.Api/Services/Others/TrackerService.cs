using MfgDocs.Api.Data;
using MfgDocs.Api.Models;
 
using Microsoft.Extensions.Logging; 

namespace MfgDocs.Api.Services.Others;
 
 

public interface ITrackerService
{
    Task<List<TrackerItemDto>> GetTrackerDataForDate(DateTime date);
    TrackerSummaryDto CalculateTrackerSummary(List<TrackerItemDto> items); 

    Task UpdateOriginalOrderProgress(
        string purchaseOrder,
        string lotName,
        int actualPoured);
}
public class TrackerService : ITrackerService
{
    private readonly ISharePointListService _sharePointService;
    private readonly IPourPlanHistoryService _historyService;
    private readonly ILogger<TrackerService> _logger;
    // Assuming GetFieldValue<T> is either accessible or moved into a dedicated helper/extension

    public TrackerService(
        ISharePointListService sharePointService,
        IPourPlanHistoryService historyService,
        ILogger<TrackerService> logger)
    {
        _sharePointService = sharePointService;
        _historyService = historyService;
        _logger = logger;
    }

    public async Task<List<TrackerItemDto>> GetTrackerDataForDate(DateTime date)
    {
        var trackerList = await _sharePointService.GetListByTitleAsync("Pour Plan Tracker");
        var items = await _sharePointService.GetFilteredListItemsAsync(
            trackerList.Id,
            $"fields/PlanDate eq '{date:yyyy-MM-dd}'"
        );

        return items.Select(item => new TrackerItemDto
        {
            Id = item.Id,
            PlanDate = GetFieldValue<DateTime>(item.Fields.AdditionalData, "PlanDate"),
            PurchaseOrder = GetFieldValue<string>(item.Fields.AdditionalData, "PurchaseOrder"),
            Company = GetFieldValue<string>(item.Fields.AdditionalData, "Company"),
            LotName = GetFieldValue<string>(item.Fields.AdditionalData, "LotName"),
            ProductType = GetFieldValue<string>(item.Fields.AdditionalData, "ProductType"),
            Dimensions = GetFieldValue<string>(item.Fields.AdditionalData, "Dimensions"),
            FullOrderQuantity = GetFieldValue<int>(item.Fields.AdditionalData, "FullOrderQuantity"),
            PlannedQuantity = GetFieldValue<int>(item.Fields.AdditionalData, "PlannedQuantity"),
            ActualPouredQuantity = GetFieldValue<int>(item.Fields.AdditionalData, "ActualPouredQuantity"),
            RemainingQuantity = GetFieldValue<int>(item.Fields.AdditionalData, "RemainingQuantity"),
            MoldName = GetFieldValue<string>(item.Fields.AdditionalData, "MoldName"),
            Status = GetFieldValue<string>(item.Fields.AdditionalData, "Status"),
            Color = GetFieldValue<string>(item.Fields.AdditionalData, "Color"),
            PourCategory = GetFieldValue<string>(item.Fields.AdditionalData, "PourCategory")
        }).ToList();
    }

    public async Task UpdateOriginalOrderProgress(
        string purchaseOrder, 
        string lotName, 
        int actualPoured)
    {
        var ordersList = await _sharePointService.GetListByTitleAsync("Orders");
        var items = await _sharePointService.GetFilteredListItemsAsync(
            ordersList.Id,
            $"fields/Purchase_x0020_Order eq '{purchaseOrder}' and " +
            $"fields/Lot_x0020_Number eq '{lotName}'"     
        );

        var orderItem = items.FirstOrDefault();
        if (orderItem != null)
        {
            var currentPoured = GetFieldValue<int>(
                orderItem.Fields.AdditionalData, "QuantityPoured");
            
            await _sharePointService.UpdateListItemAsync(
                ordersList.Id,
                orderItem.Id,
                new Dictionary<string, object>
                {
                    { "QuantityPoured", currentPoured + actualPoured }
                }
            );
        }
    }

    public TrackerSummaryDto CalculateTrackerSummary(List<TrackerItemDto> items)
    {
        return new TrackerSummaryDto
        {
            TotalOrders = items.Select(i => i.PurchaseOrder).Distinct().Count(),
            TotalItems = items.Sum(i => i.FullOrderQuantity),
            PlannedItems = items.Sum(i => i.PlannedQuantity),
            CompletedItems = items.Sum(i => i.ActualPouredQuantity),
            RemainingItems = items.Sum(i => i.RemainingQuantity),
            CompletionPercentage = CalculateCompletionPercentage(items),
            MoldsUsed = items.Select(i => i.MoldName).Distinct().Count()
        };
    }

    public double CalculateCompletionPercentage(List<TrackerItemDto> items)
    {
        if (items == null || !items.Any()) return 0;
        
        var planned = items.Sum(i => i.PlannedQuantity);
        var actual = items.Sum(i => i.ActualPouredQuantity);
        
        return planned > 0 ? Math.Round((actual / (double)planned) * 100, 2) : 0;
    }

    public T GetFieldValue<T>(IDictionary<string, object> fields, string fieldName)
    {
        // Same implementation as in SharePointListService
        if (!fields.TryGetValue(fieldName, out var value) || value == null)
            return default(T);

        if (value is System.Text.Json.JsonElement jsonElement)
        {
            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            
            if (targetType == typeof(int))
                return (T)(object)jsonElement.GetInt32();
            if (targetType == typeof(string))
                return (T)(object)jsonElement.GetString();
            if (targetType == typeof(DateTime))
                return (T)(object)jsonElement.GetDateTime();
        }

        return value is T typedValue ? typedValue : default(T);
    }

}


