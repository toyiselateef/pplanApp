using Azure.Identity;
using iText.Kernel.Font;
using MfgDocs.Api.Data;
using MfgDocs.Api.Models;
using MfgDocs.Api.Services;
using MfgDocs.Api.Services.Generators;
using MfgDocs.Api.Services.Orchestrator;
using MfgDocs.Api.Services.Others; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using Microsoft.OpenApi.Models;
using QuestPDF.Drawing;
using Serilog; 

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, cfg) => cfg
.ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console()
);

// Add services to the container.
// DI
builder.Services.Configure<PricingOptions>(builder.Configuration.GetSection("Pricing"));
builder.Services.Configure<BrandingOptions>(builder.Configuration.GetSection("Branding"));
builder.Services.Configure<PouringPlanConfig>(builder.Configuration.GetSection("PouringPlanConfig"));

// Register your custom services
builder.Services.AddSingleton<SizeCalculator>();
builder.Services.AddSingleton<WeightCalculator>();
builder.Services.AddSingleton<PricingCalculator>();  
builder.Services.AddSingleton<PPPDFGenerator>();
builder.Services.AddSingleton<WorkOrderFromExcelGenerator>();
builder.Services.AddSingleton<DeliverySlipPdfGenerator>();
//
builder.Services.AddHttpClient();

// Register services 

// Register the services that were missing registration 
// builder.Services.AddSingleton<DayToDayPourPlanGenerator>();
// builder.Services.AddSingleton<DailyPouringPlanGenerator>();

builder.Services.AddScoped<DayToDayPourPlanGenerator>();
builder.Services.AddScoped<DailyPouringPlanGenerator>();
builder.Services.AddScoped<PourPlanOrchestrationService>();

builder.Services.AddScoped<IPourPlanHistoryService, PourPlanHistoryService>();
builder.Services.AddScoped<TrackerDocumentGenerator>();


builder.Services.AddSingleton<WorkOrderFromExcelGenerator>();
builder.Services.AddScoped<PouringPlanService>();
builder.Services.AddScoped<ITrackerService, TrackerService>();
//builder.Services.AddSingleton<PouringPlanService>();
//
// builder.Services.AddScoped<IWorkOrderService, EnhancedWorkOrderService>();
// builder.Services.AddScoped<IPourPlanService, PourPlanService>();
// builder.Services.AddScoped<PdfGenerationService>();

// Register Swagger/OpenAPI services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Pouring Plan API", Version = "v1" });
});
 
// Example with Spire.PDF
 
// Register custom fonts
//FontManager.RegisterFont(File.OpenRead(Path.Combine("wwwroot", "Assets", "Fonts", "Arial.ttf")));
//FontManager.RegisterFont(File.OpenRead(Path.Combine("wwwroot", "Assets", "Fonts", "Roboto-Regular.ttf")));

// For QuestPDF default font
//QuestPDF.Settings.DefaultFont = "Arial"; 

// Configure Microsoft Graph
builder.Services.AddScoped<ISharePointListService, SharePointListService>();
builder.Services.AddScoped<GraphServiceClient>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    
    var options = new ClientSecretCredentialOptions
    {
        AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
    };
    
    var clientSecretCredential = new ClientSecretCredential(
            Environment.GetEnvironmentVariable("TENANT_ID_"),
            Environment.GetEnvironmentVariable("CLIENT_ID_"),
        Environment.GetEnvironmentVariable("CLIENT_"),
        options);
  // var clientSecretCredential = new ClientSecretCredential(
  //       config["Sharepoint:TENANT_ID"],
  //       config["Sharepoint:CLIENT_ID"],
  //       Environment.GetEnvironmentVariable("CLIENT_"),
  //       options);

    return new GraphServiceClient(clientSecretCredential);
});


var app = builder.Build();

// Configure the HTTP request pipeline.
// Enable Swagger middleware
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pouring Plan API V1");
});

app.MapGet("/", () => Results.Text("MFG Docs API (PDF-only) is running."));

