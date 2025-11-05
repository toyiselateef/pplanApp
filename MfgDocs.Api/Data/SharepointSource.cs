using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure.Identity;
using MfgDocs.Api.Models;
using MfgDocs.Api.Services.Generators;
using Microsoft.Graph;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Identity.Client;

namespace MfgDocs.Api.Data;

// public interface ISharePointService
// {
//  
//
// }

public interface ISharePointListService
{
    Task<List<WorkOrderRequest5>> GetUnpouredWorkOrdersAsync();
    Task<bool> UpdatePourProgressAsync(List<PourProgressUpdate> updates);
    Task<List<WorkOrderWithDetails>> GetWorkOrderDetailsAsync(string filter = null);
    Task<List<WorkOrderRequest4>> GetWorkOrdersByWorkIdsAsync(List<WorkOrderRequest4Dto> Ids);
    Task<List<WorkOrderRequest4>> GetWorkOrdersByIdsAsync(List<string> Ids);

    Task<FileUploadResult> UploadFileToSharePointAsync(
        byte[] fileContent,
        string fileName,
        string folderPath,
        string driveId = null,
        bool allowReplaceFile = false);

    //
    Task<List<ColumnDefinition>> GetListColumnsAsync(string listId);
    Task<ListItem> CreateListItemAsync(string listId, Dictionary<string, object> fields);
    Task DeleteListItemAsync(string listId, string itemId);
    Task<List<ListItem>> GetFilteredListItemsAsync(string listId, string filter);
    Task<List<ListItem>> GetListItemsAsync(string listId);
    Task<bool> UpdateListItemAsync(string listId, string itemId, Dictionary<string, object> fields);
    Task<List> GetListByTitleAsync(string listTitle);
    
    //
    Task<List<StandardMold>> GetStandardMoldsAsync();
}

public class SharePointListService : ISharePointListService
{
    private readonly IConfiguration _configuration;
    private readonly GraphServiceClient _graphClient;
    private readonly string _siteId;
    private const string ORDERS_LIST = "Orders";
    private const string LOTS_LIST = "Lots";
    private const string WORKORDERS_LIST = "Work Orders";
    private const string STANDARD_MOLDS_LIST = "Standard Molds";

    public SharePointListService(IConfiguration configuration)
    {
        _configuration = configuration;
    }       
//>>>>>>
/// <summary>
/// Gets standard mold definitions from SharePoint list
/// </summary>
public async Task<List<StandardMold>> GetStandardMoldsAsync()
{
    try
    {
        var standardMoldsList = await GetListByTitleAsync(STANDARD_MOLDS_LIST);
        
        if (standardMoldsList == null)
        {
            // Return empty list if list doesn't exist - fallback will be used
            return new List<StandardMold>();
        }

        var moldItems = await _graphClient.Sites[_siteId]
            .Lists[standardMoldsList.Id]
            .Items
            .GetAsync(config =>
            {
                config.QueryParameters.Expand = new[] { "fields" };
                // Optionally filter for active molds only if you have an IsActive field
                // config.QueryParameters.Filter = "fields/IsActive eq true";
            });

        var molds = new List<StandardMold>();

        foreach (var item in moldItems.Value)
        {
            var fields = item.Fields.AdditionalData;
            
            var mold = new StandardMold
            {
                Name = GetFieldValue<string>(fields, "Name") ?? GetFieldValue<string>(fields, "Name"),
                Width = (double)GetFieldValue<decimal>(fields, "Width"),
                Length = (double)GetFieldValue<decimal>(fields, "Length"),
                PourCategory = GetFieldValue<string>(fields, "PourCategory") ?? GetFieldValue<string>(fields, "Pour_x0020_Category"),
                IsActive = GetFieldValue<bool>(fields, "IsActive") // Defaults to false if field doesn't exist
            };

            // Only add valid molds (with name and positive dimensions)
            if (!string.IsNullOrEmpty(mold.Name) && mold.Width > 0 && mold.Length > 0)
            {
                molds.Add(mold);
            }
        }

        return molds.OrderBy(m => m.Name).ToList();
    }
    catch (Exception ex)
    {
        // Log the error but don't throw - allow fallback to hardcoded molds
        Console.WriteLine($"Error fetching standard molds from SharePoint: {ex.Message}");
        return new List<StandardMold>();
    }
}
//<<<<<<<
    //public SharePointListService(string tenantId, string clientId, string clientSecret, string siteUrl)
    public SharePointListService()
    {
        string tenantId = Environment.GetEnvironmentVariable("TENANT_ID_");
        string clientId = Environment.GetEnvironmentVariable("CLIENT_ID_");
        string client_ = Environment.GetEnvironmentVariable("CLIENT_");
        string siteUrl = Environment.GetEnvironmentVariable("SHAREPOINT_URL2_");
  // string tenantId = _configuration["Sharepoint:TENANT_ID"];
  //       string clientId = _configuration["Sharepoint:CLIENT_ID"];
  //       string client_ = Environment.GetEnvironmentVariable("CLIENT_");
  //       string siteUrl = _configuration["Sharepoint:SHAREPOINT_URL2"];

        var credential = new ClientSecretCredential(tenantId, clientId, client_);

        // Initialize Graph client
        _graphClient = new GraphServiceClient(credential);

        // Store site URL for later use
        _siteId = GetSiteIdFromUrl(siteUrl).Result;
    }

