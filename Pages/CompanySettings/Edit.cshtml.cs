using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Data;
using QuoteManager.Models;
using QuoteManager.ViewModels;

namespace QuoteManager.Pages.CompanySettings
{
    [Authorize(Roles = "SuperAdmin")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser>
    _userManager;
        private readonly IWebHostEnvironment _environment;

        public EditModel(
        ApplicationDbContext context,
        UserManager<ApplicationUser>
            userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        [BindProperty]
        public CompanySettingsViewModel Input { get; set; } = new CompanySettingsViewModel();

        public async Task<IActionResult>
            OnGetAsync()
        {
            var settings = await _context.CompanySettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                // Create default settings if none exist
                settings = new Models.CompanySettings
                {
                    CompanyName = "Your Company Name",
                    Country = "India",
                    FooterMessage = "Thank you for your business!",
                    LastUpdated = DateTime.UtcNow
                };
                _context.CompanySettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            Input = new CompanySettingsViewModel
            {
                Id = settings.Id,
                CompanyName = settings.CompanyName,
                AddressLine1 = settings.AddressLine1,
                AddressLine2 = settings.AddressLine2,
                City = settings.City,
                State = settings.State,
                PinCode = settings.PinCode,
                Country = settings.Country,
                PhoneNumber = settings.PhoneNumber,
                SecondaryPhone = settings.SecondaryPhone,
                WhatsAppNumber = settings.WhatsAppNumber,
                Email = settings.Email,
                Website = settings.Website,
                GSTNumber = settings.GSTNumber,
                PANNumber = settings.PANNumber,
                FooterMessage = settings.FooterMessage,
                LogoPath = settings.LogoPath
            };

            return Page();
        }

        public async Task<IActionResult>
            OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var settings = await _context.CompanySettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                return NotFound();
            }

            // Handle logo upload
            if (Input.LogoFile != null)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "logos");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{Input.LogoFile.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.LogoFile.CopyToAsync(fileStream);
                }

                // Delete old logo if exists
                if (!string.IsNullOrEmpty(settings.LogoPath))
                {
                    var oldLogoPath = Path.Combine(_environment.WebRootPath, settings.LogoPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldLogoPath))
                    {
                        System.IO.File.Delete(oldLogoPath);
                    }
                }

                settings.LogoPath = $"/uploads/logos/{uniqueFileName}";
            }

            // Update settings
            settings.CompanyName = Input.CompanyName;
            settings.AddressLine1 = Input.AddressLine1;
            settings.AddressLine2 = Input.AddressLine2;
            settings.City = Input.City;
            settings.State = Input.State;
            settings.PinCode = Input.PinCode;
            settings.Country = Input.Country;
            settings.PhoneNumber = Input.PhoneNumber;
            settings.SecondaryPhone = Input.SecondaryPhone;
            settings.WhatsAppNumber = Input.WhatsAppNumber;
            settings.Email = Input.Email;
            settings.Website = Input.Website;
            settings.GSTNumber = Input.GSTNumber;
            settings.PANNumber = Input.PANNumber;
            settings.FooterMessage = Input.FooterMessage;
            settings.LastUpdated = DateTime.UtcNow;

            var currentUser = await _userManager.GetUserAsync(User);
            settings.UpdatedById = currentUser?.Id;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Company settings updated successfully!";
            return RedirectToPage("./Edit");
        }
    }
}
