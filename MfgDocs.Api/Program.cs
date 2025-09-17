using iText.Kernel.Font;
using MfgDocs.Api.Models;
using MfgDocs.Api.Services;
using MfgDocs.Api.Services.Generators;
using MfgDocs.Api.Services.Others; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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

// Register the services that were missing registration 
builder.Services.AddSingleton<DayToDayPourPlanGenerator>();
builder.Services.AddSingleton<DailyPouringPlanGenerator>();
builder.Services.AddSingleton<WorkOrderFromExcelGenerator>();
builder.Services.AddSingleton<PouringPlanService>();
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
string fontPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Assets", "Fonts", "Roboto-Regular.ttf");

// Example with Spire.PDF
 
// Register custom fonts
//FontManager.RegisterFont(File.OpenRead(Path.Combine("wwwroot", "Assets", "Fonts", "Arial.ttf")));
//FontManager.RegisterFont(File.OpenRead(Path.Combine("wwwroot", "Assets", "Fonts", "Roboto-Regular.ttf")));

// For QuestPDF default font
//QuestPDF.Settings.DefaultFont = "Arial"; 


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
var settings = app.Services.GetRequiredService<IOptions<DocumentGenerationSettings>>().Value;
Directory.CreateDirectory(settings.TempPath);
Directory.CreateDirectory(settings.PuppeteerSettings.CachePath);

app.Run();

namespace MfgDocs.Api
{
    public partial class Program { }
}