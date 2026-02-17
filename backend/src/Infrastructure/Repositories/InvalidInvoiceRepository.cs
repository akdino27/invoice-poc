using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public class InvalidInvoiceRepository : IInvalidInvoiceRepository
    {
        private readonly ApplicationDbContext _context;

        public InvalidInvoiceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(List<InvalidInvoice> Data, int Total)> GetInvalidInvoicesAsync(int page, int pageSize, Guid? vendorId)
        {
            var query = _context.InvalidInvoices.AsQueryable();

            if (vendorId.HasValue)
            {
                query = query.Where(x => x.VendorId == vendorId.Value);
            }

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (data, total);
        }

        public async Task CreateAsync(InvalidInvoice invalidInvoice)
        {
            await _context.InvalidInvoices.AddAsync(invalidInvoice);
        }

        public async Task DeleteByJobIdAsync(Guid jobId)
        {
            var items = await _context.InvalidInvoices.Where(x => x.JobId == jobId).ToListAsync();
            if (items.Any())
            {
                _context.InvalidInvoices.RemoveRange(items);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
