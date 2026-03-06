using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Constants;
using QuoteManager.Data;
using QuoteManager.Models;
using QuoteManager.ViewModels;

namespace QuoteManager.Pages.Quotes
{
    [Authorize(Roles = "SuperAdmin,Admin,Staff")]
    public class EditModel : QuoteBasePage
    {
        public EditModel(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            ILogger<EditModel> logger)
            : base(context, userManager, logger)
        {
        }

        [BindProperty]
        public EditQuoteViewModel Input { get; set; } = default!;

        public SelectList StatusList { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var quote = await GetAuthorizedQuoteAsync(id.Value);
            
            if (quote == null)
            {
                TempData[TempDataKeys.Error] = "Quote not found or you don't have permission to edit it.";
                return RedirectToPage("./Index");
            }

            // Map to ViewModel to prevent mass assignment
            Input = new EditQuoteViewModel
            {
                Id = quote.Id,
                QuoteNumber = quote.QuoteNumber,
                ClientName = quote.Client.FullName,
                CreatedDate = quote.CreatedDate,
                Title = quote.Title,
                Description = quote.Description,
                ValidUntil = quote.ValidUntil,
                Status = quote.Status,
                SubTotal = quote.SubTotal,
                TotalTax = quote.TotalTax,
                GrandTotal = quote.GrandTotal
            };

            LoadStatusList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                LoadStatusList();
                return Page();
            }

            // Validate status
            if (!QuoteStatus.IsValid(Input.Status))
            {
                ModelState.AddModelError("Input.Status", "Invalid status selected");
                LoadStatusList();
                return Page();
            }

            // Get original quote from database with tracking
            var quoteToUpdate = await _context.Quotes
                .FirstOrDefaultAsync(q => q.Id == Input.Id);

            if (quoteToUpdate == null)
            {
                return NotFound();
            }

            // Verify access using base page method
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Forbid();

            var hasAccess = await VerifyQuoteAccessAsync(currentUser.Id, quoteToUpdate);
            if (!hasAccess)
            {
                TempData[TempDataKeys.Error] = "You don't have permission to edit this quote.";
                return RedirectToPage("./Index");
            }

            // CRITICAL: Only update allowed fields from ViewModel
            // This prevents mass assignment attacks
            quoteToUpdate.Title = Input.Title;
            quoteToUpdate.Description = Input.Description;
            quoteToUpdate.Status = Input.Status;
            quoteToUpdate.ValidUntil = Input.ValidUntil;

            // Do NOT update: QuoteNumber, ClientId, CreatedById, CreatedDate, SubTotal, TotalTax, GrandTotal
            // These are calculated or system-managed fields

            try
            {
                await _context.SaveChangesAsync();
                
                _logger.LogInformation(
                    "Quote {QuoteId} updated by user {UserId}",
                    quoteToUpdate.Id,
                    currentUser.Id);

                TempData[TempDataKeys.Success] = $"Quote '{quoteToUpdate.QuoteNumber}' updated successfully!";
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!QuoteExists(Input.Id))
                {
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "Concurrency error updating quote {QuoteId}", Input.Id);
                    ModelState.AddModelError("", "The quote was modified by another user. Please refresh and try again.");
                    LoadStatusList();
                    return Page();
                }
            }

            return RedirectToPage("./Index");
        }

        private void LoadStatusList()
        {
            StatusList = new SelectList(QuoteStatus.All);
        }

        private bool QuoteExists(int id)
        {
            return _context.Quotes.Any(e => e.Id == id);
        }
    }
}