    // Alternative constructor using app-only token
    // public SharePointListService(string accessToken, string siteUrl)
    // {
    //     _graphClient = new GraphServiceClient(
    //         new DelegateAuthenticationProvider((requestMessage) =>
    //         {
    //             requestMessage.Headers.Authorization = 
    //                 new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    //             return Task.CompletedTask;
    //         }));
    //     
    //     _siteId = GetSiteIdFromUrl(siteUrl).Result;
    // }

    private async Task<string> GetSiteIdFromUrl(string siteUrl)
    {
        // Extract site path from URL
        // Example: https://contoso.sharepoint.com/sites/MySite
        var uri = new Uri(siteUrl);
        var hostname = uri.Host;
        var sitePath = uri.AbsolutePath;

        var site = await _graphClient.Sites[$"{hostname}:{sitePath}"].GetAsync();
        return site.Id;
    }

    /// <summary>
    /// Gets work orders that haven't been fully poured yet with all related lots and orders
    /// </summary>
    public async Task<List<WorkOrderRequest5>> GetUnpouredWorkOrdersAsync()
    {
        var result = new List<WorkOrderRequest5>();

        // Get WorkOrders list ID
        var workOrdersList = await GetListByTitleAsync(WORKORDERS_LIST);
        var lotsList = await GetListByTitleAsync(LOTS_LIST);
        var ordersList = await GetListByTitleAsync(ORDERS_LIST);

        if (workOrdersList == null || lotsList == null || ordersList == null)
            throw new Exception("Required lists not found in SharePoint");

        // Get all unpoured work orders (IsPoured = No or null)
        var workOrderItems = await _graphClient.Sites[_siteId]
            .Lists[workOrdersList.Id]
            .Items
            .GetAsync(config =>
            {
                config.QueryParameters.Expand = new[] { "fields" };
                //config.QueryParameters.Filter = "fields/IsPoured ne 'Yes'";
                config.QueryParameters.Filter = $"fields/IsPoured ne '{false}'";
            });

        foreach (var woItem in workOrderItems.Value)
        {
            var fields = woItem.Fields.AdditionalData;

            var workOrder = new WorkOrderRequest5
            {
                OrderDate2 = GetFieldValue<DateTime>(fields, "Work_x0020_Order_x0020_Date"),
                PurchaseOrder = GetFieldValue<string>(fields, "Purchase_x0020_Order_x0020_Numbe"),
                Company = await GetLookupDisplayValue(fields, "Customer"),
                Contact = await GetLookupDisplayValue(fields, "Contact"),
                Builder = await GetLookupDisplayValue(fields, "Builder"),
                Site = GetFieldValue<string>(fields, "Site"),
                City = await GetLookupDisplayValue(fields, "City"),
                Notes = GetFieldValue<string>(fields, "Notes"),
                ExpectedDeliveryDate2 = GetFieldValue<DateTime>(fields, "Delivery_x0020_Date"),
                Priority = DeterminePriority(fields)
            };

            // Get lots for this work order
            var lots = await GetLotsForWorkOrderAsync(lotsList.Id, woItem.Id);

            // Get orders for each lot and aggregate
            foreach (var lot in lots)
            {
                var orders = await GetOrdersForLotAsync(ordersList.Id, lot.LotId);

                foreach (var order in orders)
                {
                    // Only include orders that haven't been fully poured
                    if (order.Quantity > order.QuantityPoured)
                    {
                        workOrder.Items.Add(new Order
                        {
                            LotName = lot.LotNumber,
                            Quantity = (int)(order.Quantity - order.QuantityPoured), // Remaining quantity
                            FinishedLength = (double)order.FinishedLength,
                            FinishedWidth = (double)order.FinishedWidth,
                            Color = order.Color,
                            Type = order.ProductType,
                            PourWidth = (double)order.PouredWidth,
                            PourLength = (double)order.PouredLength
                        });
                    }
                }
            }

            // Only add work orders that have items to pour
            if (workOrder.Items.Any())
            {
                result.Add(workOrder);
            }
        }

        return result;
    }