app.MapPost("/api/day2day-pourplan", async ([FromServices] PouringPlanService pouringService) =>
{
    //var pouringService = new PouringPlanService();
    var workOrders = PouringPlanService.GetSampleWorkOrders();

    byte[] pdfBytes = pouringService.GenerateDailyPourPlan(
        workOrders, 
        "2025-09-05",  // date
        "Gray",        // color
        "1"           // pour number
    );
    return Results.File(pdfBytes, "application/pdf", "Sampled2dpp.pdf");

});
app.MapPost("/api/daymultiday-pourplan", async ([FromServices] PouringPlanService pouringService) =>
{
    //var pouringService = new PouringPlanService();
    var workOrders = PouringPlanService.GetSampleWorkOrders();

    byte[] pdfBytes =
        pouringService.GenerateMultiDayPourPlan(PouringPlanService.GetEnhancedSampleWorkOrders(), DateTime.Now,
 
        "Gray",        // color
        "1"           // pour number
    );
    return Results.File(pdfBytes, "application/pdf", "Sampled2dpp.pdf");

});
app.MapGet("/api/wkOrderplan", async (WorkOrderFromExcelGenerator generator) =>
{
   
    try
    {
        var sampleRequest = new WorkOrderRequest4
        {
            OrderDate = "24-Jun-2025",
            PurchaseOrder = "BL-45678",
            Company = "LEGACY",
            Contact = "Steph",
            Builder = "Mattamy Homes",
            Site = "LAKEHAVEN",
            City = "MILTON",
            BlkNo = "12,23,12,12",
            LotNo = "68, 63, 31, 35",
            Items = new List<Order>()
     {
    new Order { LotName = "Lot A", Quantity = 10, FinishedLength = 22, FinishedWidth = 6, Color = "OLD WHITE",   Type = "ROCK FACE" },
    new Order { LotName = "Lot A", Quantity = 5,  FinishedLength = 20, FinishedWidth = 5, Color = "GRAY",  Type = "ROCK FACE 2S" },
    new Order { LotName = "Lot C", Quantity = 8,  FinishedLength = 18, FinishedWidth = 4, Color = "NEW WHITE",  Type = "ROCK FACE 1L,2S" },

    new Order { LotName = "Lot D", Quantity = 12, FinishedLength = 25, FinishedWidth = 7, Color = "NEW WHITE",   Type = "ROCK FACE 2L" },
    new Order { LotName = "Lot B", Quantity = 6,  FinishedLength = 23, FinishedWidth = 6, Color = "OLD WHITE", Type = "ROCK FACE 2L,1S" },
    new Order { LotName = "Lot D", Quantity = 4,  FinishedLength = 19, FinishedWidth = 5, Color = "GRAY",  Type = "SMOOTH FACE" }
    }
        };

        //var output = generator.GenerateWorkOrderExcel(sampleRequest, $"wk_{Guid.NewGuid()}.xlsx");
        var pdfBytes = generator.GenerateWorkOrderPdf(sampleRequest);
        Console.WriteLine($"PDF generated successfully:");
        return Results.File(pdfBytes, "application/pdf", "SampleWorkOrder.pdf");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        return Results.File([], "application/pdf", "SamplePourplan.pdf");

    }
});

  
app.MapPost("/api/wkOrderplan-actual", async ([FromBody]WorkOrderRequest4 sampleRequest, WorkOrderFromExcelGenerator generator) =>
{
   
    try
    { 
        //var output = generator.GenerateWorkOrderExcel(sampleRequest, $"wk_{Guid.NewGuid()}.xlsx");
        var pdfBytes = generator.GenerateWorkOrderPdf(sampleRequest);
        Console.WriteLine($"PDF generated successfully:");
        if (pdfBytes.Length <= 0 || pdfBytes == null ||  pdfBytes == Array.Empty<byte>()) return Results.Problem();
        return Results.File(pdfBytes, "application/pdf", "SampleWorkOrder.pdf");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        return Results.Problem(); //.File([], "application/pdf", "SamplePourplan.pdf");

    }
});

