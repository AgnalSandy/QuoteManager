using QuoteManager.Core.Interfaces.Services;
using QuoteManager.Data;
using Microsoft.EntityFrameworkCore;

namespace QuoteManager.Application.Services
{
    /// <summary>
    /// Thread-safe invoice number generator using database-level locking
    /// </summary>
    public class InvoiceNumberGenerator : IInvoiceNumberGenerator
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InvoiceNumberGenerator> _logger;
        private static readonly SemaphoreSlim _semaphore = new(1, 1);

        public InvoiceNumberGenerator(
            ApplicationDbContext context,
            ILogger<InvoiceNumberGenerator> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
        {
            // Use semaphore to prevent race conditions
            await _semaphore.WaitAsync(cancellationToken);
            
            try
            {
                var year = DateTime.UtcNow.Year;
                
                // Get the last invoice number for this year with row-level locking
                var lastInvoice = await _context.Invoices
                    .Where(i => i.InvoiceNumber.StartsWith($"INV-{year}-"))
                    .OrderByDescending(i => i.InvoiceNumber)
                    .Select(i => i.InvoiceNumber)
                    .FirstOrDefaultAsync(cancellationToken);

                int nextNumber = 1;

                if (!string.IsNullOrEmpty(lastInvoice))
                {
                    // Extract number from INV-2026-000123 format
                    var parts = lastInvoice.Split('-');
                    if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
                    {
                        nextNumber = lastNumber + 1;
                    }
                }

                var invoiceNumber = $"INV-{year}-{nextNumber:D6}";
                
                _logger.LogInformation("Generated invoice number: {InvoiceNumber}", invoiceNumber);
                
                return invoiceNumber;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice number");
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