    public async Task<List<WorkOrderRequest4>> GetWorkOrdersByIdsAsync(List<string> Ids)
    {
        var result = new List<WorkOrderRequest4>();

        // Get WorkOrders list ID
        var workOrdersList = await GetListByTitleAsync(WORKORDERS_LIST);
        var lotsList = await GetListByTitleAsync(LOTS_LIST);
        var ordersList = await GetListByTitleAsync(ORDERS_LIST);

        if (workOrdersList == null || lotsList == null || ordersList == null)
            throw new Exception("Required lists not found in SharePoint");
        foreach (var woId in Ids)
        {
            // Get all unpoured work orders (IsPoured = No or null)
            var workOrderItems = await _graphClient.Sites[_siteId]
                .Lists[workOrdersList.Id]
                .Items
                .GetAsync(config =>
                {
                    config.QueryParameters.Expand = new[] { "fields" };
                    //config.QueryParameters.Filter = "fields/IsPoured ne 'Yes'";
                    config.QueryParameters.Filter = $"fields/Id eq '{woId}'";
                });

            foreach (var woItem in workOrderItems.Value)
            {
                var fields = woItem.Fields.AdditionalData;
                var orderdate = GetFieldValue<DateTime>(fields, "Work_x0020_Order_x0020_Date");
                var expectedDeliveryDate = GetFieldValue<DateTime>(fields, "Delivery_x0020_Date");
                var workOrder = new WorkOrderRequest4
                {
                    OrderDate = orderdate.ToString(),
                    PurchaseOrder = GetFieldValue<string>(fields, "Purchase_x0020_Order_x0020_Numbe"),
                    Company = await GetLookupDisplayValue(fields, "Customer"),
                    Contact = await GetLookupDisplayValue(fields, "Contact"),
                    Builder = await GetLookupDisplayValue(fields, "Builder"),
                    Site = GetFieldValue<string>(fields, "Site"),
                    City = await GetLookupDisplayValue(fields, "City"),
                    Notes = GetFieldValue<string>(fields, "Notes"),
                    ExpectedDeliveryDate = expectedDeliveryDate.ToString(),
                    Priority = DeterminePriority(fields)
                };

                // Get lots for this work order
                var lots = await GetLotsForWorkOrderAsync(lotsList.Id, woItem.Id);

                // Get orders for each lot and aggregate
                foreach (var lot in lots)
                {
                    var orders = await GetOrdersForLotAsync(ordersList.Id, lot.LotId);

                    foreach (var order in orders)
                    {
                        // Only include orders that haven't been fully poured
                        if (order.Quantity > order.QuantityPoured)
                        {
                            workOrder.Items.Add(new Order
                            {
                                LotName = lot.LotNumber,
                                Quantity = (int)(order.Quantity - order.QuantityPoured), // Remaining quantity
                                FinishedLength = (double)order.FinishedLength,
                                FinishedWidth = (double)order.FinishedWidth,
                                Color = order.Color,
                                Type = order.ProductType,
                                PourWidth = (double)order.PouredWidth,
                                PourLength = (double)order.PouredLength
                            });
                        }
                    }
                }

                // Only add work orders that have items to pour
                if (workOrder.Items.Any())
                {
                    result.Add(workOrder);
                }
            }
        }

        return result;
    }
public async Task<List<WorkOrderRequest4>> GetWorkOrdersByWorkIdsAsync(List<WorkOrderRequest4Dto> wkOrder)
    {
        var result = new List<WorkOrderRequest4>();

        // Get WorkOrders list ID
        var workOrdersList = await GetListByTitleAsync(WORKORDERS_LIST);
        var lotsList = await GetListByTitleAsync(LOTS_LIST);
        var ordersList = await GetListByTitleAsync(ORDERS_LIST);

        if (workOrdersList == null || lotsList == null || ordersList == null)
            throw new Exception("Required lists not found in SharePoint");
        foreach (var woId in wkOrder)
        {
           

             
                
                var workOrder = new WorkOrderRequest4
                {
                    OrderDate = woId.OrderDate,
                    PurchaseOrder = woId.PurchaseOrder,
                    Company = woId.Company,
                    Contact = woId.Contact,
                    Builder = woId.Builder,
                    Site = woId.Site,
                    City = woId.City,
                    Notes = woId.Notes,
                    ExpectedDeliveryDate = woId.ExpectedDeliveryDate
                };

                // Get lots for this work order
                var lots = await GetLotsForWorkOrderAsync(lotsList.Id, woId.Id);

                // Get orders for each lot and aggregate
                foreach (var lot in lots)
                {
                    var orders = await GetOrdersForLotAsync(ordersList.Id, lot.LotId);

                    foreach (var order in orders)
                    {
                        // Only include orders that haven't been fully poured
                        if (order.Quantity > order.QuantityPoured)
                        {
                            workOrder.Items.Add(new Order
                            {
                                LotName = lot.LotNumber,
                                Quantity = (int)(order.Quantity - order.QuantityPoured), // Remaining quantity
                                FinishedLength = (double)order.FinishedLength,
                                FinishedWidth = (double)order.FinishedWidth,
                                Color = order.Color,
                                Type = order.ProductType,
                                PourWidth = (double)order.PouredWidth,
                                PourLength = (double)order.PouredLength
                            });
                        }
                    }
                }

                // Only add work orders that have items to pour
                if (workOrder.Items.Any())
                {
                    result.Add(workOrder);
                }
            
        }

        return result;
    }

