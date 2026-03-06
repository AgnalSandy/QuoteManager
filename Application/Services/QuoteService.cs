using Microsoft.EntityFrameworkCore;
using QuoteManager.Constants;
using QuoteManager.Core.Common;
using QuoteManager.Core.Interfaces;
using QuoteManager.Core.Interfaces.Services;
using QuoteManager.Data;
using QuoteManager.Models;
using QuoteManager.ViewModels;
using System.Globalization;

namespace QuoteManager.Application.Services
{
    public class QuoteService : IQuoteService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly ILogger<QuoteService> _logger;
        private static readonly SemaphoreSlim _quoteNumberLock = new(1, 1);

        public QuoteService(
            IUnitOfWork unitOfWork,
            ApplicationDbContext context,
            ICacheService cacheService,
            ILogger<QuoteService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<QuoteDetailsViewModel>> GetQuoteByIdAsync(
            int quoteId, 
            string userId, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var quote = await _context.Quotes
                    .Include(q => q.Client)
                    .Include(q => q.CreatedBy)
                    .Include(q => q.QuoteItems)
                        .ThenInclude(qi => qi.Service)
                    .Include(q => q.QuoteItems)
                        .ThenInclude(qi => qi.QuoteItemTaxes)
                            .ThenInclude(qit => qit.Tax)
                    .FirstOrDefaultAsync(q => q.Id == quoteId, cancellationToken);

                if (quote == null)
                {
                    return Result.Failure<QuoteDetailsViewModel>("Quote not found");
                }

                var viewModel = new QuoteDetailsViewModel
                {
                    Id = quote.Id,
                    QuoteNumber = quote.QuoteNumber,
                    Title = quote.Title,
                    Description = quote.Description,
                    ClientName = quote.Client?.FullName ?? "Unknown",
                    ClientEmail = quote.Client?.Email ?? "",
                    CreatedByName = quote.CreatedBy?.FullName ?? "Unknown",
                    CreatedDate = quote.CreatedDate,
                    ValidUntil = quote.ValidUntil,
                    Status = quote.Status,
                    SubTotal = quote.SubTotal,
                    TotalTax = quote.TotalTax,
                    GrandTotal = quote.GrandTotal,
                    Items = quote.QuoteItems.Select(qi => new QuoteItemDetailViewModel
                    {
                        ServiceName = qi.CustomServiceName ?? qi.Service?.ServiceName ?? "Unknown Service",
                        CustomDescription = qi.CustomDescription,
                        Quantity = qi.Quantity,
                        UnitPrice = qi.UnitPrice,
                        Amount = qi.Amount,
                        Taxes = qi.QuoteItemTaxes.Select(qit => new TaxDetailViewModel
                        {
                            TaxName = qit.Tax?.TaxName ?? "Unknown Tax",
                            Rate = qit.Tax?.TaxPercentage ?? 0,
                            Amount = qit.TaxAmount
                        }).ToList()
                    }).ToList()
                };

                return Result.Success(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving quote {QuoteId}", quoteId);
                return Result.Failure<QuoteDetailsViewModel>("An error occurred while retrieving the quote");
            }
        }

        public async Task<Result<(List<QuoteDetailsViewModel> Quotes, int TotalCount)>> GetQuotesAsync(
            string userId,
            string userRole,
            int pageNumber = 1,
            int pageSize = 20,
            string? statusFilter = null,
            string? searchTerm = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.Quotes
                    .Include(q => q.Client)
                    .Include(q => q.CreatedBy)
                    .AsQueryable();

                // Apply role-based filtering
                query = ApplyRoleBasedFilter(query, userId, userRole);

                // Apply status filter
                if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
                {
                    query = query.Where(q => q.Status == statusFilter);
                }

                // Apply search filter
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(q =>
                        q.Title.Contains(searchTerm) ||
                        (q.Description != null && q.Description.Contains(searchTerm)) ||
                        q.Client.FullName.Contains(searchTerm) ||
                        q.QuoteNumber.Contains(searchTerm)
                    );
                }

                var totalCount = await query.CountAsync(cancellationToken);

                var quotes = await query
                    .OrderByDescending(q => q.CreatedDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(q => new QuoteDetailsViewModel
                    {
                        Id = q.Id,
                        QuoteNumber = q.QuoteNumber,
                        Title = q.Title,
                        Description = q.Description,
                        ClientName = q.Client.FullName,
                        ClientEmail = q.Client.Email ?? "",
                        CreatedByName = q.CreatedBy != null ? q.CreatedBy.FullName : "Unknown",
                        CreatedDate = q.CreatedDate,
                        ValidUntil = q.ValidUntil,
                        Status = q.Status,
                        SubTotal = q.SubTotal,
                        TotalTax = q.TotalTax,
                        GrandTotal = q.GrandTotal
                    })
                    .ToListAsync(cancellationToken);

                return Result.Success((quotes, totalCount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving quotes for user {UserId}", userId);
                return Result.Failure<(List<QuoteDetailsViewModel>, int)>("An error occurred while retrieving quotes");
            }
        }

        public async Task<Result<int>> CreateQuoteAsync(
            CreateQuoteViewModel model, 
            string userId, 
            CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            
            try
            {
                // Validate items
                if (model.Items == null || !model.Items.Any())
                {
                    return Result.Failure<int>("At least one quote item is required");
                }

                // Load all required data in one go (fix N+1 query issue)
                var serviceIds = model.Items
                    .Where(i => !i.IsCustomService && i.ServiceId.HasValue)
                    .Select(i => i.ServiceId!.Value)
                    .Distinct()
                    .ToList();

                var allTaxIds = model.Items
                    .SelectMany(i => i.SelectedTaxIds ?? new List<int>())
                    .Distinct()
                    .ToList();

                var services = serviceIds.Any() 
                    ? (await _unitOfWork.ServiceMasters.FindAsync(s => serviceIds.Contains(s.Id), cancellationToken)).ToDictionary(s => s.Id)
                    : new Dictionary<int, ServiceMaster>();

                var taxes = allTaxIds.Any() 
                    ? (await _unitOfWork.TaxMasters.FindAsync(t => allTaxIds.Contains(t.Id), cancellationToken)).ToDictionary(t => t.Id)
                    : new Dictionary<int, TaxMaster>();

                // Generate quote number (thread-safe)
                var quoteNumber = await GenerateQuoteNumberAsync(cancellationToken);

                // Calculate totals
                decimal subTotal = 0;
                decimal totalTax = 0;

                // Auto-generate title if not provided
                var quoteTitle = await GenerateQuoteTitleAsync(model, services);

                // Create quote
                var quote = new Quote
                {
                    QuoteNumber = quoteNumber,
                    Title = quoteTitle,
                    Description = model.Description,
                    ClientId = model.ClientId,
                    CreatedById = userId,
                    CreatedDate = DateTime.UtcNow,
                    ValidUntil = model.ValidUntil,
                    Status = QuoteStatus.Pending,
                    SubTotal = 0,
                    TotalTax = 0,
                    GrandTotal = 0
                };

                await _unitOfWork.Quotes.AddAsync(quote, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Create quote items and taxes
                foreach (var itemInput in model.Items)
                {
                    // Validate item
                    if (itemInput.IsCustomService && string.IsNullOrWhiteSpace(itemInput.CustomServiceName))
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result.Failure<int>("Custom service name is required for custom services");
                    }

                    if (!itemInput.IsCustomService && !itemInput.ServiceId.HasValue)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result.Failure<int>("Service selection is required for catalog services");
                    }

                    var quoteItem = new QuoteItem
                    {
                        QuoteId = quote.Id,
                        ServiceId = itemInput.ServiceId,
                        CustomServiceName = itemInput.CustomServiceName,
                        Quantity = itemInput.Quantity,
                        UnitPrice = itemInput.UnitPrice,
                        Amount = itemInput.Quantity * itemInput.UnitPrice,
                        CustomDescription = itemInput.CustomDescription
                    };

                    await _unitOfWork.QuoteItems.AddAsync(quoteItem, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    subTotal += quoteItem.Amount;

                    // Add taxes
                    if (itemInput.SelectedTaxIds != null && itemInput.SelectedTaxIds.Any())
                    {
                        var quoteItemTaxes = new List<QuoteItemTax>();
                        
                        foreach (var taxId in itemInput.SelectedTaxIds)
                        {
                            if (taxes.TryGetValue(taxId, out var tax))
                            {
                                var taxAmount = (quoteItem.Amount * tax.TaxPercentage) / 100;
                                quoteItemTaxes.Add(new QuoteItemTax
                                {
                                    QuoteItemId = quoteItem.Id,
                                    TaxId = taxId,
                                    TaxAmount = taxAmount
                                });
                                
                                totalTax += taxAmount;
                            }
                        }

                        await _unitOfWork.QuoteItemTaxes.AddRangeAsync(quoteItemTaxes, cancellationToken);
                    }
                }

                // Update quote totals
                quote.SubTotal = subTotal;
                quote.TotalTax = totalTax;
                quote.GrandTotal = subTotal + totalTax;

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Quote {QuoteNumber} created successfully by user {UserId}", quoteNumber, userId);
                
                return Result.Success(quote.Id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error creating quote for user {UserId}", userId);
                return Result.Failure<int>("An error occurred while creating the quote");
            }
        }

        public async Task<Result> UpdateQuoteAsync(
            int quoteId, 
            EditQuoteViewModel model, 
            string userId, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var quote = await _unitOfWork.Quotes.GetByIdAsync(quoteId, cancellationToken);
                
                if (quote == null)
                {
                    return Result.Failure("Quote not found");
                }

                // Only allow updates to specific fields
                quote.Title = model.Title;
                quote.Description = model.Description;
                quote.ValidUntil = model.ValidUntil;
                quote.Status = model.Status;

                // EF Core tracks changes automatically
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Quote {QuoteId} updated by user {UserId}", quoteId, userId);
                
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quote {QuoteId}", quoteId);
                return Result.Failure("An error occurred while updating the quote");
            }
        }

        public async Task<Result> DeleteQuoteAsync(
            int quoteId, 
            string userId, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var quote = await _unitOfWork.Quotes.GetByIdAsync(quoteId, cancellationToken);
                
                if (quote == null)
                {
                    return Result.Failure("Quote not found");
                }

                // Check if quote has associated invoice
                var hasInvoice = await _context.Invoices.AnyAsync(i => i.QuoteId == quoteId, cancellationToken);
                
                if (hasInvoice)
                {
                    return Result.Failure("Cannot delete quote with associated invoice");
                }

                await _unitOfWork.Quotes.DeleteAsync(quote, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Quote {QuoteId} deleted by user {UserId}", quoteId, userId);
                
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting quote {QuoteId}", quoteId);
                return Result.Failure("An error occurred while deleting the quote");
            }
        }

        public async Task<Result> UpdateQuoteStatusAsync(
            int quoteId, 
            string status, 
            string userId, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var quote = await _unitOfWork.Quotes.GetByIdAsync(quoteId, cancellationToken);
                
                if (quote == null)
                {
                    return Result.Failure("Quote not found");
                }

                quote.Status = status;
                // EF Core tracks changes automatically
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Quote {QuoteId} status updated to {Status} by user {UserId}", quoteId, status, userId);
                
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quote status for {QuoteId}", quoteId);
                return Result.Failure("An error occurred while updating the quote status");
            }
        }

        public async Task<bool> CanUserAccessQuoteAsync(
            int quoteId, 
            string userId, 
            string userRole, 
            CancellationToken cancellationToken = default)
        {
            var quote = await _context.Quotes
                .Include(q => q.Client)
                .FirstOrDefaultAsync(q => q.Id == quoteId, cancellationToken);

            if (quote == null) return false;

            if (userRole == Roles.SuperAdmin) return true;
            if (userRole == Roles.Client) return quote.ClientId == userId;
            if (userRole == Roles.Admin || userRole == Roles.Staff)
            {
                // Check if user created the quote or the client belongs to them
                return quote.CreatedById == userId || quote.Client?.CreatedById == userId;
            }

            return false;
        }

        public async Task<string> GenerateQuoteNumberAsync(CancellationToken cancellationToken = default)
        {
            await _quoteNumberLock.WaitAsync(cancellationToken);
            
            try
            {
                var year = DateTime.UtcNow.Year;
                var prefix = $"QT-{year}-";

                var lastQuote = await _context.Quotes
                    .Where(q => q.QuoteNumber.StartsWith(prefix))
                    .OrderByDescending(q => q.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                int nextNumber = 1;
                
                if (lastQuote != null)
                {
                    var parts = lastQuote.QuoteNumber.Split('-');
                    if (parts.Length >= 3 && int.TryParse(parts[2], out var lastNumber))
                    {
                        nextNumber = lastNumber + 1;
                    }
                }

                return $"{prefix}{nextNumber:D4}";
            }
            finally
            {
                _quoteNumberLock.Release();
            }
        }

        private IQueryable<Quote> ApplyRoleBasedFilter(IQueryable<Quote> query, string userId, string userRole)
        {
            if (userRole == Roles.SuperAdmin)
            {
                return query;
            }
            else if (userRole == Roles.Admin)
            {
                var myStaffIds = _context.Users
                    .Where(u => u.CreatedById == userId)
                    .Select(u => u.Id);

                var myTeamClientIds = _context.Users
                    .Where(u => myStaffIds.Contains(u.CreatedById))
                    .Select(u => u.Id);

                return query.Where(q => myTeamClientIds.Contains(q.ClientId));
            }
            else if (userRole == Roles.Staff)
            {
                var myClientIds = _context.Users
                    .Where(u => u.CreatedById == userId)
                    .Select(u => u.Id);

                return query.Where(q => myClientIds.Contains(q.ClientId));
            }
            else if (userRole == Roles.Client)
            {
                return query.Where(q => q.ClientId == userId);
            }

            return query.Where(q => false); // No access by default
        }

        private async Task<string> GenerateQuoteTitleAsync(
            CreateQuoteViewModel model, 
            Dictionary<int, ServiceMaster> services)
        {
            if (!string.IsNullOrWhiteSpace(model.Title))
            {
                return model.Title.Length > 200 ? model.Title.Substring(0, 197) + "..." : model.Title;
            }

            var serviceNames = new List<string>();
            
            foreach (var item in model.Items.Take(3))
            {
                if (item.IsCustomService && !string.IsNullOrWhiteSpace(item.CustomServiceName))
                {
                    serviceNames.Add(item.CustomServiceName);
                }
                else if (item.ServiceId.HasValue && services.TryGetValue(item.ServiceId.Value, out var service))
                {
                    serviceNames.Add(service.ServiceName);
                }
            }

            string title;
            if (serviceNames.Any())
            {
                title = string.Join(", ", serviceNames);
                if (model.Items.Count > 3)
                {
                    title += $" +{model.Items.Count - 3} more";
                }
            }
            else
            {
                var client = await _unitOfWork.Users.GetByIdAsync(model.ClientId);
                title = $"Quote for {client?.FullName ?? "Client"}";
            }

            return title.Length > 200 ? title.Substring(0, 197) + "..." : title;
        }
    }
}
