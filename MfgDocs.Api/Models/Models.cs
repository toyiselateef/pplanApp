using DocumentFormat.OpenXml.EMMA;
using MfgDocs.Api.Services.Generators;
using System.ComponentModel.DataAnnotations;

namespace MfgDocs.Api.Models;

public enum FinishType { RockFace, SmoothFace }
public enum RockFaceSides { AllSides, TwoLong, TwoLongOneShort, OneLongOneShort, TwoShort, Butt }
public enum ColorType { NewWhiteSmooth, Gray, OldWhite }

public record Dimension(decimal WidthInches, decimal LengthInches, decimal ThicknessInches = 3);

public record WorkOrderLine(
    string ProductName,
    Dimension FinishedSize,
    FinishType FinishType,
    RockFaceSides? RockFaceSides,
    ColorType Color,
    int Quantity
);

public record WorkOrderRequest(
    string OrderNumber,
    string Customer,
    string Contact,
    string Site,
    DateOnly OrderDate,
    string? PurchaseOrder,
    string? LotOrBlock,
    string? MapLink,
    List<WorkOrderLine> Lines
);

public record PouredResult(
    Dimension PouredSize,
    bool RequiresRebar,
    decimal UnitWeightPounds,
    decimal UnitPrice,
    decimal LineTotal
);

public record WorkOrderComputedLine(
    WorkOrderLine Line,
    PouredResult Result
);

public record WorkOrderComputed(
    WorkOrderRequest Request,
    List<WorkOrderComputedLine> Lines,
    decimal Subtotal,
    decimal Surcharge,
    decimal Total
);

public record PourPlanRequest(
    DateOnly PlanDate,
    List<WorkOrderRequest> Orders
);

public record DeliverySlipRequest(
    string OrderNumber,
    string Customer,
    string Address,
    DateOnly DeliveryDate,
    List<DeliveryLine> Lines
);

public record DeliveryLine(
    string Description,
    int Quantity,
    decimal WeightEachPounds
);

public class PricingOptions
{
    public string Currency { get; set; } = "USD";
    public decimal RoundTo { get; set; } = 0.05m;
    public int RebarLengthThresholdInches { get; set; } = 50;
    public decimal RebarCost { get; set; } = 6.30m;
    public decimal SmoothFaceSurcharge { get; set; } = 7.85m;
    public decimal RockfaceRateSmall { get; set; } = 0.114m;
    public decimal RockfaceRateLarge { get; set; } = 0.124m;
    public int LargeThresholdShortSideInches { get; set; } = 29;
    public decimal WeightFactor { get; set; } = 0.21m;
}

public class BrandingOptions
{
    public string CompanyName { get; set; } = "MFG Precast";
    public string Address { get; set; } = "";
    public string Phone { get; set; } = "";
}

/// <summary>
/// 
/// </summary>

public class WorkOrderLineItem
{
    public int Quantity { get; set; }
    public string LotNumber { get; set; }
    public string LotItem { get; set; }
    public string Color { get; set; }
    public string Type { get; set; }
    public string AreaSqIn { get; set; }
    public string WeightLbs { get; set; }
    public List<Tuple<string, string>> PouredSizes { get; set; } = new List<Tuple<string, string>>();
    public List<Tuple<string, string>> FinishedSizes { get; set; } = new List<Tuple<string, string>>();
}

// A class to represent the entire work order's data.
public class WorkOrderData
{
    public string CompanyName { get; set; }
    public string CompanyContact { get; set; }
    public string WorkOrderDate { get; set; }
    public string PurchaseOrderNumber { get; set; }
    public string BuilderSiteCity { get; set; }
    public string BlockNumber { get; set; }
    public string LotNumber { get; set; }
    public string LotStreet { get; set; }
    public string Notes { get; set; }
    public string TotalWeight { get; set; }
    public string ExpectedDeliveryDate { get; set; }
    public List<WorkOrderLineItem> LineItems { get; set; } = new List<WorkOrderLineItem>();
}


public class ExcelPlanEntry
{
    [Required]
    public DateTime Date { get; set; }
    [Required]
    public string Supplier { get; set; }
    [Required]
    public string PO { get; set; }
    public string Lot { get; set; }
    public string Location { get; set; }
    public List<string> FullOrderLines { get; set; } = new List<string>();
    public List<string> PlannedLines { get; set; } = new List<string>();
    public string SuggestedMold { get; set; }
    public string LeftToPour { get; set; } = "NILL";
}

