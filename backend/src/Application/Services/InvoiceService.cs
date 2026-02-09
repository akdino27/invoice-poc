using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace invoice_v1.src.Application.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InvoiceService> _logger;

        public InvoiceService(ApplicationDbContext context, ILogger<InvoiceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<InvoiceDto> CreateOrUpdateInvoiceFromCallbackAsync(Guid jobId, object result)
        {
            var resultJson = JsonSerializer.Serialize(result);
            var extractedData = JsonSerializer.Deserialize<JsonElement>(resultJson);

            //  VALIDATE CRITICAL FIELDS
            ValidateCriticalFields(extractedData);

            //  GET JOB AND FILE INFO 
            var job = await _context.JobQueues.FindAsync(jobId);
            if (job == null)
            {
                throw new InvalidOperationException($"Job {jobId} not found");
            }

            if (job.PayloadJson == null)
            {
                throw new InvalidOperationException("Job payload is missing");
            }

            var payload = job.PayloadJson.RootElement;

            var fileId = payload.GetProperty("fileId").GetString()
                ?? throw new InvalidOperationException("FileId not found in job payload");
            var fileName = GetStringProperty(payload, "originalName");

            // FIND OR CREATE INVOICE 
            var existingInvoice = await _context.Invoices
                .Include(i => i.LineItems)
                .FirstOrDefaultAsync(i => i.DriveFileId == fileId);

            Invoice invoice;
            if (existingInvoice != null)
            {
                invoice = existingInvoice;
                invoice.UpdatedAt = DateTime.UtcNow;
                _logger.LogInformation("Updating existing invoice {InvoiceId}", invoice.Id);

                // Remove old line items
                _context.InvoiceLines.RemoveRange(existingInvoice.LineItems);
                invoice.LineItems.Clear();
            }
            else
            {
                invoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    DriveFileId = fileId,
                    OriginalFileName = fileName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Invoices.Add(invoice);
                _logger.LogInformation("Creating new invoice {InvoiceId}", invoice.Id);
            }

            // MAP INVOICE HEADER FIELDS (WITH NULL SAFETY)

            invoice.InvoiceNumber = GetStringProperty(extractedData, "InvoiceNumber");
            invoice.OrderId = GetStringProperty(extractedData, "OrderId");
            invoice.VendorName = GetStringProperty(extractedData, "VendorName");
            invoice.ShipMode = GetStringProperty(extractedData, "ShipMode");
            invoice.Currency = GetStringProperty(extractedData, "Currency") ?? "USD";  // Default to USD
            invoice.Notes = GetStringProperty(extractedData, "Notes");
            invoice.Terms = GetStringProperty(extractedData, "Terms");

            // Parse invoice date
            invoice.InvoiceDate = GetDateTimeProperty(extractedData, "InvoiceDate");

            // ===== PARSE BILLTO OBJECT =====
            if (extractedData.TryGetProperty("BillTo", out var billToElement)
                && billToElement.ValueKind == JsonValueKind.Object)
            {
                invoice.BillToName = GetStringProperty(billToElement, "Name");
            }
            else
            {
                invoice.BillToName = null;
            }

            //  PARSE SHIPTO OBJECT 
            if (extractedData.TryGetProperty("ShipTo", out var shipToElement)
                && shipToElement.ValueKind == JsonValueKind.Object)
            {
                invoice.ShipToCity = GetStringProperty(shipToElement, "City");
                invoice.ShipToState = GetStringProperty(shipToElement, "State");
                invoice.ShipToCountry = GetStringProperty(shipToElement, "Country");
            }
            else
            {
                invoice.ShipToCity = null;
                invoice.ShipToState = null;
                invoice.ShipToCountry = null;
            }

            //  PARSE FINANCIAL FIELDS (ALL NULLABLE) 
            invoice.Subtotal = GetDecimalProperty(extractedData, "Subtotal");
            invoice.ShippingCost = GetDecimalProperty(extractedData, "ShippingCost");
            invoice.TotalAmount = GetDecimalProperty(extractedData, "TotalAmount");
            invoice.BalanceDue = GetDecimalProperty(extractedData, "BalanceDue");

            //  PARSE DISCOUNT OBJECT (SPECIAL CASE)
            if (extractedData.TryGetProperty("Discount", out var discountElement)
                && discountElement.ValueKind == JsonValueKind.Object)
            {
                invoice.DiscountPercentage = GetDecimalProperty(discountElement, "Percentage");
                invoice.DiscountAmount = GetDecimalProperty(discountElement, "Amount");
            }
            else
            {
                // Discount is null or missing
                invoice.DiscountPercentage = null;
                invoice.DiscountAmount = null;
            }

            // Store full extracted JSON
            invoice.ExtractedDataJson = JsonDocument.Parse(resultJson);


            //  PROCESS LINE ITEMS 
            if (!extractedData.TryGetProperty("LineItems", out var lineItemsElement)
                || lineItemsElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("LineItems array is required");
            }

            var lineItemsArray = lineItemsElement.EnumerateArray().ToList();
            if (lineItemsArray.Count == 0)
            {
                throw new InvalidOperationException("Invoice must have at least one line item");
            }

            // Track which products we've already counted for this invoice
            var processedProducts = new HashSet<Guid>();

            foreach (var lineElement in lineItemsArray)
            {
                var productId = GetStringProperty(lineElement, "ProductId");
                if (string.IsNullOrWhiteSpace(productId))
                {
                    _logger.LogWarning("Skipping line item with missing ProductId");
                    continue;
                }

                var productName = GetStringProperty(lineElement, "ProductName") ?? "Unknown";
                var category = GetStringProperty(lineElement, "Category");
                var quantity = GetDecimalProperty(lineElement, "Quantity") ?? 0;
                var unitRate = GetDecimalProperty(lineElement, "UnitRate") ?? 0;
                var amount = GetDecimalProperty(lineElement, "Amount") ?? 0;

                // Validate critical line item fields
                if (quantity <= 0)
                {
                    _logger.LogWarning("Skipping line item {ProductId} with invalid quantity: {Quantity}",
                        productId, quantity);
                    continue;
                }

                // Find or create product WITHOUT calling SaveChanges
                var product = await FindOrCreateProductNoSaveAsync(
                    productId,
                    productName,
                    category,
                    unitRate
                );

                // Create invoice line
                var line = new InvoiceLine
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    ProductGuid = product.Id,
                    ProductId = productId,
                    ProductName = productName,
                    Category = category,
                    Quantity = quantity,
                    UnitRate = unitRate,
                    Amount = amount,
                    CreatedAt = DateTime.UtcNow
                };

                invoice.LineItems.Add(line);

                //  UPDATE PRODUCT STATISTICS 
                product.TotalQuantitySold += quantity;
                product.TotalRevenue += amount;

                if (!processedProducts.Contains(product.Id))
                {
                    product.InvoiceCount++;
                    processedProducts.Add(product.Id);
                }

                // Update last sold date
                if (invoice.InvoiceDate.HasValue)
                {
                    if (!product.LastSoldDate.HasValue
                        || invoice.InvoiceDate.Value > product.LastSoldDate.Value)
                    {
                        product.LastSoldDate = invoice.InvoiceDate.Value;
                    }
                }

                product.UpdatedAt = DateTime.UtcNow;
            }

            // Validate at least one valid line item was processed
            if (invoice.LineItems.Count == 0)
            {
                throw new InvalidOperationException(
                    "No valid line items found. All line items were skipped due to missing or invalid data.");
            }

            // SAVE EVERYTHING ATOMICALLY 
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Invoice {InvoiceId} ({InvoiceNumber}) saved successfully with {LineCount} line items",
                invoice.Id,
                invoice.InvoiceNumber ?? "N/A",
                invoice.LineItems.Count);

            return await MapToDtoAsync(invoice);
        }

        // Validates that critical fields are present and valid.
        private void ValidateCriticalFields(JsonElement extractedData)
        {
            var errors = new List<string>();

            // Critical Field 1: InvoiceNumber
            var invoiceNumber = GetStringProperty(extractedData, "InvoiceNumber");
            if (string.IsNullOrWhiteSpace(invoiceNumber))
            {
                errors.Add("InvoiceNumber is required");
            }

            // Critical Field 2: TotalAmount
            var totalAmount = GetDecimalProperty(extractedData, "TotalAmount");
            if (!totalAmount.HasValue || totalAmount.Value <= 0)
            {
                errors.Add("TotalAmount is required and must be greater than 0");
            }

            // Critical Field 3: LineItems array must exist
            if (!extractedData.TryGetProperty("LineItems", out var lineItems)
                || lineItems.ValueKind != JsonValueKind.Array)
            {
                errors.Add("LineItems array is required");
            }
            else if (lineItems.GetArrayLength() == 0)
            {
                errors.Add("LineItems array must contain at least one item");
            }

            if (errors.Any())
            {
                var errorMessage = "Critical validation failed: " + string.Join("; ", errors);
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
        }

        public async Task<InvoiceDto?> GetInvoiceByIdAsync(Guid id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.LineItems)
                    .ThenInclude(l => l.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id);

            return invoice != null ? await MapToDtoAsync(invoice) : null;
        }

        public async Task<InvoiceDto?> GetInvoiceByFileIdAsync(string fileId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.LineItems)
                    .ThenInclude(l => l.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.DriveFileId == fileId);

            return invoice != null ? await MapToDtoAsync(invoice) : null;
        }

        // Find existing product or create new one WITHOUT calling SaveChanges.
        private async Task<Product> FindOrCreateProductNoSaveAsync(
            string productId,
            string productName,
            string? category,
            decimal? unitRate)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
            {
                // Parse category into primary and secondary
                string? primaryCategory = null;
                string? secondaryCategory = null;

                if (!string.IsNullOrWhiteSpace(category))
                {
                    var parts = category.Split(',', StringSplitOptions.TrimEntries);
                    if (parts.Length > 0) primaryCategory = parts[0];
                    if (parts.Length > 1) secondaryCategory = parts[1];
                }

                product = new Product
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    ProductName = productName,
                    Category = category,
                    PrimaryCategory = primaryCategory,
                    SecondaryCategory = secondaryCategory,
                    DefaultUnitRate = unitRate,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);

                _logger.LogInformation(
                    "Created new product {ProductId}: {ProductName} ({Category})",
                    productId,
                    productName,
                    category ?? "N/A");
            }
            else
            {
                // Update product name/category if changed
                if (product.ProductName != productName || product.Category != category)
                {
                    product.ProductName = productName;
                    product.Category = category;

                    // Re-parse category
                    if (!string.IsNullOrWhiteSpace(category))
                    {
                        var parts = category.Split(',', StringSplitOptions.TrimEntries);
                        product.PrimaryCategory = parts.Length > 0 ? parts[0] : null;
                        product.SecondaryCategory = parts.Length > 1 ? parts[1] : null;
                    }
                    else
                    {
                        product.PrimaryCategory = null;
                        product.SecondaryCategory = null;
                    }

                    product.UpdatedAt = DateTime.UtcNow;
                }
            }

            return product;
        }

        private async Task<InvoiceDto> MapToDtoAsync(Invoice invoice)
        {
            object? extractedData = null;

            if (invoice.ExtractedDataJson != null)
            {
                extractedData = JsonSerializer.Deserialize<object>(
                    invoice.ExtractedDataJson.RootElement.GetRawText()
                );
            }


            return new InvoiceDto
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                OrderId = invoice.OrderId,
                VendorName = invoice.VendorName,
                BillToName = invoice.BillToName,
                ShipTo = new ShipToDto
                {
                    City = invoice.ShipToCity,
                    State = invoice.ShipToState,
                    Country = invoice.ShipToCountry
                },
                ShipMode = invoice.ShipMode,
                Subtotal = invoice.Subtotal,
                Discount = new DiscountDto
                {
                    Percentage = invoice.DiscountPercentage,
                    Amount = invoice.DiscountAmount
                },
                ShippingCost = invoice.ShippingCost,
                TotalAmount = invoice.TotalAmount,
                BalanceDue = invoice.BalanceDue,
                Currency = invoice.Currency,
                Notes = invoice.Notes,
                Terms = invoice.Terms,
                DriveFileId = invoice.DriveFileId,
                OriginalFileName = invoice.OriginalFileName,
                ExtractedData = extractedData,
                CreatedAt = invoice.CreatedAt,
                UpdatedAt = invoice.UpdatedAt,
                LineItems = invoice.LineItems.Select(l => new InvoiceLineDto
                {
                    Id = l.Id,
                    ProductName = l.ProductName,
                    Category = l.Category,
                    ProductId = l.ProductId,
                    Quantity = l.Quantity,
                    UnitRate = l.UnitRate,
                    Amount = l.Amount
                }).ToList()
            };
        }


        // Safely extracts a string property from JSON.
        private string? GetStringProperty(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var prop))
                return null;  // Property doesn't exist

            if (prop.ValueKind == JsonValueKind.Null)
                return null;

            if (prop.ValueKind == JsonValueKind.String)
                return prop.GetString();

            // Log unexpected type for debugging
            _logger.LogDebug(
                "Property {PropertyName} has unexpected type {ValueKind}, expected String or Null",
                propertyName,
                prop.ValueKind);

            return null;
        }


        // Returns null if property doesn't exist, is null, or cannot be parsed.
        private decimal? GetDecimalProperty(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var prop))
                return null;  // Property doesn't exist

            if (prop.ValueKind == JsonValueKind.Null)
                return null;

            if (prop.ValueKind == JsonValueKind.Number)
            {
                try
                {
                    return prop.GetDecimal();
                }
                catch (FormatException)
                {
                    // Number is too large for decimal, try double
                    try
                    {
                        return (decimal)prop.GetDouble();
                    }
                    catch
                    {
                        _logger.LogWarning(
                            "Failed to parse {PropertyName} as decimal (value out of range)",
                            propertyName);
                        return null;
                    }
                }
            }

            if (prop.ValueKind == JsonValueKind.String)
            {
                var str = prop.GetString();
                if (!string.IsNullOrWhiteSpace(str) && decimal.TryParse(str, out var value))
                    return value;
            }

            _logger.LogDebug(
                "Property {PropertyName} has unexpected type {ValueKind}, expected Number or Null",
                propertyName,
                prop.ValueKind);

            return null;
        }

        // DateTime property from JSON.
        private DateTime? GetDateTimeProperty(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var prop))
                return null;

            if (prop.ValueKind == JsonValueKind.Null)
                return null;

            if (prop.ValueKind == JsonValueKind.String)
            {
                var str = prop.GetString();
                if (!string.IsNullOrWhiteSpace(str) && DateTime.TryParse(str, out var value))
                    return value;

                _logger.LogWarning(
                    "Failed to parse {PropertyName} as DateTime: {Value}",
                    propertyName,
                    str);
            }

            return null;
        }

        private int? GetIntProperty(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var prop))
                return null;

            if (prop.ValueKind == JsonValueKind.Null)
                return null;

            if (prop.ValueKind == JsonValueKind.Number)
            {
                try
                {
                    return prop.GetInt32();
                }
                catch (FormatException)
                {
                    try
                    {
                        return (int)prop.GetDouble();
                    }
                    catch
                    {
                        return null;
                    }
                }
            }

            if (prop.ValueKind == JsonValueKind.String)
            {
                var str = prop.GetString();
                if (int.TryParse(str, out var value))
                    return value;
            }

            return null;
        }
    }
}
