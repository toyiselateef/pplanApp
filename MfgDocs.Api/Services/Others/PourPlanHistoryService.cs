using System.Text.Json;
using MfgDocs.Api.Data;
using MfgDocs.Api.Services.Generators;

namespace MfgDocs.Api.Services.Others;
 
 
public interface IPourPlanHistoryService
{
    Task<string> SavePourPlanAsync(MultiDayPourPlan plan, DateTime planDate);
    Task<MultiDayPourPlan> GetPourPlanForDateAsync(DateTime date);
    Task SaveActualPourFeedbackAsync(DateTime date, ActualPourFeedback feedback);
    Task<ActualPourFeedback> GetActualPourDataAsync(DateTime date);
    Task<List<PourPlanHistoryRecord>> GetAllHistoryAsync();
    Task UpdatePlanStatusAsync(DateTime date, string status);
}

public class PourPlanHistoryService : IPourPlanHistoryService
{
    private readonly ISharePointListService _sharePointService;
    private readonly ILogger<PourPlanHistoryService> _logger;
    private const string HISTORY_LIST = "Pour Plan History";

    public PourPlanHistoryService(
        ISharePointListService sharePointService,
        ILogger<PourPlanHistoryService> logger)
    {
        _sharePointService = sharePointService;
        _logger = logger;
    }

