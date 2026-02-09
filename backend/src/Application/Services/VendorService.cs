using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Infrastructure.Repositories;

namespace invoice_v1.src.Application.Services
{
    /// <summary>
    /// Service for vendor management operations.
    /// Handles vendor creation, retrieval, and activity tracking.
    /// </summary>
    public class VendorService : IVendorService
    {
        private readonly IVendorRepository vendorRepository;
        private readonly IInvoiceRepository invoiceRepository;
        private readonly IProductRepository productRepository;
        private readonly ILogger<VendorService> logger;

        public VendorService(
            IVendorRepository vendorRepository,
            IInvoiceRepository invoiceRepository,
            IProductRepository productRepository,
            ILogger<VendorService> logger)
        {
            this.vendorRepository = vendorRepository;
            this.invoiceRepository = invoiceRepository;
            this.productRepository = productRepository;
            this.logger = logger;
        }

        public async Task<Vendor> GetOrCreateVendorAsync(string email, string? displayName = null)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email cannot be empty", nameof(email));
            }

            var vendor = await vendorRepository.GetByEmailAsync(email);

            if (vendor == null)
            {
                var extractedDisplayName = displayName ?? ExtractDisplayNameFromEmail(email);

                vendor = new Vendor
                {
                    Email = email,
                    DisplayName = extractedDisplayName,
                    FirstSeenAt = DateTime.UtcNow,
                    LastActivityAt = DateTime.UtcNow
                };

                await vendorRepository.CreateAsync(vendor);
                logger.LogInformation("Auto-created new vendor: {Email} ({DisplayName})", email, extractedDisplayName);
            }

            return vendor;
        }

        public async Task<VendorDto?> GetVendorByEmailAsync(string email)
        {
            var vendor = await vendorRepository.GetByEmailAsync(email);
            if (vendor == null)
            {
                return null;
            }

            var stats = await GetVendorStatsAsync(email);

            return new VendorDto
            {
                Email = vendor.Email,
                DisplayName = vendor.DisplayName,
                FirstSeenAt = vendor.FirstSeenAt,
                LastActivityAt = vendor.LastActivityAt,
                TotalInvoices = stats.InvoiceCount,
                TotalProducts = stats.ProductCount,
                TotalRevenue = stats.TotalRevenue
            };
        }

        public async Task<List<VendorDto>> GetAllVendorsAsync(int skip, int take)
        {
            var vendors = await vendorRepository.GetAllAsync(skip, take);

            var vendorDtos = new List<VendorDto>();

            foreach (var vendor in vendors)
            {
                var stats = await GetVendorStatsAsync(vendor.Email);

                vendorDtos.Add(new VendorDto
                {
                    Email = vendor.Email,
                    DisplayName = vendor.DisplayName,
                    FirstSeenAt = vendor.FirstSeenAt,
                    LastActivityAt = vendor.LastActivityAt,
                    TotalInvoices = stats.InvoiceCount,
                    TotalProducts = stats.ProductCount,
                    TotalRevenue = stats.TotalRevenue
                });
            }

            return vendorDtos;
        }

        public async Task<int> GetVendorCountAsync()
        {
            return await vendorRepository.GetCountAsync();
        }

        public async Task UpdateVendorActivityAsync(string email)
        {
            var vendor = await vendorRepository.GetByEmailAsync(email);
            if (vendor != null)
            {
                vendor.LastActivityAt = DateTime.UtcNow;
                await vendorRepository.UpdateAsync(vendor);
                logger.LogDebug("Updated activity timestamp for vendor: {Email}", email);
            }
        }

        //Uses correct repository methods
        private async Task<(int InvoiceCount, int ProductCount, decimal TotalRevenue)> GetVendorStatsAsync(string email)
        {
            // Use repository RBAC method
            var invoiceCount = await invoiceRepository.GetCountByVendorEmailAsync(email, isAdmin: true);

            // Use repository method with exact signature
            var productCount = await productRepository.GetProductCountAsync(
                vendorEmail: email,
                category: null,
                search: null,
                isAdmin: true);

            // Get invoices to calculate revenue
            var invoices = await invoiceRepository.GetByVendorEmailAsync(
                vendorEmail: email,
                skip: 0,
                take: int.MaxValue,
                includeLineItems: false,
                isAdmin: true);

            var totalRevenue = invoices
                .Where(i => i.TotalAmount.HasValue)
                .Sum(i => i.TotalAmount ?? 0);

            return (invoiceCount, productCount, totalRevenue);
        }

        private string ExtractDisplayNameFromEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return "Unknown";
            }

            var atIndex = email.IndexOf('@');
            if (atIndex > 0)
            {
                return email.Substring(0, atIndex);
            }

            return email;
        }
    }
}
