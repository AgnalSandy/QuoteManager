using QuoteManager.Models;

namespace QuoteManager.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<Quote> Quotes { get; }
        IRepository<QuoteItem> QuoteItems { get; }
        IRepository<QuoteItemTax> QuoteItemTaxes { get; }
        IRepository<Invoice> Invoices { get; }
        IRepository<ServiceMaster> ServiceMasters { get; }
        IRepository<TaxMaster> TaxMasters { get; }
        IRepository<CompanySettings> CompanySettings { get; }
        IRepository<ApplicationUser> Users { get; }
        
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}
