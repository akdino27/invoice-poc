using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public class InvalidInvoiceRepository : IInvalidInvoiceRepository
    {
        private readonly ApplicationDbContext context;

        public InvalidInvoiceRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task CreateAsync(InvalidInvoice invalidInvoice)
        {
            context.InvalidInvoices.Add(invalidInvoice);
            await context.SaveChangesAsync();
        }

        public async Task<List<InvalidInvoice>> GetAllAsync(
            int skip,
            int take,
            string? userEmail = null,
            bool isAdmin = false)
        {
            IQueryable<InvalidInvoice> query = context.InvalidInvoices;

            // RBAC filtering
            if (!isAdmin && !string.IsNullOrWhiteSpace(userEmail))
            {
                query = query.Where(i => i.VendorEmail == userEmail);
            }

            return await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetCountAsync(string? userEmail = null, bool isAdmin = false)
        {
            IQueryable<InvalidInvoice> query = context.InvalidInvoices;

            // RBAC filtering
            if (!isAdmin && !string.IsNullOrWhiteSpace(userEmail))
            {
                query = query.Where(i => i.VendorEmail == userEmail);
            }

            return await query.CountAsync();
        }
    }
}
