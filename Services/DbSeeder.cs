using Microsoft.AspNetCore.Identity;
using QuoteManager.Constants;
using QuoteManager.Data;
using QuoteManager.Models;
using Microsoft.EntityFrameworkCore;

namespace QuoteManager.Services
{
    public class DbSeeder
    {
        public static async Task SeedRolesAndSuperAdmin(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Step 1: Create the 4 roles
            string[] roleNames = { Roles.SuperAdmin, Roles.Admin, Roles.Staff, Roles.Client };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Step 2: Create SuperAdmin user
            var superAdminEmail = "superadmin@quotemanager.com";
            var superAdminUser = await userManager.FindByEmailAsync(superAdminEmail);

            if (superAdminUser == null)
            {
                var newSuperAdmin = new ApplicationUser
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    FullName = "Super Administrator",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(newSuperAdmin, "Super@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newSuperAdmin, Roles.SuperAdmin);
                }
            }
            else
            {
                // Ensure SuperAdmin has the SuperAdmin role
                if (!await userManager.IsInRoleAsync(superAdminUser, Roles.SuperAdmin))
                {
                    await userManager.AddToRoleAsync(superAdminUser, Roles.SuperAdmin);
                }
            }
        }

        public static async Task SeedMasterData(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Seed Tax Masters (Indian GST taxes)
            if (!await context.TaxMasters.AnyAsync())
            {
                var taxes = new List<TaxMaster>
        {
            new TaxMaster
            {
                TaxName = "GST",
                TaxPercentage = 18.00m,
                Description = "Goods and Services Tax - Standard Rate",
                IsActive = true
            },
            new TaxMaster
            {
                TaxName = "CGST",
                TaxPercentage = 9.00m,
                Description = "Central Goods and Services Tax",
                IsActive = true
            },
            new TaxMaster
            {
                TaxName = "SGST",
                TaxPercentage = 9.00m,
                Description = "State Goods and Services Tax",
                IsActive = true
            },
            new TaxMaster
            {
                TaxName = "IGST",
                TaxPercentage = 18.00m,
                Description = "Integrated Goods and Services Tax (Interstate)",
                IsActive = true
            },
            new TaxMaster
            {
                TaxName = "GST 12%",
                TaxPercentage = 12.00m,
                Description = "Goods and Services Tax - Reduced Rate",
                IsActive = true
            },
            // Added additional valid GST slabs (not removing anything)
            new TaxMaster
            {
                TaxName = "GST 5%",
                TaxPercentage = 5.00m,
                Description = "Goods and Services Tax - Essential Goods Rate",
                IsActive = true
            },
            new TaxMaster
            {
                TaxName = "GST 28%",
                TaxPercentage = 28.00m,
                Description = "Goods and Services Tax - Luxury Goods Rate",
                IsActive = true
            }
        };

                await context.TaxMasters.AddRangeAsync(taxes);
                await context.SaveChangesAsync();
            }

            // Seed Company Settings (default values)
            if (!await context.CompanySettings.AnyAsync())
            {
                var companySettings = new CompanySettings
                {
                    CompanyName = "QuoteManager",
                    AddressLine1 = "372 Avenue Street",
                    AddressLine2 = "372 Avenue Street",
                    City = "Thrissur",
                    State = "Kerala",
                    PinCode = "680732",
                    Country = "India",
                    PhoneNumber = "+91",
                    Email = "qm@quotemanager.com",
                    Website = "www.quotemanager.com",
                    FooterMessage = "Thank you for choosing QuoteManager. We value your trust and look forward to supporting your business growth.",
                    LastUpdated = DateTime.UtcNow
                };

                await context.CompanySettings.AddAsync(companySettings);
                await context.SaveChangesAsync();
            }
        }





    }
}