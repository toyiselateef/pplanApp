using System.Diagnostics;
using Newtonsoft.Json;
using QuestPDF.Helpers;

namespace MfgDocs.Api.Services.Generators;

using System;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;
using MfgDocs.Api.Models;
using QuestPDF.Fluent;
using Spire.Xls;

public class Order
{
    public string LotName { get; set; }
    public int Quantity { get; set; }
    public double FinishedLength { get; set; }
    public double FinishedWidth { get; set; }
    public string Color { get; set; }
    public string Type { get; set; }
    public double PourWidth { get; set; }
    public double PourLength { get; set; }
}

public class WorkOrderFromExcelGenerator
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<WorkOrderFromExcelGenerator> _logger;
    private readonly string _templatePath;

    public WorkOrderFromExcelGenerator(IWebHostEnvironment environment, ILogger<WorkOrderFromExcelGenerator> logger,
        string templatePath =
            "C:\\Users\\User\\Downloads\\MfgDocs.Api\\MfgDocs.Api\\wwwroot\\Assets\\Templates\\FORMULA SHEET WITH CONSTANT.xlsx")
    {
        _environment = environment;
        _logger = logger;
        _templatePath = Path.Combine(_environment.WebRootPath, "Assets", "Templates",
            "FORMULA SHEET WITH CONSTANT.xlsx"); //templatePath;
    }
    
    private void LogSystemInfo()
    {
        
        try
        {
            _logger.LogInformation($"Current working directory: {Directory.GetCurrentDirectory()}");
            _logger.LogInformation($"Temp path: {Path.GetTempPath()}");
        
            // Check PATH environment variable
            string pathEnv = Environment.GetEnvironmentVariable("PATH");
            _logger.LogInformation($"PATH environment variable: {pathEnv}");
        
            // List /usr/bin directory
            if (Directory.Exists("/usr/bin"))
            {
                var files = Directory.GetFiles("/usr/bin", "*office*")
                    .Concat(Directory.GetFiles("/usr/bin", "*libre*"))
                    .ToList();
                _logger.LogInformation($"Office-related files in /usr/bin: {string.Join(", ", files)}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging system info");
        }
    }

    // public byte[] GenerateWorkOrderPdf(WorkOrderRequest4 orders)
    // {
    //     LogSystemInfo();
    //     // 1. Create isolated temp folder
    //     string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    //     Directory.CreateDirectory(tempDir);
    //
    //     string excelPath = Path.Combine(tempDir, "workorder.xlsx");
    //     string pdfPath = Path.Combine(tempDir, "workorder.pdf");
    //     _logger.LogInformation($"excel:{excelPath}\n pdfPath:{pdfPath} ");
    //     try
    //     {
    //         // 2. Load template and fill data using ClosedXML
    //         using (var workbook = new XLWorkbook(_templatePath))
    //         {
    //             var sheet = workbook.Worksheet("WORK ORDER");
    //
    //             int startRow = 19;
    //             int endRow = 50;
    //             int currentRow = startRow;
    //
    //             // Group by LotName
    //             var groupedOrders = orders.Items.GroupBy(o => o.LotName);
    //
    //             var LotName = string.Join(",", orders.Items.Select(o => o.LotName).Distinct());
    //             sheet.Cell(12, 10).Value = LotName;
    //             var BlockName = orders.BlkNo;
    //             sheet.Cell(12, 10).Value = BlockName;
    //
    //             sheet.Cell(9, 2).Value = orders.Builder;
    //             sheet.Cell(10, 2).Value = orders.Site;
    //             sheet.Cell(11, 2).Value = orders.City;
    //
    //             sheet.Cell(3, 13).Value = orders.OrderDate;
    //             sheet.Cell(6, 13).Value = orders.PurchaseOrder;
    //             sheet.Cell(9, 13).Value = orders.Company;
    //             sheet.Cell(12, 13).Value = orders.Contact;
    //
    //             foreach (var group in groupedOrders)
    //             {
    //                 sheet.Cell(currentRow, 2).Value = group.Key;
    //                 currentRow++;
    //
    //                 foreach (var order in group)
    //                 {
    //                     if (currentRow > endRow - 1)
    //                         sheet.Row(currentRow).InsertRowsBelow(1);
    //
    //                     sheet.Cell(currentRow, 2).Value = order.Quantity;
    //                     sheet.Cell(currentRow, 7).Value = order.FinishedLength;
    //                     sheet.Cell(currentRow, 8).Value = "X";
    //                     sheet.Cell(currentRow, 9).Value = order.FinishedWidth;
    //                     sheet.Cell(currentRow, 10).Value = order.Color;
    //                     sheet.Cell(currentRow, 11).Value = order.Type;
    //
    //                     sheet.Cell(currentRow, 3).FormulaA1 =
    //                         $"IF(OR(K{currentRow}=\"ROCK FACE\",K{currentRow}=\"ROCK FACE BUTT\",K{currentRow}=\"ROCK FACE 2L,1S\",K{currentRow}=\"ROCK FACE 2L\",K{currentRow}=\"ROCK FACE 1L,2S\"),EVEN(G{currentRow}+1),EVEN(G{currentRow}+2))";
    //
    //                     sheet.Cell(currentRow, 4).Value = "X";
    //
    //                     sheet.Cell(currentRow, 5).FormulaA1 =
    //                         $"IF(I{currentRow}<>G{currentRow},IF(OR(K{currentRow}=\"ROCK FACE\",K{currentRow}=\"ROCK FACE BUTT\",K{currentRow}=\"ROCK FACE 2L,1S\",K{currentRow}=\"ROCK FACE 2L\",K{currentRow}=\"ROCK FACE 1L,2S\"),I{currentRow}+1.5,I{currentRow}+2),C{currentRow})";
    //
    //                     sheet.Cell(currentRow, 12).FormulaA1 =
    //                         $"=B{currentRow}*C{currentRow}*E{currentRow}";
    //                     sheet.Cell(currentRow, 13).FormulaA1 =
    //                         $"=(G{currentRow}+1)*(I{currentRow}+1)*(B{currentRow}*0.21)";
    //
    //                     currentRow++;
    //                 }
    //             }
    //
    //             //   workbook.SaveAs(excelPath);
    //             workbook.SaveAs(excelPath);
    //             _logger.LogInformation($"saved to excel path:{excelPath}");
    //         }
    //
    //         // Find LibreOffice executable
    //         string libreOfficePath = FindLibreOfficeExecutable();
    //         if (string.IsNullOrEmpty(libreOfficePath))
    //         {
    //             throw new Exception("LibreOffice executable not found");
    //         }
    //
    //         var psi = new ProcessStartInfo
    //         {
    //             FileName = libreOfficePath,
    //             Arguments = $"--headless --convert-to pdf --outdir \"{tempDir}\" \"{excelPath}\"",
    //             WorkingDirectory = tempDir,
    //             RedirectStandardOutput = true,
    //             RedirectStandardError = true,
    //             UseShellExecute = false,
    //             CreateNoWindow = true
    //         };
    //
    //         // Set environment variables
    //         psi.EnvironmentVariables["HOME"] = "/tmp";
    //         psi.EnvironmentVariables["DISPLAY"] = ":99";
    //
    //         _logger.LogInformation($"Starting process: {libreOfficePath}");
    //
    //         using (var process = new Process { StartInfo = psi })
    //         {
    //             process.Start();
    //
    //             string stdout = process.StandardOutput.ReadToEnd();
    //             string stderr = process.StandardError.ReadToEnd();
    //             process.WaitForExit();
    //
    //             _logger.LogInformation($"LibreOffice stdout: {stdout}");
    //             if (!string.IsNullOrEmpty(stderr))
    //             {
    //                 _logger.LogWarning($"LibreOffice stderr: {stderr}");
    //             }
    //
    //             if (process.ExitCode != 0)
    //             {
    //                 throw new Exception(
    //                     $"LibreOffice process failed with exit code {process.ExitCode}. Stderr: {stderr}");
    //             }
    //
    //             if (!File.Exists(pdfPath))
    //             {
    //                 throw new Exception($"PDF file was not created at expected path: {pdfPath}");
    //             }
    //         }
    //
    //         _logger.LogInformation($"loading pdf from {pdfPath}");
    //         return File.ReadAllBytes(pdfPath);
    //     }
    //     finally
    //     {
    //         try
    //         {
    //             _logger.LogInformation($"deleting temp directory: {tempDir}");
    //             Directory.Delete(tempDir, true);
    //         }
    //         catch (Exception ex)
    //         {
    //             _logger.LogError(ex, "Error deleting temp directory");
    //         }
    //     }
    // }
    //
    
    public byte[] GenerateWorkOrderPdf(WorkOrderRequest4 orders)
    {
        LogSystemInfo();
        // 1. Create isolated temp folder
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        string excelPath = Path.Combine(tempDir, "workorder.xlsx");
        string pdfPath = Path.Combine(tempDir, "workorder.pdf");
        _logger.LogInformation($"excel:{excelPath}\n pdfPath:{pdfPath} ");
        try
        {
            // 2. Load template and fill data using ClosedXML
            using (var workbook = new XLWorkbook(_templatePath))
            {
                var sheet = workbook.Worksheet("WORK ORDER");

                int startRow = 19;
                int endRow = 50;
                int currentRow = startRow;

                // Group by LotName
                var groupedOrders = orders.Items.GroupBy(o => o.LotName);

                var LotName = string.Join(",", orders.Items.Select(o => o.LotName).Distinct());
                sheet.Cell(12, 10).Value = LotName;
                var BlockName = orders.BlkNo;
                sheet.Cell(12, 10).Value = BlockName;

                sheet.Cell(9, 2).Value = orders.Builder;
                sheet.Cell(10, 2).Value = orders.Site;
                sheet.Cell(11, 2).Value = orders.City;

                sheet.Cell(3, 13).Value = orders.OrderDate;
                sheet.Cell(6, 13).Value = orders.PurchaseOrder;
                sheet.Cell(9, 13).Value = orders.Company;
                sheet.Cell(12, 13).Value = orders.Contact;

                foreach (var group in groupedOrders)
                {
                    sheet.Cell(currentRow, 2).Value = group.Key;
                    currentRow++;

                    foreach (var order in group)
                    {
                        if (currentRow > endRow - 1)
                            sheet.Row(currentRow).InsertRowsBelow(1);

                        sheet.Cell(currentRow, 2).Value = order.Quantity;
                        sheet.Cell(currentRow, 7).Value = order.FinishedLength;
                        sheet.Cell(currentRow, 8).Value = "X";
                        sheet.Cell(currentRow, 9).Value = order.FinishedWidth;
                        sheet.Cell(currentRow, 10).Value = order.Color;
                        sheet.Cell(currentRow, 11).Value = order.Type;

                        sheet.Cell(currentRow, 3).FormulaA1 =
                            $"IF(OR(K{currentRow}=\"ROCK FACE\",K{currentRow}=\"ROCK FACE BUTT\",K{currentRow}=\"ROCK FACE 2L,1S\",K{currentRow}=\"ROCK FACE 2L\",K{currentRow}=\"ROCK FACE 1L,2S\"),EVEN(G{currentRow}+1),EVEN(G{currentRow}+2))";

                        sheet.Cell(currentRow, 4).Value = "X";

                        sheet.Cell(currentRow, 5).FormulaA1 =
                            $"IF(I{currentRow}<>G{currentRow},IF(OR(K{currentRow}=\"ROCK FACE\",K{currentRow}=\"ROCK FACE BUTT\",K{currentRow}=\"ROCK FACE 2L,1S\",K{currentRow}=\"ROCK FACE 2L\",K{currentRow}=\"ROCK FACE 1L,2S\"),I{currentRow}+1.5,I{currentRow}+2),C{currentRow})";

                        sheet.Cell(currentRow, 12).FormulaA1 =
                            $"=B{currentRow}*C{currentRow}*E{currentRow}";
                        sheet.Cell(currentRow, 13).FormulaA1 =
                            $"=(G{currentRow}+1)*(I{currentRow}+1)*(B{currentRow}*0.21)";

                        currentRow++;
                    }
                }

                //   workbook.SaveAs(excelPath);
                workbook.SaveAs(excelPath);
                _logger.LogInformation($"saved to excel path:{excelPath}");
            }

            // Find LibreOffice executable
            string libreOfficePath = "/usr/lib/libreoffice/program/soffice";
        
            var psi = new ProcessStartInfo
            {
                FileName = libreOfficePath,
                Arguments = $"--headless --convert-to pdf --outdir \"{tempDir}\" \"{excelPath}\"",
                WorkingDirectory = tempDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Set environment variables
            psi.EnvironmentVariables["HOME"] = "/tmp";

            _logger.LogInformation($"Starting LibreOffice process: {libreOfficePath}");
        
            using (var process = new Process { StartInfo = psi })
            {
                process.Start();
            
                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                _logger.LogInformation($"LibreOffice stdout: {stdout}");
                if (!string.IsNullOrEmpty(stderr))
                {
                    _logger.LogWarning($"LibreOffice stderr: {stderr}");
                }

                if (process.ExitCode != 0)
                {
                    throw new Exception($"LibreOffice process failed with exit code {process.ExitCode}. Stderr: {stderr}");
                }

                if (!File.Exists(pdfPath))
                {
                    throw new Exception($"PDF file was not created at expected path: {pdfPath}");
                }
            }

            _logger.LogInformation($"loading pdf from {pdfPath}");
            return File.ReadAllBytes(pdfPath);
        }
        finally
        {
            try
            {
                _logger.LogInformation($"deleting temp directory: {tempDir}");
                Directory.Delete(tempDir, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting temp directory");
            }
        }
    }
    private string FindLibreOfficeExecutable()
    {
        string[] possiblePaths = {
            "/usr/lib/libreoffice/program/soffice",  // The actual executable
            "/usr/bin/libreoffice",                  // Symlink
            "/usr/bin/soffice",                      // Another symlink
            "libreoffice",
            "soffice"
        };

        foreach (string path in possiblePaths)
        {
            try
            {
                // Use File.Exists for absolute paths, or try to execute for relative paths
                if (path.StartsWith("/"))
                {
                    if (File.Exists(path))
                    {
                        _logger.LogInformation($"Found LibreOffice at: {path}");
                        return path;
                    }
                }
                else
                {
                    // For relative paths, try executing with --version to test
                    var testProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = path,
                            Arguments = "--version",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                
                    testProcess.Start();
                    testProcess.WaitForExit(5000); // 5 second timeout
                
                    if (testProcess.ExitCode == 0)
                    {
                        _logger.LogInformation($"Found LibreOffice at: {path}");
                        return path;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Error checking path {path}: {ex.Message}");
            }
        }

        _logger.LogError("LibreOffice executable not found in any expected location");
        return null;
    }
    private string FindLibreOfficeExecutableDepeeee()
{
    string[] possiblePaths = {
        "/usr/bin/libreoffice",
        "/usr/bin/libreoffice7.4",
        "/usr/bin/libreoffice6.4",
        "/usr/bin/soffice",
        "/opt/libreoffice/program/soffice",
        "/snap/libreoffice/current/bin/libreoffice",
        "libreoffice",
        "soffice"
    };

    foreach (string path in possiblePaths)
    {
        try
        {
            if (File.Exists(path))
            {
                _logger.LogInformation($"Found LibreOffice at: {path}");
                return path;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"Error checking path {path}: {ex.Message}");
        }
    }

    // Try using 'which' command to find it
    try
    {
        var whichProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "which",
                Arguments = "libreoffice",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        
        whichProcess.Start();
        string result = whichProcess.StandardOutput.ReadToEnd().Trim();
        whichProcess.WaitForExit();
        
        if (whichProcess.ExitCode == 0 && !string.IsNullOrEmpty(result))
        {
            _logger.LogInformation($"Found LibreOffice using 'which': {result}");
            return result;
        }
    }
    catch (Exception ex)
    {
        _logger.LogDebug($"Error using 'which' command: {ex.Message}");
    }

    // Log available files for debugging
    try
    {
        var usrBinFiles = Directory.GetFiles("/usr/bin", "*libre*")
            .Concat(Directory.GetFiles("/usr/bin", "*office*"))
            .ToList();
        
        _logger.LogError($"Available office-related files in /usr/bin: {string.Join(", ", usrBinFiles)}");
    }
    catch (Exception ex)
    {
        _logger.LogError($"Could not list /usr/bin directory: {ex.Message}");
    }

    _logger.LogError("LibreOffice executable not found in any expected location");
    return null;
}
    private string FindLibreOfficeExecutableDep()
    {
        string[] possiblePaths =
        {
            "/usr/bin/libreoffice",
            "/usr/bin/libreoffice7.4",
            "/usr/bin/soffice",
            "libreoffice"
        };

        foreach (string path in possiblePaths)
        {
            try
            {
                if (File.Exists(path))
                {
                    _logger.LogInformation($"Found LibreOffice at: {path}");
                    return path;
                }
            }
            catch
            {
                // Continue to next path
            }
        }

        _logger.LogError("LibreOffice executable not found in any expected location");
        return null;
    }

    public byte[] GenerateWorkOrderPdf2(WorkOrderRequest4 orders)
    {
        using var workbook = new XLWorkbook(_templatePath);
        var sheet = workbook.Worksheet("WORK ORDER");

        int startRow = 19;
        int endRow = 50;
        int currentRow = startRow;

        // Group by LotName
        var groupedOrders = orders.Items.GroupBy(o => o.LotName);

        var LotName = string.Join(",", orders.Items.Select(o => o.LotName).Distinct());
        sheet.Cell(12, 10).Value = LotName;
        var BlockName = orders.BlkNo;
        sheet.Cell(12, 10).Value = BlockName;

        sheet.Cell(9, 2).Value = orders.Builder;
        sheet.Cell(10, 2).Value = orders.Site;
        sheet.Cell(11, 2).Value = orders.City;

        sheet.Cell(3, 13).Value = orders.OrderDate;
        sheet.Cell(6, 13).Value = orders.PurchaseOrder;
        sheet.Cell(9, 13).Value = orders.Company;
        sheet.Cell(12, 13).Value = orders.Contact;

        foreach (var group in groupedOrders)
        {
            sheet.Cell(currentRow, 2).Value = group.Key;
            currentRow++;

            foreach (var order in group)
            {
                if (currentRow > endRow - 1)
                    sheet.Row(currentRow).InsertRowsBelow(1);

                sheet.Cell(currentRow, 2).Value = order.Quantity;
                sheet.Cell(currentRow, 7).Value = order.FinishedLength;
                sheet.Cell(currentRow, 8).Value = "X";
                sheet.Cell(currentRow, 9).Value = order.FinishedWidth;
                sheet.Cell(currentRow, 10).Value = order.Color;
                sheet.Cell(currentRow, 11).Value = order.Type;

                sheet.Cell(currentRow, 3).FormulaA1 =
                    $"IF(OR(K{currentRow}=\"ROCK FACE\",K{currentRow}=\"ROCK FACE BUTT\",K{currentRow}=\"ROCK FACE 2L,1S\",K{currentRow}=\"ROCK FACE 2L\",K{currentRow}=\"ROCK FACE 1L,2S\"),EVEN(G{currentRow}+1),EVEN(G{currentRow}+2))";

                sheet.Cell(currentRow, 4).Value = "X";

                sheet.Cell(currentRow, 5).FormulaA1 =
                    $"IF(I{currentRow}<>G{currentRow},IF(OR(K{currentRow}=\"ROCK FACE\",K{currentRow}=\"ROCK FACE BUTT\",K{currentRow}=\"ROCK FACE 2L,1S\",K{currentRow}=\"ROCK FACE 2L\",K{currentRow}=\"ROCK FACE 1L,2S\"),I{currentRow}+1.5,I{currentRow}+2),C{currentRow})";

                sheet.Cell(currentRow, 12).FormulaA1 =
                    $"=B{currentRow}*C{currentRow}*E{currentRow}";
                sheet.Cell(currentRow, 13).FormulaA1 =
                    $"=(G{currentRow}+1)*(I{currentRow}+1)*(B{currentRow}*0.21)";

                currentRow++;
            }
        }

        // Save Excel to memory
        using var excelStream = new MemoryStream();
        workbook.SaveAs(excelStream);
        excelStream.Position = 0;

        // Convert Excel -> PDF in memory
        using var spireWorkbook = new Workbook();
        spireWorkbook.LoadFromStream(excelStream);

        using var pdfStream = new MemoryStream();
        spireWorkbook.SaveToStream(pdfStream, FileFormat.PDF);

        return pdfStream.ToArray();
    }

    public string GenerateWorkOrderExcelDep(WorkOrderRequest4 orders, string outputPath)
    {
        string excelPath = outputPath;
        string pdfPath = System.IO.Path.ChangeExtension(outputPath, ".pdf");


        using var workbook = new XLWorkbook(_templatePath);
        var sheet = workbook.Worksheet("WORK ORDER");

        // Assume row 5 is your "sample" row with formulas
        int startRow = 19;
        int endRow = 50;
        int currentRow = startRow;


        // Group by LotName
        var groupedOrders = orders.Items.GroupBy(o => o.LotName);

        var LotName = string.Join(",", orders.Items.Select(o => o.LotName).Distinct());
        sheet.Cell(12, 10).Value = LotName;
        var BlockName = orders.BlkNo; //string.Join(",",orders.Items.Select(o => o.LotName).Distinct());
        sheet.Cell(12, 10).Value = BlockName;


        sheet.Cell(9, 2).Value = orders.Builder;
        sheet.Cell(10, 2).Value = orders.Site;
        sheet.Cell(11, 2).Value = orders.City;


        sheet.Cell(3, 13).Value = orders.OrderDate;
        sheet.Cell(6, 13).Value = orders.PurchaseOrder;
        sheet.Cell(9, 13).Value = orders.Company;
        sheet.Cell(12, 13).Value = orders.Contact;


        foreach (var group in groupedOrders)
        {
            sheet.Cell(currentRow, 2).Value = group.Key;
            currentRow++;

            foreach (var order in group)
            {
                // Insert row (keep formulas structure)
                //if (currentRow > startRow+1)
                //    sheet.Row(currentRow).InsertRowsBelow(1);

                if (currentRow > endRow - 1)
                    sheet.Row(currentRow).InsertRowsBelow(1);

                // Fill data

                sheet.Cell(currentRow, 2).Value = order.Quantity;

                sheet.Cell(currentRow, 7).Value = order.FinishedLength;
                sheet.Cell(currentRow, 8).Value = "X";
                sheet.Cell(currentRow, 9).Value = order.FinishedWidth;


                sheet.Cell(currentRow, 10).Value = order.Color;
                sheet.Cell(currentRow, 11).Value = order.Type;

                // Copy formulas from template row (row 5)
                //var templateRow = sheet.Row(startRow);
                //for (int col = 7; col <= 10; col++) // assume formulas in columns 7–10
                //{
                //    sheet.Cell(currentRow, col).FormulaA1 = templateRow.Cell(col).FormulaA1;
                //}

                sheet.Cell(currentRow, 3).FormulaA1 =
                    $"IF(OR(K{currentRow}=\"ROCK FACE\",K{currentRow}=\"ROCK FACE BUTT\",K{currentRow}=\"ROCK FACE 2L,1S\",K{currentRow}=\"ROCK FACE 2L\",K{currentRow}=\"ROCK FACE 1L,2S\"),EVEN(G{currentRow}+1),EVEN(G{currentRow}+2))";

                sheet.Cell(currentRow, 4).Value = "X";

                sheet.Cell(currentRow, 5).FormulaA1 =
                    $"IF(I{currentRow}<>G{currentRow},IF(OR(K{currentRow}=\"ROCK FACE\",K{currentRow}=\"ROCK FACE BUTT\",K{currentRow}=\"ROCK FACE 2L,1S\",K{currentRow}=\"ROCK FACE 2L\",K{currentRow}=\"ROCK FACE 1L,2S\"),I{currentRow}+1.5,I{currentRow}+2),C{currentRow})";

                //area and weight
                sheet.Cell(currentRow, 12).FormulaA1 =
                    $"=B{currentRow}*C{currentRow}*E{currentRow}";
                sheet.Cell(currentRow, 13).FormulaA1 =
                    $"=(G{currentRow}+1)*(I{currentRow}+1)*(B{currentRow}*0.21)";

                currentRow++;
            }
        }

        workbook.SaveAs(outputPath);

        using (var spireWorkbook = new Workbook())
        {
            spireWorkbook.LoadFromFile(excelPath);
            spireWorkbook.SaveToFile(pdfPath, FileFormat.PDF);
        }

        //return pdfPath;
        return outputPath;
    }

    public string GenerateWorkOrderPdf(List<Order> orders, string pdfPath)
    {
        Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Text("Work Order Report").FontSize(18).Bold().AlignCenter();

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(80); // Lot
                            cols.ConstantColumn(60); // Qty
                            cols.ConstantColumn(80); // Length
                            cols.ConstantColumn(80); // Width
                            cols.ConstantColumn(80); // Color
                            cols.ConstantColumn(80); // Type
                        });

                        // Header row
                        table.Header(header =>
                        {
                            header.Cell().Text("Lot").Bold();
                            header.Cell().Text("Qty").Bold();
                            header.Cell().Text("Length").Bold();
                            header.Cell().Text("Width").Bold();
                            header.Cell().Text("Color").Bold();
                            header.Cell().Text("Type").Bold();
                        });

                        // Group by lot
                        foreach (var group in orders.GroupBy(o => o.LotName))
                        {
                            foreach (var order in group)
                            {
                                table.Cell().Text(order.LotName);
                                table.Cell().Text(order.Quantity.ToString());
                                table.Cell().Text(order.FinishedLength.ToString());
                                table.Cell().Text(order.FinishedWidth.ToString());
                                table.Cell().Text(order.Color);
                                table.Cell().Text(order.Type);
                            }
                        }
                    });

                    page.Footer().AlignRight().Text($"Generated {DateTime.Now}");
                });
            })
            .GeneratePdf(pdfPath);

        return pdfPath;
    }
}