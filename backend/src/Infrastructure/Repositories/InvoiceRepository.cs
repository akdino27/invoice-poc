using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly ApplicationDbContext context;
        private readonly ILogger<InvoiceRepository> logger;

        public InvoiceRepository(
            ApplicationDbContext context,
            ILogger<InvoiceRepository> logger)
        {
            this.context = context;
            this.logger = logger;
        }

        public async Task<Invoice?> GetByIdAsync(Guid id, bool includeLineItems = false)
        {
            var query = context.Invoices.AsNoTracking();

            if (includeLineItems)
            {
                query = query
                    .Include(i => i.LineItems)
                    .ThenInclude(l => l.Product);
            }

            return await query.FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Invoice?> GetByFileIdAsync(string fileId, bool includeLineItems = false)
        {
            var query = context.Invoices.AsNoTracking();

            if (includeLineItems)
            {
                query = query
                    .Include(i => i.LineItems)
                    .ThenInclude(l => l.Product);
            }

            return await query.FirstOrDefaultAsync(i => i.DriveFileId == fileId);
        }

        public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber, bool includeLineItems = false)
        {
            var query = context.Invoices.AsNoTracking();

            if (includeLineItems)
            {
                query = query
                    .Include(i => i.LineItems)
                    .ThenInclude(l => l.Product);
            }

            return await query.FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber);
        }

        public async Task<List<Invoice>> GetAllAsync(int skip, int take, bool includeLineItems = false)
        {
            var query = context.Invoices.AsNoTracking();

            if (includeLineItems)
            {
                query = query
                    .Include(i => i.LineItems)
                    .ThenInclude(l => l.Product);
            }

            return await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<Invoice>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, bool includeLineItems = false)
        {
            var query = context.Invoices.AsNoTracking();

            if (includeLineItems)
            {
                query = query
                    .Include(i => i.LineItems)
                    .ThenInclude(l => l.Product);
            }

            return await query
                .Where(i => i.InvoiceDate.HasValue &&
                           i.InvoiceDate >= startDate &&
                           i.InvoiceDate <= endDate)
                .OrderBy(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task<List<Invoice>> GetByVendorAsync(string vendorName, int skip, int take)
        {
            return await context.Invoices
                .AsNoTracking()
                .Where(i => i.VendorName != null && i.VendorName.Contains(vendorName))
                .OrderByDescending(i => i.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetCountAsync()
        {
            return await context.Invoices.CountAsync();
        }

        public async Task<Invoice> CreateAsync(Invoice invoice)
        {
            context.Invoices.Add(invoice);
            await context.SaveChangesAsync();
            logger.LogInformation("Created invoice {Id} with {LineItemCount} line items",
                invoice.Id, invoice.LineItems.Count);
            return invoice;
        }

        public async Task UpdateAsync(Invoice invoice)
        {
            invoice.UpdatedAt = DateTime.UtcNow;
            context.Invoices.Update(invoice);
            await context.SaveChangesAsync();
            logger.LogInformation("Updated invoice {Id}", invoice.Id);
        }

        public async Task DeleteAsync(Guid id)
        {
            var invoice = await context.Invoices.FindAsync(id);
            if (invoice != null)
            {
                context.Invoices.Remove(invoice);
                await context.SaveChangesAsync();
                logger.LogInformation("Deleted invoice {Id}", id);
            }
        }
    }
}