    /// <summary>
    /// Updates SharePoint after pour plan execution
    /// </summary>
    public async Task<bool> UpdatePourProgressAsync(List<PourProgressUpdate> updates)
    {
        var ordersList = await GetListByTitleAsync(ORDERS_LIST);
        var workOrdersList = await GetListByTitleAsync(WORKORDERS_LIST);

        // Group updates by work order
        var updatesByWorkOrder = updates.GroupBy(u => u.PurchaseOrder);

        foreach (var woGroup in updatesByWorkOrder)
        {
            // Update each order item's QuantityPoured
            foreach (var update in woGroup)
            {
                var orderItem = await FindOrderItemAsync(ordersList.Id, update.LotName, update.ProductDetails);

                if (orderItem != null)
                {
                    var currentQtyPoured = GetFieldValue<decimal>(orderItem.Fields.AdditionalData, "QuantityPoured");
                    var newQtyPoured = currentQtyPoured + update.QuantityPouredToday;

                    await UpdateListItemAsync(ordersList.Id, orderItem.Id, new Dictionary<string, object>
                    {
                        { "QuantityPoured", newQtyPoured }
                    });
                }
            }

            // Check if work order is now fully poured
            var workOrderItem = await FindWorkOrderByPOAsync(workOrdersList.Id, woGroup.Key);

            if (workOrderItem != null)
            {
                var isFullyPoured = await CheckIfWorkOrderFullyPouredAsync(workOrderItem.Id);

                if (isFullyPoured)
                {
                    await UpdateListItemAsync(workOrdersList.Id, workOrderItem.Id, new Dictionary<string, object>
                    {
                        { "IsPoured", true }
                    });
                }
            }
        }

        return true;
    }

    public Task<List<WorkOrderWithDetails>> GetWorkOrderDetailsAsync(string filter = null)
    {
        throw new NotImplementedException();
    }


    //>>>>
    private async Task<List<LotInfo>> GetLotsForWorkOrderAsync(string lotsListId, string workOrderItemId)
    {
        var lots = new List<LotInfo>();

        var lotItems = await _graphClient.Sites[_siteId]
            .Lists[lotsListId]
            .Items
            .GetAsync(config =>
            {
                config.QueryParameters.Expand = new[] { "fields" };
                config.QueryParameters.Filter =
                    $"fields/Purchase_x0020_Order_x0020_NumbeLookupId eq '{workOrderItemId}'";
            });

        foreach (var item in lotItems.Value)
        {
            lots.Add(new LotInfo
            {
                LotId = item.Id,
                LotNumber = GetFieldValue<string>(item.Fields.AdditionalData, "Lot_x0020_Number"),
                BlockNumber = GetFieldValue<string>(item.Fields.AdditionalData, "Block_x0020_Number")
            });
        }

        return lots;
    }

