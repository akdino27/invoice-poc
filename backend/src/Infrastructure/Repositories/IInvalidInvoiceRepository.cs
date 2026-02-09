using invoice_v1.src.Domain.Entities;

namespace invoice_v1.src.Infrastructure.Repositories
{
    public interface IInvalidInvoiceRepository
    {
        Task CreateAsync(InvalidInvoice invalidInvoice);

        Task<List<InvalidInvoice>> GetAllAsync(
            int skip,
            int take,
            string? userEmail = null,
            bool isAdmin = false);

        Task<int> GetCountAsync(string? userEmail = null, bool isAdmin = false);
    }
}
