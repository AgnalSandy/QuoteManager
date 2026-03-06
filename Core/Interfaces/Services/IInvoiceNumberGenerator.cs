namespace QuoteManager.Core.Interfaces.Services
{
    /// <summary>
    /// Service for generating unique, thread-safe invoice numbers
    /// </summary>
    public interface IInvoiceNumberGenerator
    {
        /// <summary>
        /// Generates a unique invoice number in format: INV-YYYY-NNNNNN
        /// </summary>
        Task<string> GenerateAsync(CancellationToken cancellationToken = default);
    }
}
