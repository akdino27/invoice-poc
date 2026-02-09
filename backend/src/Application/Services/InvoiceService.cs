using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Infrastructure.Data;
using invoice_v1.src.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace invoice_v1.src.Application.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext context;
        private readonly IInvoiceRepository invoiceRepository;
        private readonly IProductRepository productRepository;
        private readonly ILogger<InvoiceService> logger;

        public InvoiceService(
            ApplicationDbContext context,
            IInvoiceRepository invoiceRepository,
            IProductRepository productRepository,
            ILogger<InvoiceService> logger)
        {
            this.context = context;
            this.invoiceRepository = invoiceRepository;
            this.productRepository = productRepository;
            this.logger = logger;
        }

        public async Task<InvoiceDto> CreateOrUpdateInvoiceFromCallbackAsync(Guid jobId, object result)
        {
            var resultJson = JsonSerializer.Serialize(result);
            var extractedData = JsonSerializer.Deserialize<JsonElement>(resultJson);

            ValidateCriticalFields(extractedData);

            var job = await context.JobQueues.FindAsync(jobId);
            if (job == null)
                throw new InvalidOperationException($"Job {jobId} not found");

            if (job.PayloadJson == null)
            {
                throw new InvalidOperationException("Job payload is missing");
            }

            var payload = job.PayloadJson.RootElement;

            var fileId = payload.GetProperty("fileId").GetString()
                ?? throw new InvalidOperationException("FileId not found in job payload");
            var fileName = GetStringProperty(payload, "originalName");

            //Use a fresh query with no tracking to avoid concurrency issues
            var existingInvoice = await context.Invoices
                .AsNoTracking()
                .Include(i => i.LineItems)
                .FirstOrDefaultAsync(i => i.DriveFileId == fileId);

            Invoice invoice;
            bool isUpdate = false;

            if (existingInvoice != null)
            {
                // UPDATE EXISTING INVOICE
                logger.LogInformation("Updating existing invoice {InvoiceId} for file {FileId}",
                    existingInvoice.Id, fileId);

                // Delete existing line items in a separate operation
                var existingLineItemIds = await context.InvoiceLines
                    .Where(il => il.InvoiceId == existingInvoice.Id)
                    .Select(il => il.Id)
                    .ToListAsync();

                if (existingLineItemIds.Any())
                {
                    await context.Database.ExecuteSqlRawAsync(
                        "DELETE FROM InvoiceLines WHERE InvoiceId = {0}",
                        existingInvoice.Id);

                    logger.LogDebug("Deleted {Count} existing line items for invoice {InvoiceId}",
                        existingLineItemIds.Count, existingInvoice.Id);
                }

                // Create a new tracked instance
                invoice = await context.Invoices.FindAsync(existingInvoice.Id);
                if (invoice == null)
                    throw new InvalidOperationException($"Invoice {existingInvoice.Id} not found");

                invoice.LineItems.Clear();
                invoice.UpdatedAt = DateTime.UtcNow;
                isUpdate = true;
            }
            else
            {
                // CREATE NEW INVOICE
                logger.LogInformation("Creating new invoice for file {FileId}", fileId);

                invoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    DriveFileId = fileId,
                    OriginalFileName = fileName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Invoices.Add(invoice);
                isUpdate = false;
            }

            // Map all fields (same for both create and update)
            invoice.InvoiceNumber = GetStringProperty(extractedData, "InvoiceNumber");
            invoice.OrderId = GetStringProperty(extractedData, "OrderId");
            invoice.VendorName = GetStringProperty(extractedData, "VendorName");
            invoice.ShipMode = GetStringProperty(extractedData, "ShipMode");
            invoice.Currency = GetStringProperty(extractedData, "Currency") ?? "USD";
            invoice.Notes = GetStringProperty(extractedData, "Notes");
            invoice.Terms = GetStringProperty(extractedData, "Terms");
            invoice.InvoiceDate = GetDateTimeProperty(extractedData, "InvoiceDate");

            if (extractedData.TryGetProperty("BillTo", out var billToElement) &&
                billToElement.ValueKind == JsonValueKind.Object)
            {
                invoice.BillToName = GetStringProperty(billToElement, "Name");
            }
            else
            {
                invoice.BillToName = null;
            }

            if (extractedData.TryGetProperty("ShipTo", out var shipToElement) &&
                shipToElement.ValueKind == JsonValueKind.Object)
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

            invoice.Subtotal = GetDecimalProperty(extractedData, "Subtotal");
            invoice.ShippingCost = GetDecimalProperty(extractedData, "ShippingCost");
            invoice.TotalAmount = GetDecimalProperty(extractedData, "TotalAmount");
            invoice.BalanceDue = GetDecimalProperty(extractedData, "BalanceDue");

            if (extractedData.TryGetProperty("Discount", out var discountElement) &&
                discountElement.ValueKind == JsonValueKind.Object)
            {
                invoice.DiscountPercentage = GetDecimalProperty(discountElement, "Percentage");
                invoice.DiscountAmount = GetDecimalProperty(discountElement, "Amount");
            }
            else
            {
                invoice.DiscountPercentage = null;
                invoice.DiscountAmount = null;
            }

            // Store full extracted JSON
            invoice.ExtractedDataJson = JsonDocument.Parse(resultJson);


            if (!extractedData.TryGetProperty("LineItems", out var lineItemsElement) ||
                lineItemsElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("LineItems array is required");

            var lineItemsArray = lineItemsElement.EnumerateArray().ToList();
            if (lineItemsArray.Count == 0)
                throw new InvalidOperationException("Invoice must have at least one line item");

            var processedProducts = new HashSet<Guid>();

            foreach (var lineElement in lineItemsArray)
            {
                var productId = GetStringProperty(lineElement, "ProductId");
                if (string.IsNullOrWhiteSpace(productId))
                {
                    logger.LogWarning("Skipping line item with missing ProductId");
                    continue;
                }

                var productName = GetStringProperty(lineElement, "ProductName") ?? "Unknown";
                var category = GetStringProperty(lineElement, "Category");
                var quantity = GetDecimalProperty(lineElement, "Quantity") ?? 0;
                var unitRate = GetDecimalProperty(lineElement, "UnitRate") ?? 0;
                var amount = GetDecimalProperty(lineElement, "Amount") ?? 0;

                if (quantity <= 0)
                {
                    logger.LogWarning("Skipping line item {ProductId} with invalid quantity {Quantity}",
                        productId, quantity);
                    continue;
                }

                var product = await FindOrCreateProductNoSaveAsync(productId, productName, category, unitRate);

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

                product.TotalQuantitySold += quantity;
                product.TotalRevenue += amount;

                if (!processedProducts.Contains(product.Id))
                {
                    product.InvoiceCount++;
                    processedProducts.Add(product.Id);
                }

                if (invoice.InvoiceDate.HasValue)
                {
                    if (!product.LastSoldDate.HasValue ||
                        invoice.InvoiceDate.Value > product.LastSoldDate.Value)
                    {
                        product.LastSoldDate = invoice.InvoiceDate.Value;
                    }
                }

                product.UpdatedAt = DateTime.UtcNow;
            }

            if (invoice.LineItems.Count == 0)
                throw new InvalidOperationException("No valid line items found. All line items were skipped due to missing or invalid data.");

            // Mark invoice as modified if updating
            if (isUpdate)
            {
                context.Entry(invoice).State = EntityState.Modified;
            }

            await context.SaveChangesAsync();

            logger.LogInformation("Invoice {InvoiceId} ({InvoiceNumber}) {Action} successfully with {LineCount} line items",
                invoice.Id, invoice.InvoiceNumber ?? "N/A", isUpdate ? "updated" : "created", invoice.LineItems.Count);

            return await MapToDtoAsync(invoice);
        }


        public async Task<InvoiceDto?> GetInvoiceByIdAsync(Guid id)
        {
            var invoice = await invoiceRepository.GetByIdAsync(id, includeLineItems: true);
            return invoice != null ? await MapToDtoAsync(invoice) : null;
        }

        public async Task<InvoiceDto?> GetInvoiceByFileIdAsync(string fileId)
        {
            var invoice = await invoiceRepository.GetByFileIdAsync(fileId, includeLineItems: true);
            return invoice != null ? await MapToDtoAsync(invoice) : null;
        }

        private async Task<Product> FindOrCreateProductNoSaveAsync(
            string productId,
            string productName,
            string? category,
            decimal? unitRate)
        {
            var product = await productRepository.GetByProductIdAsync(productId);

            if (product == null)
            {
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
                context.Products.Add(product);

                logger.LogInformation("Created new product {ProductId} - {ProductName} ({Category})",
                    productId, productName, category ?? "N/A");
            }
            else
            {
                if (product.ProductName != productName || product.Category != category)
                {
                    product.ProductName = productName;
                    product.Category = category;

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

        private void ValidateCriticalFields(JsonElement extractedData)
        {
            var errors = new List<string>();

            var invoiceNumber = GetStringProperty(extractedData, "InvoiceNumber");
            if (string.IsNullOrWhiteSpace(invoiceNumber))
                errors.Add("InvoiceNumber is required");

            var totalAmount = GetDecimalProperty(extractedData, "TotalAmount");
            if (!totalAmount.HasValue || totalAmount.Value <= 0)
                errors.Add("TotalAmount is required and must be greater than 0");

            if (!extractedData.TryGetProperty("LineItems", out var lineItems) ||
                lineItems.ValueKind != JsonValueKind.Array)
            {
                errors.Add("LineItems array is required");
            }
            else if (lineItems.GetArrayLength() == 0)
            {
                errors.Add("LineItems array must contain at least one item");
            }

            if (errors.Any())
            {
                var errorMessage = $"Critical validation failed: {string.Join(", ", errors)}";
                logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
        }

        private string? GetStringProperty(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var prop))
                return null;

            if (prop.ValueKind == JsonValueKind.Null)
                return null;

            if (prop.ValueKind == JsonValueKind.String)
                return prop.GetString();

            logger.LogDebug("Property {PropertyName} has unexpected type {ValueKind}, expected String or Null",
                propertyName, prop.ValueKind);
            return null;
        }

        private decimal? GetDecimalProperty(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var prop))
                return null;

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
                    try
                    {
                        return (decimal)prop.GetDouble();
                    }
                    catch
                    {
                        logger.LogWarning("Failed to parse {PropertyName} as decimal (value out of range)", propertyName);
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

            logger.LogDebug("Property {PropertyName} has unexpected type {ValueKind}, expected Number or Null",
                propertyName, prop.ValueKind);
            return null;
        }

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

                logger.LogWarning("Failed to parse {PropertyName} as DateTime: {Value}", propertyName, str);
            }

            return null;
        }
    }
}