    private async Task<List<OrderInfo>> GetOrdersForLotAsync(string ordersListId, string lotId)
    {
        var orders = new List<OrderInfo>();
        var orderItems = await _graphClient.Sites[_siteId]
            .Lists[ordersListId]
            .Items
            .GetAsync(config =>
            {
                config.QueryParameters.Expand = new[] { "fields" };
                config.QueryParameters.Filter = $"fields/Lot_x0020_NumberLookupId eq '{lotId}'";
            });

        foreach (var item in orderItems.Value)
        {
            var fields = item.Fields.AdditionalData;
            orders.Add(new OrderInfo
            {
                OrderId = item.Id,
                ProductType = GetFieldValue<string>(fields, "Product_x0020_Type"),
                Color = GetFieldValue<string>(fields, "Color"),
                Quantity = GetFieldValue<decimal>(fields, "Quantity"),
                QuantityPoured = GetFieldValue<decimal>(fields, "QuantityPoured"),
                FinishedLength = GetFieldValue<decimal>(fields, "FinishedLength"),
                FinishedWidth = GetFieldValue<decimal>(fields, "FinishedWidth"),
                PouredWidth = GetFieldValue<decimal>(fields, "PouredWidth"),
                PouredLength = GetFieldValue<decimal>(fields, "PouredLength")
            });
        }

        return orders;
    }

    private async Task<bool> CheckIfWorkOrderFullyPouredAsync(string workOrderItemId)
    {
        var lotsList = await GetListByTitleAsync(LOTS_LIST);
        var ordersList = await GetListByTitleAsync(ORDERS_LIST);

        // Get all lots for this work order
        var lots = await GetLotsForWorkOrderAsync(lotsList.Id, workOrderItemId);

        // Check all orders for all lots
        foreach (var lot in lots)
        {
            var orders = await GetOrdersForLotAsync(ordersList.Id, lot.LotId);

            // If any order has remaining quantity, return false
            if (orders.Any(o => o.Quantity > o.QuantityPoured))
            {
                return false;
            }
        }

        return true;
    }

    private async Task<ListItem> FindOrderItemAsync(string ordersListId, string lotName, string productDetails)
    {
        string filter = $"fields/Lot_x0020_Number/Value eq '{lotName}'";

        if (!string.IsNullOrEmpty(productDetails))
        {
            // Example parsing: assume last word is color, rest is type
            // var parts = productDetails.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            // if (parts.Length >= 2)
            // {
            //     string color = parts[^1];
            //     string productType = string.Join(" ", parts[0..^1]);
            //     filter += $" and fields/Product_x0020_Type/Value eq '{productType}' and fields/Color/Value eq '{color}'";
            // }
        }

        var items = await _graphClient.Sites[_siteId]
            .Lists[ordersListId]
            .Items
            .GetAsync(config =>
            {
                config.QueryParameters.Expand = new[] { "fields" };
                config.QueryParameters.Filter = filter;
            });

        return items.Value.FirstOrDefault();
    }
    // private async Task<ListItem> FindOrderItemAsync(string ordersListId, string lotName, string productDetails)
    // {
    //     // This would need refinement based on how you uniquely identify orders
    //     var items = await _graphClient.Sites[_siteId]
    //         .Lists[ordersListId]
    //         .Items
    //         .GetAsync(config =>
    //         {
    //             config.QueryParameters.Expand = new[] { "fields" };
    //             // Add filter logic to find specific order
    //         });
    //
    //     return items.Value.FirstOrDefault();
    // }

    private async Task<ListItem> FindWorkOrderByPOAsync(string workOrdersListId, string purchaseOrder)
    {
        var items = await _graphClient.Sites[_siteId]
            .Lists[workOrdersListId]
            .Items
            .GetAsync(config =>
            {
                config.QueryParameters.Expand = new[] { "fields" };
                config.QueryParameters.Filter = $"fields/Purchase_x0020_Order_x0020_Numbe eq '{purchaseOrder}'";
            });

        return items?.Value.FirstOrDefault();
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
            {
                return (T)(object)jsonElement.GetInt32();
            }

            if (targetType == typeof(double))
            {
                return (T)(object)jsonElement.GetDouble();
            }

            if (targetType == typeof(DateTime))
            {
                return (T)(object)jsonElement.GetDateTime();
            }

            if (targetType == typeof(decimal))
            {
                return (T)(object)jsonElement.GetDecimal();
            }

            if (targetType == typeof(string))
            {
                return (T)(object)jsonElement.GetString();
            }

            if (targetType == typeof(bool))
            {
                return (T)(object)jsonElement.GetBoolean();
            }
        }