    public async Task<string> SavePourPlanAsync(MultiDayPourPlan plan, DateTime planDate)
    {
        try
        {
            var planJson = JsonSerializer.Serialize(plan, new JsonSerializerOptions 
            { 
                WriteIndented = false,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
            });

            var fields = new Dictionary<string, object>
            {
                { "Title", $"Pour Plan {planDate:yyyy-MM-dd}" },
                { "PlanDate", planDate.ToString("yyyy-MM-dd") },
                { "PlanGeneratedDate", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ") },
                { "PlanData", planJson },
                { "Status", "Planned" },
                { "TotalDays", plan.DailyPlans.Count },
                { "TotalWorkOrders", plan.FullyProcessedOrders.Count + plan.PartiallyProcessedOrders.Count }
            };

            var historyList = await _sharePointService.GetListByTitleAsync(HISTORY_LIST);
            var item = await _sharePointService.CreateListItemAsync(historyList.Id, fields);

            _logger.LogInformation("Saved pour plan for {Date} with ID {ItemId}", planDate, item.Id);
            
            return item.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving pour plan for {Date}", planDate);
            throw;
        }
    }

    public async Task<MultiDayPourPlan> GetPourPlanForDateAsync(DateTime date)
    {
        try
        {
            var historyList = await _sharePointService.GetListByTitleAsync(HISTORY_LIST);
            
            var items = await _sharePointService.GetFilteredListItemsAsync(
                historyList.Id, 
                $"fields/PlanDate eq '{date:yyyy-MM-dd}'"
            );

            var item = items.OrderByDescending(i => 
                GetFieldValue<DateTime>(i.Fields.AdditionalData, "PlanGeneratedDate"))
                .FirstOrDefault();

            if (item == null)
            {
                _logger.LogWarning("No pour plan found for date {Date}", date);
                return null;
            }

            var planDataJson = GetFieldValue<string>(item.Fields.AdditionalData, "PlanData");
            
            if (string.IsNullOrEmpty(planDataJson))
            {
                _logger.LogWarning("Plan data is empty for date {Date}", date);
                return null;
            }

            return JsonSerializer.Deserialize<MultiDayPourPlan>(planDataJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pour plan for {Date}", date);
            throw;
        }
    }

    public async Task SaveActualPourFeedbackAsync(DateTime date, ActualPourFeedback feedback)
    {
        try
        {
            var historyList = await _sharePointService.GetListByTitleAsync(HISTORY_LIST);
            
            var items = await _sharePointService.GetFilteredListItemsAsync(
                historyList.Id, 
                $"fields/PlanDate eq '{date:yyyy-MM-dd}'"
            );

            var item = items.FirstOrDefault();
            
            if (item == null)
            {
                throw new Exception($"No pour plan found for date {date:yyyy-MM-dd}");
            }

            var feedbackJson = JsonSerializer.Serialize(feedback);

            await _sharePointService.UpdateListItemAsync(historyList.Id, item.Id, new Dictionary<string, object>
            {
                { "ActualPourData", feedbackJson },
                { "Status", "Completed" },
                { "FeedbackSubmittedDate", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ") }
            });

            _logger.LogInformation("Saved actual pour feedback for {Date}", date);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving actual pour feedback for {Date}", date);
            throw;
        }
    }

    public async Task<ActualPourFeedback> GetActualPourDataAsync(DateTime date)
    {
        try
        {
            var historyList = await _sharePointService.GetListByTitleAsync(HISTORY_LIST);
            
            var items = await _sharePointService.GetFilteredListItemsAsync(
                historyList.Id, 
                $"fields/PlanDate eq '{date:yyyy-MM-dd}'"
            );

            var item = items.FirstOrDefault();
            
            if (item == null) return null;

            var feedbackJson = GetFieldValue<string>(item.Fields.AdditionalData, "ActualPourData");
            
            if (string.IsNullOrEmpty(feedbackJson)) return null;

            return JsonSerializer.Deserialize<ActualPourFeedback>(feedbackJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving actual pour data for {Date}", date);
            return null;
        }
    }

    public async Task<List<PourPlanHistoryRecord>> GetAllHistoryAsync()
    {
        try
        {
            var historyList = await _sharePointService.GetListByTitleAsync(HISTORY_LIST);
            var items = await _sharePointService.GetListItemsAsync(historyList.Id);

            return items.Select(item => new PourPlanHistoryRecord
            {
                Id = item.Id,
                PlanDate = GetFieldValue<DateTime>(item.Fields.AdditionalData, "PlanDate"),
                PlanGeneratedDate = GetFieldValue<DateTime>(item.Fields.AdditionalData, "PlanGeneratedDate"),
                Status = GetFieldValue<string>(item.Fields.AdditionalData, "Status"),
                TotalDays = GetFieldValue<int>(item.Fields.AdditionalData, "TotalDays"),
                TotalWorkOrders = GetFieldValue<int>(item.Fields.AdditionalData, "TotalWorkOrders"),
                HasFeedback = !string.IsNullOrEmpty(GetFieldValue<string>(item.Fields.AdditionalData, "ActualPourData"))
            }).OrderByDescending(r => r.PlanDate).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all history");
            throw;
        }
    }

    public async Task UpdatePlanStatusAsync(DateTime date, string status)
    {
        try
        {
            var historyList = await _sharePointService.GetListByTitleAsync(HISTORY_LIST);
            
            var items = await _sharePointService.GetFilteredListItemsAsync(
                historyList.Id, 
                $"fields/PlanDate eq '{date:yyyy-MM-dd}'"
            );

            var item = items.FirstOrDefault();
            
            if (item == null)
            {
                throw new Exception($"No pour plan found for date {date:yyyy-MM-dd}");
            }

            await _sharePointService.UpdateListItemAsync(historyList.Id, item.Id, new Dictionary<string, object>
            {
                { "Status", status }
            });

            _logger.LogInformation("Updated status to {Status} for plan on {Date}", status, date);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating plan status for {Date}", date);
            throw;
        }
    }

    private T GetFieldValue<T>(IDictionary<string, object> fields, string fieldName)
    {
        if (!fields.TryGetValue(fieldName, out var value) || value == null)
        {
            return default(T);
        }

        if (value is JsonElement jsonElement)
        {
            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            if (targetType == typeof(int))
                return (T)(object)jsonElement.GetInt32();
            if (targetType == typeof(double))
                return (T)(object)jsonElement.GetDouble();
            if (targetType == typeof(DateTime))
                return (T)(object)jsonElement.GetDateTime();
            if (targetType == typeof(decimal))
                return (T)(object)jsonElement.GetDecimal();
            if (targetType == typeof(string))
                return (T)(object)jsonElement.GetString();
            if (targetType == typeof(bool))
                return (T)(object)jsonElement.GetBoolean();
        }

        if (value is T typedValue)
        {
            return typedValue;
        }

        return default(T);
    }
}

#region Data Models

public class ActualPourFeedback
{
    public DateTime PourDate { get; set; }
    public string SubmittedBy { get; set; }
    public DateTime SubmittedDate { get; set; }
    public List<MoldFeedback> MoldFeedbacks { get; set; } = new List<MoldFeedback>();
    public string GeneralNotes { get; set; }
}

public class MoldFeedback
{
    public string MoldName { get; set; }
    public bool WasUsed { get; set; }
    public List<ItemFeedback> Items { get; set; } = new List<ItemFeedback>();
    public string Notes { get; set; }
    public string ReasonNotUsed { get; set; }
}

public class ItemFeedback
{
    public string LotName { get; set; }
    public string PurchaseOrder { get; set; }
    public double Width { get; set; }
    public double Length { get; set; }
    public bool WasPoured { get; set; }
    public string ReasonNotPoured { get; set; }
    public DateTime? ActualPourTime { get; set; }
}

public class PourPlanHistoryRecord
{
    public string Id { get; set; }
    public DateTime PlanDate { get; set; }
    public DateTime PlanGeneratedDate { get; set; }
    public string Status { get; set; }
    public int TotalDays { get; set; }
    public int TotalWorkOrders { get; set; }
    public bool HasFeedback { get; set; }
}

#endregion