using QuoteManager.Models;

namespace QuoteManager.Core.Interfaces.Services
{
    /// <summary>
    /// Service for accessing master data (ServiceMasters, TaxMasters) with caching
    /// </summary>
    public interface IMasterDataService
    {
        /// <summary>
        /// Gets all active services (cached for 1 hour)
        /// </summary>
        Task<List<ServiceMaster>> GetActiveServicesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all active taxes (cached for 1 hour)
        /// </summary>
        Task<List<TaxMaster>> GetActiveTaxesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a specific service by ID
        /// </summary>
        Task<ServiceMaster?> GetServiceByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a specific tax by ID
        /// </summary>
        Task<TaxMaster?> GetTaxByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates the cache (call after creating/updating/deleting master data)
        /// </summary>
        void InvalidateCache();
    }
}
