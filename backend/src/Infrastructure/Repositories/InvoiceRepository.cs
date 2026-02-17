using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly ApplicationDbContext _context;

        public InvoiceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Invoice?> GetByIdAsync(Guid id, bool includeLineItems = true)
        {
            var query = _context.Invoices.AsQueryable();

            if (includeLineItems)
            {
                query = query
                    .Include(i => i.LineItems)
                        .ThenInclude(l => l.Product);
            }

            return await query
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Invoice?> GetByFileIdAsync(string fileId, bool includeLineItems = true)
        {
            var query = _context.Invoices.AsQueryable();

            if (includeLineItems)
            {
                query = query.Include(i => i.LineItems);
            }

            return await query.FirstOrDefaultAsync(i => i.DriveFileId == fileId);
        }

        public async Task<List<Invoice>> GetInvoicesAsync(Guid? vendorId, int skip, int take)
        {
            var query = _context.Invoices
                .Include(i => i.LineItems)
                    .ThenInclude(l => l.Product)
                .AsNoTracking()
                .AsQueryable();

            if (vendorId.HasValue)
            {
                query = query.Where(i => i.UploadedByVendorId == vendorId.Value);
            }

            return await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetInvoiceCountAsync(Guid? vendorId)
        {
            var query = _context.Invoices.AsQueryable();

            if (vendorId.HasValue)
            {
                query = query.Where(i => i.UploadedByVendorId == vendorId.Value);
            }

            return await query.CountAsync();
        }

        public async Task<Invoice> CreateAsync(Invoice invoice)
        {
            _context.Invoices.Add(invoice);
            return invoice;
        }

        public Task UpdateAsync(Invoice invoice)
        {
            _context.Invoices.Update(invoice);
            return Task.CompletedTask;
        }

        public Task DeleteLineItemsAsync(IEnumerable<InvoiceLine> lineItems)
        {
            _context.InvoiceLines.RemoveRange(lineItems);
            return Task.CompletedTask;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
