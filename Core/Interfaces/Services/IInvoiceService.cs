using QuoteManager.Core.Common;
using QuoteManager.ViewModels;

namespace QuoteManager.Core.Interfaces.Services
{
    public interface IInvoiceService
    {
        Task<Result<InvoiceDetailsViewModel>> GetInvoiceByIdAsync(int invoiceId, string userId, CancellationToken cancellationToken = default);
        Task<Result<(List<InvoiceDetailsViewModel> Invoices, int TotalCount)>> GetInvoicesAsync(
            string userId,
            string userRole,
            int pageNumber = 1,
            int pageSize = 20,
            string? statusFilter = null,
            CancellationToken cancellationToken = default);
        
        Task<Result<int>> CreateInvoiceAsync(CreateInvoiceViewModel model, string userId, CancellationToken cancellationToken = default);
        Task<Result> UpdateInvoiceStatusAsync(int invoiceId, string status, string userId, CancellationToken cancellationToken = default);
        Task<string> GenerateInvoiceNumberAsync(CancellationToken cancellationToken = default);
    }

    public class InvoiceDetailsViewModel
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal GrandTotal { get; set; }
    }
}