public class ExcelPouringPlanRequest
{
    [Required, MinLength(1)]
    public List<ExcelPlanEntry> Entries { get; set; }
    public string DayOfWeek { get; set; }
}

public class MoldDetail
{
    [Required]
    public string Name { get; set; }
    public string Size { get; set; }
    public double Length { get; set; }
    public double Height { get; set; }
    public string Color { get; set; } = "Red";
}

public class BagCalculationRow
{
    public string MoldSize { get; set; }
    public int NoOfPcs { get; set; }
    public double PouredSizeInches { get; set; }
    public int ExtraMargin { get; set; }
    public double TotalSqInch { get; set; }
}

public class PdfPouringPlanRequest
{
    [Required]
    public string Title { get; set; }
    [Required, MinLength(1)]
    public List<MoldDetail> Molds { get; set; }
    public List<BagCalculationRow> BagTable { get; set; } = new List<BagCalculationRow>();
}

////
///
public class PourPlanRequest2
{
    public string Day { get; set; } = "";
    public DateTime Date { get; set; }
    public List<ScheduleBlock> ScheduleBlocks { get; set; } = new();
    public List<MoldDetails> Molds { get; set; } = new();
    public CalculationData? CalculationData { get; set; }
}

public class ScheduleBlock
{
    public string TimeSlot { get; set; } = "";
    public List<ScheduleItem> Items { get; set; } = new();
}

public class ScheduleItem
{
    public int Sequence { get; set; }
    public string Description { get; set; } = "";
    public string Duration { get; set; } = "";
    public string Status { get; set; } = "";
}

public class MoldDetails
{
    public string Name { get; set; } = "";
    public List<MoldSection> Sections { get; set; } = new();
}

public class MoldSection
{
    public string Width { get; set; } = "";
    public string Height { get; set; } = "";
    public string TotalMeasurement { get; set; } = "";
}

public class CalculationData
{
    public List<CalculationRow> Rows { get; set; } = new();
}

public class CalculationRow
{
    public string MoldSize { get; set; } = "";
    public int SqFace1 { get; set; }
    public string PouredSize1 { get; set; } = "";
    public int SqFace2 { get; set; }
    public string PouredSize2 { get; set; } = "";
    public int SqFace3 { get; set; }
    public int ExtraMargin { get; set; }
    public int TotalProduct { get; set; }
}