        if (value is T typedValue)
        {
            return typedValue;
        }

        return default(T);
    }

    private async Task<string> GetLookupDisplayValue(IDictionary<string, object> fields, string fieldName)
    {
        // Handle SharePoint lookup fields
        var lookupFieldName = fieldName + "LookupId";
        if (fields.TryGetValue(lookupFieldName, out var lookupId))
        {
            // Fetch the actual lookup value if needed
            return lookupId?.ToString() ?? string.Empty;
        }

        return string.Empty;
    }

    private int DeterminePriority(IDictionary<string, object> fields)
    {
        try
        {
            // Implement priority logic based on delivery date, order date, etc. 
            var date = GetFieldValue<DateTime>(fields, "Delivery_x0020_Date");
            // if (DateTime.TryParse(deliveryDate, out var date))
            // {
            var daysUntilDelivery = (date - DateTime.Now).Days;
            if (daysUntilDelivery < 7) return 1;
            if (daysUntilDelivery < 14) return 2;
            return 3;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return 5;
    }

    public async Task<List> GetListByTitleAsync(string listTitle)
    {
        var lists = await _graphClient.Sites[_siteId].Lists.GetAsync(config =>
        {
            config.QueryParameters.Filter = $"displayName eq '{listTitle}'";
        });
        return lists.Value.FirstOrDefault();
    }

    public async Task<bool> UpdateListItemAsync(string listId, string itemId, Dictionary<string, object> fields)
    {
        var fieldValueSet = new FieldValueSet
        {
            AdditionalData = fields
        };

        var result = await _graphClient.Sites[_siteId]
            .Lists[listId]
            .Items[itemId]
            .Fields
            .PatchAsync(fieldValueSet);

        return result != null;
    }

    //----
    // Get all lists in the site
    public async Task<List<List>> GetAllListsAsync()
    {
        var lists = await _graphClient.Sites[_siteId].Lists.GetAsync();
        return lists.Value.ToList();
    }

    // Get a specific list by title
    // public async Task<List> GetListByTitleAsync(string listTitle)
    // {
    //     var lists = await _graphClient.Sites[_siteId].Lists.GetAsync(config =>
    //     {
    //         config.QueryParameters.Filter = $"displayName eq '{listTitle}'";
    //     });
    //     
    //     return lists.Value.FirstOrDefault();
    // }

    // Get all items from a list
    public async Task<List<ListItem>> GetListItemsAsync(string listId)
    {
        var items = await _graphClient.Sites[_siteId].Lists[listId].Items.GetAsync(config =>
        {
            config.QueryParameters.Expand = ["fields"];
        });

        return items.Value.ToList();
    }

    // Get list items with filtering
    public async Task<List<ListItem>> GetFilteredListItemsAsync(string listId, string filter)
    {
        var items = await _graphClient.Sites[_siteId].Lists[listId].Items.GetAsync(config =>
        {
            config.QueryParameters.Expand = ["fields"];
            config.QueryParameters.Filter = filter;
        });

        return items.Value.ToList();
    }

    // Create a new list item
    public async Task<ListItem> CreateListItemAsync(string listId, Dictionary<string, object> fields)
    {
        var newItem = new ListItem
        {
            Fields = new FieldValueSet
            {
                AdditionalData = fields
            }
        };

        return await _graphClient.Sites[_siteId].Lists[listId].Items.PostAsync(newItem);
    }

    // Update an existing list item
    // public async Task<bool> UpdateListItemAsync(string listId, string itemId, Dictionary<string, object> fields)
    // {
    //     var fieldValueSet = new FieldValueSet
    //     {
    //         AdditionalData = fields
    //     };
    //
    //     var  result =  await _graphClient.Sites[_siteId].Lists[listId].Items[itemId]
    //         .Fields.PatchAsync(fieldValueSet);
    //
    //     return result != null;
    // }

    // Delete a list item
    public async Task DeleteListItemAsync(string listId, string itemId)
    {
        await _graphClient.Sites[_siteId].Lists[listId].Items[itemId].DeleteAsync();
    }

    // Get list columns/fields
    public async Task<List<ColumnDefinition>> GetListColumnsAsync(string listId)
    {
        var columns = await _graphClient.Sites[_siteId].Lists[listId].Columns.GetAsync();
        return columns.Value.ToList();
    }

    // Batch operations for better performance
    public async Task<List<ListItem>> GetListItemsInBatchesAsync(string listId, int batchSize = 100)
    {
        var allItems = new List<ListItem>();
        var pageIterator = PageIterator<ListItem, ListItemCollectionResponse>
            .CreatePageIterator(
                _graphClient,
                await _graphClient.Sites[_siteId].Lists[listId].Items.GetAsync(config =>
                {
                    config.QueryParameters.Expand = new string[] { "fields" };
                    config.QueryParameters.Top = batchSize;
                }),
                item =>
                {
                    allItems.Add(item);
                    return true;
                });

        await pageIterator.IterateAsync();
        return allItems;
    }


    //file uploads
    //file uploads

    public async Task<FileUploadResult> UploadFileToSharePointAsync(
        byte[] fileContent,
        string fileName,
        string folderPath,
        string driveId = null,
        bool allowReplaceFile = false)
    {
        try
        {
            // Ensure folder path doesn't start with /
            folderPath = folderPath.TrimStart('/');

            // Generate unique filename with timestamp
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var uniqueFileName = $"{fileNameWithoutExtension}_{timestamp}{extension}";

            // Get the drive
            Drive defaultDrive = null;
            if (string.IsNullOrEmpty(driveId))
            {
                var drives = await _graphClient.Sites[_siteId].Drives.GetAsync();
                defaultDrive = drives?.Value?.FirstOrDefault(d => d.Name == "Documents");
                if (defaultDrive == null)
                {
                    return new FileUploadResult
                    {
                        Success = false,
                        Message = "No document library found for this site"
                    };
                }

                driveId = defaultDrive.Id;
            }

            // Check if file already exists in the folder
            var existingFile = await CheckFileExistsAsync(driveId, folderPath, fileName);

            if (existingFile != null)
            {
                if (allowReplaceFile)
                {
                    // Delete the existing file
                    await _graphClient.Drives[driveId]
                        .Items[existingFile.Id]
                        .DeleteAsync();

                    // Use the original filename
                    uniqueFileName = fileName;
                }
                else
                {
                    // Use unique filename to avoid conflicts
                    // Keep the timestamp-based unique name
                }
            }
            else
            {
                // No existing file, can use original name if preferred
                uniqueFileName = fileName;
            }

            // Upload the file
            using (var stream = new MemoryStream(fileContent))
            {
                var uploadedItem = await _graphClient.Drives[driveId]
                    .Root
                    .ItemWithPath($"{folderPath}/{uniqueFileName}")
                    .Content
                    .PutAsync(stream);

                return new FileUploadResult
                {
                    Success = true,
                    Message = existingFile != null && allowReplaceFile
                        ? "File replaced successfully"
                        : "File uploaded successfully",
                    FileUrl = uploadedItem.WebUrl,
                    FileName = uniqueFileName,
                    WasReplaced = existingFile != null && allowReplaceFile
                };
            }
        }
        catch (ODataError odataError)
        {
            return new FileUploadResult
            {
                Success = false,
                Message = $"SharePoint API Error: {odataError.Error?.Message ?? odataError.Message}"
            };
        }
        catch (Exception ex)
        {
            return new FileUploadResult
            {
                Success = false,
                Message = $"Error uploading file: {ex.Message}"
            };
        }
    }

// Helper method to check if file exists
    private async Task<DriveItem> CheckFileExistsAsync(string driveId, string folderPath, string fileName)
    {
        try
        {
            var item = await _graphClient
                .Drives[driveId]
                .Root
                .ItemWithPath($"{folderPath}/{fileName}")
                .GetAsync();

            return item;
        }
        catch (ODataError)
        {
            // File doesn't exist
            return null;
        }
        catch
        {
            // Any other error, assume file doesn't exist
            return null;
        }
    }

// Optional: Method to ensure folder exists (creates if needed)
    private async Task<bool> EnsureFolderExistsAsync(string driveId, string folderPath)
    {
        try
        {
            var parts = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var currentPath = "";

            foreach (var part in parts)
            {
                currentPath = string.IsNullOrEmpty(currentPath) ? part : $"{currentPath}/{part}";

                try
                {
                    await _graphClient
                        .Drives[driveId]
                        .Root
                        .ItemWithPath(currentPath)
                        .GetAsync();
                }
                catch (ODataError)
                {
                    // Folder doesn't exist, create it
                    var parentPath = currentPath.Contains('/')
                        ? currentPath.Substring(0, currentPath.LastIndexOf('/'))
                        : "";

                    var driveItem = new DriveItem
                    {
                        Name = part,
                        Folder = new Folder(),
                        AdditionalData = new Dictionary<string, object>
                        {
                            { "@microsoft.graph.conflictBehavior", "rename" }
                        }
                    };

                    if (string.IsNullOrEmpty(parentPath))
                    {
                        //     await _graphClient
                        //         .Drives[driveId]
                        //         .Root
                        //         .Children
                        //         .PostAsync(driveItem);
                    }
                    else
                    {
                        await _graphClient
                            .Drives[driveId]
                            .Root
                            .ItemWithPath(parentPath)
                            .Children
                            .PostAsync(driveItem);
                    }
                }
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

// Enhanced upload method with folder creation
    public async Task<FileUploadResult> UploadFileToSharePointWithFolderCreationAsync(
        byte[] fileContent,
        string fileName,
        string folderPath,
        string driveId = null,
        bool allowReplaceFile = false,
        bool createFolderIfNotExists = true)
    {
        try
        {
            folderPath = folderPath.TrimStart('/');

            // Get the driveId if not provided
            if (string.IsNullOrEmpty(driveId))
            {
                var drives = await _graphClient.Sites[_siteId].Drives.GetAsync();
                var defaultDrive = drives?.Value?.FirstOrDefault();
                if (defaultDrive == null)
                {
                    return new FileUploadResult
                    {
                        Success = false,
                        Message = "No document library found for this site"
                    };
                }

                driveId = defaultDrive.Id;
            }

            // Ensure folder exists if requested
            if (createFolderIfNotExists)
            {
                var folderCreated = await EnsureFolderExistsAsync(driveId, folderPath);
                if (!folderCreated)
                {
                    return new FileUploadResult
                    {
                        Success = false,
                        Message = "Failed to create or verify folder path"
                    };
                }
            }

            // Call the main upload method
            return await UploadFileToSharePointAsync(fileContent, fileName, folderPath, driveId, allowReplaceFile);
        }
        catch (Exception ex)
        {
            return new FileUploadResult
            {
                Success = false,
                Message = $"Error in upload with folder creation: {ex.Message}"
            };
        }
    }
}

// Add this class at the bottom with your other model classes
public class FileUploadResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string FileUrl { get; set; }
    public string FileName { get; set; }
    public bool WasReplaced { get; set; }
}

public class LotInfo
{
    public string LotId { get; set; }
    public string LotNumber { get; set; }
    public string BlockNumber { get; set; }
}

public class OrderInfo
{
    public string OrderId { get; set; }
    public string ProductType { get; set; }
    public string Color { get; set; }

    public decimal Quantity { get; set; }
    public decimal QuantityPoured { get; set; }
    public decimal FinishedLength { get; set; }
    public decimal FinishedWidth { get; set; }
    public decimal PouredWidth { get; set; }
    public decimal PouredLength { get; set; }
}
// public class OrderInfo
// {
//     public string OrderId { get; set; }
//     public string ProductType { get; set; }
//     public string Color { get; set; }
//     public int Quantity { get; set; }
//     public int QuantityPoured { get; set; }
//     public double FinishedLength { get; set; }
//     public double FinishedWidth { get; set; }
//     public double PouredWidth { get; set; }
//     public double PouredLength { get; set; }
// }

public class PourProgressUpdate
{
    public string PurchaseOrder { get; set; }
    public string LotName { get; set; }
    public string ProductDetails { get; set; }
    public int QuantityPouredToday { get; set; }
    public string PourDate { get; set; }
}

public class WorkOrderWithDetails
{
    public string WorkOrderId { get; set; }
    public string PurchaseOrder { get; set; }
    public bool IsPoured { get; set; }
    public List<LotInfo> Lots { get; set; }
    public List<OrderInfo> Orders { get; set; }
}