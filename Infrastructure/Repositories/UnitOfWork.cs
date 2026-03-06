using Microsoft.EntityFrameworkCore.Storage;
using QuoteManager.Core.Interfaces;
using QuoteManager.Data;
using QuoteManager.Models;

namespace QuoteManager.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;
        
        private IRepository<Quote>? _quotes;
        private IRepository<QuoteItem>? _quoteItems;
        private IRepository<QuoteItemTax>? _quoteItemTaxes;
        private IRepository<Invoice>? _invoices;
        private IRepository<ServiceMaster>? _serviceMasters;
        private IRepository<TaxMaster>? _taxMasters;
        private IRepository<CompanySettings>? _companySettings;
        private IRepository<ApplicationUser>? _users;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IRepository<Quote> Quotes => _quotes ??= new Repository<Quote>(_context);
        public IRepository<QuoteItem> QuoteItems => _quoteItems ??= new Repository<QuoteItem>(_context);
        public IRepository<QuoteItemTax> QuoteItemTaxes => _quoteItemTaxes ??= new Repository<QuoteItemTax>(_context);
        public IRepository<Invoice> Invoices => _invoices ??= new Repository<Invoice>(_context);
        public IRepository<ServiceMaster> ServiceMasters => _serviceMasters ??= new Repository<ServiceMaster>(_context);
        public IRepository<TaxMaster> TaxMasters => _taxMasters ??= new Repository<TaxMaster>(_context);
        public IRepository<CompanySettings> CompanySettings => _companySettings ??= new Repository<CompanySettings>(_context);
        public IRepository<ApplicationUser> Users => _users ??= new Repository<ApplicationUser>(_context);

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                if (_transaction != null)
                {
                    await _transaction.CommitAsync(cancellationToken);
                }
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync(cancellationToken);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context?.Dispose();
        }
    }
}