public class WorkOrderRequest2
{
    public string OrderNumber { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public string PurchaseOrder { get; set; } = "";
    public string Company { get; set; } = "";
    public string Contact { get; set; } = "";
    public CompanyInfo CompanyInfo { get; set; } = new();
    public List<SiteInfo> Sites { get; set; } = new();
    public List<OrderItem> OrderItems { get; set; } = new();
    public string Notes { get; set; } = "";
    public int TotalWeight { get; set; }
    public DateTime ExpectedDelivery { get; set; }
}

public class CompanyInfo
{
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
}

public class SiteInfo
{
    public string Builder { get; set; } = "";
    public string Location { get; set; } = "";
    public string BlockNumber { get; set; } = "";
    public string LotNumber { get; set; } = "";
    public string Company { get; set; } = "";
    public string Contact { get; set; } = "";
}

public class OrderItem
{
    public string LotNumber { get; set; } = "";
    public List<ItemDetail> Details { get; set; } = new();
}

public class ItemDetail
{
    public int Quantity { get; set; }
    public string PouredWidth { get; set; } = "";
    public string PouredLength { get; set; } = "";
    public string FinishedWidth { get; set; } = "";
    public string FinishedLength { get; set; } = "";
    public string Color { get; set; } = "";
    public string Type { get; set; } = "";
    public int Area { get; set; }
    public int Weight { get; set; }
}

public class DailyScheduleRequest
{
    public DateTime Date { get; set; }
    public List<ScheduleDay> ScheduleDays { get; set; } = new();
}

public class ScheduleDay
{
    public DateTime Date { get; set; }
    public List<ProjectInfo> Projects { get; set; } = new();
}

public class ProjectInfo
{
    public string ProjectName { get; set; } = "";
    public string Location { get; set; } = "";
    public List<string> FullOrder { get; set; } = new();
    public List<string> PlannedToPour { get; set; } = new();
    public string? SuggestedMold { get; set; }
    public List<string>? LeftToPour { get; set; }
}

public enum TemplateFormat
{
    Html,
    Razor,
    Liquid

}

public enum PdfPageFormat
{
    A4Portrait,
    A4Landscape,
    A3Portrait,
    A3Landscape
}


public class WorkOrderRequest4
{
    public string OrderDate { get; set; } = string.Empty;
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
    public int Priority { get; set; }
}

public class WorkOrderRequest3
{
    public string OrderDate { get; set; } = string.Empty;
    public string PurchaseOrder { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public string BuilderSiteCity { get; set; } = string.Empty;
    public string BlkNo { get; set; } = string.Empty;
    public string LotNo { get; set; } = string.Empty;
    public List<WorkOrderItem> Items { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
    public string ExpectedDeliveryDate { get; set; } = string.Empty;
}

public class WorkOrderItem
{
    public string LotNo { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string PouredSize { get; set; } = string.Empty;
    public string FinishedSize { get; set; } = string.Empty;
    public string Width { get; set; } = string.Empty;
    public string Length { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Weight { get; set; } = string.Empty;
}

public class PourPlanRequest3
{
    public string Date { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string PourNumber { get; set; } = string.Empty;
    public List<MoldInfos> Molds { get; set; } = new();
    public PourPlanTable? Table { get; set; }
}


public class MoldInfos
{
    public string Name { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public List<MoldItem> Items { get; set; } = new();
}

public class MoldItem
{
    public string Size { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsHighlighted { get; set; }
}

public class PourPlanTable
{
    public List<PourPlanTableRow> Rows { get; set; } = new();
}

public class PourPlanTableRow
{
    public string MoldSize { get; set; } = string.Empty;
    public string PouredSize { get; set; } = string.Empty;
    public string PouredSizeInches { get; set; } = string.Empty;
    public string PouredSizeInches2 { get; set; } = string.Empty;
    public string Alo { get; set; } = string.Empty;
    public string Op { get; set; } = string.Empty;
    public string FormLenth { get; set; } = string.Empty;
    public string FormHeightMax { get; set; } = string.Empty;
    public string FormHeightMaxInches { get; set; } = string.Empty;
    public string TotalMixing { get; set; } = string.Empty;
    public string TotalMixing2 { get; set; } = string.Empty;
    public string CumMixing { get; set; } = string.Empty;
}

public class PourPlanDay
{
    public string Date { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string FullOrder { get; set; } = string.Empty;
    public string PlannedToBePoured { get; set; } = string.Empty;
    public string SuggestedMold { get; set; } = string.Empty;
    public string LeftToBePoured { get; set; } = string.Empty;
    public bool IsHighlighted { get; set; }
}


////////////
///
public class DocumentGenerationSettings
{
    public const string SectionName = "DocumentGeneration";

    public string TempPath { get; set; } = "temp";
    public int MaxFileSizeMB { get; set; } = 50;
    public PdfSettings PdfSettings { get; set; } = new();
    public PuppeteerSettings PuppeteerSettings { get; set; } = new();
}

public class PdfSettings
{
    public string Quality { get; set; } = "High";
    public bool Compression { get; set; } = true;
    public bool EnableJavaScript { get; set; } = false;
}

public class PuppeteerSettings
{
    public bool HeadlessMode { get; set; } = true;
    public int Timeout { get; set; } = 30000;
    public string CachePath { get; set; } = "puppeteer_cache";
    public bool NoSandbox { get; set; } = false;
    public bool DisableSetuidSandbox { get; set; } = false;
    public bool DevToolsEnabled { get; set; } = false;
}

public class CorsSettings
{
    public const string SectionName = "Cors";
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}

public class TableRow
     {
         public string MoldSize { get; set; }
         public int Poured { get; set; }
         public string PourDate { get; set; }
         public int TotalPcs { get; set; }
         public float CubicYards { get; set; }
         public float TotalArea { get; set; }
     }
public class MoldInfo
    {
        public string Name { get; set; }
        public float MoldWidth { get; set; }
        public float MoldHeight { get; set; }
        public List<SectionInfo> Sections { get; set; } = new List<SectionInfo>();
        public string TotalLengthWithMargin { get; set; }
        public string PourCategory { get; set; }
    }


     public class SectionInfo
     {
         public float Width { get; set; }
         public float Height { get; set; }
         public string Label { get; set; }
         public string RedLineLabel { get; set; }
         public bool IsTopSide { get; set; } = true;
     }
////
///
 public class DataData
            { 
                public string pourNumber { get; set; }   
                public string date { get; set; }   
                public string color { get; set; }   
                public List<MoldInfo> molds { get; set; }
            }