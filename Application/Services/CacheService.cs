using Microsoft.Extensions.Caching.Memory;
using QuoteManager.Core.Interfaces;
using QuoteManager.Core.Interfaces.Services;
using QuoteManager.Models;

namespace QuoteManager.Application.Services
{
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CacheService> _logger;
        
        private const string SERVICE_MASTERS_KEY = "ServiceMasters_Active";
        private const string TAX_MASTERS_KEY = "TaxMasters_Active";
        private const string COMPANY_SETTINGS_KEY = "CompanySettings";
        private const string MASTER_DATA_PREFIX = "MasterData_";

        public CacheService(
            IMemoryCache cache, 
            IUnitOfWork unitOfWork,
            ILogger<CacheService> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            _cache.TryGetValue(key, out T? value);
            return Task.FromResult(value);
        }

        public Task SetAsync<T>(
            string key, 
            T value, 
            TimeSpan? absoluteExpiration = null, 
            CancellationToken cancellationToken = default) where T : class
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpiration ?? TimeSpan.FromHours(1)
            };
            
            _cache.Set(key, value, options);
            _logger.LogDebug("Cached item with key: {Key}", key);
            
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
            _logger.LogDebug("Removed cache item with key: {Key}", key);
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class
        {
            if (_cache.TryGetValue(key, out T? cachedValue) && cachedValue != null)
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return cachedValue;
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);
            var value = await factory();
            
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromHours(1)
            };
            
            _cache.Set(key, value, options);
            return value;
        }

        public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            // Note: IMemoryCache doesn't support prefix removal natively
            // For production, consider using Redis or implementing a key tracking mechanism
            _logger.LogWarning("RemoveByPrefix called but not fully implemented for IMemoryCache: {Prefix}", prefix);
            return Task.CompletedTask;
        }

        public async Task<List<ServiceMaster>> GetActiveServicesAsync(CancellationToken cancellationToken = default)
        {
            return await _cache.GetOrCreateAsync(SERVICE_MASTERS_KEY, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2);
                
                var services = await _unitOfWork.ServiceMasters.FindAsync(
                    s => s.IsActive, 
                    cancellationToken);
                
                _logger.LogInformation("Loaded {Count} active services into cache", services.Count());
                return services.ToList();
            }) ?? new List<ServiceMaster>();
        }

        public async Task<List<TaxMaster>> GetActiveTaxesAsync(CancellationToken cancellationToken = default)
        {
            return await _cache.GetOrCreateAsync(TAX_MASTERS_KEY, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2);
                
                var taxes = await _unitOfWork.TaxMasters.FindAsync(
                    t => t.IsActive, 
                    cancellationToken);
                
                _logger.LogInformation("Loaded {Count} active taxes into cache", taxes.Count());
                return taxes.ToList();
            }) ?? new List<TaxMaster>();
        }

        public async Task<CompanySettings?> GetCompanySettingsAsync(CancellationToken cancellationToken = default)
        {
            return await _cache.GetOrCreateAsync(COMPANY_SETTINGS_KEY, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
                
                var settings = (await _unitOfWork.CompanySettings.GetAllAsync(cancellationToken)).FirstOrDefault();
                
                _logger.LogInformation("Loaded company settings into cache");
                return settings;
            });
        }

        public async Task InvalidateMasterDataCacheAsync(CancellationToken cancellationToken = default)
        {
            await RemoveAsync(SERVICE_MASTERS_KEY, cancellationToken);
            await RemoveAsync(TAX_MASTERS_KEY, cancellationToken);
            await RemoveAsync(COMPANY_SETTINGS_KEY, cancellationToken);
            
            _logger.LogInformation("Invalidated all master data cache");
        }
    }
}
