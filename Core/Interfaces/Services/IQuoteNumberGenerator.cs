namespace QuoteManager.Core.Interfaces.Services
{
    /// <summary>
    /// Service for generating unique, thread-safe quote numbers
    /// </summary>
    public interface IQuoteNumberGenerator
    {
        /// <summary>
        /// Generates a unique quote number in format: QT-YYYY-NNNNNN
        /// </summary>
        Task<string> GenerateAsync(CancellationToken cancellationToken = default);
    }
}
