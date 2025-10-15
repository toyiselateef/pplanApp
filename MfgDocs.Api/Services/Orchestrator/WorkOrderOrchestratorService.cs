using MfgDocs.Api.Data;
using MfgDocs.Api.Models;
using MfgDocs.Api.Services.Generators;
using MfgDocs.Api.Services.Others;
using Newtonsoft.Json;

namespace MfgDocs.Api.Services.Orchestrator;

/// <summary>
/// Orchestrates the complete pour planning workflow from SharePoint to PDF generation and back
/// </summary>
public class WorkOrderOrchestrationService
{
    private readonly ISharePointListService _sharePointService; 
    private readonly ILogger<WorkOrderOrchestrationService> _logger;
    private readonly WorkOrderFromExcelGenerator generator;

    public WorkOrderOrchestrationService(
        ISharePointListService sharePointService, 
        ILogger<WorkOrderOrchestrationService> logger, 
        WorkOrderFromExcelGenerator generator)
    {
        _sharePointService = sharePointService; 
        _logger = logger;
        generator = generator;
    }

    /// <summary>
    /// Complete workflow: Fetch from SharePoint → Generate Plan → Create PDF → Update SharePoint
    /// </summary>
    public async Task<bool> GenerateWorkOrderDocumentAsync(
        List<WorkOrderRequest4Dto> Ids)
    {
        try
        {
            _logger.LogInformation("Starting work order generation for Ids {Id}", JsonConvert.SerializeObject(Ids));

            // Step 1: Fetch unpoured work orders from SharePoint
            var workOrders = await _sharePointService.GetWorkOrdersByWorkIdsAsync(Ids);

            if (!workOrders.Any())
            {
                _logger.LogWarning("No work orders found");
                return  false;
            }

            var pdfBytes = generator.GenerateWorkOrderPdf(workOrders.First()); 
            
            var uploadResult = await _sharePointService.UploadFileToSharePointAsync(
                pdfBytes,
                $"WorkOrder_{workOrders.First().PurchaseOrder}.pdf",
                "WorkOrders",
                allowReplaceFile: true
            );
                    
            if (uploadResult.Success)
            {
                _logger.LogInformation("Uploaded PDF to SharePoint: {FileUrl}", uploadResult.FileUrl);
            }

           
            return  true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing pour plan workflow");
            return false;
        }
    }

    
    
    public async Task<bool> GenerateWorkOrderDocument(
        List<string> Ids)
    {
        try
        {
            _logger.LogInformation("Starting work order generation for Ids {Id}", JsonConvert.SerializeObject(Ids));

            // Step 1: Fetch unpoured work orders from SharePoint
            var workOrders = await _sharePointService.GetWorkOrdersByIdsAsync(Ids);

            if (!workOrders.Any())
            {
                _logger.LogWarning("No work orders found");
                return  false;
            }

            var pdfBytes = generator.GenerateWorkOrderPdf(workOrders.First()); 
            
            var uploadResult = await _sharePointService.UploadFileToSharePointAsync(
                pdfBytes,
                $"WorkOrder_{workOrders.First().PurchaseOrder}.pdf",
                "WorkOrders",
                allowReplaceFile: true
            );
                    
            if (uploadResult.Success)
            {
                _logger.LogInformation("Uploaded PDF to SharePoint: {FileUrl}", uploadResult.FileUrl);
            }

           
            return  true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing pour plan workflow");
            return false;
        }
    }

}
