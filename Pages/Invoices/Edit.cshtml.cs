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
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EditModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public EditInvoiceViewModel Input { get; set; } = new EditInvoiceViewModel();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.Quote)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            // Authorization check
            var hasAccess = await ValidateInvoiceAccessAsync(invoice);
            if (!hasAccess)
            {
                TempData[TempDataKeys.Error] = "You don't have permission to edit this invoice.";
                return RedirectToPage("/AccessDenied");
            }

            // Populate the view model
            Input = new EditInvoiceViewModel
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate,
                Discount = invoice.Discount,
                TemplateType = invoice.TemplateType,
                Status = invoice.Status,
                PaidDate = invoice.PaidDate,
                BankName = invoice.BankName,
                AccountName = invoice.AccountName,
                AccountNumber = invoice.AccountNumber,
                SwiftAddress = invoice.SwiftAddress,
                IBANNumber = invoice.IBANNumber,
                Notes = invoice.Notes,
                ClientName = invoice.Client.FullName,
                QuoteNumber = invoice.Quote.QuoteNumber,
                SubTotal = invoice.SubTotal,
                VATTotal = invoice.VATTotal,
                GrandTotal = invoice.GrandTotal,
                RowVersion = invoice.RowVersion
            };

            await LoadDropdowns();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return Page();
            }

            var invoice = await _context.Invoices
                .Include(i => i.Quote)
                    .ThenInclude(q => q.QuoteItems)
                        .ThenInclude(qi => qi.QuoteItemTaxes)
                .FirstOrDefaultAsync(i => i.Id == Input.Id);

            if (invoice == null)
            {
                return NotFound();
            }

            // Authorization check
            var hasAccess = await ValidateInvoiceAccessAsync(invoice);
            if (!hasAccess)
            {
                TempData[TempDataKeys.Error] = "You don't have permission to edit this invoice.";
                return RedirectToPage("/AccessDenied");
            }

            // Concurrency check
            if (invoice.RowVersion != null && Input.RowVersion != null)
            {
                if (!invoice.RowVersion.SequenceEqual(Input.RowVersion))
                {
                    TempData[TempDataKeys.Warning] = "This invoice was modified by another user. Please review the changes and try again.";
                    return RedirectToPage("./Edit", new { id = Input.Id });
                }
            }

            // Recalculate grand total if discount changed
            var grandTotal = invoice.SubTotal + invoice.VATTotal - Input.Discount;

            // Update invoice properties
            invoice.InvoiceDate = Input.InvoiceDate;
            invoice.DueDate = Input.DueDate;
            invoice.Discount = Input.Discount;
            invoice.GrandTotal = grandTotal;
            invoice.TemplateType = Input.TemplateType;
            invoice.Status = Input.Status;
            invoice.PaidDate = Input.PaidDate;
            invoice.BankName = Input.BankName;
            invoice.AccountName = Input.AccountName;
            invoice.AccountNumber = Input.AccountNumber;
            invoice.SwiftAddress = Input.SwiftAddress;
            invoice.IBANNumber = Input.IBANNumber;
            invoice.Notes = Input.Notes;

            // If status changed to Paid and no paid date, set it
            if (invoice.Status == InvoiceStatus.Paid && invoice.PaidDate == null)
            {
                invoice.PaidDate = DateTime.UtcNow;
            }

            // If status changed from Paid to something else, clear paid date
            if (invoice.Status != InvoiceStatus.Paid && invoice.PaidDate != null)
            {
                invoice.PaidDate = null;
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData[TempDataKeys.Success] = $"Invoice {invoice.InvoiceNumber} updated successfully!";
                return RedirectToPage("./View", new { id = invoice.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InvoiceExists(invoice.Id))
                {
                    return NotFound();
                }
                else
                {
                    TempData[TempDataKeys.Error] = "This invoice was modified by another user. Please refresh and try again.";
                    return RedirectToPage("./Edit", new { id = Input.Id });
                }
            }
        }

        private async Task<bool> ValidateInvoiceAccessAsync(Invoice invoice)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // SuperAdmin has access to all invoices
            if (User.IsInRole(ApplicationRoles.SuperAdmin))
            {
                return true;
            }

            // Get the client
            var client = await _context.Users.FindAsync(invoice.ClientId);
            if (client == null) return false;

            // Staff can only access invoices for clients they created
            if (User.IsInRole(ApplicationRoles.Staff))
            {
                return client.CreatedById == currentUser!.Id;
            }

            // Admin can access invoices for their staff's clients
            if (User.IsInRole(ApplicationRoles.Admin))
            {
                // Check if client was created by Admin or Admin's staff
                if (client.CreatedById == currentUser!.Id)
                {
                    return true;
                }

                var isStaffClient = await _context.Users
                    .AnyAsync(staff => staff.Id == client.CreatedById && staff.CreatedById == currentUser.Id);

                return isStaffClient;
            }

            return false;
        }

        private bool InvoiceExists(int id)
        {
            return _context.Invoices.Any(e => e.Id == id);
        }

        private async Task LoadDropdowns()
        {
            // Load template types
            Input.AvailableTemplates = InvoiceTemplateTypes.GetAll()
                .Select(t => new SelectListItem
                {
                    Value = t,
                    Text = t
                })
                .ToList();

            // Load invoice statuses
            Input.AvailableStatuses = new List<SelectListItem>
            {
                new SelectListItem { Value = InvoiceStatus.Unpaid, Text = "Unpaid" },
                new SelectListItem { Value = InvoiceStatus.Paid, Text = "Paid" },
                new SelectListItem { Value = InvoiceStatus.PartiallyPaid, Text = "Partially Paid" },
                new SelectListItem { Value = InvoiceStatus.Overdue, Text = "Overdue" },
                new SelectListItem { Value = InvoiceStatus.Cancelled, Text = "Cancelled" }
            };
        }
    }
}
