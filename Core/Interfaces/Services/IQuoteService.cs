using QuoteManager.Core.Common;
using QuoteManager.ViewModels;

namespace QuoteManager.Core.Interfaces.Services
{
    public interface IQuoteService
    {
        Task<Result<QuoteDetailsViewModel>> GetQuoteByIdAsync(int quoteId, string userId, CancellationToken cancellationToken = default);
        Task<Result<(List<QuoteDetailsViewModel> Quotes, int TotalCount)>> GetQuotesAsync(
            string userId, 
            string userRole,
            int pageNumber = 1, 
            int pageSize = 20,
            string? statusFilter = null,
            string? searchTerm = null,
            CancellationToken cancellationToken = default);
        
        Task<Result<int>> CreateQuoteAsync(CreateQuoteViewModel model, string userId, CancellationToken cancellationToken = default);
        Task<Result> UpdateQuoteAsync(int quoteId, EditQuoteViewModel model, string userId, CancellationToken cancellationToken = default);
        Task<Result> DeleteQuoteAsync(int quoteId, string userId, CancellationToken cancellationToken = default);
        Task<Result> UpdateQuoteStatusAsync(int quoteId, string status, string userId, CancellationToken cancellationToken = default);
        
        Task<bool> CanUserAccessQuoteAsync(int quoteId, string userId, string userRole, CancellationToken cancellationToken = default);
        Task<string> GenerateQuoteNumberAsync(CancellationToken cancellationToken = default);
    }
}
