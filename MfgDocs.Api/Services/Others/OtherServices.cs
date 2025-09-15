// using MfgDocs.Api.Models;
// using PuppeteerSharp.Media;
// using PuppeteerSharp;
// using System.Text;
//
// namespace MfgDocs.Api.Services.Others;
//
// using PuppeteerSharp;
// using System.Text;
//
// public class PdfGenerationService
// {
//     public async Task<byte[]> GeneratePdfFromHtmlAsync(string html, bool landscape = false)
//     {
//         var browserFetcher = new BrowserFetcher();
//         await browserFetcher.DownloadAsync();
//
//         await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
//         {
//             Headless = true,
//             Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
//         });
//
//         await using var page = await browser.NewPageAsync();
//         await page.SetContentAsync(html, new NavigationOptions
//         {
//             WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
//         });
//
//         var pdfOptions = new PdfOptions
//         {
//             Format = PaperFormat.A4,
//             PrintBackground = true,
//             Landscape = landscape,
//             MarginOptions = new MarginOptions
//             {
//                 Top = "10mm",
//                 Bottom = "10mm",
//                 Left = "10mm",
//                 Right = "10mm"
//             }
//         };
//
//         await page.PdfAsync($"sample{Guid.NewGuid()}.pdf", pdfOptions);
//         return new byte[0];
//
//     }
// }
//
// public interface IPourPlanService
// {
//     Task<byte[]> GeneratePourPlanPdfAsync(Models.PourPlanRequest3 request);
//     Task<byte[]> GenerateDayToDayPourPlanPdfAsync(List<Models.PourPlanDay> days);
// }
//
// public interface IWorkOrderService
// {
//     Task<byte[]> GenerateWorkOrderPdfAsync(Models.WorkOrderRequest3 request);
// }
//
// //public class WorkOrderService : IWorkOrderService
// //{
// //    public async Task<byte[]> GenerateWorkOrderPdfAsync(WorkOrderRequest3 request)
// //    {
// //        var html = GenerateWorkOrderHtml(request);
//
// //        /*using*/
// //        var browserFetcher = new BrowserFetcher();
// //        await browserFetcher.DownloadAsync();
//
// //        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
// //        {
// //            Headless = true
// //        });
//
// //        await using var page = await browser.NewPageAsync();
// //        await page.SetContentAsync(html);
//
// //        var pdfBytes = new byte[0];
//
// //        await page.PdfAsync($"NewPDF {Guid.NewGuid()}.pdf", new PdfOptions
// //        {
// //            Format = PaperFormat.A4,
// //            PrintBackground = true,
// //            MarginOptions = new MarginOptions
// //            {
// //                Top = "10mm",
// //                Bottom = "10mm",
// //                Left = "10mm",
// //                Right = "10mm"
// //            }
// //        });
//
// //        // return html.ToString();
//
// //        return pdfBytes;
// //    }
// //    public string GenerateWorkOrderHtml(WorkOrderRequest3 request)
// //    {
// //        var html = new StringBuilder();
//
// //        html.Append(@"
// //<!DOCTYPE html>
// //<html>
// //<head>
// //    <meta charset='UTF-8'>
// //    <style>
// //        * { margin: 0; padding: 0; box-sizing: border-box; }
// //        body { font-family: Arial, sans-serif; font-size: 10px; }
// //        .container { width: 100%; max-width: 800px; margin: 0 auto; }
// //        .header { display: flex; justify-content: space-between; align-items: center; padding: 10px; border-bottom: 2px solid #000; }
// //        .logo { display: flex; align-items: center; }
// //        .logo-icon { width: 30px; height: 30px; background: #ff6b6b; margin-right: 10px; }
// //        .company-name { font-size: 16px; font-weight: bold; color: #666; }
// //        .company-details { font-size: 8px; color: #666; line-height: 1.2; }
// //        .work-order-header { text-align: center; background: #ffb3b3; padding: 5px; font-weight: bold; }
// //        .order-info { display: flex; justify-content: space-between; padding: 5px 0; }
// //        .order-info-left, .order-info-right { width: 48%; }
// //        .info-box { background: #e6e6e6; padding: 3px 5px; margin: 2px 0; font-size: 9px; }
// //        .info-box-header { background: #999; color: white; padding: 3px 5px; font-weight: bold; }
// //        .table-container { margin: 10px 0; }
// //        .main-table { width: 100%; border-collapse: collapse; border: 2px solid #000; }
// //        .main-table th { background: #ffb3b3; padding: 5px; border: 1px solid #000; font-size: 8px; text-align: center; }
// //        .main-table td { padding: 4px; border: 1px solid #000; text-align: center; font-size: 8px; }
// //        .lot-header { background: #e6e6e6; font-weight: bold; }
// //        .red-text { color: red; }
// //        .notes-section { margin-top: 10px; }
// //        .notes-label { color: red; font-weight: bold; }
// //        .total-section { display: flex; justify-content: space-between; align-items: center; margin-top: 10px; border-top: 1px solid #000; padding-top: 5px; }
// //        .expected-delivery { font-size: 9px; }
// //    </style>
// //</head>
// //<body>
// //    <div class='container'>
// //        <div class='header'>
// //            <div>
// //                <div class='logo'>
// //                    <div class='logo-icon'></div>
// //                    <div>
// //                        <div class='company-name'>MFG PRECAST</div>
// //                        <div class='company-details'>
// //                            PO Box 71071, Maplelawn<br>
// //                            Burlington, ON L7T 2E0<br>
// //                            Phone: (905) 643 114 (905) 469 1119<br>
// //                            Email: info@mfgprecast.com<br>
// //                            www.mfgprecast.com
// //                        </div>
// //                    </div>
// //                </div>
// //            </div>
// //            <div class='work-order-header'>WORK ORDER</div>
// //        </div>
//
// //        <div class='order-info'>
// //            <div class='order-info-left'>
// //                <div class='info-box-header'>BUILDER / SITE / CITY</div>
// //                <div class='info-box'>" + request.BuilderSiteCity + @"</div>
//
// //                <div class='info-box-header' style='margin-top: 10px;'>BLK NO.</div>
// //                <div class='info-box'>" + request.BlkNo + @"</div>
//
// //                <div class='info-box-header' style='margin-top: 10px;'>LOT NO.</div>
// //                <div class='info-box'>" + request.LotNo + @"</div>
// //            </div>
// //            <div class='order-info-right'>
// //                <div style='background: #ffb3b3; padding: 3px 5px; margin: 2px 0; text-align: center;'>ORDER DATE</div>
// //                <div style='text-align: center; padding: 5px;'>" + request.OrderDate + @"</div>
//
// //                <div style='background: #b3d9ff; padding: 3px 5px; margin: 2px 0; text-align: center;'>PURCHASE ORDER</div>
// //                <div style='text-align: center; padding: 5px;'>" + request.PurchaseOrder + @"</div>
//
// //                <div style='background: #b3d9ff; padding: 3px 5px; margin: 2px 0; text-align: center;'>COMPANY</div>
// //                <div style='text-align: center; padding: 5px;'>" + request.Company + @"</div>
//
// //                <div style='background: #b3d9ff; padding: 3px 5px; margin: 2px 0; text-align: center;'>CONTACT</div>
// //                <div style='text-align: center; padding: 5px;'>" + request.Contact + @"</div>
// //            </div>
// //        </div>
//
// //        <div class='table-container'>
// //            <table class='main-table'>
// //                <thead>
// //                    <tr style='background: #ffb3b3;'>
// //                        <th>QTY</th>
// //                        <th colspan='2'>POURED SIZE</th>
// //                        <th colspan='2'>FINISHED SIZE</th>
// //                        <th>TYPE</th>
// //                        <th>AREA<br>(SQ.M)</th>
// //                        <th>WEIGHT<br>(LBS)</th>
// //                    </tr>
// //                    <tr style='background: #ffb3b3;'>
// //                        <th></th>
// //                        <th>WIDTH</th>
// //                        <th>LENGTH</th>
// //                        <th>WIDTH</th>
// //                        <th>LENGTH</th>
// //                        <th>COLOR</th>
// //                        <th></th>
// //                        <th></th>
// //                    </tr>
// //                </thead>
// //                <tbody>");
//
// //        var currentLot = "";
// //        foreach (var item in request.Items)
// //        {
// //            if (currentLot != item.LotNo)
// //            {
// //                currentLot = item.LotNo;
// //                html.Append($@"
// //                    <tr class='lot-header'>
// //                        <td colspan='8' style='text-align: left; font-weight: bold;'>{item.LotNo}</td>
// //                    </tr>");
// //            }
//
// //            var colorClass = item.Color.ToUpper().Contains("RED") || item.Color.ToUpper().Contains("NEW WHITE") ? "red-text" : "";
//
// //            html.Append($@"
// //                <tr>
// //                    <td>{item.Quantity}</td>
// //                    <td>({item.PouredSize.Split('x')[0].Trim()})</td>
// //                    <td>{(item.PouredSize.Contains('x') ? item.PouredSize.Split('x')[1].Trim() : "")}</td>
// //                    <td>{item.Width}</td>
// //                    <td>{item.Length}</td>
// //                    <td class='{colorClass}'>{item.Color}</td>
// //                    <td class='{colorClass}'>{item.Type}</td>
// //                    <td>{item.Area}</td>
// //                    <td>{item.Weight}</td>
// //                </tr>");
// //        }
//
// //        html.Append(@"
// //                </tbody>
// //            </table>
// //        </div>
//
// //        <div class='notes-section'>
// //            <span class='notes-label'>NOTES:</span>
// //            <span style='margin-left: 10px;'>" + request.Notes + @"</span>
// //        </div>
//
// //        <div class='total-section'>
// //            <div class='expected-delivery'>
// //                <strong>EXPECTED DELIVERY DATE IS BEFORE:</strong> " + request.ExpectedDeliveryDate + @"
// //            </div>
// //            <div style='font-weight: bold;'>
// //                TOTAL WEIGHT: " + request.Items.Sum(i => int.TryParse(i.Weight, out int w) ? w : 0) + @"
// //            </div>
// //        </div>
// //    </div>
// //</body>
// //</html>");
//
// //        return html.ToString();
// //    }
// //}
// public class EnhancedWorkOrderService : IWorkOrderService
// {
//     private readonly PdfGenerationService _pdfService;
//
//     public EnhancedWorkOrderService()
//     {
//         _pdfService = new PdfGenerationService();
//     }
//
//     public async Task<byte[]> GenerateWorkOrderPdfAsync(WorkOrderRequest3 request)
//     {
//         var html = GenerateEnhancedWorkOrderHtml(request);
//         return await _pdfService.GeneratePdfFromHtmlAsync(html);
//     }
//
//     private string GenerateEnhancedWorkOrderHtml(WorkOrderRequest3 request)
//     {
//         var totalWeight = request.Items.Sum(i => int.TryParse(i.Weight, out int w) ? w : 0);
//         var html = new StringBuilder();
//         var builderAddress = string.Join("<br>", request.BuilderSiteCity.Split('\n'));
//
//         html.Append($@"<!DOCTYPE html>
// <html lang=""en"">
// <head>
//     <meta charset=""UTF-8"">
//     <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
//     <title>MFG Precast Work Order</title>
//     <link href=""http://fonts.googleapis.com/css?family=Open+Sans:300,400,600,700"" rel=""stylesheet"" />
//     <style>
//         body {{
//             font-family: serif;
//             margin: 0;
//             padding: 20px;
//             background: white;
//         }}
//
//         .form-container {{
//             border: 2px solid black;
//             width: 8.5in;
//             height: 11in;
//             margin: 0 auto;
//             background: white;
//             position: relative;
//         }}
//
//         .header {{
//             display: flex;
//             justify-content: space-between;
//             padding: 10px;
//         }}
//         .middle {{
//             display: flex;
//             justify-content: space-between;
//             padding: 10px;
//             border-bottom: 5px solid black;
//         }}
//
//         .company-info {{
//             flex: 1;
//         }}
//
//         .company-logo {{
//             color: dimgrey;
//             font-weight: bold;
//             font-size: 18px;
//             margin-bottom: 15px;
//             margin-left: 15px;
//         }}
// /*
//         .company-details {{
//             font-size: 10px;
//             line-height: 1.3;
//             color: #333;
//         }}*/
//
//         .work-order-section {{
//             width: 200px;
//             text-align: center;
//         }}
//         .box-section {{
//             width: 200px;
//             text-align: center;
//         }}
//
//         .work-order-title {{
//             
//             color: grey;
//             padding: 8px;
//             font-weight: bold;
//             font-size: 22.5px;
//         }}
//         .order-date {{
//             background: #d89795;
//             color: white;
//             padding: 8px;
//             font-weight: bold;
//             font-size: 10px;
//         }}
//
//         .date-section {{
//             background: #e8e8e8;
//             padding: 5px;
//             font-size: 11px;
//             text-align: right;
//         }}
//
//         .order-details {{
//             background: #b8cce5;
//             padding: 8px;
//             text-align: center;
//         }}
//
//         .purchase-order {{
//             font-size: 11px;
//             margin-bottom: 3px;
//         }}
//
//         .order-number {{
//             font-weight: bold;
//             font-size: 14px;
//             margin-bottom: 8px;
//         }}
//
//         .company-name {{
//             background: #e8e8e8;
//             padding: 3px;
//             font-size: 11px;
//             margin-bottom: 3px;
//         }}
//
//         .legacy-text {{
//             font-size: 10px;
//         }}
//
//         .contact-section {{
//             background: #b8cce5;
//             padding: 5px;
//             text-align: center;
//             font-size: 10px;
//         }}
//
//         .info-row {{
//             display: flex;
//             border-bottom: 1px solid black;
//         }}
//
//         .info-left {{
//             width: 60%;
//             border-right: 1px solid black;
//             padding: 5px 10px;
//         }}
//
//         .info-right {{
//             width: 40%;
//             padding: 5px 10px;
//         }}
//
//         .field-header {{
//             background: #d0d0d0;
//             font-weight: bold;
//             font-size: 10px;
//             padding: 2px 5px;
//             margin-bottom: 2px;
//         }}
//
//         .field-content {{
//             font-size: 11px;
//             line-height: 1.2;
//             background-color: #f2f2f2;
//         }}
//
//         .table-container {{
//             margin-top: 0;
//         }}
//
//         .description-header {{
//             background: #e8e8e8;
//             text-align: center;
//             padding: 8px;
//             font-weight: bold;
//             font-size: 12px;
//             border-bottom: 1px solid black;
//         }}
//
//         .table-headers {{
//             display: flex;
//             background: #e8e8e8;
//             border-bottom: 1px solid black;
//             font-size: 9px;
//             font-weight: bold;
//             text-align: center;
//         }}
//
//         .col-qty {{
//             width: 70px;
//         }}
//
//         .col-power {{
//             width: 130px;
//         }}
//
//         .col-width {{
//             width: 70px;
//         }}
//
//         .col-length {{
//             width: 70px;
//         }}
//
//         .col-finished {{
//             width: 110px;
//         }}
//
//         .col-color {{
//             width: 230px;
//         }}
//
//         .col-type {{
//             width: 90px;
//         }}
//
//         .col-area {{
//             width: 30px;
//         }}
//
//         .col-weight {{
//             width: 30px;
//         }}
//
//         .table-headers > div {{
//             padding: 5px 2px;
//             border-right: 1px solid black;
//         }}
//
//             .table-headers > div:last-child {{
//                 border-right: none;
//             }}
//
//         .table-row {{
//             display: flex;
//             border-bottom: 1px solid #ccc;
//             font-size: 10px;
//             min-height: 25px;
//             align-items: center;
//         }}
//
//             .table-row > div {{
//                 padding: 3px 2px;
//                 border-right: 1px solid #ccc;
//                 text-align: center;
//             }}
//
//                 .table-row > div:last-child {{
//                     border-right: none;
//                 }}
//
//         .lot-number {{
//             font-weight: bold;
//             background: #f5f5f5;
//         }}
//
//         .red-text {{
//             color: red;
//         }}
//
//         .bottom-section {{
//             position: absolute;
//             bottom: 20px;
//             left: 10px;
//             right: 10px;
//         }}
//
//         .notes-section {{
//             display: flex;
//             border-top: 1px solid black;
//             border-bottom: 1px solid black;
//         }}
//
//         .notes-label {{
//             background: #e8e8e8;
//             padding: 5px 10px;
//             font-weight: bold;
//             font-size: 10px;
//             border-right: 1px solid black;
//             width: 80px;
//         }}
//
//         .notes-content {{
//             padding: 5px 10px;
//             font-size: 11px;
//             flex: 1;
//             color: red;
//             font-weight: bold;
//         }}
//
//         .delivery-section {{
//             display: flex;
//             justify-content: space-between;
//             align-items: center;
//             padding: 10px;
//             font-size: 11px;
//         }}
//
//         .total-weight {{
//             text-align: center;
//             font-weight: bold;
//         }}
//
//         .company-name {{
//             color: #e74c3c;
//             font-weight: bold;
//             font-size: 16px;
//         }}
//
//         .company-details {{
//             font-size: 10px;
//             line-height: 1.3; 
//             color: black;
//         }}
//         .company-section {{
//             width: 50%;
//             padding: 8px;
//              
//         }}
//
//         .logo-row {{
//             display: flex;
//             align-items: center;
//             margin-bottom: 5px;
//         }}
//         .house-icon {{
//             width: 18px;
//             height: 12px;
//             background: #e74c3c;
//             margin-right: 5px;
//             position: relative;
//         }}
//
//             .house-icon::before {{
//                 content: '';
//                 position: absolute;
//                 top: -6px;
//                 left: 50%;
//                 transform: translateX(-50%);
//                 width: 0;
//                 height: 0;
//                 border-left: 9px solid transparent;
//                 border-right: 9px solid transparent;
//                 border-bottom: 8px solid #e74c3c;
//             }}
//
//     </style>
// </head>
// <body>
//     <div class=""form-container"">
//         
//         <div class=""header"">
//             <div class=""company-section"">
//                 <div class=""logo-row"">
//                     <div class=""house-icon""></div>
//                     <div class=""company-name"">MFG PRECAST</div>
//                 </div>
//                 <div class=""company-details"">
//                     PO Box 76874, Mississauga<br>
//                     Burlington, ON L7T 4M0<br>
//                     Phone: (905) 632-214, (905) 588-1110<br>
//                     Email: info@mfgprecast.com<br>
//                                               <a href=""https://www.mfgprecast.com"">www.mfgprecast.com</a>
//                 </div>
//             </div>
//             <div class=""work-order-section"">
//                 <div class=""work-order-title"">WORK ORDER</div>
//                 <div class=""order-date"">ORDER DATE</div>
//                 <div class=""date-section"">
//
//                 <div class=""date-section"">{request.OrderDate}</div>
//                 <div class=""field-header"" style=""margin-top:20px;background:#b8cce5;"">PURCHASE ORDER</div>
//                 <div class=""field-content"">{request.PurchaseOrder}</div>
//             </div>
//         </div>
//
//         <!-- Middle section -->
//         <div class=""middle"">
//             <div class=""work-order-section"">
//                 <div class=""field-header"">BUILDER / SITE / CITY</div>
//                 <div class=""field-content"">{builderAddress}</div>
//             </div>
//             <div class=""work-order-section"">
//                 <div class=""field-header"">BLK NO.</div>
//                 <div class=""field-content"">{request.BlkNo}</div>
//                 <div class=""field-header"" style=""margin-top:20px;"">LOT NO.</div>
//                 <div class=""field-content"">{request.LotNo}</div>
//             </div>
//             <div class=""work-order-section"">
//                 <div class=""field-header"" style=""background:#b8cce5;"">COMPANY</div>
//                 <div class=""field-content"">{request.Company}</div> 
//                 <div class=""field-header"" style=""margin-top:20px;background:#b8cce5;"">CONTACT</div>
//                 <div class=""field-content"">{request.Contact}</div>
//             </div>
//         </div>
//
//         <!-- Table Header -->
//         <div class=""table-container"">
//             <div class=""description-header"">DESCRIPTION</div>
//             <div class=""table-headers"">
//                 <div class=""col-qty"">QTY</div>
//                 <div class=""col-power"">POWER SIZE<br>L x W</div>
//                 <div class=""col-width"">WIDTH</div>
//                 <div class=""col-length"">LENGTH</div>
//                 <div class=""col-finished"">FINISHED SIZE<br>COLOR</div>
//                 <div class=""col-color"">TYPE</div>
//                 <div class=""col-area"">AREA<br>(SQ.M)</div>
//                 <div class=""col-weight"">WEIGHT<br>(LBS)</div>
//             </div>");
//
//         string currentLot = null;
//         StringBuilder qtyStr = null, powerStr = null, widthStr = null, lengthStr = null, finishedStr = null, areaStr = null, weightStr = null, typeStr = null;
//
//         void FlushLot()
//         {
//             if (qtyStr == null) return;
//             html.Append($@"
//             <div class=""table-row"">
//                 {qtyStr}</div>
//                 {powerStr}</div>
//                 {widthStr}</div>
//                 {lengthStr}</div>
//                 {finishedStr}</div>
//                 {typeStr}</div>
//                 {areaStr}</div>
//                 {weightStr}</div>
//             </div>");
//         }
//
//         foreach (var item in request.Items)
//         {
//             var colorClass = (item.Color?.ToUpper().Contains("NEW WHITE") == true ||
//                               item.Type?.ToUpper().Contains("SMOOTH FACE") == true)
//                               ? "red-text" : "";
//
//             if (currentLot != item.LotNo)
//             {
//                 FlushLot(); // flush previous lot
//                 currentLot = item.LotNo;
//                 qtyStr = new StringBuilder($@"<div class=""col-qty lot-number"">{item.LotNo}<br>");
//                 powerStr = new StringBuilder(@"<div class=""col-power"">");
//                 widthStr = new StringBuilder(@"<div class=""col-width"">");
//                 lengthStr = new StringBuilder(@"<div class=""col-length"">");
//                 finishedStr = new StringBuilder(@"<div class=""col-finished"">");
//                 typeStr = new StringBuilder(@"<div class=""col-color"">");
//                 areaStr = new StringBuilder(@"<div class=""col-area"">");
//                 weightStr = new StringBuilder(@"<div class=""col-weight"">");
//             }
//
//             qtyStr.Append($@"<span class=""{colorClass}"">{item.Quantity}</span><br>");
//             powerStr.Append($@"<span class=""{colorClass}"">{item.PouredSize}</span><br>");
//             widthStr.Append($@"<span class=""{colorClass}"">{item.Width}</span><br>");
//             lengthStr.Append($@"<span class=""{colorClass}"">{item.Length}</span><br>");
//             finishedStr.Append($@"<span class=""{colorClass}"">{item.Color}</span><br>");
//             typeStr.Append($@"<span class=""{colorClass}"">{item.Type}</span><br>");
//             areaStr.Append($@"<span class=""{colorClass}"">{item.Area}</span><br>");
//             weightStr.Append($@"<span class=""{colorClass}"">{item.Weight}</span><br>");
//         }
//
//         // flush last lot
//         FlushLot();
//
//         html.Append($@"
//         <!-- Empty rows -->
//         <div class=""table-row"" style=""height:100px;""><div class=""col-qty""></div><div class=""col-power""></div><div class=""col-width""></div><div class=""col-length""></div><div class=""col-finished""></div><div class=""col-color""></div><div class=""col-type""></div><div class=""col-area""></div><div class=""col-weight""></div></div>
//     </div>
//
//     <div class=""bottom-section"">
//         <div class=""notes-section"">
//             <div class=""notes-label"">NOTES:</div>
//             <div class=""notes-content"">{request.Notes}</div>
//         </div>
//         <div class=""delivery-section"">
//             <div>EXPECTED DELIVERY DATE(S) BEFORE:</div>
//             <div class=""total-weight"">TOTAL WEIGHT:</div>
//             <div style=""font-weight:bold;"">{totalWeight}</div>
//         </div>
//         <div style=""text-align:center;font-size:11px;margin-top:5px;"">
//             {request.ExpectedDeliveryDate:dd MMM yyyy (dddd)}
//         </div>
//     </div>
// </div>
// </body>
// </html>");
//
//         return html.ToString();
//     }
//
//     //    private string GenerateEnhancedWorkOrderHtml(WorkOrderRequest3 request)
//     //    {
//     //        var totalWeight = request.Items.Sum(i => int.TryParse(i.Weight, out int w) ? w : 0);
//     //        var html = new StringBuilder(); // Missing StringBuilder declaration
//     //        var builderAddress = string.Join("<br>", request.BuilderSiteCity.Split(','));
//     //        html.Append($@"<!DOCTYPE html>
//     //<html lang=""en"">
//     //<head>
//     //    <meta charset=""UTF-8"">
//     //    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
//     //    <title>MFG Precast Work Order</title>
//     //    <style>
//     //        body {{
//     //            font-family: serif;
//     //            margin: 0;
//     //            padding: 20px;
//     //            background: white;
//     //        }}
//
//     //        .form-container {{
//     //            border: 2px solid black;
//     //            width: 8.5in;
//     //            height: 11in;
//     //            margin: 0 auto;
//     //            background: white;
//     //            position: relative;
//     //        }}
//
//     //        .header {{
//     //            display: flex;
//     //            justify-content: space-between;
//     //            padding: 10px;
//     //        }}
//     //        .middle {{
//     //            display: flex;
//     //            justify-content: space-between;
//     //            padding: 10px;
//     //            border-bottom: 5px solid black;
//     //        }}
//
//     //        .company-info {{
//     //            flex: 1;
//     //        }}
//
//     //        .company-logo {{
//     //            color: dimgrey;
//     //            font-weight: bold;
//     //            font-size: 18px;
//     //            margin-bottom: 15px;
//     //            margin-left: 15px;
//     //        }}
//     ///*
//     //        .company-details {{
//     //            font-size: 10px;
//     //            line-height: 1.3;
//     //            color: #333;
//     //        }}*/
//
//     //        .work-order-section {{
//     //            width: 200px;
//     //            text-align: center;
//     //        }}
//     //        .box-section {{
//     //            width: 200px;
//     //            text-align: center;
//     //        }}
//
//     //        .work-order-title {{
//
//     //            color: grey;
//     //            padding: 8px;
//     //            font-weight: bold;
//     //            font-size: 22.5px;
//     //        }}
//     //        .order-date {{
//     //            background: #d89795;
//     //            color: white;
//     //            padding: 8px;
//     //            font-weight: bold;
//     //            font-size: 10px;
//     //        }}
//
//     //        .date-section {{
//     //            background: #e8e8e8;
//     //            padding: 5px;
//     //            font-size: 11px;
//     //            text-align: right;
//     //        }}
//
//     //        .order-details {{
//     //            background: #b8cce5;
//     //            padding: 8px;
//     //            text-align: center;
//     //        }}
//
//     //        .purchase-order {{
//     //            font-size: 11px;
//     //            margin-bottom: 3px;
//     //        }}
//
//     //        .order-number {{
//     //            font-weight: bold;
//     //            font-size: 14px;
//     //            margin-bottom: 8px;
//     //        }}
//
//     //        .company-name {{
//     //            background: #e8e8e8;
//     //            padding: 3px;
//     //            font-size: 11px;
//     //            margin-bottom: 3px;
//     //        }}
//
//     //        .legacy-text {{
//     //            font-size: 10px;
//     //        }}
//
//     //        .contact-section {{
//     //            background: #b8cce5;
//     //            padding: 5px;
//     //            text-align: center;
//     //            font-size: 10px;
//     //        }}
//
//     //        .info-row {{
//     //            display: flex;
//     //            border-bottom: 1px solid black;
//     //        }}
//
//     //        .info-left {{
//     //            width: 60%;
//     //            border-right: 1px solid black;
//     //            padding: 5px 10px;
//     //        }}
//
//     //        .info-right {{
//     //            width: 40%;
//     //            padding: 5px 10px;
//     //        }}
//
//     //        .field-header {{
//     //            background: #d0d0d0;
//     //            font-weight: bold;
//     //            font-size: 10px;
//     //            padding: 2px 5px;
//     //            margin-bottom: 2px;
//     //        }}
//
//     //        .field-content {{
//     //            font-size: 11px;
//     //            line-height: 1.2;
//     //            background-color: #f2f2f2;
//     //        }}
//
//     //        .table-container {{
//     //            margin-top: 0;
//     //        }}
//
//     //        .description-header {{
//     //            background: #e8e8e8;
//     //            text-align: center;
//     //            padding: 8px;
//     //            font-weight: bold;
//     //            font-size: 12px;
//     //            border-bottom: 1px solid black;
//     //        }}
//
//     //        .table-headers {{
//     //            display: flex;
//     //            background: #e8e8e8;
//     //            border-bottom: 1px solid black;
//     //            font-size: 9px;
//     //            font-weight: bold;
//     //            text-align: center;
//     //        }}
//
//     //        .col-qty {{
//     //            width: 70px;
//     //        }}
//
//     //        .col-power {{
//     //            width: 130px;
//     //        }}
//
//     //        .col-width {{
//     //            width: 70px;
//     //        }}
//
//     //        .col-length {{
//     //            width: 70px;
//     //        }}
//
//     //        .col-finished {{
//     //            width: 110px;
//     //        }}
//
//     //        .col-color {{
//     //            width: 230px;
//     //        }}
//
//     //        .col-type {{
//     //            width: 90px;
//     //        }}
//
//     //        .col-area {{
//     //            width: 30px;
//     //        }}
//
//     //        .col-weight {{
//     //            width: 30px;
//     //        }}
//
//     //        .table-headers > div {{
//     //            padding: 5px 2px;
//     //            border-right: 1px solid black;
//     //        }}
//
//     //            .table-headers > div:last-child {{
//     //                border-right: none;
//     //            }}
//
//     //        .table-row {{
//     //            display: flex;
//     //            border-bottom: 1px solid #ccc;
//     //            font-size: 10px;
//     //            min-height: 25px;
//     //            align-items: center;
//     //        }}
//
//     //            .table-row > div {{
//     //                padding: 3px 2px;
//     //                border-right: 1px solid #ccc;
//     //                text-align: center;
//     //            }}
//
//     //                .table-row > div:last-child {{
//     //                    border-right: none;
//     //                }}
//
//     //        .lot-number {{
//     //            font-weight: bold;
//     //            background: #f5f5f5;
//     //        }}
//
//     //        .red-text {{
//     //            color: red;
//     //        }}
//
//     //        .bottom-section {{
//     //            position: absolute;
//     //            bottom: 20px;
//     //            left: 10px;
//     //            right: 10px;
//     //        }}
//
//     //        .notes-section {{
//     //            display: flex;
//     //            border-top: 1px solid black;
//     //            border-bottom: 1px solid black;
//     //        }}
//
//     //        .notes-label {{
//     //            background: #e8e8e8;
//     //            padding: 5px 10px;
//     //            font-weight: bold;
//     //            font-size: 10px;
//     //            border-right: 1px solid black;
//     //            width: 80px;
//     //        }}
//
//     //        .notes-content {{
//     //            padding: 5px 10px;
//     //            font-size: 11px;
//     //            flex: 1;
//     //            color: red;
//     //            font-weight: bold;
//     //        }}
//
//     //        .delivery-section {{
//     //            display: flex;
//     //            justify-content: space-between;
//     //            align-items: center;
//     //            padding: 10px;
//     //            font-size: 11px;
//     //        }}
//
//     //        .total-weight {{
//     //            text-align: center;
//     //            font-weight: bold;
//     //        }}
//
//     //        .company-name {{
//     //            color: #e74c3c;
//     //            font-weight: bold;
//     //            font-size: 16px;
//     //        }}
//
//     //        .company-details {{
//     //            font-size: 10px;
//     //            line-height: 1.3; 
//     //            color: black;
//     //        }}
//     //        .company-section {{
//     //            width: 50%;
//     //            padding: 8px;
//
//     //        }}
//
//     //        .logo-row {{
//     //            display: flex;
//     //            align-items: center;
//     //            margin-bottom: 5px;
//     //        }}
//     //        .house-icon {{
//     //            width: 18px;
//     //            height: 12px;
//     //            background: #e74c3c;
//     //            margin-right: 5px;
//     //            position: relative;
//     //        }}
//
//     //            .house-icon::before {{
//     //                content: '';
//     //                position: absolute;
//     //                top: -6px;
//     //                left: 50%;
//     //                transform: translateX(-50%);
//     //                width: 0;
//     //                height: 0;
//     //                border-left: 9px solid transparent;
//     //                border-right: 9px solid transparent;
//     //                border-bottom: 8px solid #e74c3c;
//     //            }}
//
//     //    </style>
//     //</head>
//     //<body>
//     //    <div class=""form-container"">
//
//     //        <div class=""header"">
//     //            <div class=""company-section"">
//     //                <div class=""logo-row"">
//     //                    <div class=""house-icon""></div>
//     //                    <div class=""company-name"">MFG PRECAST</div>
//     //                </div>
//     //                <div class=""company-details"">
//     //                    PO Box 76874, Mississauga<br>
//     //                    Burlington, ON L7T 4M0<br>
//     //                    Phone: (905) 632-214, (905) 588-1110<br>
//     //                    Email: info@mfgprecast.com<br>
//     //                    <a href=""https://www.mfgprecast.com"">www.mfgprecast.com</a>
//     //                </div>
//     //            </div>
//     //            <div class=""work-order-section"">
//     //                <div class=""work-order-title"">WORK ORDER</div>
//     //                <div class=""order-date"">ORDER DATE</div>
//     //                <div class=""date-section"">
//     //                    {request.OrderDate}
//     //                </div>
//
//     //                <div class=""field-header"" style=""margin-top: 20px; background: #b8cce5;"">PURCHASE ORDER</div>
//     //                <div class=""field-content"">{request.PurchaseOrder}</div>
//
//
//     //            </div>
//     //        </div>
//
//     //        <div class=""middle"">
//     //            <div class=""work-order-section"">
//     //                <div class=""field-header"">BUILDER / SITE / CITY</div>
//     //                <div class=""field-content"">
//     //                    {builderAddress}
//     //                </div>
//     //            </div>
//     //            <div class=""work-order-section"">
//     //                <div class=""field-header"">BLK NO.</div>
//     //                <div class=""field-content"">{request.BlkNo}</div>
//     //                <div class=""field-header"" style=""margin-top: 20px;"">LOT NO.</div>
//     //                <div class=""field-content"">{request.LotNo}</div>
//     //            </div>
//
//
//     //            <div class=""work-order-section"">
//
//     //                <div class=""field-header"" style=""background: #b8cce5;"">COMPANY</div>
//     //                <div class=""field-content"">{request.Company}</div> 
//     //                <div class=""field-header"" style=""margin-top: 20px; background: #b8cce5;"">CONTACT</div>
//     //                <div class=""field-content"">{request.Contact}</div>
//
//     //            </div>
//     //        </div>
//
//     //        <div class=""table-container"">
//     //            <div class=""description-header"">DESCRIPTION</div>
//
//     //            <div class=""table-headers"">
//     //                <div class=""col-qty"">QTY</div>
//     //                <div class=""col-power"">POWER SIZE<br>L x W</div>
//     //                <div class=""col-width"">WIDTH</div>
//     //                <div class=""col-length"">LENGTH</div>
//     //                <div class=""col-finished"">FINISHED SIZE<br>COLOR</div>
//     //                <div class=""col-color"">TYPE</div>
//
//     //                <div class=""col-area"">AREA<br>(SQ.M)</div>
//     //                <div class=""col-weight"">WEIGHT<br>(LBS)</div>
//     //            </div>
//     //        ");
//
//
//
//     //        var currentLot = "";
//     //        var qtyStr = "";
//     //        var pourSizeStr = "";
//     //        var widthStr = "";
//     //        var LengthStr = "";
//     //        var finishedStr = "";
//     //        var areaStr = "";
//     //        var weightStr = "";
//     //        foreach (var item in request.Items)
//     //        {
//     //            var isRedItem = item.Color.ToUpper().Contains("NEW WHITE") || item.Type.ToUpper().Contains("SMOOTH FACE");
//     //            var colorClass = isRedItem ? "red-text" : "";
//
//
//     //            if (currentLot != item.LotNo)
//     //            {
//     //                html.Append(@"< div class=""table-row"">");
//     //                if(currentLot == "")
//     //                { 
//     //                    currentLot = item.LotNo;
//     //                }
//     //                else
//     //                {
//     //                    currentLot = item.LotNo;
//
//     //                    var finalStr = $@"
//     //                                   {qtyStr.TrimEnd("<br>").Append("</div>")}
//     //                                   {pourSizeStr.TrimEnd("<br>").Append("</div>")}
//     //                                   {widthStr.TrimEnd("<br>").Append("</div>")}
//     //                                   {LengthStr.TrimEnd("<br>").Append("</div>")}
//     //                                   {finishedStr.TrimEnd("<br>").Append("</div>")}
//     //                                   {areaStr.TrimEnd("<br>").Append("</div>")}
//     //                                   {weightStr.TrimEnd("<br>").Append("</div>")}
//     //                                   ";
//
//     //                    html.Append(finalStr);
//     //                    html.Append(@"</div>");
//     //                }
//
//
//
//     //                qtyStr = $@"<div class=""col-qty lot-number"">{item.LotNo}<br>";
//     //                pourSizeStr = @"<div class=""col-power"">";
//     //                widthStr = @"<div class=""col-width"">";
//     //                LengthStr = @"<div class=""col-length"">";
//     //                finishedStr = @"<div class=""col-finished"">";
//     //                areaStr = @"<div class=""col-area"">";
//     //                weightStr = @"<div class=""col-weight"">";
//     //            }
//
//     //            qtyStr = qtyStr + @$"<span class=""{colorClass}"">{item.Quantity}</span><br>";
//     //            pourSizeStr = pourSizeStr + @$"<span class=""{colorClass}"">{item.PouredSize}</span><br>";
//     //            widthStr = widthStr + @$"<span class=""{colorClass}"">{item.Width}</span><br>";
//     //            LengthStr = LengthStr + @$"<span class=""{colorClass}"">{item.Length}</span><br>";
//     //            finishedStr = finishedStr + @$"<span class=""{colorClass}"">{item.Color}</span><br>";
//     //            areaStr = areaStr + @$"<span class=""{colorClass}"">{item.Area}</span><br>";
//     //            weightStr = weightStr + @$"<span class=""{colorClass}"">{item.Weight}</span><br>";
//
//     //            if()//final item )
//     //            {
//
//
//     //                var finalStr = $@"
//     //                                   {qtyStr.TrimEnd("<br>").Append("</div>")}
//     //                                   {pourSizeStr.TrimEnd("<br>").Append("</div>")}
//     //                                   {widthStr.TrimEnd("<br>").Append("</div>")}
//     //                                   {LengthStr.TrimEnd("<br>").Append("</div>")}
//     //                                   {finishedStr.TrimEnd("<br>").Append("</div>")}
//     //                                   {areaStr.TrimEnd("<br>").Append("</div>")}
//     //                                   {weightStr.TrimEnd("<br>").Append("</div>")}
//     //                                   ";
//
//     //                html.Append(finalStr);
//     //                html.Append(@"</div>");
//     //            }
//
//
//     //            // Parse poured size - handle different formats
//     //            var pouredSizeParts = item.PouredSize.Replace("(", "").Replace(")", "").Split('X', 'x');
//     //            var width = pouredSizeParts.Length > 0 ? pouredSizeParts[0].Trim() : "";
//     //            var length = pouredSizeParts.Length > 1 ? pouredSizeParts[1].Trim() : "";
//
//
//     //        }
//
//     //        foreach (var lotData in request.Items)
//     //        {
//
//     //            html.Append(@" < div class=""table-row"">
//     //                <div class=""col-qty lot-number"">LOT 68<br>1<br><span class=""red-text"">1</span></div>
//     //                <div class=""col-power"">(24 X 30)<br><span class=""red-text"">(34 X 60)</span></div>
//     //                <div class=""col-width"">21<br><span class=""red-text"">31</span></div>
//     //                <div class=""col-length"">28<br><span class=""red-text"">66</span></div>
//     //                <div class=""col-finished"">NEW WHITE<br><span class=""red-text"">NEW WHITE</span></div>
//     //                <div class=""col-color"">SMOOTH FACE<br><span class=""red-text"">SMOOTH FACE</span></div>
//
//     //                <div class=""col-area"">720<br><span class=""red-text"">952</span></div>
//     //                <div class=""col-weight"">134<br><span class=""red-text"">199</span></div>
//     //            </div>");
//     //        }
//
//
//     //        html.Append($@"
//     //          < !-- Empty rows to fill space -->
//     //            <div class=""table-row"" style=""height: 100px;""><div class=""col-qty""></div><div class=""col-power""></div><div class=""col-width""></div><div class=""col-length""></div><div class=""col-finished""></div><div class=""col-color""></div><div class=""col-type""></div><div class=""col-area""></div><div class=""col-weight""></div></div>
//     //            <div class=""table-row"" style=""height: 100px;""><div class=""col-qty""></div><div class=""col-power""></div><div class=""col-width""></div><div class=""col-length""></div><div class=""col-finished""></div><div class=""col-color""></div><div class=""col-type""></div><div class=""col-area""></div><div class=""col-weight""></div></div>
//     //            <div class=""table-row"" style=""height: 100px;""><div class=""col-qty""></div><div class=""col-power""></div><div class=""col-width""></div><div class=""col-length""></div><div class=""col-finished""></div><div class=""col-color""></div><div class=""col-type""></div><div class=""col-area""></div><div class=""col-weight""></div></div>
//     //        </div>
//
//     //        <div class=""bottom-section"">
//     //            <div class=""notes-section"">
//     //                <div class=""notes-label"">NOTES:</div>
//     //                <div class=""notes-content"">NEED BRICK TIES</div>
//     //            </div>
//
//     //            <div class=""delivery-section"">
//     //                <div>EXPECTED DELIVERY DATE(S) BEFORE:</div>
//     //                <div class=""total-weight"">TOTAL WEIGHT:</div>
//     //                <div style=""font-weight: bold;"">1,764</div>
//     //            </div>
//
//     //            <div style=""text-align: center; font-size: 11px; margin-top: 5px;"">
//     //                08 Jul 2025 (Tuesday)
//     //            </div>
//     //        </div>
//     //    </div>
//     //</body>
//     //</html>");
//
//
//     //    }
//
//     private string GenerateEnhancedWorkOrderHtmlDep(WorkOrderRequest3 request)
//     {
//         var totalWeight = request.Items.Sum(i => int.TryParse(i.Weight, out int w) ? w : 0);
//         var html = new StringBuilder(); // Missing StringBuilder declaration
//
//         html.Append($@"
// <!DOCTYPE html>
// <html>
// <head>
//     <meta charset='UTF-8'>
//     <meta name='viewport' content='width=device-width, initial-scale=1.0'>
//     <style>
//         @page {{
//             size: A4;
//             margin: 15mm;
//         }}
//         * {{
//             margin: 0;
//             padding: 0;
//             box-sizing: border-box;
//         }}
//         body {{
//             font-family: 'Arial', sans-serif;
//             font-size: 11px;
//             line-height: 1.2;
//             color: #000;
//         }}
//         .document {{
//             width: 100%;
//             max-width: 210mm;
//             margin: 0 auto;
//             background: white;
//         }}
//         .header {{
//             display: flex;
//             justify-content: space-between;
//             align-items: flex-start;
//             padding: 15px 0;
//             border-bottom: 3px solid #000;
//             margin-bottom: 15px;
//         }}
//         .logo-section {{
//             display: flex;
//             align-items: center;
//         }}
//         .logo-icon {{
//             width: 35px;
//             height: 35px;
//             background: linear-gradient(45deg, #ff4444, #ff6666);
//             margin-right: 12px;
//             position: relative;
//             border-radius: 3px;
//         }}
//         .logo-icon::before {{
//             content: '🏠';
//             position: absolute;
//             top: 50%;
//             left: 50%;
//             transform: translate(-50%, -50%);
//             color: white;
//             font-size: 18px;
//         }}
//         .company-info {{
//             flex-grow: 1;
//         }}
//         .company-name {{
//             font-size: 18px;
//             font-weight: bold;
//             color: #666;
//             margin-bottom: 5px;
//         }}
//         .company-details {{
//             font-size: 9px;
//             color: #666;
//             line-height: 1.3;
//         }}
//         .work-order-title {{
//             background: linear-gradient(135deg, #ffb3b3, #ff9999);
//             padding: 12px 20px;
//             text-align: center;
//             font-weight: bold;
//             font-size: 16px;
//             color: #333;
//             border: 2px solid #999;
//             border-radius: 5px;
//         }}
//         .info-section {{
//             display: flex;
//             justify-content: space-between;
//             margin: 20px 0;
//             gap: 20px;
//         }}
//         .info-left, .info-right {{
//             flex: 1;
//         }}
//         .info-group {{
//             margin-bottom: 12px;
//         }}
//         .info-header {{
//             background: #999;
//             color: white;
//             padding: 6px 10px;
//             font-weight: bold;
//             font-size: 10px;
//             border: 1px solid #000;
//         }}
//         .info-content {{
//             background: #f0f0f0;
//             padding: 8px 10px;
//             border: 1px solid #000;
//             border-top: none;
//             min-height: 40px;
//             white-space: pre-line;
//         }}
//         .order-info-header {{
//             background: #ffb3b3;
//             color: #000;
//             padding: 6px 10px;
//             font-weight: bold;
//             text-align: center;
//             border: 1px solid #000;
//         }}
//         .order-info-content {{
//             background: white;
//             padding: 8px 10px;
//             border: 1px solid #000;
//             border-top: none;
//             text-align: center;
//             min-height: 35px;
//             display: flex;
//             align-items: center;
//             justify-content: center;
//         }}
//         .purchase-order-header {{
//             background: #b3d9ff;
//             color: #000;
//             padding: 6px 10px;
//             font-weight: bold;
//             text-align: center;
//             border: 1px solid #000;
//         }}
//         .purchase-order-content {{
//             background: white;
//             padding: 8px 10px;
//             border: 1px solid #000;
//             border-top: none;
//             text-align: center;
//             min-height: 35px;
//             display: flex;
//             align-items: center;
//             justify-content: center;
//         }}
//         .main-table {{
//             width: 100%;
//             border-collapse: collapse;
//             margin: 20px 0;
//             border: 2px solid #000;
//         }}
//         .table-header {{
//             background: #ffb3b3;
//             font-weight: bold;
//             text-align: center;
//             padding: 8px 4px;
//             border: 1px solid #000;
//             font-size: 9px;
//         }}
//         .table-subheader {{
//             background: #ffcccc;
//             font-weight: bold;
//             text-align: center;
//             padding: 6px 4px;
//             border: 1px solid #000;
//             font-size: 8px;
//         }}
//         .table-cell {{
//             padding: 6px 4px;
//             border: 1px solid #000;
//             text-align: center;
//             font-size: 9px;
//         }}
//         .lot-header {{
//             background: #e6e6e6;
//             font-weight: bold;
//             text-align: left;
//             padding: 6px 8px;
//             border: 1px solid #000;
//         }}
//         .red-text {{
//             color: #d40000;
//             font-weight: bold;
//         }}
//         .poured-size {{
//             font-size: 8px;
//         }}
//         .notes-section {{
//             margin: 20px 0;
//             display: flex;
//             align-items: center;
//         }}
//         .notes-label {{
//             color: #d40000;
//             font-weight: bold;
//             margin-right: 15px;
//         }}
//         .footer-section {{
//             display: flex;
//             justify-content: space-between;
//             align-items: center;
//             margin-top: 25px;
//             padding-top: 15px;
//             border-top: 2px solid #000;
//         }}
//         .delivery-info {{
//             font-size: 10px;
//             font-weight: bold;
//         }}
//         .total-weight {{
//             font-size: 12px;
//             font-weight: bold;
//         }}
//     </style>
// </head>
// <body>
//     <div class='document'>
//         <!-- Header Section -->
//         <div class='header'>
//             <div class='logo-section'>
//                 <div class='logo-icon'></div>
//                 <div class='company-info'>
//                     <div class='company-name'>MFG PRECAST</div>
//                     <div class='company-details'>
// PO Box 71071, Maplelawn<br>
// Burlington, ON L7T 2E0<br>
// Phone: (905) 643 114 (905) 469 1119<br>
// Email: info@mfgprecast.com<br>
// www.mfgprecast.com
//                     </div>
//                 </div>
//             </div>
//             <div class='work-order-title'>WORK ORDER</div>
//         </div>
//         
//         <!-- Info Section -->
//         <div class='info-section'>
//             <div class='info-left'>
//                 <div class='info-group'>
//                     <div class='info-header'>BUILDER / SITE / CITY</div>
//                     <div class='info-content'>{request.BuilderSiteCity}</div>
//                 </div>
//                 
//                 <div class='info-group'>
//                     <div class='info-header'>BLK NO.</div>
//                     <div class='info-content'>{request.BlkNo}</div>
//                 </div>
//                 
//                 <div class='info-group'>
//                     <div class='info-header'>LOT NO.</div>
//                     <div class='info-content'>{request.LotNo}</div>
//                 </div>
//             </div>
//             
//             <div class='info-right'>
//                 <div class='info-group'>
//                     <div class='order-info-header'>ORDER DATE</div>
//                     <div class='order-info-content'>{request.OrderDate}</div>
//                 </div>
//                 
//                 <div class='info-group'>
//                     <div class='purchase-order-header'>PURCHASE ORDER</div>
//                     <div class='purchase-order-content'>{request.PurchaseOrder}</div>
//                 </div>
//                 
//                 <div class='info-group'>
//                     <div class='purchase-order-header'>COMPANY</div>
//                     <div class='purchase-order-content'>{request.Company}</div>
//                 </div>
//                 
//                 <div class='info-group'>
//                     <div class='purchase-order-header'>CONTACT</div>
//                     <div class='purchase-order-content'>{request.Contact}</div>
//                 </div>
//             </div>
//         </div>
//         
//         <!-- Main Table -->
//         <table class='main-table'>
//             <thead>
//                 <tr>
//                     <th rowspan='2' class='table-header'>QTY</th>
//                     <th colspan='2' class='table-header'>POURED SIZE</th>
//                     <th colspan='2' class='table-header'>FINISHED SIZE</th>
//                     <th rowspan='2' class='table-header'>TYPE</th>
//                     <th rowspan='2' class='table-header'>AREA<br>(SQ.M)</th>
//                     <th rowspan='2' class='table-header'>WEIGHT<br>(LBS)</th>
//                 </tr>
//                 <tr>
//                     <th class='table-subheader'>WIDTH</th>
//                     <th class='table-subheader'>LENGTH</th>
//                     <th class='table-subheader'>WIDTH</th>
//                     <th class='table-subheader'>LENGTH</th>
//                     <th class='table-subheader'>COLOR</th>
//                 </tr>
//             </thead>
//             <tbody>");
//
//         var currentLot = "";
//         foreach (var item in request.Items)
//         {
//             if (currentLot != item.LotNo)
//             {
//                 currentLot = item.LotNo;
//                 html.Append($@"
//                 <tr>
//                     <td colspan='8' class='lot-header'>{item.LotNo}</td>
//                 </tr>");
//             }
//
//             var isRedItem = item.Color.ToUpper().Contains("NEW WHITE") || item.Type.ToUpper().Contains("SMOOTH FACE");
//             var colorClass = isRedItem ? "red-text" : "";
//
//             // Parse poured size - handle different formats
//             var pouredSizeParts = item.PouredSize.Replace("(", "").Replace(")", "").Split('X', 'x');
//             var width = pouredSizeParts.Length > 0 ? pouredSizeParts[0].Trim() : "";
//             var length = pouredSizeParts.Length > 1 ? pouredSizeParts[1].Trim() : "";
//
//             html.Append($@"
//                 <tr>
//                     <td class='table-cell'>{item.Quantity}</td>
//                     <td class='table-cell poured-size'>({width})</td>
//                     <td class='table-cell'>{length}</td>
//                     <td class='table-cell'>{item.Width}</td>
//                     <td class='table-cell'>{item.Length}</td>
//                     <td class='table-cell {colorClass}'>{item.Color}</td>
//                     <td class='table-cell {colorClass}'>{item.Type}</td>
//                     <td class='table-cell'>{item.Area}</td>
//                     <td class='table-cell'>{item.Weight}</td>
//                 </tr>");
//         }
//
//         html.Append($@"
//             </tbody>
//         </table>
//         
//         <!-- Notes Section -->
//         <div class='notes-section'>
//             <span class='notes-label'>NOTES:</span>
//             <span>{request.Notes}</span>
//         </div>
//         
//         <!-- Footer Section -->
//         <div class='footer-section'>
//             <div class='delivery-info'>
//                 <strong>EXPECTED DELIVERY DATE IS BEFORE:</strong> {request.ExpectedDeliveryDate}
//             </div>
//             <div class='total-weight'>
//                 TOTAL WEIGHT: {totalWeight}
//             </div>
//         </div>
//     </div>
// </body>
// </html>");
//
//         return html.ToString();
//     }
// }
// //
// public class PourPlanService : IPourPlanService
// {
//     public async Task<byte[]> GeneratePourPlanPdfAsync(PourPlanRequest3 request)
//     {
//         var html = GeneratePourPlanHtml(request);
//
//         var browserFetcher = new BrowserFetcher();
//         await browserFetcher.DownloadAsync();
//
//         await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
//         {
//             Headless = true
//         });
//
//         await using var page = await browser.NewPageAsync();
//         await page.SetContentAsync(html);
//
//         /*var pdfBytes =*/
//         await page.PdfAsync($"NewPDF {Guid.NewGuid()}.pdf", new PdfOptions
//         {
//             Format = PaperFormat.A4,
//             PrintBackground = true,
//             Landscape = true,
//             MarginOptions = new MarginOptions
//             {
//                 Top = "10mm",
//                 Bottom = "10mm",
//                 Left = "10mm",
//                 Right = "10mm"
//             }
//         });
//
//         return new byte[0];
//     }
//
//     public async Task<byte[]> GenerateDayToDayPourPlanPdfAsync(List<PourPlanDay> days)
//     {
//         var html = GenerateDayToDayPourPlanHtml(days);
//
//         var browserFetcher = new BrowserFetcher();
//         await browserFetcher.DownloadAsync();
//
//         await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
//         {
//             Headless = true
//         });
//
//         await using var page = await browser.NewPageAsync();
//         await page.SetContentAsync(html);
//
//         /* var pdfBytes =*/
//         await page.PdfAsync($"NewPDF {Guid.NewGuid()}.pdf", new PdfOptions
//         {
//             Format = PaperFormat.A4,
//             PrintBackground = true,
//             Landscape = true,
//             MarginOptions = new MarginOptions
//             {
//                 Top = "10mm",
//                 Bottom = "10mm",
//                 Left = "10mm",
//                 Right = "10mm"
//             }
//         });
//
//         return new byte[0];
//     }
//
//     private string GeneratePourPlanHtml(PourPlanRequest3 request)
//     {
//         var html = new StringBuilder();
//
//         html.Append(@"
// <!DOCTYPE html>
// <html>
// <head>
//     <meta charset='UTF-8'>
//     <style>
//         * { margin: 0; padding: 0; box-sizing: border-box; }
//         body { font-family: Arial, sans-serif; font-size: 10px; background: white; }
//         .container { width: 100%; display: flex; height: 100vh; }
//         .left-section { width: 60%; padding: 10px; }
//         .right-section { width: 40%; padding: 10px; background: #f8f8f8; }
//         .pour-plans { display: flex; flex-direction: column; gap: 20px; }
//         .plan-day { border: 2px solid #000; padding: 10px; background: white; }
//         .plan-title { background: #4a90e2; color: white; text-align: center; padding: 8px; font-weight: bold; font-size: 12px; margin-bottom: 10px; }
//         .plan-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 15px; }
//         .mold-section { border: 1px solid #ccc; padding: 8px; background: #fafafa; }
//         .mold-title { background: #e6e6e6; padding: 4px; text-align: center; font-weight: bold; margin-bottom: 5px; }
//         .mold-items { display: flex; flex-direction: column; gap: 3px; }
//         .mold-item { display: flex; align-items: center; padding: 2px 5px; }
//         .item-color { width: 8px; height: 8px; margin-right: 5px; }
//         .red-item { background: #ff0000; }
//         .orange-item { background: #ffa500; }
//         .black-item { background: #000000; }
//         .pour-info { background: #e6f3ff; padding: 10px; margin: 10px 0; border-left: 4px solid #4a90e2; }
//         .data-table { width: 100%; border-collapse: collapse; margin-top: 10px; font-size: 8px; }
//         .data-table th { background: #4a90e2; color: white; padding: 4px; border: 1px solid #000; }
//         .data-table td { padding: 3px; border: 1px solid #000; text-align: center; }
//         .green-cell { background: #90EE90; }
//         .blue-cell { background: #ADD8E6; }
//         .instructions { background: #f0f8ff; padding: 15px; margin-top: 10px; border: 1px solid #ccc; }
//         .instructions h3 { color: #4a90e2; margin-bottom: 10px; }
//         .form-sizes { background: #fff3cd; padding: 10px; margin: 10px 0; }
//         .form-sizes h4 { color: #856404; margin-bottom: 5px; }
//     </style>
// </head>
// <body>
//     <div class='container'>
//         <div class='left-section'>
//             <div class='pour-plans'>
//                 <div class='plan-day'>
//                     <div class='plan-title'>THURSDAY POURING PLAN</div>
//                     <div class='plan-grid'>");
//
//         // Generate Thursday plan molds
//         foreach (var mold in request.Molds.Take(2))
//         {
//             html.Append($@"
//                         <div class='mold-section'>
//                             <div class='mold-title'>{mold.Name}</div>
//                             <div class='mold-items'>");
//
//             foreach (var item in mold.Items)
//             {
//                 var colorClass = item.IsHighlighted ? "red-item" : "black-item";
//                 html.Append($@"
//                                 <div class='mold-item'>
//                                     <div class='item-color {colorClass}'></div>
//                                     <span>{item.Size} - {item.Label}</span>
//                                 </div>");
//             }
//
//             html.Append(@"
//                             </div>
//                         </div>");
//         }
//
//         html.Append(@"
//                     </div>
//                 </div>
//
//                 <div class='plan-day'>
//                     <div class='plan-title'>FRIDAY POURING PLAN</div>
//                     <div class='plan-grid'>");
//
//         // Generate Friday plan molds
//         foreach (var mold in request.Molds.Skip(2).Take(2))
//         {
//             html.Append($@"
//                         <div class='mold-section'>
//                             <div class='mold-title'>{mold.Name}</div>
//                             <div class='mold-items'>");
//
//             foreach (var item in mold.Items)
//             {
//                 var colorClass = item.IsHighlighted ? "red-item" : "black-item";
//                 html.Append($@"
//                                 <div class='mold-item'>
//                                     <div class='item-color {colorClass}'></div>
//                                     <span>{item.Size} - {item.Label}</span>
//                                 </div>");
//             }
//
//             html.Append(@"
//                             </div>
//                         </div>");
//         }
//
//         html.Append(@"
//                     </div>
//                 </div>
//             </div>
//         </div>
//
//         <div class='right-section'>
//             <div class='pour-info'>
//                 <div style='display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px;'>
//                     <div><strong>DATE:</strong></div>
//                     <div><strong>POUR " + request.PourNumber + @"</strong></div>
//                 </div>
//                 <div><strong>COLOR:</strong> " + request.Color + @"</div>
//             </div>");
//
//         // Generate mold specifications
//         foreach (var mold in request.Molds.Take(3))
//         {
//             html.Append($@"
//             <div style='margin: 15px 0; border: 1px solid #ccc; padding: 10px;'>
//                 <div style='background: #ff6b6b; color: white; padding: 5px; text-align: center; font-weight: bold;'>
//                     MOLD NAME - {mold.Name}
//                 </div>
//                 <div style='background: #f0f0f0; padding: 8px; margin: 5px 0;'>
//                     <div>P1</div>
//                     <div style='border: 1px solid #000; padding: 15px; margin: 5px 0; background: white; text-align: center;'>
//                         {mold.Size}
//                     </div>
//                 </div>
//             </div>");
//         }
//
//         // Generate data table if provided
//         if (request.Table != null && request.Table.Rows.Any())
//         {
//             html.Append(@"
//             <table class='data-table'>
//                 <thead>
//                     <tr>
//                         <th>MOLD SIZE</th>
//                         <th>POURED SIZE<br>INCHES</th>
//                         <th>POURED SIZE<br>INCHES</th>
//                         <th>ALO OF<br>PCS</th>
//                         <th>POURED SIZE<br>INCHES</th>
//                         <th>ALO OF<br>PCS</th>
//                         <th>FORM<br>LENGTH</th>
//                         <th>FORM HEIGHT<br>MAX (INCHES)</th>
//                         <th>TOTAL<br>MIXING<br>GAL (BATCH)</th>
//                         <th>CUM MIXING<br>GAL BATCH</th>
//                     </tr>
//                 </thead>
//                 <tbody>");
//
//             foreach (var row in request.Table.Rows)
//             {
//                 html.Append($@"
//                     <tr>
//                         <td>{row.MoldSize}</td>
//                         <td>{row.PouredSize}</td>
//                         <td>{row.PouredSizeInches}</td>
//                         <td>{row.Alo}</td>
//                         <td>{row.PouredSizeInches2}</td>
//                         <td>{row.Op}</td>
//                         <td class='green-cell'>{row.FormLenth}</td>
//                         <td class='blue-cell'>{row.FormHeightMax}</td>
//                         <td class='green-cell'>{row.TotalMixing}</td>
//                         <td class='blue-cell'>{row.CumMixing}</td>
//                     </tr>");
//             }
//
//             html.Append(@"
//                 </tbody>
//             </table>");
//         }
//
//         html.Append(@"
//             <div class='instructions'>
//                 <h3>Example: - DAY TO DAY POURING PLAN (AUTOCAD)</h3>
//                 <div class='form-sizes'>
//                     <h4>TYPE OF COLOURS</h4>
//                     <div>NEW WHITE SMOOTH</div>
//                     <div>GRAY</div>
//                     <div>OLD WHITE</div>
//                 </div>
//                 <div class='form-sizes'>
//                     <h4>TYPE OF FINISHING</h4>
//                     <div>• ROCKFACE FACE (ROCKFACE 2 LONG, 1 SHORT, ROCKFACE 1L, 1S, ROCKFACE 1L, 2S, ROCKFACE 2L, ROCKFACE 2S, ROCKFACE BUTT)</div>
//                     <div>• SMOOTH FACE (SMOOTH FACE BUTT)</div>
//                     <div>*SPLIT - WE SPLIT THE PIECE IN TWO WHEN IT IS GREATER THAN 50"" FOR ROCKFACE,</div>
//                     <div>WE SPLIT THE PIECE IN TWO WHEN IT IS GREATER THAN 72"" FOR SMOOTHFACE</div>
//                     <div>*REBAR - WE PUT REBAR WHEN THE PIECE IS GREATER THAN 50""</div>
//                     <div>1 REBAR IF BETWEEN 50""</div>
//                 </div>
//             </div>
//         </div>
//     </div>
// </body>
// </html>");
//
//         return html.ToString();
//     }
//
//     private string GenerateDayToDayPourPlanHtml(List<PourPlanDay> days)
//     {
//         var html = new StringBuilder();
//
//         html.Append(@"
// <!DOCTYPE html>
// <html>
// <head>
//     <meta charset='UTF-8'>
//     <style>
//         * { margin: 0; padding: 0; box-sizing: border-box; }
//         body { font-family: Arial, sans-serif; font-size: 10px; }
//         .container { width: 100%; padding: 20px; }
//         .title { text-align: center; font-size: 16px; font-weight: bold; margin-bottom: 20px; }
//         .schedule-table { width: 100%; border-collapse: collapse; }
//         .schedule-table th { background: #ddd; padding: 8px; border: 1px solid #000; text-align: center; font-weight: bold; }
//         .schedule-table td { padding: 6px; border: 1px solid #000; text-align: center; font-size: 9px; }
//         .date-cell { background: #ffff99; font-weight: bold; width: 80px; }
//         .location-cell { width: 120px; }
//         .content-cell { min-height: 60px; vertical-align: top; padding: 8px; }
//         .highlight { background: #ffff99; }
//         .nil-cell { background: #ddd; color: #666; }
//     </style>
// </head>
// <body>
//     <div class='container'>
//         <div class='title'>EXAMPLE: DAY TO DAY POURING PLAN (EXCEL)</div>
//         
//         <table class='schedule-table'>
//             <thead>
//                 <tr>
//                     <th rowspan='2'>Date</th>
//                     <th rowspan='2'>Location</th>
//                     <th>FULL ORDER</th>
//                     <th>PLANNED TO BE POURED</th>
//                     <th>SUGGESTED MOLD</th>
//                     <th>LEFT TO BE POURED</th>
//                 </tr>
//             </thead>
//             <tbody>");
//
//         foreach (var day in days)
//         {
//             var dateClass = day.IsHighlighted ? "date-cell highlight" : "date-cell";
//             var nilClass = day.LeftToBePoured == "NIL" ? "nil-cell" : "";
//
//             html.Append($@"
//                 <tr>
//                     <td class='{dateClass}'>{day.Date}</td>
//                     <td class='location-cell'>{day.Location}</td>
//                     <td class='content-cell'>{day.FullOrder}</td>
//                     <td class='content-cell'>{day.PlannedToBePoured}</td>
//                     <td class='content-cell'>{day.SuggestedMold}</td>
//                     <td class='content-cell {nilClass}'>{day.LeftToBePoured}</td>
//                 </tr>");
//         }
//
//         html.Append(@"
//             </tbody>
//         </table>
//     </div>
// </body>
// </html>");
//
//         return html.ToString();
//     }
//
//
// }
