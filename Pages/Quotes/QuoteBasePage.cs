using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Constants;
using QuoteManager.Data;
using QuoteManager.Models;

namespace QuoteManager.Pages.Quotes
{
    /// <summary>
    /// Base page for all quote operations with built-in access control
    /// </summary>
    public abstract class QuoteBasePage : PageModel
    {
        protected readonly ApplicationDbContext _context;
        protected readonly UserManager<ApplicationUser> _userManager;
        protected readonly ILogger _logger;

        protected QuoteBasePage(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets current logged-in user
        /// </summary>
        protected async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            return await _userManager.GetUserAsync(User);
        }

        /// <summary>
        /// Verifies if current user has access to view/edit a quote
        /// Implements role-based access control and ownership checks
        /// </summary>
        protected async Task<bool> VerifyQuoteAccessAsync(string currentUserId, Quote quote)
        {
            if (quote == null) return false;

            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            if (currentUser == null) return false;

            var roles = await _userManager.GetRolesAsync(currentUser);
            var primaryRole = roles.FirstOrDefault();

            // SuperAdmin can access all quotes
            if (primaryRole == ApplicationRoles.SuperAdmin)
            {
                return true;
            }

            // Admin can access quotes from their clients and staff
            if (primaryRole == ApplicationRoles.Admin)
            {
                // Quotes created by the admin
                if (quote.CreatedById == currentUserId)
                    return true;

                // Quotes from clients under this admin
                var isClientUnderAdmin = await _context.Users
                    .Where(u => u.Id == quote.ClientId)
                    .AnyAsync(u => u.CreatedById == currentUserId);

                if (isClientUnderAdmin)
                    return true;

                // Quotes from staff under this admin
                var isStaffUnderAdmin = await _context.Users
                    .Where(u => u.Id == quote.CreatedById)
                    .AnyAsync(u => u.CreatedById == currentUserId);

                if (isStaffUnderAdmin)
                    return true;
            }

            // Staff can access their own quotes
            if (primaryRole == ApplicationRoles.Staff)
            {
                if (quote.CreatedById == currentUserId)
                    return true;
            }

            // Client can access quotes where they are the client
            if (primaryRole == ApplicationRoles.Client)
            {
                if (quote.ClientId == currentUserId)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets a quote with full details if user has access
        /// Returns null if quote doesn't exist or user doesn't have access
        /// </summary>
        protected async Task<Quote?> GetAuthorizedQuoteAsync(int quoteId)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return null;

            var quote = await _context.Quotes
                .Include(q => q.Client)
                .Include(q => q.CreatedBy)
                .Include(q => q.QuoteItems)
                    .ThenInclude(qi => qi.Service)
                .Include(q => q.QuoteItems)
                    .ThenInclude(qi => qi.QuoteItemTaxes)
                        .ThenInclude(qit => qit.Tax)
                .FirstOrDefaultAsync(q => q.Id == quoteId);

            if (quote == null) return null;

            var hasAccess = await VerifyQuoteAccessAsync(currentUser.Id, quote);
            if (!hasAccess)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to access quote {QuoteId} without permission",
                    currentUser.Id,
                    quoteId);
                return null;
            }

            return quote;
        }

        /// <summary>
        /// Checks if current user can edit quotes (not a client)
        /// </summary>
        protected async Task<bool> CanEditQuotesAsync()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return false;

            var roles = await _userManager.GetRolesAsync(currentUser);
            return !roles.Contains(ApplicationRoles.Client);
        }

        /// <summary>
        /// Checks if current user can delete quotes (not a client)
        /// </summary>
        protected async Task<bool> CanDeleteQuotesAsync()
        {
            return await CanEditQuotesAsync();
        }
    }
}
