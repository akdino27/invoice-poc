namespace invoice_v1.src.Application.Interfaces
{
    public interface IInvalidInvoiceService
    {
        Task<object> GetInvalidInvoicesAsync(int page, int pageSize, Guid? vendorId);
    }
}
