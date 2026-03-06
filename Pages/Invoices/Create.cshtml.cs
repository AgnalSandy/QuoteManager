using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Constants;
using QuoteManager.Data;
using QuoteManager.Models;
using QuoteManager.ViewModels;

namespace QuoteManager.Pages.Invoices
{
    [Authorize(Roles = "SuperAdmin,Admin,Staff")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public CreateInvoiceViewModel Input { get; set; } = new CreateInvoiceViewModel();

        public async Task<IActionResult> OnGetAsync(int? quoteId)
        {
            await LoadDropdowns();

            if (quoteId.HasValue)
            {
                var quote = await _context.Quotes
                    .Include(q => q.Client)
                    .FirstOrDefaultAsync(q => q.Id == quoteId.Value);

                if (quote != null)
                {
                    Input.QuoteId = quote.Id;
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return Page();
            }

            var quote = await _context.Quotes
                .Include(q => q.Client)
                .Include(q => q.QuoteItems)
                    .ThenInclude(qi => qi.QuoteItemTaxes)
                .FirstOrDefaultAsync(q => q.Id == Input.QuoteId);

            if (quote == null)
            {
                ModelState.AddModelError("", "Quote not found.");
                await LoadDropdowns();
                return Page();
            }

            // Authorization check: Ensure user has access to this quote's client
            var currentUser = await _userManager.GetUserAsync(User);
            var hasAccess = await ValidateQuoteAccessAsync(quote, currentUser!);
            
            if (!hasAccess)
            {
                TempData[TempDataKeys.Error] = "You don't have permission to generate an invoice for this quote.";
                return RedirectToPage("/AccessDenied");
            }

            // Check if quote is accepted
            if (quote.Status != QuoteStatus.Accepted)
            {
                ModelState.AddModelError("", "Only accepted quotes can be converted to invoices.");
                await LoadDropdowns();
                return Page();
            }

            // Check if invoice already exists for this quote
            var existingInvoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.QuoteId == quote.Id);
            
            if (existingInvoice != null)
            {
                TempData[TempDataKeys.Warning] = $"An invoice already exists for this quote (Invoice #{existingInvoice.InvoiceNumber}).";
                return RedirectToPage("./View", new { id = existingInvoice.Id });
            }

            // Generate invoice number
            var lastInvoice = await _context.Invoices
                .OrderByDescending(i => i.Id)
                .FirstOrDefaultAsync();

            var invoiceNumber = lastInvoice == null
                ? "INV-2026-001"
                : $"INV-2026-{(int.Parse(lastInvoice.InvoiceNumber.Split('-')[2]) + 1):D3}";

            // Calculate totals
            var subTotal = quote.QuoteItems.Sum(qi => qi.Amount);
            var vatTotal = quote.QuoteItems
                .SelectMany(qi => qi.QuoteItemTaxes)
                .Sum(qit => qit.TaxAmount);
            var grandTotal = subTotal + vatTotal - Input.Discount;

            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumber,
                QuoteId = quote.Id,
                ClientId = quote.ClientId,
                InvoiceDate = DateTime.UtcNow,
                DueDate = Input.DueDate,
                SubTotal = subTotal,
                VATTotal = vatTotal,
                Discount = Input.Discount,
                GrandTotal = grandTotal,
                Status = InvoiceStatus.Unpaid,
                TemplateType = Input.TemplateType,
                BankName = Input.BankName,
                AccountName = Input.AccountName,
                AccountNumber = Input.AccountNumber,
                Notes = Input.Notes,
                PreparedById = currentUser?.Id
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            TempData[TempDataKeys.Success] = $"Invoice {invoice.InvoiceNumber} generated successfully!";
            return RedirectToPage("./View", new { id = invoice.Id });
        }

        private async Task<bool> ValidateQuoteAccessAsync(Quote quote, ApplicationUser currentUser)
        {
            // SuperAdmin has access to all quotes
            if (User.IsInRole(ApplicationRoles.SuperAdmin))
            {
                return true;
            }

            // Check if the client belongs to the user's hierarchy
            var client = await _context.Users.FindAsync(quote.ClientId);
            if (client == null) return false;

            // Staff can only access quotes for clients they created
            if (User.IsInRole(ApplicationRoles.Staff))
            {
                return client.CreatedById == currentUser.Id;
            }

            // Admin can access quotes for their staff's clients
            if (User.IsInRole(ApplicationRoles.Admin))
            {
                // Check if client was created by Admin or Admin's staff
                if (client.CreatedById == currentUser.Id)
                {
                    return true;
                }

                var isStaffClient = await _context.Users
                    .AnyAsync(staff => staff.Id == client.CreatedById && staff.CreatedById == currentUser.Id);
                
                return isStaffClient;
            }

            return false;
        }

        private async Task LoadDropdowns()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            // Build query for accepted quotes with role-based filtering
            var quotesQuery = _context.Quotes
                .Where(q => q.Status == QuoteStatus.Accepted)
                .Include(q => q.Client)
                .AsQueryable();

            // Apply role-based filtering (same logic as Quotes/Index)
            if (User.IsInRole(ApplicationRoles.SuperAdmin))
            {
                // SuperAdmin sees all accepted quotes
            }
            else if (User.IsInRole(ApplicationRoles.Admin))
            {
                // Admin sees quotes for their staff's clients
                var accessibleClientIds = await _context.Users
                    .Where(u => u.CreatedById == currentUser!.Id || 
                                _context.Users.Any(staff => staff.Id == u.CreatedById && staff.CreatedById == currentUser.Id))
                    .Select(u => u.Id)
                    .ToListAsync();

                quotesQuery = quotesQuery.Where(q => accessibleClientIds.Contains(q.ClientId));
            }
            else if (User.IsInRole(ApplicationRoles.Staff))
            {
                // Staff sees quotes for clients they created
                var myClientIds = await _context.Users
                    .Where(u => u.CreatedById == currentUser!.Id)
                    .Select(u => u.Id)
                    .ToListAsync();

                quotesQuery = quotesQuery.Where(q => myClientIds.Contains(q.ClientId));
            }

            // Load the filtered quotes
            Input.AvailableQuotes = await quotesQuery
                .OrderByDescending(q => q.CreatedDate)
                .Select(q => new SelectListItem
                {
                    Value = q.Id.ToString(),
                    Text = $"{q.QuoteNumber} - {q.Client.FullName} (₹{q.GrandTotal:N2})"
                })
                .ToListAsync();

            // Load template types
            Input.AvailableTemplates = InvoiceTemplateTypes.GetAll()
                .Select(t => new SelectListItem
                {
                    Value = t,
                    Text = t
                })
                .ToList();
        }
    }
}
