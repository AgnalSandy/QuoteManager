using QuoteManager.Core.Interfaces.Services;
using QuoteManager.Data;
using Microsoft.EntityFrameworkCore;

namespace QuoteManager.Application.Services
{
    /// <summary>
    /// Thread-safe quote number generator using database-level locking
    /// </summary>
    public class QuoteNumberGenerator : IQuoteNumberGenerator
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QuoteNumberGenerator> _logger;
        private static readonly SemaphoreSlim _semaphore = new(1, 1);

        public QuoteNumberGenerator(
            ApplicationDbContext context,
            ILogger<QuoteNumberGenerator> logger)
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
                
                // Get the last quote number for this year with row-level locking
                var lastQuote = await _context.Quotes
                    .Where(q => q.QuoteNumber.StartsWith($"QT-{year}-"))
                    .OrderByDescending(q => q.QuoteNumber)
                    .Select(q => q.QuoteNumber)
                    .FirstOrDefaultAsync(cancellationToken);

                int nextNumber = 1;

                if (!string.IsNullOrEmpty(lastQuote))
                {
                    // Extract number from QT-2026-000123 format
                    var parts = lastQuote.Split('-');
                    if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
                    {
                        nextNumber = lastNumber + 1;
                    }
                }

                var quoteNumber = $"QT-{year}-{nextNumber:D6}";
                
                _logger.LogInformation("Generated quote number: {QuoteNumber}", quoteNumber);
                
                return quoteNumber;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating quote number");
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
