using QuoteManager.Models;

namespace QuoteManager.Core.Interfaces.Services
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default) where T : class;
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
        void Remove(string key);
        Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets value from cache or executes factory and caches the result
        /// </summary>
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class;
        
        // Convenience methods for common cached items
        Task<List<ServiceMaster>> GetActiveServicesAsync(CancellationToken cancellationToken = default);
        Task<List<TaxMaster>> GetActiveTaxesAsync(CancellationToken cancellationToken = default);
        Task<CompanySettings?> GetCompanySettingsAsync(CancellationToken cancellationToken = default);
        Task InvalidateMasterDataCacheAsync(CancellationToken cancellationToken = default);
    }
}
