using Microsoft.EntityFrameworkCore;
using QuoteManager.Core.Interfaces.Services;
using QuoteManager.Data;
using QuoteManager.Models;

namespace QuoteManager.Application.Services
{
    /// <summary>
    /// Master data service with caching to reduce database calls for lookup tables
    /// </summary>
    public class MasterDataService : IMasterDataService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly ILogger<MasterDataService> _logger;

        private const string ServicesKey = "master_data_services";
        private const string TaxesKey = "master_data_taxes";
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(1);

        public MasterDataService(
            ApplicationDbContext context,
            ICacheService cacheService,
            ILogger<MasterDataService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<ServiceMaster>> GetActiveServicesAsync(CancellationToken cancellationToken = default)
        {
            return await _cacheService.GetOrSetAsync(
                ServicesKey,
                async () =>
                {
                    _logger.LogInformation("Loading active services from database");
                    return await _context.ServiceMasters
                        .Where(s => s.IsActive)
                        .OrderBy(s => s.ServiceName)
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);
                },
                CacheExpiration
            );
        }

        public async Task<List<TaxMaster>> GetActiveTaxesAsync(CancellationToken cancellationToken = default)
        {
            return await _cacheService.GetOrSetAsync(
                TaxesKey,
                async () =>
                {
                    _logger.LogInformation("Loading active taxes from database");
                    return await _context.TaxMasters
                        .Where(t => t.IsActive)
                        .OrderBy(t => t.TaxName)
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);
                },
                CacheExpiration
            );
        }

        public async Task<ServiceMaster?> GetServiceByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var services = await GetActiveServicesAsync(cancellationToken);
            return services.FirstOrDefault(s => s.Id == id);
        }

        public async Task<TaxMaster?> GetTaxByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var taxes = await GetActiveTaxesAsync(cancellationToken);
            return taxes.FirstOrDefault(t => t.Id == id);
        }

        public void InvalidateCache()
        {
            _logger.LogInformation("Invalidating master data cache");
            _cacheService.Remove(ServicesKey);
            _cacheService.Remove(TaxesKey);
        }
    }
}
