using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Constants;
using QuoteManager.Data;
using QuoteManager.Models;
using QuoteManager.ViewModels;

namespace QuoteManager.Pages.Invoices
{
    [Authorize]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class ViewModel : InvoicePageModelBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ViewModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invoice = await _context.Invoices
                .Include(i => i.Quote)
                    .ThenInclude(q => q.QuoteItems)
                        .ThenInclude(qi => qi.Service)
                .Include(i => i.Quote)
                    .ThenInclude(q => q.QuoteItems)
                        .ThenInclude(qi => qi.QuoteItemTaxes)
                            .ThenInclude(qit => qit.Tax)
                .Include(i => i.Client)
                .Include(i => i.PreparedBy)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            // Authorization: Verify user has access to this invoice
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Forbid();
            }

            var hasAccess = await VerifyInvoiceAccess(currentUser.Id, invoice);
            if (!hasAccess)
            {
                return Forbid();
            }

            // Map to ViewModel
            Invoice = new InvoiceDetailsViewModel
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate,
                PaidDate = invoice.PaidDate,
                Status = invoice.Status,
                TemplateType = invoice.TemplateType,
                ClientName = invoice.Client.FullName,
                ClientEmail = invoice.Client.Email,
                ClientPhone = invoice.Client.PhoneNumber,
                QuoteTitle = invoice.Quote.Title,
                QuoteNumber = invoice.Quote.QuoteNumber,
                SubTotal = invoice.SubTotal,
                VATTotal = invoice.VATTotal,
                Discount = invoice.Discount,
                GrandTotal = invoice.GrandTotal,
                BankName = invoice.BankName,
                AccountName = invoice.AccountName,
                SwiftAddress = invoice.SwiftAddress,
                AccountNumber = invoice.AccountNumber,
                IBANNumber = invoice.IBANNumber,
                Notes = invoice.Notes,
                Items = invoice.Quote.QuoteItems.Select(qi => new InvoiceItemViewModel
                {
                    // Handle both catalog services and custom services
                    ServiceName = qi.Service != null ? qi.Service.ServiceName : qi.CustomServiceName ?? "Custom Service",
                    Description = qi.CustomDescription ?? qi.Service?.Description ?? "",
                    Quantity = qi.Quantity,
                    UnitPrice = qi.UnitPrice,
                    Amount = qi.Amount,
                    Taxes = qi.QuoteItemTaxes.Select(qit => new InvoiceTaxItemViewModel
                    {
                        TaxName = qit.Tax.TaxName,
                        TaxPercentage = qit.Tax.TaxPercentage,
                        TaxAmount = qit.TaxAmount
                    }).ToList()
                }).ToList()
            };

            // Load company settings
            var company = await _context.CompanySettings.FirstOrDefaultAsync();
            if (company != null)
            {
                Company = new CompanySettingsViewModel
                {
                    CompanyName = company.CompanyName,
                    AddressLine1 = company.AddressLine1,
                    AddressLine2 = company.AddressLine2,
                    City = company.City,
                    State = company.State,
                    PinCode = company.PinCode,
                    Country = company.Country,
                    PhoneNumber = company.PhoneNumber,
                    Email = company.Email,
                    Website = company.Website,
                    GSTNumber = company.GSTNumber,
                    PANNumber = company.PANNumber,
                    FooterMessage = company.FooterMessage,
                    LogoPath = company.LogoPath
                };
            }

            return Page();
        }

        private async Task<bool> VerifyInvoiceAccess(string userId, Invoice invoice)
        {
            // SuperAdmin can view any invoice
            if (User.IsInRole(ApplicationRoles.SuperAdmin))
            {
                return true;
            }

            // Admin can view invoices for their team's clients
            if (User.IsInRole(ApplicationRoles.Admin))
            {
                var myStaffIds = await _userManager.Users
                    .Where(u => u.CreatedById == userId)
                    .Select(u => u.Id)
                    .ToListAsync();

                var isMyTeamClient = await _userManager.Users
                    .AnyAsync(u => u.Id == invoice.ClientId && u.CreatedById != null && myStaffIds.Contains(u.CreatedById));

                // Also check if client was created directly by admin
                var isMyClient = await _userManager.Users
                    .AnyAsync(u => u.Id == invoice.ClientId && u.CreatedById == userId);

                return isMyTeamClient || isMyClient;
            }

            // Staff can view invoices for their own clients
            if (User.IsInRole(ApplicationRoles.Staff))
            {
                var isMyClient = await _userManager.Users
                    .AnyAsync(u => u.Id == invoice.ClientId && u.CreatedById == userId);

                return isMyClient;
            }

            // Client can view their own invoices
            if (User.IsInRole(ApplicationRoles.Client))
            {
                return invoice.ClientId == userId;
            }

            return false;
        }
    }
}
