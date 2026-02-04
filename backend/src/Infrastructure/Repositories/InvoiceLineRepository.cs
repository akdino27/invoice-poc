using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public class InvoiceLineRepository : IInvoiceLineRepository
    {
        private readonly ApplicationDbContext context;
        private readonly ILogger<InvoiceLineRepository> logger;

        public InvoiceLineRepository(
            ApplicationDbContext context,
            ILogger<InvoiceLineRepository> logger)
        {
            this.context = context;
            this.logger = logger;
        }

        public async Task<InvoiceLine?> GetByIdAsync(Guid id)
        {
            return await context.InvoiceLines
                .AsNoTracking()
                .Include(l => l.Product)
                .Include(l => l.Invoice)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<List<InvoiceLine>> GetByInvoiceIdAsync(Guid invoiceId)
        {
            return await context.InvoiceLines
                .AsNoTracking()
                .Include(l => l.Product)
                .Where(l => l.InvoiceId == invoiceId)
                .ToListAsync();
        }

        public async Task<List<InvoiceLine>> GetByProductIdAsync(string productId, int skip, int take)
        {
            return await context.InvoiceLines
                .AsNoTracking()
                .Include(l => l.Invoice)
                .Where(l => l.ProductId == productId)
                .OrderByDescending(l => l.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<InvoiceLine>> GetByCategoryAsync(string category, int skip, int take)
        {
            return await context.InvoiceLines
                .AsNoTracking()
                .Include(l => l.Product)
                .Include(l => l.Invoice)
                .Where(l => l.Category != null && l.Category.Contains(category))
                .OrderByDescending(l => l.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<InvoiceLine> CreateAsync(InvoiceLine invoiceLine)
        {
            context.InvoiceLines.Add(invoiceLine);
            await context.SaveChangesAsync();
            logger.LogDebug("Created invoice line {Id} for invoice {InvoiceId}",
                invoiceLine.Id, invoiceLine.InvoiceId);
            return invoiceLine;
        }

        public async Task UpdateAsync(InvoiceLine invoiceLine)
        {
            context.InvoiceLines.Update(invoiceLine);
            await context.SaveChangesAsync();
            logger.LogDebug("Updated invoice line {Id}", invoiceLine.Id);
        }

        public async Task DeleteAsync(Guid id)
        {
            var invoiceLine = await context.InvoiceLines.FindAsync(id);
            if (invoiceLine != null)
            {
                context.InvoiceLines.Remove(invoiceLine);
                await context.SaveChangesAsync();
                logger.LogDebug("Deleted invoice line {Id}", id);
            }
        }

        public async Task DeleteByInvoiceIdAsync(Guid invoiceId)
        {
            var invoiceLines = await context.InvoiceLines
                .Where(l => l.InvoiceId == invoiceId)
                .ToListAsync();

            if (invoiceLines.Any())
            {
                context.InvoiceLines.RemoveRange(invoiceLines);
                await context.SaveChangesAsync();
                logger.LogDebug("Deleted {Count} invoice lines for invoice {InvoiceId}",
                    invoiceLines.Count, invoiceId);
            }
        }
    }
}