app.MapGet("/api/wkOrderplan-excel", async (WorkOrderFromExcelGenerator generator) =>
{
   
    try
    {
        var sampleRequest = new WorkOrderRequest4
        {
            OrderDate = "24-Jun-2025",
            PurchaseOrder = "BL-45678",
            Company = "LEGACY",
            Contact = "Steph",
            Builder = "Mattamy Homes",
            Site = "LAKEHAVEN",
            City = "MILTON",
            BlkNo = "12,23,12,12",
            LotNo = "68, 63, 31, 35",
            Items = new List<Order>()
     {
      new Order { LotName = "Lot A", Quantity = 10, FinishedLength = 22, FinishedWidth = 6, Color = "OLD WHITE",   Type = "ROCK FACE" },
    new Order { LotName = "Lot A", Quantity = 5,  FinishedLength = 20, FinishedWidth = 5, Color = "GRAY",  Type = "ROCK FACE 2S" },
    new Order { LotName = "Lot C", Quantity = 8,  FinishedLength = 18, FinishedWidth = 4, Color = "NEW WHITE",  Type = "ROCK FACE 1L,2S" },

    new Order { LotName = "Lot D", Quantity = 12, FinishedLength = 25, FinishedWidth = 7, Color = "NEW WHITE",   Type = "ROCK FACE 2L" },
    new Order { LotName = "Lot B", Quantity = 6,  FinishedLength = 23, FinishedWidth = 6, Color = "OLD WHITE", Type = "ROCK FACE 2L,1S" },
    new Order { LotName = "Lot D", Quantity = 4,  FinishedLength = 19, FinishedWidth = 5, Color = "GRAY",  Type = "SMOOTH FACE" }
    }
        };

        //var output = generator.GenerateWorkOrderExcel(sampleRequest, $"wk_{Guid.NewGuid()}.xlsx");
        var excelBytes = generator.GenerateWorkOrderExcel(sampleRequest);
        Console.WriteLine($"PDF generated successfully:");
        return Results.File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SampleWorkOrder.xlsx");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        return Results.File([], "application/pdf", "SamplePourplan.pdf");

    }
});
//.............
app.MapPost("/api/work-order/generate", async ([FromServices] WorkOrderOrchestrationService service, [FromBody] List<WorkOrderRequest4Dto> request) =>
{
    var result = await service.GenerateWorkOrderDocumentAsync(
        request);

    if (!result)
        return Results.BadRequest();

    return Results.Ok(result);
});
//.............
app.MapPost("/api/pourplan/preview", async ([FromServices] PourPlanOrchestrationService service, [FromBody] PourPlanRequestNew request) =>
{
    var preview = await service.PreviewPourPlanAsync(request.StartDate, request.Color);
    return Results.Ok(preview);
});

app.MapPost("/api/pourplan/generate", async ([FromServices] PourPlanOrchestrationService service, [FromBody] PourPlanRequestNew request) =>
{
    var result = await service.ExecuteCompletePourPlanWorkflowAsync(
        request.StartDate,
        request.Color,
        request.PourNumber ?? "1",
        request.AutoUpdateSharePoint,
        request.saveHistory);

    if (!result.Success)
        return Results.BadRequest(new { result.Message });

    return Results.File(result.PdfDocument, "application/pdf", $"PourPlan_{DateTime.Now:yyyyMMdd}.pdf");
});

// app.MapPost("/api/pourplan/generate-with-details", async ([FromServices] PourPlanOrchestrationService service, [FromBody] PourPlanRequestNew request) =>
// {
//     var result = await service.ExecuteCompletePourPlanWorkflowAsync(
//         request.StartDate,
//         request.Color,
//         request.PourNumber ?? "1",
//         request.AutoUpdateSharePoint);
//
//     if (!result.Success)
//         return Results.BadRequest(new { result.Message });
//
//     var response = new
//     {
//         result.Success,
//         PdfBase64 = Convert.ToBase64String(result.PdfDocument),
//         result.Summary
//     };
//
//     return Results.Ok(response);
// });
//
// app.MapGet("/api/pourplan/work-orders/status", async ([FromServices] PourPlanOrchestrationService service) =>
// {
//     var status = await service.GetWorkOrderStatusAsync();
//     return Results.Ok(status);
// });
//
// app.MapPost("/api/pourplan/update-sharepoint", async ([FromBody] SharePointUpdateRequest request) =>
// {
//     return Results.Ok(new { message = "SharePoint update feature requires stored pour plan data" });
// });
//...........
//...........



