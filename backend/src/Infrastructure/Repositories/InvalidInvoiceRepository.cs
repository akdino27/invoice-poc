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

        public async Task<InvalidInvoice> CreateAsync(InvalidInvoice invalidInvoice)
        {
            context.InvalidInvoices.Add(invalidInvoice);
            await context.SaveChangesAsync();
            return invalidInvoice;
        }

        public async Task<InvalidInvoice?> GetByIdAsync(Guid id)
        {
            return await context.InvalidInvoices.FindAsync(id);
        }

        public async Task<List<InvalidInvoice>> GetAllAsync(int skip, int take)
        {
            return await context.InvalidInvoices
                .OrderByDescending(i => i.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetCountAsync()
        {
            return await context.InvalidInvoices.CountAsync();
        }
    }
}
