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
        // ApplicationDbContext ONLY for transaction management
        private readonly ApplicationDbContext context;
        private readonly IInvoiceRepository invoiceRepository;
        private readonly IProductRepository productRepository;
        private readonly IJobRepository jobRepository;
        private readonly IVendorService vendorService;
        private readonly ILogger<InvoiceService> logger;

        public InvoiceService(
            ApplicationDbContext context, //  ONLY for transactions
            IInvoiceRepository invoiceRepository,
            IProductRepository productRepository,
            IJobRepository jobRepository,
            IVendorService vendorService,
            ILogger<InvoiceService> logger)
        {
            this.context = context;
            this.invoiceRepository = invoiceRepository;
            this.productRepository = productRepository;
            this.jobRepository = jobRepository;
            this.vendorService = vendorService;
            this.logger = logger;
        }

        public async Task<InvoiceDto> CreateOrUpdateInvoiceFromCallbackAsync(Guid jobId, object result)
        {
            var job = await jobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                throw new InvalidOperationException($"Job {jobId} not found");
            }

            string? vendorEmail = null;

            if (!string.IsNullOrWhiteSpace(job.PayloadJson))
            {
                try
                {
                    var payloadObj = JsonSerializer.Deserialize<JsonElement>(job.PayloadJson);
                    if (payloadObj.TryGetProperty("modifiedBy", out var modifiedByElement))
                    {
                        vendorEmail = modifiedByElement.GetString();
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error extracting vendor email from job payload");
                }
            }

            if (string.IsNullOrWhiteSpace(vendorEmail))
            {
                vendorEmail = "unknown@system.local";
                logger.LogWarning("No vendor email found in job {JobId}, using default", jobId);
            }

            await vendorService.GetOrCreateVendorAsync(vendorEmail);

            var extractedData = result as JsonElement? ?? JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(result));

            ValidateCriticalFields(extractedData);

            var driveFileId = GetStringProperty(extractedData, "DriveFileId") ?? GetStringProperty(extractedData, "fileId");
            if (string.IsNullOrWhiteSpace(driveFileId))
            {
                if (!string.IsNullOrWhiteSpace(job.PayloadJson))
                {
                    try
                    {
                        var payloadObj = JsonSerializer.Deserialize<JsonElement>(job.PayloadJson);
                        driveFileId = payloadObj.TryGetProperty("fileId", out var fid) ? fid.GetString() : null;
                    }
                    catch { }
                }
            }

            if (string.IsNullOrWhiteSpace(driveFileId))
            {
                throw new InvalidOperationException("DriveFileId is required but missing from result");
            }

            var existingInvoice = await invoiceRepository.GetByFileIdAsync(driveFileId, includeLineItems: true);

            if (existingInvoice != null)
            {
                logger.LogInformation("Updating existing invoice {InvoiceId} for file {FileId}", existingInvoice.Id, driveFileId);
                await UpdateInvoiceFromDataAsync(existingInvoice, extractedData, vendorEmail);
                return await MapToDtoAsync(existingInvoice);
            }
            else
            {
                logger.LogInformation("Creating new invoice for file {FileId}, vendor {VendorEmail}", driveFileId, vendorEmail);
                var newInvoice = await CreateInvoiceFromDataAsync(extractedData, driveFileId, vendorEmail);
                return await MapToDtoAsync(newInvoice);
            }
        }

        public async Task<InvoiceDto?> GetInvoiceByIdAsync(Guid id, string userEmail, bool isAdmin = false)
        {
            //  USE REPOSITORY
            var invoice = await invoiceRepository.GetByIdAsync(id, includeLineItems: true);
            if (invoice == null)
            {
                return null;
            }

            if (!isAdmin && invoice.VendorEmail != userEmail)
            {
                throw new UnauthorizedAccessException($"User {userEmail} is not authorized to access invoice {id}");
            }

            return await MapToDtoAsync(invoice);
        }

        public async Task<InvoiceDto?> GetInvoiceByFileIdAsync(string fileId, string userEmail, bool isAdmin = false)
        {
            //  USE REPOSITORY
            var invoice = await invoiceRepository.GetByFileIdAsync(fileId, includeLineItems: true);
            if (invoice == null)
            {
                return null;
            }

            if (!isAdmin && invoice.VendorEmail != userEmail)
            {
                throw new UnauthorizedAccessException($"User {userEmail} is not authorized to access invoice for file {fileId}");
            }

            return await MapToDtoAsync(invoice);
        }

        public async Task<List<InvoiceDto>> GetInvoicesByVendorAsync(string? vendorEmail, int skip, int take, bool isAdmin = false)
        {
            //  USE REPOSITORY: RBAC filtering is now in repository
            var invoices = await invoiceRepository.GetByVendorEmailAsync(
                vendorEmail,
                skip,
                take,
                includeLineItems: true,
                isAdmin);

            var dtos = new List<InvoiceDto>();
            foreach (var invoice in invoices)
            {
                dtos.Add(await MapToDtoAsync(invoice));
            }

            return dtos;
        }

        public async Task<int> GetInvoiceCountByVendorAsync(string? vendorEmail, bool isAdmin = false)
        {
            //  USE REPOSITORY
            return await invoiceRepository.GetCountByVendorEmailAsync(vendorEmail, isAdmin);
        }

        private async Task<Invoice> CreateInvoiceFromDataAsync(JsonElement extractedData, string driveFileId, string vendorEmail)
        {
            //  DbContext used ONLY for transaction coordination
            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    var invoice = new Invoice
                    {
                        Id = Guid.NewGuid(),
                        VendorEmail = vendorEmail,
                        DriveFileId = driveFileId,
                        OriginalFileName = GetStringProperty(extractedData, "OriginalFileName"),
                        InvoiceNumber = GetStringProperty(extractedData, "InvoiceNumber"),
                        InvoiceDate = GetDateTimeProperty(extractedData, "InvoiceDate"),
                        OrderId = GetStringProperty(extractedData, "OrderId"),
                        VendorName = GetStringProperty(extractedData, "VendorName"),
                        BillToName = GetStringProperty(extractedData, "BillToName"),
                        ShipToCity = GetStringProperty(extractedData, "ShipToCity"),
                        ShipToState = GetStringProperty(extractedData, "ShipToState"),
                        ShipToCountry = GetStringProperty(extractedData, "ShipToCountry"),
                        ShipMode = GetStringProperty(extractedData, "ShipMode"),
                        Subtotal = GetDecimalProperty(extractedData, "Subtotal"),
                        DiscountPercentage = GetDecimalProperty(extractedData, "DiscountPercentage"),
                        DiscountAmount = GetDecimalProperty(extractedData, "DiscountAmount"),
                        ShippingCost = GetDecimalProperty(extractedData, "ShippingCost"),
                        TotalAmount = GetDecimalProperty(extractedData, "TotalAmount"),
                        BalanceDue = GetDecimalProperty(extractedData, "BalanceDue"),
                        Currency = GetStringProperty(extractedData, "Currency"),
                        Notes = GetStringProperty(extractedData, "Notes"),
                        Terms = GetStringProperty(extractedData, "Terms"),
                        ExtractedDataJson = JsonSerializer.Serialize(extractedData),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    context.Invoices.Add(invoice);

                    if (extractedData.TryGetProperty("LineItems", out var lineItemsElement) && lineItemsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var lineItem in lineItemsElement.EnumerateArray())
                        {
                            var productId = GetStringProperty(lineItem, "ProductId");
                            var productName = GetStringProperty(lineItem, "ProductName") ?? "Unknown";
                            var category = GetStringProperty(lineItem, "Category");
                            var quantity = GetDecimalProperty(lineItem, "Quantity");
                            var unitRate = GetDecimalProperty(lineItem, "UnitRate");
                            var amount = GetDecimalProperty(lineItem, "Amount");

                            if (string.IsNullOrWhiteSpace(productId))
                            {
                                logger.LogWarning("Skipping line item with missing ProductId");
                                continue;
                            }

                            var product = await GetOrCreateProductAsync(vendorEmail, productId, productName, category, unitRate);

                            var invoiceLine = new InvoiceLine
                            {
                                Id = Guid.NewGuid(),
                                InvoiceId = invoice.Id,
                                ProductGuid = product.Id,
                                ProductId = productId,
                                ProductName = productName,
                                Category = category,
                                Quantity = quantity ?? 0,
                                UnitRate = unitRate ?? 0,
                                Amount = amount ?? 0,
                                CreatedAt = DateTime.UtcNow
                            };

                            context.InvoiceLines.Add(invoiceLine);

                            product.TotalQuantitySold += invoiceLine.Quantity ?? 0;
                            product.TotalRevenue += invoiceLine.Amount ?? 0;
                            product.InvoiceCount++;
                            if (invoice.InvoiceDate.HasValue && (!product.LastSoldDate.HasValue || invoice.InvoiceDate > product.LastSoldDate))
                            {
                                product.LastSoldDate = invoice.InvoiceDate;
                            }
                            product.UpdatedAt = DateTime.UtcNow;
                        }
                    }

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    await vendorService.UpdateVendorActivityAsync(vendorEmail);

                    logger.LogInformation("Created invoice {InvoiceId} with {LineCount} line items for vendor {VendorEmail}",
                        invoice.Id, invoice.LineItems.Count, vendorEmail);

                    return invoice;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    logger.LogError(ex, "Error creating invoice from extracted data");
                    throw;
                }
            });
        }

        private async Task UpdateInvoiceFromDataAsync(Invoice invoice, JsonElement extractedData, string vendorEmail)
        {
            invoice.InvoiceNumber = GetStringProperty(extractedData, "InvoiceNumber") ?? invoice.InvoiceNumber;
            invoice.InvoiceDate = GetDateTimeProperty(extractedData, "InvoiceDate") ?? invoice.InvoiceDate;
            invoice.OrderId = GetStringProperty(extractedData, "OrderId") ?? invoice.OrderId;
            invoice.VendorName = GetStringProperty(extractedData, "VendorName") ?? invoice.VendorName;
            invoice.TotalAmount = GetDecimalProperty(extractedData, "TotalAmount") ?? invoice.TotalAmount;
            invoice.ExtractedDataJson = JsonSerializer.Serialize(extractedData);
            invoice.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            await vendorService.UpdateVendorActivityAsync(vendorEmail);

            logger.LogInformation("Updated invoice {InvoiceId}", invoice.Id);
        }

        private async Task<Product> GetOrCreateProductAsync(string vendorEmail, string productId, string productName, string? category, decimal? unitRate)
        {
            //  USE REPOSITORY
            var product = await productRepository.GetByVendorAndProductIdAsync(vendorEmail, productId);

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
                    VendorEmail = vendorEmail,
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
                logger.LogInformation("Created new product {ProductId} for vendor {VendorEmail}", productId, vendorEmail);
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
            if (!string.IsNullOrWhiteSpace(invoice.ExtractedDataJson))
            {
                try
                {
                    extractedData = JsonSerializer.Deserialize<object>(invoice.ExtractedDataJson);
                }
                catch
                {
                    extractedData = invoice.ExtractedDataJson;
                }
            }

            return new InvoiceDto
            {
                Id = invoice.Id,
                VendorEmail = invoice.VendorEmail,
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
            {
                errors.Add("InvoiceNumber is required");
            }

            var totalAmount = GetDecimalProperty(extractedData, "TotalAmount");
            if (!totalAmount.HasValue || totalAmount.Value <= 0)
            {
                errors.Add("TotalAmount is required and must be greater than 0");
            }

            if (!extractedData.TryGetProperty("LineItems", out var lineItems) || lineItems.ValueKind != JsonValueKind.Array)
            {
                errors.Add("LineItems array is required");
            }
            else if (lineItems.GetArrayLength() == 0)
            {
                errors.Add("LineItems array must contain at least one item");
            }

            if (errors.Any())
            {
                var errorMessage = "Critical validation failed: " + string.Join(", ", errors);
                logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
        }

        private string? GetStringProperty(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var prop)) return null;
            if (prop.ValueKind == JsonValueKind.Null) return null;
            if (prop.ValueKind == JsonValueKind.String) return prop.GetString();
            return null;
        }

        private decimal? GetDecimalProperty(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var prop)) return null;
            if (prop.ValueKind == JsonValueKind.Null) return null;
            if (prop.ValueKind == JsonValueKind.Number)
            {
                try { return prop.GetDecimal(); }
                catch { return (decimal?)prop.GetDouble(); }
            }
            if (prop.ValueKind == JsonValueKind.String)
            {
                var str = prop.GetString();
                if (!string.IsNullOrWhiteSpace(str) && decimal.TryParse(str, out var value))
                {
                    return value;
                }
            }
            return null;
        }

        private DateTime? GetDateTimeProperty(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var prop)) return null;
            if (prop.ValueKind == JsonValueKind.Null) return null;
            if (prop.ValueKind == JsonValueKind.String)
            {
                var str = prop.GetString();
                if (!string.IsNullOrWhiteSpace(str) && DateTime.TryParse(str, out var value))
                {
                    return value;
                }
            }
            return null;
        }
    }
}