// Define the Minimal API Group
var trackerApi = app.MapGroup("/api/PourPlanTracker");

//
// 1. Get tracker data for a specific date (READ ONLY view)
//
trackerApi.MapGet("/date/{date}", async (
    [FromRoute] DateTime date,
    ITrackerService trackerService,
    ILogger<Program> logger) =>
    {
        try
        {
            var trackerData = await trackerService.GetTrackerDataForDate(date);
            
            if (trackerData == null || !trackerData.Any())
            {
                return Results.NotFound(new { message = "No pour plan found for this date" });
            }

            return Results.Ok(new
            {
                planDate = date,
                isEditable = date.Date == DateTime.Today,
                items = trackerData,
                summary = trackerService.CalculateTrackerSummary(trackerData)
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving tracker data for {Date}", date);
            return Results.Ok(new { message = ex.Message });
        }
    })
.WithName("GetTrackerForDate")
.WithDescription("Get tracker data for a specific date - READ ONLY view");

//
// 2. Get all historical pour plans (for history view)
// LOGIC REMAINS IN THE HISTORY SERVICE BUT API HANDLER ADDS FILTER/PAGINATION
//
trackerApi.MapGet("/history", async (
    [FromQuery] DateTime? startDate,
    [FromQuery] DateTime? endDate,
    [FromQuery] int pageSize,
    [FromQuery] int pageNumber,
    IPourPlanHistoryService historyService,
    ILogger<Program> logger) =>
{
    try
    {
        // Default pagination values if not provided in query string
        pageSize = pageSize == 0 ? 50 : pageSize;
        pageNumber = pageNumber == 0 ? 1 : pageNumber;

        var history = await historyService.GetAllHistoryAsync();

        // Apply filters
        if (startDate.HasValue)
            history = history.Where(h => h.PlanDate >= startDate.Value).ToList();
        
        if (endDate.HasValue)
            history = history.Where(h => h.PlanDate <= endDate.Value).ToList();

        // Pagination
        var totalCount = history.Count;
        var pagedData = history
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Results.Ok(new
        {
            totalCount,
            pageSize,
            pageNumber,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            data = pagedData
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error retrieving history");
        return Results.Ok(new { message = ex.Message });
    }
})
.WithName("GetPourPlanHistory")
.WithDescription("Get all historical pour plans (for history view)");

//
// 3. Update actual poured quantities (EDITABLE - only for today)
//
trackerApi.MapPost("/update-actuals", async (
    [FromBody] List<TrackerUpdateRequest> updates,
    [FromServices]ITrackerService trackerService,
    [FromServices] IPourPlanHistoryService _historyService,
    [FromServices]ISharePointListService _sharePointService,
    ILogger<Program> logger) =>
    {
        try
        {
            // Validate that all updates are for today
            var today = DateTime.Today;
            if (updates.Any(u => u.PlanDate.Date != today))
            {
                return Results.BadRequest(new 
                { 
                    message = "You can only edit pour quantities for today's date" 
                });
            }

            // Validate quantities
            foreach (var update in updates)
            {
                if (update.ActualPouredQuantity < 0)
                {
                    return Results.BadRequest(new 
                    { 
                        message = $"Invalid quantity for {update.LotName}: Cannot be negative" 
                    });
                }

                if (update.ActualPouredQuantity > update.PlannedQuantity)
                {
                    return Results.BadRequest(new 
                    { 
                        message = $"Invalid quantity for {update.LotName}: " +
                                 $"Actual ({update.ActualPouredQuantity}) cannot exceed " +
                                 $"Planned ({update.PlannedQuantity})" 
                    });
                }
            }

            // Update SharePoint Tracker List
            var trackerList = await _sharePointService.GetListByTitleAsync("Pour Plan Tracker");
            
            foreach (var update in updates)
            {
                var items = await _sharePointService.GetFilteredListItemsAsync(
                    trackerList.Id,
                    $"fields/PlanDate eq '{update.PlanDate:yyyy-MM-dd}' and " +
                    $"fields/LotName eq '{update.LotName}' and " +
                    $"fields/PurchaseOrder eq '{update.PurchaseOrder}'"
                );

                var item = items.FirstOrDefault();
                if (item != null)
                {
                    var remaining = update.FullOrderQuantity - update.ActualPouredQuantity;
                    
                    await _sharePointService.UpdateListItemAsync(
                        trackerList.Id,
                        item.Id,
                        new Dictionary<string, object>
                        {
                            { "ActualPouredQuantity", update.ActualPouredQuantity },
                            { "RemainingQuantity", remaining },
                            { "Status", remaining == 0 ? "Completed" : "In Progress" },
                            { "LastModifiedBy", update.ModifiedBy },
                            { "LastModifiedDate", DateTime.Now },
                            { "ModificationNotes", update.Notes }
                        }
                    );

                    // Update the original Orders list
                    await trackerService.UpdateOriginalOrderProgress(
                        update.PurchaseOrder, 
                        update.LotName,
                        update.ActualPouredQuantity
                    );
                }
            }

            // Mark history as modified
            await _historyService.UpdatePlanStatusAsync(today, "Modified by Builder");

            return Results.Ok(new 
            { 
                message = "Actual quantities updated successfully",
                updatedCount = updates.Count
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating actual quantities");
            return Results.Ok(new { message = ex.Message });
        }
    })
.WithName("UpdateActualQuantities")
.WithDescription("Update actual poured quantities - EDITABLE (only for today)");

//
// 4. Get summary statistics for dashboard
//
trackerApi.MapGet("/dashboard", async (
     
    ILogger<Program> logger,
    [FromServices]ITrackerService trackerService,
    [FromServices] IPourPlanHistoryService _historyService,
    [FromServices]ISharePointListService _sharePointService) =>
    {
        try
        {
            var today = DateTime.Today;
            var todayData = await trackerService.GetTrackerDataForDate(today);
            var history = await _historyService.GetAllHistoryAsync();

            return Results.Ok(new
            {
                today = new
                {
                    date = today,
                    totalItems = todayData?.Count ?? 0,
                    plannedItems = todayData?.Sum(d => d.PlannedQuantity) ?? 0,
                    completedItems = todayData?.Sum(d => d.ActualPouredQuantity) ?? 0,
                    remainingItems = todayData?.Sum(d => d.RemainingQuantity) ?? 0,
                    completionPercentage = trackerService.CalculateTrackerSummary(todayData)
                },
                thisWeek = new
                {
                    totalPlans = history.Count(h => h.PlanDate >= today.AddDays(-7)),
                    completedPlans = history.Count(h => 
                        h.PlanDate >= today.AddDays(-7) && h.Status == "Completed")
                },
                recentActivity = history
                    .OrderByDescending(h => h.PlanDate)
                    .Take(5)
                    .ToList()
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving dashboard data");
            return Results.Ok(new { message = ex.Message });
        }
    })
.WithName("GetDashboardData")
.WithDescription("Get summary statistics for dashboard");
var settings = app.Services.GetRequiredService<IOptions<DocumentGenerationSettings>>().Value;
Directory.CreateDirectory(settings.TempPath);
Directory.CreateDirectory(settings.PuppeteerSettings.CachePath);

app.Run();

namespace MfgDocs.Api
{
    public partial class Program { }
}

#region Request/Response Models

public class PourPlanRequestNew
{
    public DateTime StartDate { get; set; } = DateTime.Now;
    public string Color { get; set; } = "Standard";
    public string PourNumber { get; set; } = "1";
    public bool AutoUpdateSharePoint { get; set; } = true;
    public bool saveHistory { get; set; } = true;
}

public class SharePointUpdateRequest
{
    public string PourPlanId { get; set; }
}

#endregion