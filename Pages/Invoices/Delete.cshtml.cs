using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Constants;
using QuoteManager.Data;
using QuoteManager.Models;
using System.Linq;
using System.Threading.Tasks;

namespace QuoteManager.Pages.Invoices
{
    [Authorize(Roles = ApplicationRoles.SuperAdmin + "," + ApplicationRoles.Admin + "," + ApplicationRoles.Staff)]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<DeleteModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public Invoice Invoice { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.Quote)
                .Include(i => i.PreparedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Forbid();
            }

            var hasAccess = await VerifyInvoiceAccess(currentUser.Id, invoice);
            if (!hasAccess)
            {
                TempData[TempDataKeys.Error] = "You don't have permission to delete this invoice.";
                return RedirectToPage("./Index");
            }

            Invoice = invoice;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.Quote)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Forbid();
            }

            var hasAccess = await VerifyInvoiceAccess(currentUser.Id, invoice);
            if (!hasAccess)
            {
                TempData[TempDataKeys.Error] = "You don't have permission to delete this invoice.";
                return RedirectToPage("./Index");
            }

            // Security: Only allow deletion of unpaid invoices
            if (invoice.Status != InvoiceStatus.Unpaid)
            {
                TempData[TempDataKeys.Error] = $"Cannot delete invoice '{invoice.InvoiceNumber}' because it has been paid or partially paid. Only unpaid invoices can be deleted.";
                return RedirectToPage("./Index");
            }

            var invoiceNumber = invoice.InvoiceNumber;

            try
            {
                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();

                _logger.LogWarning("Invoice {InvoiceNumber} (ID: {InvoiceId}) deleted by user {UserId}", 
                    invoiceNumber, id, currentUser.Id);

                TempData[TempDataKeys.Success] = $"Invoice '{invoiceNumber}' deleted successfully.";
                return RedirectToPage("./Index");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error deleting invoice {InvoiceId}", id);
                TempData[TempDataKeys.Error] = $"Error deleting invoice '{invoiceNumber}'. Please try again or contact support.";
                return RedirectToPage("./Index");
            }
        }

        private async Task<bool> VerifyInvoiceAccess(string userId, Invoice invoice)
        {
            // SuperAdmin can delete any invoice
            if (User.IsInRole(ApplicationRoles.SuperAdmin))
            {
                return true;
            }

            // Admin can delete invoices for their team's clients
            if (User.IsInRole(ApplicationRoles.Admin))
            {
                var myStaffIds = await _userManager.Users
                    .Where(u => u.CreatedById == userId)
                    .Select(u => u.Id)
                    .ToListAsync();

                var isMyTeamClient = await _userManager.Users
                    .AnyAsync(u => u.Id == invoice.ClientId && u.CreatedById != null && myStaffIds.Contains(u.CreatedById));

                return isMyTeamClient;
            }

            // Staff can delete invoices for their own clients
            if (User.IsInRole(ApplicationRoles.Staff))
            {
                var isMyClient = await _userManager.Users
                    .AnyAsync(u => u.Id == invoice.ClientId && u.CreatedById == userId);

                return isMyClient;
            }

            return false;
        }
    }
}
